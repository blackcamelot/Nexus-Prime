using System.Collections.Generic;
using UnityEngine;

namespace NexusPrime.Building
{
    public class BuildingFactory : MonoBehaviour
    {
        [Header("Building Definitions")]
        public List<BuildingDefinition> definitions = new List<BuildingDefinition>();

        private Dictionary<string, BuildingDefinition> definitionLookup;

        void Awake()
        {
            definitionLookup = new Dictionary<string, BuildingDefinition>();
            foreach (var def in definitions)
            {
                if (def != null && !string.IsNullOrEmpty(def.buildingId))
                {
                    definitionLookup[def.buildingId] = def;
                }
            }
        }

        public BuildingDefinition GetBuildingDefinition(string buildingId)
        {
            if (definitionLookup == null)
            {
                definitionLookup = new Dictionary<string, BuildingDefinition>();
                foreach (var def in definitions)
                {
                    if (def != null && !string.IsNullOrEmpty(def.buildingId))
                        definitionLookup[def.buildingId] = def;
                }
            }
            return definitionLookup.TryGetValue(buildingId, out var d) ? d : null;
        }

        public List<BuildingDefinition> GetAllDefinitions()
        {
            return new List<BuildingDefinition>(definitions);
        }

        public GameObject CreateBuilding(string buildingId, Vector3 position, Quaternion rotation, string ownerFaction)
        {
            var def = GetBuildingDefinition(buildingId);
            if (def == null || def.prefab == null) return null;
            var go = Instantiate(def.prefab, position, rotation);
            var building = go.GetComponent<Building>();
            if (building != null)
            {
                building.definition = def;
                building.buildingId = def.buildingId;
                building.ownerFaction = ownerFaction;
                building.InitializeFromDefinition();
            }
            return go;
        }
    }
}
