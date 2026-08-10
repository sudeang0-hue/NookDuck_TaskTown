/*
 * 역할:
 * - 사운드 설정 UI의 슬라이더, 퍼센트 텍스트, 아이콘을 제어하는 패널 스크립트입니다.
 *
 * 주요 기능:
 * - JSON에 저장된 볼륨을 로드해 UI에 반영합니다.
 * - 슬라이더 변경 시 AudioMixer에 즉시 적용하고 SoundSettingsStore에 저장합니다.
 * - 볼륨 아이콘 클릭 시 해당 채널을 음소거하거나 마지막 볼륨으로 복원합니다.
 * - 볼륨이 0이면 mute icon, 0보다 크면 playing icon으로 갱신합니다.
 * - Master, BGM, UI 채널은 각각 전용 playing/mute icon을 사용할 수 있습니다.
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

    [Header("Sound Icon Fallback")]
    [Tooltip("채널별 아이콘이 연결되지 않았을 때 사용하는 기본 재생 아이콘입니다.")]
    [SerializeField] private Sprite soundPlayingIcon;
    [Tooltip("채널별 아이콘이 연결되지 않았을 때 사용하는 기본 음소거 아이콘입니다.")]
    [SerializeField] private Sprite soundMuteIcon;

    [Header("Master")]
    [SerializeField] private Sprite masterPlayingIcon;
    [SerializeField] private Sprite masterMuteIcon;
    [SerializeField] private Image masterIconImage;
    [SerializeField] private Button masterMuteButton;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private TextMeshProUGUI masterPercentText;

    [Header("BGM")]
    [SerializeField] private Sprite bgmPlayingIcon;
    [SerializeField] private Sprite bgmMuteIcon;
    [SerializeField] private Image bgmIconImage;
    [SerializeField] private Button bgmMuteButton;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private TextMeshProUGUI bgmPercentText;

    [Header("UI")]
    [SerializeField] private Sprite uiPlayingIcon;
    [SerializeField] private Sprite uiMuteIcon;
    [SerializeField] private Image uiIconImage;
    [SerializeField] private Button uiMuteButton;
    [SerializeField] private Slider uiSlider;
    [SerializeField] private TextMeshProUGUI uiPercentText;

    [Header("Environment")]
    [FormerlySerializedAs("toolIconImage")]
    [SerializeField] private Image environmentIconImage;
    [SerializeField] private Button environmentMuteButton;
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

        if (masterMuteButton != null)
        {
            masterMuteButton.onClick.AddListener(OnMasterMuteButtonClicked);
        }

        if (bgmMuteButton != null)
        {
            bgmMuteButton.onClick.AddListener(OnBGMMuteButtonClicked);
        }

        if (uiMuteButton != null)
        {
            uiMuteButton.onClick.AddListener(OnUIMuteButtonClicked);
        }

        if (environmentMuteButton != null)
        {
            environmentMuteButton.onClick.AddListener(OnEnvironmentMuteButtonClicked);
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

        if (masterMuteButton != null)
        {
            masterMuteButton.onClick.RemoveListener(OnMasterMuteButtonClicked);
        }

        if (bgmMuteButton != null)
        {
            bgmMuteButton.onClick.RemoveListener(OnBGMMuteButtonClicked);
        }

        if (uiMuteButton != null)
        {
            uiMuteButton.onClick.RemoveListener(OnUIMuteButtonClicked);
        }

        if (environmentMuteButton != null)
        {
            environmentMuteButton.onClick.RemoveListener(OnEnvironmentMuteButtonClicked);
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

    private void OnMasterMuteButtonClicked()
    {
        ToggleMute(SoundVolumeChannel.Master);
    }

    private void OnBGMMuteButtonClicked()
    {
        ToggleMute(SoundVolumeChannel.BGM);
    }

    private void OnUIMuteButtonClicked()
    {
        ToggleMute(SoundVolumeChannel.UI);
    }

    private void OnEnvironmentMuteButtonClicked()
    {
        ToggleMute(SoundVolumeChannel.Environment);
    }

    private void ToggleMute(SoundVolumeChannel channel)
    {
        Slider slider = GetSlider(channel);

        if (slider == null)
        {
            return;
        }

        float currentPercent = Mathf.Round(AudioVolumeUtility.SliderValueToPercent(slider.value));
        float targetPercent = currentPercent > 0f
            ? 0f
            : SoundSettingsStore.GetLastNonZeroVolume(channel);

        slider.value = AudioVolumeUtility.PercentToSliderValue(targetPercent);
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

        bool isMuted = percent <= 0f;
        Sprite targetIcon = GetVolumeIcon(channel, isMuted);

        if (targetIcon == null)
        {
            return;
        }

        iconImage.sprite = targetIcon;
        iconImage.enabled = true;
        iconImage.preserveAspect = true;
    }

    private Sprite GetVolumeIcon(SoundVolumeChannel channel, bool isMuted)
    {
        Sprite channelIcon = channel switch
        {
            SoundVolumeChannel.Master => isMuted ? masterMuteIcon : masterPlayingIcon,
            SoundVolumeChannel.BGM => isMuted ? bgmMuteIcon : bgmPlayingIcon,
            SoundVolumeChannel.UI => isMuted ? uiMuteIcon : uiPlayingIcon,
            _ => null
        };

        if (channelIcon != null)
        {
            return channelIcon;
        }

        return isMuted ? soundMuteIcon : soundPlayingIcon;
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

    private Slider GetSlider(SoundVolumeChannel channel)
    {
        return channel switch
        {
            SoundVolumeChannel.Master => masterSlider,
            SoundVolumeChannel.BGM => bgmSlider,
            SoundVolumeChannel.UI => uiSlider,
            SoundVolumeChannel.Environment => environmentSlider,
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
