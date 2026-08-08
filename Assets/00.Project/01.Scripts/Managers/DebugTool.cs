using Animal.Data;
using TaskTown.Gacha;
using TaskTown.KDH;
using Tool.Data;
using UI;
using UnityEngine;
/*#if UNITY_EDITOR || DEVELOPMENT_BUILD*/
/// <summary>
/// 코인/획득량/시간당 한도/리텐션 상태에 사용하는 디버그 패널.
/// OnGUI로 간단히 만들어서(백쿼트 키) 토글로 켜고 끕니다.
/// 릴리즈 빌드에서는 컴파일에서 제외됩니다.
/// </summary>
public class DebugTool : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote; // ` 키
    [SerializeField] private bool showOnStart = true;
    // 입력 상태 (OnGUI TextField용). Update에서 문자열 변환을 하지 않고 필드만 유지합니다.
    private string coinBalanceInput = "0";
    private string clickCoinInput = "5";
    private string typingCoinInput = "3";
    private string maxPerHourInput = "10000";
    private string offlineHoursInput = "5";
    private bool isVisible;
    private Rect windowRect = new Rect(20f, 20f, 380f, 520f);
    private Vector2 scrollPos;
    // Awake에서 싱글턴을 캐시해 Update 안에서는 Find 검색을 안 합니다.
    private CoinManager coinManager;
    private EarnProcessor earnProcessor;
    private OfflineRewardManager offlineRewardManager;
    private InventoryManager_Animal animalInventory;
    private InventoryManager_Tool toolInventory;
    private TownUpgradeManager townUpgradeManager;
    private string lastOfflineRewardText = "(아직 없음)";
    private string lastSaveActionText = "(없음)";
    private string lastUpgradeActionText = "(없음)";

    //------------------26.07.24 KDH 추가---------------------------------
    private string grantIdInput = "";
    private string grantCountInput = "1";
    private string grantLevelInput = "1";
    private bool grantAsAnimal = true; // true=동물, false=도구
    private string lastGrantActionText = "(없음)";
    //-----------------26.07.27 KDH-----------------------------------------
    private VillageUpgradeUI_Manager villageUpgradeUIManager;
    //---------------------------------------------------------------------

    //------------------26.08.05 KAY 추가 (마을 레벨 설정)---------------------------------
    private string townLevelInput = "1";
    private const int DebugTownLevelMax = 39; // 40은 설정 불가
    //-----------------------------------------------------------------------------

    //-----------------26.08.04 KNW: 난이도 전환 디버그(시크릿 동물 난이도 해금 테스트용)-------
    private RealProductionTicker realProductionTicker;
    private string lastDifficultyActionText = "(없음)";
    //---------------------------------------------------------------------

    // ----------------08.08.KAY (인벤토리 + 도감 초기화)------------------
    [Header("도감 초기화 (Debug)")]
    [SerializeField] private AnimalDatabase animalDatabaseForDexReset;
    [SerializeField] private ToolDatabase toolDatabaseForDexReset;
    private string lastDexResetActionText = "(없음)";
    // ---------------------------------------------------------

    private void Awake()
    {
        coinManager = CoinManager.Instance;
        earnProcessor = EarnProcessor.Instance;
        isVisible = showOnStart;
    }

    private void Start()
    {
        // Start 시점에는 Instance가 준비돼 있을 수 있어 한 번 더 캐시 + 현재값으로 입력칸 채움
        CacheManagersIfNeeded();
        SyncInputsFromManagers();

        if (offlineRewardManager != null)
            offlineRewardManager.OnOfflineRewardGranted += HandleOfflineRewardGranted;
    }

    private void OnDestroy()
    {
        if (offlineRewardManager != null)
            offlineRewardManager.OnOfflineRewardGranted -= HandleOfflineRewardGranted;
    }

    private void Update()
    {
        // 토글만 처리. 그외엔 갱신/할당 없음.
        if (Input.GetKeyDown(toggleKey))
            isVisible = !isVisible;
    }

    private void OnGUI()
    {
        if (!isVisible) return;
        CacheManagersIfNeeded();
        windowRect = GUI.Window(GetInstanceID(), windowRect, DrawWindow, "Debug Tool");
    }

    private void DrawWindow(int id)
    {
        scrollPos = GUILayout.BeginScrollView(scrollPos);

        GUILayout.Space(8f);
        // --- 1) 코인 잔액 ---
        GUILayout.Label($"코인 잔액: {(coinManager != null ? coinManager.Balance.ToString() : "-")}");
        GUILayout.BeginHorizontal();
        coinBalanceInput = GUILayout.TextField(coinBalanceInput, GUILayout.Width(160f));
        if (GUILayout.Button("잔액 설정"))
            ApplyCoinBalance();
        GUILayout.EndHorizontal();
        GUILayout.Space(8f);

        // --- 2) 인풋 획득량 (클릭/타이핑) ---
        GUILayout.Label("인풋 획득 설정");
        GUILayout.BeginHorizontal();
        GUILayout.Label("클릭", GUILayout.Width(48f));
        clickCoinInput = GUILayout.TextField(clickCoinInput, GUILayout.Width(80f));
        if (GUILayout.Button("적용", GUILayout.Width(60f)))
            ApplyClickCoin();
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("타이핑", GUILayout.Width(48f));
        typingCoinInput = GUILayout.TextField(typingCoinInput, GUILayout.Width(80f));
        if (GUILayout.Button("적용", GUILayout.Width(60f)))
            ApplyTypingCoin();
        GUILayout.EndHorizontal();
        GUILayout.Space(8f);

        // --- 3) 시간당(주기당) 최대 획득량 ---
        if (earnProcessor != null)
        {
            GUILayout.Label(
                $"주기 한도: {earnProcessor.CurrentPeriodEarnedCoin} / {earnProcessor.MaxCoinPerHour}" +
                $"  (타이머 {earnProcessor.LimitTimer:0} / {earnProcessor.LimitPeriodSeconds:0}s)");
        }
        else
        {
            GUILayout.Label("EarnProcessor 없음");
        }
        GUILayout.BeginHorizontal();
        maxPerHourInput = GUILayout.TextField(maxPerHourInput, GUILayout.Width(160f));
        if (GUILayout.Button("시간당 한도 설정"))
            ApplyMaxPerHour();
        GUILayout.EndHorizontal();
        GUILayout.Space(8f);

        // --- 4) limitPeriodSeconds 주기 리셋 ---
        if (GUILayout.Button("획득 제한 시간 리셋 (limitPeriod)"))
            ResetEarnLimitPeriod();
        GUILayout.Space(8f);
        if (GUILayout.Button("입력칸 값 실제값과 동기화"))
            SyncInputsFromManagers();

        GUILayout.Space(16f);
        DrawSeparator();

        // --- 5) 오프라인 보상 테스트 ---
        GUILayout.Label("오프라인 보상 테스트", GUI.skin.box);
        if (offlineRewardManager == null)
        {
            GUILayout.Label("OfflineRewardManager 없음");
        }
        else
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("경과 시간(h)", GUILayout.Width(80f));
            offlineHoursInput = GUILayout.TextField(offlineHoursInput, GUILayout.Width(60f));
            if (GUILayout.Button("오프라인 시뮬레이션"))
                SimulateOffline();
            GUILayout.EndHorizontal();
            GUILayout.Label($"마지막 지급 결과: {lastOfflineRewardText}");
        }

        GUILayout.Space(16f);
        DrawSeparator();

        // --- 6) 도감 유지 테스트 ---
        GUILayout.Label("도감(수집 기록) 유지 테스트", GUI.skin.box);
        GUILayout.Label("보유 동물/도구만 초기화합니다. 도감은 DexRecordManager가 별도로 저장하고 있어서");
        GUILayout.Label("초기화 후에도 도감 패널에는 계속 '이미 본 적 있음'으로 표시돼야 정상입니다.");
        if (GUILayout.Button("보유 동물/도구 인벤토리 초기화"))
            ResetInventoryForDexTest();

        // ----------------08.08.KAY (인벤토리 + 도감 초기화)------------------
        GUILayout.Space(16f);
        DrawSeparator();
        GUILayout.Label("인벤토리 + 도감 초기화", GUI.skin.box);
        GUILayout.Label("보유 인벤토리와 도감 해금(PlayerPrefs)을 함께 초기화합니다.");
        if (GUILayout.Button("인벤토리 + 도감 초기화"))
            ResetInventoryAndDex();
        GUILayout.Label($"마지막 결과: {lastDexResetActionText}");
        // ---------------------------------------------------------

        //-------------------26.07.24 KDH 추가----------------------------------
        GUILayout.Space(16f);
        DrawSeparator();
        // --- 동물/도구 ID 지급·레벨 ---
        GUILayout.Label("동물/도구 디버그 지급", GUI.skin.box);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(grantAsAnimal ? "[동물]" : "동물", GUILayout.Width(70f)))
            grantAsAnimal = true;
        if (GUILayout.Button(!grantAsAnimal ? "[도구]" : "도구", GUILayout.Width(70f)))
            grantAsAnimal = false;
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("ID", GUILayout.Width(40f));
        grantIdInput = GUILayout.TextField(grantIdInput, GUILayout.Width(180f));
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("개수", GUILayout.Width(40f));
        grantCountInput = GUILayout.TextField(grantCountInput, GUILayout.Width(60f));
        if (GUILayout.Button("지급", GUILayout.Width(60f)))
            GrantById();
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("레벨", GUILayout.Width(40f));
        grantLevelInput = GUILayout.TextField(grantLevelInput, GUILayout.Width(60f));
        if (GUILayout.Button("레벨 설정", GUILayout.Width(80f)))
            SetLevelById();
        GUILayout.EndHorizontal();
        GUILayout.Label($"마지막 결과: {lastGrantActionText}");
        //-----------------------------------------------------------------------

        GUILayout.Space(16f);
        DrawSeparator();

        // --- 7) 세이브(저장/불러오기) 테스트 ---
        GUILayout.Label("세이브(저장/불러오기) 테스트", GUI.skin.box);
        if (SaveManager.Instance == null)
        {
            GUILayout.Label("SaveManager 없음");
        }
        else
        {
            GUILayout.Label($"세이브 파일 존재: {(SaveManager.Instance.HasSave() ? "예" : "아니오")}");
            GUILayout.Label($"마지막 세이브 동작: {lastSaveActionText}");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("지금 저장"))
            {
                SaveManager.Instance.SaveGame();
                lastSaveActionText = "저장 완료";
            }
            if (GUILayout.Button("불러오기"))
            {
                SaveManager.Instance.LoadGame();
                lastSaveActionText = "불러오기 완료";
            }
            if (GUILayout.Button("세이브 삭제"))
            {
                SaveManager.Instance.DeleteSave();
                lastSaveActionText = "세이브 삭제 완료";
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("빌드에서 껐다 켜도 유지되는지 확인: 저장 -> 게임 종료 -> 재실행 시 코인/인벤토리 복원");
        }

        GUILayout.Space(16f);
        DrawSeparator();

        // --- 8) 마을 업그레이드(클릭/타이핑/도구효율) 테스트 ---
        GUILayout.Label("마을 업그레이드 테스트 (이슈 #72)", GUI.skin.box);
        if (townUpgradeManager == null)
        {
            GUILayout.Label("TownUpgradeManager 없음");
        }
        else
        {
            GUILayout.Label(
                $"클릭 배율: x{(earnProcessor != null ? earnProcessor.ClickMultiplier : 1)}" +
                $"  타이핑 배율: x{(earnProcessor != null ? earnProcessor.TypingMultiplier : 1)}" +
                $"  도구효율 배율: x{townUpgradeManager.ToolEfficiencyMultiplier:0.00}");
            GUILayout.Label("(마을 레벨을 올려도 도구효율 배율이 바뀌지 않아야 정상 - 이제 구매로만 오릅니다)");

            GUILayout.BeginHorizontal();
            GUILayout.Label($"클릭 코인 Lv.{townUpgradeManager.ClickLevel + 1}", GUILayout.Width(120f));
            GUILayout.Label($"다음 비용 {townUpgradeManager.ClickUpgradeNextCost:N0}", GUILayout.Width(120f));
            //-------------------------------------26.07.29 KDH---------------------------------------------
            //if (GUILayout.Button("구매", GUILayout.Width(60f)))
            //    BuyUpgrade(townUpgradeManager.TryUpgradeClick, "클릭 코인");   Original
            if (GUILayout.Button("구매", GUILayout.Width(60f)))
            {
                if (villageUpgradeUIManager == null)
                    lastUpgradeActionText = "VillageUpgradeUI_Manager 없음";
                else
                    BuyUpgrade(villageUpgradeUIManager.DebugTryUpgradeClick, "클릭 코인");
            }
            //-----------------------------------------------------------------------------------------
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"타이핑 코인 Lv.{townUpgradeManager.TypingLevel + 1}", GUILayout.Width(120f));
            GUILayout.Label($"다음 비용 {townUpgradeManager.TypingUpgradeNextCost:N0}", GUILayout.Width(120f));
            //-------------------------------------26.07.29 KDH---------------------------------------------
            //if (GUILayout.Button("구매", GUILayout.Width(60f)))
            //    BuyUpgrade(townUpgradeManager.TryUpgradeTyping, "타이핑 코인");     Original
            if (GUILayout.Button("구매", GUILayout.Width(60f)))
            {
                if (villageUpgradeUIManager == null)
                    lastUpgradeActionText = "VillageUpgradeUI_Manager 없음";
                else
                    BuyUpgrade(villageUpgradeUIManager.DebugTryUpgradeTyping, "타이핑 코인");
            }
            //------------------------------------------------------------------------------------------
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"도구 효율 Lv.{townUpgradeManager.ToolEfficiencyLevel + 1}", GUILayout.Width(120f));
            GUILayout.Label($"다음 비용 {townUpgradeManager.ToolEfficiencyUpgradeNextCost:N0}", GUILayout.Width(120f));
            //-------------------------------------26.07.29 KDH---------------------------------------------
            //if (GUILayout.Button("구매", GUILayout.Width(60f)))
            //    BuyUpgrade(townUpgradeManager.TryUpgradeToolEfficiency, "도구 효율");
            if (GUILayout.Button("구매", GUILayout.Width(60f)))
            {
                if (villageUpgradeUIManager == null)
                    lastUpgradeActionText = "VillageUpgradeUI_Manager 없음";
                else
                    BuyUpgrade(villageUpgradeUIManager.DebugTryUpgradeTool, "도구 효율");
            }
            //------------------------------------------------------------------------------------------
            GUILayout.EndHorizontal();

            GUILayout.Label($"마지막 결과: {lastUpgradeActionText}");
        }

        //--------------------------26.07.27 KDH--------------------------------------
        GUILayout.Label($"마을 레벨: {(villageUpgradeUIManager != null ? villageUpgradeUIManager.UiTownLevel.ToString() : "-")}");
        if (GUILayout.Button("마을 레벨 초기화 (Lv.1)"))
        {
            if (villageUpgradeUIManager == null)
                lastUpgradeActionText = "VillageUpgradeUI_Manager 없음";
            else
            {
                townUpgradeManager.LoadLevels(0, 0, 0);   // 26.07.29 LoadLevels(1,1,1) Change
                villageUpgradeUIManager.DebugSetTownLevel(1);
                lastUpgradeActionText = "마을 레벨 Lv.1 초기화";
            }
        }
        //-----------------------------------------------------------------------------

        //------------------26.08.05 KAY 추가 (마을 레벨업)---------------------------------
        if (GUILayout.Button("마을 레벨업 (요소 완료 + Lv+1)"))
        {
            if (villageUpgradeUIManager == null)
                lastUpgradeActionText = "VillageUpgradeUI_Manager 없음";
            else if (!villageUpgradeUIManager.DebugForceVillageLevelUp())
                lastUpgradeActionText = "마을 레벨업 실패 (최대 레벨/코인 부족/매니저 없음)";
            else
                lastUpgradeActionText = $"마을 레벨업 완료 → Lv.{villageUpgradeUIManager.UiTownLevel}";
        }
        //-----------------------------------------------------------------------------

        //------------------26.08.05 KAY 추가 (마을 레벨 설정)---------------------------------
        GUILayout.BeginHorizontal();
        GUILayout.Label("마을 레벨 설정 (1~39)", GUILayout.Width(140f));
        townLevelInput = GUILayout.TextField(townLevelInput, GUILayout.Width(80f));
        if (GUILayout.Button("적용", GUILayout.Width(60f)))
        {
            if (villageUpgradeUIManager == null)
                lastUpgradeActionText = "VillageUpgradeUI_Manager 없음";
            else if (!int.TryParse(townLevelInput, out int level))
                lastUpgradeActionText = "마을 레벨 값이 숫자가 아님";
            else if (level >= 40)
                lastUpgradeActionText = "마을 레벨 40 이상은 설정 불가 (최대 39)";
            else
            {
                int clamped = Mathf.Clamp(level, 1, DebugTownLevelMax);
                villageUpgradeUIManager.DebugSetTownLevel(clamped);
                lastUpgradeActionText = $"마을 레벨 설정 → Lv.{villageUpgradeUIManager.UiTownLevel}";
            }
        }
        GUILayout.EndHorizontal();
        //-----------------------------------------------------------------------------

        //--------------------------26.07.30 KNW--------------------------------------
        // #19: 완주 판정/선택 UI가 아직 없어서, 엔드리스 모드를 임시로 켜고 끄며 검증하기 위한 버튼입니다.
        GUILayout.Label($"엔드리스 모드: {(villageUpgradeUIManager != null ? villageUpgradeUIManager.IsEndlessMode.ToString() : "-")}");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("엔드리스 ON"))
        {
            if (villageUpgradeUIManager == null)
                lastUpgradeActionText = "VillageUpgradeUI_Manager 없음";
            else
            {
                villageUpgradeUIManager.DebugSetEndlessMode(true);
                lastUpgradeActionText = "엔드리스 모드 ON";
            }
        }
        if (GUILayout.Button("엔드리스 OFF"))
        {
            if (villageUpgradeUIManager == null)
                lastUpgradeActionText = "VillageUpgradeUI_Manager 없음";
            else
            {
                villageUpgradeUIManager.DebugSetEndlessMode(false);
                lastUpgradeActionText = "엔드리스 모드 OFF";
            }
        }
        GUILayout.EndHorizontal();
        //-----------------------------------------------------------------------------

        //--------------------------26.08.04 KNW--------------------------------------
        // 시크릿 동물(하드/매우어려움 전용)이 실제로 뽑히는지 확인하기 위한 난이도 전환 테스트.
        // 정식 난이도 선택 UI/흐름은 아직 없어서, RealProductionTicker.SetDifficulty를 직접 호출합니다.
        GUILayout.Space(16f);
        DrawSeparator();
        GUILayout.Label("난이도 테스트 (시크릿 동물 해금 확인용)", GUI.skin.box);
        GUILayout.Label($"현재 난이도: {(realProductionTicker != null ? realProductionTicker.CurrentDifficulty.ToString() : "-")}");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Normal"))
            SetDebugDifficulty(DifficultyType.Normal);
        if (GUILayout.Button("Hard"))
            SetDebugDifficulty(DifficultyType.Hard);
        if (GUILayout.Button("VeryHard"))
            SetDebugDifficulty(DifficultyType.VeryHard);
        GUILayout.EndHorizontal();
        GUILayout.Label($"마지막 결과: {lastDifficultyActionText}");
        //-----------------------------------------------------------------------------

        GUILayout.Space(8f);
        GUI.DragWindow();
        GUILayout.EndScrollView();
    }

    private void DrawSeparator()
    {
        GUILayout.Box(string.Empty, GUILayout.Height(2f), GUILayout.ExpandWidth(true));
    }

    private void CacheManagersIfNeeded()
    {
        // Instance가 늦게 준비되는 것에 대비. null이 아니면 재조회해서 불필요한 검색 비용을 없앱니다.
        if (coinManager == null)
            coinManager = CoinManager.Instance;
        if (earnProcessor == null)
            earnProcessor = EarnProcessor.Instance;
        if (offlineRewardManager == null)
            offlineRewardManager = OfflineRewardManager.Instance;
        if (animalInventory == null)
            animalInventory = InventoryManager_Animal.Instance;
        if (toolInventory == null)
            toolInventory = InventoryManager_Tool.Instance;
        if (townUpgradeManager == null)
            townUpgradeManager = TownUpgradeManager.Instance;
        //-------------------------------26.07.27 KDH--------------------------------------
        if (villageUpgradeUIManager == null)
            villageUpgradeUIManager = FindAnyObjectByType<VillageUpgradeUI_Manager>();
        //---------------------------------------------------------------------------------
        //-------------------------------26.08.04 KNW--------------------------------------
        if (realProductionTicker == null)
            realProductionTicker = RealProductionTicker.Instance;
        //---------------------------------------------------------------------------------
    }

    private void SyncInputsFromManagers()
    {
        if (coinManager != null)
            coinBalanceInput = coinManager.Balance.ToString();
        if (earnProcessor != null)
        {
            clickCoinInput = earnProcessor.BaseCoinPerClick.ToString();
            typingCoinInput = earnProcessor.BaseCoinPerTyping.ToString();
            maxPerHourInput = earnProcessor.MaxCoinPerHour.ToString();
        }
    }

    private void ApplyCoinBalance()
    {
        if (coinManager == null)
        {
            Debug.LogWarning("[DebugTool] CoinManager가 없습니다.");
            return;
        }
        if (!long.TryParse(coinBalanceInput, out long value))
        {
            Debug.LogWarning("[DebugTool] 잔액 값이 숫자가 아닙니다.");
            return;
        }
        coinManager.SetCoin(value);
    }

    private void ApplyClickCoin()
    {
        if (earnProcessor == null) return;
        if (!int.TryParse(clickCoinInput, out int value)) return;
        earnProcessor.SetBaseCoinPerClick(value);
    }

    private void ApplyTypingCoin()
    {
        if (earnProcessor == null) return;
        if (!int.TryParse(typingCoinInput, out int value)) return;
        earnProcessor.SetBaseCoinPerTyping(value);
    }

    private void ApplyMaxPerHour()
    {
        if (earnProcessor == null) return;
        if (!int.TryParse(maxPerHourInput, out int value)) return;
        earnProcessor.SetMaxCoinPerHour(value);
    }

    // EarnProcessor.limitTimer / currentPeriodEarnedCoin 을 0으로 리셋합니다.
    private void ResetEarnLimitPeriod()
    {
        if (earnProcessor == null)
        {
            Debug.LogWarning("[DebugTool] EarnProcessor가 없습니다.");
            return;
        }
        earnProcessor.ResetLimitPeriod();
    }

    private void SimulateOffline()
    {
        if (offlineRewardManager == null)
        {
            Debug.LogWarning("[DebugTool] OfflineRewardManager가 없습니다.");
            return;
        }
        if (!float.TryParse(offlineHoursInput, out float hours))
        {
            Debug.LogWarning("[DebugTool] 경과 시간 값이 숫자가 아닙니다.");
            return;
        }
        offlineRewardManager.SimulateOfflineElapsed(hours);
    }

    private void HandleOfflineRewardGranted(long reward, System.TimeSpan duration)
    {
        lastOfflineRewardText = $"{duration.TotalHours:0.#}시간 방치 -> {reward:N0}코인 지급";
    }

    private void BuyUpgrade(System.Func<bool> tryUpgrade, string label)
    {
        bool bought = tryUpgrade();
        lastUpgradeActionText = bought ? $"{label} 구매 성공" : $"{label} 구매 실패(코인 부족 또는 최대 레벨)";
    }

    private void ResetInventoryForDexTest()
    {
        if (animalInventory != null)
            animalInventory.ClearAnimalInventory();

        if (toolInventory != null)
            toolInventory.ClearToolInventory();

        Debug.Log("[DebugTool] 보유 동물/도구 인벤토리를 초기화했습니다. 도감 패널에서 유지 여부를 확인하세요.");
    }

    // ----------------08.08.KAY (인벤토리 + 도감 초기화)------------------
    /// <summary>
    /// 보유 인벤토리와 도감 해금 기록을 함께 초기화합니다.
    /// </summary>
    private void ResetInventoryAndDex()
    {
        CacheManagersIfNeeded();

        if (animalInventory != null)
            animalInventory.ClearAnimalInventory();

        if (toolInventory != null)
            toolInventory.ClearToolInventory();

        AnimalDatabase animalDb = ResolveAnimalDatabaseForDexReset();
        ToolDatabase toolDb = ResolveToolDatabaseForDexReset();

        if (DexRecordManager.Instance == null)
        {
            lastDexResetActionText = "DexRecordManager 없음 (인벤토리만 초기화됨)";
            Debug.LogWarning("[DebugTool] DexRecordManager가 없어 도감 기록은 지우지 못했습니다.");
            return;
        }

        if (animalDb == null && toolDb == null)
        {
            lastDexResetActionText = "Animal/Tool Database 없음 (인벤토리만 초기화됨)";
            Debug.LogWarning("[DebugTool] AnimalDatabase/ToolDatabase를 찾지 못해 도감 기록은 지우지 못했습니다. Inspector에 연결하세요.");
            return;
        }

        DexRecordManager.Instance.ClearAllDiscoveredRecords(animalDb, toolDb);

        UIController_AnimalDex animalDex = FindAnyObjectByType<UIController_AnimalDex>();
        if (animalDex != null)
            animalDex.DebugRefreshUnlockStates();

        UIController_ToolDex toolDex = FindAnyObjectByType<UIController_ToolDex>();
        if (toolDex != null)
            toolDex.DebugRefreshUnlockStates();

        lastDexResetActionText = "인벤토리 + 도감 초기화 완료";
        Debug.Log("[DebugTool] 인벤토리와 도감 해금 기록을 초기화했습니다.");
    }

    private AnimalDatabase ResolveAnimalDatabaseForDexReset()
    {
        if (animalDatabaseForDexReset != null)
            return animalDatabaseForDexReset;

        AnimalDatabase[] found = Resources.FindObjectsOfTypeAll<AnimalDatabase>();
        if (found != null && found.Length > 0)
            return found[0];

        return null;
    }

    private ToolDatabase ResolveToolDatabaseForDexReset()
    {
        if (toolDatabaseForDexReset != null)
            return toolDatabaseForDexReset;

        ToolDatabase[] found = Resources.FindObjectsOfTypeAll<ToolDatabase>();
        if (found != null && found.Length > 0)
            return found[0];

        return null;
    }
    // ---------------------------------------------------------

    private void GrantById()    // 26.07.24 KDH 추가
    {
        CacheManagersIfNeeded();
        string id = grantIdInput != null ? grantIdInput.Trim() : string.Empty;
        if (string.IsNullOrEmpty(id))
        {
            lastGrantActionText = "ID가 비어 있음";
            return;
        }
        if (!int.TryParse(grantCountInput, out int count) || count < 1)
        {
            lastGrantActionText = "개수가 잘못됨";
            return;
        }
        bool ok;
        if (grantAsAnimal)
        {
            if (animalInventory == null)
            {
                lastGrantActionText = "AnimalInventory 없음";
                return;
            }
            ok = animalInventory.DebugAddAnimal(id, count);
            lastGrantActionText = ok ? $"동물 지급 성공: {id} x{count}" : $"동물 지급 실패: {id}";
        }
        else
        {
            if (toolInventory == null)
            {
                lastGrantActionText = "ToolInventory 없음";
                return;
            }
            ok = toolInventory.DebugAddTool(id, count);
            lastGrantActionText = ok ? $"도구 지급 성공: {id} x{count}" : $"도구 지급 실패: {id}";
        }
    }

    private void SetDebugDifficulty(DifficultyType difficulty)   // 26.08.04 KNW 추가
    {
        CacheManagersIfNeeded();
        if (realProductionTicker == null)
        {
            lastDifficultyActionText = "RealProductionTicker 없음";
            return;
        }
        realProductionTicker.SetDifficulty(difficulty);
        lastDifficultyActionText = $"난이도 -> {difficulty}";

        // ----------------08.08.KAY (난이도 변경 시 도감 Dif 갱신)------------------
        // 개발용: 난이도 전환 직후 이미 생성된 동물 도감 슬롯의 Dif 표시를 갱신합니다.
        UIController_AnimalDex animalDex = FindAnyObjectByType<UIController_AnimalDex>(FindObjectsInactive.Include);
        if (animalDex != null)
            animalDex.DebugRefreshUnlockStates();
        // ---------------------------------------------------------
    }

    private void SetLevelById()     // 26.07.24 KDH 추가
    {
        CacheManagersIfNeeded();
        string id = grantIdInput != null ? grantIdInput.Trim() : string.Empty;
        if (string.IsNullOrEmpty(id))
        {
            lastGrantActionText = "ID가 비어 있음";
            return;
        }
        if (!int.TryParse(grantLevelInput, out int level))
        {
            lastGrantActionText = "레벨이 잘못됨";
            return;
        }
        bool ok;
        if (grantAsAnimal)
        {
            if (animalInventory == null)
            {
                lastGrantActionText = "AnimalInventory 없음";
                return;
            }
            ok = animalInventory.DebugSetAnimalLevel(id, level);
            lastGrantActionText = ok ? $"동물 레벨 설정: {id} -> Lv.{Mathf.Clamp(level, 1, 5)}" : $"동물 레벨 실패: {id}";
        }
        else
        {
            if (toolInventory == null)
            {
                lastGrantActionText = "ToolInventory 없음";
                return;
            }
            ok = toolInventory.DebugSetToolLevel(id, level);
            lastGrantActionText = ok ? $"도구 레벨 설정: {id} -> Lv.{Mathf.Clamp(level, 1, 5)}" : $"도구 레벨 실패: {id}";
        }
    }
}
/*#else
// 릴리즈 빌드에서는 컴포넌트만 남겨 둬 레퍼런스가 깨지지 않게 하지만 동작하지 않게 합니다.
public class DebugTool : MonoBehaviour { }
#endif*/
