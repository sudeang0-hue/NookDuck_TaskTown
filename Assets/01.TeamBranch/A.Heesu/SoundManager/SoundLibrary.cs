using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundLibrary", menuName = "A.Heesu/Sound/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    [SerializeField] private SoundClipData[] sounds;

    private readonly Dictionary<string, SoundClipData> soundLookup = new Dictionary<string, SoundClipData>();

    public bool TryGetSound(string soundId, out SoundClipData soundData)
    {
        BuildLookupIfNeeded();

        if (string.IsNullOrWhiteSpace(soundId))
        {
            soundData = null;
            return false;
        }

        return soundLookup.TryGetValue(soundId, out soundData);
    }

    public bool TryGetRandomSound(SoundCategory category, out SoundClipData soundData)
    {
        List<SoundClipData> candidates = new List<SoundClipData>();

        if (sounds != null)
        {
            for (int i = 0; i < sounds.Length; i++)
            {
                SoundClipData candidate = sounds[i];

                if (candidate != null && candidate.Category == category && candidate.HasClip)
                {
                    candidates.Add(candidate);
                }
            }
        }

        if (candidates.Count == 0)
        {
            soundData = null;
            return false;
        }

        soundData = candidates[Random.Range(0, candidates.Count)];
        return true;
    }

    private void OnEnable()
    {
        RebuildLookup();
    }

    private void OnValidate()
    {
        soundLookup.Clear();
    }

    private void BuildLookupIfNeeded()
    {
        if (soundLookup.Count == 0)
        {
            RebuildLookup();
        }
    }

    private void RebuildLookup()
    {
        soundLookup.Clear();

        if (sounds == null)
        {
            return;
        }

        for (int i = 0; i < sounds.Length; i++)
        {
            SoundClipData soundData = sounds[i];

            if (soundData == null || string.IsNullOrWhiteSpace(soundData.SoundId))
            {
                continue;
            }

            if (!soundLookup.ContainsKey(soundData.SoundId))
            {
                soundLookup.Add(soundData.SoundId, soundData);
            }
        }
    }
}
