//NB

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TaskTown.Gacha;

// 가챠 콘텐츠 종류 구분자
public enum GachaType
{
    Tool,   // 도구 뽑기 (상자 연출)
    Animal  // 동물 뽑기 (차원문 연출)
}

// 도구 및 동물 가챠 통합 브릿지 클래스 (중앙 라우터 역할)
public class GachaSystemBridge : MonoBehaviour
{
    [Header("Data Asset References")]
    [SerializeField] private GachaPoolData toolGachaPoolData;
    [SerializeField] private GachaPoolData animalGachaPoolData;

    [Header("System Manager References")]
    [SerializeField] private ToolGachaManager toolGachaManager;
    [SerializeField] private AnimalGachaManager animalGachaManager;

    [Tooltip("ICoinWallet을 구현한 코인 관리자 컴포넌트")]
    [SerializeField] private MonoBehaviour coinWalletSource;
    private ICoinWallet CoinWallet => coinWalletSource as ICoinWallet;

    [Header("Main UI Window References")]
    [SerializeField] private GameObject teammateInitialWindow;
    // ------------ 26.08.06 KAY 수정 (주석처리) ------------------
    //[SerializeField] private Button btnOpenGachaWindow;

    [Header(" Auto Close Target Panels ")]
    [Tooltip("가챠 창이 열릴 때 자동으로 비활성화할 다른 메인 UI 패널 목록")]
    [SerializeField] private List<GameObject> otherMainPanels = new List<GameObject>();

    [Header("UI State Events")]
    public UnityEvent onGachaWindowOpened;

    [Header("Tool Gacha UI Buttons (Initial Window)")]
    [SerializeField] private Button btnToolDraw1;
    [SerializeField] private Button btnToolDraw10;

    [Header("Animal Gacha UI Buttons (Initial Window)")]
    [SerializeField] private Button btnAnimalDraw1;
    [SerializeField] private Button btnAnimalDraw10;

    [Header("Tool Gacha Result Window Buttons ")]
    [Tooltip("도구 가챠 결과창에 있는 1회/10회 뽑기 버튼을 등록하세요.")]
    [SerializeField] private Button btnToolResultDraw1;
    [SerializeField] private Button btnToolResultDraw10;

    [Header("Animal Gacha Result Window Buttons ")]
    [Tooltip("동물 가챠 결과창에 있는 1회/10회 뽑기 버튼을 등록하세요.")]
    [SerializeField] private Button btnAnimalResultDraw1;
    [SerializeField] private Button btnAnimalResultDraw10;

    [Header(" Shared Result Window Buttons (Fallback) ")]
    [Tooltip("도구/동물이 결과창을 단일 공용으로 함께 사용할 경우 여기에 등록하세요.")]
    [SerializeField] private Button btnResultDraw1;
    [SerializeField] private Button btnResultDraw10;

    [Header("Dedicated Director References")]
    [SerializeField] private GachaDirector toolGachaDirector;           // 도구 전용 디렉터 (상자 연출)
    [SerializeField] private GachaPortalController animalGachaDirector; // 동물 전용 디렉터 (차원문 연출)

    private GachaType _lastExecutedType = GachaType.Tool;
    private float _lastClickTime = 0f;
    private const float CLICK_THRESHOLD = 0.3f; // 연타 방지 임계값 ($T_{\text{click\_delta}} = 0.3\text{s}$)

    private void Awake()
    {
        // 씬 내부 자동 탐색 예외 처리 (컴포넌트 미할당 시 자동 캐싱)
        if (toolGachaDirector == null)
        {
            toolGachaDirector = FindAnyObjectByType<GachaDirector>(FindObjectsInactive.Include);
        }

        if (animalGachaDirector == null)
        {
            animalGachaDirector = FindAnyObjectByType<GachaPortalController>(FindObjectsInactive.Include);
        }
    }

    private void OnEnable() => InitButtonListeners();
    private void Start() => InitButtonListeners();
    private void OnDisable() => RemoveButtonListeners();

