using UnityEngine;

public class AccuracyTracker : MonoBehaviour
{
    [Header("State")]
    [SerializeField] private int totalTaps = 0;
    [SerializeField] private float totalWeight = 0f;
    [SerializeField] private float totalAccuracySum = 0f;

    public int TotalTaps => totalTaps;

    // Lifetime average accuracy
    public float OverallAccuracy => totalTaps == 0 ? 0f : (totalAccuracySum / totalWeight) * 100f;
    public int OverallAccuracyInt => Mathf.Clamp(Mathf.RoundToInt(OverallAccuracy), 0, 100);

    // Normalized score 0.0 to 1.0 (for ambient color lerping)
    public float OverallScore => OverallAccuracy / 100f;


    /// <summary>
    /// Registers a tap and computes accuracy based on distance to the target key.
    /// Distance is normalized by the key's average dimension (width/height), making it resolution-independent.
    /// Accuracy falls off non-linearly (superlinear/quadratic curve).
    /// </summary>
    /// <param name="tapScreenPosition">Screen touch position</param>
    /// <param name="targetKeyRect">RectTransform of the expected key (null if not found/fallback)</param>
    public float RegisterTap(Vector2 tapScreenPosition, RectTransform targetKeyRect)
    {
        float tapScore = CalculateTapScore(tapScreenPosition, targetKeyRect);

        //misses are weighted more for added difficulty
        float weight = tapScore < 1.0f ? 1.3f : 1f;

        totalTaps++;
        totalWeight += weight;
        totalAccuracySum += tapScore;

        return tapScore;
    }

    private float CalculateTapScore(Vector2 tapScreenPos, RectTransform keyRect)
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
        Vector2 bl = RectTransformUtility.WorldToScreenPoint(Camera.main, worldCorners[0]);
        Vector2 tl = RectTransformUtility.WorldToScreenPoint(Camera.main, worldCorners[1]);
        Vector2 tr = RectTransformUtility.WorldToScreenPoint(Camera.main, worldCorners[2]);
        Vector2 br = RectTransformUtility.WorldToScreenPoint(Camera.main, worldCorners[3]);

        float minX = Mathf.Min(bl.x, tl.x);
        float maxX = Mathf.Max(tr.x, br.x);
        float minY = Mathf.Min(bl.y, br.y);
        float maxY = Mathf.Max(tl.y, tr.y);

        float keyWidth = Mathf.Max(1f, maxX - minX);
        float keyHeight = Mathf.Max(1f, maxY - minY);
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

        // Normalize distance: in units of key size
        // e.g. 0.5 = half a key width away, 1.0 = one key width away
        float normalizedDist = edgeDistancePx / keySize;

        // falloff curve:
        // Minimal penalty for being just slightly outside the border (up to 0.2 key size).
        // Drops off superlinearly / steeper as distance grows.
        // When normalizedDist >= 2.0 (more than 2 keys away), accuracy approaches 0.
        float maxDropDist = 1.1f;
        float t = Mathf.Clamp01(normalizedDist / maxDropDist);

        // Non-linear decay: (1 - t^1.75) so slight misses retain ~85-95%, while far misses plummet
        float score = 1.0f - Mathf.Pow(t, 1.75f);
        return Mathf.Clamp01(score);
    }

    public void ApplyTimerPenalty()
    {
        // Deduct 25% from overall accuracy
        totalAccuracySum -= 0.25f * totalTaps;
        totalAccuracySum = Mathf.Max(0f, totalAccuracySum);
    }

    public void ResetAll()
    {
        totalTaps = 0;
        totalAccuracySum = 0f;
        totalWeight = 0f;
    }
}
