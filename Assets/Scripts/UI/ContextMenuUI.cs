using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NexusPrime.UI
{
    public class ContextMenuUI : MonoBehaviour
    {
        [Header("References")]
        public Transform itemsContainer;
        public GameObject itemButtonPrefab;

        private List<GameObject> buttons = new List<GameObject>();

        public void SetItems(List<ContextMenuItem> items)
        {
            if (itemsContainer == null || itemButtonPrefab == null) return;
            foreach (var b in buttons)
                if (b != null) Destroy(b);
            buttons.Clear();

            foreach (var item in items)
            {
                if (!item.enabled) continue;
                var btn = Instantiate(itemButtonPrefab, itemsContainer);
                var label = btn.GetComponentInChildren<UnityEngine.UI.Text>();
                if (label == null) label = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (label != null) label.text = item.label;
                var button = btn.GetComponent<Button>();
                if (button != null && item.action != null)
                {
                    var action = item.action;
                    button.onClick.AddListener(() => action?.Invoke());
                }
                buttons.Add(btn);
            }
        }
    }
}
