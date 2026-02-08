using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace NexusPrime.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance;
        
        [Header("UI References")]
        public Canvas mainCanvas;
        public CanvasScaler canvasScaler;
        public GraphicRaycaster graphicRaycaster;
        
        [Header("Screen Components")]
        public GameObject mainMenuScreen;
        public GameObject gameHUD;
        public GameObject pauseMenu;
        public GameObject gameOverScreen;
        public GameObject loadingScreen;
        public GameObject optionsMenu;
        public GameObject techTreeScreen;
        public GameObject buildMenu;
        public GameObject unitCommandsPanel;
        
        [Header("HUD Elements")]
        public ResourcePanel resourcePanel;
        public Minimap minimap;
        public SelectionPanel selectionPanel;
        public NotificationPanel notificationPanel;
        public ObjectivesPanel objectivesPanel;
        public ChatPanel chatPanel;
        
        [Header("Prefabs")]
        public GameObject floatingTextPrefab;
        public GameObject selectionBoxPrefab;
        public GameObject waypointMarkerPrefab;
        public GameObject damageNumberPrefab;
        
        [Header("UI Settings")]
        public Color positiveColor = Color.green;
        public Color negativeColor = Color.red;
        public Color warningColor = Color.yellow;
        public Color infoColor = Color.cyan;
        
        [Header("Animation Settings")]
        public float fadeDuration = 0.3f;
        public float slideDuration = 0.2f;
        
        // Internal state
        private Stack<GameObject> screenStack = new Stack<GameObject>();
        private Dictionary<string, GameObject> uiElements = new Dictionary<string, GameObject>();
        private List<GameObject> activeFloatingTexts = new List<GameObject>();
        private Camera uiCamera;
        
        // Events
        public delegate void UIEventHandler(string eventName);
        public event UIEventHandler OnUIEvent;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeUI();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializeUI()
        {
            // Setup canvas
            if (canvasScaler == null)
                canvasScaler = mainCanvas.GetComponent<CanvasScaler>();
            
            if (graphicRaycaster == null)
                graphicRaycaster = mainCanvas.GetComponent<GraphicRaycaster>();
            
            // Configure canvas scaler for mobile
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
            
            // Find UI camera
            uiCamera = GameObject.FindGameObjectWithTag("UICamera")?.GetComponent<Camera>();
            
            // Initialize all UI components
            InitializeUIElements();
            
            // Start with main menu
            ShowScreen(mainMenuScreen);
            
            Debug.Log("UI Manager Initialized");
        }
        
        void InitializeUIElements()
        {
            // Register all UI elements
            RegisterUIElement("MainMenu", mainMenuScreen);
            RegisterUIElement("GameHUD", gameHUD);
            RegisterUIElement("PauseMenu", pauseMenu);
            RegisterUIElement("GameOver", gameOverScreen);
            RegisterUIElement("Loading", loadingScreen);
            RegisterUIElement("Options", optionsMenu);
            RegisterUIElement("TechTree", techTreeScreen);
            RegisterUIElement("BuildMenu", buildMenu);
            RegisterUIElement("UnitCommands", unitCommandsPanel);
            
            // Initialize panels
            if (resourcePanel != null) resourcePanel.Initialize();
            if (minimap != null) minimap.Initialize();
            if (selectionPanel != null) selectionPanel.Initialize();
            if (notificationPanel != null) notificationPanel.Initialize();
            if (objectivesPanel != null) objectivesPanel.Initialize();
            if (chatPanel != null) chatPanel.Initialize();
        }
        
        void RegisterUIElement(string id, GameObject element)
        {
            if (element != null && !uiElements.ContainsKey(id))
            {
                uiElements.Add(id, element);
                element.SetActive(false);
            }
        }
        
        public void ShowScreen(GameObject screen)
        {
            if (screen == null) return;
            
            // Hide current top screen
            if (screenStack.Count > 0)
            {
                GameObject currentTop = screenStack.Peek();
                StartCoroutine(FadeOut(currentTop));
            }
            
            // Show new screen
            screen.SetActive(true);
            StartCoroutine(FadeIn(screen));
            
            // Push to stack
            screenStack.Push(screen);
            
            // Fire event
            OnUIEvent?.Invoke($"Screen_{screen.name}_Shown");
        }
        
        public void HideScreen()
        {
            if (screenStack.Count == 0) return;
            
            // Hide current top screen
            GameObject topScreen = screenStack.Pop();
            StartCoroutine(FadeOut(topScreen));
            
            // Show previous screen
            if (screenStack.Count > 0)
            {
                GameObject previousScreen = screenStack.Peek();
                previousScreen.SetActive(true);
                StartCoroutine(FadeIn(previousScreen));
            }
        }
        
        public void ShowGameHUD()
        {
            ShowScreen(gameHUD);
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }
        
        public void ShowMainMenu()
        {
            ShowScreen(mainMenuScreen);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        public void ShowPauseMenu()
        {
            ShowScreen(pauseMenu);
            Time.timeScale = 0f;
        }
        
        public void HidePauseMenu()
        {
            HideScreen();
            Time.timeScale = 1f;
        }
        
        public void ShowGameOver(bool victory)
        {
            ShowScreen(gameOverScreen);
            
            // Update game over screen
            GameOverScreen gameOver = gameOverScreen != null ? gameOverScreen.GetComponent<GameOverScreen>() : null;
            if (gameOver != null)
            {
                gameOver.SetVictory(victory);
            }
        }
        
        public void ShowLoadingScreen(string loadingMessage = "Loading...")
        {
            ShowScreen(loadingScreen);
            
            // Update loading text
            TextMeshProUGUI loadingText = loadingScreen.GetComponentInChildren<TextMeshProUGUI>();
            if (loadingText != null)
            {
                loadingText.text = loadingMessage;
            }
        }
        
        public void UpdateResourceDisplay(Dictionary<ResourceType, int> resources)
        {
            if (resourcePanel != null)
            {
                resourcePanel.UpdateResourceDisplay(resources);
            }
        }
        
        public void UpdateSelectionDisplay(List<SelectableUnit> selectedUnits)
        {
            if (selectionPanel != null)
            {
                selectionPanel.UpdateSelection(selectedUnits);
            }
        }
        
        public void ShowUnitCommands(SelectableUnit unit)
        {
            if (unitCommandsPanel != null)
            {
                unitCommandsPanel.SetActive(true);
                UnitCommandPanel commandPanel = unitCommandsPanel.GetComponent<UnitCommandPanel>();
                if (commandPanel != null)
                {
                    commandPanel.SetUnit(unit);
                    
                    // Position near unit
                    Vector3 screenPos = Camera.main.WorldToScreenPoint(unit.transform.position);
                    unitCommandsPanel.transform.position = screenPos + new Vector3(0, 100, 0);
                }
            }
        }
        
        public void HideUnitCommands()
        {
            if (unitCommandsPanel != null)
            {
                unitCommandsPanel.SetActive(false);
            }
        }
        
        public void ShowBuildMenu()
        {
            if (buildMenu != null)
            {
                buildMenu.SetActive(true);
                BuildMenu menu = buildMenu.GetComponent<BuildMenu>();
                if (menu != null)
                {
                    menu.UpdateAvailableBuildings();
                }
            }
        }
        
        public void HideBuildMenu()
        {
            if (buildMenu != null)
            {
                buildMenu.SetActive(false);
            }
        }
        
        public void ShowTechTree()
        {
            ShowScreen(techTreeScreen);
            
            TechTreeUI techTree = techTreeScreen.GetComponent<TechTreeUI>();
            if (techTree != null)
            {
                techTree.UpdateTechTreeDisplay();
            }
        }
        
        public void CreateFloatingText(Vector3 worldPosition, string text, Color color, float duration = 2f)
        {
            if (floatingTextPrefab == null) return;
            
            // Convert world position to screen position
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
            
            // Create floating text
            GameObject floatingText = Instantiate(floatingTextPrefab, mainCanvas.transform);
            floatingText.transform.position = screenPosition;
            
            // Set text and color
            TextMeshProUGUI textComponent = floatingText.GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = text;
                textComponent.color = color;
            }
            
            // Add to active list
            activeFloatingTexts.Add(floatingText);
            
            // Auto-destroy
            Destroy(floatingText, duration);
        }
        
        public void ShowDamageNumber(Vector3 worldPosition, float damage, bool isCritical = false)
        {
            if (damageNumberPrefab == null) return;
            
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
            GameObject damageNumber = Instantiate(damageNumberPrefab, mainCanvas.transform);
            damageNumber.transform.position = screenPosition;
            
            DamageNumberUI damageUI = damageNumber.GetComponent<DamageNumberUI>();
            if (damageUI != null)
            {
                damageUI.Initialize(damage, isCritical);
            }
        }
        
        public void ShowNotification(string title, string message, NotificationType type = NotificationType.Info)
        {
            if (notificationPanel != null)
            {
                notificationPanel.ShowNotification(title, message, type);
            }
        }
        
        public void UpdateObjective(string objectiveId, string description, float progress = 0f)
        {
            if (objectivesPanel != null)
            {
                objectivesPanel.UpdateObjective(objectiveId, description, progress);
            }
        }
        
        public void CompleteObjective(string objectiveId)
        {
            if (objectivesPanel != null)
            {
                objectivesPanel.CompleteObjective(objectiveId);
            }
        }
        
        public void AddChatMessage(string sender, string message, ChatMessageType type = ChatMessageType.Normal)
        {
            if (chatPanel != null)
            {
                chatPanel.AddMessage(sender, message, type);
            }
        }
        
        public void ShowTooltip(string text, Vector3 position)
        {
            // Create or update tooltip
            GameObject tooltip = GetOrCreateUIElement("Tooltip", "UI/Tooltip");
            if (tooltip != null)
            {
                tooltip.transform.position = position;
                TextMeshProUGUI textComponent = tooltip.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = text;
                }
                tooltip.SetActive(true);
            }
        }
        
        public void HideTooltip()
        {
            GameObject tooltip = GetUIElement("Tooltip");
            if (tooltip != null)
            {
                tooltip.SetActive(false);
            }
        }
        
        public void ShowContextMenu(Vector3 position, List<ContextMenuItem> items)
        {
            GameObject contextMenu = GetOrCreateUIElement("ContextMenu", "UI/ContextMenu");
            if (contextMenu != null)
            {
                contextMenu.transform.position = position;
                ContextMenuUI menuUI = contextMenu.GetComponent<ContextMenuUI>();
                if (menuUI != null)
                {
                    menuUI.SetItems(items);
                }
                contextMenu.SetActive(true);
            }
        }
        
        public void ShowConfirmationDialog(string title, string message, System.Action onConfirm, System.Action onCancel = null)
        {
            GameObject dialog = GetOrCreateUIElement("ConfirmationDialog", "UI/ConfirmationDialog");
            if (dialog != null)
            {
                ConfirmationDialogUI dialogUI = dialog.GetComponent<ConfirmationDialogUI>();
                if (dialogUI != null)
                {
                    dialogUI.Show(title, message, onConfirm, onCancel);
                }
            }
        }
        
        public void UpdateMinimapTexture(Texture2D texture)
        {
            if (minimap != null)
            {
                minimap.UpdateMinimapTexture(texture);
            }
        }
        
        public void AddMinimapMarker(Vector3 worldPosition, MinimapMarkerType type, Color color)
        {
            if (minimap != null)
            {
                minimap.AddMarker(worldPosition, type, color);
            }
        }
        
        public void RemoveMinimapMarker(Vector3 worldPosition)
        {
            if (minimap != null)
            {
                minimap.RemoveMarker(worldPosition);
            }
        }
        
        GameObject GetOrCreateUIElement(string id, string prefabPath)
        {
            if (uiElements.ContainsKey(id))
            {
                return uiElements[id];
            }
            
            // Load prefab
            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"UI prefab not found: {prefabPath}");
                return null;
            }
            
            // Create instance
            GameObject element = Instantiate(prefab, mainCanvas.transform);
            element.name = id;
            uiElements.Add(id, element);
            element.SetActive(false);
            
            return element;
        }
        
        GameObject GetUIElement(string id)
        {
            return uiElements.ContainsKey(id) ? uiElements[id] : null;
        }
        
        System.Collections.IEnumerator FadeIn(GameObject obj)
        {
            CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = obj.AddComponent<CanvasGroup>();
            }
            
            canvasGroup.alpha = 0;
            float elapsed = 0;
            
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / fadeDuration);
                yield return null;
            }
            
            canvasGroup.alpha = 1;
        }
        
        System.Collections.IEnumerator FadeOut(GameObject obj)
        {
            CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
            if (canvasGroup == null) yield break;
            
            float startAlpha = canvasGroup.alpha;
            float elapsed = 0;
            
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0, elapsed / fadeDuration);
                yield return null;
            }
            
            canvasGroup.alpha = 0;
            obj.SetActive(false);
        }
        
        public void SetUIScale(float scale)
        {
            if (canvasScaler != null)
            {
                canvasScaler.scaleFactor = Mathf.Clamp(scale, 0.5f, 2f);
            }
        }
        
        public bool IsPointerOverUI()
        {
            if (EventSystem.current == null) return false;
            
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;
            
            List<RaycastResult> results = new List<RaycastResult>();
            graphicRaycaster.Raycast(eventData, results);
            
            return results.Count > 0;
        }
        
        public Vector3 ScreenToWorldPoint(Vector3 screenPosition)
        {
            if (Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(screenPosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    return hit.point;
                }
            }
            return Vector3.zero;
        }
        
        public void CleanupFloatingTexts()
        {
            for (int i = activeFloatingTexts.Count - 1; i >= 0; i--)
            {
                if (activeFloatingTexts[i] == null)
                {
                    activeFloatingTexts.RemoveAt(i);
                }
            }
        }
        
        void Update()
        {
            // Handle UI input
            HandleUIInput();
            
            // Cleanup floating texts
            if (Time.frameCount % 60 == 0)
            {
                CleanupFloatingTexts();
            }
        }
        
        void HandleUIInput()
        {
            // Escape key for pause menu
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (screenStack.Count > 0 && screenStack.Peek() == gameHUD)
                {
                    ShowPauseMenu();
                }
                else if (screenStack.Count > 0 && screenStack.Peek() == pauseMenu)
                {
                    HidePauseMenu();
                }
            }
            
            // Tech tree shortcut
            if (Input.GetKeyDown(KeyCode.T))
            {
                if (screenStack.Count > 0 && screenStack.Peek() == gameHUD)
                {
                    ShowTechTree();
                }
            }
            
            // Build menu shortcut
            if (Input.GetKeyDown(KeyCode.B))
            {
                if (screenStack.Count > 0 && screenStack.Peek() == gameHUD)
                {
                    ShowBuildMenu();
                }
            }
        }
        
        public void ShowGameOverScreen(bool victory)
        {
            ShowGameOver(victory);
        }
        
        public void QuitGame()
        {
            Application.Quit();
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }
        
        void OnDestroy()
        {
            // Cleanup
            foreach (var element in uiElements.Values)
            {
                if (element != null)
                {
                    Destroy(element);
                }
            }
            uiElements.Clear();
            screenStack.Clear();
        }
    }
    
    public enum NotificationType
    {
        Info,
        Warning,
        Error,
        Success,
        Achievement
    }
    
    public enum ChatMessageType
    {
        Normal,
        System,
        Player,
        Enemy,
        Alliance
    }
    
    public enum MinimapMarkerType
    {
        Player,
        Enemy,
        Resource,
        Objective,
        Waypoint,
        Building
    }
    
    [System.Serializable]
    public class ContextMenuItem
    {
        public string label;
        public Sprite icon;
        public System.Action action;
        public bool enabled = true;
        
        public ContextMenuItem(string label, System.Action action, Sprite icon = null, bool enabled = true)
        {
            this.label = label;
            this.action = action;
            this.icon = icon;
            this.enabled = enabled;
        }
    }
}