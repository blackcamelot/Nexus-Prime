using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace NexusPrime.UI
{
    public class TechTreeUI : MonoBehaviour
    {
        [Header("UI References")]
        public CanvasGroup canvasGroup;
        public RectTransform techTreeContainer;
        public GameObject techNodePrefab;
        public GameObject connectionLinePrefab;
        public ScrollRect scrollRect;
        
        [Header("Info Panel")]
        public GameObject infoPanel;
        public TextMeshProUGUI techNameText;
        public TextMeshProUGUI techDescriptionText;
        public TextMeshProUGUI techCostText;
        public TextMeshProUGUI techTimeText;
        public TextMeshProUGUI techRequirementsText;
        public TextMeshProUGUI techUnlocksText;
        public Image techIcon;
        public Button researchButton;
        public Button cancelButton;
        public TextMeshProUGUI researchProgressText;
        public Image researchProgressBar;
        
        [Header("Categories")]
        public Transform categoryTabsContainer;
        public GameObject categoryTabPrefab;
        public Color selectedCategoryColor = Color.cyan;
        public Color normalCategoryColor = Color.gray;
        
        [Header("Visual Settings")]
        public float nodeSpacing = 200f;
        public float tierSpacing = 150f;
        public Color availableColor = new Color(0, 1, 0, 0.3f);
        public Color unavailableColor = new Color(1, 0, 0, 0.3f);
        public Color researchedColor = new Color(0, 0.5f, 1, 0.3f);
        public Color researchingColor = new Color(1, 1, 0, 0.3f);
        public Color connectionColor = new Color(0.2f, 0.8f, 1f, 0.5f);
        
        [Header("Animation")]
        public float fadeDuration = 0.3f;
        public float nodeAppearDuration = 0.5f;
        
        // References
        private TechTreeSystem techTreeSystem;
        private ResearchManager researchManager;
        
        // UI State
        private Dictionary<string, TechNodeUI> techNodeUIs = new Dictionary<string, TechNodeUI>();
        private List<GameObject> connectionLines = new List<GameObject>();
        private Dictionary<TechCategory, List<string>> techsByCategory = new Dictionary<TechCategory, List<string>>();
        private TechCategory currentCategory = TechCategory.General;
        private Technology selectedTech;
        
        // Animation
        private bool isAnimating = false;
        private Sequence showSequence;
        
        void Start()
        {
            Initialize();
        }
        
        void Initialize()
        {
            // Get references
            techTreeSystem = FindObjectOfType<TechTreeSystem>();
            researchManager = FindObjectOfType<ResearchManager>();
            
            if (techTreeSystem == null)
            {
                Debug.LogError("TechTreeSystem not found!");
                return;
            }
            
            // Initialize categories
            InitializeCategories();
            
            // Build tech tree
            BuildTechTree();
            
            // Hide initially
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
            
            // Hide info panel
            if (infoPanel != null)
            {
                infoPanel.SetActive(false);
            }
            
            // Subscribe to events
            if (techTreeSystem != null)
            {
                techTreeSystem.OnResearchStarted += OnResearchStarted;
                techTreeSystem.OnResearchCompleted += OnResearchCompleted;
                techTreeSystem.OnResearchProgress += OnResearchProgress;
            }
            
            Debug.Log("Tech Tree UI Initialized");
        }
        
        void InitializeCategories()
        {
            // Clear existing tabs
            foreach (Transform child in categoryTabsContainer)
            {
                Destroy(child.gameObject);
            }
            
            techsByCategory.Clear();
            
            // Initialize categories
            foreach (TechCategory category in System.Enum.GetValues(typeof(TechCategory)))
            {
                techsByCategory[category] = new List<string>();
            }
            
            // Create category tabs
            int tabIndex = 0;
            foreach (TechCategory category in System.Enum.GetValues(typeof(TechCategory)))
            {
                if (category == TechCategory.General) continue; // Skip general, it's always first
                
                CreateCategoryTab(category, tabIndex);
                tabIndex++;
            }
            
            // Select first category
            SelectCategory(TechCategory.General);
        }
        
        void CreateCategoryTab(TechCategory category, int index)
        {
            if (categoryTabPrefab == null) return;
            
            GameObject tabObj = Instantiate(categoryTabPrefab, categoryTabsContainer);
            tabObj.name = $"Tab_{category}";
            
            // Set position
            RectTransform rect = tabObj.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(index * 120, 0);
            
            // Set text
            TextMeshProUGUI text = tabObj.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = GetCategoryDisplayName(category);
            }
            
            // Set button
            Button button = tabObj.GetComponent<Button>();
            if (button != null)
            {
                TechCategory cat = category; // Capture for lambda
                button.onClick.AddListener(() => SelectCategory(cat));
            }
            
            // Set initial color
            Image image = tabObj.GetComponent<Image>();
            if (image != null)
            {
                image.color = normalCategoryColor;
            }
        }
        
        void BuildTechTree()
        {
            if (techTreeSystem == null) return;
            
            // Clear existing
            ClearTechTree();
            
            // Get all technologies
            List<Technology> allTechs = techTreeSystem.GetAvailableTechnologies();
            allTechs.AddRange(techTreeSystem.GetCompletedTechnologies());
            
            // Group by category
            foreach (Technology tech in allTechs)
            {
                if (!techsByCategory.ContainsKey(tech.category))
                {
                    techsByCategory[tech.category] = new List<string>();
                }
                
                if (!techsByCategory[tech.category].Contains(tech.techId))
                {
                    techsByCategory[tech.category].Add(tech.techId);
                }
            }
            
            // Create nodes for current category
            CreateTechNodes(currentCategory);
            
            // Create connections
            CreateConnections();
            
            // Layout nodes
            LayoutTechTree();
        }
        
        void CreateTechNodes(TechCategory category)
        {
            if (!techsByCategory.ContainsKey(category)) return;
            if (techNodePrefab == null) return;
            
            foreach (string techId in techsByCategory[category])
            {
                Technology tech = techTreeSystem.GetTechnology(techId);
                if (tech == null) continue;
                
                // Create node
                GameObject nodeObj = Instantiate(techNodePrefab, techTreeContainer);
                nodeObj.name = $"Node_{techId}";
                
                // Add TechNodeUI component
                TechNodeUI nodeUI = nodeObj.GetComponent<TechNodeUI>();
                if (nodeUI == null)
                {
                    nodeUI = nodeObj.AddComponent<TechNodeUI>();
                }
                
                // Initialize node
                nodeUI.Initialize(tech, OnTechNodeClicked);
                
                // Add to dictionary
                techNodeUIs[techId] = nodeUI;
                
                // Set initial state
                UpdateNodeState(techId);
            }
        }
        
        void CreateConnections()
        {
            if (connectionLinePrefab == null) return;
            
            // Clear existing connections
            foreach (GameObject line in connectionLines)
            {
                Destroy(line);
            }
            connectionLines.Clear();
            
            // Create connections between technologies
            foreach (var kvp in techNodeUIs)
            {
                Technology tech = techTreeSystem.GetTechnology(kvp.Key);
                if (tech == null) continue;
                
                foreach (string requiredTech in tech.requiredTechs)
                {
                    if (techNodeUIs.ContainsKey(requiredTech))
                    {
                        CreateConnectionLine(requiredTech, kvp.Key);
                    }
                }
            }
        }
        
        void CreateConnectionLine(string fromTechId, string toTechId)
        {
            if (!techNodeUIs.ContainsKey(fromTechId) || !techNodeUIs.ContainsKey(toTechId)) return;
            
            TechNodeUI fromNode = techNodeUIs[fromTechId];
            TechNodeUI toNode = techNodeUIs[toTechId];
            
            GameObject lineObj = Instantiate(connectionLinePrefab, techTreeContainer);
            lineObj.name = $"Connection_{fromTechId}_to_{toTechId}";
            
            // Get line renderer or UI image
            Image lineImage = lineObj.GetComponent<Image>();
            if (lineImage != null)
            {
                // Position and rotate line between nodes
                Vector3 fromPos = fromNode.transform.position;
                Vector3 toPos = toNode.transform.position;
                
                lineObj.transform.position = (fromPos + toPos) / 2;
                
                Vector3 direction = toPos - fromPos;
                float distance = direction.magnitude;
                
                lineImage.rectTransform.sizeDelta = new Vector2(distance, 5f);
                lineImage.rectTransform.rotation = Quaternion.FromToRotation(Vector3.right, direction);
                
                // Set color
                lineImage.color = connectionColor;
            }
            
            connectionLines.Add(lineObj);
            
            // Send to back
            lineObj.transform.SetAsFirstSibling();
        }
        
        void LayoutTechTree()
        {
            // Simple grid layout based on tech tiers
            Dictionary<int, List<TechNodeUI>> nodesByTier = new Dictionary<int, List<TechNodeUI>>();
            
            // Group nodes by tier
            foreach (var kvp in techNodeUIs)
            {
                Technology tech = techTreeSystem.GetTechnology(kvp.Key);
                if (tech == null) continue;
                
                int tier = CalculateTechTier(tech);
                
                if (!nodesByTier.ContainsKey(tier))
                {
                    nodesByTier[tier] = new List<TechNodeUI>();
                }
                
                nodesByTier[tier].Add(kvp.Value);
            }
            
            // Position nodes
            float startX = -800f;
            float startY = 300f;
            
            foreach (var kvp in nodesByTier)
            {
                int tier = kvp.Key;
                List<TechNodeUI> nodes = kvp.Value;
                
                float x = startX + (tier * nodeSpacing);
                float ySpacing = tierSpacing / Mathf.Max(1, nodes.Count);
                
                for (int i = 0; i < nodes.Count; i++)
                {
                    float y = startY - (i * ySpacing);
                    
                    // Animate to position
                    RectTransform rect = nodes[i].GetComponent<RectTransform>();
                    rect.anchoredPosition = new Vector2(x, y);
                    
                    // Store position for connections
                    techTreeSystem.GetTechnology(nodes[i].techId).treePosition = new Vector2(x, y);
                }
            }
            
            // Update connections after positioning
            UpdateConnections();
        }
        
        int CalculateTechTier(Technology tech)
        {
            // Calculate tier based on requirements
            int maxTier = 0;
            
            foreach (string requiredTech in tech.requiredTechs)
            {
                Technology reqTech = techTreeSystem.GetTechnology(requiredTech);
                if (reqTech != null)
                {
                    int reqTier = CalculateTechTier(reqTech) + 1;
                    maxTier = Mathf.Max(maxTier, reqTier);
                }
            }
            
            return maxTier;
        }
        
        void UpdateNodeState(string techId)
        {
            if (!techNodeUIs.ContainsKey(techId)) return;
            
            TechNodeUI nodeUI = techNodeUIs[techId];
            
            // Determine state
            bool isResearched = techTreeSystem.IsResearched(techId);
            bool isResearching = techTreeSystem.IsResearching(techId);
            bool isAvailable = techTreeSystem.IsAvailable(techId);
            
            // Update node appearance
            if (isResearched)
            {
                nodeUI.SetState(TechNodeState.Researched, researchedColor);
            }
            else if (isResearching)
            {
                nodeUI.SetState(TechNodeState.Researching, researchingColor);
                
                // Update progress
                float progress = techTreeSystem.GetResearchProgress(techId);
                nodeUI.SetProgress(progress);
            }
            else if (isAvailable)
            {
                nodeUI.SetState(TechNodeState.Available, availableColor);
            }
            else
            {
                nodeUI.SetState(TechNodeState.Unavailable, unavailableColor);
            }
            
            // Update connections
            UpdateConnections();
        }
        
        void UpdateConnections()
        {
            // Update connection colors based on state
            foreach (GameObject line in connectionLines)
            {
                Image lineImage = line.GetComponent<Image>();
                if (lineImage != null)
                {
                    // Determine connection state
                    string[] parts = line.name.Split('_');
                    if (parts.Length >= 4)
                    {
                        string fromTech = parts[1];
                        string toTech = parts[3];
                        
                        bool fromResearched = techTreeSystem.IsResearched(fromTech);
                        bool toResearched = techTreeSystem.IsResearched(toTech);
                        
                        if (fromResearched && toResearched)
                        {
                            lineImage.color = researchedColor;
                        }
                        else if (fromResearched)
                        {
                            lineImage.color = availableColor;
                        }
                        else
                        {
                            lineImage.color = unavailableColor;
                        }
                    }
                }
            }
        }
        
        void OnTechNodeClicked(Technology tech)
        {
            selectedTech = tech;
            ShowTechInfo(tech);
        }
        
        void ShowTechInfo(Technology tech)
        {
            if (infoPanel == null) return;
            
            infoPanel.SetActive(true);
            
            // Set tech info
            if (techNameText != null)
                techNameText.text = tech.techName;
            
            if (techDescriptionText != null)
                techDescriptionText.text = tech.description;
            
            if (techCostText != null)
                techCostText.text = $"Cost: {tech.GetCostString()}";
            
            if (techTimeText != null)
                techTimeText.text = $"Research Time: {tech.GetResearchTimeString()}";
            
            if (techRequirementsText != null)
                techRequirementsText.text = $"Requirements: {tech.GetRequirementsString()}";
            
            if (techUnlocksText != null)
                techUnlocksText.text = $"Unlocks: {tech.GetUnlocksString()}";
            
            if (techIcon != null && tech.icon != null)
                techIcon.sprite = tech.icon;
            
            // Update button states
            UpdateResearchButton();
        }
        
        void UpdateResearchButton()
        {
            if (selectedTech == null || researchButton == null) return;
            
            bool isResearched = techTreeSystem.IsResearched(selectedTech.techId);
            bool isResearching = techTreeSystem.IsResearching(selectedTech.techId);
            bool isAvailable = techTreeSystem.IsAvailable(selectedTech.techId);
            
            if (isResearched)
            {
                researchButton.interactable = false;
                researchButton.GetComponentInChildren<TextMeshProUGUI>().text = "Researched";
            }
            else if (isResearching)
            {
                researchButton.interactable = true;
                researchButton.GetComponentInChildren<TextMeshProUGUI>().text = "Cancel Research";
                
                // Show progress
                if (researchProgressText != null && researchProgressBar != null)
                {
                    float progress = techTreeSystem.GetResearchProgress(selectedTech.techId);
                    researchProgressText.text = $"{progress * 100:F1}%";
                    researchProgressBar.fillAmount = progress;
                    
                    researchProgressText.gameObject.SetActive(true);
                    researchProgressBar.gameObject.SetActive(true);
                }
            }
            else if (isAvailable)
            {
                researchButton.interactable = true;
                researchButton.GetComponentInChildren<TextMeshProUGUI>().text = "Start Research";
                
                // Hide progress
                if (researchProgressText != null && researchProgressBar != null)
                {
                    researchProgressText.gameObject.SetActive(false);
                    researchProgressBar.gameObject.SetActive(false);
                }
            }
            else
            {
                researchButton.interactable = false;
                researchButton.GetComponentInChildren<TextMeshProUGUI>().text = "Requirements Not Met";
            }
        }
        
        public void OnResearchButtonClicked()
        {
            if (selectedTech == null) return;
            
            if (techTreeSystem.IsResearching(selectedTech.techId))
            {
                // Cancel research
                techTreeSystem.CancelResearch(selectedTech.techId);
            }
            else
            {
                // Start research
                techTreeSystem.StartResearch(selectedTech.techId);
            }
            
            UpdateResearchButton();
        }
        
        void SelectCategory(TechCategory category)
        {
            currentCategory = category;
            
            // Update tab colors
            UpdateCategoryTabs();
            
            // Rebuild tech tree for selected category
            ClearTechTree();
            CreateTechNodes(category);
            CreateConnections();
            LayoutTechTree();
            
            // Hide info panel
            if (infoPanel != null)
            {
                infoPanel.SetActive(false);
            }
        }
        
        void UpdateCategoryTabs()
        {
            int tabIndex = 0;
            foreach (Transform child in categoryTabsContainer)
            {
                TechCategory category = (TechCategory)(tabIndex + 1); // +1 to skip General
                
                Image image = child.GetComponent<Image>();
                if (image != null)
                {
                    image.color = (category == currentCategory) ? selectedCategoryColor : normalCategoryColor;
                }
                
                tabIndex++;
            }
        }
        
        string GetCategoryDisplayName(TechCategory category)
        {
            switch (category)
            {
                case TechCategory.Construction: return "Construction";
                case TechCategory.Economy: return "Economy";
                case TechCategory.Military: return "Military";
                case TechCategory.Defense: return "Defense";
                case TechCategory.Energy: return "Energy";
                case TechCategory.Cybernetics: return "Cybernetics";
                case TechCategory.Nanotechnology: return "Nanotech";
                case TechCategory.ArtificialIntelligence: return "AI";
                case TechCategory.Space: return "Space";
                default: return category.ToString();
            }
        }
        
        void ClearTechTree()
        {
            // Clear nodes
            foreach (var kvp in techNodeUIs)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }
            techNodeUIs.Clear();
            
            // Clear connections
            foreach (GameObject line in connectionLines)
            {
                Destroy(line);
            }
            connectionLines.Clear();
        }
        
        public void ShowTechTree()
        {
            if (isAnimating) return;
            
            isAnimating = true;
            
            // Update tech tree display
            UpdateTechTreeDisplay();
            
            // Show with animation
            if (canvasGroup != null)
            {
                showSequence = DOTween.Sequence();
                showSequence.Append(canvasGroup.DOFade(1, fadeDuration));
                showSequence.OnComplete(() =>
                {
                    canvasGroup.blocksRaycasts = true;
                    canvasGroup.interactable = true;
                    isAnimating = false;
                });
                showSequence.Play();
            }
            else
            {
                gameObject.SetActive(true);
                isAnimating = false;
            }
            
            // Pause game
            Time.timeScale = 0f;
        }
        
        public void HideTechTree()
        {
            if (isAnimating) return;
            
            isAnimating = true;
            
            // Hide with animation
            if (canvasGroup != null)
            {
                Sequence hideSequence = DOTween.Sequence();
                hideSequence.Append(canvasGroup.DOFade(0, fadeDuration));
                hideSequence.OnComplete(() =>
                {
                    canvasGroup.blocksRaycasts = false;
                    canvasGroup.interactable = false;
                    isAnimating = false;
                });
                hideSequence.Play();
            }
            else
            {
                gameObject.SetActive(false);
                isAnimating = false;
            }
            
            // Resume game
            Time.timeScale = 1f;
        }
        
        public void UpdateTechTreeDisplay()
        {
            // Update all node states
            foreach (var kvp in techNodeUIs)
            {
                UpdateNodeState(kvp.Key);
            }
            
            // Update selected tech info if shown
            if (selectedTech != null)
            {
                ShowTechInfo(selectedTech);
            }
        }
        
        void OnResearchStarted(string techId, bool completed)
        {
            if (!completed)
            {
                UpdateNodeState(techId);
                
                // Show notification
                Technology tech = techTreeSystem.GetTechnology(techId);
                if (tech != null)
                {
                    UIManager.Instance.ShowNotification("Research Started", 
                        $"Researching: {tech.techName}", NotificationType.Info);
                }
            }
        }
        
        void OnResearchCompleted(string techId, bool completed)
        {
            if (completed)
            {
                UpdateNodeState(techId);
                
                // Update connections
                UpdateConnections();
                
                // Show notification
                Technology tech = techTreeSystem.GetTechnology(techId);
                if (tech != null)
                {
                    UIManager.Instance.ShowNotification("Research Complete", 
                        $"{tech.techName} has been researched!", NotificationType.Success);
                }
            }
        }
        
        void OnResearchProgress(string techId, bool completed)
        {
            if (!completed)
            {
                UpdateNodeState(techId);
                
                // Update progress on info panel if this tech is selected
                if (selectedTech != null && selectedTech.techId == techId)
                {
                    UpdateResearchButton();
                }
            }
        }
        
        public void OnCloseButtonClicked()
        {
            HideTechTree();
        }
        
        void Update()
        {
            // Handle keyboard shortcuts
            if (canvasGroup != null && canvasGroup.interactable)
            {
                // Escape to close
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    OnCloseButtonClicked();
                }
                
                // Tab to switch categories
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    SwitchToNextCategory();
                }
            }
        }
        
        void SwitchToNextCategory()
        {
            TechCategory[] categories = (TechCategory[])System.Enum.GetValues(typeof(TechCategory));
            int currentIndex = System.Array.IndexOf(categories, currentCategory);
            int nextIndex = (currentIndex + 1) % categories.Length;
            
            SelectCategory(categories[nextIndex]);
        }
        
        public bool IsVisible()
        {
            return canvasGroup != null ? canvasGroup.alpha > 0.5f : gameObject.activeSelf;
        }
        
        void OnDestroy()
        {
            // Clean up animations
            if (showSequence != null)
            {
                showSequence.Kill();
            }
            
            // Unsubscribe from events
            if (techTreeSystem != null)
            {
                techTreeSystem.OnResearchStarted -= OnResearchStarted;
                techTreeSystem.OnResearchCompleted -= OnResearchCompleted;
                techTreeSystem.OnResearchProgress -= OnResearchProgress;
            }
        }
    }
    
    public class TechNodeUI : MonoBehaviour
    {
        [Header("UI References")]
        public Image background;
        public TextMeshProUGUI techNameText;
        public Image techIcon;
        public GameObject progressOverlay;
        public Image progressFill;
        public TextMeshProUGUI progressText;
        public GameObject lockedIcon;
        public GameObject researchedIcon;
        
        [Header("Settings")]
        public Technology technology;
        public string techId;
        public Color hoverColor = new Color(1, 1, 1, 0.5f);
        
        // State
        private TechNodeState currentState = TechNodeState.Unavailable;
        private Color currentColor;
        private System.Action<Technology> onClickCallback;
        
        public void Initialize(Technology tech, System.Action<Technology> onClick)
        {
            technology = tech;
            techId = tech.techId;
            onClickCallback = onClick;
            
            // Set UI elements
            if (techNameText != null)
                techNameText.text = tech.techName;
            
            if (techIcon != null && tech.icon != null)
                techIcon.sprite = tech.icon;
            
            // Add button component if not present
            Button button = GetComponent<Button>();
            if (button == null)
            {
                button = gameObject.AddComponent<Button>();
            }
            
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClicked);
            
            // Add hover effect
            AddHoverEffect();
            
            // Hide overlays initially
            if (progressOverlay != null) progressOverlay.SetActive(false);
            if (lockedIcon != null) lockedIcon.SetActive(false);
            if (researchedIcon != null) researchedIcon.SetActive(false);
        }
        
        void AddHoverEffect()
        {
            // Add EventTrigger for hover effects
            EventTrigger eventTrigger = GetComponent<EventTrigger>();
            if (eventTrigger == null)
            {
                eventTrigger = gameObject.AddComponent<EventTrigger>();
            }
            
            // Clear existing triggers
            eventTrigger.triggers.Clear();
            
            // Pointer Enter
            EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
            pointerEnter.eventID = EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener((data) => OnPointerEnter());
            eventTrigger.triggers.Add(pointerEnter);
            
            // Pointer Exit
            EventTrigger.Entry pointerExit = new EventTrigger.Entry();
            pointerExit.eventID = EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((data) => OnPointerExit());
            eventTrigger.triggers.Add(pointerExit);
        }
        
        public void SetState(TechNodeState state, Color color)
        {
            currentState = state;
            currentColor = color;
            
            // Update appearance
            if (background != null)
            {
                background.color = color;
            }
            
            // Update overlays
            if (lockedIcon != null)
            {
                lockedIcon.SetActive(state == TechNodeState.Unavailable);
            }
            
            if (researchedIcon != null)
            {
                researchedIcon.SetActive(state == TechNodeState.Researched);
            }
            
            if (progressOverlay != null)
            {
                progressOverlay.SetActive(state == TechNodeState.Researching);
            }
        }
        
        public void SetProgress(float progress)
        {
            if (progressFill != null)
            {
                progressFill.fillAmount = progress;
            }
            
            if (progressText != null)
            {
                progressText.text = $"{progress * 100:F0}%";
            }
        }
        
        void OnPointerEnter()
        {
            // Highlight on hover
            if (background != null)
            {
                background.color = Color.Lerp(currentColor, hoverColor, 0.3f);
            }
            
            // Show tooltip
            if (technology != null)
            {
                string tooltip = $"{technology.techName}\n{technology.description}";
                UIManager.Instance.ShowTooltip(tooltip, transform.position);
            }
        }
        
        void OnPointerExit()
        {
            // Restore color
            if (background != null)
            {
                background.color = currentColor;
            }
            
            // Hide tooltip
            UIManager.Instance.HideTooltip();
        }
        
        void OnClicked()
        {
            if (technology != null && onClickCallback != null)
            {
                onClickCallback(technology);
                
                // Play click sound
                AudioManager.Instance?.PlayUISound("UI_Click");
            }
        }
        
        public void Pulse()
        {
            // Pulse animation when selected or important
            transform.DOScale(transform.localScale * 1.2f, 0.2f)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.OutQuad);
        }
    }
    
    public enum TechNodeState
    {
        Unavailable,
        Available,
        Researching,
        Researched
    }
}