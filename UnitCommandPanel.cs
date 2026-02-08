using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NexusPrime.UI
{
    public class UnitCommandPanel : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject commandButtonPrefab;
        public Transform commandsContainer;
        public Image unitIcon;
        public TextMeshProUGUI unitNameText;
        public TextMeshProUGUI unitHealthText;
        public Image healthBar;
        public Image shieldBar;
        public GameObject abilityPanel;
        
        [Header("Command Settings")]
        public int maxCommands = 6;
        public float buttonSpacing = 5f;
        
        [Header("Colors")]
        public Color availableColor = Color.white;
        public Color unavailableColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        public Color cooldownColor = new Color(1f, 0.5f, 0.5f, 0.5f);
        
        // Current unit
        private SelectableUnit currentUnit;
        private CombatUnit combatUnit;
        private UnitStats unitStats;
        
        // Command buttons
        private List<CommandButton> commandButtons = new List<CommandButton>();
        private Dictionary<string, CommandButton> commandDictionary = new Dictionary<string, CommandButton>();
        
        // Ability cooldowns
        private Dictionary<string, float> abilityCooldowns = new Dictionary<string, float>();
        
        void Start()
        {
            Initialize();
        }
        
        void Initialize()
        {
            // Clear existing buttons
            ClearCommands();
            
            // Create layout
            if (commandsContainer.GetComponent<VerticalLayoutGroup>() == null)
            {
                VerticalLayoutGroup layout = commandsContainer.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = buttonSpacing;
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
            }
            
            // Hide by default
            gameObject.SetActive(false);
        }
        
        public void SetUnit(SelectableUnit unit)
        {
            if (unit == null) return;
            
            currentUnit = unit;
            combatUnit = unit.GetComponent<CombatUnit>();
            unitStats = unit.GetComponent<UnitStats>();
            
            // Update unit info
            UpdateUnitInfo();
            
            // Create command buttons
            CreateCommandButtons();
            
            // Show panel
            gameObject.SetActive(true);
            
            // Position near unit
            UpdatePosition();
        }
        
        public void ClearUnit()
        {
            currentUnit = null;
            combatUnit = null;
            unitStats = null;
            
            // Hide panel
            gameObject.SetActive(false);
        }
        
        void UpdateUnitInfo()
        {
            if (currentUnit == null) return;
            
            // Unit name
            if (unitNameText != null)
            {
                unitNameText.text = currentUnit.displayName;
            }
            
            // Unit icon
            if (unitIcon != null && currentUnit.icon != null)
            {
                unitIcon.sprite = currentUnit.icon;
            }
            
            // Update health display
            UpdateHealthDisplay();
        }
        
        void UpdateHealthDisplay()
        {
            if (unitStats == null) return;
            
            // Health text
            if (unitHealthText != null)
            {
                unitHealthText.text = $"{unitStats.currentHealth:F0}/{unitStats.maxHealth:F0}";
            }
            
            // Health bar
            if (healthBar != null)
            {
                float healthPercent = unitStats.GetHealthPercentage();
                healthBar.fillAmount = healthPercent;
                
                // Color based on health
                if (healthPercent > 0.7f)
                {
                    healthBar.color = Color.green;
                }
                else if (healthPercent > 0.3f)
                {
                    healthBar.color = Color.yellow;
                }
                else
                {
                    healthBar.color = Color.red;
                }
            }
            
            // Shield bar
            if (shieldBar != null && unitStats.maxShield > 0)
            {
                float shieldPercent = unitStats.GetShieldPercentage();
                shieldBar.fillAmount = shieldPercent;
                shieldBar.gameObject.SetActive(true);
            }
            else if (shieldBar != null)
            {
                shieldBar.gameObject.SetActive(false);
            }
        }
        
        void CreateCommandButtons()
        {
            ClearCommands();
            
            // Basic movement commands
            AddCommandButton("Move", "Move to location", OnMoveCommand, "Icons/Commands/Move");
            AddCommandButton("Stop", "Stop movement", OnStopCommand, "Icons/Commands/Stop");
            AddCommandButton("Hold", "Hold position", OnHoldCommand, "Icons/Commands/Hold");
            AddCommandButton("Patrol", "Patrol between points", OnPatrolCommand, "Icons/Commands/Patrol");
            
            // Attack commands if combat unit
            if (combatUnit != null)
            {
                AddCommandButton("Attack", "Attack target", OnAttackCommand, "Icons/Commands/Attack");
                AddCommandButton("Stop Attack", "Stop attacking", OnStopAttackCommand, "Icons/Commands/StopAttack");
                
                // Ability commands
                if (combatUnit.abilities != null && combatUnit.abilities.Length > 0)
                {
                    for (int i = 0; i < Mathf.Min(combatUnit.abilities.Length, 3); i++)
                    {
                        string ability = combatUnit.abilities[i];
                        AddCommandButton(ability, $"Use {ability}", () => OnAbilityCommand(ability), 
                            $"Icons/Abilities/{ability}");
                    }
                }
            }
            
            // Special commands based on unit type
            AddSpecialCommands();
        }
        
        void AddCommandButton(string commandId, string tooltip, UnityEngine.Events.UnityAction action, string iconPath)
        {
            if (commandButtonPrefab == null) return;
            if (commandButtons.Count >= maxCommands) return;
            
            // Create button
            GameObject buttonObj = Instantiate(commandButtonPrefab, commandsContainer);
            buttonObj.name = $"Command_{commandId}";
            
            // Get or add CommandButton component
            CommandButton commandButton = buttonObj.GetComponent<CommandButton>();
            if (commandButton == null)
            {
                commandButton = buttonObj.AddComponent<CommandButton>();
            }
            
            // Initialize
            commandButton.Initialize(commandId, tooltip, action, iconPath);
            
            // Add to lists
            commandButtons.Add(commandButton);
            commandDictionary[commandId] = commandButton;
        }
        
        void AddSpecialCommands()
        {
            // Add unit-specific commands
            if (currentUnit.unitId.Contains("engineer"))
            {
                AddCommandButton("Repair", "Repair damaged units/buildings", OnRepairCommand, "Icons/Commands/Repair");
                AddCommandButton("Build", "Construct buildings", OnBuildCommand, "Icons/Commands/Build");
            }
            
            if (currentUnit.unitId.Contains("scout"))
            {
                AddCommandButton("Scan", "Scan area", OnScanCommand, "Icons/Commands/Scan");
                AddCommandButton("Cloak", "Activate cloak", OnCloakCommand, "Icons/Commands/Cloak");
            }
            
            if (currentUnit.unitId.Contains("medic"))
            {
                AddCommandButton("Heal", "Heal friendly units", OnHealCommand, "Icons/Commands/Heal");
                AddCommandButton("Revive", "Revive fallen units", OnReviveCommand, "Icons/Commands/Revive");
            }
        }
        
        void ClearCommands()
        {
            foreach (CommandButton button in commandButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }
            
            commandButtons.Clear();
            commandDictionary.Clear();
            abilityCooldowns.Clear();
        }
        
        void UpdatePosition()
        {
            if (currentUnit == null) return;
            
            // Convert unit world position to screen position
            Vector3 screenPos = Camera.main.WorldToScreenPoint(currentUnit.transform.position);
            
            // Offset above unit
            screenPos += new Vector3(0, 150, 0);
            
            // Clamp to screen
            RectTransform rectTransform = GetComponent<RectTransform>();
            Vector2 size = rectTransform.sizeDelta;
            
            screenPos.x = Mathf.Clamp(screenPos.x, size.x / 2, Screen.width - size.x / 2);
            screenPos.y = Mathf.Clamp(screenPos.y, size.y / 2, Screen.height - size.y / 2);
            
            rectTransform.position = screenPos;
        }
        
        void Update()
        {
            if (currentUnit == null) return;
            
            // Update position to follow unit
            UpdatePosition();
            
            // Update health display
            UpdateHealthDisplay();
            
            // Update command states
            UpdateCommandStates();
            
            // Update ability cooldowns
            UpdateAbilityCooldowns();
            
            // Check if unit is still selected
            if (!currentUnit.IsSelected())
            {
                ClearUnit();
            }
        }
        
        void UpdateCommandStates()
        {
            // Update availability of commands based on unit state
            foreach (CommandButton button in commandButtons)
            {
                UpdateButtonState(button);
            }
        }
        
        void UpdateButtonState(CommandButton button)
        {
            if (button == null || currentUnit == null) return;
            
            bool isAvailable = true;
            string reason = "";
            
            switch (button.commandId)
            {
                case "Move":
                    isAvailable = currentUnit.IsAlive();
                    break;
                    
                case "Attack":
                    isAvailable = combatUnit != null && currentUnit.IsAlive();
                    if (!isAvailable) reason = "Unit cannot attack";
                    break;
                    
                case "Repair":
                    isAvailable = currentUnit.unitId.Contains("engineer") && currentUnit.IsAlive();
                    if (!isAvailable) reason = "Not an engineer";
                    break;
                    
                case "Heal":
                    isAvailable = currentUnit.unitId.Contains("medic") && currentUnit.IsAlive();
                    if (!isAvailable) reason = "Not a medic";
                    break;
                    
                case "Cloak":
                    isAvailable = currentUnit.unitId.Contains("scout") && currentUnit.IsAlive();
                    if (!isAvailable) reason = "Not a scout";
                    break;
            }
            
            // Check cooldown
            if (abilityCooldowns.ContainsKey(button.commandId))
            {
                float cooldown = abilityCooldowns[button.commandId];
                if (cooldown > 0)
                {
                    isAvailable = false;
                    reason = $"Cooldown: {cooldown:F1}s";
                    button.SetCooldown(cooldown, 10f); // Assuming 10s max cooldown
                }
            }
            
            button.SetAvailable(isAvailable, reason);
        }
        
        void UpdateAbilityCooldowns()
        {
            // Update cooldown timers
            List<string> toRemove = new List<string>();
            
            foreach (var kvp in abilityCooldowns)
            {
                abilityCooldowns[kvp.Key] = Mathf.Max(0, kvp.Value - Time.deltaTime);
                
                if (abilityCooldowns[kvp.Key] <= 0)
                {
                    toRemove.Add(kvp.Key);
                }
            }
            
            // Remove completed cooldowns
            foreach (string ability in toRemove)
            {
                abilityCooldowns.Remove(ability);
            }
        }
        
        // Command handlers
        void OnMoveCommand()
        {
            Debug.Log("Move command issued");
            
            // Enter move mode
            SelectionManager selectionManager = FindObjectOfType<SelectionManager>();
            if (selectionManager != null)
            {
                selectionManager.EnterMoveMode();
            }
            
            // Hide command panel
            ClearUnit();
        }
        
        void OnStopCommand()
        {
            if (currentUnit == null) return;
            
            UnitMovement movement = currentUnit.GetComponent<UnitMovement>();
            if (movement != null)
            {
                movement.StopMovement();
            }
            
            if (combatUnit != null)
            {
                combatUnit.ClearTarget();
            }
            
            Debug.Log("Stop command issued");
        }
        
        void OnHoldCommand()
        {
            if (currentUnit == null) return;
            
            // Set unit to hold position
            Debug.Log("Hold position command issued");
            
            // This would set an AI state to hold position
        }
        
        void OnPatrolCommand()
        {
            Debug.Log("Patrol command issued");
            
            // Enter patrol mode
            SelectionManager selectionManager = FindObjectOfType<SelectionManager>();
            if (selectionManager != null)
            {
                selectionManager.EnterPatrolMode();
            }
            
            ClearUnit();
        }
        
        void OnAttackCommand()
        {
            Debug.Log("Attack command issued");
            
            // Enter attack mode
            SelectionManager selectionManager = FindObjectOfType<SelectionManager>();
            if (selectionManager != null)
            {
                selectionManager.EnterAttackMode();
            }
            
            ClearUnit();
        }
        
        void OnStopAttackCommand()
        {
            if (combatUnit != null)
            {
                combatUnit.ClearTarget();
                Debug.Log("Stop attack command issued");
            }
        }
        
        void OnAbilityCommand(string ability)
        {
            if (combatUnit == null) return;
            
            // Use ability
            Debug.Log($"Ability command issued: {ability}");
            
            // Set cooldown (example: 10 seconds)
            abilityCooldowns[ability] = 10f;
            
            // Actually use the ability
            // combatUnit.UseAbility(ability);
        }
        
        void OnRepairCommand()
        {
            Debug.Log("Repair command issued");
            
            // Enter repair mode
            SelectionManager selectionManager = FindObjectOfType<SelectionManager>();
            if (selectionManager != null)
            {
                selectionManager.EnterRepairMode();
            }
            
            ClearUnit();
        }
        
        void OnBuildCommand()
        {
            Debug.Log("Build command issued");
            
            // Show build menu
            UIManager.Instance.ShowBuildMenu();
            
            ClearUnit();
        }
        
        void OnScanCommand()
        {
            if (currentUnit == null) return;
            
            Debug.Log("Scan command issued");
            
            // Start scan ability
            abilityCooldowns["Scan"] = 30f;
            
            // Create scan effect
            CreateScanEffect(currentUnit.transform.position, 20f);
        }
        
        void OnCloakCommand()
        {
            if (currentUnit == null) return;
            
            Debug.Log("Cloak command issued");
            
            // Toggle cloak
            abilityCooldowns["Cloak"] = 5f;
            
            // Apply cloak effect
            ApplyCloakEffect(currentUnit.gameObject, true);
        }
        
        void OnHealCommand()
        {
            Debug.Log("Heal command issued");
            
            // Enter heal mode
            SelectionManager selectionManager = FindObjectOfType<SelectionManager>();
            if (selectionManager != null)
            {
                selectionManager.EnterHealMode();
            }
            
            ClearUnit();
        }
        
        void OnReviveCommand()
        {
            Debug.Log("Revive command issued");
            
            // Enter revive mode
            SelectionManager selectionManager = FindObjectOfType<SelectionManager>();
            if (selectionManager != null)
            {
                selectionManager.EnterReviveMode();
            }
            
            ClearUnit();
        }
        
        void CreateScanEffect(Vector3 position, float radius)
        {
            // Create visual scan effect
            GameObject scanEffect = new GameObject("ScanEffect");
            scanEffect.transform.position = position;
            
            // Add particle system or other visual effect
            // This is a placeholder
        }
        
        void ApplyCloakEffect(GameObject unit, bool cloak)
        {
            // Apply cloak material or effect
            Renderer[] renderers = unit.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (cloak)
                {
                    renderer.material.color = new Color(1, 1, 1, 0.3f);
                }
                else
                {
                    renderer.material.color = Color.white;
                }
            }
        }
        
        public void ShowAbilityPanel(bool show)
        {
            if (abilityPanel != null)
            {
                abilityPanel.SetActive(show);
            }
        }
        
        public void UpdateAbilityPanel(string abilityName, float cooldown, float maxCooldown)
        {
            if (abilityPanel == null) return;
            
            // Update ability panel UI
            // This would show detailed ability info and cooldown
        }
        
        void OnDestroy()
        {
            ClearCommands();
        }
    }
    
    public class CommandButton : MonoBehaviour
    {
        [Header("UI References")]
        public Button button;
        public Image icon;
        public TextMeshProUGUI hotkeyText;
        public GameObject cooldownOverlay;
        public Image cooldownFill;
        public TextMeshProUGUI cooldownText;
        public GameObject unavailableOverlay;
        public TextMeshProUGUI unavailableText;
        
        [Header("Settings")]
        public string commandId;
        public string tooltip;
        public KeyCode hotkey = KeyCode.None;
        
        // State
        private bool isAvailable = true;
        private float currentCooldown = 0f;
        private float maxCooldown = 0f;
        
        public void Initialize(string id, string tooltipText, UnityEngine.Events.UnityAction action, string iconPath)
        {
            commandId = id;
            tooltip = tooltipText;
            
            // Get references
            if (button == null) button = GetComponent<Button>();
            if (icon == null) icon = GetComponentInChildren<Image>();
            
            // Set up button
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(action);
                button.onClick.AddListener(PlayClickSound);
            }
            
            // Load icon
            if (icon != null && !string.IsNullOrEmpty(iconPath))
            {
                Sprite iconSprite = Resources.Load<Sprite>(iconPath);
                if (iconSprite != null)
                {
                    icon.sprite = iconSprite;
                }
            }
            
            // Set hotkey text
            if (hotkeyText != null && hotkey != KeyCode.None)
            {
                hotkeyText.text = hotkey.ToString();
            }
            
            // Hide overlays initially
            if (cooldownOverlay != null) cooldownOverlay.SetActive(false);
            if (unavailableOverlay != null) unavailableOverlay.SetActive(false);
            
            // Add hover effect
            AddHoverEffect();
        }
        
        void AddHoverEffect()
        {
            // Add EventTrigger for hover
            EventTrigger eventTrigger = gameObject.AddComponent<EventTrigger>();
            
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
        
        public void SetAvailable(bool available, string reason = "")
        {
            isAvailable = available;
            
            if (button != null)
            {
                button.interactable = available;
            }
            
            // Show/hide unavailable overlay
            if (unavailableOverlay != null)
            {
                unavailableOverlay.SetActive(!available);
                
                if (!available && unavailableText != null && !string.IsNullOrEmpty(reason))
                {
                    unavailableText.text = reason;
                }
            }
        }
        
        public void SetCooldown(float cooldown, float max)
        {
            currentCooldown = cooldown;
            maxCooldown = max;
            
            if (cooldownOverlay != null)
            {
                cooldownOverlay.SetActive(cooldown > 0);
                
                if (cooldownFill != null)
                {
                    float fillAmount = 1f - (cooldown / max);
                    cooldownFill.fillAmount = fillAmount;
                }
                
                if (cooldownText != null)
                {
                    cooldownText.text = $"{cooldown:F1}s";
                }
            }
            
            // Also disable button during cooldown
            if (button != null)
            {
                button.interactable = cooldown <= 0;
            }
        }
        
        void OnPointerEnter()
        {
            // Show tooltip
            UIManager.Instance?.ShowTooltip(tooltip, transform.position);
            
            // Scale up
            transform.localScale = Vector3.one * 1.1f;
        }
        
        void OnPointerExit()
        {
            // Hide tooltip
            UIManager.Instance?.HideTooltip();
            
            // Scale back
            transform.localScale = Vector3.one;
        }
        
        void PlayClickSound()
        {
            // Play UI click sound
            AudioManager.Instance?.PlayUISound("UI_Click");
        }
        
        void Update()
        {
            // Update cooldown
            if (currentCooldown > 0)
            {
                currentCooldown -= Time.deltaTime;
                SetCooldown(currentCooldown, maxCooldown);
            }
            
            // Check for hotkey press
            if (hotkey != KeyCode.None && Input.GetKeyDown(hotkey) && isAvailable && currentCooldown <= 0)
            {
                if (button != null)
                {
                    button.onClick.Invoke();
                }
            }
        }
    }
}