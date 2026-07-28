using UnityEngine;

namespace UI
{
    /// <summary>
    /// 이전 Coin_Slider Hover → LvUp_VillageInfo_Root 표시용 컴포넌트입니다.
    /// VillageInfo에서 호버 패널을 사용하지 않으므로 동작하지 않습니다.
    /// Prefab에서 해당 오브젝트/컴포넌트를 제거한 뒤 이 스크립트도 삭제하면 됩니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class VillageLvUpStateHover : MonoBehaviour
    {
        private void Awake()
        {
            enabled = false;
        }
    }
}
