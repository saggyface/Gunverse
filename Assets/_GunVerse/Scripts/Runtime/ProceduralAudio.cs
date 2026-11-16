using UnityEngine;

namespace GunVerse
{
    public static class ProceduralAudio
    {
        private const int SAMPLE_RATE = 44100;
        private const float NOISE_DECAY = 22f;
        private const float CLICK_DECAY = 60f;
        private const float KILL_DECAY = 12f;
        private const float DAMAGE_DECAY = 9f;
        private const float DEATH_DECAY = 5f;
        private const float RESPAWN_DECAY = 8f;
        private const float JUMP_DECAY = 14f;
        private const float RELOAD_COMPLETE_DECAY = 30f;

        private static AudioClip fireClip;
        private static AudioClip reloadClip;
        private static AudioClip reloadCompleteClip;
        private static AudioClip pickupClip;
        private static AudioClip footstepClip;
        private static AudioClip jumpClip;
        private static AudioClip landingClip;
        private static AudioClip hitClip;
        private static AudioClip killClip;
        private static AudioClip damageClip;
        private static AudioClip deathClip;
        private static AudioClip respawnClip;
        private static GunVerseAudioPool pool;

        public static AudioClip FireClip => GetOrCreateClip(ref fireClip, "GunFire", 0.09f, 0.8f, NOISE_DECAY);
        public static AudioClip ReloadClip => GetOrCreateClip(ref reloadClip, "GunReload", 0.12f, 0.6f, NOISE_DECAY);
        public static AudioClip ReloadCompleteClip => GetOrCreateClip(ref reloadCompleteClip, "GunReloadComplete", 0.08f, 0.6f, RELOAD_COMPLETE_DECAY);
        public static AudioClip PickupClip => GetOrCreateClip(ref pickupClip, "GunPickup", 0.1f, 0.5f, NOISE_DECAY);
        public static AudioClip FootstepClip => GetOrCreateClip(ref footstepClip, "GunFootstep", 0.07f, 0.5f, NOISE_DECAY);
        public static AudioClip JumpClip => GetOrCreateClip(ref jumpClip, "GunJump", 0.12f, 0.6f, JUMP_DECAY);
        public static AudioClip LandingClip => GetOrCreateClip(ref landingClip, "GunLanding", 0.14f, 0.8f, NOISE_DECAY);
        public static AudioClip HitClip => GetOrCreateClip(ref hitClip, "GunHitConfirm", 0.05f, 0.7f, CLICK_DECAY);
        public static AudioClip KillClip => GetOrCreateClip(ref killClip, "GunKillConfirm", 0.18f, 0.9f, KILL_DECAY);
        public static AudioClip DamageClip => GetOrCreateClip(ref damageClip, "GunDamage", 0.18f, 0.8f, DAMAGE_DECAY);
        public static AudioClip DeathClip => GetOrCreateClip(ref deathClip, "GunDeath", 0.45f, 0.9f, DEATH_DECAY);
        public static AudioClip RespawnClip => GetOrCreateClip(ref respawnClip, "GunRespawn", 0.25f, 0.6f, RESPAWN_DECAY);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCachedClips()
        {
            fireClip = null;
            reloadClip = null;
            reloadCompleteClip = null;
            pickupClip = null;
            footstepClip = null;
            jumpClip = null;
            landingClip = null;
            hitClip = null;
            killClip = null;
            damageClip = null;
            deathClip = null;
            respawnClip = null;
            pool = null;
        }

        private static AudioClip GetOrCreateClip(ref AudioClip clip, string name, float duration, float volume, float decay)
        {
            if (clip == null)
            {
                clip = Generate(name, duration, volume, decay);
            }

            return clip;
        }

        public static void Play(AudioClip clip, Vector3 position, float volume)
        {
            Play(clip, position, volume, 1f);
        }

        public static void Play(AudioClip clip, Vector3 position, float volume, float pitch)
        {
            if (clip == null)
            {
                return;
            }

            GetPool().Play(clip, position, volume, pitch, 1f);
        }

        public static void Play2D(AudioClip clip, float volume, float pitch = 1f)
        {
            if (clip == null)
            {
                return;
            }

            GetPool().Play(clip, Vector3.zero, volume, pitch, 0f);
        }

        private static GunVerseAudioPool GetPool()
        {
            if (pool != null)
            {
                return pool;
            }

            GameObject poolObject = new GameObject("GunVerseAudioPool");
            Object.DontDestroyOnLoad(poolObject);
            pool = poolObject.AddComponent<GunVerseAudioPool>();
            return pool;
        }

        private static AudioClip Generate(string name, float duration, float volume, float decay)
        {
            int samples = Mathf.CeilToInt(duration * SAMPLE_RATE);
            AudioClip clip = AudioClip.Create(name, samples, 1, SAMPLE_RATE, false);

            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float normalizedTime = (float)i / samples;
                float envelope = Mathf.Exp(-normalizedTime * decay);
                float noise = Random.value * 2f - 1f;
                data[i] = noise * envelope * volume;
            }

            clip.SetData(data, 0);
            return clip;
        }
    }
}
