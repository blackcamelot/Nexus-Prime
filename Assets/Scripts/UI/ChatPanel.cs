using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace NexusPrime.UI
{
    public class ChatPanel : MonoBehaviour
    {
        [Header("References")]
        public Transform messageContainer;
        public GameObject messagePrefab;
        public int maxMessages = 50;

        private List<GameObject> messageInstances = new List<GameObject>();

        public void Initialize()
        {
            if (messageContainer != null)
            {
                foreach (Transform t in messageContainer)
                    Destroy(t.gameObject);
                messageInstances.Clear();
            }
        }

        public void AddMessage(string sender, string message, ChatMessageType type = ChatMessageType.Normal)
        {
            if (messageContainer == null || messagePrefab == null) return;
            var go = Instantiate(messagePrefab, messageContainer);
            var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = $"[{sender}] {message}";
            messageInstances.Add(go);
            while (messageInstances.Count > maxMessages && messageInstances[0] != null)
            {
                Destroy(messageInstances[0]);
                messageInstances.RemoveAt(0);
            }
        }
    }
}
