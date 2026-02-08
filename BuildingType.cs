using System;
using System.Collections.Generic;

namespace NexusPrime.Building
{
    [System.Serializable]
    public class BuildingDefinition
    {
        [Header("Basic Info")]
        public string buildingId;
        public string buildingName;
        public string description;
        public BuildingType buildingType;
        public GameObject prefab;
        public Sprite icon;
        
        [Header("Stats")]
        public float health = 1000f;
        public float armor = 0f;
        public float constructionTime = 30f;
        public float powerConsumption = 0f;
        public float powerProduction = 0f;
        public float influenceRadius = 0f;
        
        [Header("Size")]
        public int width = 1;
        public int depth = 1;
        public float height = 1f;
        public float heightOffset = 0f;
        
        [Header("Cost")]
        public Dictionary<ResourceType, int> cost;
        
        [Header("Requirements")]
        public string[] requiredTechs;
        public string[] unlocksTechs;
        public string[] producesUnits;
        public string[] producesResources;
        
        [Header("Special")]
        public bool isUnique = false;
        public int maxPerPlayer = 99;
        public bool requiresPower = true;
        public bool providesPopulation = false;
        public int populationProvided = 0;
        
        public BuildingDefinition()
        {
            cost = new Dictionary<ResourceType, int>();
            requiredTechs = new string[0];
            unlocksTechs = new string[0];
            producesUnits = new string[0];
            producesResources = new string[0];
            buildingType = BuildingType.Utility;
        }
        
        public bool CanAfford(Dictionary<ResourceType, int> playerResources)
        {
            foreach (var resourceCost in cost)
            {
                if (!playerResources.ContainsKey(resourceCost.Key) || 
                    playerResources[resourceCost.Key] < resourceCost.Value)
                {
                    return false;
                }
            }
            return true;
        }
        
        public Dictionary<ResourceType, int> GetResourceProduction()
        {
            Dictionary<ResourceType, int> production = new Dictionary<ResourceType, int>();
            
            foreach (string resource in producesResources)
            {
                // Parse resource production from string
                // Format: "ResourceType:Amount"
                string[] parts = resource.Split(':');
                if (parts.Length == 2)
                {
                    if (Enum.TryParse(parts[0], out ResourceType resourceType))
                    {
                        if (int.TryParse(parts[1], out int amount))
                        {
                            production[resourceType] = amount;
                        }
                    }
                }
            }
            
            return production;
        }
        
        public string GetCostString()
        {
            List<string> costStrings = new List<string>();
            foreach (var kvp in cost)
            {
                costStrings.Add($"{kvp.Value} {kvp.Key}");
            }
            return string.Join(", ", costStrings);
        }
        
        public string GetRequirementsString()
        {
            if (requiredTechs.Length == 0) return "None";
            return string.Join(", ", requiredTechs);
        }
        
        public string GetProductionString()
        {
            List<string> production = new List<string>();
            
            if (producesUnits.Length > 0)
            {
                production.Add($"Units: {string.Join(", ", producesUnits)}");
            }
            
            if (producesResources.Length > 0)
            {
                production.Add($"Resources: {string.Join(", ", producesResources)}");
            }
            
            if (powerProduction > 0)
            {
                production.Add($"Power: +{powerProduction}");
            }
            
            return string.Join("\n", production);
        }
    }
    
    public enum BuildingType
    {
        // Core Buildings
        Command,
        Economy,
        
        // Military
        Defense,
        Production,
        Support,
        
        // Technology
        Research,
        Special,
        
        // Utility
        Utility,
        Infrastructure,
        
        // Unique
        Unique,
        Wonder
    }
    
    public static class BuildingTypeExtensions
    {
        public static string GetDisplayName(this BuildingType type)
        {
            switch (type)
            {
                case BuildingType.Command: return "Command";
                case BuildingType.Economy: return "Economy";
                case BuildingType.Defense: return "Defense";
                case BuildingType.Production: return "Production";
                case BuildingType.Support: return "Support";
                case BuildingType.Research: return "Research";
                case BuildingType.Special: return "Special";
                case BuildingType.Utility: return "Utility";
                case BuildingType.Infrastructure: return "Infrastructure";
                case BuildingType.Unique: return "Unique";
                case BuildingType.Wonder: return "Wonder";
                default: return type.ToString();
            }
        }
        
        public static string GetDescription(this BuildingType type)
        {
            switch (type)
            {
                case BuildingType.Command: return "Core buildings required for base operations";
                case BuildingType.Economy: return "Resource generation and management buildings";
                case BuildingType.Defense: return "Defensive structures for base protection";
                case BuildingType.Production: return "Unit production facilities";
                case BuildingType.Support: return "Support buildings for units and other structures";
                case BuildingType.Research: return "Technology research facilities";
                case BuildingType.Special: return "Specialized buildings with unique abilities";
                case BuildingType.Utility: return "Utility buildings for various functions";
                case BuildingType.Infrastructure: return "Infrastructure for base expansion";
                case BuildingType.Unique: return "Unique faction-specific buildings";
                case BuildingType.Wonder: return "Game-changing super structures";
                default: return "Unknown building type";
            }
        }
        
        public static Color GetColor(this BuildingType type)
        {
            switch (type)
            {
                case BuildingType.Command: return Color.red;
                case BuildingType.Economy: return Color.yellow;
                case BuildingType.Defense: return new Color(1f, 0.5f, 0f); // Orange
                case BuildingType.Production: return Color.green;
                case BuildingType.Support: return Color.blue;
                case BuildingType.Research: return Color.cyan;
                case BuildingType.Special: return Color.magenta;
                case BuildingType.Utility: return Color.gray;
                case BuildingType.Infrastructure: return Color.white;
                case BuildingType.Unique: return new Color(0.5f, 0f, 0.5f); // Purple
                case BuildingType.Wonder: return new Color(1f, 1f, 0f); // Gold
                default: return Color.white;
            }
        }
        
        public static string GetIconPath(this BuildingType type)
        {
            switch (type)
            {
                case BuildingType.Command: return "Icons/Buildings/Command";
                case BuildingType.Economy: return "Icons/Buildings/Economy";
                case BuildingType.Defense: return "Icons/Buildings/Defense";
                case BuildingType.Production: return "Icons/Buildings/Production";
                case BuildingType.Support: return "Icons/Buildings/Support";
                case BuildingType.Research: return "Icons/Buildings/Research";
                case BuildingType.Special: return "Icons/Buildings/Special";
                case BuildingType.Utility: return "Icons/Buildings/Utility";
                case BuildingType.Infrastructure: return "Icons/Buildings/Infrastructure";
                case BuildingType.Unique: return "Icons/Buildings/Unique";
                case BuildingType.Wonder: return "Icons/Buildings/Wonder";
                default: return "Icons/Buildings/Default";
            }
        }
    }
}