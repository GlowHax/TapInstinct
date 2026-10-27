using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TapInstinct.UI
{
    public class ChatManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextPromptController promptController;
        [SerializeField] private KeyboardController keyboardController;
        [SerializeField] private Button sendButton;
        [SerializeField] private Image sendButtonImage;
        [SerializeField] private ScrollRect chatScrollRect;
        [SerializeField] private RectTransform chatContent;
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private Sprite bubbleSprite;

        [Header("Colors")]
        [SerializeField] private Color sendButtonActiveColor = new Color32(59, 114, 214, 255);   // #3B72D6 (Blue)
        [SerializeField] private Color sendButtonDisabledColor = new Color32(40, 44, 58, 255);  // #282C3A (Dim gray)
        [SerializeField] private Color bubbleColor = new Color32(59, 114, 214, 255);            // #3B72D6 (Messenger blue)
        [SerializeField] private Color bubbleTextColor = Color.white;

        [Header("Sentence Pool")]
        [TextArea(2, 3)]
        [SerializeField] private List<string> sentencePool = new List<string>
        {
            "Hallo, wie geht es dir.",
            "Bin gleich da.",
            "Hast du heute kurz Zeit.",
            "Treffen wir uns spaeter.",
            "Alles klar, danke dir.",
            "Klingt super, bis gleich.",
            "Wo bist du gerade.",
            "Schreib mir einfach nachher."
        };

        private int lastSentenceIndex = -1;

        private void Awake()
        {
            if (sendButton != null)
            {
                sendButton.onClick.AddListener(OnSendClicked);
            }
        }

        private void Start()
        {
            if (promptController != null)
            {
                promptController.OnProgressChanged += OnProgressChanged;
                SetRandomNextSentence();
            }
            UpdateSendButtonState();
        }

        private void OnDestroy()
        {
            if (promptController != null)
            {
                promptController.OnProgressChanged -= OnProgressChanged;
            }
        }

        private void OnProgressChanged()
        {
            UpdateSendButtonState();
        }

        public void UpdateSendButtonState()
        {
            if (promptController == null || sendButton == null) return;

            bool isCompleted = promptController.IsCompleted;
            sendButton.interactable = isCompleted;

            if (sendButtonImage != null)
            {
                sendButtonImage.color = isCompleted ? sendButtonActiveColor : sendButtonDisabledColor;
            }
        }

        public void OnSendClicked()
        {
            if (promptController == null || !promptController.IsCompleted) return;

            string sentText = promptController.TargetText;
            SpawnChatBubble(sentText);

            SetRandomNextSentence();
            UpdateSendButtonState();

            // Play reveal animation for the overall accuracy, which automatically fades the keys back in
            if (keyboardController != null)
            {
                keyboardController.PlayRevealSequence();
            }

            // Scroll to bottom after new message layout rebuilds
            Canvas.ForceUpdateCanvases();
            if (chatScrollRect != null)
            {
                chatScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private void SpawnChatBubble(string message)
        {
            if (chatContent == null) return;

            // Container row to align bubble to the right
            GameObject rowObj = new GameObject("ChatBubbleRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowObj.transform.SetParent(chatContent, false);

            var rowHlg = rowObj.GetComponent<HorizontalLayoutGroup>();
            rowHlg.childAlignment = TextAnchor.MiddleRight;
            rowHlg.childControlWidth = false;
            rowHlg.childControlHeight = true;
            rowHlg.childForceExpandWidth = false;
            rowHlg.childForceExpandHeight = false;

            // Message Bubble
            GameObject bubbleObj = new GameObject("Bubble", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            bubbleObj.transform.SetParent(rowObj.transform, false);

            var bubbleImg = bubbleObj.GetComponent<Image>();
            if (bubbleSprite != null)
            {
                bubbleImg.sprite = bubbleSprite;
                bubbleImg.type = Image.Type.Sliced;
                bubbleImg.pixelsPerUnitMultiplier = 1f;
            }
            bubbleImg.color = bubbleColor;

            var bubbleHlg = bubbleObj.GetComponent<HorizontalLayoutGroup>();
            bubbleHlg.padding = new RectOffset(35, 35, 22, 22);
            bubbleHlg.childControlWidth = true;
            bubbleHlg.childControlHeight = true;
            bubbleHlg.childForceExpandWidth = false;
            bubbleHlg.childForceExpandHeight = false;

            var bubbleFitter = bubbleObj.GetComponent<ContentSizeFitter>();
            bubbleFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            bubbleFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Message Text
            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            textObj.transform.SetParent(bubbleObj.transform, false);

            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            if (fontAsset != null) tmp.font = fontAsset;
            tmp.text = message;
            tmp.fontSize = 32;
            tmp.color = bubbleTextColor;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;

            var layoutElement = textObj.GetComponent<LayoutElement>();
            // Maximum width for chat bubble (e.g. 1000px on a 1440px screen width)
            layoutElement.preferredWidth = -1;
            layoutElement.flexibleWidth = -1;

            // Force layout update so scroll view recognizes the new child height
            LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent);
        }

        public void SetRandomNextSentence()
        {
            if (sentencePool == null || sentencePool.Count == 0 || promptController == null) return;

            int nextIndex;
            if (sentencePool.Count == 1)
            {
                nextIndex = 0;
            }
            else
            {
                do
                {
                    nextIndex = Random.Range(0, sentencePool.Count);
                } while (nextIndex == lastSentenceIndex);
            }

            lastSentenceIndex = nextIndex;
            promptController.SetTargetText(sentencePool[nextIndex]);
        }
    }
}
