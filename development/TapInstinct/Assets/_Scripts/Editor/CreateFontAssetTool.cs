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

            // 5. Attach TextPromptController
            var controller = inputBarObj.AddComponent<TapInstinct.UI.TextPromptController>();
            var field = typeof(TapInstinct.UI.TextPromptController).GetField("promptText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(controller, inputTmp);
            }
            controller.UpdateDisplay();

            UnityEditor.EditorUtility.SetDirty(safeAreaObj);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(safeAreaObj.scene);
            Debug.Log("[SetupInputBar] Input bar UI created directly above keyboard and TextPromptController wired successfully!");
        }

        [MenuItem("Tools/TapInstinct/Setup Chat and Send Flow")]
        public static void SetupChatAndSendFlow()
        {
            GameObject safeAreaObj = GameObject.Find("SafeAreaContainer");
            if (safeAreaObj == null)
            {
                Debug.LogError("[SetupChatAndSendFlow] SafeAreaContainer not found!");
                return;
            }

            Transform safeArea = safeAreaObj.transform;
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Roboto-Medium SDF.asset");
            Sprite roundedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/KeyCap_Rounded.png");

            // 1. Setup Chat/Screen Background Panel
            Transform existingBg = safeArea.Find("ChatBackground");
            if (existingBg != null) Object.DestroyImmediate(existingBg.gameObject);

            GameObject bgObj = new GameObject("ChatBackground", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(safeArea, false);

            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            var bgImg = bgObj.GetComponent<Image>();
            bgImg.color = new Color32(11, 13, 17, 255); // #0B0D11 Dark elegant messenger background
            bgImg.raycastTarget = false;

            // 2. Remove existing ChatAreaContainer if present
            Transform existingChat = safeArea.Find("ChatAreaContainer");
            if (existingChat != null) Object.DestroyImmediate(existingChat.gameObject);

            // 3. Create ChatAreaContainer with ScrollRect
            GameObject chatAreaObj = new GameObject("ChatAreaContainer", typeof(RectTransform), typeof(ScrollRect), typeof(RectMask2D));
            chatAreaObj.transform.SetParent(safeArea, false);

            var chatAreaRect = chatAreaObj.GetComponent<RectTransform>();
            chatAreaRect.anchorMin = new Vector2(0f, 0f);
            chatAreaRect.anchorMax = new Vector2(1f, 1f);
            chatAreaRect.offsetMin = new Vector2(25f, 840f); // directly above input bar (which is at 700 + 130 = 830)
            chatAreaRect.offsetMax = new Vector2(-25f, -25f); // top margin

            // 4. Create Content container
            GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObj.transform.SetParent(chatAreaObj.transform, false);

            var contentRect = contentObj.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 0f);
            contentRect.anchorMax = new Vector2(1f, 0f);
            contentRect.pivot = new Vector2(0.5f, 0f); // aligns to bottom so newest messages appear at bottom
            contentRect.sizeDelta = new Vector2(0f, 0f);

            var vlg = contentObj.GetComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.LowerCenter;
            vlg.spacing = 16f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = contentObj.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = chatAreaObj.GetComponent<ScrollRect>();
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.inertia = true;

            // 5. Attach ChatManager to InputBarContainer
            Transform inputBar = safeArea.Find("InputBarContainer");
            if (inputBar == null)
            {
                Debug.LogError("[SetupChatAndSendFlow] InputBarContainer not found!");
                return;
            }

            var chatManager = inputBar.GetComponent<TapInstinct.UI.ChatManager>();
            if (chatManager == null)
            {
                chatManager = inputBar.gameObject.AddComponent<TapInstinct.UI.ChatManager>();
            }

            var promptController = inputBar.GetComponent<TapInstinct.UI.TextPromptController>();
            var keyboardController = safeArea.Find("KeyboardContainer")?.GetComponent<TapInstinct.UI.KeyboardController>();
            var sendBtn = inputBar.Find("SendButton")?.GetComponent<Button>();
            var sendImg = inputBar.Find("SendButton")?.GetComponent<Image>();

            var type = typeof(TapInstinct.UI.ChatManager);
            type.GetField("promptController", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(chatManager, promptController);
            type.GetField("keyboardController", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(chatManager, keyboardController);
            type.GetField("sendButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(chatManager, sendBtn);
            type.GetField("sendButtonImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(chatManager, sendImg);
            type.GetField("chatScrollRect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(chatManager, scrollRect);
            type.GetField("chatContent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(chatManager, contentRect);
            type.GetField("fontAsset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(chatManager, fontAsset);
            type.GetField("bubbleSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(chatManager, roundedSprite);

            // 6. Wire sendButton onClick persistent in editor or initialize
            if (sendBtn != null)
            {
                UnityEditor.Events.UnityEventTools.RemovePersistentListener(sendBtn.onClick, chatManager.OnSendClicked);
                UnityEditor.Events.UnityEventTools.AddPersistentListener(sendBtn.onClick, chatManager.OnSendClicked);
            }

            chatManager.UpdateSendButtonState();

            // Set hierarchy order: Background (0), ChatArea (1), InputBar, Keyboard
            bgObj.transform.SetSiblingIndex(0);
            chatAreaObj.transform.SetSiblingIndex(1);

            EditorUtility.SetDirty(safeAreaObj);
            EditorUtility.SetDirty(inputBar.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(safeAreaObj.scene);
            Debug.Log("[SetupChatAndSendFlow] Chat background, area and send flow with keyboard fade setup successfully!");
        }

        [MenuItem("Tools/TapInstinct/Setup Accuracy and Ambient Glow")]
        public static void SetupAccuracyAndAmbientGlow()
        {
            GameObject safeAreaObj = GameObject.Find("SafeAreaContainer");
            if (safeAreaObj == null)
            {
                Debug.LogError("[SetupAccuracyAndAmbientGlow] SafeAreaContainer not found!");
                return;
            }

            Transform kbTransform = safeAreaObj.transform.Find("KeyboardContainer");
            if (kbTransform == null)
            {
                Debug.LogError("[SetupAccuracyAndAmbientGlow] KeyboardContainer not found!");
                return;
            }

            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Roboto-Medium SDF.asset");

            // 1. AccuracyTracker component
            var accuracyTracker = kbTransform.GetComponent<TapInstinct.UI.AccuracyTracker>();
            if (accuracyTracker == null)
            {
                accuracyTracker = kbTransform.gameObject.AddComponent<TapInstinct.UI.AccuracyTracker>();
            }

            var kbController = kbTransform.GetComponent<TapInstinct.UI.KeyboardController>();

            // 2. Ambient Glow Image (Background under rows)
            Transform existingGlow = kbTransform.Find("AmbientGlowOverlay");
            GameObject glowObj;
            if (existingGlow != null)
            {
                glowObj = existingGlow.gameObject;
            }
            else
            {
                glowObj = new GameObject("AmbientGlowOverlay", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                glowObj.transform.SetParent(kbTransform, false);
            }

            var glowLayout = glowObj.GetComponent<LayoutElement>();
            if (glowLayout == null) glowLayout = glowObj.AddComponent<LayoutElement>();
            glowLayout.ignoreLayout = true;

            var glowRect = glowObj.GetComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = Vector2.zero;
            glowRect.offsetMax = Vector2.zero;

            var glowImg = glowObj.GetComponent<Image>();
            glowImg.color = new Color(0.18f, 0.85f, 0.50f, 0f); // 0 alpha in Editor!
            glowImg.raycastTarget = false;

            // Make sure AmbientGlowOverlay is sibling index 0 (behind rows and reveal text)
            glowObj.transform.SetSiblingIndex(0);

            // 3. Reveal Text (Centered overall accuracy display)
            Transform existingReveal = kbTransform.Find("RevealAccuracyContainer");
            GameObject revealObj;
            if (existingReveal != null)
            {
                revealObj = existingReveal.gameObject;
            }
            else
            {
                revealObj = new GameObject("RevealAccuracyContainer", typeof(RectTransform), typeof(CanvasGroup), typeof(LayoutElement));
                revealObj.transform.SetParent(kbTransform, false);
            }

            var revealLayout = revealObj.GetComponent<LayoutElement>();
            if (revealLayout == null) revealLayout = revealObj.AddComponent<LayoutElement>();
            revealLayout.ignoreLayout = true;

            var revealRect = revealObj.GetComponent<RectTransform>();
            revealRect.anchorMin = Vector2.zero;
            revealRect.anchorMax = Vector2.one;
            revealRect.offsetMin = Vector2.zero;
            revealRect.offsetMax = Vector2.zero;

            var revealCg = revealObj.GetComponent<CanvasGroup>();
            revealCg.alpha = 0f;
            revealCg.blocksRaycasts = false;
            revealCg.interactable = false;

            Transform existingText = revealObj.transform.Find("AccuracyNumber");
            GameObject textObj;
            if (existingText != null)
            {
                textObj = existingText.gameObject;
            }
            else
            {
                textObj = new GameObject("AccuracyNumber", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(revealObj.transform, false);
            }

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var revealTmp = textObj.GetComponent<TextMeshProUGUI>();
            if (fontAsset != null) revealTmp.font = fontAsset;
            revealTmp.text = "100%";
            revealTmp.fontSize = 130;
            revealTmp.fontStyle = FontStyles.Bold;
            revealTmp.alignment = TextAlignmentOptions.Center;
            revealTmp.color = Color.white;
            revealTmp.raycastTarget = false;

            // Make sure revealContainer is on top of rows
            revealObj.transform.SetAsLastSibling();

            // 4. Wire references to KeyboardController via reflection
            if (kbController != null)
            {
                var kbType = typeof(TapInstinct.UI.KeyboardController);
                kbType.GetField("accuracyTracker", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(kbController, accuracyTracker);
                kbType.GetField("ambientGlowImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(kbController, glowImg);
                kbType.GetField("revealCanvasGroup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(kbController, revealCg);
                kbType.GetField("revealAccuracyText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(kbController, revealTmp);
                kbController.BuildKeyMap();
                EditorUtility.SetDirty(kbController);
            }

            EditorUtility.SetDirty(kbTransform.gameObject);
            EditorUtility.SetDirty(safeAreaObj);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(safeAreaObj.scene);
            Debug.Log("[SetupAccuracyAndAmbientGlow] Accuracy tracker, ambient glow and reveal sequence wired successfully!");
        }
    }
}
#endif
