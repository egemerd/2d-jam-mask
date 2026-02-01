using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 10f; // How fast you reach max speed
    [SerializeField] private float decceleration = 10f; // How fast you stop

    private Rigidbody2D rb;
    private PlayerControls controls;
    private Vector2 moveInput;
    private Vector2 currentVelocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controls = new PlayerControls();

        // Disable gravity for top-down
        rb.gravityScale = 0f;
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void Update()
    {
        // Read input from the generated C# class
        moveInput = controls.Gameplay.PlayerMovement.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }

    private void ApplyMovement()
    {
        // Determine target velocity
        Vector2 targetVelocity = moveInput * moveSpeed;

        // Calculate the difference between current and target velocity
        float lerpSpeed = moveInput.magnitude > 0 ? acceleration : decceleration;

        // Smoothly transition velocity
        currentVelocity = Vector2.Lerp(currentVelocity, targetVelocity, lerpSpeed * Time.fixedDeltaTime);

        // Apply to Rigidbody
        rb.linearVelocity = currentVelocity;
    }
}