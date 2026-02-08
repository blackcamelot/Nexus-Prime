using System.Collections.Generic;
using UnityEngine;

namespace NexusPrime.Economy
{
    public class ResourceConsumer : MonoBehaviour
    {
        [System.Serializable]
        public class ConsumptionInput
        {
            public ResourceType resourceType;
            public int amountPerMinute;
        }

        [Header("Consumption Settings")]
        public List<ConsumptionInput> inputs = new List<ConsumptionInput>();
        public bool isActive = true;

        public bool IsActive()
        {
            return isActive;
        }

        public Dictionary<ResourceType, int> GetInput()
        {
            Dictionary<ResourceType, int> input = new Dictionary<ResourceType, int>();

            if (!IsActive()) return input;

            foreach (ConsumptionInput consumption in inputs)
            {
                int amount = Mathf.RoundToInt(consumption.amountPerMinute / 60f);
                if (input.ContainsKey(consumption.resourceType))
                    input[consumption.resourceType] += amount;
                else
                    input[consumption.resourceType] = amount;
            }

            return input;
        }
    }
}
