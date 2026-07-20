/*
 * 역할:
 * - 저장된 사운드 설정 JSON 값을 AudioMixer에 적용하는 컴포넌트입니다.
 *
 * 주요 기능:
 * - SoundSettingsStore에서 Master/BGM/UIController_AnimalInvPage/Environment 볼륨을 읽습니다.
 * - 각 값을 CompanionAudioMixer의 exposed parameter에 적용합니다.
 * - SoundManager가 재생 직전 저장 볼륨을 보장할 때 호출합니다.
 */
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

public class SoundSettingsApplier : MonoBehaviour
{
    [Header("Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Exposed Parameters")]
    [SerializeField] private string masterVolumeParameter = "MasterVolume";
    [SerializeField] private string bgmVolumeParameter = "BGMVolume";
    [SerializeField] private string uiVolumeParameter = "UIVolume";
    [FormerlySerializedAs("animalVolumeParameter")]
    [SerializeField] private string environmentVolumeParameter = "EnvironmentVolume";

    [Header("Lifetime")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    private void Awake()
    {
        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }

        ApplySavedSettings();
    }

    public void ApplySavedSettings()
    {
        TryApplySavedSettings();
    }

    public bool TryApplySavedSettings()
    {
        SoundSettingsData data = SoundSettingsStore.Load();

        bool masterApplied = ApplyVolume(SoundVolumeChannel.Master, data.masterVolume);
        bool bgmApplied = ApplyVolume(SoundVolumeChannel.BGM, data.bgmVolume);
        bool uiApplied = ApplyVolume(SoundVolumeChannel.UI, data.uiVolume);
        bool environmentApplied = ApplyVolume(SoundVolumeChannel.Environment, data.environmentVolume);

        return masterApplied && bgmApplied && uiApplied && environmentApplied;
    }

    public bool ApplyVolume(SoundVolumeChannel channel, float percent)
    {
        return AudioVolumeUtility.ApplyPercent(audioMixer, GetParameterName(channel), percent);
    }

    public string GetParameterName(SoundVolumeChannel channel)
    {
        return channel switch
        {
            SoundVolumeChannel.Master => masterVolumeParameter,
            SoundVolumeChannel.BGM => bgmVolumeParameter,
            SoundVolumeChannel.UI => uiVolumeParameter,
            SoundVolumeChannel.Environment => environmentVolumeParameter,
            _ => masterVolumeParameter
        };
    }
}
