#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TMPro;
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
    }
}
#endif
