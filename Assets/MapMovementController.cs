#nullable enable

using UnityEngine;
using UnityEngine.InputSystem;

public class MapMovementController : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Camera cam;
    private PlayerInput playerInput;
    private InputAction? mapMoveAction;
    private InputAction? mapClickAction;

    public GameObject? travelGraphControllerObject;
    private TravelGraphController? travelGraphController;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cam = GetComponent<Camera>();
        playerInput = GetComponent<PlayerInput>();

        if (playerInput != null)
        {
            mapMoveAction = playerInput.currentActionMap.FindAction("MapMove");
            mapClickAction = playerInput.currentActionMap.FindAction("MapClick");
        }

        if (travelGraphControllerObject != null)
        {
            travelGraphController = travelGraphControllerObject.GetComponent<TravelGraphController>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        HandleMapMovement();
        HandleMapClick();
    }

    void HandleMapMovement()
    {
        Vector2? moveInput = mapMoveAction?.ReadValue<Vector2>();

        if (moveInput == null)
        {
            return;
        }

        // Get camera forward and right directions
        Vector3 forward = cam.transform.forward;
        Vector3 right = cam.transform.right;

        // Project onto XZ plane and normalize
        forward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
        right = Vector3.ProjectOnPlane(right, Vector3.up).normalized;

        // Calculate movement direction
        Vector3 moveDirection = (right * moveInput.Value.x + forward * moveInput.Value.y);

        // Move the object
        transform.position += moveDirection * moveSpeed * Time.deltaTime;
    }

    void HandleMapClick()
    {
        bool? clickPressed = mapClickAction?.WasPressedThisFrame();

        if (clickPressed == null || !clickPressed.Value)
        {
            return;
        }

        // Get mouse position
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        // Create ray from camera through mouse position
        Ray ray = cam.ScreenPointToRay(mousePosition);

        // Perform raycast
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Object was clicked
            GameObject clickedObject = hit.collider.gameObject;

            while (clickedObject.transform.parent != null)
            {
                clickedObject = clickedObject.transform.parent.gameObject;
            }

            if (travelGraphController != null)
            {
                travelGraphController.SetCurrentNode(clickedObject);
            }
        }
    }
}