    // 모든 가챠 관련 버튼 리스너 일괄 등록
    private void InitButtonListeners()
    {
        RemoveButtonListeners();

        // ------------ 26.08.06 KAY 수정 (주석처리) ------------------
        // 1. 메인 가챠 창 열기 버튼
        //if (btnOpenGachaWindow != null)
        //    btnOpenGachaWindow.onClick.AddListener(ToggleGachaWindow);

        // 2. 도구 가챠 초기창 버튼
        if (btnToolDraw1 != null)
            btnToolDraw1.onClick.AddListener(HandleToolDrawOne);
        if (btnToolDraw10 != null)
            btnToolDraw10.onClick.AddListener(HandleToolDrawTen);

        // 3. 동물 가챠 초기창 버튼
        if (btnAnimalDraw1 != null)
            btnAnimalDraw1.onClick.AddListener(HandleAnimalDrawOne);
        if (btnAnimalDraw10 != null)
            btnAnimalDraw10.onClick.AddListener(HandleAnimalDrawTen);

        // 4. 도구 가챠 전용 결과창 다시 뽑기 버튼
        if (btnToolResultDraw1 != null)
            btnToolResultDraw1.onClick.AddListener(HandleToolResultDrawOne);
        if (btnToolResultDraw10 != null)
            btnToolResultDraw10.onClick.AddListener(HandleToolResultDrawTen);

        // 5. 동물 가챠 전용 결과창 다시 뽑기 버튼
        if (btnAnimalResultDraw1 != null)
            btnAnimalResultDraw1.onClick.AddListener(HandleAnimalResultDrawOne);
        if (btnAnimalResultDraw10 != null)
            btnAnimalResultDraw10.onClick.AddListener(HandleAnimalResultDrawTen);

        // 6. 통합 공용 결과창 다시 뽑기 버튼 (마지막 실행된 가챠 타입 추적)
        if (btnResultDraw1 != null)
            btnResultDraw1.onClick.AddListener(HandleSharedResultDrawOne);
        if (btnResultDraw10 != null)
            btnResultDraw10.onClick.AddListener(HandleSharedResultDrawTen);
    }

    // 가챠 버튼별 콜백
    private void HandleToolDrawOne() => OnClickedDrawGacha(GachaType.Tool, 1);
    private void HandleToolDrawTen() => OnClickedDrawGacha(GachaType.Tool, 10);
    private void HandleAnimalDrawOne() => OnClickedDrawGacha(GachaType.Animal, 1);
    private void HandleAnimalDrawTen() => OnClickedDrawGacha(GachaType.Animal, 10);
    private void HandleToolResultDrawOne() => OnClickedDrawGacha(GachaType.Tool, 1);
    private void HandleToolResultDrawTen() => OnClickedDrawGacha(GachaType.Tool, 10);
    private void HandleAnimalResultDrawOne() => OnClickedDrawGacha(GachaType.Animal, 1);
    private void HandleAnimalResultDrawTen() => OnClickedDrawGacha(GachaType.Animal, 10);
    private void HandleSharedResultDrawOne() => OnClickedDrawGacha(_lastExecutedType, 1);
    private void HandleSharedResultDrawTen() => OnClickedDrawGacha(_lastExecutedType, 10);

    // 이벤트 중복 등록 방지를 위한 자체 리스너 해제 처리
    private void RemoveButtonListeners()
    {
        // ------------ 26.08.06 KAY 수정 (주석처리) ------------------
        //if (btnOpenGachaWindow != null) btnOpenGachaWindow.onClick.RemoveAllListeners();

        if (btnToolDraw1 != null) btnToolDraw1.onClick.RemoveListener(HandleToolDrawOne);
        if (btnToolDraw10 != null) btnToolDraw10.onClick.RemoveListener(HandleToolDrawTen);

        if (btnAnimalDraw1 != null) btnAnimalDraw1.onClick.RemoveListener(HandleAnimalDrawOne);
        if (btnAnimalDraw10 != null) btnAnimalDraw10.onClick.RemoveListener(HandleAnimalDrawTen);

        if (btnToolResultDraw1 != null) btnToolResultDraw1.onClick.RemoveListener(HandleToolResultDrawOne);
        if (btnToolResultDraw10 != null) btnToolResultDraw10.onClick.RemoveListener(HandleToolResultDrawTen);

        if (btnAnimalResultDraw1 != null) btnAnimalResultDraw1.onClick.RemoveListener(HandleAnimalResultDrawOne);
        if (btnAnimalResultDraw10 != null) btnAnimalResultDraw10.onClick.RemoveListener(HandleAnimalResultDrawTen);

        if (btnResultDraw1 != null) btnResultDraw1.onClick.RemoveListener(HandleSharedResultDrawOne);
        if (btnResultDraw10 != null) btnResultDraw10.onClick.RemoveListener(HandleSharedResultDrawTen);
    }

    public void ToggleGachaWindow()
    {
        if (IsAnyDirectorAnimating()) return;
        if (teammateInitialWindow == null) return;

        bool isCurrentlyActive = teammateInitialWindow.activeSelf;
        if (isCurrentlyActive)
            CloseAllGachaWindows();
        else
            OpenAllGachaWindows();
    }

    public void OpenAllGachaWindows()
    {
        CloseOtherMainPanels();
        onGachaWindowOpened?.Invoke();

        if (teammateInitialWindow != null) teammateInitialWindow.SetActive(true);
        if (toolGachaDirector != null) toolGachaDirector.OpenGachaWindowOnly();
    }

    public void CloseAllGachaWindows()
    {
        if (teammateInitialWindow != null) teammateInitialWindow.SetActive(false);
        if (toolGachaDirector != null) toolGachaDirector.CloseGachaUI();
        if (animalGachaDirector != null) animalGachaDirector.CloseGachaUI();
    }

