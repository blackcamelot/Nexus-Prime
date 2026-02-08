using System.Collections.Generic;
using UnityEngine;

namespace NexusPrime.Economy
{
    public class ResourceProducer : MonoBehaviour
    {
        [System.Serializable]
        public class ProductionOutput
        {
            public ResourceType resourceType;
            public int amountPerMinute;
            public bool requiresPower = true;
        }
        
        [Header("Production Settings")]
        public List<ProductionOutput> outputs = new List<ProductionOutput>();
        public bool isActive = true;
        public float efficiency = 1.0f;
        
        [Header("Power Requirements")]
        public int powerConsumption = 0;
        public bool hasPower = true;
        
        [Header("Visuals")]
        public ParticleSystem productionParticles;
        public Light productionLight;
        
        private ResourceManager resourceManager;
        
        void Start()
        {
            resourceManager = ResourceManager.Instance;
            
            if (resourceManager != null)
            {
                resourceManager.AddProducer(this);
            }
            
            // Initialize visuals
            UpdateVisuals();
        }
        
        void OnDestroy()
        {
            if (resourceManager != null)
            {
                resourceManager.RemoveProducer(this);
            }
        }
        
        public bool IsActive()
        {
            return isActive && (powerConsumption == 0 || hasPower);
        }
        
        public Dictionary<ResourceType, int> GetOutput()
        {
            Dictionary<ResourceType, int> output = new Dictionary<ResourceType, int>();
            
            if (!IsActive()) return output;
            
            foreach (ProductionOutput production in outputs)
            {
                if (production.requiresPower && !hasPower)
                    continue;
                    
                int amount = Mathf.RoundToInt(production.amountPerMinute / 60f * efficiency);
                
                if (output.ContainsKey(production.resourceType))
                {
                    output[production.resourceType] += amount;
                }
                else
                {
                    output[production.resourceType] = amount;
                }
            }
            
            return output;
        }
        
        public void SetActive(bool active)
        {
            isActive = active;
            UpdateVisuals();
        }
        
        public void SetEfficiency(float newEfficiency)
        {
            efficiency = Mathf.Clamp01(newEfficiency);
            UpdateVisuals();
        }
        
        public void SetPower(bool powered)
        {
            hasPower = powered;
            UpdateVisuals();
        }
        
        public void AddOutput(ResourceType type, int amountPerMinute)
        {
            ProductionOutput output = new ProductionOutput();
            output.resourceType = type;
            output.amountPerMinute = amountPerMinute;
            outputs.Add(output);
        }
        
        public void RemoveOutput(ResourceType type)
        {
            outputs.RemoveAll(o => o.resourceType == type);
        }
        
        public int GetTotalOutput(ResourceType type)
        {
            int total = 0;
            foreach (ProductionOutput output in outputs)
            {
                if (output.resourceType == type)
                {
                    total += Mathf.RoundToInt(output.amountPerMinute / 60f * efficiency);
                }
            }
            return total;
        }
        
        private void UpdateVisuals()
        {
            bool shouldBeVisible = IsActive() && efficiency > 0.1f;
            
            if (productionParticles != null)
            {
                if (shouldBeVisible && !productionParticles.isPlaying)
                {
                    productionParticles.Play();
                }
                else if (!shouldBeVisible && productionParticles.isPlaying)
                {
                    productionParticles.Stop();
                }
                
                var emission = productionParticles.emission;
                emission.rateOverTime = efficiency * 10f;
            }
            
            if (productionLight != null)
            {
                productionLight.enabled = shouldBeVisible;
                productionLight.intensity = efficiency * 2f;
            }
        }
        
        public void UpgradeProduction(float efficiencyMultiplier)
        {
            efficiency *= efficiencyMultiplier;
            UpdateVisuals();
            Debug.Log($"Production upgraded. New efficiency: {efficiency}");
        }
    }
}