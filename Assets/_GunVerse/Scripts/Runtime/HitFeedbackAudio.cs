using UnityEngine;

namespace GunVerse
{
    public static class HitFeedbackAudio
    {
        private const float HIT_VOLUME = 0.45f;
        private const float KILL_VOLUME = 0.8f;
        private const float HIT_PITCH = 1.2f;
        private const float KILL_PITCH = 0.9f;

        public static void PlayHitFeedback(bool isKill, HitCue hitCue)
        {
            AudioClip clip = ResolveClip(isKill, hitCue);
            if (clip == null)
            {
                return;
            }

            ProceduralAudio.Play2D(
                clip,
                isKill ? KILL_VOLUME : HIT_VOLUME,
                isKill ? KILL_PITCH : HIT_PITCH);
        }

        private static AudioClip ResolveClip(bool isKill, HitCue hitCue)
        {
            switch (hitCue)
            {
                case HitCue.Splash:
                case HitCue.Metal:
                case HitCue.Thud:
                case HitCue.Glass:
                case HitCue.Soft:
                case HitCue.Generic:
                default:
                    return isKill ? ProceduralAudio.KillClip : ProceduralAudio.HitClip;
            }
        }
    }
}
