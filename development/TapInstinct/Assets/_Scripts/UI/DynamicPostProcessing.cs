using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DynamicPostProcessing : MonoBehaviour
{
    [SerializeField] private Volume volume;
    [SerializeField] private AccuracyTracker accuracyTracker;
    private Vignette vignette;

    private void Start()
    {
        if (volume != null && volume.profile.TryGet(out vignette))
        {
            vignette.color.Override(new Color(0.95f, 0.2f, 0.2f));
            vignette.intensity.Override(0f);
        }
    }

    private void Update()
    {
        if (vignette == null || accuracyTracker == null) return;

        bool hasStartedTyping = accuracyTracker.gameObject.activeInHierarchy && accuracyTracker.TotalTaps > 0;
        float targetIntensity = hasStartedTyping
            ? Mathf.Clamp01(1f - accuracyTracker.OverallScore) * 0.55f : 0f;

        vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, targetIntensity, Time.deltaTime * 5f);
    }
}
