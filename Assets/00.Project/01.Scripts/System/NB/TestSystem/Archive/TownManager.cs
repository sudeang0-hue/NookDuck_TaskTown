using System;
using UnityEngine;

public class TownManager : MonoBehaviour
{
    // 어디서나 접근 가능하도록 싱글톤 선언 (단, 무분별한 사용은 지양합니다!)
    public static TownManager Instance { get; private set; }

    [Header("마을 설정")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int maxLevel = 10;

    public int CurrentLevel => currentLevel;
    // 레벨 변경을 알리는 C# 이벤트 (저결합 설계의 꽃입니다)
    public event Action<int> OnTownLevelChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 게임 시작 시 초기 레벨 상태 전파
        OnTownLevelChanged?.Invoke(currentLevel);
    }

    private void Update()
    {
        // 테스트용 키 'U'를 누르면 레벨 상승
        if (Input.GetKeyDown(KeyCode.U))
        {
            LevelUp();
        }
    }

    public void LevelUp()
    {
        if (currentLevel < maxLevel)
        {
            currentLevel++;
            Debug.Log($"<color=cyan>[TownManager] 레벨 업! 현재 마을 레벨: {currentLevel}</color>");

            // 구독 중인 모든 객체에 레벨 변경 알림
            OnTownLevelChanged?.Invoke(currentLevel);
        }
        else
        {
            Debug.Log("<color=yellow>[TownManager] 이미 최고 레벨(10레벨)에 도달했습니다!</color>");
        }
    }
}