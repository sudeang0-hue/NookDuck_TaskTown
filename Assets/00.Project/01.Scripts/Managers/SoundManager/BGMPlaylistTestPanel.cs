/*
 * 역할:
 * - BGM 플레이리스트 기능을 통합 테스트 씬에서 검증하기 위한 임시 UI 컨트롤러입니다.
 *
 * 주요 기능:
 * - 현재 곡 이름, 곡 번호, 재생 시간과 전체 시간을 표시합니다.
 * - 이전/다음, 재생/일시정지, 순차 반복/현재 곡 반복 조작을 SoundManager에 전달합니다.
 *
 * 주의:
 * - 실제 옵션 UIController가 아닌 테스트 전용 컴포넌트입니다.
 */
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BGMPlaylistTestPanel : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button playPauseButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button playbackModeButton;

    [Header("Labels")]
    [SerializeField] private TMP_Text trackNameText;
    [SerializeField] private TMP_Text trackIndexText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text playPauseButtonText;
    [SerializeField] private TMP_Text playbackModeButtonText;

    private SoundManager soundManager;
    private Coroutine progressRefreshCoroutine;
    private WaitForSecondsRealtime progressRefreshWait;
    private int lastDisplayedSecond = -1;
    private int lastDisplayedDuration = -1;

    private void OnEnable()
    {
        AddButtonListeners();
        TryBindSoundManager();
        RefreshAll();

        if (progressRefreshCoroutine == null)
        {
            progressRefreshWait = new WaitForSecondsRealtime(0.25f);
            progressRefreshCoroutine = StartCoroutine(RefreshProgressRoutine());
        }
    }

    private void OnDisable()
    {
        RemoveButtonListeners();
        UnbindSoundManager();

        if (progressRefreshCoroutine != null)
        {
            StopCoroutine(progressRefreshCoroutine);
            progressRefreshCoroutine = null;
        }
    }

    private IEnumerator RefreshProgressRoutine()
    {
        while (true)
        {
            if (soundManager == null)
            {
                if (TryBindSoundManager())
                {
                    RefreshAll();
                }
            }
            else
            {
                RefreshTimeText();
            }

            yield return progressRefreshWait;
        }
    }

    private bool TryBindSoundManager()
    {
        SoundManager instance = SoundManager.Instance;

        if (soundManager == instance && soundManager != null)
        {
            return true;
        }

        UnbindSoundManager();

        if (instance == null)
        {
            return false;
        }

        soundManager = instance;
        soundManager.BGMStateChanged += HandleBGMStateChanged;
        return true;
    }

    private void UnbindSoundManager()
    {
        if (soundManager != null)
        {
            soundManager.BGMStateChanged -= HandleBGMStateChanged;
        }

        soundManager = null;
    }

    private void AddButtonListeners()
    {
        previousButton?.onClick.AddListener(HandlePreviousClicked);
        playPauseButton?.onClick.AddListener(HandlePlayPauseClicked);
        nextButton?.onClick.AddListener(HandleNextClicked);
        playbackModeButton?.onClick.AddListener(HandlePlaybackModeClicked);
    }

    private void RemoveButtonListeners()
    {
        previousButton?.onClick.RemoveListener(HandlePreviousClicked);
        playPauseButton?.onClick.RemoveListener(HandlePlayPauseClicked);
        nextButton?.onClick.RemoveListener(HandleNextClicked);
        playbackModeButton?.onClick.RemoveListener(HandlePlaybackModeClicked);
    }

    private void HandlePreviousClicked()
    {
        if (soundManager != null)
        {
            soundManager.PlayPreviousBGM();
        }
    }

    private void HandlePlayPauseClicked()
    {
        if (soundManager != null)
        {
            soundManager.ToggleBGMPause();
        }
    }

    private void HandleNextClicked()
    {
        if (soundManager != null)
        {
            soundManager.PlayNextBGM();
        }
    }

    private void HandlePlaybackModeClicked()
    {
        if (soundManager == null)
        {
            return;
        }

        BGMPlaybackMode nextMode = soundManager.PlaybackMode == BGMPlaybackMode.SequentialLoop
            ? BGMPlaybackMode.RepeatCurrent
            : BGMPlaybackMode.SequentialLoop;

        soundManager.SetBGMPlaybackMode(nextMode);
    }

    private void HandleBGMStateChanged()
    {
        RefreshAll();
    }

    private void RefreshAll()
    {
        lastDisplayedSecond = -1;
        lastDisplayedDuration = -1;

        if (soundManager == null)
        {
            SetText(trackNameText, "BGM 준비 중");
            SetText(trackIndexText, "- / -");
            SetText(playPauseButtonText, "재생");
            SetText(playbackModeButtonText, "순차 반복");
            SetPlaylistButtonsInteractable(false);

            if (playbackModeButton != null)
            {
                playbackModeButton.interactable = false;
            }

            SetText(timeText, "00:00 / 00:00");
            return;
        }

        bool hasPlaylist = soundManager.HasActiveBGMPlaylist;
        SetText(trackNameText, hasPlaylist ? soundManager.CurrentBGMTrackName : "재생 중인 BGM 없음");

        if (hasPlaylist && trackIndexText != null)
        {
            trackIndexText.SetText(
                "{0:0} / {1:0}",
                soundManager.CurrentBGMTrackIndex + 1,
                soundManager.CurrentBGMTrackCount);
        }
        else
        {
            SetText(trackIndexText, "- / -");
        }
        SetText(playPauseButtonText, soundManager.IsBGMPaused || !soundManager.IsBGMPlaying ? "재생" : "일시정지");
        SetText(playbackModeButtonText, GetPlaybackModeLabel(soundManager.PlaybackMode));

        SetPlaylistButtonsInteractable(hasPlaylist);

        if (playbackModeButton != null)
        {
            playbackModeButton.interactable = true;
        }

        RefreshTimeText();
    }

    private void RefreshTimeText()
    {
        if (soundManager == null || !soundManager.HasActiveBGMPlaylist)
        {
            SetText(timeText, "00:00 / 00:00");
            return;
        }

        int elapsedSecond = Mathf.FloorToInt(soundManager.CurrentBGMTime);
        int durationSecond = Mathf.RoundToInt(soundManager.CurrentBGMDuration);

        if (elapsedSecond == lastDisplayedSecond && durationSecond == lastDisplayedDuration)
        {
            return;
        }

        lastDisplayedSecond = elapsedSecond;
        lastDisplayedDuration = durationSecond;

        if (timeText != null)
        {
            timeText.SetText(
                "{0:00}:{1:00} / {2:00}:{3:00}",
                elapsedSecond / 60,
                elapsedSecond % 60,
                durationSecond / 60,
                durationSecond % 60);
        }
    }

    private void SetPlaylistButtonsInteractable(bool interactable)
    {
        if (previousButton != null)
        {
            previousButton.interactable = interactable;
        }

        if (playPauseButton != null)
        {
            playPauseButton.interactable = interactable;
        }

        if (nextButton != null)
        {
            nextButton.interactable = interactable;
        }
    }

    private static string GetPlaybackModeLabel(BGMPlaybackMode mode)
    {
        return mode == BGMPlaybackMode.RepeatCurrent ? "현재 곡 반복" : "순차 반복";
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }
}
