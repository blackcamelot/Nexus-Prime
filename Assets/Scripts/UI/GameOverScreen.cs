using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NexusPrime.UI
{
    public class GameOverScreen : MonoBehaviour
    {
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI messageText;
        public Button mainMenuButton;

        public void SetVictory(bool victory)
        {
            if (titleText != null) titleText.text = victory ? "Vittoria!" : "Sconfitta";
            if (messageText != null) messageText.text = victory ? "Hai completato la missione." : "La tua base è stata distrutta.";
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(() => UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu"));
        }
    }
}
