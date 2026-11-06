using System.Collections.Generic;
using UnityEngine;

namespace TapInstinct.UI
{
    public class AccuracyTracker : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private int totalTaps = 0;
        [SerializeField] private float totalAccuracySum = 0f;

        // Lifetime average accuracy: Starts at 100%
        public float OverallAccuracy => totalTaps == 0 ? 0f : (totalAccuracySum / totalTaps) * 100f;
        public int OverallAccuracyInt => Mathf.Clamp(Mathf.RoundToInt(OverallAccuracy), 0, 100);

        // Normalized score 0.0 to 1.0 (for ambient color lerping)
        public float OverallScore => OverallAccuracy / 100f;

        public event System.Action<float> OnAccuracyUpdated;

        /// <summary>
        /// Registers a tap and computes accuracy based on distance to the target key.
        /// Distance is normalized by the key's average dimension (width/height), making it resolution-independent.
        /// Accuracy falls off non-linearly (superlinear/quadratic curve).
        /// </summary>
        /// <param name="tapScreenPosition">Screen touch position</param>
        /// <param name="targetKeyRect">RectTransform of the expected key (null if not found/fallback)</param>
        /// <param name="canvasCamera">Camera used for canvas raycasts (null if Screen Space Overlay)</param>
        public float RegisterTap(Vector2 tapScreenPosition, RectTransform targetKeyRect, Camera canvasCamera = null)
        {
            float tapScore = CalculateTapScore(tapScreenPosition, targetKeyRect, canvasCamera);

            totalTaps++;
            totalAccuracySum += tapScore;

            OnAccuracyUpdated?.Invoke(OverallAccuracy);
            return tapScore;
        }

        private float CalculateTapScore(Vector2 tapScreenPos, RectTransform keyRect, Camera cam)
        {
            if (keyRect == null)
            {
                // Fallback: If no key rect found or unknown character, award neutral/default score
                return 1.0f;
            }

            // Get the 4 world corners of the target key in screen space
            Vector3[] worldCorners = new Vector3[4];
            keyRect.GetWorldCorners(worldCorners);

            // worldCorners: 0 = Bottom-Left, 1 = Top-Left, 2 = Top-Right, 3 = Bottom-Right
            Vector2 bl = RectTransformUtility.WorldToScreenPoint(cam, worldCorners[0]);
            Vector2 tl = RectTransformUtility.WorldToScreenPoint(cam, worldCorners[1]);
            Vector2 tr = RectTransformUtility.WorldToScreenPoint(cam, worldCorners[2]);
            Vector2 br = RectTransformUtility.WorldToScreenPoint(cam, worldCorners[3]);

            float minX = Mathf.Min(bl.x, tl.x);
            float maxX = Mathf.Max(tr.x, br.x);
            float minY = Mathf.Min(bl.y, br.y);
            float maxY = Mathf.Max(tl.y, tr.y);

            float keyWidth = Mathf.Max(1f, maxX - minX);
            float keyHeight = Mathf.Max(1f, maxY - minY);
            // Average dimension of a single key on this screen
            float keySize = (keyWidth + keyHeight) * 0.5f;

            // Check if touch is inside the key rect
            if (tapScreenPos.x >= minX && tapScreenPos.x <= maxX &&
                tapScreenPos.y >= minY && tapScreenPos.y <= maxY)
            {
                return 1.0f; // Direct hit inside key bounds = 100%
            }

            // Calculate shortest distance from tap to the edge of the rectangle
            float dx = 0f;
            if (tapScreenPos.x < minX) dx = minX - tapScreenPos.x;
            else if (tapScreenPos.x > maxX) dx = tapScreenPos.x - maxX;

            float dy = 0f;
            if (tapScreenPos.y < minY) dy = minY - tapScreenPos.y;
            else if (tapScreenPos.y > maxY) dy = tapScreenPos.y - maxY;

            float edgeDistancePx = Mathf.Sqrt(dx * dx + dy * dy);

            // Normalize distance: in units of key size (resolution-independent!)
            // e.g. 0.5 = half a key width away, 1.0 = one key width away
            float normalizedDist = edgeDistancePx / keySize;

            // Non-linear falloff curve:
            // Minimal penalty for being just slightly outside the border (up to 0.2 key size).
            // Drops off superlinearly / steeper as distance grows.
            // When normalizedDist >= 2.0 (more than 2 keys away), accuracy approaches 0.
            float maxDropDist = 2.0f;
            float t = Mathf.Clamp01(normalizedDist / maxDropDist);

            // Non-linear decay: (1 - t^1.5) so slight misses retain ~85-95%, while far misses plummet
            float score = 1.0f - Mathf.Pow(t, 1.5f);
            return Mathf.Clamp01(score);
        }

        public void ResetAll()
        {
            totalTaps = 0;
            totalAccuracySum = 0f;
            OnAccuracyUpdated?.Invoke(100f);
        }
    }
}
