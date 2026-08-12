using UnityEngine;

namespace UI
{
    /// <summary>
    /// VillageHouse 클릭 대상 표시용 마커입니다.
    /// 실제 클릭 처리는 VillageUI_Manager의 Input System + LayerMask Raycast가 담당합니다.
    /// (OnMouseDown / OnPointerClick 이중 호출로 패널이 깜빡이던 문제를 피하기 위해 클릭 로직은 두지 않습니다.)
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class VillageHouseClickable : MonoBehaviour
    {
        [SerializeField] private bool requireVillageHouseLayer = true;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!requireVillageHouseLayer)
                return;

            int layer = LayerMask.NameToLayer("VillageHouse");
            if (layer >= 0 && gameObject.layer != layer)
            {
                Debug.LogWarning(
                    "[VillageHouseClickable] '" + gameObject.name +
                    "' 레이어를 VillageHouse로 설정하는 것을 권장합니다.",
                    this);
            }
        }
#endif
    }
}
