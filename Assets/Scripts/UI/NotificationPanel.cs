using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NexusPrime.UI
{
    public class NotificationPanel : MonoBehaviour
    {
        [Header("References")]
        public GameObject notificationItemPrefab;
        public Transform container;
        public float displayDuration = 4f;
        public int maxVisible = 5;

        public void Initialize() { }

        public void ShowNotification(string title, string message, NotificationType type = NotificationType.Info)
        {
            if (container == null || notificationItemPrefab == null) return;
            var item = Instantiate(notificationItemPrefab, container);
            var titleT = item.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
            var msgT = item.transform.Find("Message")?.GetComponent<TextMeshProUGUI>();
            if (titleT != null) titleT.text = title;
            if (msgT != null) msgT.text = message;
            Destroy(item, displayDuration);
        }
    }
}
