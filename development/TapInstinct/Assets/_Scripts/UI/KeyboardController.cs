using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

namespace TapInstinct.UI
{
    public class KeyboardController : MonoBehaviour, IPointerDownHandler
    {
        [Header("Prefab & Rows")]
        [SerializeField] private GameObject keyPrefab;
        [SerializeField] private RectTransform[] rows; // Row_0 to Row_4

        [Header("Gameplay Connection")]
        [SerializeField] private TextPromptController promptController;
        [SerializeField] private AccuracyTracker accuracyTracker;

        [Header("Colors")]
        [SerializeField] private Color normalKeyColor = new Color32(37, 40, 55, 255);
        [SerializeField] private Color specialKeyColor = new Color32(28, 30, 40, 255);
        [SerializeField] private Color textColor = Color.white;

        [Header("Appearance")]
        [Tooltip("Scale multiplier for 9-sliced corner roundness")]
        [SerializeField] private float keyCornerScale = 1.0f;

        [Header("Fade Settings")]
        [SerializeField] private CanvasGroup rowsCanvasGroup;
        [SerializeField] private float fadeDuration = 0.3f;
        private Coroutine activeFadeRoutine;
        private bool isHidden = false;

        [Header("Ambient Glow Background")]
        [SerializeField] private Image ambientGlowImage;
        [SerializeField] private Color highAccuracyColor = new Color(0.18f, 0.85f, 0.50f, 0.40f); // Vibrant Soft Emerald Green
        [SerializeField] private Color midAccuracyColor  = new Color(0.98f, 0.78f, 0.20f, 0.40f); // Warm Amber
        [SerializeField] private Color lowAccuracyColor  = new Color(0.95f, 0.32f, 0.25f, 0.40f); // Coral Red
        [SerializeField] private float glowLerpSpeed = 5.0f;
        private Color targetGlowColor;
        private Color currentGlowColor;
        private float ambientAlphaMultiplier = 0f; // Ambient glow stays 0 until keys fade out!

        [Header("End-of-Sentence Reveal UI")]
        [SerializeField] private CanvasGroup revealCanvasGroup;
        [SerializeField] private TextMeshProUGUI revealAccuracyText;
        private Coroutine activeRevealRoutine;

        // Key map for accuracy distance calculation
        private Dictionary<char, RectTransform> keyMap = new Dictionary<char, RectTransform>();
        private Canvas cachedCanvas;

        [System.Serializable]
        public struct KeyData
        {
            public string label;
            public float flexibleWidth;
            public bool isSpecial;
            public bool isSpacer;
        }

#if UNITY_EDITOR
        private void Reset()
        {
            AutoAssignReferences();
        }

        [ContextMenu("Auto Assign References")]
        public void AutoAssignReferences()
        {
            if (keyPrefab == null)
            {
                keyPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/KeyButton.prefab");
            }

            if (promptController == null)
            {
                promptController = Object.FindAnyObjectByType<TextPromptController>();
            }

            if (accuracyTracker == null)
            {
                accuracyTracker = GetComponent<AccuracyTracker>();
                if (accuracyTracker == null)
                {
                    accuracyTracker = gameObject.AddComponent<AccuracyTracker>();
                }
            }

            if (rows == null || rows.Length == 0)
            {
                var rowList = new List<RectTransform>();
                for (int i = 0; i < 5; i++)
                {
                    Transform t = transform.Find($"Row_{i}");
                    if (t != null && t.TryGetComponent<RectTransform>(out var rt))
                    {
                        rowList.Add(rt);
                    }
                }
                rows = rowList.ToArray();
            }
        }

        [UnityEditor.MenuItem("Tools/TapInstinct/Generate Keyboard")]
        public static void GenerateKeyboardFromMenu()
        {
            var kb = Object.FindAnyObjectByType<KeyboardController>();
            if (kb != null)
            {
                kb.GenerateKeyboard();
                UnityEditor.EditorUtility.SetDirty(kb.gameObject);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(kb.gameObject.scene);
            }
            else
            {
                Debug.LogWarning("[KeyboardController] No KeyboardController found in current scene.");
            }
        }

