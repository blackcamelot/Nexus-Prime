using System;
using System.Collections.Generic;
using UnityEngine;

namespace NexusPrime.Technology
{
    [System.Serializable]
    public class Technology
    {
        [Header("Basic Information")]
        public string techId;
        public string techName;
        public string description;
        public TechCategory category;
        public Sprite icon;
        public string iconPath;
        
        [Header("Research Requirements")]
        public Dictionary<ResourceType, int> researchCost;
        public float researchTime; // in seconds
        public string[] requiredTechs;
        
        [Header("Unlocks")]
        public string[] unlocks; // Format: "system:item" e.g., "unit:assault_trooper", "building:factory"
        
        [Header("Effects")]
        public TechEffect[] effects;
        
        [Header("Visual")]
        public Vector2 treePosition; // Position in tech tree UI
        public Color displayColor = Color.white;
        
        [Header("State")]
        [NonSerialized] public bool isResearched = false;
        [NonSerialized] public bool isAvailable = false;
        [NonSerialized] public bool isResearching = false;
        
        public Technology()
        {
            techId = "unnamed_tech";
            techName = "Unnamed Technology";
            description = "No description available";
            category = TechCategory.General;
            researchCost = new Dictionary<ResourceType, int>();
            researchTime = 60f;
            requiredTechs = new string[0];
            unlocks = new string[0];
            effects = new TechEffect[0];
            treePosition = Vector2.zero;
        }
        
        public string GetCostString()
        {
            List<string> costStrings = new List<string>();
            foreach (var kvp in researchCost)
            {
                costStrings.Add($"{kvp.Value} {kvp.Key}");
            }
            return string.Join(", ", costStrings);
        }
        
        public string GetRequirementsString()
        {
            if (requiredTechs.Length == 0) return "None";
            return string.Join("\n", requiredTechs);
        }
        
        public string GetUnlocksString()
        {
            if (unlocks.Length == 0) return "None";
            
            List<string> unlockStrings = new List<string>();
            foreach (string unlock in unlocks)
            {
                string[] parts = unlock.Split(':');
                if (parts.Length == 2)
                {
                    unlockStrings.Add($"{parts[0]}: {parts[1]}");
                }
                else
                {
                    unlockStrings.Add(unlock);
                }
            }
            return string.Join("\n", unlockStrings);
        }
        
        public string GetEffectsString()
        {
            if (effects.Length == 0) return "None";
            
            List<string> effectStrings = new List<string>();
            foreach (TechEffect effect in effects)
            {
                effectStrings.Add(effect.GetDescription());
            }
            return string.Join("\n", effectStrings);
        }
        
        public string GetResearchTimeString()
        {
            int minutes = Mathf.FloorToInt(researchTime / 60);
            int seconds = Mathf.FloorToInt(researchTime % 60);
            return $"{minutes:00}:{seconds:00}";
        }
        
        public bool HasEffect(TechEffectType type)
        {
            foreach (TechEffect effect in effects)
            {
                if (effect.effectType == type)
                {
                    return true;
                }
            }
            return false;
        }
        
        public TechEffect GetEffect(TechEffectType type)
        {
            foreach (TechEffect effect in effects)
            {
                if (effect.effectType == type)
                {
                    return effect;
                }
            }
            return null;
        }
        
        public float GetEffectValue(TechEffectType type, float defaultValue = 0f)
        {
            TechEffect effect = GetEffect(type);
            return effect != null ? effect.value : defaultValue;
        }
        
        public string GetEffectDescription(TechEffectType type)
        {
            TechEffect effect = GetEffect(type);
            return effect != null ? effect.GetDescription() : string.Empty;
        }
        
        public void ApplyEffects(GameObject target = null)
        {
            foreach (TechEffect effect in effects)
            {
                effect.Apply(target);
            }
        }
        
