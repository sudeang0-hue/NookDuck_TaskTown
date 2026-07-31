using UnityEngine;
using UnityEngine.InputSystem;

public class WorldHoverOutlineDetector : MonoBehaviour
{
    [Header("Raycast")]
    private Camera targetCamera;
    [SerializeField] private LayerMask hoverTargetLayer;
    [SerializeField, Min(0f)] private float maxDistance = 1000f;

    private HoverOutlineTarget currentTarget;
    private Vector2 lastMousePosition;
    private bool hasLastMousePosition;

    private void Awake()
    {
        targetCamera = Camera.main;
    }

    private void Update()
    {
        if (targetCamera == null || Mouse.current == null)
        {
            ChangeTarget(null);
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        // 마우스 좌표가 변하지 않으면 Raycast / UI 판정 생략
        if (hasLastMousePosition && mousePosition == lastMousePosition)
            return;

        hasLastMousePosition = true;
        lastMousePosition = mousePosition;

        if (InputGuard.IsPointerOverUI())
        {
            ChangeTarget(null);
            return;
        }

        Ray ray = targetCamera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                maxDistance,
                hoverTargetLayer,
                QueryTriggerInteraction.Ignore))
        {
            HoverOutlineTarget newTarget =
                hit.collider.GetComponentInParent<HoverOutlineTarget>();

            ChangeTarget(newTarget);
            return;
        }

        ChangeTarget(null);
    }

    private void ChangeTarget(HoverOutlineTarget newTarget)
    {
        if (currentTarget == newTarget)
            return;

        if (currentTarget != null)
            currentTarget.SetOutline(false);

        currentTarget = newTarget;

        if (currentTarget != null)
            currentTarget.SetOutline(true);
    }

    private void OnDisable()
    {
        ChangeTarget(null);
        hasLastMousePosition = false;
    }
}
