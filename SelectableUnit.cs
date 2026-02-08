using UnityEngine;
using UnityEngine.Events;

namespace NexusPrime.Units
{
    [RequireComponent(typeof(Collider))]
    public class SelectableUnit : MonoBehaviour
    {
        [Header("Selection Settings")]
        public string unitId;
        public string displayName;
        public string description;
        public Sprite icon;
        
        [Header("Visual Feedback")]
        public GameObject selectionIndicator;
        public Material selectedMaterial;
        public Material normalMaterial;
        public ParticleSystem selectionParticles;
        
        [Header("Health Display")]
        public GameObject healthBarPrefab;
        public Vector3 healthBarOffset = new Vector3(0, 2, 0);
        
        [Header("Events")]
        public UnityEvent onSelected;
        public UnityEvent onDeselected;
        public UnityEvent onCommandReceived;
        
        // Internal state
        private bool isSelected = false;
        private bool isHighlighted = false;
        private Renderer[] renderers;
        private GameObject healthBarInstance;
        private HealthBar healthBar;
        
        // References
        private UnitStats unitStats;
        private CombatUnit combatUnit;
        
        // Events
        public delegate void SelectionHandler(SelectableUnit unit, bool selected);
        public event SelectionHandler OnSelected;
        public event SelectionHandler OnDeselected;
        public event SelectionHandler OnMovementStarted;
        
        void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>();
            unitStats = GetComponent<UnitStats>();
            combatUnit = GetComponent<CombatUnit>();
        }
        
