using UnityEngine;

/// <summary>
/// 마우스 호버 시 머리 위 돋보기 아이콘을 켜고 끄는 이벤트 컴포넌트
/// </summary>
[RequireComponent(typeof(Collider))]
public class AnimalWorldHover : MonoBehaviour
{
    [Header("UI Reference")]
    [Tooltip("동물 머리 위에 배치한 돋보기 아이콘 GameObject")]
    [SerializeField] private GameObject magnifierIconObject;

    private void Awake()
    {
        // 게임 시작 시 아이콘은 숨겨둠
        if (magnifierIconObject != null)
        {
            magnifierIconObject.SetActive(false);
        }
    }

    private void OnMouseEnter()
    {
        // 마우스가 오면 돋보기 아이콘 활성화
        if (magnifierIconObject != null)
        {
            magnifierIconObject.SetActive(true);
        }
    }

    private void OnMouseExit()
    {
        // 마우스가 나가면 돋보기 아이콘 비활성화
        if (magnifierIconObject != null)
        {
            magnifierIconObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        // 동물이 디스폰/비활성화될 때 아이콘도 안전하게 끎
        if (magnifierIconObject != null)
        {
            magnifierIconObject.SetActive(false);
        }
    }
}