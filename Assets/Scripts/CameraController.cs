using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Header("Movement Settings")]
    [SerializeField] private float normalSpeed = 20f;
    [SerializeField] private float fastSpeed = 100f;
    [SerializeField] private float movementSpeed = 20f;
    [SerializeField] private float mouseSensitivity = 5f;
    [SerializeField] private float speedChangeRate = 10f;

    [Header("Starting Position")]
    [SerializeField] private Vector3 startPosition = new Vector3(0, 100, -100);
    [SerializeField] private Vector3 startRotation = new Vector3(30, 0, 0);

    private float rotationX = 0f;
    private float rotationY = 0f;
    private Vector3 currentVelocity;
    private bool cursorLocked = true;

    void Start()
    {
        // Set initial position and rotation
        transform.position = startPosition;
        transform.eulerAngles = startRotation;

        // Initialize rotation variables
        rotationX = transform.eulerAngles.y;
        rotationY = -transform.eulerAngles.x;
        
        // Lock cursor to game window
        UpdateCursorLock();
    }

    void Update()
    {
        HandleMouseLock();
        if (cursorLocked)
        {
            HandleMovement();
            HandleRotation();
        }
    }

    private void HandleMovement()
    {
        // Get input axes
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        float up = (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f);

        // Check if fast mode is requested
        bool fastMode = Input.GetKey(KeyCode.LeftShift);
        float targetSpeed = fastMode ? fastSpeed : normalSpeed;
        movementSpeed = Mathf.Lerp(movementSpeed, targetSpeed, Time.deltaTime * speedChangeRate);

        // Calculate movement vector
        Vector3 moveDirection = new Vector3(horizontal, up, vertical).normalized;
        Vector3 targetVelocity = transform.TransformDirection(moveDirection) * movementSpeed;

        // Smooth movement
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, Time.deltaTime * 15f);

        // Apply movement
        transform.position += currentVelocity * Time.deltaTime;
    }

    private void HandleRotation()
    {
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Calculate new rotation
        rotationX += mouseX;
        rotationY = Mathf.Clamp(rotationY + mouseY, -89f, 89f);

        // Apply rotation
        transform.rotation = Quaternion.Euler(-rotationY, rotationX, 0);
    }

    private void HandleMouseLock()
    {
        // Toggle cursor lock with Escape key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            cursorLocked = !cursorLocked;
            UpdateCursorLock();
        }
    }

    private void UpdateCursorLock()
    {
        Cursor.lockState = cursorLocked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !cursorLocked;
    }

    void OnValidate()
    {
        // Ensure speeds are positive
        normalSpeed = Mathf.Max(0.1f, normalSpeed);
        fastSpeed = Mathf.Max(normalSpeed, fastSpeed);
        mouseSensitivity = Mathf.Max(0.1f, mouseSensitivity);
    }
}
