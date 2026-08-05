using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SoundManager의 BGM 재생 상태를 표시하고 재생 제어 입력을 전달하는 공용 UI 패널입니다.
/// 테스트 UI와 실제 옵션 UI가 동일한 표시 및 조작 로직을 사용하도록 분리했습니다.
/// </summary>
[DisallowMultipleComponent]
public class BGMPlaylistPanel : MonoBehaviour
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
    [SerializeField] private TMP_Text currentTimeText;
    [SerializeField] private TMP_Text durationText;
    [SerializeField] private TMP_Text playPauseButtonText;
    [SerializeField] private TMP_Text playbackModeButtonText;

    [Header("Optional Progress")]
    [SerializeField] private Image progressFillImage;

    private SoundManager soundManager;
    private Coroutine progressRefreshCoroutine;
    private WaitForSecondsRealtime progressRefreshWait;
    private int lastDisplayedSecond = -1;
    private int lastDisplayedDuration = -1;

    protected virtual void OnEnable()
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

    protected virtual void OnDisable()
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
                RefreshTimeDisplay();
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
        soundManager?.PlayPreviousBGM();
    }

    private void HandlePlayPauseClicked()
    {
        soundManager?.ToggleBGMPause();
    }

    private void HandleNextClicked()
    {
        soundManager?.PlayNextBGM();
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

            SetTimeDisplay(0, 0);
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

        RefreshTimeDisplay();
    }

    private void RefreshTimeDisplay()
    {
        if (soundManager == null || !soundManager.HasActiveBGMPlaylist)
        {
            SetTimeDisplay(0, 0);
            SetProgress(0f);
            return;
        }

        float elapsedTime = Mathf.Max(0f, soundManager.CurrentBGMTime);
        float duration = Mathf.Max(0f, soundManager.CurrentBGMDuration);
        int elapsedSecond = Mathf.FloorToInt(elapsedTime);
        int durationSecond = Mathf.RoundToInt(duration);

        SetProgress(duration > 0f ? elapsedTime / duration : 0f);

        if (elapsedSecond == lastDisplayedSecond && durationSecond == lastDisplayedDuration)
        {
            return;
        }

        lastDisplayedSecond = elapsedSecond;
        lastDisplayedDuration = durationSecond;
        SetTimeDisplay(elapsedSecond, durationSecond);
    }

    private void SetTimeDisplay(int elapsedSecond, int durationSecond)
    {
        if (timeText != null)
        {
            timeText.SetText(
                "{0:00}:{1:00} / {2:00}:{3:00}",
                elapsedSecond / 60,
                elapsedSecond % 60,
                durationSecond / 60,
                durationSecond % 60);
        }

        SetFormattedTime(currentTimeText, elapsedSecond);
        SetFormattedTime(durationText, durationSecond);
    }

    private void SetProgress(float normalizedProgress)
    {
        if (progressFillImage == null)
        {
            return;
        }

        float progress = Mathf.Clamp01(normalizedProgress);
        RectTransform progressRect = progressFillImage.rectTransform;

        progressRect.anchorMin = Vector2.zero;
        progressRect.anchorMax = new Vector2(progress, 1f);
        progressRect.offsetMin = Vector2.zero;
        progressRect.offsetMax = Vector2.zero;

        // Sprite가 나중에 연결되더라도 RectTransform 비율만으로 진행도를 표시합니다.
        progressFillImage.fillAmount = 1f;
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

    private static void SetFormattedTime(TMP_Text target, int totalSeconds)
    {
        if (target != null)
        {
            target.SetText("{0:00}:{1:00}", totalSeconds / 60, totalSeconds % 60);
        }
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }
}
