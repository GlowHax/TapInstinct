using UnityEngine;
using TMPro;

namespace TapInstinct.UI
{
    public class TextPromptController : MonoBehaviour
    {
        [Header("UI Reference")]
        [SerializeField] private TextMeshProUGUI promptText;

        [Header("Colors & Styling")]
        [SerializeField] private Color typedColor = Color.white;
        [SerializeField] private Color untypedColor = new Color(0.33f, 0.35f, 0.40f, 1f); // Muted gray #555866
        [SerializeField] private Color activeCharColor = new Color(0.49f, 0.66f, 1f, 1f);  // Accent blue #7DA8FF
        [SerializeField] private bool underlineActiveChar = true;

        [Header("Gameplay Data")]
        [TextArea(2, 4)]
        [SerializeField] private string targetText = "Hallo, wie geht es dir.";

        [SerializeField] private int currentIndex = 0;

        // Events
        public event System.Action OnProgressChanged;

        // Public properties for external game logic
        public bool IsCompleted => currentIndex >= targetText.Length;
        public char CurrentTargetChar => !IsCompleted ? targetText[currentIndex] : '\0';
        public int CurrentIndex => currentIndex;
        public string TargetText => targetText;

        private void Start()
        {
            UpdateDisplay();
        }

        // Sets a new sentence and resets progress
        public void SetTargetText(string newText)
        {
            targetText = newText ?? string.Empty;
            currentIndex = 0;
            UpdateDisplay();
        }

        // Advances progress by one character (called when player taps)
        public void AdvanceText()
        {
            if (currentIndex < targetText.Length)
            {
                currentIndex++;
                UpdateDisplay();
            }
        }

        // Resets progress back to start
        public void ResetProgress()
        {
            currentIndex = 0;
            UpdateDisplay();
        }

        // Rebuilds the formatted rich text string
        public void UpdateDisplay()
        {
            if (promptText == null) return;

            if (string.IsNullOrEmpty(targetText))
            {
                promptText.text = string.Empty;
                return;
            }

            // Convert Unity Colors to Hex strings for TextMeshPro rich text tags
            string typedHex = ColorUtility.ToHtmlStringRGBA(typedColor);
            string untypedHex = ColorUtility.ToHtmlStringRGBA(untypedColor);
            string activeHex = ColorUtility.ToHtmlStringRGBA(activeCharColor);

            // 1. Text already typed
            string typedPart = targetText.Substring(0, currentIndex);

            if (currentIndex < targetText.Length)
            {
                // 2. The next active character to type
                char activeChar = targetText[currentIndex];
                string formattedActive = underlineActiveChar 
                    ? $"<u>{activeChar}</u>" 
                    : activeChar.ToString();

                // 3. Remaining ghost text
                string remainingPart = targetText.Substring(currentIndex + 1);

                // Combine all three sections
                promptText.text = $"<color=#{typedHex}>{typedPart}</color>" +
                                  $"<color=#{activeHex}>{formattedActive}</color>" +
                                  $"<color=#{untypedHex}>{remainingPart}</color>";
            }
            else
            {
                // Whole sentence is finished: everything in white
                promptText.text = $"<color=#{typedHex}>{typedPart}</color>";
            }

            OnProgressChanged?.Invoke();
        }

#if UNITY_EDITOR
        [ContextMenu("Test: Type Next Char")]
        public void TestTypeNextChar()
        {
            AdvanceText();
        }

        [ContextMenu("Test: Reset Progress")]
        public void TestResetProgress()
        {
            ResetProgress();
        }
#endif
    }
}
