#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.IO;

namespace TapInstinct.Editor
{
    public static class CreateFontAssetTool
    {
        [MenuItem("Tools/TapInstinct/Create Roboto Font Asset")]
        public static void CreateRobotoFontAsset()
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Roboto-Medium.ttf");
            if (font == null)
            {
                Debug.LogError("[TapInstinct] Could not find Assets/Fonts/Roboto-Medium.ttf");
                return;
            }

            // Delete old asset if exists
            AssetDatabase.DeleteAsset("Assets/Fonts/Roboto-Medium SDF.asset");

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
            if (fontAsset != null)
            {
                fontAsset.name = "Roboto-Medium SDF";
                AssetDatabase.CreateAsset(fontAsset, "Assets/Fonts/Roboto-Medium SDF.asset");

                // Save atlas texture as sub-asset
                if (fontAsset.atlasTextures != null)
                {
                    for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
                    {
                        var tex = fontAsset.atlasTextures[i];
                        if (tex != null)
                        {
                            tex.name = $"{fontAsset.name} Atlas {i}";
                            AssetDatabase.AddObjectToAsset(tex, fontAsset);
                            EditorUtility.SetDirty(tex);
                        }
                    }
                }
                else if (fontAsset.atlasTexture != null)
                {
                    fontAsset.atlasTexture.name = $"{fontAsset.name} Atlas";
                    AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
                    EditorUtility.SetDirty(fontAsset.atlasTexture);
                }

                // Save material as sub-asset
                if (fontAsset.material != null)
                {
                    fontAsset.material.name = $"{fontAsset.name} Material";
                    AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                    EditorUtility.SetDirty(fontAsset.material);
                }

                EditorUtility.SetDirty(fontAsset);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[TapInstinct] Successfully created and saved Assets/Fonts/Roboto-Medium SDF.asset with atlas texture and material!");
            }
            else
            {
                Debug.LogError("[TapInstinct] Failed to create TMP_FontAsset.");
            }
        }

