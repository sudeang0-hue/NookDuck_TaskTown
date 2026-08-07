using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 난이도 버튼 호버 시 선택 버튼은 확대, 나머지 버튼은 축소합니다.
/// 씬 로드 인트로 연출(지연 후 동시 활성화 + 오픈 스케일)도 담당합니다.
/// 배열 순서: 0 Easy / 1 Normal / 2 Hard / 3 Very Hard
/// </summary>
public class DifficultyHover : MonoBehaviour
{
    private const int DifficultyCount = 4;

    [Header("0: Easy / 1: Normal / 2: Hard / 3: Very Hard")]
    [SerializeField] private Button[] hoverPanel;

    [Header("Scale")]
    [Tooltip("호버 중인 버튼의 목표 스케일")]
    [SerializeField] private float hoverScale = 1.2f;
    [Tooltip("호버되지 않은 나머지 버튼의 목표 스케일")]
    [SerializeField] private float shrinkScale = 0.9f;

    [Header("Tween")]
    [Tooltip("마우스 진입 후 스케일 변경을 시작하기까지 대기 시간(초)")]
    [SerializeField, Min(0f)] private float hoverStartDelay = 0f;
    [Tooltip("확대/축소에 공통으로 사용하는 스케일 변경 시간(초)")]
    [SerializeField, Min(0f)] private float scaleDuration = 0.15f;
    [SerializeField] private Ease scaleEase = Ease.OutQuad;

    [Header("Intro")]
    [Tooltip("씬 로드 후 인트로 활성화를 시작하기까지 대기 시간(초)")]
    [SerializeField, Min(0f)] private float introStartDelay = 0.3f;
    [Tooltip("인트로 오픈 스케일의 피크 배율")]
    [SerializeField] private float introPeakScale = 1.1f;
    [Tooltip("인트로 오픈 스케일 한 구간(커졌다/원래대로) 시간(초)")]
    [SerializeField, Min(0f)] private float introScaleDuration = 0.12f;
    [SerializeField] private Ease introEaseOut = Ease.OutQuad;
    [SerializeField] private Ease introEaseBack = Ease.InOutQuad;

    private RectTransform[] targets;
    private Vector3[] baseScales;
    private Tween[] scaleTweens;
    private int hoveredIndex = -1;
    private Coroutine introRoutine;
    private Coroutine hoverDelayRoutine;
    private bool isIntroPlaying;
    private int introRemainingCount;

    private void Awake()
    {
        CacheTargets();
        BindHoverRelays();
    }

    private void Start()
    {
        introRoutine = StartCoroutine(PlayIntroRoutine());
    }

    private void OnDisable()
    {
        StopIntroRoutine();
        StopHoverDelayRoutine();
        hoveredIndex = -1;
        isIntroPlaying = false;
        introRemainingCount = 0;
        ResetAllScalesImmediate();
    }

    private void OnDestroy()
    {
        StopIntroRoutine();
        StopHoverDelayRoutine();
        KillAllTweens();
    }

    private IEnumerator PlayIntroRoutine()
    {
        if (introStartDelay > 0f)
            yield return new WaitForSeconds(introStartDelay);

        PlayIntroOpenScales();
        introRoutine = null;
    }

    /// <summary>
    /// 연결된 hoverPanel 버튼을 동시에 활성화하고 오픈 스케일 연출을 재생합니다.
    /// </summary>
    private void PlayIntroOpenScales()
    {
        if (hoverPanel == null || targets == null || targets.Length == 0)
            return;

        isIntroPlaying = true;
        introRemainingCount = 0;

        for (int i = 0; i < targets.Length; i++)
        {
            if (hoverPanel[i] == null || targets[i] == null)
                continue;

            hoverPanel[i].gameObject.SetActive(true);
            baseScales[i] = Vector3.one;
            introRemainingCount++;
            PlayIntroScale(i);
        }

        if (introRemainingCount == 0)
            isIntroPlaying = false;
    }

    private void PlayIntroScale(int index)
    {
        if (!IsValidIndex(index))
            return;

        if (scaleTweens[index] != null && scaleTweens[index].IsActive())
            scaleTweens[index].Kill();

        targets[index].localScale = Vector3.one;

        if (introScaleDuration <= 0f)
        {
            targets[index].localScale = Vector3.one;
            CompleteIntroStep();
            return;
        }

        // UIPanelWindow / UITweenManager.PlayOpenScale 과 동일한 1 -> peak -> 1 연출
        scaleTweens[index] = DOTween.Sequence()
            .Append(targets[index].DOScale(introPeakScale, introScaleDuration).SetEase(introEaseOut))
            .Append(targets[index].DOScale(1f, introScaleDuration).SetEase(introEaseBack))
            .SetUpdate(true)
            .SetLink(targets[index].gameObject)
            .OnComplete(CompleteIntroStep);
    }

