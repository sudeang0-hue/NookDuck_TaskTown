using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TaskTown.Gacha;
using TaskTown.KDH;
using Tool.Data;

public enum GachaType
{
    Tool,   // 도구 뽑기
    Animal  // 동물 뽑기
}

/// <summary>
/// [ECHO TD Refactored] 도구 및 동물 가챠 통합 브릿지 클래스.
/// 인스펙터 이벤트 충돌을 방지하고 최근 실행된 가챠 타입을 추적합니다.
/// </summary>
public class GachaSystemBridge : MonoBehaviour
{
    [Header("★ Data Asset References ★")]
    [SerializeField] private GachaPoolData toolGachaPoolData;
    [SerializeField] private GachaPoolData animalGachaPoolData;

    [Header("★ System Manager References ★")]
    [SerializeField] private ToolGachaManager toolGachaManager;
    [SerializeField] private ToolGachaManager animalGachaManager; // 동물 매니저 클래스 연결

    [Tooltip("ICoinWallet을 구현한 코인 관리자 컴포넌트")]
    [SerializeField] private MonoBehaviour coinWalletSource;
    private ICoinWallet CoinWallet => coinWalletSource as ICoinWallet;

    [Header("★ Main UI Window References ★")]
    [SerializeField] private GameObject teammateInitialWindow;
    [SerializeField] private Button btnOpenGachaWindow;

    [Header("★ Tool Gacha UI Buttons ★")]
    [SerializeField] private Button btnToolDraw1;
    [SerializeField] private Button btnToolDraw10;

    [Header("★ Animal Gacha UI Buttons ★")]
    [SerializeField] private Button btnAnimalDraw1;
    [SerializeField] private Button btnAnimalDraw10;

    [Header("★ Result Window Re-roll Buttons ★")]
    [SerializeField] private Button btnResultDraw1;
    [SerializeField] private Button btnResultDraw10;

    [Header("★ Director Reference ★")]
    [SerializeField] private GachaDirector gachaDirector;

    // 최근에 실행한 가챠 타입을 기억 (결과창 다시 뽑기 용도)
    private GachaType _lastExecutedType = GachaType.Tool;

    private float _lastClickTime = 0f;
    private const float CLICK_THRESHOLD = 0.3f;

    private void Awake()
    {
        if (gachaDirector == null)
        {
            gachaDirector = FindAnyObjectByType<GachaDirector>(FindObjectsInactive.Include);
        }
    }

    private void OnEnable() => InitButtonListeners();
    private void Start() => InitButtonListeners();
    private void OnDisable() => RemoveButtonListeners();

    private void InitButtonListeners()
    {
        RemoveButtonListeners();

        if (btnOpenGachaWindow != null)
            btnOpenGachaWindow.onClick.AddListener(ToggleGachaWindow);

        // 도구 뽑기 바인딩
        if (btnToolDraw1 != null)
            btnToolDraw1.onClick.AddListener(() => OnClickedDrawGacha(GachaType.Tool, 1));
        if (btnToolDraw10 != null)
            btnToolDraw10.onClick.AddListener(() => OnClickedDrawGacha(GachaType.Tool, 10));

        // 동물 뽑기 바인딩
        if (btnAnimalDraw1 != null)
            btnAnimalDraw1.onClick.AddListener(() => OnClickedDrawGacha(GachaType.Animal, 1));
        if (btnAnimalDraw10 != null)
            btnAnimalDraw10.onClick.AddListener(() => OnClickedDrawGacha(GachaType.Animal, 10));

        // ★ 결과창 다시 뽑기: 최근 실행했던 가챠 타입(_lastExecutedType)으로 재요청!
        if (btnResultDraw1 != null)
            btnResultDraw1.onClick.AddListener(() => OnClickedDrawGacha(_lastExecutedType, 1));
        if (btnResultDraw10 != null)
            btnResultDraw10.onClick.AddListener(() => OnClickedDrawGacha(_lastExecutedType, 10));
    }

    private void RemoveButtonListeners()
    {
        if (btnOpenGachaWindow != null) btnOpenGachaWindow.onClick.RemoveAllListeners();
        if (btnToolDraw1 != null) btnToolDraw1.onClick.RemoveAllListeners();
        if (btnToolDraw10 != null) btnToolDraw10.onClick.RemoveAllListeners();
        if (btnAnimalDraw1 != null) btnAnimalDraw1.onClick.RemoveAllListeners();
        if (btnAnimalDraw10 != null) btnAnimalDraw10.onClick.RemoveAllListeners();
        if (btnResultDraw1 != null) btnResultDraw1.onClick.RemoveAllListeners();
        if (btnResultDraw10 != null) btnResultDraw10.onClick.RemoveAllListeners();
    }

    public void ToggleGachaWindow()
    {
        if (teammateInitialWindow == null) return;

        bool isCurrentlyActive = teammateInitialWindow.activeSelf;
        if (isCurrentlyActive) CloseAllGachaWindows();
        else OpenAllGachaWindows();
    }

    public void OpenAllGachaWindows()
    {
        if (teammateInitialWindow != null) teammateInitialWindow.SetActive(true);
        if (gachaDirector != null) gachaDirector.OpenGachaWindowOnly();
    }

    public void CloseAllGachaWindows()
    {
        if (teammateInitialWindow != null) teammateInitialWindow.SetActive(false);
        if (gachaDirector != null) gachaDirector.CloseGachaUI();
    }

    private void OnClickedDrawGacha(GachaType gachaType, int drawCount)
    {
        if (Time.time - _lastClickTime < CLICK_THRESHOLD) return;
        _lastClickTime = Time.time;

        // 최근 가챠 타입 갱신
        _lastExecutedType = gachaType;

        ToolGachaManager targetManager = (gachaType == GachaType.Tool) ? toolGachaManager : animalGachaManager;

        if (targetManager == null)
        {
            Debug.LogError($"<color=red>[GachaBridge]</color> {gachaType} GachaManager가 할당되지 않았습니다!");
            return;
        }

        long requiredCost = targetManager.GetCost(drawCount);

        if (CoinWallet != null && CoinWallet.Balance < requiredCost)
        {
            Debug.LogWarning($"<color=red>[GachaBridge] {gachaType} 골드 부족!</color> (필요: {requiredCost})");
            return;
        }

        if (CoinWallet != null && !CoinWallet.TrySpend(requiredCost)) return;

        List<GachaResult> rollResults = targetManager.RollMulti(drawCount);
        List<GachaEntryData> entryDataList = new List<GachaEntryData>();

        if (rollResults != null)
        {
            foreach (GachaResult result in rollResults)
            {
                if (result.Entry == null) continue;
                entryDataList.Add(result.Entry);
                Debug.Log($"<color=lime>[GachaBridge] {gachaType} 획득:</color> {result.Entry.DisplayName}");
            }
        }

        if (teammateInitialWindow != null) teammateInitialWindow.SetActive(false);

        if (gachaDirector != null)
        {
            int actualCount = entryDataList.Count > 0 ? entryDataList.Count : drawCount;
            gachaDirector.StartGachaSequence(actualCount, entryDataList);
        }
    }
}