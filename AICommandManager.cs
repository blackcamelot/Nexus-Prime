using System.Collections.Generic;
using UnityEngine;

namespace NexusPrime.AI
{
    public class AICommandManager : MonoBehaviour
    {
        [Header("AI Configuration")]
        public AIFactionController factionController;
        public AIProfile currentProfile;
        public AIDifficulty difficulty = AIDifficulty.Normal;
        
        [Header("Command Execution")]
        public float commandInterval = 1.0f;
        public int maxCommandsPerInterval = 5;
        
        [Header("Strategic AI")]
        public AIStrategicPlanner strategicPlanner;
        public AITacticalController tacticalController;
        public AIEconomicManager economicManager;
        
        [Header("Debug")]
        public bool showDebugInfo = true;
        public bool pauseAI = false;
        
        // Internal state
        private float commandTimer;
        private Queue<AICommand> commandQueue = new Queue<AICommand>();
        private List<AIUnitGroup> unitGroups = new List<AIUnitGroup>();
        private Dictionary<string, object> blackboard = new Dictionary<string, object>();
        
        // Behavior Tree
        private AIBehaviorTree behaviorTree;
        
        void Start()
        {
            InitializeAI();
            
            if (factionController != null)
            {
                factionController.OnUnitCreated += OnUnitCreated;
                factionController.OnBuildingCreated += OnBuildingCreated;
            }
            
            Debug.Log($"AI Command Manager initialized for {factionController?.factionName}");
        }
        
        void Update()
        {
            if (pauseAI) return;
            
            UpdateTimers();
            UpdateBehaviorTree();
            ProcessCommands();
            UpdateUnitGroups();
        }
        
        void InitializeAI()
        {
            // Load AI profile based on difficulty
            currentProfile = LoadAIProfile(difficulty);
            
            // Initialize components
            if (strategicPlanner == null)
                strategicPlanner = gameObject.AddComponent<AIStrategicPlanner>();
            
            if (tacticalController == null)
                tacticalController = gameObject.AddComponent<AITacticalController>();
            
            if (economicManager == null)
                economicManager = gameObject.AddComponent<AIEconomicManager>();
            
            // Initialize behavior tree
            behaviorTree = new AIBehaviorTree();
            BuildBehaviorTree();
            
            // Initialize blackboard
            InitializeBlackboard();
            
            // Subscribe to game events
            GameManager.Instance.OnUnitDestroyed += OnUnitDestroyed;
            GameManager.Instance.OnBuildingDestroyed += OnBuildingDestroyed;
        }
        
        AIProfile LoadAIProfile(AIDifficulty diff)
        {
            // Load or create AI profile based on difficulty
            AIProfile profile = ScriptableObject.CreateInstance<AIProfile>();
            
            switch (diff)
            {
                case AIDifficulty.Easy:
                    profile.aggressionLevel = 0.3f;
                    profile.expansionRate = 0.5f;
                    profile.researchFocus = 0.4f;
                    profile.defenseFocus = 0.6f;
                    profile.economyFocus = 0.7f;
                    profile.reactionTime = 1.5f;
                    break;
                    
                case AIDifficulty.Normal:
                    profile.aggressionLevel = 0.6f;
                    profile.expansionRate = 0.8f;
                    profile.researchFocus = 0.6f;
                    profile.defenseFocus = 0.5f;
                    profile.economyFocus = 0.8f;
                    profile.reactionTime = 1.0f;
                    break;
                    
                case AIDifficulty.Hard:
                    profile.aggressionLevel = 0.9f;
                    profile.expansionRate = 1.0f;
                    profile.researchFocus = 0.8f;
                    profile.defenseFocus = 0.4f;
                    profile.economyFocus = 0.9f;
                    profile.reactionTime = 0.7f;
                    break;
                    
                case AIDifficulty.Insane:
                    profile.aggressionLevel = 1.2f;
                    profile.expansionRate = 1.5f;
                    profile.researchFocus = 1.0f;
                    profile.defenseFocus = 0.3f;
                    profile.economyFocus = 1.0f;
                    profile.reactionTime = 0.5f;
                    break;
            }
            
            return profile;
        }
        
