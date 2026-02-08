using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NexusPrime.UI
{
    public class SelectionManager : MonoBehaviour
    {
        [Header("Selection Settings")]
        public LayerMask selectableLayer;
        public LayerMask groundLayer;
        public float doubleClickTime = 0.3f;
        public bool boxSelectEnabled = true;
        public float minBoxSize = 10f;
        
        [Header("Visuals")]
        public GameObject selectionBoxPrefab;
        public Material selectionBoxMaterial;
        public Color selectionBoxColor = new Color(0, 1, 1, 0.3f);
        public Color selectionBoxBorderColor = new Color(0, 1, 1, 0.8f);
        
        [Header("Multi-Selection")]
        public bool additiveSelection = true;
        public KeyCode additiveSelectionKey = KeyCode.LeftShift;
        public KeyCode subtractiveSelectionKey = KeyCode.LeftControl;
        
        [Header("Group Selection")]
        public int maxSelectionGroups = 10;
        public KeyCode[] groupHotkeys = new KeyCode[10];
        
        // Selection state
        private List<SelectableUnit> selectedUnits = new List<SelectableUnit>();
        private Vector3 selectionStartPos;
        private bool isSelecting = false;
        private GameObject selectionBox;
        private RectTransform selectionBoxRect;
        
        // Double click tracking
        private float lastClickTime;
        private Vector3 lastClickPosition;
        
        // Selection groups
        private Dictionary<int, List<SelectableUnit>> selectionGroups = new Dictionary<int, List<SelectableUnit>>();
        
        // Special modes
        private SelectionMode currentMode = SelectionMode.Normal;
        private System.Action<Vector3> specialModeAction;
        
        void Start()
        {
            InitializeGroupHotkeys();
            CreateSelectionBox();
            
            // Hide selection box initially
            if (selectionBox != null)
            {
                selectionBox.SetActive(false);
            }
        }
        
        void InitializeGroupHotkeys()
        {
            // Default group hotkeys: 1-0
            groupHotkeys[0] = KeyCode.Alpha1;
            groupHotkeys[1] = KeyCode.Alpha2;
            groupHotkeys[2] = KeyCode.Alpha3;
            groupHotkeys[3] = KeyCode.Alpha4;
            groupHotkeys[4] = KeyCode.Alpha5;
            groupHotkeys[5] = KeyCode.Alpha6;
            groupHotkeys[6] = KeyCode.Alpha7;
            groupHotkeys[7] = KeyCode.Alpha8;
            groupHotkeys[8] = KeyCode.Alpha9;
            groupHotkeys[9] = KeyCode.Alpha0;
        }
        
        void CreateSelectionBox()
        {
            if (selectionBoxPrefab != null)
            {
                selectionBox = Instantiate(selectionBoxPrefab, UIManager.Instance.mainCanvas.transform);
                selectionBox.name = "SelectionBox";
                selectionBoxRect = selectionBox.GetComponent<RectTransform>();
                
                // Set material and color
                Image image = selectionBox.GetComponent<Image>();
                if (image != null)
                {
                    if (selectionBoxMaterial != null)
                    {
                        image.material = selectionBoxMaterial;
                    }
                    image.color = selectionBoxColor;
                }
                
                selectionBox.SetActive(false);
            }
            else
            {
                // Create default selection box
                selectionBox = new GameObject("SelectionBox");
                selectionBox.transform.SetParent(UIManager.Instance.mainCanvas.transform);
                
                selectionBoxRect = selectionBox.AddComponent<RectTransform>();
                Image image = selectionBox.AddComponent<Image>();
                image.color = selectionBoxColor;
                
                selectionBox.SetActive(false);
            }
        }
        
        void Update()
        {
            HandleSelectionInput();
            HandleGroupHotkeys();
            HandleSpecialModes();
            
            UpdateSelectionBox();
        }
        
        void HandleSelectionInput()
        {
            // Start selection
            if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
            {
                OnSelectionStart();
            }
            
            // Update selection
            if (Input.GetMouseButton(0) && isSelecting)
            {
                OnSelectionUpdate();
            }
            
            // End selection
            if (Input.GetMouseButtonUp(0) && isSelecting)
            {
                OnSelectionEnd();
            }
            
            // Right click for commands
            if (Input.GetMouseButtonDown(1) && selectedUnits.Count > 0)
            {
                OnRightClick();
            }
            
            // Deselect all
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                DeselectAll();
            }
        }
        
        void OnSelectionStart()
        {
            selectionStartPos = Input.mousePosition;
            isSelecting = true;
            
            // Check for double click
            float timeSinceLastClick = Time.time - lastClickTime;
            float distanceSinceLastClick = Vector3.Distance(Input.mousePosition, lastClickPosition);
            
            if (timeSinceLastClick < doubleClickTime && distanceSinceLastClick < 10f)
            {
                // Double click - select all units of same type
                OnDoubleClick();
                isSelecting = false;
                return;
            }
            
            lastClickTime = Time.time;
            lastClickPosition = Input.mousePosition;
            
            // Single click selection
            if (!boxSelectEnabled || currentMode != SelectionMode.Normal)
            {
                // Try to select single unit
                SelectSingleUnit();
                isSelecting = false;
            }
            else
            {
                // Start box selection
                if (selectionBox != null)
                {
                    selectionBox.SetActive(true);
                    selectionBoxRect.position = selectionStartPos;
                    selectionBoxRect.sizeDelta = Vector2.zero;
                }
            }
        }
        
        void OnSelectionUpdate()
        {
            if (!boxSelectEnabled || selectionBox == null) return;
            
            Vector3 currentMousePos = Input.mousePosition;
            
            // Calculate selection box
            Vector3 center = (selectionStartPos + currentMousePos) / 2;
            Vector3 size = currentMousePos - selectionStartPos;
            
            // Update selection box visual
            selectionBoxRect.position = center;
            selectionBoxRect.sizeDelta = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
            
            // Preview selection
            PreviewBoxSelection();
        }
        
        void OnSelectionEnd()
        {
            if (!isSelecting) return;
            
            isSelecting = false;
            
            if (selectionBox != null)
            {
                selectionBox.SetActive(false);
            }
            
            if (boxSelectEnabled && currentMode == SelectionMode.Normal)
            {
                // Perform box selection
                PerformBoxSelection();
            }
        }
        
        void SelectSingleUnit()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, selectableLayer))
            {
                SelectableUnit unit = hit.collider.GetComponent<SelectableUnit>();
                if (unit != null && unit.IsAlive())
                {
                    // Check for additive/subtractive selection
                    bool additive = Input.GetKey(additiveSelectionKey);
                    bool subtractive = Input.GetKey(subtractiveSelectionKey);
                    
                    if (additive)
                    {
                        // Add to selection
                        if (!selectedUnits.Contains(unit))
                        {
                            SelectUnit(unit);
                        }
                    }
                    else if (subtractive)
                    {
                        // Remove from selection
                        if (selectedUnits.Contains(unit))
                        {
                            DeselectUnit(unit);
                        }
                    }
                    else
                    {
                        // Replace selection
                        DeselectAll();
                        SelectUnit(unit);
                    }
                }
                else
                {
                    // Clicked on non-selectable, deselect all if not additive
                    if (!Input.GetKey(additiveSelectionKey) && !Input.GetKey(subtractiveSelectionKey))
                    {
                        DeselectAll();
                    }
                }
            }
            else
            {
                // Clicked on empty space, deselect all if not additive
                if (!Input.GetKey(additiveSelectionKey) && !Input.GetKey(subtractiveSelectionKey))
                {
                    DeselectAll();
                }
            }
        }
        
        void OnDoubleClick()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, selectableLayer))
            {
                SelectableUnit clickedUnit = hit.collider.GetComponent<SelectableUnit>();
                if (clickedUnit != null)
                {
                    // Select all units of same type
                    SelectAllUnitsOfType(clickedUnit.unitId);
                }
            }
        }
        
        void PreviewBoxSelection()
        {
            // Highlight units that would be selected
            List<SelectableUnit> unitsInBox = GetUnitsInSelectionBox();
            
            // Preview highlight (could change material temporarily)
            foreach (SelectableUnit unit in unitsInBox)
            {
                unit.Highlight(true);
            }
        }
        
        void PerformBoxSelection()
        {
            Vector3 currentMousePos = Input.mousePosition;
            Vector3 size = currentMousePos - selectionStartPos;
            
            // Check if box is large enough
            if (Mathf.Abs(size.x) < minBoxSize && Mathf.Abs(size.y) < minBoxSize)
            {
                // Box too small, treat as single click
                SelectSingleUnit();
                return;
            }
            
            List<SelectableUnit> unitsInBox = GetUnitsInSelectionBox();
            
            // Determine selection mode
            bool additive = Input.GetKey(additiveSelectionKey);
            bool subtractive = Input.GetKey(subtractiveSelectionKey);
            
            if (subtractive)
            {
                // Remove units from selection
                foreach (SelectableUnit unit in unitsInBox)
                {
                    if (selectedUnits.Contains(unit))
                    {
                        DeselectUnit(unit);
                    }
                }
            }
            else if (additive)
            {
                // Add units to selection
                foreach (SelectableUnit unit in unitsInBox)
                {
                    if (!selectedUnits.Contains(unit))
                    {
                        SelectUnit(unit);
                    }
                }
            }
            else
            {
                // Replace selection
                DeselectAll();
                foreach (SelectableUnit unit in unitsInBox)
                {
                    SelectUnit(unit);
                }
            }
            
            // Remove highlights
            foreach (SelectableUnit unit in unitsInBox)
            {
                unit.Highlight(false);
            }
        }
        
        List<SelectableUnit> GetUnitsInSelectionBox()
        {
            List<SelectableUnit> unitsInBox = new List<SelectableUnit>();
            
            // Get all selectable units
            List<SelectableUnit> allUnits = GameManager.Instance.GetAllUnits();
            
            foreach (SelectableUnit unit in allUnits)
            {
                if (unit == null || !unit.IsAlive()) continue;
                
                // Convert unit position to screen position
                Vector3 screenPos = Camera.main.WorldToScreenPoint(unit.transform.position);
                
                // Check if within selection box
                Rect selectionRect = GetSelectionRect();
                if (selectionRect.Contains(screenPos))
                {
                    unitsInBox.Add(unit);
                }
            }
            
            return unitsInBox;
        }
        
        Rect GetSelectionRect()
        {
            Vector3 start = selectionStartPos;
            Vector3 end = Input.mousePosition;
            
            // Normalize rect (start might be greater than end)
            float xMin = Mathf.Min(start.x, end.x);
            float xMax = Mathf.Max(start.x, end.x);
            float yMin = Mathf.Min(start.y, end.y);
            float yMax = Mathf.Max(start.y, end.y);
            
            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }
        
        void OnRightClick()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                // Check what was clicked
                SelectableUnit targetUnit = hit.collider.GetComponent<SelectableUnit>();
                Building targetBuilding = hit.collider.GetComponent<Building>();
                
                if (targetUnit != null && targetUnit.IsAlive())
                {
                    // Right-clicked on unit
                    OnRightClickUnit(targetUnit);
                }
                else if (targetBuilding != null && targetBuilding.IsOperational())
                {
                    // Right-clicked on building
                    OnRightClickBuilding(targetBuilding);
                }
                else
                {
                    // Right-clicked on ground
                    OnRightClickGround(hit.point);
                }
            }
        }
        
        void OnRightClickUnit(SelectableUnit targetUnit)
        {
            // Check if target is enemy
            CombatUnit targetCombat = targetUnit.GetComponent<CombatUnit>();
            bool isEnemy = targetCombat != null && targetCombat.faction != "player";
            
            if (isEnemy)
            {
                // Attack command
                foreach (SelectableUnit unit in selectedUnits)
                {
                    CombatUnit combat = unit.GetComponent<CombatUnit>();
                    if (combat != null)
                    {
                        combat.SetTarget(targetUnit.transform);
                    }
                }
                
                // Show attack feedback
                ShowCommandFeedback(targetUnit.transform.position, "Attack", Color.red);
            }
            else
            {
                // Friendly unit - could be follow, repair, heal, etc.
                // For now, just move to unit
                foreach (SelectableUnit unit in selectedUnits)
                {
                    UnitMovement movement = unit.GetComponent<UnitMovement>();
                    if (movement != null)
                    {
                        movement.MoveTo(targetUnit.transform.position);
                    }
                }
                
                ShowCommandFeedback(targetUnit.transform.position, "Follow", Color.green);
            }
        }
        
        void OnRightClickBuilding(Building targetBuilding)
        {
            // Move to building or interact with it
            foreach (SelectableUnit unit in selectedUnits)
            {
                UnitMovement movement = unit.GetComponent<UnitMovement>();
                if (movement != null)
                {
                    movement.MoveTo(targetBuilding.transform.position);
                }
            }
            
            ShowCommandFeedback(targetBuilding.transform.position, "Move", Color.blue);
        }
        
        void OnRightClickGround(Vector3 position)
        {
            // Move to position
            foreach (SelectableUnit unit in selectedUnits)
            {
                UnitMovement movement = unit.GetComponent<UnitMovement>();
                if (movement != null)
                {
                    movement.MoveTo(position);
                }
            }
            
            // Show waypoint
            ShowWaypoint(position);
            
            ShowCommandFeedback(position, "Move", Color.blue);
        }
        
        void SelectUnit(SelectableUnit unit)
        {
            if (unit == null || !unit.IsAlive()) return;
            
            if (!selectedUnits.Contains(unit))
            {
                selectedUnits.Add(unit);
                unit.Select();
                
                // Update UI
                UIManager.Instance.UpdateSelectionDisplay(selectedUnits);
                
                // Show unit commands if only one unit selected
                if (selectedUnits.Count == 1)
                {
                    UIManager.Instance.ShowUnitCommands(unit);
                }
                else
                {
                    UIManager.Instance.HideUnitCommands();
                }
            }
        }
        
        void DeselectUnit(SelectableUnit unit)
        {
            if (selectedUnits.Contains(unit))
            {
                selectedUnits.Remove(unit);
                unit.Deselect();
                
                // Update UI
                UIManager.Instance.UpdateSelectionDisplay(selectedUnits);
                
                // Update unit commands display
                if (selectedUnits.Count == 1)
                {
                    UIManager.Instance.ShowUnitCommands(selectedUnits[0]);
                }
                else if (selectedUnits.Count == 0)
                {
                    UIManager.Instance.HideUnitCommands();
                }
            }
        }
        
        void DeselectAll()
        {
            foreach (SelectableUnit unit in selectedUnits)
            {
                unit.Deselect();
            }
            
            selectedUnits.Clear();
            
            // Update UI
            UIManager.Instance.UpdateSelectionDisplay(selectedUnits);
            UIManager.Instance.HideUnitCommands();
            
            // Exit any special mode
            ExitSpecialMode();
        }
        
        void SelectAllUnitsOfType(string unitId)
        {
            List<SelectableUnit> allUnits = GameManager.Instance.GetAllUnits();
            List<SelectableUnit> unitsOfType = new List<SelectableUnit>();
            
            foreach (SelectableUnit unit in allUnits)
            {
                if (unit != null && unit.IsAlive() && unit.unitId == unitId)
                {
                    unitsOfType.Add(unit);
                }
            }
            
            // Select these units
            DeselectAll();
            foreach (SelectableUnit unit in unitsOfType)
            {
                SelectUnit(unit);
            }
        }
        
        void HandleGroupHotkeys()
        {
            for (int i = 0; i < groupHotkeys.Length; i++)
            {
                if (Input.GetKeyDown(groupHotkeys[i]))
                {
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    {
                        // Assign group
                        AssignSelectionToGroup(i);
                    }
                    else
                    {
                        // Recall group
                        RecallGroup(i);
                    }
                }
            }
        }
        
        void AssignSelectionToGroup(int groupIndex)
        {
            if (groupIndex < 0 || groupIndex >= maxSelectionGroups) return;
            
            // Create copy of current selection
            List<SelectableUnit> group = new List<SelectableUnit>(selectedUnits);
            selectionGroups[groupIndex] = group;
            
            // Show feedback
            ShowGroupFeedback(groupIndex, "Group Assigned");
        }
        
        void RecallGroup(int groupIndex)
        {
            if (!selectionGroups.ContainsKey(groupIndex)) return;
            
            List<SelectableUnit> group = selectionGroups[groupIndex];
            
            // Remove dead units from group
            group.RemoveAll(unit => unit == null || !unit.IsAlive());
            
            if (group.Count == 0)
            {
                selectionGroups.Remove(groupIndex);
                return;
            }
            
            // Select group
            DeselectAll();
            foreach (SelectableUnit unit in group)
            {
                SelectUnit(unit);
            }
            
            // Show feedback
            ShowGroupFeedback(groupIndex, "Group Recalled");
            
            // Center camera on group
            CenterCameraOnSelection();
        }
        
        void CenterCameraOnSelection()
        {
            if (selectedUnits.Count == 0) return;
            
            // Calculate average position
            Vector3 center = Vector3.zero;
            foreach (SelectableUnit unit in selectedUnits)
            {
                center += unit.transform.position;
            }
            center /= selectedUnits.Count;
            
            // Move camera to center (this would depend on your camera system)
            // Camera.main.transform.position = center + offset;
        }
        
        void HandleSpecialModes()
        {
            if (currentMode != SelectionMode.Normal)
            {
                // In special mode, left click triggers the special action
                if (Input.GetMouseButtonDown(0) && !IsPointerOverUI() && specialModeAction != null)
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;
                    
                    if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
                    {
                        specialModeAction(hit.point);
                        ExitSpecialMode();
                    }
                }
                
                // Escape cancels special mode
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    ExitSpecialMode();
                }
            }
        }
        
        public void EnterMoveMode()
        {
            currentMode = SelectionMode.Move;
            specialModeAction = (position) =>
            {
                foreach (SelectableUnit unit in selectedUnits)
                {
                    UnitMovement movement = unit.GetComponent<UnitMovement>();
                    if (movement != null)
                    {
                        movement.MoveTo(position);
                    }
                }
                ShowWaypoint(position);
            };
            
            ShowModeFeedback("Move Mode", "Click where to move");
        }
        
        public void EnterAttackMode()
        {
            currentMode = SelectionMode.Attack;
            specialModeAction = (position) =>
            {
                // Find enemy at position or near it
                Collider[] colliders = Physics.OverlapSphere(position, 5f, selectableLayer);
                Transform target = null;
                
                foreach (Collider collider in colliders)
                {
                    SelectableUnit unit = collider.GetComponent<SelectableUnit>();
                    CombatUnit combat = unit?.GetComponent<CombatUnit>();
                    if (combat != null && combat.faction != "player")
                    {
                        target = unit.transform;
                        break;
                    }
                }
                
                foreach (SelectableUnit unit in selectedUnits)
                {
                    CombatUnit combat = unit.GetComponent<CombatUnit>();
                    if (combat != null)
                    {
                        if (target != null)
                        {
                            combat.SetTarget(target);
                        }
                        else
                        {
                            // Move to position and attack anything there
                            combat.SetTarget(null);
                            UnitMovement movement = unit.GetComponent<UnitMovement>();
                            if (movement != null)
                            {
                                movement.MoveTo(position);
                            }
                        }
                    }
                }
                
                ShowCommandFeedback(position, "Attack", Color.red);
            };
            
            ShowModeFeedback("Attack Mode", "Click on target to attack");
        }
        
        public void EnterPatrolMode()
        {
            currentMode = SelectionMode.Patrol;
            List<Vector3> patrolPoints = new List<Vector3>();
            
            specialModeAction = (position) =>
            {
                patrolPoints.Add(position);
                ShowWaypoint(position);
                
                if (patrolPoints.Count >= 2)
                {
                    // Set up patrol between points
                    foreach (SelectableUnit unit in selectedUnits)
                    {
                        PatrolBehavior patrol = unit.GetComponent<PatrolBehavior>();
                        if (patrol == null)
                        {
                            patrol = unit.gameObject.AddComponent<PatrolBehavior>();
                        }
                        patrol.SetPatrolPoints(patrolPoints.ToArray());
                    }
                    
                    ExitSpecialMode();
                }
                else
                {
                    ShowModeFeedback("Patrol Mode", "Click second patrol point");
                }
            };
            
            ShowModeFeedback("Patrol Mode", "Click first patrol point");
        }
        
        public void EnterRepairMode()
        {
            currentMode = SelectionMode.Repair;
            specialModeAction = (position) =>
            {
                // Find damaged building or unit at position
                Collider[] colliders = Physics.OverlapSphere(position, 5f);
                
                foreach (Collider collider in colliders)
                {
                    Building building = collider.GetComponent<Building>();
                    if (building != null && building.GetHealthPercentage() < 1f)
                    {
                        // Send engineers to repair
                        foreach (SelectableUnit unit in selectedUnits)
                        {
                            if (unit.unitId.Contains("engineer"))
                            {
                                EngineerBehavior engineer = unit.GetComponent<EngineerBehavior>();
                                if (engineer == null)
                                {
                                    engineer = unit.gameObject.AddComponent<EngineerBehavior>();
                                }
                                engineer.SetRepairTarget(building);
                            }
                        }
                        break;
                    }
                }
            };
            
            ShowModeFeedback("Repair Mode", "Click on damaged building to repair");
        }
        
        public void EnterHealMode()
        {
            currentMode = SelectionMode.Heal;
            specialModeAction = (position) =>
            {
                // Find damaged friendly unit at position
                Collider[] colliders = Physics.OverlapSphere(position, 5f, selectableLayer);
                
                foreach (Collider collider in colliders)
                {
                    SelectableUnit unit = collider.GetComponent<SelectableUnit>();
                    UnitStats stats = unit?.GetComponent<UnitStats>();
                    if (stats != null && stats.GetHealthPercentage() < 1f)
                    {
                        // Send medics to heal
                        foreach (SelectableUnit medic in selectedUnits)
                        {
                            if (medic.unitId.Contains("medic"))
                            {
                                MedicBehavior medicAI = medic.GetComponent<MedicBehavior>();
                                if (medicAI == null)
                                {
                                    medicAI = medic.gameObject.AddComponent<MedicBehavior>();
                                }
                                medicAI.SetHealTarget(unit);
                            }
                        }
                        break;
                    }
                }
            };
            
            ShowModeFeedback("Heal Mode", "Click on damaged unit to heal");
        }
        
        void ExitSpecialMode()
        {
            currentMode = SelectionMode.Normal;
            specialModeAction = null;
            
            // Hide mode feedback
            UIManager.Instance.HideTooltip();
        }
        
        void ShowCommandFeedback(Vector3 position, string command, Color color)
        {
            // Create visual feedback at command location
            GameObject feedback = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            feedback.transform.position = position + Vector3.up * 0.1f;
            feedback.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);
            
            Renderer renderer = feedback.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
            
            Destroy(feedback, 1f);
            
            // Show floating text
            UIManager.Instance.CreateFloatingText(position + Vector3.up * 2, command, color, 1f);
        }
        
        void ShowWaypoint(Vector3 position)
        {
            if (UIManager.Instance.waypointMarkerPrefab != null)
            {
                GameObject waypoint = Instantiate(UIManager.Instance.waypointMarkerPrefab);
                waypoint.transform.position = position + Vector3.up * 0.5f;
                Destroy(waypoint, 5f);
            }
        }
        
        void ShowModeFeedback(string title, string message)
        {
            UIManager.Instance.ShowNotification(title, message, NotificationType.Info);
        }
        
        void ShowGroupFeedback(int groupIndex, string message)
        {
            UIManager.Instance.ShowNotification($"Group {groupIndex + 1}", message, NotificationType.Info);
        }
        
        void UpdateSelectionBox()
        {
            if (selectionBox != null && selectionBox.activeSelf)
            {
                // Update selection box visual
                // Could add pulsing effect or other visual feedback
            }
        }
        
        bool IsPointerOverUI()
        {
            return UIManager.Instance.IsPointerOverUI();
        }
        
        public List<SelectableUnit> GetSelectedUnits()
        {
            return new List<SelectableUnit>(selectedUnits);
        }
        
        public bool HasSelection()
        {
            return selectedUnits.Count > 0;
        }
        
        public int GetSelectedCount()
        {
            return selectedUnits.Count;
        }
        
        void OnDestroy()
        {
            if (selectionBox != null)
            {
                Destroy(selectionBox);
            }
        }
    }
    
    public enum SelectionMode
    {
        Normal,
        Move,
        Attack,
        Patrol,
        Repair,
        Heal,
        Build,
        Special
    }
    
    // Support classes for special behaviors
    public class PatrolBehavior : MonoBehaviour
    {
        private Vector3[] patrolPoints;
        private int currentPoint = 0;
        private UnitMovement movement;
        
        public void SetPatrolPoints(Vector3[] points)
        {
            patrolPoints = points;
            movement = GetComponent<UnitMovement>();
            
            if (movement != null && patrolPoints.Length > 0)
            {
                MoveToNextPoint();
            }
        }
        
        void Update()
        {
            if (patrolPoints == null || patrolPoints.Length == 0) return;
            if (movement == null) return;
            
            if (movement.HasReachedDestination())
            {
                currentPoint = (currentPoint + 1) % patrolPoints.Length;
                MoveToNextPoint();
            }
        }
        
        void MoveToNextPoint()
        {
            if (movement != null && patrolPoints != null && patrolPoints.Length > 0)
            {
                movement.MoveTo(patrolPoints[currentPoint]);
            }
        }
    }
    
    public class EngineerBehavior : MonoBehaviour
    {
        private Building repairTarget;
        private UnitMovement movement;
        private float repairRange = 3f;
        private float repairRate = 10f;
        
        public void SetRepairTarget(Building target)
        {
            repairTarget = target;
            movement = GetComponent<UnitMovement>();
            
            if (movement != null && repairTarget != null)
            {
                movement.MoveTo(repairTarget.transform.position);
            }
        }
        
        void Update()
        {
            if (repairTarget == null) return;
            
            float distance = Vector3.Distance(transform.position, repairTarget.transform.position);
            
            if (distance <= repairRange)
            {
                // Stop moving and start repairing
                if (movement != null)
                {
                    movement.StopMovement();
                }
                
                // Repair target
                repairTarget.Repair(repairRate * Time.deltaTime);
                
                // Check if fully repaired
                if (repairTarget.GetHealthPercentage() >= 1f)
                {
                    repairTarget = null;
                }
            }
        }
    }
    
    public class MedicBehavior : MonoBehaviour
    {
        private SelectableUnit healTarget;
        private UnitMovement movement;
        private float healRange = 3f;
        private float healRate = 5f;
        
        public void SetHealTarget(SelectableUnit target)
        {
            healTarget = target;
            movement = GetComponent<UnitMovement>();
            
            if (movement != null && healTarget != null)
            {
                movement.MoveTo(healTarget.transform.position);
            }
        }
        
        void Update()
        {
            if (healTarget == null || !healTarget.IsAlive()) return;
            
            float distance = Vector3.Distance(transform.position, healTarget.transform.position);
            
            if (distance <= healRange)
            {
                // Stop moving and start healing
                if (movement != null)
                {
                    movement.StopMovement();
                }
                
                // Heal target
                UnitStats targetStats = healTarget.GetComponent<UnitStats>();
                if (targetStats != null)
                {
                    targetStats.Heal(healRate * Time.deltaTime);
                    
                    // Check if fully healed
                    if (targetStats.GetHealthPercentage() >= 1f)
                    {
                        healTarget = null;
                    }
                }
            }
        }
    }
}