        [MenuItem("Tools/TapInstinct/Regenerate KeyCap Sprite")]
        public static void GenerateKeyCapSprite()
        {
            int size = 128;
            float radius = 26f; // Balanced, moderate rounded corners matching Samsung One UI
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                    float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));

                    float alpha = Mathf.Clamp01(radius + 0.5f - dist);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();

            byte[] bytes = tex.EncodeToPNG();
            string path = "Assets/Sprites/KeyCap_Rounded.png";
            File.WriteAllBytes(path, bytes);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = new Vector4(28, 28, 28, 28);
                importer.filterMode = FilterMode.Bilinear;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            Debug.Log("[TapInstinct] KeyCap_Rounded.png regenerated with balanced corners (radius " + radius + ")!");
        }

        [MenuItem("Tools/TapInstinct/Setup Input Bar")]
        public static void SetupInputBar()
        {
            GameObject safeAreaObj = GameObject.Find("SafeAreaContainer");
            if (safeAreaObj == null)
            {
                Debug.LogError("[SetupInputBar] SafeAreaContainer not found in scene!");
                return;
            }

            Transform safeArea = safeAreaObj.transform;

            // Load assets
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Roboto-Medium SDF.asset");
            Sprite roundedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/KeyCap_Rounded.png");

            // Remove existing InputBarContainer if present
            Transform existingBar = safeArea.Find("InputBarContainer");
            if (existingBar != null) Object.DestroyImmediate(existingBar.gameObject);

            // 1. InputBarContainer (positioned directly above KeyboardContainer: height 130, anchored bottom at y=700)
            GameObject inputBarObj = new GameObject("InputBarContainer", typeof(RectTransform), typeof(Image));
            inputBarObj.transform.SetParent(safeArea, false);

            var inputBarRect = inputBarObj.GetComponent<RectTransform>();
            inputBarRect.anchorMin = new Vector2(0f, 0f);
            inputBarRect.anchorMax = new Vector2(1f, 0f);
            inputBarRect.pivot = new Vector2(0.5f, 0f);
            inputBarRect.anchoredPosition = new Vector2(0f, 700f);
            inputBarRect.sizeDelta = new Vector2(0f, 130f);

            var inputBarImg = inputBarObj.GetComponent<Image>();
            inputBarImg.color = new Color32(18, 20, 26, 255); // #12141A (matches keyboard tone)

            // 2. Rounded Input Field Pill
            GameObject inputPillObj = new GameObject("InputPill", typeof(RectTransform), typeof(Image));
            inputPillObj.transform.SetParent(inputBarObj.transform, false);

            var pillRect = inputPillObj.GetComponent<RectTransform>();
            pillRect.anchorMin = new Vector2(0f, 0.5f);
            pillRect.anchorMax = new Vector2(1f, 0.5f);
            pillRect.pivot = new Vector2(0.5f, 0.5f);
            pillRect.anchoredPosition = new Vector2(-45f, 0f);
            pillRect.sizeDelta = new Vector2(-150f, 85f); // Leaves space on right for send button

            var pillImg = inputPillObj.GetComponent<Image>();
            if (roundedSprite != null)
            {
                pillImg.sprite = roundedSprite;
                pillImg.type = Image.Type.Sliced;
                pillImg.pixelsPerUnitMultiplier = 1f;
            }
            pillImg.color = new Color32(32, 36, 48, 255); // #202430

            // 3. TextMeshPro Text inside Pill
            GameObject inputTextObj = new GameObject("InputPromptText", typeof(RectTransform), typeof(TextMeshProUGUI));
            inputTextObj.transform.SetParent(inputPillObj.transform, false);

            var inputTextRect = inputTextObj.GetComponent<RectTransform>();
            inputTextRect.anchorMin = Vector2.zero;
            inputTextRect.anchorMax = Vector2.one;
            inputTextRect.offsetMin = new Vector2(25f, 0f);
            inputTextRect.offsetMax = new Vector2(-25f, 0f);

            var inputTmp = inputTextObj.GetComponent<TextMeshProUGUI>();
            if (fontAsset != null) inputTmp.font = fontAsset;
            inputTmp.text = "<color=#FFFFFF>Hallo, </color><color=#7DA8FF><u>w</u></color><color=#555866>ie geht's?</color>";
            inputTmp.fontSize = 32;
            inputTmp.alignment = TextAlignmentOptions.MidlineLeft;
            inputTmp.textWrappingMode = TextWrappingModes.NoWrap;
            inputTmp.overflowMode = TextOverflowModes.Ellipsis;
            inputTmp.raycastTarget = false;

            // 4. Send Button
            GameObject sendBtnObj = new GameObject("SendButton", typeof(RectTransform), typeof(Image), typeof(Button));
            sendBtnObj.transform.SetParent(inputBarObj.transform, false);

            var sendRect = sendBtnObj.GetComponent<RectTransform>();
            sendRect.anchorMin = new Vector2(1f, 0.5f);
            sendRect.anchorMax = new Vector2(1f, 0.5f);
            sendRect.pivot = new Vector2(1f, 0.5f);
            sendRect.anchoredPosition = new Vector2(-25f, 0f);
            sendRect.sizeDelta = new Vector2(80f, 80f);

            var sendImg = sendBtnObj.GetComponent<Image>();
            if (roundedSprite != null)
            {
                sendImg.sprite = roundedSprite;
                sendImg.type = Image.Type.Sliced;
                sendImg.pixelsPerUnitMultiplier = 1f;
            }
            sendImg.color = new Color32(59, 114, 214, 255); // Messenger Blue #3B72D6

            GameObject sendIconObj = new GameObject("SendIcon", typeof(RectTransform), typeof(TextMeshProUGUI));
            sendIconObj.transform.SetParent(sendBtnObj.transform, false);

            var sendIconRect = sendIconObj.GetComponent<RectTransform>();
            sendIconRect.anchorMin = Vector2.zero;
            sendIconRect.anchorMax = Vector2.one;
            sendIconRect.sizeDelta = Vector2.zero;

            var sendTmp = sendIconObj.GetComponent<TextMeshProUGUI>();
            if (fontAsset != null) sendTmp.font = fontAsset;
            sendTmp.text = ">";
            sendTmp.fontSize = 40;
            sendTmp.fontStyle = FontStyles.Bold;
            sendTmp.alignment = TextAlignmentOptions.Center;
            sendTmp.color = Color.white;
            sendTmp.raycastTarget = false;

            // Ensure KeyboardContainer stays below or appropriately in hierarchy
            Transform kb = safeArea.Find("KeyboardContainer");
            if (kb != null)
            {
                kb.SetAsLastSibling();
            }

            UnityEditor.EditorUtility.SetDirty(safeAreaObj);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(safeAreaObj.scene);
            Debug.Log("[SetupInputBar] Input bar UI created directly above keyboard successfully!");
        }
    }
}
#endif
