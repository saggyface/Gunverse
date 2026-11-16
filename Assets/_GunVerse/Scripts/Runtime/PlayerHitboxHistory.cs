using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace GunVerse
{
    public sealed class PlayerHitboxHistory : MonoBehaviour
    {
        private const float SAMPLE_INTERVAL_SECONDS = 1f / 120f;
        private const float BUFFER_SECONDS = 1f;
        private const int BUFFER_CAPACITY = 160;

        private struct HitboxSample
        {
            public double Time;
            public Vector3 RootPosition;
            public float CenterY;
            public float Height;
            public float Radius;
        }

        private static readonly Dictionary<ulong, PlayerHitboxHistory> histories =
            new Dictionary<ulong, PlayerHitboxHistory>();

        private readonly HitboxSample[] samples = new HitboxSample[BUFFER_CAPACITY];
        private NetworkObject networkObject;
        private CharacterController characterController;
        private int sampleCount;
        private int sampleHead;
        private double nextSampleTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            histories.Clear();
        }

        public static bool TryGet(ulong clientId, out PlayerHitboxHistory history)
        {
            return histories.TryGetValue(clientId, out history) && history != null;
        }

        private void Awake()
        {
            networkObject = GetComponent<NetworkObject>();
            characterController = GetComponent<CharacterController>();
            if (networkObject == null || characterController == null)
            {
                enabled = false;
                return;
            }

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                histories[networkObject.OwnerClientId] = this;
            }
        }

        private void OnDestroy()
        {
            if (networkObject != null &&
                histories.TryGetValue(networkObject.OwnerClientId, out PlayerHitboxHistory history) &&
                history == this)
            {
                histories.Remove(networkObject.OwnerClientId);
            }
        }

        private void Update()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            {
                return;
            }

            double now = NetworkManager.Singleton.ServerTime.Time;
            if (now < nextSampleTime)
            {
                return;
            }

            nextSampleTime = now + SAMPLE_INTERVAL_SECONDS;
            RecordSample(now);
        }

        private void RecordSample(double time)
        {
            Vector3 center = transform.TransformPoint(characterController.center);
            HitboxSample sample = new HitboxSample
            {
                Time = time,
                RootPosition = transform.position,
                CenterY = center.y - transform.position.y,
                Height = characterController.height * transform.lossyScale.y,
                Radius = characterController.radius * Mathf.Max(
                    transform.lossyScale.x,
                    transform.lossyScale.z)
            };

            samples[sampleHead] = sample;
            sampleHead = (sampleHead + 1) % samples.Length;
            sampleCount = Mathf.Min(sampleCount + 1, samples.Length);
        }

        public bool TryGetCapsule(
            double viewTime,
            out Vector3 bottom,
            out Vector3 top,
            out float radius)
        {
            bottom = Vector3.zero;
            top = Vector3.zero;
            radius = 0f;

            if (sampleCount == 0)
            {
                return false;
            }

            int newestIndex = (sampleHead - 1 + samples.Length) % samples.Length;
            HitboxSample newest = samples[newestIndex];
            int oldestIndex = (sampleHead - sampleCount + samples.Length) % samples.Length;
            HitboxSample oldest = samples[oldestIndex];

            if (viewTime <= oldest.Time)
            {
                ApplyCapsule(oldest, out bottom, out top, out radius);
                return true;
            }

            if (viewTime >= newest.Time)
            {
                ApplyCapsule(newest, out bottom, out top, out radius);
                return true;
            }

            for (int offset = 1; offset < sampleCount; offset++)
            {
                int previousIndex = (sampleHead - offset - 1 + samples.Length) % samples.Length;
                int nextIndex = (sampleHead - offset + samples.Length) % samples.Length;
                HitboxSample previous = samples[previousIndex];
                HitboxSample next = samples[nextIndex];

                if (viewTime < previous.Time || viewTime > next.Time)
                {
                    continue;
                }

                double span = next.Time - previous.Time;
                float t = span > 0.0001
                    ? Mathf.Clamp01((float)((viewTime - previous.Time) / span))
                    : 1f;

                HitboxSample interpolated = new HitboxSample
                {
                    Time = viewTime,
                    RootPosition = Vector3.Lerp(previous.RootPosition, next.RootPosition, t),
                    CenterY = Mathf.Lerp(previous.CenterY, next.CenterY, t),
                    Height = Mathf.Lerp(previous.Height, next.Height, t),
                    Radius = Mathf.Lerp(previous.Radius, next.Radius, t)
                };
                ApplyCapsule(interpolated, out bottom, out top, out radius);
                return true;
            }

            ApplyCapsule(newest, out bottom, out top, out radius);
            return true;
        }

        private static void ApplyCapsule(
            HitboxSample sample,
            out Vector3 bottom,
            out Vector3 top,
            out float radius)
        {
            float halfHeight = sample.Height * 0.5f;
            float centerY = sample.RootPosition.y + sample.CenterY;
            bottom = new Vector3(sample.RootPosition.x, centerY - halfHeight, sample.RootPosition.z);
            top = new Vector3(sample.RootPosition.x, centerY + halfHeight, sample.RootPosition.z);
            radius = sample.Radius;
        }

        public static bool TryGetCapsuleForClient(
            ulong clientId,
            double viewTime,
            out Vector3 bottom,
            out Vector3 top,
            out float radius)
        {
            bottom = Vector3.zero;
            top = Vector3.zero;
            radius = 0f;
            if (!histories.TryGetValue(clientId, out PlayerHitboxHistory history) || history == null)
            {
                return false;
            }

            return history.TryGetCapsule(viewTime, out bottom, out top, out radius);
        }

        public static float DistanceToCapsule(
            Vector3 point,
            Vector3 bottom,
            Vector3 top,
            float radius)
        {
            Vector3 axis = top - bottom;
            float lengthSquared = axis.sqrMagnitude;
            if (lengthSquared <= 0.0001f)
            {
                return Mathf.Max(0f, Vector3.Distance(point, bottom) - radius);
            }

            float t = Mathf.Clamp01(Vector3.Dot(point - bottom, axis) / lengthSquared);
            Vector3 closest = bottom + axis * t;
            return Mathf.Max(0f, Vector3.Distance(point, closest) - radius);
        }
    }
}
