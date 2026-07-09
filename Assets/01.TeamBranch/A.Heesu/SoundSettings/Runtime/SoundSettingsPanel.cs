using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SoundSettingsPanel : MonoBehaviour
{
    [Header("Mixer")]
    [SerializeField] private SoundSettingsApplier settingsApplier;
    [SerializeField] private AudioMixer audioMixer;

    [Header("Fallback Exposed Parameters")]
    [SerializeField] private string masterVolumeParameter = "MasterVolume";
    [SerializeField] private string bgmVolumeParameter = "BGMVolume";
    [SerializeField] private string uiVolumeParameter = "UIVolume";
    [SerializeField] private string animalVolumeParameter = "AnimalVolume";

    [Header("Master")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private TextMeshProUGUI masterPercentText;

    [Header("BGM")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private TextMeshProUGUI bgmPercentText;

    [Header("UI")]
    [SerializeField] private Slider uiSlider;
    [SerializeField] private TextMeshProUGUI uiPercentText;

    [Header("Animal")]
    [SerializeField] private Slider animalSlider;
    [SerializeField] private TextMeshProUGUI animalPercentText;

    private bool listenersRegistered;

    private void Awake()
    {
        ConfigureSliders();
        RegisterListeners();
    }

    private void OnEnable()
    {
        RefreshFromStore();
    }

    private void OnDestroy()
    {
        UnregisterListeners();
    }

    public void RefreshFromStore()
    {
        SoundSettingsData data = SoundSettingsStore.Load();

        SetSliderWithoutNotify(masterSlider, data.masterVolume);
        SetSliderWithoutNotify(bgmSlider, data.bgmVolume);
        SetSliderWithoutNotify(uiSlider, data.uiVolume);
        SetSliderWithoutNotify(animalSlider, data.animalVolume);

        UpdatePercentText(masterPercentText, data.masterVolume);
        UpdatePercentText(bgmPercentText, data.bgmVolume);
        UpdatePercentText(uiPercentText, data.uiVolume);
        UpdatePercentText(animalPercentText, data.animalVolume);

        ApplyVolume(SoundVolumeChannel.Master, data.masterVolume);
        ApplyVolume(SoundVolumeChannel.BGM, data.bgmVolume);
        ApplyVolume(SoundVolumeChannel.UI, data.uiVolume);
        ApplyVolume(SoundVolumeChannel.Animal, data.animalVolume);
    }

    private void RegisterListeners()
    {
        if (listenersRegistered)
        {
            return;
        }

        if (masterSlider != null)
        {
            masterSlider.onValueChanged.AddListener(OnMasterSliderChanged);
        }

        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.AddListener(OnBGMSliderChanged);
        }

        if (uiSlider != null)
        {
            uiSlider.onValueChanged.AddListener(OnUISliderChanged);
        }

        if (animalSlider != null)
        {
            animalSlider.onValueChanged.AddListener(OnAnimalSliderChanged);
        }

        listenersRegistered = true;
    }

    private void UnregisterListeners()
    {
        if (!listenersRegistered)
        {
            return;
        }

        if (masterSlider != null)
        {
            masterSlider.onValueChanged.RemoveListener(OnMasterSliderChanged);
        }

        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveListener(OnBGMSliderChanged);
        }

        if (uiSlider != null)
        {
            uiSlider.onValueChanged.RemoveListener(OnUISliderChanged);
        }

        if (animalSlider != null)
        {
            animalSlider.onValueChanged.RemoveListener(OnAnimalSliderChanged);
        }

        listenersRegistered = false;
    }

    private void ConfigureSliders()
    {
        ConfigureSlider(masterSlider);
        ConfigureSlider(bgmSlider);
        ConfigureSlider(uiSlider);
        ConfigureSlider(animalSlider);
    }

    private void ConfigureSlider(Slider slider)
    {
        if (slider == null)
        {
            return;
        }

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
    }

    private void SetSliderWithoutNotify(Slider slider, float percent)
    {
        if (slider == null)
        {
            return;
        }

        slider.SetValueWithoutNotify(AudioVolumeUtility.PercentToSliderValue(percent));
    }

    private void OnMasterSliderChanged(float sliderValue)
    {
        HandleSliderChanged(SoundVolumeChannel.Master, sliderValue, masterPercentText);
    }

    private void OnBGMSliderChanged(float sliderValue)
    {
        HandleSliderChanged(SoundVolumeChannel.BGM, sliderValue, bgmPercentText);
    }

    private void OnUISliderChanged(float sliderValue)
    {
        HandleSliderChanged(SoundVolumeChannel.UI, sliderValue, uiPercentText);
    }

    private void OnAnimalSliderChanged(float sliderValue)
    {
        HandleSliderChanged(SoundVolumeChannel.Animal, sliderValue, animalPercentText);
    }

    private void HandleSliderChanged(SoundVolumeChannel channel, float sliderValue, TextMeshProUGUI percentText)
    {
        float percent = Mathf.Round(AudioVolumeUtility.SliderValueToPercent(sliderValue));

        UpdatePercentText(percentText, percent);
        ApplyVolume(channel, percent);
        SoundSettingsStore.SetVolume(channel, percent);
    }

    private void ApplyVolume(SoundVolumeChannel channel, float percent)
    {
        if (settingsApplier != null)
        {
            settingsApplier.ApplyVolume(channel, percent);
            return;
        }

        AudioVolumeUtility.ApplyPercent(audioMixer, GetFallbackParameterName(channel), percent);
    }

    private string GetFallbackParameterName(SoundVolumeChannel channel)
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

    private void UpdatePercentText(TextMeshProUGUI percentText, float percent)
    {
        if (percentText == null)
        {
            return;
        }

        percentText.text = $"{Mathf.RoundToInt(Mathf.Clamp(percent, 0f, 100f))}%";
    }
}
