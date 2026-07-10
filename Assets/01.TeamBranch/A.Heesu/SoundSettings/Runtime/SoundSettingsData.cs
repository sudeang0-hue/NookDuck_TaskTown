using System;
using UnityEngine;
using UnityEngine.Serialization;

public enum SoundVolumeChannel
{
    Master,
    BGM,
    UI,
    Environment
}

[Serializable]
public class SoundSettingsData
{
    public float masterVolume = 100f;
    public float bgmVolume = 100f;
    public float uiVolume = 100f;
    [FormerlySerializedAs("animalVolume")] public float environmentVolume = 100f;

    public void Clamp()
    {
        masterVolume = ClampPercent(masterVolume);
        bgmVolume = ClampPercent(bgmVolume);
        uiVolume = ClampPercent(uiVolume);
        environmentVolume = ClampPercent(environmentVolume);
    }

    public float GetVolume(SoundVolumeChannel channel)
    {
        return channel switch
        {
            SoundVolumeChannel.Master => masterVolume,
            SoundVolumeChannel.BGM => bgmVolume,
            SoundVolumeChannel.UI => uiVolume,
            SoundVolumeChannel.Environment => environmentVolume,
            _ => 100f
        };
    }

    public void SetVolume(SoundVolumeChannel channel, float percent)
    {
        float clampedPercent = ClampPercent(percent);

        switch (channel)
        {
            case SoundVolumeChannel.Master:
                masterVolume = clampedPercent;
                break;
            case SoundVolumeChannel.BGM:
                bgmVolume = clampedPercent;
                break;
            case SoundVolumeChannel.UI:
                uiVolume = clampedPercent;
                break;
            case SoundVolumeChannel.Environment:
                environmentVolume = clampedPercent;
                break;
        }
    }

    public SoundSettingsData Clone()
    {
        return new SoundSettingsData
        {
            masterVolume = masterVolume,
            bgmVolume = bgmVolume,
            uiVolume = uiVolume,
            environmentVolume = environmentVolume
        };
    }

    private static float ClampPercent(float percent)
    {
        return Mathf.Clamp(percent, 0f, 100f);
    }
}
