using UnityEngine;

namespace NexusPrime.AI.BehaviorTree
{
    // Specialized action nodes for common AI behaviors
    
    public class AITrainUnitsNode : AIActionNode
    {
        private string unitType;
        private int count;
        private int trained;
        
        public AITrainUnitsNode(string name, string type, int unitCount) 
            : base(name, () => NodeStatus.Ready)
        {
            unitType = type;
            count = unitCount;
            trained = 0;
            action = TrainUnitsAction;
        }
        
        private NodeStatus TrainUnitsAction()
        {
            // Get AI controller from blackboard
            AIFactionController controller = GetBlackboardValue("AIController") as AIFactionController;
            if (controller == null)
            {
                Debug.LogError("No AI controller in blackboard!");
                return NodeStatus.Failure;
            }
            
            // Train units
            if (trained < count)
            {
                bool success = controller.TrainUnit(unitType);
                if (success)
                {
                    trained++;
                    Debug.Log($"Training unit {trained}/{count} of type {unitType}");
                }
                
                return NodeStatus.Running;
            }
            
            // All units trained
            Debug.Log($"Completed training {count} {unitType} units");
            return NodeStatus.Success;
        }
        
        public override void Reset()
        {
            base.Reset();
            trained = 0;
        }
    }
    
    public class AIBuildBuildingNode : AIActionNode
    {
        private string buildingType;
        private Vector3 location;
        private bool locationSet;
        
        public AIBuildBuildingNode(string name, string type) 
            : base(name, () => NodeStatus.Ready)
        {
            buildingType = type;
            locationSet = false;
            action = BuildBuildingAction;
        }
        
        private NodeStatus BuildBuildingAction()
        {
            // Get AI controller
            AIFactionController controller = GetBlackboardValue("AIController") as AIFactionController;
            if (controller == null)
            {
                return NodeStatus.Failure;
            }
            
            // Find build location if not set
            if (!locationSet)
            {
                location = FindBuildLocation(controller);
                if (location == Vector3.zero)
                {
                    return NodeStatus.Failure;
                }
                locationSet = true;
            }
            
            // Build the building
            bool success = controller.BuildBuilding(buildingType, location);
            
            if (success)
            {
                Debug.Log($"Built {buildingType} at {location}");
                return NodeStatus.Success;
            }
            
            return NodeStatus.Running; // Might need to wait for resources
        }
        
        private Vector3 FindBuildLocation(AIFactionController controller)
        {
            Vector3 basePos = controller.GetHomeBasePosition();
            
            // Try multiple locations around base
            for (int i = 0; i < 10; i++)
            {
                Vector3 testLocation = basePos + Random.insideUnitSphere * 15f;
                testLocation.y = 0;
                
                // Simple validation - would need proper placement check
                if (Vector3.Distance(testLocation, basePos) > 5f)
                {
                    return testLocation;
                }
            }
            
            return Vector3.zero;
        }
        
        public override void Reset()
        {
            base.Reset();
            locationSet = false;
        }
    }
    
    public class AIAttackTargetNode : AIActionNode
    {
        private Vector3 target;
        private float proximityThreshold = 5f;
        
        public AIAttackTargetNode(string name, Vector3 attackTarget) 
            : base(name, () => NodeStatus.Ready)
        {
            target = attackTarget;
            action = AttackTargetAction;
        }
        
        private NodeStatus AttackTargetAction()
        {
            // Get unit group from blackboard
            AIUnitGroup group = GetBlackboardValue("CurrentGroup") as AIUnitGroup;
            if (group == null)
            {
                return NodeStatus.Failure;
            }
            
            // Check if target is still valid
            object blackboardTarget = GetBlackboardValue("AttackTarget");
            if (blackboardTarget is Vector3)
            {
                target = (Vector3)blackboardTarget;
            }
            
            // Send group to attack
            group.SetAttackTarget(target);
            
            // Check if target reached
            float distance = Vector3.Distance(group.position, target);
            if (distance < proximityThreshold)
            {
                Debug.Log($"Attack group reached target at {target}");
                return NodeStatus.Success;
            }
            
            return NodeStatus.Running;
        }
    }
    
    public class AIDefendPositionNode : AIActionNode
    {
        private Vector3 position;
        private float defenseRadius = 10f;
        
        public AIDefendPositionNode(string name, Vector3 defendPosition) 
            : base(name, () => NodeStatus.Ready)
        {
            position = defendPosition;
            action = DefendPositionAction;
        }
        
        private NodeStatus DefendPositionAction()
        {
            // Get unit group
            AIUnitGroup group = GetBlackboardValue("CurrentGroup") as AIUnitGroup;
            if (group == null)
            {
                return NodeStatus.Failure;
            }
            
            // Set defense orders
            group.SetDefensePosition(position, defenseRadius);
            
            // Check if position needs reinforcement
            float threatLevel = CalculateThreatLevel(position);
            SetBlackboardValue("DefenseThreatLevel", threatLevel);
            
            if (threatLevel > 0.7f)
            {
                // Under heavy attack
                Debug.Log($"Defense position under heavy attack! Threat: {threatLevel:F2}");
                return NodeStatus.Running;
            }
            
            // Position secure
            return NodeStatus.Success;
        }
        
        private float CalculateThreatLevel(Vector3 position)
        {
            // Count enemy units in radius
            int enemyCount = 0;
            
            // This would actually scan for enemies
            // For now, return a simulated value
            return Mathf.PingPong(Time.time * 0.1f, 1f);
        }
    }
    
