using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// 동물 인벤 페이지 상호작용 및 Tool Setting 패널 슬라이드 연출 제어
    /// </summary>
    public class AnimalInvPage_Manager : MonoBehaviour
    {
        [Header("해당 페이지 내 상호작용 버튼")]
        [SerializeField] private Button levelupButton;          // 레벨업 버튼
        [SerializeField] private Button toolSetButton;          // Set Tool - 도구 장착 목록
        [SerializeField] private Button toolChangeButton;       // 도구 변경 버튼 (동일 슬라이드 적용)
        [SerializeField] private Button toolSetOffButton;       // 도구 해제 버튼

        [Header("도구 목록 / 툴 세팅페이지")]
        [SerializeField] private GameObject toolSettingPage;
        [Tooltip("비어있으면 같은 GameObject에서 UIController_AnimalInvPage를 찾습니다.")]
        [SerializeField] private UIController_AnimalInvPage animalInvPage;

        [Header("Tool Setting 연출 설정")]
        [SerializeField] private float startPosX = 85f;          // 숨겨진 위치 X ($X_{start}$)
        [SerializeField] private float endPosX = 345f;           // 나타난 위치 X ($X_{end}$)
        [SerializeField] private float slideDuration = 0.35f;    // 이동 시간 $t$ (초)
        [SerializeField] private Ease slideEase = Ease.OutCubic; // 부드러운 감속 곡선

        private RectTransform toolSettingRect;

        private void Awake()
        {
            ResolveAnimalInvPage();
            CacheToolSettingRect();

            // 버튼 이벤트 바인딩
            if (levelupButton != null)
                levelupButton.onClick.AddListener(OnClickLevelUp);

            // [핵심] Set Tool 버튼과 Tool Change 버튼 모두 동일한 스르륵 연출 로직 연결
            if (toolSetButton != null)
                toolSetButton.onClick.AddListener(OpenToolSettingWithSlide);

            if (toolChangeButton != null)
                toolChangeButton.onClick.AddListener(OpenToolSettingWithSlide);
        }

        private void OnDisable()
        {
            CloseToolSettingPageImmediate();
        }

        private void OnDestroy()
        {
            if (levelupButton != null)
                levelupButton.onClick.RemoveListener(OnClickLevelUp);

            if (toolSetButton != null)
                toolSetButton.onClick.RemoveListener(OpenToolSettingWithSlide);

            if (toolChangeButton != null)
                toolChangeButton.onClick.RemoveListener(OpenToolSettingWithSlide);
        }

        private void ResolveAnimalInvPage()
        {
            if (animalInvPage == null)
                animalInvPage = GetComponent<UIController_AnimalInvPage>();
        }

        private void CacheToolSettingRect()
        {
            if (toolSettingPage != null)
                toolSettingRect = toolSettingPage.GetComponent<RectTransform>();

            toolSettingRect.SetAsFirstSibling();
        }

        private void OnClickLevelUp()
        {
            Debug.Log("[AnimalInvPage_Manager] 레벨업 시도");
        }

        /// <summary>
        /// 도구 세팅/변경 버튼 클릭 시 $X = 85 \to 345$ 좌표로 부드럽게 슬라이드합니다.
        /// </summary>
        private void OpenToolSettingWithSlide()
        {
            ResolveAnimalInvPage();

            if (toolSettingPage == null || animalInvPage == null) return;

            string animalId = animalInvPage.CurrentAnimalId;
            if (string.IsNullOrEmpty(animalId)) return;

            // 1. UI Controller 데이터 및 패널 오픈
            if (toolSettingPage.TryGetComponent(out UIController_ToolSetList toolSetList))
            {
                toolSetList.Open(animalId, animalInvPage.RefreshAnimalInvPage);
            }

            // 2. 스르륵 연출 로직
            if (toolSettingRect != null)
            {
                toolSettingRect.DOKill();

                // [핵심 해결책] Open() 내부에서 SetAsLastSibling()이 실행되었더라도
                // 연출 직전에 0번(맨 뒤)으로 위치를 다시 강제 재배치합니다.
                toolSettingRect.SetAsFirstSibling();

                SetRectPosX(startPosX); // X = 85px로 초기화
                toolSettingPage.SetActive(true);

                // X = 345px로 슬라이딩
                toolSettingRect.DOAnchorPosX(endPosX, slideDuration)
                    .SetEase(slideEase)
                    .SetUpdate(true);
            }
            else
            {
                toolSettingPage.SetActive(true);
            }
        }

        /// <summary>
        /// 패널을 역으로 스르륵 닫습니다 ($X = 345 \to 85$).
        /// </summary>
        public void CloseToolSettingPage()
        {
            if (toolSettingPage == null || !toolSettingPage.activeSelf) return;

            if (toolSettingRect != null)
            {
                toolSettingRect.DOKill();
                toolSettingRect.DOAnchorPosX(startPosX, slideDuration)
                    .SetEase(Ease.InCubic)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        if (toolSettingPage.TryGetComponent(out UIController_ToolSetList toolSetList))
                            toolSetList.Close();
                        else
                            toolSettingPage.SetActive(false);
                    });
            }
        }

        private void CloseToolSettingPageImmediate()
        {
            if (toolSettingPage == null) return;

            if (toolSettingRect != null)
            {
                toolSettingRect.DOKill();
                SetRectPosX(startPosX);
            }

            if (toolSettingPage.TryGetComponent(out UIController_ToolSetList toolSetList))
                toolSetList.Close();
            else
                toolSettingPage.SetActive(false);
        }

        private void SetRectPosX(float x)
        {
            Vector2 pos = toolSettingRect.anchoredPosition;
            pos.x = x;
            toolSettingRect.anchoredPosition = pos;
        }
    }
}