    private void CloseOtherMainPanels()
    {
        for (int i = 0; i < otherMainPanels.Count; i++)
        {
            if (otherMainPanels[i] != null && otherMainPanels[i].activeSelf)
            {
                otherMainPanels[i].SetActive(false);
            }
        }
    }

    // 가챠 뽑기 핵심 실행 핸들러 (초기창 및 결과창 리롤 공용)
    private void OnClickedDrawGacha(GachaType gachaType, int drawCount)
    {
        // 1. 연출 진행 중 뽑기 실행 방지 락
        if (IsAnyDirectorAnimating())
        {
            Debug.LogWarning("<color=yellow>[GachaBridge]</color> 현재 가챠 연출이 진행 중이므로 뽑기를 실행할 수 없습니다!");
            return;
        }

        // 2. 연타 방지 임계시간 검사 ($T_{\text{current}} - T_{\text{last}} < 0.3\text{s}$)
        if (Time.time - _lastClickTime < CLICK_THRESHOLD) return;
        _lastClickTime = Time.time;

        _lastExecutedType = gachaType;
        List<GachaEntryData> entryDataList = new List<GachaEntryData>();

        // ----------------------------------------------------
        // 도구 뽑기 실행 로직 (Tool Gacha)
        // ----------------------------------------------------
        if (gachaType == GachaType.Tool)
        {
            if (toolGachaManager == null)
            {
                Debug.LogError("<color=red>[GachaBridge]</color> ToolGachaManager가 할당되지 않았습니다!");
                return;
            }

            long requiredCost = toolGachaManager.GetCost(drawCount);
            if (CoinWallet != null && CoinWallet.Balance < requiredCost)
            {
                Debug.LogWarning($"<color=red>[GachaBridge] 도구 골드 부족!</color> (필요: {requiredCost})");
                return;
            }

            if (CoinWallet != null && !CoinWallet.TrySpend(requiredCost)) return;

            List<GachaResult> rollResults = toolGachaManager.RollMulti(drawCount);
            if (rollResults != null)
            {
                foreach (GachaResult res in rollResults)
                {
                    if (res.Entry != null) entryDataList.Add(res.Entry);
                }
            }

            // 초기 대기창 비활성화
            if (teammateInitialWindow != null) teammateInitialWindow.SetActive(false);

            int actualCount = entryDataList.Count > 0 ? entryDataList.Count : drawCount;
            if (toolGachaDirector != null)
            {
                // 연출 디렉터 실행 (내부에서 이전 결과창 자동 정리 및 연출 시작)
                toolGachaDirector.StartGachaSequence(actualCount, entryDataList);
            }
            else
            {
                Debug.LogError("<color=red>[GachaBridge]</color> toolGachaDirector가 할당되지 않았습니다!");
            }
        }
        // ----------------------------------------------------
        // 동물 뽑기 실행 로직 (Animal Gacha)
        // ----------------------------------------------------
        else if (gachaType == GachaType.Animal)
        {
            if (animalGachaManager == null)
            {
                Debug.LogError("<color=red>[GachaBridge]</color> AnimalGachaManager가 할당되지 않았습니다!");
                return;
            }

            long requiredCost = animalGachaManager.GetCost(drawCount);
            if (CoinWallet != null && CoinWallet.Balance < requiredCost)
            {
                Debug.LogWarning($"<color=red>[GachaBridge] 동물 골드 부족!</color> (필요: {requiredCost})");
                return;
            }

            if (CoinWallet != null && !CoinWallet.TrySpend(requiredCost)) return;

            List<GachaResult> rollResults = animalGachaManager.RollMulti(drawCount);
            if (rollResults != null)
            {
                foreach (GachaResult res in rollResults)
                {
                    if (res.Entry != null) entryDataList.Add(res.Entry);
                }
            }

            // 초기 대기창 비활성화
            if (teammateInitialWindow != null) teammateInitialWindow.SetActive(false);

            int actualCount = entryDataList.Count > 0 ? entryDataList.Count : drawCount;
            if (animalGachaDirector != null)
            {
                // 연출 디렉터 실행 (내부에서 이전 결과창 자동 정리 및 연출 시작)
                animalGachaDirector.StartGachaSequence(actualCount, entryDataList);
            }
            else
            {
                Debug.LogError("<color=red>[GachaBridge]</color> animalGachaDirector가 할당되지 않았습니다!");
            }
        }
    }

    // 현재 연출 진행 중인 디렉터가 하나라도 있는지 종합 검사
    private bool IsAnyDirectorAnimating()
    {
        bool toolAnimating = toolGachaDirector != null && toolGachaDirector.IsAnimating;
        bool animalAnimating = animalGachaDirector != null && animalGachaDirector.IsAnimating;

        return toolAnimating || animalAnimating;
    }
}