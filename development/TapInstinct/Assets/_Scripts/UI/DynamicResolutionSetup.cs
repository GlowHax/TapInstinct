using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class DynamicResolutionSetup : MonoBehaviour
{
    [Header("Camera & UI References")]
    [Tooltip("The camera that renders main UI (e.g. Main Camera)")]
    public Camera uiCamera;

    [Tooltip("The RawImage component inside the KeyboardWaveMask")]
    public RawImage targetRawImage;

    private RenderTexture dynamicRT;
    private int lastWidth;
    private int lastHeight;

    private void OnEnable()
    {
        SetupRenderTexture();
    }

    private void Update()
    {
        // Re-create & re-align if resolution or window size changes in Editor / Game mode
        if (Screen.width != lastWidth || Screen.height != lastHeight || dynamicRT == null || !dynamicRT.IsCreated())
        {
            SetupRenderTexture();
        }

        // Keep the RawImage aligned with the full screen at all times
        AlignRawImageToScreen();
    }

    private void SetupRenderTexture()
    {
        int width = Mathf.Max(1, Screen.width);
        int height = Mathf.Max(1, Screen.height);

        if (dynamicRT != null && dynamicRT.width == width && dynamicRT.height == height)
        {
            AlignRawImageToScreen();
            return;
        }

        Cleanup();

        lastWidth = width;
        lastHeight = height;

        // 1. Create resolution-matching Render Texture
        dynamicRT = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32);
        dynamicRT.Create();

        if (uiCamera != null)
        {
            uiCamera.targetTexture = dynamicRT;
        }

        if (targetRawImage != null)
        {
            targetRawImage.texture = dynamicRT;
        }

        AlignRawImageToScreen();
    }

    private void AlignRawImageToScreen()
    {
        if (targetRawImage == null) return;

        Canvas rootCanvas = targetRawImage.canvas;
        if (rootCanvas == null) return;

        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        RectTransform rawRect = targetRawImage.rectTransform;

        if (canvasRect != null && rawRect != null)
        {
            // 1. Dynamically match full canvas resolution
            rawRect.sizeDelta = canvasRect.sizeDelta;

            // 2. Lock world position to screen center (counteracts parent mask offset automatically)
            rawRect.position = canvasRect.position;
        }
    }

    private void Cleanup()
    {
        if (uiCamera != null && uiCamera.targetTexture == dynamicRT)
        {
            uiCamera.targetTexture = null;
        }

        if (dynamicRT != null)
        {
            dynamicRT.Release();

            if (Application.isPlaying)
            {
                Destroy(dynamicRT);
            }
            else
            {
                DestroyImmediate(dynamicRT);
            }

            dynamicRT = null;
        }
    }

    private void OnDisable()
    {
        Cleanup();
    }

    private void OnDestroy()
    {
        Cleanup();
    }
}