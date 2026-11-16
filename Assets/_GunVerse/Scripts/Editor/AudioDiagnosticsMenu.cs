using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GunVerse.EditorTools
{
    public static class AudioDiagnosticsMenu
    {
        private const int SAMPLE_RATE = 48000;
        private const int DURATION_SECONDS = 2;
        private const float FREQUENCY = 440f;
        private const float AMPLITUDE = 0.25f;

        private static AudioClip testClip;

        [MenuItem("GunVerse/Diagnostics/Play Editor Test Tone")]
        private static void PlayEditorTestTone()
        {
            if (testClip == null)
            {
                testClip = CreateTestTone();
            }

            MethodInfo playMethod = GetPlayPreviewClipMethod();
            if (playMethod == null)
            {
                Debug.LogError("[AudioDiagnostics] UnityEditor.AudioUtil.PlayPreviewClip was not found.");
                return;
            }

            playMethod.Invoke(null, new object[] { testClip, 0, false });
            LogAudioState();
        }

        [MenuItem("GunVerse/Diagnostics/Stop Editor Test Tone")]
        private static void StopEditorTestTone()
        {
            MethodInfo stopMethod = GetAudioUtilMethod("StopAllPreviewClips");
            stopMethod?.Invoke(null, null);
        }

        [MenuItem("GunVerse/Diagnostics/Log Audio State")]
        private static void LogAudioState()
        {
            AudioListener[] listeners = Object.FindObjectsByType<AudioListener>();
            Debug.Log(
                $"[AudioDiagnostics] supportsAudio={SystemInfo.supportsAudio} " +
                $"outputSampleRate={AudioSettings.outputSampleRate} " +
                $"speakerMode={AudioSettings.speakerMode} " +
                $"listenerVolume={AudioListener.volume} " +
                $"activeListeners={listeners.Length}");

            foreach (AudioListener listener in listeners)
            {
                Debug.Log($"[AudioDiagnostics] listener '{listener.name}' enabled={listener.enabled} active={listener.gameObject.activeInHierarchy}", listener);
            }
        }

        private static AudioClip CreateTestTone()
        {
            int sampleCount = SAMPLE_RATE * DURATION_SECONDS;
            AudioClip clip = AudioClip.Create("GunVerseAudioDiagnosticsTone", sampleCount, 1, SAMPLE_RATE, false);
            clip.hideFlags = HideFlags.DontSave;

            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                samples[i] = AMPLITUDE * Mathf.Sin(2f * Mathf.PI * FREQUENCY * i / SAMPLE_RATE);
            }

            clip.SetData(samples, 0);
            return clip;
        }

        private static MethodInfo GetPlayPreviewClipMethod()
        {
            return GetAudioUtilMethod("PlayPreviewClip", typeof(AudioClip), typeof(int), typeof(bool));
        }

        private static MethodInfo GetAudioUtilMethod(string methodName, params System.Type[] parameterTypes)
        {
            System.Type audioUtilType = System.Type.GetType("UnityEditor.AudioUtil,UnityEditor");
            return audioUtilType?.GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                parameterTypes.Length == 0 ? System.Type.EmptyTypes : parameterTypes,
                null);
        }
    }
}