        [UnityEditor.MenuItem("Tools/TapInstinct/Clear Keyboard")]
        public static void ClearKeyboardFromMenu()
        {
            var kb = Object.FindAnyObjectByType<KeyboardController>();
            if (kb != null)
            {
                kb.ClearKeyboard();
                UnityEditor.EditorUtility.SetDirty(kb.gameObject);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(kb.gameObject.scene);
            }
        }
#endif

        private void Awake()
        {
            cachedCanvas = GetComponentInParent<Canvas>();
            if (accuracyTracker == null)
            {
                accuracyTracker = GetComponent<AccuracyTracker>();
                if (accuracyTracker == null)
                {
                    accuracyTracker = gameObject.AddComponent<AccuracyTracker>();
                }
            }

            // If rows are empty (keys haven't been generated yet), auto-generate them!
            bool needsGeneration = (rows == null || rows.Length == 0 || rows[0] == null || rows[0].childCount == 0);
            if (needsGeneration)
            {
                GenerateKeyboard();
            }
            else
            {
                BuildKeyMap();
                RebindKeyClickEvents();
            }

            targetGlowColor = highAccuracyColor;
            currentGlowColor = highAccuracyColor;
            ambientAlphaMultiplier = 0f; // Hidden initially while keys are visible!

            if (ambientGlowImage != null)
            {
                Color c = currentGlowColor;
                c.a = 0f;
                ambientGlowImage.color = c;
            }

            if (revealCanvasGroup != null)
            {
                revealCanvasGroup.alpha = 0f;
            }
        }

        private void Update()
        {
            // lerp for ambient background color
            currentGlowColor = Color.Lerp(currentGlowColor, targetGlowColor, Time.deltaTime * glowLerpSpeed);
            if (ambientGlowImage != null)
            {
                Color displayColor = currentGlowColor;
                displayColor.a *= ambientAlphaMultiplier;
                ambientGlowImage.color = displayColor;
            }
        }

        public void BuildKeyMap()
        {
            keyMap.Clear();
            if (rows == null) return;

            foreach (var row in rows)
            {
                if (row == null) continue;
                for (int i = 0; i < row.childCount; i++)
                {
                    var child = row.GetChild(i);
                    var tmp = child.GetComponentInChildren<TextMeshProUGUI>();
                    if (tmp != null && !string.IsNullOrEmpty(tmp.text) && child.TryGetComponent<RectTransform>(out var rt))
                    {
                        string label = tmp.text.Trim();
                        if (label.Equals("Space", System.StringComparison.OrdinalIgnoreCase))
                        {
                            keyMap[' '] = rt;
                        }
                        else if (label.Length == 1)
                        {
                            char lower = char.ToLowerInvariant(label[0]);
                            char upper = char.ToUpperInvariant(label[0]);
                            keyMap[lower] = rt;
                            keyMap[upper] = rt;
                        }
                    }
                }
            }
        }

        public void RebindKeyClickEvents()
        {
            if (rows == null) return;
            foreach (var row in rows)
            {
                if (row == null) continue;
                for (int i = 0; i < row.childCount; i++)
                {
                    var child = row.GetChild(i);
                    var btn = child.GetComponent<Button>();
                    var tmp = child.GetComponentInChildren<TextMeshProUGUI>();
                    if (btn != null && tmp != null && !string.IsNullOrEmpty(tmp.text))
                    {
                        string keyLabel = tmp.text.Trim();
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => OnKeyButtonClicked(child.GetComponent<RectTransform>(), keyLabel));
                    }
                }
            }
        }

