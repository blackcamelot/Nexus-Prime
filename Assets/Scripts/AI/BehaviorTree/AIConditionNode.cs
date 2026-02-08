using UnityEngine;

namespace NexusPrime.AI.BehaviorTree
{
    // Additional specialized condition nodes
    
    public class AICheckTimeNode : AIConditionNode
    {
        private float checkTime;
        private System.Func<float> timeSource;
        
        public AICheckTimeNode(string name, float time, System.Func<float> source = null) 
            : base(name, () => false)
        {
            checkTime = time;
            timeSource = source ?? (() => Time.time);
            condition = CheckTimeCondition;
        }
        
        private bool CheckTimeCondition()
        {
            float currentTime = timeSource();
            return currentTime >= checkTime;
        }
    }
    
    public class AICheckRandomChanceNode : AIConditionNode
    {
        private float chance;
        
        public AICheckRandomChanceNode(string name, float probability) 
            : base(name, () => false)
        {
            chance = Mathf.Clamp01(probability);
            condition = CheckRandomCondition;
        }
        
        private bool CheckRandomCondition()
        {
            return Random.value <= chance;
        }
    }
    
    public class AICheckDistanceNode : AIConditionNode
    {
        private Vector3 pointA;
        private Vector3 pointB;
        private float maxDistance;
        private System.Func<Vector3> pointASource;
        private System.Func<Vector3> pointBSource;
        
        public AICheckDistanceNode(string name, Vector3 a, Vector3 b, float distance) 
            : base(name, () => false)
        {
            pointA = a;
            pointB = b;
            maxDistance = distance;
            condition = CheckDistanceCondition;
        }
        
        public AICheckDistanceNode(string name, System.Func<Vector3> aSource, System.Func<Vector3> bSource, float distance) 
            : base(name, () => false)
        {
            pointASource = aSource;
            pointBSource = bSource;
            maxDistance = distance;
            condition = CheckDistanceCondition;
        }
        
        private bool CheckDistanceCondition()
        {
            Vector3 a = pointASource != null ? pointASource() : pointA;
            Vector3 b = pointBSource != null ? pointBSource() : pointB;
            
            float distance = Vector3.Distance(a, b);
            return distance <= maxDistance;
        }
    }
    
    public class AICheckHealthNode : AIConditionNode
    {
        private GameObject target;
        private float minHealthPercentage;
        private System.Func<GameObject> targetSource;
        
        public AICheckHealthNode(string name, GameObject obj, float minHealth) 
            : base(name, () => false)
        {
            target = obj;
            minHealthPercentage = minHealth;
            condition = CheckHealthCondition;
        }
        
        public AICheckHealthNode(string name, System.Func<GameObject> source, float minHealth) 
            : base(name, () => false)
        {
            targetSource = source;
            minHealthPercentage = minHealth;
            condition = CheckHealthCondition;
        }
        
        private bool CheckHealthCondition()
        {
            GameObject obj = targetSource != null ? targetSource() : target;
            if (obj == null) return false;
            
            // Try to get health from various components
            UnitStats stats = obj.GetComponent<UnitStats>();
            if (stats != null)
            {
                float healthPercent = stats.GetHealthPercentage();
                return healthPercent >= minHealthPercentage;
            }
            
            Building building = obj.GetComponent<Building>();
            if (building != null)
            {
                float healthPercent = building.GetHealthPercentage();
                return healthPercent >= minHealthPercentage;
            }
            
            return false;
        }
    }
    
    public class AICheckEnemyInRangeNode : AIConditionNode
    {
        private Vector3 position;
        private float range;
        private string faction;
        
        public AICheckEnemyInRangeNode(string name, Vector3 checkPosition, float checkRange, string enemyFaction) 
            : base(name, () => false)
        {
            position = checkPosition;
            range = checkRange;
            faction = enemyFaction;
            condition = CheckEnemyInRangeCondition;
        }
        
        private bool CheckEnemyInRangeCondition()
        {
            // This would scan for enemy units in range
            // For simulation, use blackboard value
            object enemyCountObj = GetBlackboardValue($"EnemiesNear_{position}");
            if (enemyCountObj is int)
            {
                int enemyCount = (int)enemyCountObj;
                return enemyCount > 0;
            }
            
            // Simulated: 30% chance of enemy in range
            return Random.value < 0.3f;
        }
    }
    
    public class AICheckResearchCompleteNode : AIConditionNode
    {
        private string techId;
        
        public AICheckResearchCompleteNode(string name, string technologyId) 
            : base(name, () => false)
        {
            techId = technologyId;
            condition = CheckResearchCondition;
        }
        
        private bool CheckResearchCondition()
        {
            // Get research manager from blackboard
            TechTreeSystem research = GetBlackboardValue("ResearchSystem") as TechTreeSystem;
            if (research != null)
            {
                return research.IsResearched(techId);
            }
            
            return false;
        }
    }
    
    public class AICheckProductionCapacityNode : AIConditionNode
    {
        private string buildingType;
        
        public AICheckProductionCapacityNode(string name, string type) 
            : base(name, () => false)
        {
            buildingType = type;
            condition = CheckProductionCondition;
        }
        
        private bool CheckProductionCondition()
        {
            // Check if we have buildings that can produce the requested type
            // For simulation, return true 80% of the time
            return Random.value < 0.8f;
        }
    }
    
    public class AICheckVisibilityNode : AIConditionNode
    {
        private Vector3 position;
        private string targetFaction;
        
        public AICheckVisibilityNode(string name, Vector3 checkPosition, string faction) 
            : base(name, () => false)
        {
            position = checkPosition;
            targetFaction = faction;
            condition = CheckVisibilityCondition;
        }
        
