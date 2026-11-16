using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace GunVerse
{
    public static class ServerProjectileHistory
    {
        private const float HISTORY_SECONDS = 1f;
        private const float SAMPLE_INTERVAL_SECONDS = 1f / 120f;
        private const float CAPSULE_HIT_TOLERANCE = 0.2f;
        private const int BUFFER_CAPACITY = 768;
        private static readonly float[] HitTimeOffsets =
        {
            0f,
            -0.05f,
            0.05f,
            -0.1f,
            0.1f
        };

        private struct ProjectileSample
        {
            public double Time;
            public Vector3 Position;
        }

        private sealed class ProjectileTrack
        {
            public ulong ShooterClientId;
            public float Damage;
            public HitCue HitCue;
            public float Radius;
            public double ExpireTime;
            public readonly ProjectileSample[] Samples = new ProjectileSample[BUFFER_CAPACITY];
            public int SampleCount;
            public int SampleHead;
            public double LastSampleTime;
            public readonly HashSet<ulong> HitVictims = new HashSet<ulong>();
        }

        private static readonly Dictionary<ulong, ProjectileTrack> tracks =
            new Dictionary<ulong, ProjectileTrack>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            tracks.Clear();
        }

        public static void Reset()
        {
            tracks.Clear();
        }

        public static void Register(
            ulong projectileId,
            ulong shooterClientId,
            float damage,
            HitCue hitCue,
            float radius,
            double now,
            Vector3 position)
        {
            ProjectileTrack track = new ProjectileTrack
            {
                ShooterClientId = shooterClientId,
                Damage = damage,
                HitCue = hitCue,
                Radius = Mathf.Max(0.02f, radius),
                ExpireTime = now + HISTORY_SECONDS
            };

            tracks[projectileId] = track;
            RecordSample(track, now, position);
            PruneExpired(now);
        }

        public static void Record(ulong projectileId, double now, Vector3 position)
        {
            if (!tracks.TryGetValue(projectileId, out ProjectileTrack track))
            {
                return;
            }

            if (now - track.LastSampleTime < SAMPLE_INTERVAL_SECONDS)
            {
                return;
            }

            RecordSample(track, now, position);
            track.ExpireTime = Mathf.Max((float)track.ExpireTime, (float)(now + HISTORY_SECONDS));
        }

        public static void MarkExpired(ulong projectileId, double now)
        {
            if (tracks.TryGetValue(projectileId, out ProjectileTrack track))
            {
                track.ExpireTime = now + HISTORY_SECONDS;
            }
        }

        public static bool TryApplyHit(
            ulong projectileId,
            ulong shooterClientId,
            ulong victimClientId,
            Vector3 hitPoint,
            double projectileViewTime,
            double victimViewTime,
            out string rejection)
        {
            rejection = string.Empty;
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsServer)
            {
                rejection = "not-server";
                return false;
            }

            double now = networkManager.ServerTime.Time;
            PruneExpired(now);

            if (!tracks.TryGetValue(projectileId, out ProjectileTrack track))
            {
                rejection = "no-projectile";
                return false;
            }

            if (track.ShooterClientId != shooterClientId)
            {
                rejection = "wrong-shooter";
                return false;
            }

            if (track.HitVictims.Contains(victimClientId))
            {
                rejection = "duplicate";
                return false;
            }

            if (projectileViewTime < track.Samples[OldestIndex(track)].Time - 0.1 ||
                projectileViewTime > track.ExpireTime)
            {
                rejection = "time-window";
                return false;
            }

            if (!HasServerProjectileNearVictim(
                    track,
                    victimClientId,
                    projectileViewTime,
                    victimViewTime,
                    out float distance))
            {
                rejection = "not-near-capsule";
                return false;
            }

            NetworkObject victimObject = networkManager.SpawnManager.GetPlayerNetworkObject(victimClientId);
            if (victimObject == null ||
                !victimObject.TryGetComponent(out PlayerHealth victimHealth) ||
                victimHealth.IsDead ||
                victimHealth.OwnerClientId == shooterClientId)
            {
                rejection = "invalid-victim";
                return false;
            }

            track.HitVictims.Add(victimClientId);
            ProjectileTrace.Log(
                "projectile hit accepted projectile=" + projectileId +
                " shooter=" + shooterClientId +
                " victim=" + victimClientId +
                " point=" + hitPoint.ToString("F3") +
                " projectileView=" + projectileViewTime.ToString("F3") +
                " victimView=" + victimViewTime.ToString("F3") +
                " distance=" + distance.ToString("F3"));
            victimHealth.ApplyDamage(track.Damage, shooterClientId, track.HitCue);
            return true;
        }

        private static void RecordSample(ProjectileTrack track, double now, Vector3 position)
        {
            track.Samples[track.SampleHead] = new ProjectileSample
            {
                Time = now,
                Position = position
            };
            track.SampleHead = (track.SampleHead + 1) % track.Samples.Length;
            track.SampleCount = Mathf.Min(track.SampleCount + 1, track.Samples.Length);
            track.LastSampleTime = now;
        }

        private static int OldestIndex(ProjectileTrack track)
        {
            return (track.SampleHead - track.SampleCount + track.Samples.Length) % track.Samples.Length;
        }

        private static bool HasServerProjectileNearVictim(
            ProjectileTrack track,
            ulong victimClientId,
            double projectileViewTime,
            double victimViewTime,
            out float bestDistance)
        {
            bestDistance = float.MaxValue;
            foreach (float offset in HitTimeOffsets)
            {
                double sampleProjectileTime = projectileViewTime + offset;
                double sampleVictimTime = victimViewTime + offset;
                if (!PlayerHitboxHistory.TryGetCapsuleForClient(
                        victimClientId,
                        sampleVictimTime,
                        out Vector3 bottom,
                        out Vector3 top,
                        out float capsuleRadius))
                {
                    continue;
                }

                if (!TryGetProjectileSegment(
                        track,
                        sampleProjectileTime,
                        out Vector3 segmentStart,
                        out Vector3 segmentEnd))
                {
                    continue;
                }

                float distance = DistanceBetweenSegments(
                    bottom,
                    top,
                    segmentStart,
                    segmentEnd);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                }

                if (distance <= capsuleRadius + track.Radius + CAPSULE_HIT_TOLERANCE)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetProjectileSegment(
            ProjectileTrack track,
            double viewTime,
            out Vector3 start,
            out Vector3 end)
        {
            start = Vector3.zero;
            end = Vector3.zero;
            if (track.SampleCount == 0)
            {
                return false;
            }

            if (track.SampleCount == 1)
            {
                int onlyIndex = (track.SampleHead - 1 + track.Samples.Length) % track.Samples.Length;
                start = track.Samples[onlyIndex].Position;
                end = start;
                return true;
            }

            int oldest = OldestIndex(track);
            ProjectileSample previous = track.Samples[oldest];
            ProjectileSample next = track.Samples[(oldest + 1) % track.Samples.Length];

            for (int offset = 1; offset < track.SampleCount; offset++)
            {
                int previousIndex = (oldest + offset - 1) % track.Samples.Length;
                int nextIndex = (oldest + offset) % track.Samples.Length;
                previous = track.Samples[previousIndex];
                next = track.Samples[nextIndex];
                if (viewTime <= next.Time)
                {
                    break;
                }
            }

            start = previous.Position;
            end = next.Position;
            return true;
        }

        private static float DistanceBetweenSegments(
            Vector3 firstStart,
            Vector3 firstEnd,
            Vector3 secondStart,
            Vector3 secondEnd)
        {
            Vector3 firstDirection = firstEnd - firstStart;
            Vector3 secondDirection = secondEnd - secondStart;
            Vector3 offset = firstStart - secondStart;
            float firstLengthSquared = Vector3.Dot(firstDirection, firstDirection);
            float secondLengthSquared = Vector3.Dot(secondDirection, secondDirection);
            float directionDot = Vector3.Dot(firstDirection, secondDirection);
            float firstOffsetDot = Vector3.Dot(firstDirection, offset);
            float secondOffsetDot = Vector3.Dot(secondDirection, offset);
            float denominator = firstLengthSquared * secondLengthSquared -
                                directionDot * directionDot;

            float firstParameter;
            float secondParameter;

            if (denominator < 0.0001f || firstLengthSquared < 0.0001f || secondLengthSquared < 0.0001f)
            {
                firstParameter = 0f;
                secondParameter = firstLengthSquared < 0.0001f ? 0f : secondOffsetDot / secondLengthSquared;
            }
            else
            {
                firstParameter = (directionDot * secondOffsetDot - secondLengthSquared * firstOffsetDot) /
                                 denominator;
                secondParameter = (firstLengthSquared * secondOffsetDot - directionDot * firstOffsetDot) /
                                  denominator;
            }

            firstParameter = Mathf.Clamp01(firstParameter);
            secondParameter = Mathf.Clamp01(secondParameter);

            Vector3 firstPoint = firstStart + firstDirection * firstParameter;
            Vector3 secondPoint = secondStart + secondDirection * secondParameter;
            return Vector3.Distance(firstPoint, secondPoint);
        }

        private static void PruneExpired(double now)
        {
            if (tracks.Count == 0)
            {
                return;
            }

            List<ulong> expired = null;
            foreach (KeyValuePair<ulong, ProjectileTrack> pair in tracks)
            {
                if (pair.Value.ExpireTime < now)
                {
                    expired ??= new List<ulong>();
                    expired.Add(pair.Key);
                }
            }

            if (expired == null)
            {
                return;
            }

            foreach (ulong projectileId in expired)
            {
                tracks.Remove(projectileId);
            }
        }
    }
}
