using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls the brush that paints on the mask texture.
/// Uses Unity's New Input System for input handling.
/// </summary>
public class Brush : MonoBehaviour
{
    [Header("Brush Settings")]
    [Tooltip("Size of the brush in world units")]
    [SerializeField] private float brushSize = 0.1f;

    [Tooltip("Current brush mode: false = white (reveal top), true = black (reveal bottom)")]
    [SerializeField] private bool revealBottomLayer = false;

    [Header("References")]
    [Tooltip("Reference to the LayerMaskManager that handles painting")]
    [SerializeField] private LayerMaskManager maskManager;

    [Tooltip("Main camera for screen-to-world conversion")]
    [SerializeField] private Camera mainCamera;

    [Header("Visual Feedback")]
    [Tooltip("Sprite renderer for the brush visual")]
    [SerializeField] private SpriteRenderer brushSprite;

    [Tooltip("Sprite shown when using white brush (reveals top layer)")]
    [SerializeField] private Sprite whiteBrushSprite;

    [Tooltip("Sprite shown when using black brush (reveals bottom layer)")]
    [SerializeField] private Sprite blackBrushSprite;

    // Input System reference
    private PlayerControls controls;

    // State tracking
    private bool isPainting;
    private Vector2 currentMousePos;

    private void Awake()
    {
        // Initialize Input System controls
        controls = new PlayerControls();

        // Subscribe to brush switching actions
        controls.Gameplay.SwitchWhite.performed += OnSwitchWhite;
        controls.Gameplay.SwitchBlack.performed += OnSwitchBlack;

        // Subscribe to painting actions
        controls.Gameplay.Click.started += OnClickStarted;
        controls.Gameplay.Click.canceled += OnClickCanceled;

        // Auto-assign main camera if not set
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // Set initial brush visual
        //UpdateBrushVisual();
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
            Paint();
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

    /// <summary>
    /// Paints on the mask at the current brush position.
    /// </summary>
    private void Paint()
    {
        if (maskManager != null)
        {
            maskManager.PaintOnMask(transform.position, revealBottomLayer, brushSize);
        }
        
    }

    #region Input Callbacks

    /// <summary>
    /// Called when white brush key is pressed (Key: 1).
    /// </summary>
    private void OnSwitchWhite(InputAction.CallbackContext context)
    {
        SetBrushMode(false);
    }

    /// <summary>
    /// Called when black brush key is pressed (Key: 2).
    /// </summary>
    private void OnSwitchBlack(InputAction.CallbackContext context)
    {
        SetBrushMode(true);
    }

    /// <summary>
    /// Called when paint button is pressed down (Left Mouse Button).
    /// </summary>
    private void OnClickStarted(InputAction.CallbackContext context)
    {
        isPainting = true;
    }

    /// <summary>
    /// Called when paint button is released (Left Mouse Button).
    /// </summary>
    private void OnClickCanceled(InputAction.CallbackContext context)
    {
        isPainting = false;
    }

    #endregion

    /// <summary>
    /// Sets the brush mode (white or black).
    /// </summary>
    /// <param name="revealBottom">True = black brush (reveal bottom), False = white brush (reveal top)</param>
    private void SetBrushMode(bool revealBottom)
    {
        revealBottomLayer = revealBottom;
        UpdateBrushVisual();

        Debug.Log($"Brush mode: {(revealBottom ? "BLACK (reveal bottom)" : "WHITE (reveal top)")}");
    }

    /// <summary>
    /// Updates the brush sprite based on the current mode.
    /// </summary>
    private void UpdateBrushVisual()
    {
        if (brushSprite != null)
        {
            // Change sprite based on mode
            if (whiteBrushSprite != null && blackBrushSprite != null)
            {
                brushSprite.sprite = revealBottomLayer ? blackBrushSprite : whiteBrushSprite;
            }
            else
            {
                // Fallback: change color if sprites are not assigned
                brushSprite.color = revealBottomLayer ? Color.black : Color.white;
            }
        }
    }

    /// <summary>
    /// Public method to change brush size at runtime.
    /// </summary>
    public void SetBrushSize(float newSize)
    {
        brushSize = Mathf.Clamp(newSize, 0.01f, 1f);

        // Optionally update brush visual scale
        if (brushSprite != null)
        {
            transform.localScale = Vector3.one * (brushSize * 5f);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from all input events
        if (controls != null)
        {
            controls.Gameplay.SwitchWhite.performed -= OnSwitchWhite;
            controls.Gameplay.SwitchBlack.performed -= OnSwitchBlack;
            controls.Gameplay.Click.started -= OnClickStarted;
            controls.Gameplay.Click.canceled -= OnClickCanceled;

            controls.Dispose();
        }
    }
}