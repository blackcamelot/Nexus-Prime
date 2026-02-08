using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NexusPrime.UI
{
    public class ConfirmationDialogUI : MonoBehaviour
    {
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI messageText;
        public Button confirmButton;
        public Button cancelButton;

        private System.Action onConfirm;
        private System.Action onCancel;

        public void Show(string title, string message, System.Action onConfirm, System.Action onCancel = null)
        {
            this.onConfirm = onConfirm;
            this.onCancel = onCancel;
            if (titleText != null) titleText.text = title;
            if (messageText != null) messageText.text = message;
            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
            if (cancelButton != null) cancelButton.onClick.AddListener(OnCancel);
            gameObject.SetActive(true);
        }

        void OnConfirm()
        {
            onConfirm?.Invoke();
            gameObject.SetActive(false);
        }

        void OnCancel()
        {
            onCancel?.Invoke();
            gameObject.SetActive(false);
        }
    }
}
