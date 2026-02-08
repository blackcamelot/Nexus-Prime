using UnityEngine;
using TMPro;

namespace NexusPrime.UI
{
    public class DialogueSystem : MonoBehaviour
    {
        [Header("References")]
        public GameObject dialoguePanel;
        public TextMeshProUGUI speakerText;
        public TextMeshProUGUI lineText;

        public void ShowLine(string speaker, string line)
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(true);
            if (speakerText != null) speakerText.text = speaker;
            if (lineText != null) lineText.text = line;
        }

        public void Hide()
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
        }
    }
}
