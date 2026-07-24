using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// 패널 배경 클릭이 뒤 월드(VillageHouse 등)로 통과하지 않도록 막는 최소 컴포넌트입니다.
    /// InputGuard / VillageInfoUI_Manager가 Selectable·IPointerClickHandler를
    /// 인터랙티브 UI로 인식하는 점을 이용합니다.
    /// 같은 GameObject(또는 하위 Graphic)에 raycastTarget이 켜져 있어야 합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIClickBlocker : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("true면 Awake에서 Graphic.raycastTarget을 켭니다.")]
        [SerializeField] private bool forceRaycastTarget = true;

        private void Awake()
        {
            if (!forceRaycastTarget)
                return;

            if (TryGetComponent(out Graphic graphic))
                graphic.raycastTarget = true;
        }

        /// <summary>
        /// 클릭을 UI 쪽에서 소비하기 위한 빈 핸들러입니다.
        /// (실제 동작 없음 — 월드 Raycast 가드용)
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
        }
    }
}
