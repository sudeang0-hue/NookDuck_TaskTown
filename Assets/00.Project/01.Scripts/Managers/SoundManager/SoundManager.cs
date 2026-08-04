/*
 * 역할:
 * - BGM, UIController_AnimalInvPage, Environment 사운드 재생을 담당하는 전역 사운드 매니저입니다.
 *
 * 주요 기능:
 * - SoundLibrary에서 SoundClipData를 찾아 AudioSource로 재생합니다.
 * - BGM 플레이리스트의 순차 반복, 현재 곡 반복, 이전/다음, 일시정지/재개 상태를 관리합니다.
 * - CompanionAudioMixer의 BGM/UIController_AnimalInvPage/Environment MixerGroup으로 라우팅합니다.
 * - 저장된 SoundSettings 값을 재생 전 적용하여 씬 시작 직후에도 사용자 볼륨을 유지합니다.
 *
 * 배치:
 * - 실제 게임에서는 SoundSettingsApplier, EnvironmentSoundRuntime과 같은 오브젝트에 두고 DontDestroyOnLoad로 유지하는 구조를 권장합니다.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public enum BGMPlaybackMode
{
    SequentialLoop = 0,
    RepeatCurrent = 1
}

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
    private SoundClipData activeBgmPlaylist;
    private int currentBgmTrackIndex = -1;
    private BGMPlaybackMode bgmPlaybackMode = BGMPlaybackMode.SequentialLoop;
    private bool isBgmPaused;
    private Coroutine playlistEndWatcher;
    private int playbackRevision;

    public event Action BGMStateChanged;

    public bool HasActiveBGMPlaylist =>
        activeBgmPlaylist != null &&
        currentBgmTrackIndex >= 0 &&
        currentBgmTrackIndex < activeBgmPlaylist.ClipCount;

    public bool IsBGMPlaying => bgmSource != null && bgmSource.isPlaying;
    public bool IsBGMPaused => isBgmPaused;
    public string CurrentBGMSoundId => currentBgmSoundId;
    public int CurrentBGMTrackIndex => HasActiveBGMPlaylist ? currentBgmTrackIndex : -1;
    public int CurrentBGMTrackCount => activeBgmPlaylist != null ? activeBgmPlaylist.ClipCount : 0;
    public string CurrentBGMTrackName => HasActiveBGMPlaylist && bgmSource != null && bgmSource.clip != null
        ? bgmSource.clip.name
        : string.Empty;
    public float CurrentBGMTime => HasActiveBGMPlaylist && bgmSource != null && bgmSource.clip != null
        ? Mathf.Clamp(bgmSource.time, 0f, bgmSource.clip.length)
        : 0f;
    public float CurrentBGMDuration => HasActiveBGMPlaylist && bgmSource != null && bgmSource.clip != null
        ? bgmSource.clip.length
        : 0f;
    public BGMPlaybackMode PlaybackMode => bgmPlaybackMode;

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

    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        StopPlaylistEndWatcher();
        BGMStateChanged = null;
        Instance = null;
    }

    public bool ApplySavedSoundSettings()
    {
        bgmPlaybackMode = SoundSettingsStore.GetBGMPlaybackMode();

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

        if (!restartIfSame && activeBgmPlaylist == null && currentBgmSoundId == soundId && bgmSource.isPlaying)
        {
            return true;
        }

        ExitPlaylistMode();
        bgmSource.outputAudioMixerGroup = bgmMixerGroup;
        bgmSource.clip = clip;
        bgmSource.volume = soundData.VolumeScale;
        bgmSource.pitch = soundData.GetPitch();
        bgmSource.loop = soundData.Loop;
        bgmSource.spatialBlend = 0f;
        bgmSource.Play();
        currentBgmSoundId = soundId;
        NotifyBGMStateChanged();
        return true;
    }

    public bool PlayBGMPlaylist(string soundId, int startIndex = 0, bool restartIfSame = false)
    {
        EnsureSettingsAppliedBeforePlayback();

        if (!TryGetSound(soundId, SoundCategory.BGM, out SoundClipData soundData))
        {
            return false;
        }

        if (soundData.ClipCount == 0)
        {
            Debug.LogWarning($"BGM playlist is empty. Sound ID: {soundId}");
            return false;
        }

        if (startIndex < 0 || startIndex >= soundData.ClipCount)
        {
            Debug.LogWarning($"BGM playlist start index is out of range. Sound ID: {soundId}, Index: {startIndex}");
            return false;
        }

        if (!soundData.TryGetClip(startIndex, out _))
        {
            Debug.LogWarning($"BGM playlist clip is missing. Sound ID: {soundId}, Index: {startIndex}");
            return false;
        }

        if (!restartIfSame &&
            activeBgmPlaylist == soundData &&
            currentBgmSoundId == soundId &&
            bgmSource != null &&
            (bgmSource.isPlaying || isBgmPaused))
        {
            return true;
        }

        ExitPlaylistMode();
        activeBgmPlaylist = soundData;
        currentBgmSoundId = soundId;
        return PlayPlaylistTrack(startIndex);
    }

    public bool PlayNextBGM()
    {
        if (!HasActiveBGMPlaylist)
        {
            return false;
        }

        int nextIndex = (currentBgmTrackIndex + 1) % activeBgmPlaylist.ClipCount;
        return PlayPlaylistTrack(nextIndex);
    }

    public bool PlayPreviousBGM()
    {
        if (!HasActiveBGMPlaylist)
        {
            return false;
        }

        int previousIndex = (currentBgmTrackIndex - 1 + activeBgmPlaylist.ClipCount) % activeBgmPlaylist.ClipCount;
        return PlayPlaylistTrack(previousIndex);
    }

    public bool PauseBGM()
    {
        if (bgmSource == null || !bgmSource.isPlaying)
        {
            return false;
        }

        bgmSource.Pause();
        isBgmPaused = true;
        NotifyBGMStateChanged();
        return true;
    }

    public bool ResumeBGM()
    {
        if (bgmSource == null || bgmSource.clip == null || !isBgmPaused)
        {
            return false;
        }

        bgmSource.UnPause();
        isBgmPaused = false;
        NotifyBGMStateChanged();
        return true;
    }

    public bool ToggleBGMPause()
    {
        return isBgmPaused ? ResumeBGM() : PauseBGM();
    }

    public bool SetBGMPlaybackMode(BGMPlaybackMode mode)
    {
        if (!Enum.IsDefined(typeof(BGMPlaybackMode), mode))
        {
            Debug.LogWarning($"Unsupported BGM playback mode: {mode}");
            return false;
        }

        SoundSettingsStore.SetBGMPlaybackMode(mode);

        if (bgmPlaybackMode == mode)
        {
            return true;
        }

        bgmPlaybackMode = mode;

        if (HasActiveBGMPlaylist && bgmSource != null)
        {
            bgmSource.loop = bgmPlaybackMode == BGMPlaybackMode.RepeatCurrent;

            if (bgmPlaybackMode == BGMPlaybackMode.SequentialLoop)
            {
                RestartPlaylistEndWatcher();
            }
            else
            {
                StopPlaylistEndWatcher();
            }
        }

        NotifyBGMStateChanged();
        return true;
    }

    public void StopBGM()
    {
        bool hadBgmState = (bgmSource != null && bgmSource.clip != null) ||
                           !string.IsNullOrEmpty(currentBgmSoundId) ||
                           activeBgmPlaylist != null;

        if (bgmSource != null)
        {
            bgmSource.Stop();
            bgmSource.clip = null;
            bgmSource.loop = false;
        }

        ExitPlaylistMode();
        currentBgmSoundId = null;

        if (hadBgmState)
        {
            NotifyBGMStateChanged();
        }
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

    private bool PlayPlaylistTrack(int index)
    {
        if (activeBgmPlaylist == null || bgmSource == null)
        {
            return false;
        }

        int trackCount = activeBgmPlaylist.ClipCount;

        if (trackCount == 0)
        {
            return false;
        }

        int normalizedIndex = (index % trackCount + trackCount) % trackCount;

        if (!activeBgmPlaylist.TryGetClip(normalizedIndex, out AudioClip clip))
        {
            Debug.LogWarning($"BGM playlist clip is missing. Sound ID: {activeBgmPlaylist.SoundId}, Index: {normalizedIndex}");
            return false;
        }

        StopPlaylistEndWatcher();
        currentBgmTrackIndex = normalizedIndex;
        currentBgmSoundId = activeBgmPlaylist.SoundId;
        isBgmPaused = false;

        bgmSource.outputAudioMixerGroup = bgmMixerGroup;
        bgmSource.clip = clip;
        bgmSource.volume = activeBgmPlaylist.VolumeScale;
        bgmSource.pitch = activeBgmPlaylist.GetPitch();
        bgmSource.loop = bgmPlaybackMode == BGMPlaybackMode.RepeatCurrent;
        bgmSource.spatialBlend = 0f;
        bgmSource.Play();

        if (bgmPlaybackMode == BGMPlaybackMode.SequentialLoop)
        {
            RestartPlaylistEndWatcher();
        }

        NotifyBGMStateChanged();
        return true;
    }

    private IEnumerator WatchForPlaylistTrackEnd(int expectedRevision, AudioClip expectedClip)
    {
        yield return null;

        while (expectedRevision == playbackRevision &&
               HasActiveBGMPlaylist &&
               bgmPlaybackMode == BGMPlaybackMode.SequentialLoop)
        {
            if (bgmSource == null || bgmSource.clip != expectedClip)
            {
                yield break;
            }

            if (!isBgmPaused && !bgmSource.isPlaying)
            {
                playlistEndWatcher = null;
                PlayPlaylistTrack(currentBgmTrackIndex + 1);
                yield break;
            }

            yield return null;
        }
    }

    private void RestartPlaylistEndWatcher()
    {
        StopPlaylistEndWatcher();

        if (!HasActiveBGMPlaylist ||
            bgmSource == null ||
            bgmSource.clip == null ||
            bgmPlaybackMode != BGMPlaybackMode.SequentialLoop)
        {
            return;
        }

        int expectedRevision = playbackRevision;
        playlistEndWatcher = StartCoroutine(WatchForPlaylistTrackEnd(expectedRevision, bgmSource.clip));
    }

    private void StopPlaylistEndWatcher()
    {
        playbackRevision++;

        if (playlistEndWatcher == null)
        {
            return;
        }

        StopCoroutine(playlistEndWatcher);
        playlistEndWatcher = null;
    }

    private void ExitPlaylistMode()
    {
        StopPlaylistEndWatcher();
        activeBgmPlaylist = null;
        currentBgmTrackIndex = -1;
        isBgmPaused = false;
    }

    private void NotifyBGMStateChanged()
    {
        BGMStateChanged?.Invoke();
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
