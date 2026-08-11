using UnityEngine;

namespace UI
{
    /// <summary>
    /// UI 패널이 활성화되어 있는 동안 항상 부모의 맨 첫 번째 자식(Sibling Index 0, 화면 맨 뒤)에
    /// 위치하도록 강제 보장하는 헬퍼 컴포넌트입니다.
    /// 외부 UI 프레임워크나 Base 클래스의 SetAsLastSibling() 호출을 완벽히 무력화합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class UI_KeepBehind : MonoBehaviour
    {
        private Transform cachedTransform;

        private void Awake()
        {
            cachedTransform = transform;
        }

        private void OnEnable()
        {
            // 활성화 즉시 인덱스를 0번(맨 뒤)으로 설정
            EnforceFirstSibling();
        }

        private void LateUpdate()
        {
            // 다른 스크립트나 코루틴에 의해 인덱스가 0번 밖으로 밀려났다면 최후의 시점에 강제 복구
            if (cachedTransform.GetSiblingIndex() != 0)
            {
                EnforceFirstSibling();
            }
        }

        /// <summary>
        /// Transform의 Sibling Index를 0으로 고정합니다.
        /// </summary>
        private void EnforceFirstSibling()
        {
            cachedTransform.SetAsFirstSibling();
        }
    }
}