using UnityEngine;
using UnityEngine.Audio;

public static class AudioVolumeUtility
{
    public const float MinDecibel = -80f;
    public const float MaxDecibel = 0f;

    public static float SliderValueToPercent(float sliderValue)
    {
        return Mathf.Clamp01(sliderValue) * 100f;
    }

    public static float PercentToSliderValue(float percent)
    {
        return Mathf.Clamp(percent, 0f, 100f) / 100f;
    }

    public static float PercentToDecibel(float percent)
    {
        float normalizedVolume = Mathf.Clamp(percent, 0f, 100f) / 100f;

        if (normalizedVolume <= 0.0001f)
        {
            return MinDecibel;
        }

        return Mathf.Clamp(Mathf.Log10(normalizedVolume) * 20f, MinDecibel, MaxDecibel);
    }
    
    public static bool ApplyPercent(AudioMixer audioMixer, string exposedParameterName, float percent)
    {
        if (audioMixer == null)
        {
            Debug.LogWarning("AudioMixer is not assigned. Sound volume was saved, but mixer volume was not applied.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(exposedParameterName))
        {
            Debug.LogWarning("AudioMixer exposed parameter name is empty.");
            return false;
        }

        float decibel = PercentToDecibel(percent);
        bool applied = audioMixer.SetFloat(exposedParameterName, decibel);

        if (!applied)
        {
            Debug.LogWarning($"AudioMixer exposed parameter was not found: {exposedParameterName}");
        }

        return applied;
    }
}
