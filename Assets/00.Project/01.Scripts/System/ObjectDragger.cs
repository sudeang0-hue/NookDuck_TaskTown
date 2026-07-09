//NB
using UnityEngine;

public class ObjectDragger : MonoBehaviour
{
    private Vector3 offset;
    private bool isDragging = false;

    void Update()
    {
        // 마우스 왼쪽 클릭 시
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Physics.Raycast로 직접 확인 (물리 엔진 강제 호출)
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform == this.transform)
                {
                    isDragging = true;
                    offset = gameObject.transform.position - Camera.main.ScreenToWorldPoint(
                        new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10)); // 10은 적절한 거리
                }
            }
        }

        // 드래그 중
        if (isDragging && Input.GetMouseButton(0))
        {
            Vector3 curPos = Camera.main.ScreenToWorldPoint(
                new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10)) + offset;
            transform.position = curPos;
        }
        else
        {
            isDragging = false;
        }
    }
}