    public class AIScoutAreaNode : AIActionNode
    {
        private Vector3 areaCenter;
        private float scoutRadius = 30f;
        private float scoutedPercentage;
        
        public AIScoutAreaNode(string name, Vector3 center) 
            : base(name, () => NodeStatus.Ready)
        {
            areaCenter = center;
            scoutedPercentage = 0f;
            action = ScoutAreaAction;
        }
        
        private NodeStatus ScoutAreaAction()
        {
            // Get scout group
            AIUnitGroup group = GetBlackboardValue("ScoutGroup") as AIUnitGroup;
            if (group == null)
            {
                return NodeStatus.Failure;
            }
            
            // Simulate scouting progress
            scoutedPercentage += Time.deltaTime * 0.1f; // 10% per second
            
            // Update blackboard with discovered information
            if (scoutedPercentage > 0.3f)
            {
                SetBlackboardValue("EnemyBaseLocated", true);
                SetBlackboardValue("EnemyBasePosition", areaCenter + new Vector3(10, 0, 10));
            }
            
            if (scoutedPercentage > 0.6f)
            {
                SetBlackboardValue("ResourceNodesFound", 3);
            }
            
            if (scoutedPercentage >= 1f)
            {
                Debug.Log($"Area scouting complete at {areaCenter}");
                return NodeStatus.Success;
            }
            
            return NodeStatus.Running;
        }
        
        public override void Reset()
        {
            base.Reset();
            scoutedPercentage = 0f;
        }
    }
    
    public class AIWaitNode : AIActionNode
    {
        private float waitTime;
        private float startTime;
        
        public AIWaitNode(string name, float seconds) 
            : base(name, () => NodeStatus.Ready)
        {
            waitTime = seconds;
            action = WaitAction;
        }
        
        private NodeStatus WaitAction()
        {
            if (status == NodeStatus.Ready)
            {
                startTime = Time.time;
                Debug.Log($"Waiting for {waitTime} seconds");
            }
            
            if (Time.time - startTime >= waitTime)
            {
                return NodeStatus.Success;
            }
            
            return NodeStatus.Running;
        }
        
        public override void Reset()
        {
            base.Reset();
            startTime = 0f;
        }
    }
    
    public class AICheckResourcesNode : AIConditionNode
    {
        private ResourceType resourceType;
        private float requiredAmount;
        
        public AICheckResourcesNode(string name, ResourceType type, float amount) 
            : base(name, () => false)
        {
            resourceType = type;
            requiredAmount = amount;
            condition = CheckResourcesCondition;
        }
        
        private bool CheckResourcesCondition()
        {
            // Get resource manager
            ResourceManager rm = GetBlackboardValue("ResourceManager") as ResourceManager;
            if (rm == null)
            {
                return false;
            }
            
            float currentAmount = rm.GetResourceAmount(resourceType);
            return currentAmount >= requiredAmount;
        }
    }
    
    public class AICheckThreatNode : AIConditionNode
    {
        private float threatThreshold;
        
        public AICheckThreatNode(string name, float threshold) 
            : base(name, () => false)
        {
            threatThreshold = threshold;
            condition = CheckThreatCondition;
        }
        
        private bool CheckThreatCondition()
        {
            // Get threat level from blackboard
            object threatObj = GetBlackboardValue("ThreatLevel");
            if (threatObj is float)
            {
                float threatLevel = (float)threatObj;
                return threatLevel >= threatThreshold;
            }
            
            return false;
        }
    }
    
    public class AICheckUnitCountNode : AIConditionNode
    {
        private string unitType;
        private int requiredCount;
        
        public AICheckUnitCountNode(string name, string type, int count) 
            : base(name, () => false)
        {
            unitType = type;
            requiredCount = count;
            condition = CheckUnitCountCondition;
        }
        
        private bool CheckUnitCountCondition()
        {
            // Get AI controller
            AIFactionController controller = GetBlackboardValue("AIController") as AIFactionController;
            if (controller == null)
            {
                return false;
            }
            
            // Count units of specified type
            List<SelectableUnit> units = controller.GetUnitsByType(unitType);
            return units.Count >= requiredCount;
        }
    }
    
    public class AICheckBuildingCountNode : AIConditionNode
    {
        private string buildingType;
        private int requiredCount;
        
        public AICheckBuildingCountNode(string name, string type, int count) 
            : base(name, () => false)
        {
            buildingType = type;
            requiredCount = count;
            condition = CheckBuildingCountCondition;
        }
        
        private bool CheckBuildingCountCondition()
        {
            // This would check if we have enough buildings of a type
            // For now, return true (simulated)
            return true;
        }
    }
    
    public class AISetBlackboardNode : AIActionNode
    {
        private string key;
        private object value;
        
        public AISetBlackboardNode(string name, string keyName, object val) 
            : base(name, () => NodeStatus.Ready)
        {
            key = keyName;
            value = val;
            action = SetBlackboardAction;
        }
        
        private NodeStatus SetBlackboardAction()
        {
            SetBlackboardValue(key, value);
            Debug.Log($"Set blackboard '{key}' = {value}");
            return NodeStatus.Success;
        }
    }
    
    public class AILogNode : AIActionNode
    {
        private string message;
        
        public AILogNode(string name, string logMessage) 
            : base(name, () => NodeStatus.Ready)
        {
            message = logMessage;
            action = LogAction;
        }
        
        private NodeStatus LogAction()
        {
            Debug.Log($"AI Log [{name}]: {message}");
            return NodeStatus.Success;
        }
    }
}