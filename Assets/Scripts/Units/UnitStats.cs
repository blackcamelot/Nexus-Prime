using System;
using UnityEngine;

namespace NexusPrime.Units
{
    [System.Serializable]
    public class UnitStats
    {
        [Header("Vital Stats")]
        public float maxHealth = 100f;
        public float maxShield = 0f;
        public float healthRegen = 0f;
        public float shieldRegen = 0f;
        
        [Header("Combat Stats")]
        public float damage = 10f;
        public float attackSpeed = 1f; // attacks per second
        public float attackRange = 5f;
        public float armor = 0f; // damage reduction percentage
        public float penetration = 0f; // armor penetration percentage
        
        [Header("Movement Stats")]
        public float movementSpeed = 3.5f;
        public float rotationSpeed = 360f;
        public float acceleration = 8f;
        
        [Header("Vision")]
        public float sightRange = 20f;
        public float detectionRange = 10f;
        
        [Header("Special")]
        public float energy = 0f;
        public float maxEnergy = 0f;
        public float energyRegen = 0f;
        
        // Current values (not serialized)
        [NonSerialized] public float currentHealth;
        [NonSerialized] public float currentShield;
        [NonSerialized] public float currentEnergy;
        [NonSerialized] public float attackCooldown;
        
        public void Initialize()
        {
            currentHealth = maxHealth;
            currentShield = maxShield;
            currentEnergy = maxEnergy;
            attackCooldown = 0f;
        }
        
        public void Update(float deltaTime)
        {
            // Update cooldowns
            if (attackCooldown > 0)
            {
                attackCooldown -= deltaTime;
            }
            
            // Regenerate health
            if (currentHealth < maxHealth && healthRegen > 0)
            {
                currentHealth = Mathf.Min(maxHealth, currentHealth + healthRegen * deltaTime);
            }
            
            // Regenerate shield
            if (currentShield < maxShield && shieldRegen > 0)
            {
                currentShield = Mathf.Min(maxShield, currentShield + shieldRegen * deltaTime);
            }
            
            // Regenerate energy
            if (currentEnergy < maxEnergy && energyRegen > 0)
            {
                currentEnergy = Mathf.Min(maxEnergy, currentEnergy + energyRegen * deltaTime);
            }
        }
        
        public bool CanAttack()
        {
            return attackCooldown <= 0 && currentHealth > 0;
        }
        
        public void StartAttackCooldown()
        {
            attackCooldown = 1f / attackSpeed;
        }
        
        public void TakeDamage(float incomingDamage, float armorPenetration = 0f)
        {
            // Calculate effective armor
            float effectiveArmor = Mathf.Max(0, armor * (1f - armorPenetration));
            float damageReduction = effectiveArmor / (effectiveArmor + 100f);
            float finalDamage = incomingDamage * (1f - damageReduction);
            
            // Apply to shield first
            if (currentShield > 0)
            {
                float shieldDamage = Mathf.Min(currentShield, finalDamage);
                currentShield -= shieldDamage;
                finalDamage -= shieldDamage;
            }
            
            // Apply to health
            if (finalDamage > 0)
            {
                currentHealth = Mathf.Max(0, currentHealth - finalDamage);
            }
        }
        
        public void Heal(float amount)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        }
        
        public void RepairShield(float amount)
        {
            currentShield = Mathf.Min(maxShield, currentShield + amount);
        }
        
        public void AddEnergy(float amount)
        {
            currentEnergy = Mathf.Min(maxEnergy, currentEnergy + amount);
        }
        
        public bool UseEnergy(float amount)
        {
            if (currentEnergy >= amount)
            {
                currentEnergy -= amount;
                return true;
            }
            return false;
        }
        
        public float GetHealthPercentage()
        {
            return maxHealth > 0 ? currentHealth / maxHealth : 0;
        }
        
        public float GetShieldPercentage()
        {
            return maxShield > 0 ? currentShield / maxShield : 0;
        }
        
        public float GetEnergyPercentage()
        {
            return maxEnergy > 0 ? currentEnergy / maxEnergy : 0;
        }
        
        public bool IsAlive()
        {
            return currentHealth > 0;
        }
        
        public bool IsFullHealth()
        {
            return currentHealth >= maxHealth;
        }
        
        public bool IsFullShield()
        {
            return currentShield >= maxShield;
        }
        
        public void UpgradeHealth(float multiplier)
        {
            float healthIncrease = maxHealth * (multiplier - 1f);
            maxHealth *= multiplier;
            currentHealth += healthIncrease;
        }
        
        public void UpgradeDamage(float multiplier)
        {
            damage *= multiplier;
        }
        
        public void UpgradeArmor(float multiplier)
        {
            armor *= multiplier;
        }
        
        public UnitStats Clone()
        {
            UnitStats clone = new UnitStats();
            
            clone.maxHealth = maxHealth;
            clone.maxShield = maxShield;
            clone.healthRegen = healthRegen;
            clone.shieldRegen = shieldRegen;
            
            clone.damage = damage;
            clone.attackSpeed = attackSpeed;
            clone.attackRange = attackRange;
            clone.armor = armor;
            clone.penetration = penetration;
            
            clone.movementSpeed = movementSpeed;
            clone.rotationSpeed = rotationSpeed;
            clone.acceleration = acceleration;
            
            clone.sightRange = sightRange;
            clone.detectionRange = detectionRange;
            
            clone.energy = energy;
            clone.maxEnergy = maxEnergy;
            clone.energyRegen = energyRegen;
            
            clone.Initialize();
            
            return clone;
        }
    }
}