        void BuildBehaviorTree()
        {
            // Root sequence
            AISequenceNode root = new AISequenceNode("Root");
            
            // Economic branch
            AISequenceNode economySequence = new AISequenceNode("Economy");
            economySequence.AddChild(new AIConditionNode("HasResources", () => HasSufficientResources()));
            economySequence.AddChild(new AIActionNode("ManageEconomy", ManageEconomy));
            root.AddChild(economySequence);
            
            // Military branch
            AISequenceNode militarySequence = new AISequenceNode("Military");
            militarySequence.AddChild(new AIConditionNode("HasMilitary", () => HasMilitaryUnits()));
            militarySequence.AddChild(new AIActionNode("ManageMilitary", ManageMilitary));
            root.AddChild(militarySequence);
            
            // Research branch
            AISequenceNode researchSequence = new AISequenceNode("Research");
            researchSequence.AddChild(new AIConditionNode("CanResearch", () => CanResearch()));
            researchSequence.AddChild(new AIActionNode("ManageResearch", ManageResearch));
            root.AddChild(researchSequence);
            
            // Expansion branch
            AISequenceNode expansionSequence = new AISequenceNode("Expansion");
            expansionSequence.AddChild(new AIConditionNode("ShouldExpand", () => ShouldExpand()));
            expansionSequence.AddChild(new AIActionNode("ManageExpansion", ManageExpansion));
            root.AddChild(expansionSequence);
            
            // Defense branch
            AISequenceNode defenseSequence = new AISequenceNode("Defense");
            defenseSequence.AddChild(new AIConditionNode("UnderAttack", () => IsUnderAttack()));
            defenseSequence.AddChild(new AIActionNode("ManageDefense", ManageDefense));
            root.AddChild(defenseSequence);
            
            behaviorTree.SetRoot(root);
        }
        
        void InitializeBlackboard()
        {
            blackboard.Clear();
            blackboard["LastAttackTime"] = 0f;
            blackboard["LastExpansionTime"] = 0f;
            blackboard["LastScoutTime"] = 0f;
            blackboard["ThreatLevel"] = 0f;
            blackboard["ResourcePriority"] = "Energy";
            blackboard["CurrentStrategy"] = "Economic";
            blackboard["AttackTarget"] = null;
            blackboard["DefendPosition"] = Vector3.zero;
        }
        
        void UpdateTimers()
        {
            commandTimer += Time.deltaTime;
            if (commandTimer >= commandInterval)
            {
                commandTimer = 0;
                GenerateCommands();
            }
        }
        
        void UpdateBehaviorTree()
        {
            if (behaviorTree != null)
            {
                behaviorTree.Update();
            }
        }
        
        void GenerateCommands()
        {
            if (commandQueue.Count >= 20) return; // Limit queue size
            
            // Generate commands based on current strategy
            string strategy = (string)blackboard["CurrentStrategy"];
            
            switch (strategy)
            {
                case "Economic":
                    GenerateEconomicCommands();
                    break;
                    
                case "Military":
                    GenerateMilitaryCommands();
                    break;
                    
                case "Expansion":
                    GenerateExpansionCommands();
                    break;
                    
                case "Defensive":
                    GenerateDefensiveCommands();
                    break;
            }
            
            // Always generate some defensive commands
            GenerateDefensiveCommands();
        }
        
        void GenerateEconomicCommands()
        {
            if (economicManager == null) return;
            
            // Check resource needs
            Dictionary<ResourceType, float> resourceNeeds = economicManager.CalculateResourceNeeds();
            
            foreach (var need in resourceNeeds)
            {
                if (need.Value > 0.7f) // High need
                {
                    AICommand command = new AICommand();
                    command.commandType = AICommandType.Build;
                    command.priority = AIPriority.High;
                    command.parameters = new Dictionary<string, object>
                    {
                        { "BuildingType", need.Key.ToString() + "_Generator" },
                        { "Location", FindBuildLocation() }
                    };
                    
                    EnqueueCommand(command);
                }
            }
            
            // Build additional resource buildings if economy is strong
            if (economicManager.GetEconomyStrength() > 0.8f)
            {
                AICommand command = new AICommand();
                command.commandType = AICommandType.Build;
                command.priority = AIPriority.Medium;
                command.parameters = new Dictionary<string, object>
                {
                    { "BuildingType", "Advanced_Generator" },
                    { "Location", FindBuildLocation() }
                };
                
                EnqueueCommand(command);
            }
        }
        
