using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float decceleration = 10f;

    [Header("Black Detection")]
    [SerializeField] private BlackColorDetector blackDetector;

    private Rigidbody2D rb;
    private PlayerControls controls;
    private Vector2 moveInput;
    private Vector2 currentVelocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controls = new PlayerControls();
        rb.gravityScale = 0f;

        // Auto-find detector if not assigned
        if (blackDetector == null)
            blackDetector = GetComponent<BlackColorDetector>();
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void Update()
    {
        moveInput = controls.Gameplay.PlayerMovement.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }

    private void ApplyMovement()
    {
        Vector2 targetVelocity = moveInput * moveSpeed;

        // Check for black obstacles and modify velocity
        if (blackDetector != null && targetVelocity.sqrMagnitude > 0.01f)
        {
            targetVelocity = blackDetector.GetAllowedVelocity(targetVelocity);
        }

        float lerpSpeed = moveInput.magnitude > 0 ? acceleration : decceleration;
        currentVelocity = Vector2.Lerp(currentVelocity, targetVelocity, lerpSpeed * Time.fixedDeltaTime);
        rb.linearVelocity = currentVelocity;
    }
}