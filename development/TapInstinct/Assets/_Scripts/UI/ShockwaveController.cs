using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class ShockwaveController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RawImage shockwaveImage;
    [SerializeField] private float shockwaveDuration = 0.45f;
    [SerializeField] private Material shockwaveMaterial;
    [SerializeField] private RectTransform keyboardRect;

    private Coroutine activeShockwaveRoutine;

    private static readonly int RingSpawnPosID = Shader.PropertyToID("_RingSpawnPosition");
    private static readonly int WaveDistanceID = Shader.PropertyToID("_WaveDistanceFromCenter");
    private static readonly int XSizeRatioID = Shader.PropertyToID("_XSizeRatio");

    private void Awake()
    {
        if (shockwaveImage != null)
        {
            shockwaveMaterial = new Material(shockwaveImage.material);
            shockwaveImage.material = shockwaveMaterial;
        }
    }

    private void Update()
    {
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            Vector2 screenPos = Pointer.current.position.ReadValue();

            if (IsPointerOverKeyboard(screenPos))
                return;

            TriggerShockwaveAt(screenPos);
        }
    }

    public void TriggerShockwaveAt(Vector2 screenPos)
    {
        if (shockwaveImage == null || shockwaveMaterial == null) return;

        Vector2 normalizedPos = new Vector2(
            screenPos.x / Screen.width,
            screenPos.y / Screen.height
        );

        shockwaveMaterial.SetVector(RingSpawnPosID, normalizedPos);
        shockwaveMaterial.SetFloat(XSizeRatioID, (float)Screen.width / Screen.height);

        if (activeShockwaveRoutine != null)
        {
            StopCoroutine(activeShockwaveRoutine);
        }
        activeShockwaveRoutine = StartCoroutine(ShockwaveRoutine());
    }

    private bool IsPointerOverKeyboard(Vector2 screenPos)
    {
        if (keyboardRect == null || !keyboardRect.gameObject.activeInHierarchy)
            return false;

        return RectTransformUtility.RectangleContainsScreenPoint(keyboardRect, screenPos, Camera.main);
    }

    private IEnumerator ShockwaveRoutine()
    {
        float elapsed = 0f;
        while (elapsed < shockwaveDuration)
        {
            elapsed += Time.deltaTime;
            float currentDist = Mathf.Lerp(-0.1f, 0.25f, elapsed / shockwaveDuration);
            shockwaveMaterial.SetFloat(WaveDistanceID, currentDist);
            yield return null;
        }

        shockwaveMaterial.SetFloat(WaveDistanceID, -0.1f);
        activeShockwaveRoutine = null;
    }
}