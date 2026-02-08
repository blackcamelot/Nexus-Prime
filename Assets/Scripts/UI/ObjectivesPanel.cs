using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace NexusPrime.UI
{
    public class ObjectivesPanel : MonoBehaviour
    {
        [Header("References")]
        public Transform objectivesContainer;
        public GameObject objectiveLinePrefab;

        private Dictionary<string, GameObject> objectiveLines = new Dictionary<string, GameObject>();

        public void Initialize()
        {
            if (objectivesContainer != null)
            {
                foreach (Transform t in objectivesContainer)
                    Destroy(t.gameObject);
                objectiveLines.Clear();
            }
        }

        public void UpdateObjective(string objectiveId, string description, float progress = 0f)
        {
            if (objectivesContainer == null || objectiveLinePrefab == null) return;
            if (!objectiveLines.TryGetValue(objectiveId, out var line))
            {
                line = Instantiate(objectiveLinePrefab, objectivesContainer);
                objectiveLines[objectiveId] = line;
            }
            var tmp = line.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = $"[{progress:P0}] {description}";
            line.SetActive(true);
        }

        public void CompleteObjective(string objectiveId)
        {
            if (objectiveLines.TryGetValue(objectiveId, out var line) && line != null)
                line.SetActive(false);
        }
    }
}
