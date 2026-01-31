using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Controls the brush that paints on the mask texture.
/// Uses Unity's New Input System for input handling.
/// </summary>
public class Brush : MonoBehaviour
{
    [Header("Brush Settings")]
    [SerializeField] private float brushSize = 0.1f;


    [SerializeField] private bool revealBottomLayer = false;

    [Header("References")]
    [SerializeField] private LayerMaskManager maskManager;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Image selectedColour;
    [SerializeField] private Image otherColour;

    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer brushSprite;

    
    [SerializeField] private Sprite whiteBrushSprite;

    
    [SerializeField] private Sprite blackBrushSprite;

    // Input System reference
    private PlayerControls controls;

    // State tracking
    private bool isPainting;
    private Vector2 currentMousePos;
    private Vector2 previousMousePos;
    private bool hasPreviousMousePos;

    private void Awake()
    {
        // Initialize Input System controls
        controls = new PlayerControls();

        // Subscribe to brush switching actions
        controls.Gameplay.SwitchWhite.performed += OnSwitchWhite;

        // Subscribe to painting actions
        controls.Gameplay.Click.started += OnClickStarted;
        controls.Gameplay.Click.canceled += OnClickCanceled;

        // Auto-assign main camera if not set
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // Set initial brush visual
        
    }

    private void OnEnable()
    {
        // Enable input when script becomes active
        controls.Enable();
    }

    private void OnDisable()
    {
        // Disable input when script becomes inactive
        controls.Disable();
    }

    private void Update()
    {
        UpdateBrushPosition();

        if (isPainting)
        {
            PaintWithInterpolation();
        }
    }

    /// <summary>
    /// Updates the brush position to follow the mouse cursor.
    /// </summary>
    private void UpdateBrushPosition()
    {
        // Read mouse position from Input System
        currentMousePos = controls.Gameplay.Move.ReadValue<Vector2>();

        // Convert screen space to world space
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(
            new Vector3(currentMousePos.x, currentMousePos.y, mainCamera.nearClipPlane + 10f)
        );
        worldPos.z = 0f;

        // Move brush to mouse position
        transform.position = worldPos;
    }

    
    private void PaintWithInterpolation()
    {
        if (maskManager != null)
        {
            if (hasPreviousMousePos)
            {
                // Interpolate between previous and current positions
                Vector3 previousWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(previousMousePos.x, previousMousePos.y, 10f));
                previousWorldPos.z = 0f;

                Vector3 currentWorldPos = transform.position;

                float distance = Vector3.Distance(previousWorldPos, currentWorldPos);
                int steps = Mathf.CeilToInt(distance / (brushSize * 0.5f)); // Adjust step size based on brush size

                for (int i = 0; i <= steps; i++)
                {
                    Vector3 interpolatedPos = Vector3.Lerp(previousWorldPos, currentWorldPos, i / (float)steps);
                    maskManager.PaintOnMask(interpolatedPos, revealBottomLayer, brushSize);
                }
            }
            else
            {
                // Paint at the current position if no previous position exists
                maskManager.PaintOnMask(transform.position, revealBottomLayer, brushSize);
            }

            previousMousePos = currentMousePos;
            hasPreviousMousePos = true;
        }
        else
        {
            Debug.LogWarning("Brush: MaskManager is not assigned!");
        }
    }

    #region Input Callbacks

    
    private void OnSwitchWhite(InputAction.CallbackContext context)
    {
        revealBottomLayer = !revealBottomLayer;
        selectedColour.color = revealBottomLayer ? Color.black : Color.white;
        otherColour.color = revealBottomLayer ? Color.white : Color.black;
    }


    
    private void OnClickStarted(InputAction.CallbackContext context)
    {
        isPainting = true;
    }

   
    private void OnClickCanceled(InputAction.CallbackContext context)
    {
        isPainting = false;
        hasPreviousMousePos = false; // Reset interpolation state
    }

    #endregion

     private void SetBrushMode(bool revealBottom)
    {
        revealBottomLayer = revealBottom;
        //UpdateBrushVisual();

        Debug.Log($"Brush mode: {(revealBottom ? "BLACK (reveal bottom)" : "WHITE (reveal top)")}");
    }

    
    

    private void OnDestroy()
    {
        // Unsubscribe from all input events
        if (controls != null)
        {
            controls.Gameplay.SwitchWhite.performed -= OnSwitchWhite;
            controls.Gameplay.Click.started -= OnClickStarted;
            controls.Gameplay.Click.canceled -= OnClickCanceled;

            controls.Dispose();
        }
    }
}