        void Start()
        {
            InitializeSelectionIndicator();
            InitializeHealthBar();
            
            // Register with GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterUnit(this);
            }
        }
        
        void OnDestroy()
        {
            // Unregister from GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UnregisterUnit(this);
            }
            
            // Clean up health bar
            if (healthBarInstance != null)
            {
                Destroy(healthBarInstance);
            }
        }
        
        void Update()
        {
            UpdateHealthBar();
            UpdateSelectionIndicator();
        }
        
        void OnMouseEnter()
        {
            Highlight(true);
        }
        
        void OnMouseExit()
        {
            Highlight(false);
        }
        
        void OnMouseDown()
        {
            if (Input.GetMouseButtonDown(0))
            {
                ToggleSelection();
            }
        }
        
        public void Select()
        {
            if (isSelected) return;
            
            isSelected = true;
            UpdateVisuals();
            
            onSelected?.Invoke();
            OnSelected?.Invoke(this, true);
            
            Debug.Log($"Unit selected: {displayName}");
        }
        
        public void Deselect()
        {
            if (!isSelected) return;
            
            isSelected = false;
            UpdateVisuals();
            
            onDeselected?.Invoke();
            OnDeselected?.Invoke(this, false);
        }
        
        public void ToggleSelection()
        {
            if (isSelected)
            {
                Deselect();
            }
            else
            {
                Select();
            }
        }
        
        public void Highlight(bool highlight)
        {
            isHighlighted = highlight;
            UpdateVisuals();
        }
        
        public void ShowDamageEffect(float damage)
        {
            // Create floating damage text
            if (UIManager.Instance != null)
            {
                UIManager.Instance.CreateFloatingText(
                    transform.position + Vector3.up * 2,
                    $"-{damage:F0}",
                    Color.red
                );
            }
            
            // Flash red
            StartCoroutine(FlashColor(Color.red, 0.1f));
        }
        
        public void ShowHealEffect(float amount)
        {
            // Create floating heal text
            if (UIManager.Instance != null)
            {
                UIManager.Instance.CreateFloatingText(
                    transform.position + Vector3.up * 2,
                    $"+{amount:F0}",
                    Color.green
                );
            }
            
            // Flash green
            StartCoroutine(FlashColor(Color.green, 0.1f));
        }
        
        public void PlayDeathAnimation()
        {
            // Trigger death animation
            Animator animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("Die");
            }
            
            // Disable collider
            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
            
            // Hide selection indicator
            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(false);
            }
            
            // Hide health bar
            if (healthBarInstance != null)
            {
                healthBarInstance.SetActive(false);
            }
        }
        
        public void SetCombatState(bool inCombat)
        {
            // Change visual state when in combat
            if (selectionIndicator != null)
            {
                ParticleSystem.MainModule main = selectionIndicator.GetComponent<ParticleSystem>().main;
                main.startColor = inCombat ? Color.red : Color.cyan;
            }
        }
        
        public void SetAttackState(bool attacking)
        {
            // Visual feedback for attacking
            // Could change material or play particles
        }
        
        private System.Collections.IEnumerator FlashColor(Color color, float duration)
        {
            Color[] originalColors = new Color[renderers.Length];
            
            // Store original colors
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    originalColors[i] = renderers[i].material.color;
                    renderers[i].material.color = color;
                }
            }
            
            yield return new WaitForSeconds(duration);
            
            // Restore original colors
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].material.color = originalColors[i];
                }
            }
        }
        
        private void InitializeSelectionIndicator()
        {
            if (selectionIndicator == null)
            {
                // Create default selection indicator
                selectionIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                selectionIndicator.transform.SetParent(transform);
                selectionIndicator.transform.localPosition = Vector3.zero;
                selectionIndicator.transform.localScale = new Vector3(1.2f, 0.1f, 1.2f);
                
                Renderer renderer = selectionIndicator.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = new Material(Shader.Find("Standard"));
                    renderer.material.color = new Color(0, 1, 1, 0.3f);
                    renderer.material.SetFloat("_Mode", 3); // Transparent mode
                }
                
                Destroy(selectionIndicator.GetComponent<Collider>());
            }
            
            selectionIndicator.SetActive(false);
        }
        
        private void InitializeHealthBar()
        {
            if (healthBarPrefab != null && unitStats != null)
            {
                healthBarInstance = Instantiate(healthBarPrefab, transform);
                healthBarInstance.transform.localPosition = healthBarOffset;
                
                healthBar = healthBarInstance.GetComponent<HealthBar>();
                if (healthBar != null)
                {
                    healthBar.Initialize(unitStats);
                }
            }
        }
        
        private void UpdateVisuals()
        {
            // Update selection indicator
            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(isSelected || isHighlighted);
                
                if (isSelected)
                {
                    // Selected - cyan
                    Renderer renderer = selectionIndicator.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.color = new Color(0, 1, 1, 0.5f);
                    }
                }
                else if (isHighlighted)
                {
                    // Highlighted - yellow
                    Renderer renderer = selectionIndicator.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.color = new Color(1, 1, 0, 0.3f);
                    }
                }
            }
            
            // Update material
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                {
                    if (isSelected && selectedMaterial != null)
                    {
                        renderer.material = selectedMaterial;
                    }
                    else if (normalMaterial != null)
                    {
                        renderer.material = normalMaterial;
                    }
                }
            }
            
            // Play selection particles
            if (selectionParticles != null)
            {
                if (isSelected && !selectionParticles.isPlaying)
                {
                    selectionParticles.Play();
                }
                else if (!isSelected && selectionParticles.isPlaying)
                {
                    selectionParticles.Stop();
                }
            }
        }
        
        private void UpdateSelectionIndicator()
        {
            if (selectionIndicator != null && selectionIndicator.activeSelf)
            {
                // Make indicator face camera
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    selectionIndicator.transform.LookAt(mainCamera.transform);
                    selectionIndicator.transform.Rotate(0, 180, 0);
                }
            }
        }
        
        private void UpdateHealthBar()
        {
            if (healthBar != null && unitStats != null)
            {
                healthBar.UpdateHealth(unitStats.GetHealthPercentage());
                
                if (unitStats.maxShield > 0)
                {
                    healthBar.UpdateShield(unitStats.GetShieldPercentage());
                }
                
                // Show/hide based on health
                if (unitStats.IsAlive() && unitStats.GetHealthPercentage() < 1f)
                {
                    healthBarInstance.SetActive(true);
                }
                else
                {
                    healthBarInstance.SetActive(false);
                }
            }
        }
        
        public bool IsAlive()
        {
            return unitStats != null && unitStats.IsAlive();
        }
        
        public string GetUnitInfo()
        {
            string info = $"{displayName}\n";
            
            if (unitStats != null)
            {
                info += $"Health: {unitStats.currentHealth:F0}/{unitStats.maxHealth:F0}\n";
                if (unitStats.maxShield > 0)
                {
                    info += $"Shield: {unitStats.currentShield:F0}/{unitStats.maxShield:F0}\n";
                }
                
                if (combatUnit != null)
                {
                    info += $"Damage: {unitStats.damage:F0}\n";
                    info += $"Range: {unitStats.attackRange:F0}m";
                }
            }
            
            return info;
        }
    }
}