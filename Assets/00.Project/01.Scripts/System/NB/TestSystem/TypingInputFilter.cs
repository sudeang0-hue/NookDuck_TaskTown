//NB

using System;
using System.Collections.Generic;
using UnityEngine;

// 글로벌 키보드 입력을 수신하여 매크로/AI 타수를 검증(Filtering)하는 전담 클래스입니다.
public class TypingInputFilter : MonoBehaviour
{
    [Header("Anti-Macro / AI Protection")]
    [Tooltip("1초당 허용되는 최대 키 입력 수 (인간의 한계 타수: 약 15~18 CPS)")]
    [SerializeField] private int maxAllowedCPS = 18;
    [SerializeField] private float checkWindowDuration = 1.0f;

    private readonly Queue<float> keyInputTimestamps = new Queue<float>();

    // 정상적인 키 입력이 검증되었을 때 발생되는 이벤트 (외부 구독용)

    public static event Action OnTypingValidated;

    // 외부 UI 및 디버깅용 CPS 프로퍼티
    public int CurrentCPS { get; private set; }

    private void OnEnable()
    {
        GlobalKeyboardHook.OnGlobalKeyPressed += HandleGlobalKeyPress;
    }

    private void OnDisable()
    {
        GlobalKeyboardHook.OnGlobalKeyPressed -= HandleGlobalKeyPress;
    }

    private void HandleGlobalKeyPress()
    {
        float currentTime = Time.unscaledTime;
        keyInputTimestamps.Enqueue(currentTime);

        // $O(1)$ 시간 복잡도로 시간 창(Window)을 벗어난 타자 기록 제거
        while (keyInputTimestamps.Count > 0 && keyInputTimestamps.Peek() < currentTime - checkWindowDuration)
        {
            keyInputTimestamps.Dequeue();
        }

        CurrentCPS = keyInputTimestamps.Count;

        // CPS 한도 초과 시 매크로로 간주하고 차단 ($CPS > maxAllowedCPS$)
        if (CurrentCPS > maxAllowedCPS)
        {
            Debug.LogWarning($"[Anti-Macro Filter] 이상 타수 감지! (CPS: {CurrentCPS}/{maxAllowedCPS}) - 입력 무시");
            return;
        }

        // 검증 통과 시 이벤트 방송 (EarnProcessor 등 수신자가 알아서 처리)
        OnTypingValidated?.Invoke();
    }
}