    private void CompleteIntroStep()
    {
        if (!isIntroPlaying)
            return;

        introRemainingCount--;
        if (introRemainingCount <= 0)
        {
            introRemainingCount = 0;
            isIntroPlaying = false;
        }
    }

    private void StopIntroRoutine()
    {
        if (introRoutine == null)
            return;

        StopCoroutine(introRoutine);
        introRoutine = null;
    }

    private void StopHoverDelayRoutine()
    {
        if (hoverDelayRoutine == null)
            return;

        StopCoroutine(hoverDelayRoutine);
        hoverDelayRoutine = null;
    }

    private void CacheTargets()
    {
        // 비워둔 상태 허용: null/빈 배열이면 길이 0으로 캐시
        int count = hoverPanel != null ? Mathf.Min(DifficultyCount, hoverPanel.Length) : 0;
        targets = new RectTransform[count];
        baseScales = new Vector3[count];
        scaleTweens = new Tween[count];

        for (int i = 0; i < count; i++)
        {
            if (hoverPanel[i] == null)
                continue;

            targets[i] = hoverPanel[i].transform as RectTransform;
            if (targets[i] != null)
                baseScales[i] = targets[i].localScale;
        }
    }

    private void BindHoverRelays()
    {
        if (hoverPanel == null || targets == null || targets.Length == 0)
            return;

        for (int i = 0; i < targets.Length; i++)
        {
            if (hoverPanel[i] == null)
                continue;

            HoverRelay relay = hoverPanel[i].GetComponent<HoverRelay>();
            if (relay == null)
                relay = hoverPanel[i].gameObject.AddComponent<HoverRelay>();

            relay.Initialize(this, i);
        }
    }

    private void HandlePointerEnter(int index)
    {
        if (isIntroPlaying || !IsValidIndex(index))
            return;

        StopHoverDelayRoutine();
        hoveredIndex = index;

        if (hoverStartDelay <= 0f)
        {
            ApplyHoverScales(index);
            return;
        }

        hoverDelayRoutine = StartCoroutine(PlayHoverAfterDelay(index));
    }

    private void HandlePointerExit(int index)
    {
        if (isIntroPlaying || hoveredIndex != index)
            return;

        StopHoverDelayRoutine();
        hoveredIndex = -1;
        ApplyBaseScales();
    }

    private IEnumerator PlayHoverAfterDelay(int index)
    {
        yield return new WaitForSeconds(hoverStartDelay);

        hoverDelayRoutine = null;

        // 대기 중 마우스가 벗어나거나 다른 버튼으로 바뀌면 적용하지 않음
        if (isIntroPlaying || hoveredIndex != index || !IsValidIndex(index))
            yield break;

        ApplyHoverScales(index);
    }

    private void ApplyHoverScales(int focusedIndex)
    {
        if (targets == null || targets.Length == 0)
            return;

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null)
                continue;

            float targetScale = i == focusedIndex ? hoverScale : shrinkScale;
            PlayScale(i, baseScales[i] * targetScale);
        }
    }

    private void ApplyBaseScales()
    {
        if (targets == null || targets.Length == 0)
            return;

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null)
                continue;

            PlayScale(i, baseScales[i]);
        }
    }

    private void PlayScale(int index, Vector3 targetScale)
    {
        if (targets == null || !IsValidIndex(index))
            return;

        if (scaleTweens[index] != null && scaleTweens[index].IsActive())
            scaleTweens[index].Kill();

        if (scaleDuration <= 0f)
        {
            targets[index].localScale = targetScale;
            scaleTweens[index] = null;
            return;
        }

        scaleTweens[index] = targets[index]
            .DOScale(targetScale, scaleDuration)
            .SetEase(scaleEase)
            .SetUpdate(true)
            .SetLink(targets[index].gameObject);
    }

    private void ResetAllScalesImmediate()
    {
        KillAllTweens();

        if (targets == null || targets.Length == 0)
            return;

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null)
                continue;

            targets[i].localScale = baseScales[i];
        }
    }

    private void KillAllTweens()
    {
        if (scaleTweens == null || scaleTweens.Length == 0)
            return;

        for (int i = 0; i < scaleTweens.Length; i++)
        {
            if (scaleTweens[i] != null && scaleTweens[i].IsActive())
                scaleTweens[i].Kill();

            scaleTweens[i] = null;
        }
    }

    private bool IsValidIndex(int index)
    {
        return targets != null && index >= 0 && index < targets.Length && targets[index] != null;
    }

    /// <summary>
    /// 각 버튼의 포인터 이벤트를 DifficultyHover로 전달합니다.
    /// </summary>
    private sealed class HoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private DifficultyHover owner;
        private int index;

        public void Initialize(DifficultyHover hoverOwner, int buttonIndex)
        {
            owner = hoverOwner;
            index = buttonIndex;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            owner?.HandlePointerEnter(index);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            owner?.HandlePointerExit(index);
        }
    }
}