        // =========================================================================
        // Keyboard Generation
        // =========================================================================
        [ContextMenu("Generate Keyboard")]
        public void GenerateKeyboard()
        {
#if UNITY_EDITOR
            if (keyPrefab == null || rows == null || rows.Length == 0 || promptController == null)
            {
                AutoAssignReferences();
            }
#endif

            if (keyPrefab == null)
            {
                Debug.LogError("[KeyboardController] KeyPrefab is not assigned in the Inspector!");
                return;
            }

            ClearKeyboard();

            var layout = GetDefaultQwertzLayout();

            for (int r = 0; r < rows.Length && r < layout.Count; r++)
            {
                Transform rowParent = rows[r];
                if (rowParent == null) continue;

                foreach (var key in layout[r])
                {
                    CreateKey(key, rowParent);
                }
            }

            BuildKeyMap();
            Debug.Log("[KeyboardController] Keyboard layout generated successfully!");
        }

        [ContextMenu("Clear Keyboard")]
        public void ClearKeyboard()
        {
            if (rows == null) return;

            foreach (var row in rows)
            {
                if (row == null) continue;
                for (int i = row.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(row.GetChild(i).gameObject);
                }
            }
            keyMap.Clear();
        }

        private void CreateKey(KeyData key, Transform parent)
        {
            if (key.isSpacer)
            {
                GameObject spacerObj = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
                spacerObj.transform.SetParent(parent, false);
                var spacerLayout = spacerObj.GetComponent<LayoutElement>();
                spacerLayout.flexibleWidth = key.flexibleWidth;
                return;
            }

            GameObject keyObj = Instantiate(keyPrefab, parent);
            keyObj.name = $"Key_{key.label}";

            // Ensure key has CanvasGroup for individual tap flash
            if (keyObj.GetComponent<CanvasGroup>() == null)
            {
                keyObj.AddComponent<CanvasGroup>();
            }

            // 1. Set key label text
            var tmp = keyObj.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = key.label;
                tmp.color = textColor;
            }

            // 2. Set keycap background color and corner scaling
            var img = keyObj.GetComponent<Image>();
            if (img != null)
            {
                img.color = key.isSpecial ? specialKeyColor : normalKeyColor;
                img.pixelsPerUnitMultiplier = keyCornerScale;
            }

