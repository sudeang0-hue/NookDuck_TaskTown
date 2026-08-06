
using Manager;
using TaskTown.Gacha;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UIController_Gacha : MonoBehaviour
    {
        [Header("동물/도구 뽑기 가챠 매니저")]
        private AnimalGachaManager animalGachaManager;
        private ToolGachaManager toolGachaManager;

        [Tooltip("ICoinWallet을 구현한 컴포넌트(CoinManager)를 연결합니다. 비워두면 코인 확인 없이 뽑기를 진행합니다.")]
        private MonoBehaviour coinWalletSource;
        private ICoinWallet CoinWallet => coinWalletSource as ICoinWallet;

        // VillageSystemManager.Instance 또는 Find로 자동 탐색합니다.
        private VillageSystemManager villageSystem;
        private bool isVillageStateSubscribed;

        // -----------------------------------------------------------------------------
        // [ 2026.08.06 - KAY - Menu 오픈 경로와 Bridge 부수효과 동기화 ]
        // 기능: UIController_Menu가 뽑기 패널을 열/닫을 때 GachaSystemBridge가 수행하던
        //       Director 초기화·정리 호출을 이 컨트롤러에서 이어받습니다.
        //       참조는 FindObjectsInactive.Include 자동 탐색만 사용합니다.
        // -----------------------------------------------------------------------------
        private GachaDirector toolGachaDirector;
        private GachaPortalController animalGachaDirector;
        private GachaSystemBridge gachaSystemBridge;

        private const int MultiRollCount = 10;

        [Header("동물 뽑기 버튼")]
        [SerializeField] private Button animalOnePickButton;  // 동불 1회 뽑기
        [SerializeField] private Button animalTenPickButton;  // 동물 10회 뽑기
        [SerializeField] private TMP_Text animalOnePickCost;  // 동물 1회 뽑기 가격
        [SerializeField] private TMP_Text animalTenPickCost;  // 동물 10회 뽑기 가격

        [Header("도구 뽑기 버튼")]
        [SerializeField] private Button toolOnePickButton;    // 도구 1회 뽑기
        [SerializeField] private Button toolTenPickButton;    // 도구 10회 뽑기
        [SerializeField] private TMP_Text toolOnePickCost;    // 도구 1회 뽑기 가격
        [SerializeField] private TMP_Text toolTenPickCost;    // 도구 10회 뽑기 가격

        // -----------------------------------------------------------------------------
        // [ 2026.07.28 - Choi - 튜토리얼 뽑기 버튼 강조 연동 ]
        // 기능: Additive 튜토리얼 Scene이 동물·도구 1회 뽑기 버튼을 강조할 수 있게 읽기 전용으로 노출합니다.
        // -----------------------------------------------------------------------------
        public Button AnimalOnePickButton => animalOnePickButton;
        public Button ToolOnePickButton => toolOnePickButton;

        /// <summary>
        /// 도구/동물 가챠 연출 중이면 true. 메뉴 토글 가드에 사용합니다.
        /// </summary>
        public bool IsAnyDirectorAnimating
        {
            get
            {
                EnsureDirectorReferences();
                bool toolAnimating = toolGachaDirector != null && toolGachaDirector.IsAnimating;
                bool animalAnimating = animalGachaDirector != null && animalGachaDirector.IsAnimating;
                return toolAnimating || animalAnimating;
            }
        }

        private void Start()
        {
            if (animalGachaManager == null)
            {
                animalGachaManager = FindAnyObjectByType<AnimalGachaManager>();
            }

            if (toolGachaManager == null)
            {
                toolGachaManager = FindAnyObjectByType<ToolGachaManager>();
            }

            if (coinWalletSource == null)
            {
                coinWalletSource = FindAnyObjectByType<CoinManager>();
            }

            EnsureDirectorReferences();
            Subscribe();
            TrySubscribeVillageState();
            RefreshCostTexts();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            UnsubscribeVillageState();
        }

        /// <summary>
        /// 가챠 패널이 열릴 때 호출합니다.
        /// 가격 텍스트 갱신과 함께 Bridge OpenAllGachaWindows의 Director 준비 부수효과를 수행합니다.
        /// </summary>
        public void NotifyPanelOpened()
        {
            TrySubscribeVillageState();
            RefreshCostTexts();
            SyncDirectorsOnPanelOpened();
        }

        /// <summary>
        /// 가챠 패널이 닫힐 때 호출합니다.
        /// Bridge CloseAllGachaWindows와 동일하게 Director UI를 정리합니다.
        /// </summary>
        public void NotifyPanelClosed()
        {
            SyncDirectorsOnPanelClosed();
        }

        /// <summary>
        /// Menu 오픈 시 Bridge가 하던 toolGachaDirector.OpenGachaWindowOnly()를 호출합니다.
        /// </summary>
        private void SyncDirectorsOnPanelOpened()
        {
            EnsureDirectorReferences();

            if (toolGachaDirector != null)
                toolGachaDirector.OpenGachaWindowOnly();
        }

        /// <summary>
        /// Menu 클로즈 시 Bridge.CloseAllGachaWindows()로 초기창·Director를 함께 정리합니다.
        /// Bridge가 없으면 Director만 직접 닫습니다.
        /// </summary>
        private void SyncDirectorsOnPanelClosed()
        {
            EnsureDirectorReferences();

            if (gachaSystemBridge != null)
            {
                gachaSystemBridge.CloseAllGachaWindows();
                return;
            }

            if (toolGachaDirector != null)
                toolGachaDirector.CloseGachaUI();
            if (animalGachaDirector != null)
                animalGachaDirector.CloseGachaUI();
        }

        private void EnsureDirectorReferences()
        {
            if (toolGachaDirector == null)
                toolGachaDirector = FindAnyObjectByType<GachaDirector>(FindObjectsInactive.Include);

            if (animalGachaDirector == null)
                animalGachaDirector = FindAnyObjectByType<GachaPortalController>(FindObjectsInactive.Include);

            if (gachaSystemBridge == null)
                gachaSystemBridge = FindAnyObjectByType<GachaSystemBridge>(FindObjectsInactive.Include);
        }

        /// <summary>
        /// 마을 레벨이 바뀔 때(레벨업/디버그 설정 등) 뽑기 가격 텍스트를 즉시 갱신합니다.
        /// 패널이 닫혀있어도 구독되어 있으면 다음에 열 때 최신값이 보입니다.
        /// </summary>
        private void TrySubscribeVillageState()
        {
            if (isVillageStateSubscribed)
                return;

            if (villageSystem == null)
                villageSystem = VillageSystemManager.Instance != null
                    ? VillageSystemManager.Instance
                    : FindAnyObjectByType<VillageSystemManager>();

            if (villageSystem == null)
                return;

            villageSystem.OnVillageStateChanged -= OnVillageStateChanged;
            villageSystem.OnVillageStateChanged += OnVillageStateChanged;
            isVillageStateSubscribed = true;
        }

        private void UnsubscribeVillageState()
        {
            if (!isVillageStateSubscribed)
                return;

            if (villageSystem != null)
                villageSystem.OnVillageStateChanged -= OnVillageStateChanged;

            isVillageStateSubscribed = false;
        }

        private void OnVillageStateChanged()
        {
            RefreshCostTexts();
        }

        /// <summary>
        /// 동물/도구 1회·10회 뽑기 가격 텍스트를 매니저 현재 비용과 동기화합니다.
        /// </summary>
        private void RefreshCostTexts()
        {
            if (animalOnePickCost != null && animalGachaManager != null)
                animalOnePickCost.text = animalGachaManager.GetCost(1).ToString();

            if (animalTenPickCost != null && animalGachaManager != null)
                animalTenPickCost.text = animalGachaManager.GetCost(MultiRollCount).ToString();

            if (toolOnePickCost != null && toolGachaManager != null)
                toolOnePickCost.text = toolGachaManager.GetCost(1).ToString();

            if (toolTenPickCost != null && toolGachaManager != null)
                toolTenPickCost.text = toolGachaManager.GetCost(MultiRollCount).ToString();
        }

        /// <summary>
        /// 이벤트 구독
        /// </summary>
        private void Subscribe()
        {

            if (animalOnePickButton != null)
                animalOnePickButton.onClick.AddListener(OnClickPickUpAnimalOneTime);

            if (animalTenPickButton != null)
                animalTenPickButton.onClick.AddListener(OnClickPickUpAnimalTenTime);

            if (toolOnePickButton != null)
                toolOnePickButton.onClick.AddListener(OnClickPickUpToolOneTime);

            if (toolTenPickButton != null)
                toolTenPickButton.onClick.AddListener(OnClickPickUpToolTenTime);
        }

        /// <summary>
        /// 이벤트 구독 해제
        /// </summary>
        private void Unsubscribe()
        {
            if (animalOnePickButton != null)
                animalOnePickButton.onClick.RemoveListener(OnClickPickUpAnimalOneTime);

            if (animalTenPickButton != null)
                animalTenPickButton.onClick.RemoveListener(OnClickPickUpAnimalTenTime);

            if (toolOnePickButton != null)
                toolOnePickButton.onClick.RemoveListener(OnClickPickUpToolOneTime);

            if (toolTenPickButton != null)
                toolTenPickButton.onClick.RemoveListener(OnClickPickUpToolTenTime);
        }

        private void OnClickPickUpAnimalOneTime()
        {
            if (animalGachaManager == null) return;
            if (!TrySpendCost(animalGachaManager.GetCost(1))) return;

            animalGachaManager.Roll();

        }


        private void OnClickPickUpAnimalTenTime()
        {
            if (animalGachaManager == null) return;
            if (!TrySpendCost(animalGachaManager.GetCost(MultiRollCount))) return;

            animalGachaManager.RollMulti(MultiRollCount);
        }

        private void OnClickPickUpToolOneTime()
        {
            if (toolGachaManager == null) return;
            if (!TrySpendCost(toolGachaManager.GetCost(1))) return;

            toolGachaManager.Roll();
        }

        private void OnClickPickUpToolTenTime()
        {
            if (toolGachaManager == null) return;
            if (!TrySpendCost(toolGachaManager.GetCost(MultiRollCount))) return;

            toolGachaManager.RollMulti(MultiRollCount);
        }

        // 코인이 부족하면 뽑기를 진행하지 않습니다. coinWalletSource가 비어있으면 코인 확인 없이 진행합니다.
        private bool TrySpendCost(long cost)
        {
            if (CoinWallet == null)
            {
                Debug.LogWarning("[UIController_Gacha] ICoinWallet이 연결되지 않아 코인 확인 없이 뽑기를 진행합니다.");
                return true;
            }

            if (!CoinWallet.TrySpend(cost))
            {
                Debug.Log($"[UIController_Gacha] 코인이 부족합니다. (필요: {cost}, 보유: {CoinWallet.Balance})");
                return false;
            }

            return true;
        }
    }
}
