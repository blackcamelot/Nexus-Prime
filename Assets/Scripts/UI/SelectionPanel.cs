using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NexusPrime.Units;

namespace NexusPrime.UI
{
    public class SelectionPanel : MonoBehaviour
    {
        [Header("References")]
        public Transform unitListContainer;
        public GameObject unitSlotPrefab;
        public Image primaryUnitIcon;
        public TextMeshProUGUI primaryUnitName;
        public Image primaryHealthBar;

        private List<GameObject> slotInstances = new List<GameObject>();

        public void Initialize()
        {
            if (unitListContainer != null)
            {
                foreach (Transform t in unitListContainer)
                    Destroy(t.gameObject);
                slotInstances.Clear();
            }
        }

        public void UpdateSelection(List<SelectableUnit> selectedUnits)
        {
            if (selectedUnits == null || selectedUnits.Count == 0)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            if (primaryUnitName != null)
                primaryUnitName.text = selectedUnits.Count == 1 ? selectedUnits[0].gameObject.name : $"{selectedUnits.Count} units";

            if (selectedUnits.Count == 1)
            {
                var stats = selectedUnits[0].GetComponent<UnitStats>();
                if (primaryHealthBar != null && stats != null)
                    primaryHealthBar.fillAmount = stats.currentHealth / Mathf.Max(1f, stats.maxHealth);
            }

            if (unitListContainer != null && unitSlotPrefab != null)
            {
                while (slotInstances.Count < selectedUnits.Count)
                {
                    var slot = Instantiate(unitSlotPrefab, unitListContainer);
                    slotInstances.Add(slot);
                }
                for (int i = 0; i < slotInstances.Count; i++)
                {
                    slotInstances[i].SetActive(i < selectedUnits.Count);
                    if (i < selectedUnits.Count)
                    {
                        var label = slotInstances[i].GetComponentInChildren<TextMeshProUGUI>();
                        if (label != null) label.text = selectedUnits[i].gameObject.name;
                    }
                }
            }
        }
    }
}