        void GenerateMilitaryCommands()
        {
            if (tacticalController == null) return;
            
            // Check enemy presence
            float enemyStrength = tacticalController.EstimateEnemyStrength();
            float ourStrength = tacticalController.EstimateOurStrength();
            
            if (enemyStrength > ourStrength * 1.5f)
            {
                // Build more units
                AICommand command = new AICommand();
                command.commandType = AICommandType.Train;
                command.priority = AIPriority.High;
                command.parameters = new Dictionary<string, object>
                {
                    { "UnitType", "Assault_Trooper" },
                    { "Count", 5 }
                };
                
                EnqueueCommand(command);
            }
            else if (ourStrength > enemyStrength * 2f)
            {
                // Attack
                AICommand command = new AICommand();
                command.commandType = AICommandType.Attack;
                command.priority = AIPriority.High;
                command.parameters = new Dictionary<string, object>
                {
                    { "Target", FindAttackTarget() },
                    { "ForceSize", 10 }
                };
                
                EnqueueCommand(command);
            }
            
            // Always maintain defensive forces
            if (unitGroups.Count < 3)
            {
                AICommand command = new AICommand();
                command.commandType = AICommandType.Group;
                command.priority = AIPriority.Medium;
                command.parameters = new Dictionary<string, object>
                {
                    { "GroupType", "Defensive" },
                    { "Units", 5 }
                };
                
                EnqueueCommand(command);
            }
        }
        
        void GenerateExpansionCommands()
        {
            // Find expansion location
            Vector3 expansionLocation = FindExpansionLocation();
            if (expansionLocation == Vector3.zero) return;
            
            // Build outpost
            AICommand command = new AICommand();
            command.commandType = AICommandType.Build;
            command.priority = AIPriority.High;
            command.parameters = new Dictionary<string, object>
            {
                { "BuildingType", "Outpost" },
                { "Location", expansionLocation }
            };
            
            EnqueueCommand(command);
            
            // Send scout
            if (Time.time - (float)blackboard["LastScoutTime"] > 60f)
            {
                AICommand scoutCommand = new AICommand();
                scoutCommand.commandType = AICommandType.Scout;
                scoutCommand.priority = AIPriority.Medium;
                scoutCommand.parameters = new Dictionary<string, object>
                {
                    { "Location", expansionLocation },
                    { "Units", 2 }
                };
                
                EnqueueCommand(scoutCommand);
                blackboard["LastScoutTime"] = Time.time;
            }
        }
        
        void GenerateDefensiveCommands()
        {
            // Check threat level
            float threatLevel = (float)blackboard["ThreatLevel"];
            
            if (threatLevel > 0.5f)
            {
                // Build defensive structures
                Vector3 defendPosition = (Vector3)blackboard["DefendPosition"];
                if (defendPosition != Vector3.zero)
                {
                    AICommand command = new AICommand();
                    command.commandType = AICommandType.Build;
                    command.priority = AIPriority.High;
                    command.parameters = new Dictionary<string, object>
                    {
                        { "BuildingType", "Turret" },
                        { "Location", defendPosition }
                    };
                    
                    EnqueueCommand(command);
                }
                
                // Fortify position
                AICommand fortifyCommand = new AICommand();
                fortifyCommand.commandType = AICommandType.Fortify;
                fortifyCommand.priority = AIPriority.Medium;
                fortifyCommand.parameters = new Dictionary<string, object>
                {
                    { "Location", defendPosition },
                    { "Units", 3 }
                };
                
                EnqueueCommand(fortifyCommand);
            }
        }
        
        void ProcessCommands()
        {
            int commandsProcessed = 0;
            
            while (commandQueue.Count > 0 && commandsProcessed < maxCommandsPerInterval)
            {
                AICommand command = commandQueue.Dequeue();
                ExecuteCommand(command);
                commandsProcessed++;
            }
        }
        