        private bool CheckVisibilityCondition()
        {
            // Check if position is visible to faction
            // This would use fog of war system
            // For simulation, check distance from known bases
            return true; // Always visible for now
        }
    }
    
    public class AICheckPathAvailableNode : AIConditionNode
    {
        private Vector3 from;
        private Vector3 to;
        
        public AICheckPathAvailableNode(string name, Vector3 start, Vector3 end) 
            : base(name, () => false)
        {
            from = start;
            to = end;
            condition = CheckPathCondition;
        }
        
        private bool CheckPathCondition()
        {
            // Check if path exists between points
            // Would use NavMesh or pathfinding system
            // For simulation, check direct line of sight
            Vector3 direction = to - from;
            float distance = direction.magnitude;
            
            // Simulated: 90% chance path exists for short distances
            float probability = Mathf.Clamp01(1f - (distance / 100f));
            return Random.value < probability;
        }
    }
    
    public class AICheckWeatherNode : AIConditionNode
    {
        private string weatherCondition;
        
        public AICheckWeatherNode(string name, string condition) 
            : base(name, () => false)
        {
            weatherCondition = condition;
            condition = CheckWeatherCondition;
        }
        
        private bool CheckWeatherCondition()
        {
            // Get weather system from blackboard
            // For simulation, check blackboard or use random
            object currentWeather = GetBlackboardValue("CurrentWeather");
            if (currentWeather != null)
            {
                return currentWeather.ToString() == weatherCondition;
            }
            
            return false;
        }
    }
    
    public class AICheckTimeOfDayNode : AIConditionNode
    {
        private float startHour;
        private float endHour;
        
        public AICheckTimeOfDayNode(string name, float start, float end) 
            : base(name, () => false)
        {
            startHour = start;
            endHour = end;
            condition = CheckTimeOfDayCondition;
        }
        
        private bool CheckTimeOfDayCondition()
        {
            // Get time of day from blackboard or game manager
            object timeObj = GetBlackboardValue("TimeOfDay");
            if (timeObj is float)
            {
                float currentHour = (float)timeObj;
                
                if (startHour <= endHour)
                {
                    // Normal time range (e.g., 8:00 to 17:00)
                    return currentHour >= startHour && currentHour <= endHour;
                }
                else
                {
                    // Wrapping time range (e.g., 22:00 to 6:00)
                    return currentHour >= startHour || currentHour <= endHour;
                }
            }
            
            return false;
        }
    }
    
    public class AICheckResourceDepositNode : AIConditionNode
    {
        private ResourceType resourceType;
        private float minAmount;
        
        public AICheckResourceDepositNode(string name, ResourceType type, float amount) 
            : base(name, () => false)
        {
            resourceType = type;
            minAmount = amount;
            condition = CheckResourceDepositCondition;
        }
        
        private bool CheckResourceDepositCondition()
        {
            // Check if suitable resource deposit exists nearby
            // Would use resource scanning system
            // For simulation, return true 60% of the time
            return Random.value < 0.6f;
        }
    }
    
    public class AICheckAllianceNode : AIConditionNode
    {
        private string otherFaction;
        private bool checkAllied;
        
        public AICheckAllianceNode(string name, string faction, bool allied = true) 
            : base(name, () => false)
        {
            otherFaction = faction;
            checkAllied = allied;
            condition = CheckAllianceCondition;
        }
        
        private bool CheckAllianceCondition()
        {
            // Check diplomatic relations
            // Would use diplomacy system
            // For simulation, check blackboard
            object relationObj = GetBlackboardValue($"RelationWith_{otherFaction}");
            if (relationObj is float)
            {
                float relation = (float)relationObj;
                if (checkAllied)
                {
                    return relation > 0.7f; // Allied
                }
                else
                {
                    return relation < 0.3f; // Hostile
                }
            }
            
            return !checkAllied; // Default to hostile if no info
        }
    }
    
    public class AICheckEventNode : AIConditionNode
    {
        private string eventName;
        private bool checkActive;
        
        public AICheckEventNode(string name, string gameEvent, bool active = true) 
            : base(name, () => false)
        {
            eventName = gameEvent;
            checkActive = active;
            condition = CheckEventCondition;
        }
        
        private bool CheckEventCondition()
        {
            // Check if game event is active
            object eventObj = GetBlackboardValue($"Event_{eventName}");
            if (eventObj is bool)
            {
                bool isActive = (bool)eventObj;
                return checkActive ? isActive : !isActive;
            }
            
            return false;
        }
    }
    
    public class AICheckUnitAbilityNode : AIConditionNode
    {
        private string abilityName;
        private bool checkAvailable;
        
        public AICheckUnitAbilityNode(string name, string ability, bool available = true) 
            : base(name, () => false)
        {
            abilityName = ability;
            checkAvailable = available;
            condition = CheckAbilityCondition;
        }
        
        private bool CheckAbilityCondition()
        {
            // Check if unit has ability available
            // Would check unit's ability cooldowns
            // For simulation, return true 70% of the time
            return Random.value < 0.7f;
        }
    }
    
    public class AICheckMoraleNode : AIConditionNode
    {
        private float minMorale;
        
        public AICheckMoraleNode(string name, float morale) 
            : base(name, () => false)
        {
            minMorale = morale;
            condition = CheckMoraleCondition;
        }
        
        private bool CheckMoraleCondition()
        {
            // Check army morale
            object moraleObj = GetBlackboardValue("ArmyMorale");
            if (moraleObj is float)
            {
                float morale = (float)moraleObj;
                return morale >= minMorale;
            }
            
            return true; // Default to high morale
        }
    }
}