        public void CloneFrom(Technology other)
        {
            techId = other.techId;
            techName = other.techName;
            description = other.description;
            category = other.category;
            icon = other.icon;
            iconPath = other.iconPath;
            
            researchCost = new Dictionary<ResourceType, int>(other.researchCost);
            researchTime = other.researchTime;
            requiredTechs = (string[])other.requiredTechs.Clone();
            unlocks = (string[])other.unlocks.Clone();
            
            if (other.effects != null)
            {
                effects = new TechEffect[other.effects.Length];
                for (int i = 0; i < other.effects.Length; i++)
                {
                    effects[i] = new TechEffect();
                    effects[i].CloneFrom(other.effects[i]);
                }
            }
            
            treePosition = other.treePosition;
            displayColor = other.displayColor;
        }
    }
    
    [System.Serializable]
    public class TechEffect
    {
        public TechEffectType effectType;
        public string target; // Optional: specific unit/building type
        public float value;
        public string description;
        
        public TechEffect()
        {
            effectType = TechEffectType.StatModifier;
            target = string.Empty;
            value = 0f;
            description = string.Empty;
        }
        
        public string GetDescription()
        {
            if (!string.IsNullOrEmpty(description))
                return description;
            
            string effectName = effectType.ToString();
            string sign = value >= 0 ? "+" : "-";
            string absValue = Mathf.Abs(value).ToString();
            
            switch (effectType)
            {
                case TechEffectType.StatModifier:
                    return $"{sign}{absValue}% {target}";
                    
                case TechEffectType.Unlock:
                    return $"Unlocks {target}";
                    
                case TechEffectType.ResourceProduction:
                    return $"{sign}{absValue} {target} production";
                    
                case TechEffectType.ResearchSpeed:
                    return $"{sign}{absValue}% research speed";
                    
                case TechEffectType.ConstructionSpeed:
                    return $"{sign}{absValue}% construction speed";
                    
                case TechEffectType.UnitProductionSpeed:
                    return $"{sign}{absValue}% unit production speed";
                    
                case TechEffectType.DamageModifier:
                    return $"{sign}{absValue}% damage";
                    
                case TechEffectType.HealthModifier:
                    return $"{sign}{absValue}% health";
                    
                case TechEffectType.ArmorModifier:
                    return $"{sign}{absValue}% armor";
                    
                case TechEffectType.MovementSpeedModifier:
                    return $"{sign}{absValue}% movement speed";
                    
                case TechEffectType.AttackSpeedModifier:
                    return $"{sign}{absValue}% attack speed";
                    
                case TechEffectType.RangeModifier:
                    return $"{sign}{absValue}% attack range";
                    
                case TechEffectType.VisionModifier:
                    return $"{sign}{absValue}% vision range";
                    
                default:
                    return $"{effectName}: {sign}{absValue}";
            }
        }
        
        public void Apply(GameObject target = null)
        {
            // Apply effect to game systems
            // This would typically be handled by specific systems
            
            Debug.Log($"Applying tech effect: {GetDescription()}");
        }
        
        public void CloneFrom(TechEffect other)
        {
            effectType = other.effectType;
            target = other.target;
            value = other.value;
            description = other.description;
        }
    }
    
    public enum TechCategory
    {
        General,
        Construction,
        Economy,
        Military,
        Defense,
        Mobility,
        Energy,
        Cybernetics,
        Nanotechnology,
        ArtificialIntelligence,
        Space,
        Exotic,
        FactionSpecific
    }
    
    public enum TechEffectType
    {
        StatModifier,
        Unlock,
        ResourceProduction,
        ResearchSpeed,
        ConstructionSpeed,
        UnitProductionSpeed,
        DamageModifier,
        HealthModifier,
        ArmorModifier,
        MovementSpeedModifier,
        AttackSpeedModifier,
        RangeModifier,
        VisionModifier,
        SpecialAbility,
        PassiveBonus,
        GlobalModifier
    }
}