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
    [SerializeField] private float pushBackDistance = 0.1f; // Distance to push back when stuck

    [Header("Screen Boundaries")]
    [SerializeField] private bool clampToScreen = true;
    [SerializeField] private float boundaryPadding = 0.5f;

    private Rigidbody2D rb;
    private PlayerControls controls;
    private Vector2 moveInput;
    private Vector2 currentVelocity;
    private Camera mainCamera;
    private Vector3 lastValidPosition; // Store last position where player wasn't stuck

    // Screen boundaries in world space
    private float minX, maxX, minY, maxY;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controls = new PlayerControls();
        rb.gravityScale = 0f;
        mainCamera = Camera.main;

        // Auto-find detector if not assigned
        if (blackDetector == null)
            blackDetector = GetComponent<BlackColorDetector>();

        CalculateScreenBoundaries();
        lastValidPosition = transform.position;
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

        if (clampToScreen)
        {
            ClampToScreenBounds();
        }
    }

    private void ApplyMovement()
    {
        Vector2 targetVelocity = moveInput * moveSpeed;

        // Check for black obstacles and modify velocity
        if (blackDetector != null && targetVelocity.sqrMagnitude > 0.01f)
        {
            Vector2 allowedVelocity = blackDetector.GetAllowedVelocity(targetVelocity);

            // If completely stuck (no allowed velocity), try to push player back
            if (allowedVelocity.sqrMagnitude < 0.01f && targetVelocity.sqrMagnitude > 0.01f)
            {
                // Check if player is stuck inside a black area
                if (IsStuckInBlackArea())
                {
                    PushOutOfBlackArea();
                    targetVelocity = Vector2.zero;
                }
                else
                {
                    targetVelocity = Vector2.zero;
                }
            }
            else
            {
                targetVelocity = allowedVelocity;

                // Update last valid position if moving successfully
                if (targetVelocity.sqrMagnitude > 0.01f)
                {
                    lastValidPosition = transform.position;
                }
            }
        }

        float lerpSpeed = moveInput.magnitude > 0 ? acceleration : decceleration;
        currentVelocity = Vector2.Lerp(currentVelocity, targetVelocity, lerpSpeed * Time.fixedDeltaTime);
        rb.linearVelocity = currentVelocity;
    }

    /// <summary>
    /// Checks if the player is currently stuck inside a black area.
    /// </summary>
    private bool IsStuckInBlackArea()
    {
        if (blackDetector == null) return false;

        // Check all directions around the player
        Vector2[] directions = new Vector2[]
        {
            Vector2.up,
            Vector2.down,
            Vector2.left,
            Vector2.right,
            new Vector2(1, 1).normalized,   // Diagonal
            new Vector2(-1, 1).normalized,  // Diagonal
            new Vector2(1, -1).normalized,  // Diagonal
            new Vector2(-1, -1).normalized  // Diagonal
        };

        int blockedDirections = 0;
        foreach (Vector2 dir in directions)
        {
            if (blackDetector.IsDirectionBlocked(dir))
            {
                blockedDirections++;
            }
        }

        // If more than 75% of directions are blocked, player is likely stuck
        return blockedDirections >= directions.Length * 0.75f;
    }

    /// <summary>
    /// Pushes the player out of a black area towards the last valid position.
    /// </summary>
    private void PushOutOfBlackArea()
    {
        Vector2 pushDirection = ((Vector2)lastValidPosition - (Vector2)transform.position).normalized;

        // If last valid position is too close or invalid, push in opposite of movement input
        if (pushDirection.sqrMagnitude < 0.01f)
        {
            pushDirection = -moveInput.normalized;
        }

        // Apply small push
        transform.position += (Vector3)pushDirection * pushBackDistance;

        // Reset velocity to prevent sliding
        currentVelocity = Vector2.zero;
        rb.linearVelocity = Vector2.zero;

        Debug.Log("Player was stuck, pushed out of black area");
    }

    /// <summary>
    /// Calculates the screen boundaries in world space.
    /// </summary>
    private void CalculateScreenBoundaries()
    {
        if (mainCamera == null)
        {
            Debug.LogWarning("PlayerMovement: No main camera found!");
            return;
        }

        // Get screen boundaries in world coordinates
        Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, mainCamera.nearClipPlane));
        Vector3 topRight = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, mainCamera.nearClipPlane));

        minX = bottomLeft.x + boundaryPadding;
        maxX = topRight.x - boundaryPadding;
        minY = bottomLeft.y + boundaryPadding;
        maxY = topRight.y - boundaryPadding;
    }

    /// <summary>
    /// Clamps the player's position to stay within screen boundaries.
    /// </summary>
    private void ClampToScreenBounds()
    {
        Vector3 pos = transform.position;
        bool hitBoundary = false;

        // Clamp position
        float clampedX = Mathf.Clamp(pos.x, minX, maxX);
        float clampedY = Mathf.Clamp(pos.y, minY, maxY);

        // Check if we hit a boundary
        if (pos.x != clampedX || pos.y != clampedY)
        {
            hitBoundary = true;
        }

        pos.x = clampedX;
        pos.y = clampedY;
        transform.position = pos;

        // Stop velocity only in the direction that hit the boundary
        if (hitBoundary)
        {
            if (pos.x <= minX || pos.x >= maxX)
            {
                currentVelocity.x = 0;
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }

            if (pos.y <= minY || pos.y >= maxY)
            {
                currentVelocity.y = 0;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            }
        }
    }

    /// <summary>
    /// Recalculates boundaries if camera changes (e.g., aspect ratio change).
    /// Call this if screen resolution changes at runtime.
    /// </summary>
    public void RefreshBoundaries()
    {
        CalculateScreenBoundaries();
    }

    // Debug visualization
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || mainCamera == null) return;

        // Draw screen boundaries
        Gizmos.color = Color.cyan;

        // Bottom
        Gizmos.DrawLine(new Vector3(minX, minY, 0), new Vector3(maxX, minY, 0));
        // Top
        Gizmos.DrawLine(new Vector3(minX, maxY, 0), new Vector3(maxX, maxY, 0));
        // Left
        Gizmos.DrawLine(new Vector3(minX, minY, 0), new Vector3(minX, maxY, 0));
        // Right
        Gizmos.DrawLine(new Vector3(maxX, minY, 0), new Vector3(maxX, maxY, 0));

        // Draw last valid position
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(lastValidPosition, 0.15f);
    }
}