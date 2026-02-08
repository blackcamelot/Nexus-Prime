using System.Collections.Generic;
using UnityEngine;
using NexusPrime.Economy;

namespace NexusPrime.AI
{
    public class AIEconomicManager : MonoBehaviour
    {
        [Header("References")]
        public AIFactionController factionController;
        
        [Header("Weights")]
        public float creditsWeight = 1f;
        public float energyWeight = 1f;
        public float nanitesWeight = 1f;

        public Dictionary<ResourceType, float> CalculateResourceNeeds()
        {
            var needs = new Dictionary<ResourceType, float>();
            if (factionController == null) return needs;
            
            foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
                needs[type] = 0f;
            
            float desiredCredits = 5000f;
            float desiredEnergy = 3000f;
            float desiredNanites = 1000f;
            
            float credits = factionController.resourceStockpile.ContainsKey(ResourceType.Credits) ? factionController.resourceStockpile[ResourceType.Credits] : 0;
            float energy = factionController.resourceStockpile.ContainsKey(ResourceType.Energy) ? factionController.resourceStockpile[ResourceType.Energy] : 0;
            float nanites = factionController.resourceStockpile.ContainsKey(ResourceType.Nanites) ? factionController.resourceStockpile[ResourceType.Nanites] : 0;
            
            needs[ResourceType.Credits] = Mathf.Clamp01(1f - credits / Mathf.Max(1f, desiredCredits));
            needs[ResourceType.Energy] = Mathf.Clamp01(1f - energy / Mathf.Max(1f, desiredEnergy));
            needs[ResourceType.Nanites] = Mathf.Clamp01(1f - nanites / Mathf.Max(1f, desiredNanites));
            
            return needs;
        }

        public float GetEconomyStrength()
        {
            if (factionController == null) return 0f;
            float total = 0f, max = 0f;
            foreach (var kv in factionController.resourceStockpile)
            {
                float desired = kv.Key == ResourceType.Credits ? 5000f : kv.Key == ResourceType.Energy ? 3000f : 1000f;
                total += Mathf.Clamp01(kv.Value / Mathf.Max(1f, desired));
                max += 1f;
            }
            return max > 0 ? total / max : 0f;
        }
    }
}