            // 3. Set proportional width
            var layoutElement = keyObj.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                layoutElement.minWidth = 0;
                layoutElement.preferredWidth = 0;
                layoutElement.flexibleWidth = key.flexibleWidth;
            }

            // 4. Hook up button click event
            var btn = keyObj.GetComponent<Button>();
            if (btn != null)
            {
                string keyLabel = key.label;
                var keyRt = keyObj.GetComponent<RectTransform>();
                btn.onClick.AddListener(() => OnKeyButtonClicked(keyRt, keyLabel));
            }
        }

        // =========================================================================
        // Input Handling & Accuracy
        // =========================================================================
        public void OnPointerDown(PointerEventData eventData)
        {
            // Tapped on the keyboard area or background (missed keys or tapped between keys)
            HandleTapAtPosition(eventData.position, null, "Fallback");
        }

        private void OnKeyButtonClicked(RectTransform clickedKeyRt, string keyLabel)
        {
            // Ignore non-character function keys (Shift, Enter, etc.)
            if (keyLabel == "Shift" || keyLabel == "Del" || keyLabel == "!#1" || keyLabel == "Enter")
            {
                return;
            }

            // Flash the tapped key briefly if the keyboard is currently hidden
            if (isHidden && clickedKeyRt != null)
            {
                StartCoroutine(FlashKeyRoutine(clickedKeyRt.gameObject));
            }

            Vector2 exactScreenPos = Vector2.zero;

            if (Pointer.current != null)
            {
                exactScreenPos = Pointer.current.position.ReadValue();
            }

            // Pass the exact pixel coordinates of the touch AND the target key for accuracy calculation
            HandleTapAtPosition(exactScreenPos, clickedKeyRt, keyLabel);
        }

        //private void OnKeyButtonClicked(RectTransform clickedKeyRt, string keyLabel)
        //{
        //    // Ignore non-character function keys
        //    if (keyLabel == "Shift" || keyLabel == "Del" || keyLabel == "!#1" || keyLabel == "Enter")
        //    {
        //        return;
        //    }

        //    // Flash tapped key briefly if keys are hidden
        //    if (isHidden && clickedKeyRt != null)
        //    {
        //        StartCoroutine(FlashKeyRoutine(clickedKeyRt.gameObject));
        //    }

        //    Vector2 screenPos;
        //    Camera cam = (cachedCanvas != null && cachedCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? cachedCanvas.worldCamera : null;
        //    if (clickedKeyRt != null)
        //    {
        //        screenPos = RectTransformUtility.WorldToScreenPoint(cam, clickedKeyRt.position);
        //    }
        //    else
        //    {
        //        screenPos = Vector2.zero;
        //    }

        //    HandleTapAtPosition(screenPos, clickedKeyRt, keyLabel);
        //}


        private void HandleTapAtPosition(Vector2 screenPos, RectTransform tappedKeyRt, string sourceLabel)
        {
            if (sourceLabel == "Shift" || sourceLabel == "Del" || sourceLabel == "!#1" || sourceLabel == "Enter")
            {
                return;
            }

            // Fade out keys on first character tap
            if (!isHidden)
            {
                FadeOut();
            }

            // 1. Calculate accuracy based on target char
            if (promptController != null && !promptController.IsCompleted)
            {
                char targetChar = promptController.CurrentTargetChar;
                RectTransform targetKeyRect = null;
                if (keyMap.Count == 0) BuildKeyMap();

                keyMap.TryGetValue(targetChar, out targetKeyRect);

                Camera cam = (cachedCanvas != null && cachedCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? cachedCanvas.worldCamera : null;

                if (accuracyTracker != null)
                {
                    accuracyTracker.RegisterTap(screenPos, targetKeyRect, cam);
                    UpdateAmbientGlow(accuracyTracker.OverallScore);
                }

                promptController.AdvanceText();
            }
        }

        private System.Collections.IEnumerator FlashKeyRoutine(GameObject keyObj)
        {
            // Collect ALL Graphic components on this key (Image background + TMP label).
            // We animate color.a directly — Row CanvasGroups now stay at alpha=1 always,
            // so individual key graphics are always renderable by Unity.
            Graphic[] graphics = keyObj.GetComponentsInChildren<Graphic>(true);
            if (graphics.Length == 0) yield break;

            // Snap all key graphics to fully visible
            foreach (Graphic g in graphics)
            {
                Color c = g.color;
                c.a = 1f;
                g.color = c;
            }

            // Brief hold (~80ms)
            yield return new WaitForSeconds(0.08f);

            // Fast smooth fade back to invisible (~120ms)
            float t = 0f;
            float duration = 0.12f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float a = Mathf.Lerp(1f, 0f, t / duration);
                foreach (var g in graphics)
                {
                    if (g == null) continue;
                    Color c = g.color;
                    c.a = a;
                    g.color = c;
                }
                yield return null;
            }

            // Snap back to fully invisible
            foreach (var g in graphics)
            {
                if (g == null) continue;
                Color c = g.color;
                c.a = 0f;
                g.color = c;
            }
        }

        private void UpdateAmbientGlow(float score)
        {
            if (score >= 0.5f)
            {
                float t = Mathf.InverseLerp(0.5f, 1.0f, score);
                targetGlowColor = Color.Lerp(midAccuracyColor, highAccuracyColor, t);
            }
            else
            {
                float t = Mathf.InverseLerp(0, 0.5f, score);
                targetGlowColor = Color.Lerp(lowAccuracyColor, midAccuracyColor, t);
            }
        }

        // =========================================================================
        // Reveal & Fade Routines
        // =========================================================================
        public void FadeOut()
        {
            isHidden = true;
            StartFade(0f, 1f); // Keys go to 0, ambient glow fades up to 1
        }

        public void FadeIn()
        {
            isHidden = false;
            SetRowInteractivity(true); // Re-enable input as soon as keys start appearing
            StartFade(1f, 0f); // Keys go to 1, ambient glow fades back down to 0
        }

        /// <summary>
        /// Displays the overall accuracy percentage centered on the keyboard for a brief moment,
        /// then fades it out and fades the keys back in.
        /// </summary>
        public void PlayRevealSequence(System.Action onComplete = null)
        {
            if (activeRevealRoutine != null)
            {
                StopCoroutine(activeRevealRoutine);
            }
            activeRevealRoutine = StartCoroutine(RevealRoutine(onComplete));
        }

        private System.Collections.IEnumerator RevealRoutine(System.Action onComplete)
        {
            if (revealAccuracyText != null && accuracyTracker != null)
            {
                revealAccuracyText.text = $"{accuracyTracker.OverallAccuracyInt}%";
                // Match the ambient color but with full brightness/visibility
                Color textColor = currentGlowColor;
                textColor.a = 1.0f;
                revealAccuracyText.color = textColor;
            }

            // 1. Fade In Reveal Text
            if (revealCanvasGroup != null)
            {
                float t = 0f;
                float duration = 0.25f;
                while (t < duration)
                {
                    t += Time.deltaTime;
                    revealCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t / duration);
                    yield return null;
                }
                revealCanvasGroup.alpha = 1f;

                // 2. Hold display so user can clearly see it
                yield return new WaitForSeconds(0.9f);

                // 3. Fade Out Reveal Text
                t = 0f;
                while (t < duration)
                {
                    t += Time.deltaTime;
                    revealCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t / duration);
                    yield return null;
                }
                revealCanvasGroup.alpha = 0f;
            }

            // 4. Fade keys back in
            FadeIn();
            onComplete?.Invoke();
            activeRevealRoutine = null;
        }

        private void StartFade(float targetKeyAlpha, float targetGlowAlpha)
        {
            if (activeFadeRoutine != null)
            {
                StopCoroutine(activeFadeRoutine);
            }
            activeFadeRoutine = StartCoroutine(FadeRoutine(targetKeyAlpha, targetGlowAlpha));
        }

        private System.Collections.IEnumerator FadeRoutine(float targetKeyAlpha, float targetGlowAlpha)
        {
            // Collect all Graphic components (Image + TMP) across all rows.
            // We animate color.a directly — NOT via Row CanvasGroup alpha, which would prevent
            // individual key flash from working (parent CG alpha=0 blocks all child rendering).
            var graphics = GetAllKeyGraphics();
            float startKeyAlpha = graphics.Count > 0 ? graphics[0].color.a : (targetKeyAlpha > 0.5f ? 0f : 1f);
            float startGlowAlpha = ambientAlphaMultiplier;
            float elapsed = 0f;

            // Disable input immediately when fading out
            //if (targetKeyAlpha < 0.5f)
            //    SetRowInteractivity(false);

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float pct = elapsed / fadeDuration;
                float currentKeyAlpha = Mathf.Lerp(startKeyAlpha, targetKeyAlpha, pct);
                ambientAlphaMultiplier = Mathf.Lerp(startGlowAlpha, targetGlowAlpha, pct);

                foreach (var g in graphics)
                {
                    if (g == null) continue;
                    Color c = g.color;
                    c.a = currentKeyAlpha;
                    g.color = c;
                }
                yield return null;
            }

            // Snap to final values
            foreach (var g in graphics)
            {
                if (g == null) continue;
                Color c = g.color;
                c.a = targetKeyAlpha;
                g.color = c;
            }
            ambientAlphaMultiplier = targetGlowAlpha;
            activeFadeRoutine = null;
        }

        /// <summary>
        /// Returns all Graphic components (Image + TMP) across all key rows.
        /// Used for per-Graphic alpha fading — never touch Row CanvasGroup alpha.
        /// </summary>
        private List<Graphic> GetAllKeyGraphics()
        {
            var list = new List<Graphic>();
            if (rows == null) return list;
            foreach (var row in rows)
            {
                if (row == null) continue;
                var graphics = row.GetComponentsInChildren<Graphic>(true);
                foreach (var g in graphics)
                    list.Add(g);
            }
            return list;
        }

        /// <summary>
        /// Toggles interactable and blocksRaycasts on all Row CanvasGroups.
        /// Row CG alpha is ALWAYS kept at 1 so children remain renderable by Unity.
        /// </summary>
        private void SetRowInteractivity(bool interactable)
        {
            if (rows == null) return;
            foreach (var row in rows)
            {
                if (row == null) continue;
                var cg = row.GetComponent<CanvasGroup>();
                if (cg == null) cg = row.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 1f; // Never change — changing this blocks all child rendering!
                cg.interactable = interactable;
                cg.blocksRaycasts = interactable;
            }
        }

        // =========================================================================
        // Layout Definition (Samsung One UI QWERTZ)
        // =========================================================================
        private List<List<KeyData>> GetDefaultQwertzLayout()
        {
            return new List<List<KeyData>>
            {
                // Row 0: Number row
                new()
                {
                    new() { label = "1", flexibleWidth = 1f },
                    new() { label = "2", flexibleWidth = 1f },
                    new() { label = "3", flexibleWidth = 1f },
                    new() { label = "4", flexibleWidth = 1f },
                    new() { label = "5", flexibleWidth = 1f },
                    new() { label = "6", flexibleWidth = 1f },
                    new() { label = "7", flexibleWidth = 1f },
                    new() { label = "8", flexibleWidth = 1f },
                    new() { label = "9", flexibleWidth = 1f },
                    new() { label = "0", flexibleWidth = 1f },
                },
                // Row 1: Top letter row
                new()
                {
                    new() { label = "Q", flexibleWidth = 1f },
                    new() { label = "W", flexibleWidth = 1f },
                    new() { label = "E", flexibleWidth = 1f },
                    new() { label = "R", flexibleWidth = 1f },
                    new() { label = "T", flexibleWidth = 1f },
                    new() { label = "Z", flexibleWidth = 1f },
                    new() { label = "U", flexibleWidth = 1f },
                    new() { label = "I", flexibleWidth = 1f },
                    new() { label = "O", flexibleWidth = 1f },
                    new() { label = "P", flexibleWidth = 1f },
                    new() { label = "Ü", flexibleWidth = 1f },
                },
                // Row 2: Home letter row
                new()
                {
                    new() { label = "A", flexibleWidth = 1f },
                    new() { label = "S", flexibleWidth = 1f },
                    new() { label = "D", flexibleWidth = 1f },
                    new() { label = "F", flexibleWidth = 1f },
                    new() { label = "G", flexibleWidth = 1f },
                    new() { label = "H", flexibleWidth = 1f },
                    new() { label = "J", flexibleWidth = 1f },
                    new() { label = "K", flexibleWidth = 1f },
                    new() { label = "L", flexibleWidth = 1f },
                    new() { label = "Ö", flexibleWidth = 1f },
                    new() { label = "Ä", flexibleWidth = 1f },
                },
                // Row 3: Bottom letter row with Shift, Spacers, Del
                new()
                {
                    new() { label = "Shift", flexibleWidth = 1.2f, isSpecial = true },
                    new() { flexibleWidth = 0.8f, isSpacer = true },
                    new() { label = "Y", flexibleWidth = 1f },
                    new() { label = "X", flexibleWidth = 1f },
                    new() { label = "C", flexibleWidth = 1f },
                    new() { label = "V", flexibleWidth = 1f },
                    new() { label = "B", flexibleWidth = 1f },
                    new() { label = "N", flexibleWidth = 1f },
                    new() { label = "M", flexibleWidth = 1f },
                    new() { flexibleWidth = 0.8f, isSpacer = true },
                    new() { label = "Del", flexibleWidth = 1.2f, isSpecial = true },
                },
                // Row 4: Bottom utility row (Symbols, Comma, Space, Dot, Enter)
                new()
                {
                    new() { label = "!#1", flexibleWidth = 1.3f, isSpecial = true },
                    new() { label = ",", flexibleWidth = 1f },
                    new() { label = "Space", flexibleWidth = 6.4f },
                    new() { label = ".", flexibleWidth = 1f },
                    new() { label = "Enter", flexibleWidth = 1.3f, isSpecial = true },
                }
            };
        }
    }
}
