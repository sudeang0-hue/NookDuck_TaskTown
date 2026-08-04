/*
 * 역할:
 * - 저장/로드되는 사운드 설정 값의 데이터 모델입니다.
 *
 * 주요 기능:
 * - Master, BGM, UIController_AnimalInvPage, Environment 볼륨 percent를 보관합니다.
 * - BGM 플레이리스트의 재생 모드를 보관합니다.
 * - 채널별 마지막 0 초과 볼륨을 보관해 음소거 해제 시 복원값으로 사용합니다.
 * - 채널별 Get/Set, Clamp, Clone을 제공합니다.
 */
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
    private const float DefaultVolumePercent = 100f;

    public float masterVolume = 100f;
    public float bgmVolume = 100f;
    public float uiVolume = 100f;
    [FormerlySerializedAs("animalVolume")] public float environmentVolume = 100f;
    public BGMPlaybackMode bgmPlaybackMode = BGMPlaybackMode.SequentialLoop;

    public float masterLastNonZeroVolume;
    public float bgmLastNonZeroVolume;
    public float uiLastNonZeroVolume;
    public float environmentLastNonZeroVolume;

    public void Clamp()
    {
        masterVolume = ClampPercent(masterVolume);
        bgmVolume = ClampPercent(bgmVolume);
        uiVolume = ClampPercent(uiVolume);
        environmentVolume = ClampPercent(environmentVolume);
        bgmPlaybackMode = NormalizeBGMPlaybackMode(bgmPlaybackMode);

        masterLastNonZeroVolume = NormalizeLastNonZeroVolume(masterLastNonZeroVolume, masterVolume);
        bgmLastNonZeroVolume = NormalizeLastNonZeroVolume(bgmLastNonZeroVolume, bgmVolume);
        uiLastNonZeroVolume = NormalizeLastNonZeroVolume(uiLastNonZeroVolume, uiVolume);
        environmentLastNonZeroVolume = NormalizeLastNonZeroVolume(environmentLastNonZeroVolume, environmentVolume);
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
                UpdateLastNonZeroVolume(ref masterLastNonZeroVolume, clampedPercent);
                break;
            case SoundVolumeChannel.BGM:
                bgmVolume = clampedPercent;
                UpdateLastNonZeroVolume(ref bgmLastNonZeroVolume, clampedPercent);
                break;
            case SoundVolumeChannel.UI:
                uiVolume = clampedPercent;
                UpdateLastNonZeroVolume(ref uiLastNonZeroVolume, clampedPercent);
                break;
            case SoundVolumeChannel.Environment:
                environmentVolume = clampedPercent;
                UpdateLastNonZeroVolume(ref environmentLastNonZeroVolume, clampedPercent);
                break;
        }
    }

    public float GetLastNonZeroVolume(SoundVolumeChannel channel)
    {
        return channel switch
        {
            SoundVolumeChannel.Master => NormalizeLastNonZeroVolume(masterLastNonZeroVolume, masterVolume),
            SoundVolumeChannel.BGM => NormalizeLastNonZeroVolume(bgmLastNonZeroVolume, bgmVolume),
            SoundVolumeChannel.UI => NormalizeLastNonZeroVolume(uiLastNonZeroVolume, uiVolume),
            SoundVolumeChannel.Environment => NormalizeLastNonZeroVolume(environmentLastNonZeroVolume, environmentVolume),
            _ => DefaultVolumePercent
        };
    }

    public BGMPlaybackMode GetBGMPlaybackMode()
    {
        return NormalizeBGMPlaybackMode(bgmPlaybackMode);
    }

    public void SetBGMPlaybackMode(BGMPlaybackMode mode)
    {
        bgmPlaybackMode = NormalizeBGMPlaybackMode(mode);
    }

    public SoundSettingsData Clone()
    {
        return new SoundSettingsData
        {
            masterVolume = masterVolume,
            bgmVolume = bgmVolume,
            uiVolume = uiVolume,
            environmentVolume = environmentVolume,
            bgmPlaybackMode = GetBGMPlaybackMode(),
            masterLastNonZeroVolume = masterLastNonZeroVolume,
            bgmLastNonZeroVolume = bgmLastNonZeroVolume,
            uiLastNonZeroVolume = uiLastNonZeroVolume,
            environmentLastNonZeroVolume = environmentLastNonZeroVolume
        };
    }

    private static void UpdateLastNonZeroVolume(ref float lastNonZeroVolume, float percent)
    {
        if (percent > 0f)
        {
            lastNonZeroVolume = percent;
        }
    }

    private static float NormalizeLastNonZeroVolume(float lastNonZeroVolume, float currentVolume)
    {
        if (lastNonZeroVolume > 0f)
        {
            return ClampPercent(lastNonZeroVolume);
        }

        return currentVolume > 0f ? currentVolume : DefaultVolumePercent;
    }

    private static BGMPlaybackMode NormalizeBGMPlaybackMode(BGMPlaybackMode mode)
    {
        return Enum.IsDefined(typeof(BGMPlaybackMode), mode)
            ? mode
            : BGMPlaybackMode.SequentialLoop;
    }

    private static float ClampPercent(float percent)
    {
        return Mathf.Clamp(percent, 0f, 100f);
    }
}
