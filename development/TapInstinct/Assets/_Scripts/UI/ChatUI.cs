using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TapInstinct.Core;

namespace TapInstinct.UI
{
    public class ChatUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextPromptController promptController;
        [SerializeField] private KeyboardController keyboardController;
        [SerializeField] private AccuracyTracker accuracyTracker;
        [SerializeField] private Button backButton;
        [SerializeField] private Button sendButton;
        [SerializeField] private Image sendButtonImage;
        [SerializeField] private ScrollRect chatScrollRect;
        [SerializeField] private RectTransform chatContent;
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private Sprite bubbleSprite;

        [Header("Colors")]
        [SerializeField] private Color sendButtonActiveColor = new Color32(59, 114, 214, 255);   // Blue
        [SerializeField] private Color sendButtonDisabledColor = new Color32(40, 44, 58, 255);  // Dim gray
        [SerializeField] private Color bubbleColor = new Color32(59, 114, 214, 255);            // Blue
        [SerializeField] private Color bubbleTextColor = Color.white;
        [SerializeField] private Color npcBubbleColor = new Color32(38, 44, 60, 255);           // Dark slate messenger grey
        [SerializeField] private Color npcBubbleTextColor = Color.white;

        [Header("Completion UI")]
        [SerializeField] private GameObject levelCompletedOverlay;
        [SerializeField] private TMP_Text levelCompletedTitle;
        [SerializeField] private TMP_Text levelCompletedInfoTxt;
        [SerializeField] private Button backToLevelsButton;

        [Header("Sentence Pool (Training Mode)")]
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
        private int currentStepIndex = 0;
        private Coroutine activeDialogueRoutine;
        private Coroutine activeTimerRoutine;
        private bool isTimeLimitExceeded = false;
        private GameObject currentTypingIndicator;

