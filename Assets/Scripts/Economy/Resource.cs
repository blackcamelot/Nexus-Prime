using UnityEngine;

namespace NexusPrime.Economy
{
    [System.Serializable]
    public class Resource
    {
        public ResourceType type;
        public int amount;
        public int storageCapacity;
        public int productionRate;
        public int consumptionRate;
        
        public delegate void ResourceChangedHandler(Resource resource, int change);
        public event ResourceChangedHandler OnResourceChanged;
        
        public Resource(ResourceType type, int initialAmount, int capacity)
        {
            this.type = type;
            this.amount = initialAmount;
            this.storageCapacity = capacity;
            this.productionRate = 0;
            this.consumptionRate = 0;
        }
        
        public bool CanAfford(int cost)
        {
            return amount >= cost;
        }
        
        public void Add(int value)
        {
            int oldAmount = amount;
            amount = Mathf.Clamp(amount + value, 0, storageCapacity);
            
            OnResourceChanged?.Invoke(this, amount - oldAmount);
            
            if (value > 0)
            {
                Debug.Log($"+{value} {type}. Total: {amount}/{storageCapacity}");
            }
        }
        
        public bool Spend(int cost)
        {
            if (!CanAfford(cost))
            {
                Debug.LogWarning($"Not enough {type} to spend {cost}. Current: {amount}");
                return false;
            }
            
            int oldAmount = amount;
            amount -= cost;
            
            OnResourceChanged?.Invoke(this, amount - oldAmount);
            
            Debug.Log($"-{cost} {type}. Remaining: {amount}/{storageCapacity}");
            return true;
        }
        
        public float GetPercentage()
        {
            return storageCapacity > 0 ? (float)amount / storageCapacity : 0;
        }
        
        public bool IsFull()
        {
            return amount >= storageCapacity;
        }
        
        public bool IsEmpty()
        {
            return amount <= 0;
        }
        
        public string GetResourceName()
        {
            switch (type)
            {
                case ResourceType.Credits: return "Credits";
                case ResourceType.Energy: return "Energy";
                case ResourceType.Nanites: return "Nanites";
                case ResourceType.Data: return "Data";
                case ResourceType.Influence: return "Influence";
                default: return type.ToString();
            }
        }
        
        public Color GetResourceColor()
        {
            switch (type)
            {
                case ResourceType.Credits: return Color.yellow;
                case ResourceType.Energy: return new Color(1f, 0.5f, 0f); // Orange
                case ResourceType.Nanites: return Color.green;
                case ResourceType.Data: return Color.cyan;
                case ResourceType.Influence: return Color.magenta;
                default: return Color.white;
            }
        }
        
        public string GetResourceIconPath()
        {
            switch (type)
            {
                case ResourceType.Credits: return "Icons/Resources/Icon_Credits";
                case ResourceType.Energy: return "Icons/Resources/Icon_Energy";
                case ResourceType.Nanites: return "Icons/Resources/Icon_Nanites";
                case ResourceType.Data: return "Icons/Resources/Icon_Data";
                case ResourceType.Influence: return "Icons/Resources/Icon_Influence";
                default: return "Icons/Resources/Icon_Default";
            }
        }
    }
}