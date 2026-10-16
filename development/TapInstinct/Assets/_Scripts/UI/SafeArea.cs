using UnityEngine;

namespace TapInstinct.UI
{
    /// <summary>
    /// Adjusts RectTransform anchors to match the device's Screen.safeArea.
    /// Protects UI from punch holes, notches, and bottom gesture navigation bars.
    /// Updates automatically in the editor when changing devices in the Device Simulator.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public class SafeArea : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Rect _lastSafeArea = Rect.zero;
        private Vector2Int _lastScreenSize = Vector2Int.zero;
        private ScreenOrientation _lastOrientation = ScreenOrientation.AutoRotation;

        [Header("Constraint Settings")]
        [Tooltip("Constrain horizontally to safe area (left/right cutouts)")]
        [SerializeField] private bool conformX = true;

        [Tooltip("Constrain vertically to safe area (notch/punch hole at top, gesture bar at bottom)")]
        [SerializeField] private bool conformY = true;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void Update()
        {
            if (_lastSafeArea != Screen.safeArea ||
                _lastScreenSize.x != Screen.width ||
                _lastScreenSize.y != Screen.height ||
                _lastOrientation != Screen.orientation)
            {
                ApplySafeArea();
            }
        }

        public void ApplySafeArea()
        {
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }

            Rect safeArea = Screen.safeArea;
            Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);

            if (screenSize.x <= 0 || screenSize.y <= 0) return;

            _lastSafeArea = safeArea;
            _lastScreenSize = screenSize;
            _lastOrientation = Screen.orientation;

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= screenSize.x;
            anchorMin.y /= screenSize.y;
            anchorMax.x /= screenSize.x;
            anchorMax.y /= screenSize.y;

            if (!conformX)
            {
                anchorMin.x = 0;
                anchorMax.x = 1;
            }

            if (!conformY)
            {
                anchorMin.y = 0;
                anchorMax.y = 1;
            }

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
        }
    }
}
