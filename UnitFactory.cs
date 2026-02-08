using System.Collections.Generic;
using UnityEngine;

namespace NexusPrime.Units
{
    [CreateAssetMenu(fileName = "UnitFactory", menuName = "Nexus Prime/Unit Factory")]
    public class UnitFactory : ScriptableObject
    {
        [System.Serializable]
        public class UnitDefinition
        {
            public string unitId;
            public string unitName;
            public GameObject prefab;
            public UnitStats baseStats;
            public Dictionary<ResourceType, int> cost;
            public float buildTime;
            public string[] requiredTechs;
            public string description;
            public Sprite icon;
            
            public UnitDefinition()
            {
                cost = new Dictionary<ResourceType, int>();
                requiredTechs = new string[0];
            }
        }
        
        [Header("Unit Definitions")]
        public List<UnitDefinition> unitDefinitions = new List<UnitDefinition>();
        
        [Header("Production Settings")]
        public int maxQueueSize = 5;
        public bool parallelProduction = false;
        
        private Dictionary<string, UnitDefinition> unitDictionary = new Dictionary<string, UnitDefinition>();
        
        public void Initialize()
        {
            unitDictionary.Clear();
            
            foreach (UnitDefinition definition in unitDefinitions)
            {
                if (!unitDictionary.ContainsKey(definition.unitId))
                {
                    unitDictionary.Add(definition.unitId, definition);
                }
                else
                {
                    Debug.LogWarning($"Duplicate unit ID: {definition.unitId}");
                }
            }
            
            Debug.Log($"Unit Factory initialized with {unitDictionary.Count} units");
        }
        
        public GameObject CreateUnit(string unitId, Vector3 position, Quaternion rotation, string faction)
        {
            if (!unitDictionary.ContainsKey(unitId))
            {
                Debug.LogError($"Unit ID not found: {unitId}");
                return null;
            }
            
            UnitDefinition definition = unitDictionary[unitId];
            
            if (definition.prefab == null)
            {
                Debug.LogError($"No prefab for unit: {unitId}");
                return null;
            }
            
            // Instantiate unit
            GameObject unitInstance = Instantiate(definition.prefab, position, rotation);
            
            // Setup unit components
            CombatUnit combatUnit = unitInstance.GetComponent<CombatUnit>();
            if (combatUnit != null)
            {
                combatUnit.unitId = unitId;
                combatUnit.unitName = definition.unitName;
                combatUnit.faction = faction;
            }
            
            UnitStats stats = unitInstance.GetComponent<UnitStats>();
            if (stats != null && definition.baseStats != null)
            {
                // Copy base stats
                stats.maxHealth = definition.baseStats.maxHealth;
                stats.maxShield = definition.baseStats.maxShield;
                stats.healthRegen = definition.baseStats.healthRegen;
                stats.shieldRegen = definition.baseStats.shieldRegen;
                stats.damage = definition.baseStats.damage;
                stats.attackSpeed = definition.baseStats.attackSpeed;
                stats.attackRange = definition.baseStats.attackRange;
                stats.armor = definition.baseStats.armor;
                stats.penetration = definition.baseStats.penetration;
                stats.movementSpeed = definition.baseStats.movementSpeed;
                stats.rotationSpeed = definition.baseStats.rotationSpeed;
                stats.acceleration = definition.baseStats.acceleration;
                stats.sightRange = definition.baseStats.sightRange;
                stats.detectionRange = definition.baseStats.detectionRange;
                stats.energy = definition.baseStats.energy;
                stats.maxEnergy = definition.baseStats.maxEnergy;
                stats.energyRegen = definition.baseStats.energyRegen;
                
                stats.Initialize();
            }
            
            SelectableUnit selectable = unitInstance.GetComponent<SelectableUnit>();
            if (selectable != null)
            {
                selectable.unitId = unitId;
                selectable.displayName = definition.unitName;
                selectable.description = definition.description;
                selectable.icon = definition.icon;
            }
            
            UnitMovement movement = unitInstance.GetComponent<UnitMovement>();
            if (movement != null && stats != null)
            {
                movement.moveSpeed = stats.movementSpeed;
                movement.rotationSpeed = stats.rotationSpeed;
                movement.acceleration = stats.acceleration;
            }
            
            Debug.Log($"Created unit: {definition.unitName} at {position}");
            return unitInstance;
        }
        
        public UnitDefinition GetUnitDefinition(string unitId)
        {
            if (unitDictionary.ContainsKey(unitId))
            {
                return unitDictionary[unitId];
            }
            return null;
        }
        
        public Dictionary<ResourceType, int> GetUnitCost(string unitId)
        {
            UnitDefinition definition = GetUnitDefinition(unitId);
            return definition?.cost ?? new Dictionary<ResourceType, int>();
        }
        
        public float GetBuildTime(string unitId)
        {
            UnitDefinition definition = GetUnitDefinition(unitId);
            return definition?.buildTime ?? 0f;
        }
        
        public bool CanBuildUnit(string unitId, PlayerData playerData)
        {
            UnitDefinition definition = GetUnitDefinition(unitId);
            if (definition == null) return false;
            
            // Check tech requirements
            foreach (string requiredTech in definition.requiredTechs)
            {
                if (!playerData.HasResearched(requiredTech))
                {
                    return false;
                }
            }
            
            // Check if unit is unlocked
            if (!playerData.HasUnlockedUnit(unitId))
            {
                return false;
            }
            
            return true;
        }
        
        public List<string> GetAvailableUnits(PlayerData playerData)
        {
            List<string> availableUnits = new List<string>();
            
            foreach (var kvp in unitDictionary)
            {
                if (CanBuildUnit(kvp.Key, playerData))
                {
                    availableUnits.Add(kvp.Key);
                }
            }
            
            return availableUnits;
        }
        
        public List<string> GetUnitsByType(string unitType)
        {
            List<string> units = new List<string>();
            
            foreach (var kvp in unitDictionary)
            {
                // Check unit type based on naming convention or tags
                if (kvp.Key.Contains(unitType))
                {
                    units.Add(kvp.Key);
                }
            }
            
            return units;
        }
        
        public void AddUnitDefinition(UnitDefinition definition)
        {
            if (!unitDictionary.ContainsKey(definition.unitId))
            {
                unitDefinitions.Add(definition);
                unitDictionary.Add(definition.unitId, definition);
            }
        }
        
        public void RemoveUnitDefinition(string unitId)
        {
            if (unitDictionary.ContainsKey(unitId))
            {
                UnitDefinition definition = unitDictionary[unitId];
                unitDefinitions.Remove(definition);
                unitDictionary.Remove(unitId);
            }
        }
        
        public void UpdateUnitDefinition(string unitId, UnitDefinition newDefinition)
        {
            if (unitDictionary.ContainsKey(unitId))
            {
                UnitDefinition oldDefinition = unitDictionary[unitId];
                int index = unitDefinitions.IndexOf(oldDefinition);
                
                if (index >= 0)
                {
                    unitDefinitions[index] = newDefinition;
                    unitDictionary[unitId] = newDefinition;
                }
            }
        }
        
        public void LoadUnitDefinitionsFromJSON(string jsonPath)
        {
            // Load unit definitions from JSON file
            TextAsset jsonFile = Resources.Load<TextAsset>(jsonPath);
            if (jsonFile != null)
            {
                // Parse JSON and create unit definitions
                // Implementation depends on JSON structure
            }
        }
    }
}