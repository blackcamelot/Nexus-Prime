using System;
using System.Collections;
using UnityEngine;

namespace NexusPrime.Building
{
    public class Building : MonoBehaviour
    {
        [Header("Building Data")]
        public string buildingId;
        public string ownerFaction;
        public BuildingDefinition definition;
        
        [Header("Construction")]
        public float constructionProgress = 0f;
        public bool isUnderConstruction = true;
        public GameObject constructionEffect;
        
        [Header("Health")]
        public float currentHealth;
        public float maxHealth;
        public GameObject healthBar;
        
        [Header("Production")]
        public float productionProgress = 0f;
        public bool isProducing = false;
        public string[] productionQueue;
        public int maxQueueSize = 5;
        
        [Header("Effects")]
        public ParticleSystem operationalEffect;
        public Light operationalLight;
        public AudioSource operationalSound;
        
        // Components
        private ResourceProducer resourceProducer;
        private Collider buildingCollider;
        private Renderer[] renderers;
        
        // Events
        public delegate void BuildingEventHandler(Building building);
        public event BuildingEventHandler OnConstructionStarted;
        public event BuildingEventHandler OnConstructionComplete;
        public event BuildingEventHandler OnDestroyed;
        public event BuildingEventHandler OnProductionComplete;
        
        // State
        private bool isOperational = false;
        private bool isDestroyed = false;
        private Coroutine constructionCoroutine;
        private Coroutine productionCoroutine;
        
        void Awake()
        {
            buildingCollider = GetComponent<Collider>();
            renderers = GetComponentsInChildren<Renderer>();
            resourceProducer = GetComponent<ResourceProducer>();
            
            if (definition != null)
            {
                InitializeFromDefinition();
            }
        }
        
        void Start()
        {
            if (isUnderConstruction)
            {
                StartConstruction();
            }
            else
            {
                CompleteConstruction();
            }
            
            // Register with GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterBuilding(this);
            }
        }
        
        void OnDestroy()
        {
            // Unregister from GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UnregisterBuilding(this);
            }
            