        void ExecuteCommand(AICommand command)
        {
            if (factionController == null) return;
            
            switch (command.commandType)
            {
                case AICommandType.Build:
                    ExecuteBuildCommand(command);
                    break;
                    
                case AICommandType.Train:
                    ExecuteTrainCommand(command);
                    break;
                    
                case AICommandType.Attack:
                    ExecuteAttackCommand(command);
                    break;
                    
                case AICommandType.Defend:
                    ExecuteDefendCommand(command);
                    break;
                    
                case AICommandType.Scout:
                    ExecuteScoutCommand(command);
                    break;
                    
                case AICommandType.Group:
                    ExecuteGroupCommand(command);
                    break;
                    
                case AICommandType.Fortify:
                    ExecuteFortifyCommand(command);
                    break;
                    
                case AICommandType.Retreat:
                    ExecuteRetreatCommand(command);
                    break;
            }
            
            Debug.Log($"AI executed command: {command.commandType}");
        }
        
        void ExecuteBuildCommand(AICommand command)
        {
            string buildingType = (string)command.parameters["BuildingType"];
            Vector3 location = (Vector3)command.parameters["Location"];
            
            factionController.BuildBuilding(buildingType, location);
        }
        
        void ExecuteTrainCommand(AICommand command)
        {
            string unitType = (string)command.parameters["UnitType"];
            int count = (int)command.parameters["Count"];
            
            for (int i = 0; i < count; i++)
            {
                factionController.TrainUnit(unitType);
            }
        }
        
        void ExecuteAttackCommand(AICommand command)
        {
            object target = command.parameters["Target"];
            int forceSize = (int)command.parameters["ForceSize"];
            
            // Create attack group
            AIUnitGroup attackGroup = new AIUnitGroup();
            attackGroup.groupType = AIGroupType.Attack;
            attackGroup.target = target;
            attackGroup.objective = "Destroy enemy";
            
            // Assign units to group
            List<SelectableUnit> availableUnits = GetAvailableUnits();
            for (int i = 0; i < Mathf.Min(forceSize, availableUnits.Count); i++)
            {
                attackGroup.AddUnit(availableUnits[i]);
            }
            
            unitGroups.Add(attackGroup);
            attackGroup.ExecuteOrders();
        }
        
        void ExecuteDefendCommand(AICommand command)
        {
            Vector3 location = (Vector3)command.parameters["Location"];
            int units = (int)command.parameters["Units"];
            
            AIUnitGroup defenseGroup = new AIUnitGroup();
            defenseGroup.groupType = AIGroupType.Defense;
            defenseGroup.position = location;
            defenseGroup.objective = "Defend position";
            
            // Assign units
            List<SelectableUnit> availableUnits = GetAvailableUnits();
            for (int i = 0; i < Mathf.Min(units, availableUnits.Count); i++)
            {
                defenseGroup.AddUnit(availableUnits[i]);
            }
            
            unitGroups.Add(defenseGroup);
            defenseGroup.ExecuteOrders();
        }
        
        void ExecuteScoutCommand(AICommand command)
        {
            Vector3 location = (Vector3)command.parameters["Location"];
            int units = (int)command.parameters["Units"];
            
            AIUnitGroup scoutGroup = new AIUnitGroup();
            scoutGroup.groupType = AIGroupType.Scout;
            scoutGroup.position = location;
            scoutGroup.objective = "Scout area";
            
            // Assign fast units
            List<SelectableUnit> fastUnits = GetFastUnits();
            for (int i = 0; i < Mathf.Min(units, fastUnits.Count); i++)
            {
                scoutGroup.AddUnit(fastUnits[i]);
            }
            
            unitGroups.Add(scoutGroup);
            scoutGroup.ExecuteOrders();
        }
        
        void ExecuteGroupCommand(AICommand command)
        {
            string groupType = (string)command.parameters["GroupType"];
            int units = (int)command.parameters["Units"];
            
            AIGroupType type = AIGroupType.Defense;
            if (groupType == "Attack") type = AIGroupType.Attack;
            else if (groupType == "Scout") type = AIGroupType.Scout;
            
            AIUnitGroup newGroup = new AIUnitGroup();
            newGroup.groupType = type;
            newGroup.position = factionController.GetHomeBasePosition();
            
            // Assign units
            List<SelectableUnit> availableUnits = GetAvailableUnits();
            for (int i = 0; i < Mathf.Min(units, availableUnits.Count); i++)
            {
                newGroup.AddUnit(availableUnits[i]);
            }
            
            unitGroups.Add(newGroup);
        }
        
