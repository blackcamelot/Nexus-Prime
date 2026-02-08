using System;
using System.Collections.Generic;
using UnityEngine;

namespace NexusPrime.Economy
{
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance;
        
        [Header("Resources")]
        public Dictionary<ResourceType, Resource> resources = new Dictionary<ResourceType, Resource>();
        public List<ResourceProducer> producers = new List<ResourceProducer>();
        
        [Header("UI")]
        public ResourcePanel resourcePanel;
        
        [Header("Settings")]
        public float updateInterval = 1.0f;
        
        private float updateTimer;
        private List<ResourceConsumer> consumers = new List<ResourceConsumer>();
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeResources();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializeResources()
        {
            // Create initial resources
            resources.Add(ResourceType.Credits, new Resource(ResourceType.Credits, 1000, 10000));
            resources.Add(ResourceType.Energy, new Resource(ResourceType.Energy, 500, 5000));
            resources.Add(ResourceType.Nanites, new Resource(ResourceType.Nanites, 100, 1000));
            resources.Add(ResourceType.Data, new Resource(ResourceType.Data, 50, 500));
            resources.Add(ResourceType.Influence, new Resource(ResourceType.Influence, 0, 1000));
            
            Debug.Log("Resource Manager Initialized");
        }
        
        void Update()
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                updateTimer = 0;
                UpdateResources();
            }
        }
        
        void UpdateResources()
        {
            // Calculate net production
            Dictionary<ResourceType, int> netProduction = new Dictionary<ResourceType, int>();
            
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                netProduction[type] = 0;
            }
            
            // Add producer outputs
            foreach (ResourceProducer producer in producers)
            {
                if (producer.IsActive())
                {
                    foreach (var output in producer.GetOutput())
                    {
                        if (netProduction.ContainsKey(output.Key))
                        {
                            netProduction[output.Key] += output.Value;
                        }
                    }
                }
            }
            
            // Subtract consumer inputs
            foreach (ResourceConsumer consumer in consumers)
            {
                if (consumer.IsActive())
                {
                    foreach (var input in consumer.GetInput())
                    {
                        if (netProduction.ContainsKey(input.Key))
                        {
                            netProduction[input.Key] -= input.Value;
                        }
                    }
                }
            }
            
            // Apply changes to resources
            foreach (var production in netProduction)
            {
                if (resources.ContainsKey(production.Key))
                {
                    resources[production.Key].Add(production.Value);
                }
            }
            
            // Update UI
            if (resourcePanel != null)
            {
                var amounts = new Dictionary<ResourceType, int>();
                foreach (var kv in resources)
                    amounts[kv.Key] = kv.Value.amount;
                resourcePanel.UpdateResourceDisplay(amounts);
            }
        }
        
        public bool CheckResources(Dictionary<ResourceType, int> costs)
        {
            foreach (var cost in costs)
            {
                if (!resources.ContainsKey(cost.Key) || 
                    !resources[cost.Key].CanAfford(cost.Value))
                {
                    return false;
                }
            }
            return true;
        }
        
        public bool SpendResources(Dictionary<ResourceType, int> costs)
        {
            if (!CheckResources(costs))
                return false;
            
            foreach (var cost in costs)
            {
                resources[cost.Key].Spend(cost.Value);
            }
            
            Debug.Log($"Spent resources: {string.Join(", ", costs)}");
            return true;
        }
        
        public void AddResources(Dictionary<ResourceType, int> amounts)
        {
            foreach (var amount in amounts)
            {
                if (resources.ContainsKey(amount.Key))
                {
                    resources[amount.Key].Add(amount.Value);
                }
            }
        }
        
        public int GetResourceAmount(ResourceType type)
        {
            return resources.ContainsKey(type) ? resources[type].amount : 0;
        }
        
        public int GetStorageCapacity(ResourceType type)
        {
            return resources.ContainsKey(type) ? resources[type].storageCapacity : 0;
        }
        
        public void AddProducer(ResourceProducer producer)
        {
            if (!producers.Contains(producer))
            {
                producers.Add(producer);
            }
        }
        
        public void RemoveProducer(ResourceProducer producer)
        {
            if (producers.Contains(producer))
            {
                producers.Remove(producer);
            }
        }
        
        public void AddConsumer(ResourceConsumer consumer)
        {
            if (!consumers.Contains(consumer))
            {
                consumers.Add(consumer);
            }
        }
        
        public void RemoveConsumer(ResourceConsumer consumer)
        {
            if (consumers.Contains(consumer))
            {
                consumers.Remove(consumer);
            }
        }
        
        public void IncreaseStorage(ResourceType type, int amount)
        {
            if (resources.ContainsKey(type))
            {
                resources[type].storageCapacity += amount;
            }
        }
        
        public void SetStorage(ResourceType type, int capacity)
        {
            if (resources.ContainsKey(type))
            {
                resources[type].storageCapacity = capacity;
                if (resources[type].amount > capacity)
                {
                    resources[type].amount = capacity;
                }
            }
        }
    }
}