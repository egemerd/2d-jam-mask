using UnityEngine;

/// <summary>
/// Detects black-colored pixels on screen in front of the player
/// and blocks movement in that direction.
/// Uses a rectangular area sampling for better collision detection.
/// </summary>
public class BlackColorDetector : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("How far ahead to check for black areas (in world units)")]
    [SerializeField] private float detectionDistance = 0.3f;

    [Tooltip("Width of the detection rectangle (should match player width + margin)")]
    [SerializeField] private float detectionWidth = 0.6f;

    [Tooltip("Height of the detection rectangle (should match player height + margin)")]
    [SerializeField] private float detectionHeight = 0.6f;

    [Tooltip("Number of sample points horizontally")]
    [SerializeField] private int horizontalSamples = 5;

    [Tooltip("Number of sample points vertically")]
    [SerializeField] private int verticalSamples = 5;

    [Tooltip("Threshold for what counts as 'black' (0-1, lower = stricter)")]
    [SerializeField] private float blackThreshold = 0.15f;

    [Tooltip("Percentage of samples that must be black to block movement (0-1)")]
    [SerializeField] private float blockingThreshold = 0.3f;

    [Header("References")]
    [SerializeField] private Camera mainCamera;

    [Header("Debug Settings")]
    [Tooltip("Show debug visualization")]
    [SerializeField] private bool showDebugGizmos = true;

    [Tooltip("Show all sample points")]
    [SerializeField] private bool showSamplePoints = true;

    [Tooltip("Show detection rectangle outline")]
    [SerializeField] private bool showRectangleOutline = true;

    [Tooltip("Show color of sampled pixels")]
    [SerializeField] private bool showPixelColors = true;

    // The texture we read pixels from (captured each frame)
    private Texture2D screenCapture;
    private bool isCapturing = false;

    // Debug info
    private Vector2 lastCheckedDirection = Vector2.zero;
    private bool lastDirectionBlocked = false;
    private int lastBlackHits = 0;
    private int lastTotalSamples = 0;

    // Store sample results for debug visualization
    private struct SampleDebugInfo
    {
        public Vector2 worldPos;
        public bool hitBlack;
        public Color sampledColor;
        public bool onScreen;
    }
    private SampleDebugInfo[] sampleDebugInfos;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        screenCapture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        int totalSamples = horizontalSamples * verticalSamples;
        sampleDebugInfos = new SampleDebugInfo[totalSamples];
    }

    private void OnDisable()
    {
        if (screenCapture != null)
        {
            Destroy(screenCapture);
            screenCapture = null;
        }
    }

    private void Start()
    {
        StartCoroutine(CaptureScreenRoutine());
    }

    private System.Collections.IEnumerator CaptureScreenRoutine()
    {
        while (true)
        {
            yield return new WaitForEndOfFrame();
            CaptureScreen();
        }
    }

    private void CaptureScreen()
    {
        if (screenCapture == null) return;

        if (screenCapture.width != Screen.width || screenCapture.height != Screen.height)
        {
            Destroy(screenCapture);
            screenCapture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        }

        screenCapture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
        screenCapture.Apply();
        isCapturing = true;
    }

    /// <summary>
    /// Checks if movement in the given direction is blocked by black pixels.
    /// Uses a rectangular area in front of the player.
    /// </summary>
    public bool IsDirectionBlocked(Vector2 moveDirection)
    {
        if (!isCapturing || moveDirection.sqrMagnitude < 0.01f)
            return false;

        moveDirection.Normalize();
        lastCheckedDirection = moveDirection;

        // Calculate the perpendicular direction (for width)
        Vector2 perpendicular = new Vector2(-moveDirection.y, moveDirection.x);

        int totalSamples = horizontalSamples * verticalSamples;
        int blackHits = 0;
        int validSamples = 0;
        int sampleIndex = 0;

        // Resize debug array if needed
        if (sampleDebugInfos == null || sampleDebugInfos.Length != totalSamples)
        {
            sampleDebugInfos = new SampleDebugInfo[totalSamples];
        }

        // Sample points in a grid pattern
        for (int y = 0; y < verticalSamples; y++)
        {
            for (int x = 0; x < horizontalSamples; x++)
            {
                // Calculate position in the rectangle
                // X goes from -width/2 to +width/2 (perpendicular to movement)
                float xOffset = ((float)x / (horizontalSamples - 1) - 0.5f) * detectionWidth;

                // Y goes from 0 to detectionDistance (along movement direction)
                float yOffset = ((float)y / (verticalSamples - 1)) * detectionDistance;

                // Calculate world position
                Vector2 samplePos = (Vector2)transform.position +
                                   perpendicular * xOffset +
                                   moveDirection * yOffset;

                // Store debug info
                sampleDebugInfos[sampleIndex].worldPos = samplePos;

                // Convert to screen position
                Vector3 screenPos = mainCamera.WorldToScreenPoint(samplePos);

                // Check if on screen
                if (screenPos.x < 0 || screenPos.x >= Screen.width ||
                    screenPos.y < 0 || screenPos.y >= Screen.height ||
                    screenPos.z < 0)
                {
                    sampleDebugInfos[sampleIndex].onScreen = false;
                    sampleDebugInfos[sampleIndex].hitBlack = false;
                    sampleDebugInfos[sampleIndex].sampledColor = Color.magenta;
                    sampleIndex++;
                    continue;
                }

                sampleDebugInfos[sampleIndex].onScreen = true;
                validSamples++;

                // Sample the pixel color
                Color pixelColor = screenCapture.GetPixel((int)screenPos.x, (int)screenPos.y);
                sampleDebugInfos[sampleIndex].sampledColor = pixelColor;

                // Check if it's black
                bool isBlack = IsColorBlack(pixelColor);
                sampleDebugInfos[sampleIndex].hitBlack = isBlack;

                if (isBlack)
                {
                    blackHits++;
                }

                sampleIndex++;
            }
        }

        lastBlackHits = blackHits;
        lastTotalSamples = validSamples;

        // Block if black percentage exceeds threshold
        if (validSamples == 0) return false;

        float blackPercentage = (float)blackHits / validSamples;
        lastDirectionBlocked = blackPercentage >= blockingThreshold;

        return lastDirectionBlocked;
    }

    /// <summary>
    /// Gets a modified velocity that avoids black areas.
    /// Use this to allow sliding along black walls.
    /// </summary>
    public Vector2 GetAllowedVelocity(Vector2 desiredVelocity)
    {
        if (desiredVelocity.sqrMagnitude < 0.01f)
            return desiredVelocity;

        Vector2 normalizedDir = desiredVelocity.normalized;

        // Check if straight ahead is blocked
        if (IsDirectionBlocked(normalizedDir))
        {
            // Try sliding along walls - check perpendicular directions
            Vector2 rightDir = new Vector2(normalizedDir.y, -normalizedDir.x);
            Vector2 leftDir = new Vector2(-normalizedDir.y, normalizedDir.x);

            // Project desired velocity onto perpendicular axes
            float rightDot = Vector2.Dot(desiredVelocity, rightDir);
            float leftDot = Vector2.Dot(desiredVelocity, leftDir);

            // Check which perpendicular direction is free
            if (!IsDirectionBlocked(rightDir) && rightDot > 0)
            {
                return rightDir * rightDot;
            }
            else if (!IsDirectionBlocked(leftDir) && leftDot > 0)
            {
                return leftDir * leftDot;
            }

            // Both blocked, stop movement
            return Vector2.zero;
        }

        return desiredVelocity;
    }

    private bool IsColorBlack(Color color)
    {
        return color.r < blackThreshold &&
               color.g < blackThreshold &&
               color.b < blackThreshold;
    }

    // Debug visualization
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || !Application.isPlaying) return;

        if (lastCheckedDirection.sqrMagnitude < 0.01f)
        {
            // Draw default rectangle around player when not moving
            DrawRectangle(Vector2.right, new Color(0.5f, 0.5f, 0.5f, 0.3f));
            return;
        }

        // Draw the detection rectangle
        if (showRectangleOutline)
        {
            Color rectColor = lastDirectionBlocked ? new Color(1, 0, 0, 0.5f) : new Color(0, 1, 0, 0.5f);
            DrawRectangle(lastCheckedDirection, rectColor);
        }

        // Draw sample points
        if (showSamplePoints && sampleDebugInfos != null)
        {
            foreach (var sample in sampleDebugInfos)
            {
                if (!sample.onScreen)
                {
                    // Off-screen samples (magenta)
                    Gizmos.color = new Color(1, 0, 1, 0.3f);
                    Gizmos.DrawWireSphere(sample.worldPos, 0.03f);
                    continue;
                }

                if (sample.hitBlack)
                {
                    // Black detected - solid red sphere
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(sample.worldPos, 0.04f);
                }
                else
                {
                    // Clear - green wireframe
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(sample.worldPos, 0.03f);
                }

                // Show sampled pixel color
                if (showPixelColors)
                {
                    Gizmos.color = sample.sampledColor;
                    Gizmos.DrawCube(sample.worldPos + Vector2.up * 0.08f, Vector3.one * 0.03f);
                }
            }
        }

        // Draw player center indicator
        Gizmos.color = lastDirectionBlocked ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.1f);

        // Draw direction arrow
        Vector2 arrowEnd = (Vector2)transform.position + lastCheckedDirection * (detectionDistance + 0.15f);
        Gizmos.color = lastDirectionBlocked ? Color.red : Color.cyan;
        Gizmos.DrawLine(transform.position, arrowEnd);

        // Arrow head
        Vector2 perpendicular = new Vector2(-lastCheckedDirection.y, lastCheckedDirection.x);
        Gizmos.DrawLine(arrowEnd, arrowEnd - lastCheckedDirection * 0.1f + perpendicular * 0.07f);
        Gizmos.DrawLine(arrowEnd, arrowEnd - lastCheckedDirection * 0.1f - perpendicular * 0.07f);
    }

    private void DrawRectangle(Vector2 direction, Color color)
    {
        Vector2 perpendicular = new Vector2(-direction.y, direction.x);

        // Calculate rectangle corners
        Vector2 center = (Vector2)transform.position + direction * (detectionDistance * 0.5f);

        Vector2 topLeft = center + direction * (detectionDistance * 0.5f) + perpendicular * (detectionWidth * 0.5f);
        Vector2 topRight = center + direction * (detectionDistance * 0.5f) - perpendicular * (detectionWidth * 0.5f);
        Vector2 bottomLeft = center - direction * (detectionDistance * 0.5f) + perpendicular * (detectionWidth * 0.5f);
        Vector2 bottomRight = center - direction * (detectionDistance * 0.5f) - perpendicular * (detectionWidth * 0.5f);

        Gizmos.color = color;
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);

        // Draw filled rectangle (semi-transparent)
        Gizmos.color = new Color(color.r, color.g, color.b, 0.1f);
        DrawQuad(topLeft, topRight, bottomRight, bottomLeft);
    }

    private void DrawQuad(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
    {
        // Draw two triangles to fill the quad
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p1);

        Gizmos.DrawLine(p1, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

#if UNITY_EDITOR
        Vector3 labelPos = transform.position + Vector3.up * 0.7f;

        float blackPercentage = lastTotalSamples > 0 ? (float)lastBlackHits / lastTotalSamples * 100f : 0f;

        UnityEditor.Handles.Label(labelPos,
            $"Black Detector Stats\n" +
            $"Direction: ({lastCheckedDirection.x:F2}, {lastCheckedDirection.y:F2})\n" +
            $"Black Samples: {lastBlackHits}/{lastTotalSamples} ({blackPercentage:F1}%)\n" +
            $"Threshold: {blockingThreshold * 100:F0}%\n" +
            $"Blocked: {lastDirectionBlocked}\n" +
            $"Rectangle: {detectionWidth:F2} × {detectionDistance:F2}");
#endif
    }
}