        void ExecuteFortifyCommand(AICommand command)
        {
            Vector3 location = (Vector3)command.parameters["Location"];
            int units = (int)command.parameters["Units"];
            
            // Find or create defensive group at location
            AIUnitGroup defenseGroup = FindGroupAtLocation(location, AIGroupType.Defense);
            if (defenseGroup == null)
            {
                defenseGroup = new AIUnitGroup();
                defenseGroup.groupType = AIGroupType.Defense;
                defenseGroup.position = location;
                unitGroups.Add(defenseGroup);
            }
            
            // Add units to group
            List<SelectableUnit> availableUnits = GetAvailableUnits();
            for (int i = 0; i < Mathf.Min(units, availableUnits.Count); i++)
            {
                defenseGroup.AddUnit(availableUnits[i]);
            }
            
            defenseGroup.ExecuteOrders();
        }
        
        void ExecuteRetreatCommand(AICommand command)
        {
            // Find threatened groups
            foreach (AIUnitGroup group in unitGroups)
            {
                if (group.IsInCombat() && group.GetHealthPercentage() < 0.3f)
                {
                    group.Retreat(factionController.GetHomeBasePosition());
                }
            }
        }
        
        void UpdateUnitGroups()
        {
            for (int i = unitGroups.Count - 1; i >= 0; i--)
            {
                AIUnitGroup group = unitGroups[i];
                group.UpdateGroup();
                
                // Remove empty groups
                if (group.GetUnitCount() == 0)
                {
                    unitGroups.RemoveAt(i);
                }
            }
        }
        
        // Behavior Tree Actions
        bool HasSufficientResources()
        {
            if (economicManager == null) return false;
            return economicManager.HasSufficientResources();
        }
        
        bool ManageEconomy()
        {
            GenerateEconomicCommands();
            return true;
        }
        
        bool HasMilitaryUnits()
        {
            return GetAvailableUnits().Count > 0;
        }
        
        bool ManageMilitary()
        {
            GenerateMilitaryCommands();
            return true;
        }
        
        bool CanResearch()
        {
            // Check if research facilities exist and have capacity
            return true;
        }
        
        bool ManageResearch()
        {
            // Queue research based on strategy
            return true;
        }
        
        bool ShouldExpand()
        {
            if (economicManager == null) return false;
            return economicManager.ShouldExpand() && 
                   Time.time - (float)blackboard["LastExpansionTime"] > 120f;
        }
        
        bool ManageExpansion()
        {
            GenerateExpansionCommands();
            blackboard["LastExpansionTime"] = Time.time;
            return true;
        }
        
        bool IsUnderAttack()
        {
            float threatLevel = (float)blackboard["ThreatLevel"];
            return threatLevel > 0.3f;
        }
        
        bool ManageDefense()
        {
            GenerateDefensiveCommands();
            return true;
        }
        
        // Utility methods
        void EnqueueCommand(AICommand command)
        {
            commandQueue.Enqueue(command);
        }
        
        Vector3 FindBuildLocation()
        {
            if (factionController == null) return Vector3.zero;
            return factionController.GetHomeBasePosition() + Random.insideUnitSphere * 10f;
        }
        
        Vector3 FindExpansionLocation()
        {
            // Find resource-rich location away from base
            Vector3 basePos = factionController.GetHomeBasePosition();
            Vector3 direction = Random.onUnitSphere;
            direction.y = 0;
            direction.Normalize();
            
            return basePos + direction * Random.Range(30f, 50f);
        }
        
        object FindAttackTarget()
        {
            // Find enemy base or unit concentration
            return null;
        }
        
        List<SelectableUnit> GetAvailableUnits()
        {
            List<SelectableUnit> units = new List<SelectableUnit>();
            
            if (factionController != null)
            {
                units = factionController.GetIdleUnits();
            }
            
            return units;
        }
        
        List<SelectableUnit> GetFastUnits()
        {
            List<SelectableUnit> fastUnits = new List<SelectableUnit>();
            List<SelectableUnit> allUnits = GetAvailableUnits();
            
            foreach (SelectableUnit unit in allUnits)
            {
                UnitMovement movement = unit.GetComponent<UnitMovement>();
                if (movement != null && movement.moveSpeed > 5f)
                {
                    fastUnits.Add(unit);
                }
            }
            
            return fastUnits;
        }
        
        AIUnitGroup FindGroupAtLocation(Vector3 location, AIGroupType type)
        {
            foreach (AIUnitGroup group in unitGroups)
            {
                if (group.groupType == type && 
                    Vector3.Distance(group.position, location) < 10f)
                {
                    return group;
                }
            }
            return null;
        }
        