            // Fire destroyed event
            if (!isDestroyed)
            {
                OnDestroyed?.Invoke(this);
            }
        }
        
        void Update()
        {
            if (isDestroyed) return;
            
            UpdateVisuals();
            
            if (isOperational && resourceProducer != null)
            {
                // Update resource production
                UpdateResourceProduction();
            }
        }
        
        public void InitializeFromDefinition()
        {
            buildingId = definition.buildingId;
            maxHealth = definition.health;
            currentHealth = isUnderConstruction ? definition.health * 0.1f : definition.health;
            
            // Setup collider
            if (buildingCollider != null)
            {
                buildingCollider.enabled = !isUnderConstruction;
            }
            
            // Setup production queue
            productionQueue = new string[maxQueueSize];
        }
        
        public void StartConstruction()
        {
            if (constructionCoroutine != null)
                StopCoroutine(constructionCoroutine);
            
            constructionCoroutine = StartCoroutine(ConstructionRoutine());
            
            OnConstructionStarted?.Invoke(this);
            
            Debug.Log($"Started construction: {definition.buildingName}");
        }
        
        private IEnumerator ConstructionRoutine()
        {
            float constructionTime = definition.constructionTime;
            float startHealth = currentHealth;
            
            // Show construction effects
            if (constructionEffect != null)
            {
                constructionEffect.SetActive(true);
            }
            
            while (constructionProgress < 1f)
            {
                constructionProgress += Time.deltaTime / constructionTime;
                constructionProgress = Mathf.Clamp01(constructionProgress);
                
                // Update health during construction
                currentHealth = Mathf.Lerp(startHealth, maxHealth, constructionProgress);
                
                // Update visuals
                UpdateConstructionVisuals();
                
                yield return null;
            }
            
            CompleteConstruction();
        }
        
        public void CompleteConstruction()
        {
            isUnderConstruction = false;
            constructionProgress = 1f;
            currentHealth = maxHealth;
            isOperational = true;
            
            // Enable collider
            if (buildingCollider != null)
            {
                buildingCollider.enabled = true;
            }
            
            // Hide construction effects
            if (constructionEffect != null)
            {
                constructionEffect.SetActive(false);
            }
            
            // Show operational effects
            if (operationalEffect != null)
            {
                operationalEffect.Play();
            }
            
            if (operationalLight != null)
            {
                operationalLight.enabled = true;
            }
            
            if (operationalSound != null)
            {
                operationalSound.Play();
            }
            
            // Start resource production if applicable
            if (resourceProducer != null)
            {
                resourceProducer.SetActive(true);
            }
            
            // Fire completion event
            OnConstructionComplete?.Invoke(this);
            
            Debug.Log($"Construction complete: {definition.buildingName}");
        }
        
        public void TakeDamage(float damage)
        {
            if (isDestroyed || !isOperational) return;
            
            currentHealth -= damage;
            currentHealth = Mathf.Max(0, currentHealth);
            
            // Show damage effect
            StartCoroutine(DamageEffect());
            
            if (currentHealth <= 0)
            {
                DestroyBuilding();
            }
        }
        
        public void Repair(float amount)
        {
            if (isDestroyed) return;
            
            currentHealth += amount;
            currentHealth = Mathf.Min(currentHealth, maxHealth);
            
            // Show repair effect
            StartCoroutine(RepairEffect());
        }
        
        public void DestroyBuilding()
        {
            if (isDestroyed) return;
            
            isDestroyed = true;
            isOperational = false;
            
            // Stop all coroutines
            if (constructionCoroutine != null)
                StopCoroutine(constructionCoroutine);
            
            if (productionCoroutine != null)
                StopCoroutine(productionCoroutine);
            
            // Stop effects
            if (operationalEffect != null)
            {
                operationalEffect.Stop();
            }
            
            if (operationalLight != null)
            {
                operationalLight.enabled = false;
            }
            
            if (operationalSound != null)
            {
                operationalSound.Stop();
            }
            
            // Stop resource production
            if (resourceProducer != null)
            {
                resourceProducer.SetActive(false);
            }
            
            // Play destruction effect
            StartCoroutine(DestructionEffect());
            
            // Fire destroyed event
            OnDestroyed?.Invoke(this);
            
            Debug.Log($"Building destroyed: {definition.buildingName}");
        }
        
        public void StartProduction(string unitId)
        {
            if (!isOperational || isProducing) return;
            
            // Check if there's space in the queue
            if (GetQueueCount() >= maxQueueSize)
            {
                Debug.LogWarning("Production queue is full");
                return;
            }
            
            // Add to queue
            for (int i = 0; i < productionQueue.Length; i++)
            {
                if (string.IsNullOrEmpty(productionQueue[i]))
                {
                    productionQueue[i] = unitId;
                    break;
                }
            }
            
            // Start production if not already producing
            if (!isProducing)
            {
                productionCoroutine = StartCoroutine(ProductionRoutine());
            }
        }
        
        private IEnumerator ProductionRoutine()
        {
            isProducing = true;
            
            while (HasItemsInQueue())
            {
                string unitId = productionQueue[0];
                
                // Get production time
                float productionTime = GetProductionTime(unitId);
                
                // Start production
                productionProgress = 0f;
                
                while (productionProgress < 1f)
                {
                    productionProgress += Time.deltaTime / productionTime;
                    productionProgress = Mathf.Clamp01(productionProgress);
                    
                    // Update production visuals
                    UpdateProductionVisuals();
                    
                    yield return null;
                }
                
                // Production complete
                CompleteProduction(unitId);
                
                // Shift queue
                ShiftProductionQueue();
            }
            
            isProducing = false;
            productionCoroutine = null;
        }
        
        private void CompleteProduction(string unitId)
        {
            // Create unit
            // This would typically call a UnitFactory
            
            // Fire event
            OnProductionComplete?.Invoke(this);
            
            Debug.Log($"Production complete: {unitId}");
        }
        
        private void ShiftProductionQueue()
        {
            for (int i = 0; i < productionQueue.Length - 1; i++)
            {
                productionQueue[i] = productionQueue[i + 1];
            }
            productionQueue[productionQueue.Length - 1] = null;
        }
        
        private bool HasItemsInQueue()
        {
            foreach (string item in productionQueue)
            {
                if (!string.IsNullOrEmpty(item))
                    return true;
            }
            return false;
        }
        
        private int GetQueueCount()
        {
            int count = 0;
            foreach (string item in productionQueue)
            {
                if (!string.IsNullOrEmpty(item))
                    count++;
            }
            return count;
        }
        
        private float GetProductionTime(string unitId)
        {
            // Get production time from definition
            // This would typically come from a UnitFactory
            return 10f; // Default
        }
        
        private void UpdateResourceProduction()
        {
            // Update resource producer efficiency based on health
            if (resourceProducer != null)
            {
                float healthPercentage = currentHealth / maxHealth;
                resourceProducer.SetEfficiency(healthPercentage);
                
                // Disable if health is too low
                if (healthPercentage < 0.3f)
                {
                    resourceProducer.SetActive(false);
                }
                else
                {
                    resourceProducer.SetActive(true);
                }
            }
        }
        
        private void UpdateVisuals()
        {
            UpdateHealthBar();
            UpdateConstructionVisuals();
            UpdateProductionVisuals();
        }
        
        private void UpdateHealthBar()
        {
            if (healthBar != null)
            {
                bool shouldShow = currentHealth < maxHealth || isUnderConstruction;
                healthBar.SetActive(shouldShow);
                
                if (shouldShow)
                {
                    // Update health bar fill
                    // This depends on your health bar implementation
                }
            }
        }
        
        private void UpdateConstructionVisuals()
        {
            if (isUnderConstruction)
            {
                // Visual feedback for construction progress
                foreach (Renderer renderer in renderers)
                {
                    if (renderer != null)
                    {
                        // Could change material based on construction progress
                        float alpha = Mathf.Lerp(0.5f, 1f, constructionProgress);
                        Color color = renderer.material.color;
                        color.a = alpha;
                        renderer.material.color = color;
                    }
                }
            }
        }
        
        private void UpdateProductionVisuals()
        {
            if (isProducing)
            {
                // Visual feedback for production progress
                // Could pulse lights or play particles
            }
        }
        
        private IEnumerator DamageEffect()
        {
            // Flash red
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                {
                    Color originalColor = renderer.material.color;
                    renderer.material.color = Color.red;
                    
                    yield return new WaitForSeconds(0.1f);
                    
                    renderer.material.color = originalColor;
                }
            }
        }
        
        private IEnumerator RepairEffect()
        {
            // Flash green
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                {
                    Color originalColor = renderer.material.color;
                    renderer.material.color = Color.green;
                    
                    yield return new WaitForSeconds(0.1f);
                    
                    renderer.material.color = originalColor;
                }
            }
        }
        
        private IEnumerator DestructionEffect()
        {
            // Play destruction animation
            // This would typically be a particle effect
            
            // Disable renderers gradually
            float timer = 0f;
            float duration = 2f;
            
            while (timer < duration)
            {
                timer += Time.deltaTime;
                
                foreach (Renderer renderer in renderers)
                {
                    if (renderer != null)
                    {
                        Color color = renderer.material.color;
                        color.a = Mathf.Lerp(1f, 0f, timer / duration);
                        renderer.material.color = color;
                    }
                }
                
                yield return null;
            }
            
            // Destroy game object
            Destroy(gameObject);
        }
        
        public bool IsOperational()
        {
            return isOperational && !isDestroyed;
        }
        
        public bool IsUnderConstruction()
        {
            return isUnderConstruction;
        }
        
        public bool IsDestroyed()
        {
            return isDestroyed;
        }
        
        public float GetHealthPercentage()
        {
            return maxHealth > 0 ? currentHealth / maxHealth : 0;
        }
        
        public float GetConstructionPercentage()
        {
            return constructionProgress;
        }
        
        public float GetProductionPercentage()
        {
            return productionProgress;
        }
        
        public string[] GetProductionQueue()
        {
            return productionQueue;
        }
        
        public void CancelProduction(int index)
        {
            if (index >= 0 && index < productionQueue.Length)
            {
                productionQueue[index] = null;
                
                // If cancelling current item, stop production
                if (index == 0 && isProducing)
                {
                    if (productionCoroutine != null)
                        StopCoroutine(productionCoroutine);
                    
                    isProducing = false;
                    productionProgress = 0f;
                    
                    // Start next item if available
                    if (HasItemsInQueue())
                    {
                        productionCoroutine = StartCoroutine(ProductionRoutine());
                    }
                }
            }
        }
        
        public void Upgrade(float healthMultiplier, float productionSpeedMultiplier)
        {
            maxHealth *= healthMultiplier;
            currentHealth *= healthMultiplier;
            
            // Upgrade production speed
            // This would affect GetProductionTime()
            
            Debug.Log($"Building upgraded: {definition.buildingName}");
        }
        
        void OnDrawGizmosSelected()
        {
            if (definition != null)
            {
                // Draw building bounds
                Gizmos.color = IsOperational() ? Color.green : Color.yellow;
                Gizmos.DrawWireCube(
                    transform.position + new Vector3(
                        definition.width / 2f,
                        definition.height / 2f,
                        definition.depth / 2f
                    ),
                    new Vector3(
                        definition.width,
                        definition.height,
                        definition.depth
                    )
                );
                
                // Draw influence radius if applicable
                if (definition.influenceRadius > 0)
                {
                    Gizmos.color = new Color(0, 1, 1, 0.3f);
                    Gizmos.DrawWireSphere(transform.position, definition.influenceRadius);
                }
            }
        }
    }
}