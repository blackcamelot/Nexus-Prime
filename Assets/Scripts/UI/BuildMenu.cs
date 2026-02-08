using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NexusPrime.Building;
using NexusPrime.Economy;

namespace NexusPrime.UI
{
    public class BuildMenu : MonoBehaviour
    {
        [Header("References")]
        public GameObject buildingButtonPrefab;
        public Transform buildingListContainer;
        public BuildingSystem buildingSystem;
        public ResourceManager resourceManager;

        private List<GameObject> buttonInstances = new List<GameObject>();

        void Start()
        {
            if (buildingSystem == null) buildingSystem = FindObjectOfType<BuildingSystem>();
            if (resourceManager == null) resourceManager = FindObjectOfType<ResourceManager>();
        }

        public void UpdateAvailableBuildings()
        {
            if (buildingListContainer == null || buildingButtonPrefab == null) return;
            foreach (var b in buttonInstances)
                if (b != null) Destroy(b);
            buttonInstances.Clear();

            var factory = buildingSystem != null ? buildingSystem.buildingFactory : FindObjectOfType<BuildingFactory>();
            if (factory == null) return;

            foreach (var def in factory.GetAllDefinitions())
            {
                if (def == null) continue;
                var btn = Instantiate(buildingButtonPrefab, buildingListContainer);
                var label = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = def.buildingName;
                var button = btn.GetComponent<Button>();
                if (button != null)
                {
                    var id = def.buildingId;
                    button.onClick.AddListener(() =>
                    {
                        if (buildingSystem != null) buildingSystem.EnterBuildingMode(id);
                    });
                }
                buttonInstances.Add(btn);
            }
        }
    }
}
