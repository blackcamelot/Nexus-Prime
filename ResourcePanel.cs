using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NexusPrime.UI
{
    public class ResourcePanel : MonoBehaviour
    {
        [Header("Resource Display")]
        public Transform resourceContainer;
        public GameObject resourcePrefab;
        public bool showIcons = true;
        public bool showValues = true;
        public bool showProduction = true;
        
        [Header("Animation")]
        public float updateAnimationDuration = 0.5f;
        public Color gainColor = Color.green;
        public Color lossColor = Color.red;
        
        [Header("Layout")]
        public float spacing = 10f;
        public int maxResourcesPerRow = 5;
        
        // Internal
        private Dictionary<ResourceType, ResourceUI> resourceUIs = new Dictionary<ResourceType, ResourceUI>();
        private List<ResourceUI> resourceUIList = new List<ResourceUI>();
        private ResourceManager resourceManager;
        private HorizontalLayoutGroup layoutGroup;
        
        void Start()
        {
            Initialize();
        }
        
        public void Initialize()
        {
            // Get references
            resourceManager = FindObjectOfType<ResourceManager>();
            layoutGroup = resourceContainer.GetComponent<HorizontalLayoutGroup>();
            
            if (layoutGroup != null)
            {
                layoutGroup.spacing = spacing;
            }
            
            // Clear existing
            ClearResources();
            
            // Create resource displays
            CreateResourceDisplays();
            
            // Subscribe to resource changes
            if (resourceManager != null)
            {
                // We'll update manually in Update for now
            }
            
            Debug.Log("Resource Panel Initialized");
        }
        
        void CreateResourceDisplays()
        {
            if (resourcePrefab == null)
            {
                Debug.LogError("Resource prefab not assigned!");
                return;
            }
            
            // Create displays for each resource type
            foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
            {
                CreateResourceDisplay(type);
            }
            
            UpdateLayout();
        }
        
        void CreateResourceDisplay(ResourceType type)
        {
            GameObject resourceObj = Instantiate(resourcePrefab, resourceContainer);
            resourceObj.name = $"{type}_Display";
            
            ResourceUI resourceUI = resourceObj.GetComponent<ResourceUI>();
            if (resourceUI == null)
            {
                resourceUI = resourceObj.AddComponent<ResourceUI>();
            }
            
            resourceUI.Initialize(type);
            resourceUIs[type] = resourceUI;
            resourceUIList.Add(resourceUI);
            
            // Set initial values
            UpdateResourceDisplay(type, 0, 0);
        }
        
        public void UpdateResourceDisplay(Dictionary<ResourceType, int> resources)
        {
            if (resourceUIs.Count == 0) return;
            
            foreach (var kvp in resources)
            {
                if (resourceUIs.ContainsKey(kvp.Key))
                {
                    // Get production rate from resource manager
                    int production = 0;
                    if (resourceManager != null && showProduction)
                    {
                        // This would get actual production rate
                        production = 0; // Placeholder
                    }
                    
                    UpdateResourceDisplay(kvp.Key, kvp.Value, production);
                }
            }
        }
        
        public void UpdateResourceDisplay(ResourceType type, int amount, int production)
        {
            if (!resourceUIs.ContainsKey(type)) return;
            
            ResourceUI resourceUI = resourceUIs[type];
            
            // Check for change
            int oldAmount = resourceUI.GetCurrentAmount();
            bool amountChanged = oldAmount != amount;
            
            // Update display
            resourceUI.UpdateDisplay(amount, production);
            
            // Animate if changed
            if (amountChanged && updateAnimationDuration > 0)
            {
                int difference = amount - oldAmount;
                Color animationColor = difference > 0 ? gainColor : lossColor;
                
                resourceUI.PlayChangeAnimation(difference, animationColor, updateAnimationDuration);
            }
        }
        
        public void UpdateResourceStorage(ResourceType type, int capacity)
        {
            if (resourceUIs.ContainsKey(type))
            {
                resourceUIs[type].UpdateStorage(capacity);
            }
        }
        
        public void HighlightResource(ResourceType type, bool highlight)
        {
            if (resourceUIs.ContainsKey(type))
            {
                resourceUIs[type].SetHighlight(highlight);
            }
        }
        
        public void PulseResource(ResourceType type)
        {
            if (resourceUIs.ContainsKey(type))
            {
                resourceUIs[type].Pulse();
            }
        }
        
        public void ShowResourceTooltip(ResourceType type, bool show)
        {
            if (resourceUIs.ContainsKey(type))
            {
                resourceUIs[type].ShowTooltip(show);
            }
        }
        
        void UpdateLayout()
        {
            // Adjust layout based on screen size
            if (layoutGroup != null)
            {
                // Check if we need to wrap
                if (resourceUIList.Count > maxResourcesPerRow)
                {
                    // Switch to vertical layout or grid
                    // For now, just adjust spacing
                    layoutGroup.spacing = spacing * 0.5f;
                }
            }
            
            // Update each resource UI
            foreach (ResourceUI resourceUI in resourceUIList)
            {
                resourceUI.UpdateLayout(showIcons, showValues, showProduction);
            }
        }
        
        void ClearResources()
        {
            foreach (Transform child in resourceContainer)
            {
                Destroy(child.gameObject);
            }
            
            resourceUIs.Clear();
            resourceUIList.Clear();
        }
        
        public void SetResourceVisibility(ResourceType type, bool visible)
        {
            if (resourceUIs.ContainsKey(type))
            {
                resourceUIs[type].gameObject.SetActive(visible);
            }
        }
        
        public void SetAllResourcesVisibility(bool visible)
        {
            foreach (var kvp in resourceUIs)
            {
                kvp.Value.gameObject.SetActive(visible);
            }
        }
        
        public void UpdateFromResourceManager()
        {
            if (resourceManager == null) return;
            
            foreach (var kvp in resourceManager.resources)
            {
                UpdateResourceDisplay(kvp.Key, kvp.Value.amount, kvp.Value.productionRate);
            }
        }
        
        void Update()
        {
            // Update from resource manager periodically
            if (Time.frameCount % 30 == 0) // Every 30 frames
            {
                UpdateFromResourceManager();
            }
        }
        
        public void OnResourceClicked(ResourceType type)
        {
            // Handle resource click (e.g., show detailed info)
            Debug.Log($"Resource clicked: {type}");
            
            // Show resource info panel
            ShowResourceInfo(type);
        }
        
        void ShowResourceInfo(ResourceType type)
        {
            // Create or show resource info popup
            ResourceInfoPopup.Show(type, GetResourceDescription(type));
        }
        
        string GetResourceDescription(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Credits:
                    return "Universal currency used for construction, unit training, and research.";
                case ResourceType.Energy:
                    return "Power required to operate buildings and advanced units. Generated by power plants.";
                case ResourceType.Nanites:
                    return "Microscopic machines used for advanced construction, repair, and nanotechnology.";
                case ResourceType.Data:
                    return "Information and research points obtained from scanning, research, and capturing objectives.";
                case ResourceType.Influence:
                    return "Political power that affects diplomacy, alliances, and special abilities.";
                default:
                    return "Unknown resource type.";
            }
        }
        
        public Dictionary<ResourceType, ResourceUI> GetResourceUIs()
        {
            return new Dictionary<ResourceType, ResourceUI>(resourceUIs);
        }
        
        public ResourceUI GetResourceUI(ResourceType type)
        {
            return resourceUIs.ContainsKey(type) ? resourceUIs[type] : null;
        }
        
        public void SortResources(System.Comparison<ResourceUI> comparison)
        {
            resourceUIList.Sort(comparison);
            
            // Reorder in hierarchy
            for (int i = 0; i < resourceUIList.Count; i++)
            {
                resourceUIList[i].transform.SetSiblingIndex(i);
            }
        }
        
        public void SortByAmount()
        {
            SortResources((a, b) => b.GetCurrentAmount().CompareTo(a.GetCurrentAmount()));
        }
        
        public void SortByType()
        {
            SortResources((a, b) => a.resourceType.CompareTo(b.resourceType));
        }
        
        public void SortByProduction()
        {
            SortResources((a, b) => b.GetProductionRate().CompareTo(a.GetProductionRate()));
        }
        
        void OnDestroy()
        {
            // Cleanup
            ClearResources();
        }
    }
    
    public class ResourceUI : MonoBehaviour
    {
        [Header("References")]
        public Image iconImage;
        public TextMeshProUGUI amountText;
        public TextMeshProUGUI productionText;
        public Image backgroundImage;
        public GameObject storageBar;
        public Image storageFill;
        
        [Header("Settings")]
        public ResourceType resourceType;
        public Color defaultColor = Color.white;
        public Color highlightColor = Color.yellow;
        public Color warningColor = Color.red;
        
        [Header("Tooltip")]
        public GameObject tooltipObject;
        public TextMeshProUGUI tooltipText;
        
        // Current values
        private int currentAmount = 0;
        private int currentProduction = 0;
        private int currentCapacity = 0;
        
        // Animation
        private Coroutine changeAnimation;
        
        public void Initialize(ResourceType type)
        {
            resourceType = type;
            
            // Load icon
            if (iconImage != null)
            {
                string iconPath = $"Icons/Resources/Icon_{type}";
                Sprite icon = Resources.Load<Sprite>(iconPath);
                if (icon != null)
                {
                    iconImage.sprite = icon;
                }
                else
                {
                    Debug.LogWarning($"Resource icon not found: {iconPath}");
                }
            }
            
            // Set tooltip text
            if (tooltipText != null)
            {
                tooltipText.text = GetResourceName();
            }
            
            // Hide tooltip initially
            if (tooltipObject != null)
            {
                tooltipObject.SetActive(false);
            }
            
            // Set default color
            if (backgroundImage != null)
            {
                backgroundImage.color = defaultColor;
            }
        }
        
        public void UpdateDisplay(int amount, int production)
        {
            currentAmount = amount;
            currentProduction = production;
            
            // Update amount text
            if (amountText != null)
            {
                amountText.text = FormatNumber(amount);
            }
            
            // Update production text
            if (productionText != null)
            {
                if (production > 0)
                {
                    productionText.text = $"+{FormatNumber(production)}/s";
                    productionText.color = Color.green;
                }
                else if (production < 0)
                {
                    productionText.text = $"{FormatNumber(production)}/s";
                    productionText.color = Color.red;
                }
                else
                {
                    productionText.text = "";
                }
            }
            
            // Update storage bar if capacity is set
            if (currentCapacity > 0 && storageBar != null && storageFill != null)
            {
                float fillAmount = Mathf.Clamp01((float)amount / currentCapacity);
                storageFill.fillAmount = fillAmount;
                
                // Color based on fill level
                if (fillAmount > 0.9f)
                {
                    storageFill.color = warningColor;
                }
                else if (fillAmount > 0.7f)
                {
                    storageFill.color = Color.yellow;
                }
                else
                {
                    storageFill.color = Color.green;
                }
            }
        }
        
        public void UpdateStorage(int capacity)
        {
            currentCapacity = capacity;
            
            // Show/hide storage bar
            if (storageBar != null)
            {
                storageBar.SetActive(capacity > 0);
            }
            
            // Update display with current amount
            UpdateDisplay(currentAmount, currentProduction);
        }
        
        public void PlayChangeAnimation(int change, Color animationColor, float duration)
        {
            // Stop existing animation
            if (changeAnimation != null)
            {
                StopCoroutine(changeAnimation);
            }
            
            // Start new animation
            changeAnimation = StartCoroutine(ChangeAnimationCoroutine(change, animationColor, duration));
        }
        
        System.Collections.IEnumerator ChangeAnimationCoroutine(int change, Color animationColor, float duration)
        {
            // Create change text
            GameObject changeTextObj = new GameObject("ChangeText");
            changeTextObj.transform.SetParent(transform, false);
            changeTextObj.transform.localPosition = Vector3.up * 50;
            
            TextMeshProUGUI changeText = changeTextObj.AddComponent<TextMeshProUGUI>();
            changeText.text = change > 0 ? $"+{change}" : $"{change}";
            changeText.color = animationColor;
            changeText.fontSize = 24;
            changeText.alignment = TextAlignmentOptions.Center;
            
            // Animate
            float elapsed = 0;
            Vector3 startPos = changeTextObj.transform.localPosition;
            Vector3 endPos = startPos + Vector3.up * 100;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                changeTextObj.transform.localPosition = Vector3.Lerp(startPos, endPos, t);
                changeText.alpha = Mathf.Lerp(1, 0, t);
                
                yield return null;
            }
            
            // Cleanup
            Destroy(changeTextObj);
            changeAnimation = null;
        }
        
        public void SetHighlight(bool highlight)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = highlight ? highlightColor : defaultColor;
            }
            
            // Also pulse when highlighted
            if (highlight)
            {
                Pulse();
            }
        }
        
        public void Pulse()
        {
            // Simple pulse animation
            StartCoroutine(PulseCoroutine());
        }
        
        System.Collections.IEnumerator PulseCoroutine()
        {
            Transform iconTransform = iconImage.transform;
            Vector3 originalScale = iconTransform.localScale;
            
            // Scale up
            float pulseDuration = 0.2f;
            float elapsed = 0;
            
            while (elapsed < pulseDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / pulseDuration;
                iconTransform.localScale = Vector3.Lerp(originalScale, originalScale * 1.3f, t);
                yield return null;
            }
            
            // Scale back
            elapsed = 0;
            while (elapsed < pulseDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / pulseDuration;
                iconTransform.localScale = Vector3.Lerp(originalScale * 1.3f, originalScale, t);
                yield return null;
            }
            
            iconTransform.localScale = originalScale;
        }
        
        public void ShowTooltip(bool show)
        {
            if (tooltipObject != null)
            {
                tooltipObject.SetActive(show);
            }
        }
        
        public void UpdateLayout(bool showIcon, bool showValue, bool showProduction)
        {
            // Show/hide components based on settings
            if (iconImage != null)
            {
                iconImage.gameObject.SetActive(showIcon);
            }
            
            if (amountText != null)
            {
                amountText.gameObject.SetActive(showValue);
            }
            
            if (productionText != null)
            {
                productionText.gameObject.SetActive(showProduction);
            }
        }
        
        string FormatNumber(int number)
        {
            if (number >= 1000000)
            {
                return $"{(number / 1000000f):F1}M";
            }
            else if (number >= 1000)
            {
                return $"{(number / 1000f):F1}K";
            }
            else
            {
                return number.ToString();
            }
        }
        
        string GetResourceName()
        {
            switch (resourceType)
            {
                case ResourceType.Credits: return "Credits";
                case ResourceType.Energy: return "Energy";
                case ResourceType.Nanites: return "Nanites";
                case ResourceType.Data: return "Data";
                case ResourceType.Influence: return "Influence";
                default: return resourceType.ToString();
            }
        }
        
        public int GetCurrentAmount() => currentAmount;
        public int GetProductionRate() => currentProduction;
        public int GetStorageCapacity() => currentCapacity;
        
        // Event handler for click
        public void OnClick()
        {
            // Notify resource panel
            ResourcePanel panel = GetComponentInParent<ResourcePanel>();
            if (panel != null)
            {
                panel.OnResourceClicked(resourceType);
            }
        }
        
        void OnDestroy()
        {
            // Stop any running coroutines
            if (changeAnimation != null)
            {
                StopCoroutine(changeAnimation);
            }
        }
    }
    
    public class ResourceInfoPopup : MonoBehaviour
    {
        public static void Show(ResourceType type, string description)
        {
            // Load prefab
            GameObject prefab = Resources.Load<GameObject>("UI/ResourceInfoPopup");
            if (prefab == null)
            {
                Debug.LogWarning("ResourceInfoPopup prefab not found!");
                return;
            }
            
            // Create instance
            GameObject popup = Instantiate(prefab, UIManager.Instance.mainCanvas.transform);
            
            // Configure
            ResourceInfoPopup popupScript = popup.GetComponent<ResourceInfoPopup>();
            if (popupScript != null)
            {
                popupScript.SetResource(type, description);
            }
        }
        
        public void SetResource(ResourceType type, string description)
        {
            // Implementation would set UI elements
        }
    }
}