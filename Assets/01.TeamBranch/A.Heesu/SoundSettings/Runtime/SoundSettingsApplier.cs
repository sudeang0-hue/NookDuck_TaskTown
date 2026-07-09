using UnityEngine;
using UnityEngine.Audio;

public class SoundSettingsApplier : MonoBehaviour
{
    [Header("Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Exposed Parameters")]
    [SerializeField] private string masterVolumeParameter = "MasterVolume";
    [SerializeField] private string bgmVolumeParameter = "BGMVolume";
    [SerializeField] private string uiVolumeParameter = "UIVolume";
    [SerializeField] private string animalVolumeParameter = "AnimalVolume";

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
        SoundSettingsData data = SoundSettingsStore.Load();

        ApplyVolume(SoundVolumeChannel.Master, data.masterVolume);
        ApplyVolume(SoundVolumeChannel.BGM, data.bgmVolume);
        ApplyVolume(SoundVolumeChannel.UI, data.uiVolume);
        ApplyVolume(SoundVolumeChannel.Animal, data.animalVolume);
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
            SoundVolumeChannel.Animal => animalVolumeParameter,
            _ => masterVolumeParameter
        };
    }
}
