/*
 * 역할:
 * - BGM, UI, Environment 사운드 재생을 담당하는 전역 사운드 매니저입니다.
 *
 * 주요 기능:
 * - SoundLibrary에서 SoundClipData를 찾아 AudioSource로 재생합니다.
 * - CompanionAudioMixer의 BGM/UI/Environment MixerGroup으로 라우팅합니다.
 * - 저장된 SoundSettings 값을 재생 전 적용하여 씬 시작 직후에도 사용자 볼륨을 유지합니다.
 *
 * 배치:
 * - 실제 게임에서는 SoundSettingsApplier, EnvironmentSoundRuntime과 같은 오브젝트에 두고 DontDestroyOnLoad로 유지하는 구조를 권장합니다.
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private SoundSettingsApplier settingsApplier;
    [SerializeField] private bool applySavedSettingsOnAwake = true;
    [SerializeField] private bool dontDestroyOnLoad = true;

    [Header("Library")]
    [SerializeField] private SoundLibrary soundLibrary;

    [Header("Mixer Groups")]
    [SerializeField] private AudioMixerGroup bgmMixerGroup;
    [SerializeField] private AudioMixerGroup uiMixerGroup;
    [SerializeField] private AudioMixerGroup environmentMixerGroup;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private Transform oneShotSourceRoot;
    [SerializeField, Min(1)] private int oneShotPoolSize = 8;

    [Header("Start BGM")]
    [SerializeField] private bool playBgmOnStart;
    [SerializeField] private string startBgmSoundId;

    private readonly List<AudioSource> oneShotSources = new List<AudioSource>();
    private bool savedSettingsApplied;
    private bool savedSettingsAppliedAfterStartup;
    private bool startPhaseReached;
    private string currentBgmSoundId;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }

        if (settingsApplier == null)
        {
            TryGetComponent(out settingsApplier);
        }

        EnsureAudioSources();

        if (applySavedSettingsOnAwake)
        {
            ApplySavedSoundSettings();
        }
    }

    private void Start()
    {
        startPhaseReached = true;
        EnsureSettingsAppliedBeforePlayback();

        if (playBgmOnStart && !string.IsNullOrWhiteSpace(startBgmSoundId))
        {
            PlayBGM(startBgmSoundId);
        }
    }

    public bool ApplySavedSoundSettings()
    {
        if (settingsApplier == null)
        {
            Debug.LogWarning("SoundSettingsApplier is not assigned. Saved sound options cannot be applied before playback.");
            return false;
        }

        bool applied = settingsApplier.TryApplySavedSettings();
        savedSettingsApplied = applied;

        if (applied && startPhaseReached)
        {
            savedSettingsAppliedAfterStartup = true;
        }

        return applied;
    }

    public bool PlayBGM(string soundId, bool restartIfSame = false)
    {
        EnsureSettingsAppliedBeforePlayback();

        if (!TryGetSound(soundId, SoundCategory.BGM, out SoundClipData soundData))
        {
            return false;
        }

        AudioClip clip = soundData.GetClip();

        if (clip == null)
        {
            Debug.LogWarning($"BGM clip is missing. Sound ID: {soundId}");
            return false;
        }

        if (!restartIfSame && currentBgmSoundId == soundId && bgmSource.isPlaying)
        {
            return true;
        }

        bgmSource.outputAudioMixerGroup = bgmMixerGroup;
        bgmSource.clip = clip;
        bgmSource.volume = soundData.VolumeScale;
        bgmSource.pitch = soundData.GetPitch();
        bgmSource.loop = soundData.Loop;
        bgmSource.spatialBlend = 0f;
        bgmSource.Play();
        currentBgmSoundId = soundId;
        return true;
    }

    public void StopBGM()
    {
        if (bgmSource == null)
        {
            return;
        }

        bgmSource.Stop();
        currentBgmSoundId = null;
    }

    public bool PlayUI(string soundId)
    {
        return PlayOneShot(soundId, SoundCategory.UI, uiMixerGroup);
    }

    public bool PlayEnvironment(string soundId)
    {
        return PlayOneShot(soundId, SoundCategory.Environment, environmentMixerGroup);
    }

    public bool PlayRandomEnvironment()
    {
        EnsureSettingsAppliedBeforePlayback();

        if (soundLibrary == null || !soundLibrary.TryGetRandomSound(SoundCategory.Environment, out SoundClipData soundData))
        {
            Debug.LogWarning("No Environment sound candidate exists in SoundLibrary.");
            return false;
        }

        return PlayOneShot(soundData, environmentMixerGroup);
    }

    private bool PlayOneShot(string soundId, SoundCategory expectedCategory, AudioMixerGroup mixerGroup)
    {
        EnsureSettingsAppliedBeforePlayback();

        if (!TryGetSound(soundId, expectedCategory, out SoundClipData soundData))
        {
            return false;
        }

        return PlayOneShot(soundData, mixerGroup);
    }

    private bool PlayOneShot(SoundClipData soundData, AudioMixerGroup mixerGroup)
    {
        AudioClip clip = soundData.GetClip();

        if (clip == null)
        {
            Debug.LogWarning($"Sound clip is missing. Sound ID: {soundData.SoundId}");
            return false;
        }

        AudioSource source = GetAvailableOneShotSource();
        source.outputAudioMixerGroup = mixerGroup;
        source.clip = clip;
        source.volume = soundData.VolumeScale;
        source.pitch = soundData.GetPitch();
        source.loop = false;
        source.spatialBlend = 0f;
        source.Play();
        return true;
    }

    private bool TryGetSound(string soundId, SoundCategory expectedCategory, out SoundClipData soundData)
    {
        if (soundLibrary == null)
        {
            Debug.LogWarning("SoundLibrary is not assigned.");
            soundData = null;
            return false;
        }

        if (!soundLibrary.TryGetSound(soundId, out soundData))
        {
            Debug.LogWarning($"Sound ID was not found: {soundId}");
            return false;
        }

        if (soundData.Category != expectedCategory)
        {
            Debug.LogWarning($"Sound category mismatch. ID: {soundId}, Expected: {expectedCategory}, Actual: {soundData.Category}");
            return false;
        }

        return true;
    }

    private void EnsureSettingsAppliedBeforePlayback()
    {
        if (savedSettingsApplied && savedSettingsAppliedAfterStartup)
        {
            return;
        }

        ApplySavedSoundSettings();
    }

    private void EnsureAudioSources()
    {
        if (bgmSource == null)
        {
            GameObject bgmSourceObject = new GameObject("BGM AudioSource");
            bgmSourceObject.transform.SetParent(transform, false);
            bgmSource = bgmSourceObject.AddComponent<AudioSource>();
        }

        ConfigureBaseSource(bgmSource);
        bgmSource.outputAudioMixerGroup = bgmMixerGroup;
        bgmSource.loop = true;

        if (oneShotSourceRoot == null)
        {
            GameObject rootObject = new GameObject("One Shot AudioSources");
            rootObject.transform.SetParent(transform, false);
            oneShotSourceRoot = rootObject.transform;
        }

        oneShotSources.Clear();
        oneShotSourceRoot.GetComponentsInChildren(true, oneShotSources);

        while (oneShotSources.Count < oneShotPoolSize)
        {
            GameObject sourceObject = new GameObject($"One Shot AudioSource {oneShotSources.Count + 1:00}");
            sourceObject.transform.SetParent(oneShotSourceRoot, false);
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            ConfigureBaseSource(source);
            oneShotSources.Add(source);
        }
    }

    private void ConfigureBaseSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.loop = false;
    }

    private AudioSource GetAvailableOneShotSource()
    {
        for (int i = 0; i < oneShotSources.Count; i++)
        {
            if (!oneShotSources[i].isPlaying)
            {
                return oneShotSources[i];
            }
        }

        return oneShotSources[0];
    }
}
