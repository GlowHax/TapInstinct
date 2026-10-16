using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TapInstinct.UI
{
    public class KeyboardController : MonoBehaviour
    {
        [Header("Prefab & Rows")]
        [SerializeField] private GameObject keyPrefab;
        [SerializeField] private RectTransform[] rows; // Row_0 to Row_4

        [Header("Colors")]
        [SerializeField] private Color normalKeyColor = new Color32(37, 40, 55, 255);   // #252837 (letters & digits)
        [SerializeField] private Color specialKeyColor = new Color32(28, 30, 40, 255);  // #1C1E28 (Shift, Enter, etc.)
        [SerializeField] private Color textColor = Color.white;

        [Header("Appearance")]
        [Tooltip("Scale multiplier for 9-sliced corner roundness (1.0 = standard, higher = slightly less round, lower = rounder)")]
        [SerializeField] private float keyCornerScale = 1.0f;

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

        // =========================================================================
        // Keyboard Generation
        // =========================================================================
        [ContextMenu("Generate Keyboard")]
        public void GenerateKeyboard()
        {
#if UNITY_EDITOR
            if (keyPrefab == null || rows == null || rows.Length == 0)
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
                // Row 3: Bottom letter row with Shift, larger Spacers, and Backspace
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
                    new() { label = "Deutsch", flexibleWidth = 6.4f },
                    new() { label = ".", flexibleWidth = 1f },
                    new() { label = "Enter", flexibleWidth = 1.3f, isSpecial = true },
                }
            };
        }
    }
}
