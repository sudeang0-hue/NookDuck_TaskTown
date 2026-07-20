using UnityEngine;
using UnityEngine.EventSystems;

public class AnimalTarget : MonoBehaviour
{
    void Update()
    {
        // 마우스 왼쪽 버튼 클릭 감지
        if (Input.GetMouseButtonDown(0))
        {
            // UI 클릭 중이면 동물 클릭 무시
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            // 카메라에서 마우스 클릭 위치로 레이저 발사
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                // [수정] 맞은 오브젝트가 나 자신(동물 부모)이거나 내 자식 오브젝트인지 확인
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    Debug.Log($"레이저가 맞은 오브젝트: {hit.collider.gameObject.name}");
                    // 만약 맞은 오브젝트가 나 자신(또는 내 자식)이라면?
                    if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    {
                        Debug.Log($"[진짜 내 동물 클릭 성공!]: {gameObject.name}");

                        GameMasterManager manager = FindAnyObjectByType<GameMasterManager>();
                        if (manager != null)
                        {
                            manager.FocusOnAnimal(transform);
                        }
                    }
                }
                else
                {
                    Debug.Log("레이저가 아무것도 맞추지 못했습니다!");
                }
            }
        }
    }
}