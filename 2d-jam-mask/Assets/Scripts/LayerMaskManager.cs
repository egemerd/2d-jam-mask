using UnityEngine;

public class LayerMaskManager : MonoBehaviour
{
    [Header("Layer References")]
    [Tooltip("The sprite that will be masked (usually your top layer)")]
    [SerializeField] private SpriteRenderer topLayer;

    [Tooltip("The bottom layer (always visible, no special setup needed)")]
    [SerializeField] private SpriteRenderer bottomLayer;

    [Header("Mask Settings")]
    [Tooltip("Resolution of the mask texture. Higher = more detailed but slower.")]
    [SerializeField] private int maskResolution = 2048;

    [Tooltip("Smoothness of brush edges (0-1). Higher = softer brush.")]
    [SerializeField] private float brushSoftness = 0.1f;

    private RenderTexture maskTexture;
    private Material topLayerMaterial;
    private Texture2D brushTexture;

    private void Start()
    {
        InitializeMask();
        CreateBrushTexture();
    }

    
    private void InitializeMask()
    {
        // Step 1: Create a RenderTexture (like a canvas you can paint on)
        maskTexture = new RenderTexture(maskResolution, maskResolution, 0, RenderTextureFormat.ARGB32);
        maskTexture.filterMode = FilterMode.Bilinear; // Smooth edges
        maskTexture.Create();

        // Step 2: Fill it with WHITE (top layer fully visible at start)
        RenderTexture.active = maskTexture;
        GL.Clear(true, true, Color.white);
        RenderTexture.active = null;

        // Step 3: Create a material using our custom shader
        topLayerMaterial = new Material(Shader.Find("Custom/MaskedSprite"));
        topLayerMaterial.SetTexture("_MainTex", topLayer.sprite.texture);
        topLayerMaterial.SetTexture("_MaskTex", maskTexture);

        // Step 4: Apply the material to the top layer
        topLayer.material = topLayerMaterial;

        Debug.Log("Mask initialized! Top layer is using masked material.");
    }

    
    private void CreateBrushTexture()
    {
        int size = 128;
        brushTexture = new Texture2D(size, size, TextureFormat.ARGB32, false);

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = 1f - Mathf.Clamp01((distance - radius * (1f - brushSoftness)) / (radius * brushSoftness));
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
        }

        brushTexture.SetPixels(pixels);
        brushTexture.Apply();
    }

     public void PaintOnMask(Vector2 worldPosition, bool revealBottom, float brushSize)
    {
        // Step 1: Convert world position to UV coordinates (0-1 range)
        Bounds bounds = topLayer.bounds;

        // Check if painting is within bounds
        if (!bounds.Contains(worldPosition))
            return;

        Vector2 uv = new Vector2(
            (worldPosition.x - bounds.min.x) / bounds.size.x,
            (worldPosition.y - bounds.min.y) / bounds.size.y
        );

        // Step 2: Choose brush color
        Color brushColor = revealBottom ? Color.black : Color.white;

        // Step 3: Paint on the mask
        PaintAtUV(uv, brushColor, brushSize, bounds.size.x);
    }

    
    private void PaintAtUV(Vector2 uv, Color color, float brushSize, float worldWidth)
    {
        // 1. Calculate brush size in pixels relative to the mask resolution
        float pixelBrushSize = (brushSize / worldWidth) * maskResolution;
        int brushRadius = Mathf.RoundToInt(pixelBrushSize / 2f);

        // 2. Calculate pixel position
        // We multiply by maskResolution to get the pixel index
        int pixelX = Mathf.RoundToInt(uv.x * maskResolution);

        // FIX: Flip the Y coordinate here. 
        // uv.y is 0 at bottom, but screen-space drawing is 0 at top.
        int pixelY = Mathf.RoundToInt((1.0f - uv.y) * maskResolution);

        // 3. Prepare the Render Texture
        RenderTexture temp = RenderTexture.GetTemporary(maskResolution, maskResolution, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(maskTexture, temp);

        RenderTexture.active = temp;

        // 4. Set up the drawing matrix
        GL.PushMatrix();
        // This loads a 2D matrix where 0,0 is Top-Left
        GL.LoadPixelMatrix(0, maskResolution, maskResolution, 0);

        // 5. Draw the brush
        // We create the material once or use a cached one for performance
        Material brushMat = new Material(Shader.Find("UI/Default"));
        brushMat.color = color;

        // Draw the texture centered on the calculated pixel coordinates
        Graphics.DrawTexture(
            new Rect(pixelX - brushRadius, pixelY - brushRadius, brushRadius * 2, brushRadius * 2),
            brushTexture,
            brushMat
        );

        GL.PopMatrix();

        // 6. Finalize
        Graphics.Blit(temp, maskTexture);
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(temp);

        Destroy(brushMat); // Important to prevent memory leaks
    }

    
    public void ResetMask()
    {
        RenderTexture.active = maskTexture;
        GL.Clear(true, true, Color.white);
        RenderTexture.active = null;
        Debug.Log("Mask reset to white!");
    }

    private void OnDestroy()
    {
        if (maskTexture != null)
            maskTexture.Release();

        if (topLayerMaterial != null)
            Destroy(topLayerMaterial);

        if (brushTexture != null)
            Destroy(brushTexture);
    }
}