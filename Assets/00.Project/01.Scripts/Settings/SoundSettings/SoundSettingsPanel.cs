/*
 * 역할:
 * - 사운드 설정 UI의 슬라이더, 퍼센트 텍스트, 아이콘을 제어하는 패널 스크립트입니다.
 *
 * 주요 기능:
 * - JSON에 저장된 볼륨을 로드해 UI에 반영합니다.
 * - 슬라이더 변경 시 AudioMixer에 즉시 적용하고 SoundSettingsStore에 저장합니다.
 * - 볼륨이 0이면 mute icon, 0보다 크면 playing icon으로 갱신합니다.
 */
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;
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
    [FormerlySerializedAs("animalVolumeParameter")]
    [SerializeField] private string environmentVolumeParameter = "EnvironmentVolume";

    [Header("Sound Icons")]
    [SerializeField] private Sprite soundPlayingIcon;
    [SerializeField] private Sprite soundMuteIcon;

    [Header("Master")]
    [SerializeField] private Image masterIconImage;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private TextMeshProUGUI masterPercentText;

    [Header("BGM")]
    [SerializeField] private Image bgmIconImage;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private TextMeshProUGUI bgmPercentText;

    [Header("UI")]
    [SerializeField] private Image uiIconImage;
    [SerializeField] private Slider uiSlider;
    [SerializeField] private TextMeshProUGUI uiPercentText;

    [Header("Environment")]
    [FormerlySerializedAs("animalIconImage")]
    [SerializeField] private Image environmentIconImage;
    [FormerlySerializedAs("animalSlider")]
    [SerializeField] private Slider environmentSlider;
    [FormerlySerializedAs("animalPercentText")]
    [SerializeField] private TextMeshProUGUI environmentPercentText;

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
        SetSliderWithoutNotify(environmentSlider, data.environmentVolume);

        UpdatePercentText(masterPercentText, data.masterVolume);
        UpdatePercentText(bgmPercentText, data.bgmVolume);
        UpdatePercentText(uiPercentText, data.uiVolume);
        UpdatePercentText(environmentPercentText, data.environmentVolume);

        ApplyVolumeAndRefreshIcon(SoundVolumeChannel.Master, data.masterVolume);
        ApplyVolumeAndRefreshIcon(SoundVolumeChannel.BGM, data.bgmVolume);
        ApplyVolumeAndRefreshIcon(SoundVolumeChannel.UI, data.uiVolume);
        ApplyVolumeAndRefreshIcon(SoundVolumeChannel.Environment, data.environmentVolume);
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

        if (environmentSlider != null)
        {
            environmentSlider.onValueChanged.AddListener(OnEnvironmentSliderChanged);
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

        if (environmentSlider != null)
        {
            environmentSlider.onValueChanged.RemoveListener(OnEnvironmentSliderChanged);
        }

        listenersRegistered = false;
    }

    private void ConfigureSliders()
    {
        ConfigureSlider(masterSlider);
        ConfigureSlider(bgmSlider);
        ConfigureSlider(uiSlider);
        ConfigureSlider(environmentSlider);
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

    private void OnEnvironmentSliderChanged(float sliderValue)
    {
        HandleSliderChanged(SoundVolumeChannel.Environment, sliderValue, environmentPercentText);
    }

    private void HandleSliderChanged(SoundVolumeChannel channel, float sliderValue, TextMeshProUGUI percentText)
    {
        float percent = Mathf.Round(AudioVolumeUtility.SliderValueToPercent(sliderValue));

        UpdatePercentText(percentText, percent);
        ApplyVolumeAndRefreshIcon(channel, percent);
        SoundSettingsStore.SetVolume(channel, percent);
    }

    private void ApplyVolumeAndRefreshIcon(SoundVolumeChannel channel, float percent)
    {
        if (ApplyVolume(channel, percent))
        {
            UpdateVolumeIcon(channel, percent);
        }
    }

    private bool ApplyVolume(SoundVolumeChannel channel, float percent)
    {
        if (settingsApplier != null)
        {
            return settingsApplier.ApplyVolume(channel, percent);
        }

        return AudioVolumeUtility.ApplyPercent(audioMixer, GetFallbackParameterName(channel), percent);
    }

    private void UpdateVolumeIcon(SoundVolumeChannel channel, float percent)
    {
        Image iconImage = GetIconImage(channel);

        if (iconImage == null)
        {
            return;
        }

        Sprite targetIcon = percent <= 0f ? soundMuteIcon : soundPlayingIcon;

        if (targetIcon == null)
        {
            return;
        }

        iconImage.sprite = targetIcon;
        iconImage.enabled = true;
        iconImage.preserveAspect = true;
    }

    private Image GetIconImage(SoundVolumeChannel channel)
    {
        return channel switch
        {
            SoundVolumeChannel.Master => masterIconImage,
            SoundVolumeChannel.BGM => bgmIconImage,
            SoundVolumeChannel.UI => uiIconImage,
            SoundVolumeChannel.Environment => environmentIconImage,
            _ => null
        };
    }

    private string GetFallbackParameterName(SoundVolumeChannel channel)
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

    private void UpdatePercentText(TextMeshProUGUI percentText, float percent)
    {
        if (percentText == null)
        {
            return;
        }

        percentText.text = $"{Mathf.RoundToInt(Mathf.Clamp(percent, 0f, 100f))}%";
    }
}
