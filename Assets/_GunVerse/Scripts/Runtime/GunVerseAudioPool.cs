using System.Collections.Generic;
using UnityEngine;

namespace GunVerse
{
    public sealed class GunVerseAudioPool : MonoBehaviour
    {
        private const int POOL_SIZE = 32;
        private const float MIN_PITCH = 0.01f;
        private const float SOURCE_RELEASE_PADDING = 0.05f;

        private readonly List<PoolEntry> entries = new List<PoolEntry>(POOL_SIZE);
        private int nextIndex;

        private sealed class PoolEntry
        {
            public AudioSource Source;
            public float ReleaseTime;
            public bool InUse;
        }

        private void Awake()
        {
            for (int i = 0; i < POOL_SIZE; i++)
            {
                GameObject sourceObject = new GameObject($"GunVerseAudio{i + 1}");
                sourceObject.transform.SetParent(transform, false);

                AudioSource source = sourceObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;

                entries.Add(new PoolEntry { Source = source });
            }
        }

        public void Play(AudioClip clip, Vector3 position, float volume, float pitch, float spatialBlend)
        {
            PoolEntry entry = FindEntry();
            AudioSource source = entry.Source;

            source.Stop();
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.spatialBlend = spatialBlend;
            source.transform.position = position;
            source.Play();

            entry.InUse = true;
            entry.ReleaseTime = Time.time + clip.length / Mathf.Max(MIN_PITCH, Mathf.Abs(pitch)) + SOURCE_RELEASE_PADDING;
        }

        private PoolEntry FindEntry()
        {
            for (int i = 0; i < entries.Count; i++)
            {
                int index = (nextIndex + i) % entries.Count;
                PoolEntry entry = entries[index];
                if (!entry.InUse || !entry.Source.isPlaying)
                {
                    nextIndex = (index + 1) % entries.Count;
                    return entry;
                }
            }

            PoolEntry oldest = entries[0];
            for (int i = 1; i < entries.Count; i++)
            {
                if (entries[i].ReleaseTime < oldest.ReleaseTime)
                {
                    oldest = entries[i];
                }
            }

            return oldest;
        }

        private void Update()
        {
            float now = Time.time;
            foreach (PoolEntry entry in entries)
            {
                if (!entry.InUse || (entry.Source.isPlaying && now < entry.ReleaseTime))
                {
                    continue;
                }

                entry.Source.Stop();
                entry.Source.clip = null;
                entry.InUse = false;
            }
        }
    }
}
