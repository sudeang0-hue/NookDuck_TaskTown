using UnityEngine;
using UnityEngine.InputSystem;

public class WorldHoverOutlineDetector : MonoBehaviour
{
    [Header("Raycast")]
    private Camera targetCamera;
    [SerializeField] private LayerMask hoverTargetLayer;
    [SerializeField, Min(0f)] private float maxDistance = 1000f;

    private HoverOutlineTarget currentTarget;

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

        // 변경 전
        //if (EventSystem.current != null &&
        //    EventSystem.current.IsPointerOverGameObject())
        //{
        //    ChangeTarget(null);
        //    return;
        //}

        // 변경 후
        if (InputGuard.IsPointerOverUI())
        {
            ChangeTarget(null);
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
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
    }
}