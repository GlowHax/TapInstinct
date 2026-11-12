using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TapInstinct.Core;

namespace TapInstinct.UI
{
    public class LevelSelectionUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform chatListContent;
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private Sprite cardSprite;
        [SerializeField] private Button backButton;

        private void Start()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(() =>
                {
                    if (GameManager.Instance != null)
                        GameManager.Instance.SetState(GameState.MainMenu);
                });
            }

            PopulateChatLevels();
        }

        private void OnEnable()
        {
            PopulateChatLevels();
        }

        public void PopulateChatLevels()
        {
            if (chatListContent == null || LevelManager.Instance == null) return;

            // Clear previous items
            foreach (Transform child in chatListContent)
            {
                Destroy(child.gameObject);
            }

            var levels = LevelManager.Instance.Levels;
            if (levels == null) return;

            for (int i = 0; i < levels.Count; i++)
            {
                int index = i;
                LevelDataSO data = levels[i];
                if (data != null)
                {
                    CreateChatCard(data, index);
                }
            }
        }

        private void CreateChatCard(LevelDataSO data, int index)
        {
            bool isUnlocked = false;
            if (LevelManager.Instance != null)
            {
                isUnlocked = LevelManager.Instance.IsLevelUnlocked(index);
            }

            GameObject card = new GameObject($"ChatLevel_{index}_{data.contactName}", typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(chatListContent, false);

            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(0f, 160f);

            Image cardImg = card.GetComponent<Image>();
            if (cardSprite != null)
            {
                cardImg.sprite = cardSprite;
                cardImg.type = Image.Type.Sliced;
            }
            cardImg.color = new Color32(28, 33, 46, 255);

            Button btn = card.GetComponent<Button>();
            btn.interactable = isUnlocked;
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color32(45, 52, 70, 255);
            colors.pressedColor = new Color32(35, 40, 55, 255);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.4f);
            btn.colors = colors;

            btn.onClick.AddListener(() =>
            {
                LevelManager.Instance.SelectLevel(index);
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetState(GameState.PlayingLevel);
                }
            });

            // Avatar Circle
            GameObject avatar = new GameObject("Avatar", typeof(RectTransform), typeof(Image));
            avatar.transform.SetParent(cardRect, false);
            RectTransform avRect = avatar.GetComponent<RectTransform>();
            avRect.anchorMin = new Vector2(0f, 0.5f);
            avRect.anchorMax = new Vector2(0f, 0.5f);
            avRect.pivot = new Vector2(0f, 0.5f);
            avRect.sizeDelta = new Vector2(100f, 100f);
            avRect.anchoredPosition = new Vector2(25f, 0f);

            Image avImg = avatar.GetComponent<Image>();
            if (cardSprite != null)
            {
                avImg.sprite = cardSprite;
                avImg.type = Image.Type.Sliced;
            }
            avImg.color = data.avatarColor;

            GameObject letterObj = new GameObject("Letter", typeof(RectTransform), typeof(TextMeshProUGUI));
            letterObj.transform.SetParent(avRect, false);
            RectTransform letRect = letterObj.GetComponent<RectTransform>();
            letRect.anchorMin = Vector2.zero;
            letRect.anchorMax = Vector2.one;
            letRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI letText = letterObj.GetComponent<TextMeshProUGUI>();
            if (fontAsset != null) letText.font = fontAsset;
            letText.text = data.avatarInitial;
            letText.fontSize = 66;
            letText.fontStyle = FontStyles.Bold;
            letText.alignment = TextAlignmentOptions.Center;
            letText.color = Color.white;

            // Info container
            GameObject infoObj = new GameObject("Info", typeof(RectTransform));
            infoObj.transform.SetParent(cardRect, false);
            RectTransform infoRect = infoObj.GetComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0f, 0f);
            infoRect.anchorMax = new Vector2(1f, 1f);
            infoRect.offsetMin = new Vector2(145f, 15f);
            infoRect.offsetMax = new Vector2(-25f, -15f);

            // Name
            GameObject nameObj = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameObj.transform.SetParent(infoRect, false);
            RectTransform nRect = nameObj.GetComponent<RectTransform>();
            nRect.anchorMin = new Vector2(0f, 0.5f);
            nRect.anchorMax = new Vector2(1f, 1f);
            nRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI nText = nameObj.GetComponent<TextMeshProUGUI>();
            if (fontAsset != null) nText.font = fontAsset;
            nText.text = data.contactName + (isUnlocked ? "" : " (nicht freigeschaltet)");
            nText.fontSize = 49;
            nText.fontStyle = FontStyles.Bold;
            nText.alignment = TextAlignmentOptions.MidlineLeft;
            nText.color = Color.white;

            // Status
            GameObject subObj = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            subObj.transform.SetParent(infoRect, false);
            RectTransform sRect = subObj.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0f, 0f);
            sRect.anchorMax = new Vector2(1f, 0.5f);
            sRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI sText = subObj.GetComponent<TextMeshProUGUI>();
            if (fontAsset != null) sText.font = fontAsset;
            sText.text = data.statusOrBio;
            sText.fontSize = 34;
            sText.alignment = TextAlignmentOptions.MidlineLeft;
            sText.color = new Color32(150, 165, 190, 255);
        }
    }
}