        // Event handlers
        void OnUnitCreated(SelectableUnit unit)
        {
            // Add new unit to available pool
            Debug.Log($"AI unit created: {unit.name}");
        }
        
        void OnBuildingCreated(Building building)
        {
            Debug.Log($"AI building created: {building.buildingId}");
        }
        
        void OnUnitDestroyed(SelectableUnit unit)
        {
            // Remove from groups
            foreach (AIUnitGroup group in unitGroups)
            {
                group.RemoveUnit(unit);
            }
        }
        
        void OnBuildingDestroyed(Building building)
        {
            Debug.Log($"AI building destroyed: {building.buildingId}");
            
            // Update threat level if defensive building
            if (building.definition.buildingType == BuildingType.Defense)
            {
                float threatLevel = (float)blackboard["ThreatLevel"];
                blackboard["ThreatLevel"] = threatLevel + 0.2f;
            }
        }
        
        public void SetDifficulty(AIDifficulty newDifficulty)
        {
            difficulty = newDifficulty;
            currentProfile = LoadAIProfile(newDifficulty);
            
            Debug.Log($"AI difficulty set to: {newDifficulty}");
        }
        
        public void Pause(bool pause)
        {
            pauseAI = pause;
            Debug.Log($"AI {(pause ? "paused" : "resumed")}");
        }
        
        public string GetAIDebugInfo()
        {
            if (!showDebugInfo) return "Debug info disabled";
            
            string info = $"=== AI Debug Info ===\n";
            info += $"Difficulty: {difficulty}\n";
            info += $"Current Strategy: {blackboard["CurrentStrategy"]}\n";
            info += $"Threat Level: {(float)blackboard["ThreatLevel"]:F2}\n";
            info += $"Command Queue: {commandQueue.Count}\n";
            info += $"Unit Groups: {unitGroups.Count}\n";
            
            foreach (AIUnitGroup group in unitGroups)
            {
                info += $"  - {group.groupType}: {group.GetUnitCount()} units\n";
            }
            
            if (economicManager != null)
            {
                info += $"Economy Strength: {economicManager.GetEconomyStrength():F2}\n";
            }
            
            if (tacticalController != null)
            {
                info += $"Military Strength: {tacticalController.EstimateOurStrength():F2}\n";
            }
            
            return info;
        }
        
        void OnDrawGizmos()
        {
            if (!showDebugInfo) return;
            
            // Draw AI command ranges and positions
            Gizmos.color = Color.cyan;
            if (factionController != null)
            {
                Vector3 basePos = factionController.GetHomeBasePosition();
                Gizmos.DrawWireSphere(basePos, 20f);
                
                // Draw expansion locations
                Gizmos.color = Color.yellow;
                Vector3 expansionPos = FindExpansionLocation();
                if (expansionPos != Vector3.zero)
                {
                    Gizmos.DrawSphere(expansionPos, 2f);
                    Gizmos.DrawLine(basePos, expansionPos);
                }
            }
            
            // Draw unit groups
            foreach (AIUnitGroup group in unitGroups)
            {
                switch (group.groupType)
                {
                    case AIGroupType.Attack:
                        Gizmos.color = Color.red;
                        break;
                    case AIGroupType.Defense:
                        Gizmos.color = Color.green;
                        break;
                    case AIGroupType.Scout:
                        Gizmos.color = Color.blue;
                        break;
                }
                
                Gizmos.DrawWireSphere(group.position, 3f);
                Gizmos.DrawIcon(group.position + Vector3.up * 5, "AI_Group.png");
            }
        }
    }
    
    public enum AIDifficulty
    {
        Easy,
        Normal,
        Hard,
        Insane
    }
    
    public enum AICommandType
    {
        Build,
        Train,
        Attack,
        Defend,
        Scout,
        Group,
        Fortify,
        Retreat
    }
    
    public enum AIPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
    
    [System.Serializable]
    public class AICommand
    {
        public AICommandType commandType;
        public AIPriority priority;
        public Dictionary<string, object> parameters;
        public float timestamp;
        
        public AICommand()
        {
            parameters = new Dictionary<string, object>();
            timestamp = Time.time;
        }
    }
}