        public bool IsLevelMode
        {
            get
            {
                if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.PlayingLevel)
                    return true;
                if (LevelManager.Instance != null && LevelManager.Instance.CurrentLevel != null &&
                    (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Training))
                    return true;
                return false;
            }
        }

        private void Awake()
        {
            if (sendButton != null)
            {
                sendButton.onClick.AddListener(OnSendClicked);
            }

            if (backToLevelsButton != null && GameManager.Instance != null)
            {
                backToLevelsButton.onClick.AddListener (() =>
                {
                    if (levelCompletedOverlay != null)
                    {
                        levelCompletedOverlay.SetActive(false);
                    }
                    StopAllDialogueAndTimers();
                    ClearChatContent();
                    GameManager.Instance.SetState(GameState.LevelSelection);
                });
            }
        }

        private void OnEnable()
        {
            if (promptController != null)
            {
                promptController.OnProgressChanged += OnProgressChanged;
            }

            InitializeCurrentMode();
        }

        private void OnDisable()
        {
            if (promptController != null)
            {
                promptController.OnProgressChanged -= OnProgressChanged;
            }
        }

        public void InitializeCurrentMode()
        {
            StopAllDialogueAndTimers();
            ClearChatContent();

            if (levelCompletedOverlay != null)
            {
                levelCompletedOverlay.SetActive(false);
            }

            if (keyboardController != null)
            {
                keyboardController.ResetState();
            }

            if (IsLevelMode)
            {
                if (backButton != null && GameManager.Instance != null)
                {
                    backButton.onClick.AddListener(() =>
                    {
                        StopAllDialogueAndTimers();
                        ClearChatContent();
                        GameManager.Instance.SetState(GameState.LevelSelection);
                    });
                }

                currentStepIndex = 0;
                StartLevelDialogue();
            }
            else
            {
                if (backButton != null && GameManager.Instance != null)
                {
                    backButton.onClick.AddListener(() =>
                    {
                        StopAllDialogueAndTimers();
                        ClearChatContent();
                        GameManager.Instance.SetState(GameState.MainMenu);
                    });
                }
                accuracyTracker.ResetAll();
                SetRandomNextSentence();
            }

        }

        private void ClearChatContent()
        {
            if (chatContent == null) return;
            for (int i = chatContent.childCount - 1; i >= 0; i--)
            {
                Destroy(chatContent.GetChild(i).gameObject);
            }
        }

        private void StopAllDialogueAndTimers()
        {
            if (activeDialogueRoutine != null)
            {
                StopCoroutine(activeDialogueRoutine);
                activeDialogueRoutine = null;
            }

            if (activeTimerRoutine != null)
            {
                StopCoroutine(activeTimerRoutine);
                activeTimerRoutine = null;
            }

            if (currentTypingIndicator != null)
            {
                Destroy(currentTypingIndicator);
                currentTypingIndicator = null;
            }

            isTimeLimitExceeded = false;
        }

        private void StartLevelDialogue()
        {
            accuracyTracker.ResetAll();
            LevelDataSO currentLevel = LevelManager.Instance != null ? LevelManager.Instance.CurrentLevel : null;
            if (currentLevel == null || currentLevel.steps == null || currentLevel.steps.Count == 0)
            {
                Debug.LogWarning("[ChatManager] No level data or steps found in CurrentLevel!");
                SetRandomNextSentence();
                return;
            }

            if (currentStepIndex >= 0 && currentStepIndex < currentLevel.steps.Count)
            {
                activeDialogueRoutine = StartCoroutine(ExecuteDialogueStepRoutine(currentLevel.steps[currentStepIndex]));
            }
        }

        private void OnProgressChanged()
        {
            if(promptController != null && keyboardController != null && promptController.IsCompleted)
            {
                keyboardController.interactable = false;
            }
            UpdateSendButtonState();
        }

        public void UpdateSendButtonState()
        {
            if (promptController == null || sendButton == null) return;

            if(promptController.TargetText.Length == 0)
            {
                sendButton.interactable = false;
            }
            else 
            {
                bool isCompleted = promptController.IsCompleted;
                sendButton.interactable = isCompleted;
                if (sendButtonImage != null)
                {
                    sendButtonImage.color = isCompleted ? sendButtonActiveColor : sendButtonDisabledColor;
                }
            }
        }

        public void OnSendClicked()
        {
            if (promptController == null || !promptController.IsCompleted) return;

            // Stop input timer for this step
            if (activeTimerRoutine != null)
            {
                StopCoroutine(activeTimerRoutine);
                activeTimerRoutine = null;
            }

            string sentText = promptController.TargetText;
            SpawnChatBubble(sentText);

            // Play reveal sequence on keyboard
            if (keyboardController != null)
            {
                keyboardController.PlayRevealSequence();
            }

            ScrollToBottom();

            if (IsLevelMode)
            {
                if (accuracyTracker == null || LevelManager.Instance == null) return;

                LevelDataSO currentLevel = LevelManager.Instance.CurrentLevel;
                if(currentLevel == null) return;

                currentStepIndex++;
                int finalAccuracy = accuracyTracker.OverallAccuracyInt;

                if (currentStepIndex < currentLevel.steps.Count)
                {
                    // Proceed to next dialogue step
                    promptController.SetTargetText(string.Empty);
                    UpdateSendButtonState();
                    activeDialogueRoutine = StartCoroutine(ExecuteDialogueStepRoutine(currentLevel.steps[currentStepIndex]));
                }
                else if(finalAccuracy >= currentLevel.requiredAccuracy)
                {
                    LevelManager.Instance.UnlockNextLevel();
                    LevelManager.Instance.TrySaveHighscore(currentLevel.levelId, finalAccuracy);

                    // Level finished!
                    promptController.SetTargetText(string.Empty);
                    UpdateSendButtonState();
                    levelCompletedOverlay.SetActive(true);
                    levelCompletedTitle.text = "Level abgeschlossen!";
                    levelCompletedInfoTxt.text = 
                        $"Genauigkeit: {accuracyTracker.OverallAccuracyInt}%\n" +
                        $"Highscore: {LevelManager.Instance.GetLevelHighscore(currentLevel.levelId)}%";

                }
                else
                {
                    LevelManager.Instance.TrySaveHighscore(currentLevel.levelId, finalAccuracy);

                    // Level failed
                    promptController.SetTargetText(string.Empty);
                    UpdateSendButtonState();
                    levelCompletedOverlay.SetActive(true);
                    levelCompletedTitle.text = "Level fehlgeschlagen!";
                    levelCompletedInfoTxt.text =
                        $"Genauigkeit: {accuracyTracker.OverallAccuracyInt}%/{currentLevel.requiredAccuracy}%\n" +
                        $"Highscore: {LevelManager.Instance.GetLevelHighscore(currentLevel.levelId)}%";
                }
            }
            else
            {
                SetRandomNextSentence();
                UpdateSendButtonState();
            }
        }

        private IEnumerator ExecuteDialogueStepRoutine(DialogueStep step)
        {
            promptController.SetTargetText(string.Empty);

            // 1. NPC Typing
            if (!string.IsNullOrEmpty(step.npcMessage))
            {
                keyboardController.interactable = false;    

                // Typing indicator
                currentTypingIndicator = SpawnTypingIndicator();
                ScrollToBottom();

                float delay = Mathf.Max(0.2f, step.npcDelay);
                yield return new WaitForSeconds(delay);

                if (currentTypingIndicator != null)
                {
                    Destroy(currentTypingIndicator);
                    currentTypingIndicator = null;
                }

                SpawnNPCChatBubble(step.npcMessage);
                ScrollToBottom();

                // Small conversational pause after NPC sends message
                yield return new WaitForSeconds(0.3f);
            }


            // 2. Set player target prompt
            promptController.SetTargetText(step.playerTargetPrompt ?? string.Empty);
            keyboardController.interactable = true;
            UpdateSendButtonState();

            // 3. Optional time limit
            isTimeLimitExceeded = false;
            if (step.timeLimit > 0f)
            {
                activeTimerRoutine = StartCoroutine(StepTimerRoutine(step.timeLimit));
            }

            activeDialogueRoutine = null;
        }

        private IEnumerator StepTimerRoutine(float limit)
        {
            yield return new WaitForSeconds(limit);

            if (!promptController.IsCompleted && !isTimeLimitExceeded)
            {
                isTimeLimitExceeded = true;
                Debug.LogWarning("[ChatManager] Time limit for current prompt exceeded! Deducting accuracy penalty.");
                accuracyTracker.ApplyTimerPenalty();
            }
            activeTimerRoutine = null;
        }

        // CHAT BUBBLE SPAWNING
        public void SpawnChatBubble(string message)
        {
            SpawnBubbleInternal(message, isPlayer: true);
        }

        public void SpawnNPCChatBubble(string message)
        {
            SpawnBubbleInternal(message, isPlayer: false);
        }

        private GameObject SpawnBubbleInternal(string message, bool isPlayer)
        {
            if (chatContent == null) return null;

            GameObject rowObj = new GameObject(isPlayer ? "PlayerBubbleRow" : "NPCBubbleRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowObj.transform.SetParent(chatContent, false);

            var rowHlg = rowObj.GetComponent<HorizontalLayoutGroup>();
            rowHlg.childAlignment = isPlayer ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            rowHlg.childControlWidth = false;
            rowHlg.childControlHeight = true;
            rowHlg.childForceExpandWidth = false;
            rowHlg.childForceExpandHeight = false;

            GameObject bubbleObj = new GameObject("Bubble", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            bubbleObj.transform.SetParent(rowObj.transform, false);

            var bubbleImg = bubbleObj.GetComponent<Image>();
            if (bubbleSprite != null)
            {
                bubbleImg.sprite = bubbleSprite;
                bubbleImg.type = Image.Type.Sliced;
                bubbleImg.pixelsPerUnitMultiplier = 1f;
            }
            bubbleImg.color = isPlayer ? bubbleColor : npcBubbleColor;

            var bubbleHlg = bubbleObj.GetComponent<HorizontalLayoutGroup>();
            bubbleHlg.padding = new RectOffset(35, 35, 22, 22);
            bubbleHlg.childControlWidth = true;
            bubbleHlg.childControlHeight = true;
            bubbleHlg.childForceExpandWidth = false;
            bubbleHlg.childForceExpandHeight = false;

            var bubbleFitter = bubbleObj.GetComponent<ContentSizeFitter>();
            bubbleFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            bubbleFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            textObj.transform.SetParent(bubbleObj.transform, false);

            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            if (fontAsset != null) tmp.font = fontAsset;
            tmp.text = message;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 18;
            tmp.fontSizeMax = 45;
            tmp.color = isPlayer ? bubbleTextColor : npcBubbleTextColor;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;

            var layoutElement = textObj.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = -1;
            layoutElement.flexibleWidth = -1;

            LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent);
            return rowObj;
        }

        private GameObject SpawnTypingIndicator()
        {
            if (chatContent == null) return null;

            GameObject rowObj = new GameObject("TypingIndicatorRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowObj.transform.SetParent(chatContent, false);

            var rowHlg = rowObj.GetComponent<HorizontalLayoutGroup>();
            rowHlg.childAlignment = TextAnchor.MiddleLeft;
            rowHlg.childControlWidth = false;
            rowHlg.childControlHeight = true;
            rowHlg.childForceExpandWidth = false;
            rowHlg.childForceExpandHeight = false;

            GameObject bubbleObj = new GameObject("Bubble", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            bubbleObj.transform.SetParent(rowObj.transform, false);

            var bubbleImg = bubbleObj.GetComponent<Image>();
            if (bubbleSprite != null)
            {
                bubbleImg.sprite = bubbleSprite;
                bubbleImg.type = Image.Type.Sliced;
                bubbleImg.pixelsPerUnitMultiplier = 1f;
            }
            bubbleImg.color = npcBubbleColor;

            var bubbleHlg = bubbleObj.GetComponent<HorizontalLayoutGroup>();
            bubbleHlg.padding = new RectOffset(35, 35, 20, 20);
            bubbleHlg.childControlWidth = true;
            bubbleHlg.childControlHeight = true;
            bubbleHlg.childForceExpandWidth = false;
            bubbleHlg.childForceExpandHeight = false;

            var bubbleFitter = bubbleObj.GetComponent<ContentSizeFitter>();
            bubbleFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            bubbleFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject textObj = new GameObject("DotsText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(bubbleObj.transform, false);

            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            if (fontAsset != null) tmp.font = fontAsset;
            tmp.text = "...";
            tmp.fontSize = 36;
            tmp.color = new Color32(180, 190, 210, 255);
            tmp.alignment = TextAlignmentOptions.Center;

            LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent);
            return rowObj;
        }

        public void ScrollToBottom()
        {
            StartCoroutine(ScrollToBottomCoroutine());
        }

        private IEnumerator ScrollToBottomCoroutine()
        {
            Canvas.ForceUpdateCanvases();
            yield return null; // wait one frame for layout rebuild
            if (chatContent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent);
            }
            if (chatScrollRect != null)
            {
                chatScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        public void SetRandomNextSentence()
        {
            if (sentencePool == null || sentencePool.Count == 0 || 
                promptController == null || keyboardController == null) return;

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
                } 
                while (nextIndex == lastSentenceIndex);
            }

            lastSentenceIndex = nextIndex;
            promptController.SetTargetText(sentencePool[nextIndex]);
            keyboardController.interactable = true;
        }
    }
}
