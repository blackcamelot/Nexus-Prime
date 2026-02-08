using System.Collections.Generic;
using UnityEngine;
using NexusPrime.Units;
using NexusPrime.Building;
using NexusPrime.Economy;

namespace NexusPrime.AI
{
    public class AIFactionController : MonoBehaviour
    {
        [Header("Faction Configuration")]
        public string factionName = "AI Faction";
        public string factionId = "ai_01";
        public Color factionColor = Color.red;
        public AIProfile aiProfile;
        
        [Header("Base Management")]
        public Vector3 homeBasePosition;
        public float baseRadius = 20f;
        public List<Building> ownedBuildings = new List<Building>();
        public List<SelectableUnit> ownedUnits = new List<SelectableUnit>();
        
        [Header("Resource Management")]
        public Dictionary<ResourceType, float> resourceStockpile = new Dictionary<ResourceType, float>();
        public float resourceCollectionRate = 1.0f;
        
        [Header("Production")]
        public List<string> productionQueue = new List<string>();
        public int maxProductionQueue = 10;
        
        [Header("Intelligence")]
        public List<EnemyIntel> enemyIntel = new List<EnemyIntel>();
        public List<ResourceNodeIntel> resourceIntel = new List<ResourceNodeIntel>();
        
        [Header("Combat")]
        public List<AIUnitGroup> combatGroups = new List<AIUnitGroup>();
        public List<Vector3> knownEnemyPositions = new List<Vector3>();
        
        // Events
        public delegate void AIFactionEventHandler(object sender);
        public event AIFactionEventHandler OnBaseEstablished;
        public event AIFactionEventHandler OnResourceChanged;
        public event AIFactionEventHandler OnUnitCreated;
        public event AIFactionEventHandler OnBuildingCreated;
        public event AIFactionEventHandler OnUnderAttack;
        
        // References
        private ResourceManager resourceManager;
        private BuildingSystem buildingSystem;
        private UnitFactory unitFactory;
        private BuildingFactory buildingFactory;
        
        // State
        private bool isActive = true;
        private float decisionTimer = 0f;
        private float intelligenceUpdateTimer = 0f;
        
        void Start()
        {
            InitializeFaction();
            EstablishBase();
            
            Debug.Log($"AI Faction Controller initialized: {factionName}");
        }
        
        void Update()
        {
            if (!isActive) return;
            
            UpdateTimers();
            UpdateIntelligence();
            MakeDecisions();
            UpdateProduction();
            UpdateCombatGroups();
        }
        
        void InitializeFaction()
        {
            // Get references
            resourceManager = FindObjectOfType<ResourceManager>();
            buildingSystem = FindObjectOfType<BuildingSystem>();
            unitFactory = FindObjectOfType<UnitFactory>();
            buildingFactory = FindObjectOfType<BuildingFactory>();
            
            // Initialize resource stockpile
            foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
            {
                resourceStockpile[type] = 0f;
            }
            
            // Set starting resources based on difficulty
            if (aiProfile != null)
            {
                resourceStockpile[ResourceType.Credits] = 1000 * aiProfile.economyFocus;
                resourceStockpile[ResourceType.Energy] = 500 * aiProfile.economyFocus;
                resourceStockpile[ResourceType.Nanites] = 100 * aiProfile.economyFocus;
            }
            
            // Register with GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterAIFaction(this);
            }
        }
        
        void EstablishBase()
        {
            // Find suitable base location
            if (homeBasePosition == Vector3.zero)
            {
                homeBasePosition = FindSuitableBaseLocation();
            }
            
            // Build initial structures
            BuildInitialStructures();
            
            // Fire event
            OnBaseEstablished?.Invoke(this);
            
            Debug.Log($"Base established at: {homeBasePosition}");
        }
        
        Vector3 FindSuitableBaseLocation()
        {
            // Simple implementation - find flat area away from other bases
            Vector3 position = new Vector3(
                Random.Range(-50f, 50f),
                0,
                Random.Range(-50f, 50f)
            );
            
            // Raycast to find ground
            RaycastHit hit;
            if (Physics.Raycast(position + Vector3.up * 100, Vector3.down, out hit, 200f))
            {
                position = hit.point;
            }
            
            return position;
        }
        
        void BuildInitialStructures()
        {
            // Build command center
            BuildBuilding("command_center", homeBasePosition);
            
            // Build generators around command center
            Vector3[] generatorPositions = {
                homeBasePosition + new Vector3(5, 0, 5),
                homeBasePosition + new Vector3(-5, 0, 5),
                homeBasePosition + new Vector3(5, 0, -5),
                homeBasePosition + new Vector3(-5, 0, -5)
            };
            
            foreach (Vector3 pos in generatorPositions)
            {
                BuildBuilding("generator", pos);
            }
            
            // Build barracks
            BuildBuilding("barracks", homeBasePosition + new Vector3(8, 0, 0));
        }
        
        void UpdateTimers()
        {
            decisionTimer += Time.deltaTime;
            intelligenceUpdateTimer += Time.deltaTime;
            
            // Make decisions at intervals based on reaction time
            if (decisionTimer >= aiProfile.reactionTime)
            {
                decisionTimer = 0;
            }
            
            // Update intelligence every few seconds
            if (intelligenceUpdateTimer >= 5f)
            {
                intelligenceUpdateTimer = 0;
                GatherIntelligence();
            }
        }
        
        void UpdateIntelligence()
        {
            // Update known enemy positions
            UpdateEnemyPositions();
            
            // Update resource information
            UpdateResourceIntel();
        }
        
        void GatherIntelligence()
        {
            // Scout surrounding area
            ScoutArea();
            
            // Analyze enemy strength
            AnalyzeEnemyStrength();
            
            // Evaluate resource availability
            EvaluateResources();
        }
        
        void ScoutArea()
        {
            // Send scout units if available
            List<SelectableUnit> scoutUnits = GetUnitsByType("Scout");
            if (scoutUnits.Count > 0)
            {
                foreach (SelectableUnit scout in scoutUnits)
                {
                    Vector3 scoutLocation = homeBasePosition + Random.onUnitSphere * 30f;
                    scoutLocation.y = 0;
                    
                    UnitMovement movement = scout.GetComponent<UnitMovement>();
                    if (movement != null)
                    {
                        movement.MoveTo(scoutLocation);
                    }
                }
            }
        }
        
        void AnalyzeEnemyStrength()
        {
            // Estimate enemy strength based on known information
            float enemyStrength = 0f;
            
            foreach (EnemyIntel intel in enemyIntel)
            {
                enemyStrength += intel.estimatedStrength;
            }
            
            // Adjust aggression based on relative strength
            float ourStrength = EstimateOurStrength();
            float strengthRatio = ourStrength / Mathf.Max(1f, enemyStrength);
            
            // Update AI profile based on strength ratio
            if (strengthRatio > 2f)
            {
                aiProfile.aggressionLevel = Mathf.Min(1.2f, aiProfile.aggressionLevel + 0.1f);
            }
            else if (strengthRatio < 0.5f)
            {
                aiProfile.aggressionLevel = Mathf.Max(0.2f, aiProfile.aggressionLevel - 0.1f);
                aiProfile.defenseFocus = Mathf.Min(1f, aiProfile.defenseFocus + 0.1f);
            }
        }
        
        void EvaluateResources()
        {
            // Check resource levels
            float resourceDeficit = 0f;
            foreach (var kvp in resourceStockpile)
            {
                float desiredAmount = GetDesiredResourceAmount(kvp.Key);
                if (kvp.Value < desiredAmount * 0.3f)
                {
                    resourceDeficit++;
                }
            }
            
            // Adjust economy focus based on resource deficit
            if (resourceDeficit > 2)
            {
                aiProfile.economyFocus = Mathf.Min(1f, aiProfile.economyFocus + 0.1f);
            }
            else if (resourceDeficit == 0)
            {
                aiProfile.economyFocus = Mathf.Max(0.3f, aiProfile.economyFocus - 0.05f);
            }
        }
        
        void MakeDecisions()
        {
            // Make strategic decisions based on current situation
            
            // Economic decisions
            MakeEconomicDecisions();
            
            // Military decisions
            MakeMilitaryDecisions();
            
            // Expansion decisions
            MakeExpansionDecisions();
            
            // Research decisions
            MakeResearchDecisions();
        }
        
        void MakeEconomicDecisions()
        {
            // Build additional resource generators if needed
            if (ShouldBuildResourceGenerator())
            {
                string generatorType = GetPriorityResourceGenerator();
                Vector3 buildLocation = FindBuildLocationNearBase();
                
                if (buildLocation != Vector3.zero)
                {
                    BuildBuilding(generatorType, buildLocation);
                }
            }
            
            // Upgrade existing buildings
            if (ShouldUpgradeBuildings())
            {
                UpgradeRandomBuilding();
            }
        }
        
        void MakeMilitaryDecisions()
        {
            // Train units based on aggression level
            if (ShouldTrainUnits())
            {
                string unitType = GetPriorityUnitType();
                int count = GetUnitTrainingCount();
                
                for (int i = 0; i < count; i++)
                {
                    TrainUnit(unitType);
                }
            }
            
            // Form combat groups
            if (ShouldFormCombatGroup())
            {
                FormCombatGroup();
            }
            
            // Launch attacks
            if (ShouldLaunchAttack())
            {
                LaunchAttack();
            }
        }
        
        void MakeExpansionDecisions()
        {
            // Expand to new locations
            if (ShouldExpand())
            {
                Vector3 expansionLocation = FindExpansionLocation();
                if (expansionLocation != Vector3.zero)
                {
                    BuildOutpost(expansionLocation);
                }
            }
        }
        
        void MakeResearchDecisions()
        {
            // Start research projects
            if (ShouldResearch())
            {
                string researchTech = GetPriorityResearch();
                StartResearch(researchTech);
            }
        }
        
        void UpdateProduction()
        {
            // Process production queue
            if (productionQueue.Count > 0)
            {
                string currentProduction = productionQueue[0];
                
                // Check if production is complete (simplified)
                // In real implementation, this would track progress
                
                // For now, just remove after a delay
                if (Time.time % 10f < 0.1f)
                {
                    CompleteProduction(currentProduction);
                }
            }
        }
        
        void UpdateCombatGroups()
        {
            // Update all combat groups
            for (int i = combatGroups.Count - 1; i >= 0; i--)
            {
                AIUnitGroup group = combatGroups[i];
                group.UpdateGroup();
                
                // Remove empty groups
                if (group.GetUnitCount() == 0)
                {
                    combatGroups.RemoveAt(i);
                }
            }
        }
        
        // Building management
        public bool BuildBuilding(string buildingId, Vector3 position)
        {
            if (buildingFactory == null || buildingSystem == null) return false;
            
            // Check if we can afford the building
            BuildingDefinition definition = buildingFactory.GetBuildingDefinition(buildingId);
            if (definition == null) return false;
            
            if (!CanAfford(definition.cost)) return false;
            
            // Spend resources
            SpendResources(definition.cost);
            
            // Create building
            GameObject buildingObj = buildingFactory.CreateBuilding(
                buildingId,
                position,
                Quaternion.identity,
                factionId
            );
            
            if (buildingObj != null)
            {
                Building building = buildingObj.GetComponent<Building>();
                if (building != null)
                {
                    ownedBuildings.Add(building);
                    OnBuildingCreated?.Invoke(building);
                    
                    Debug.Log($"AI built {buildingId} at {position}");
                    return true;
                }
            }
            
            return false;
        }
        
        void BuildOutpost(Vector3 location)
        {
            BuildBuilding("outpost", location);
            
            // Send some units to defend outpost
            AIUnitGroup defenseGroup = new AIUnitGroup();
            defenseGroup.groupType = AIGroupType.Defense;
            defenseGroup.position = location;
            defenseGroup.objective = "Defend outpost";
            
            // Assign units
            List<SelectableUnit> availableUnits = GetIdleUnits();
            for (int i = 0; i < Mathf.Min(3, availableUnits.Count); i++)
            {
                defenseGroup.AddUnit(availableUnits[i]);
            }
            
            combatGroups.Add(defenseGroup);
            defenseGroup.ExecuteOrders();
        }
        
        // Unit management
        public bool TrainUnit(string unitId)
        {
            if (unitFactory == null) return false;
            
            // Check if we have barracks
            if (!HasBuildingType("barracks")) return false;
            
            // Check if we can afford the unit
            Dictionary<ResourceType, int> unitCost = unitFactory.GetUnitCost(unitId);
            if (!CanAfford(unitCost)) return false;
            
            // Add to production queue
            if (productionQueue.Count < maxProductionQueue)
            {
                productionQueue.Add(unitId);
                
                // Spend resources immediately
                SpendResources(unitCost);
                
                Debug.Log($"AI queued unit training: {unitId}");
                return true;
            }
            
            return false;
        }
        
        void CompleteProduction(string unitId)
        {
            productionQueue.RemoveAt(0);
            
            // Create unit
            Vector3 spawnLocation = homeBasePosition + Random.insideUnitSphere * 5f;
            spawnLocation.y = 0;
            
            GameObject unitObj = unitFactory.CreateUnit(
                unitId,
                spawnLocation,
                Quaternion.identity,
                factionId
            );
            
            if (unitObj != null)
            {
                SelectableUnit unit = unitObj.GetComponent<SelectableUnit>();
                if (unit != null)
                {
                    ownedUnits.Add(unit);
                    OnUnitCreated?.Invoke(unit);
                    
                    Debug.Log($"AI trained unit: {unitId}");
                }
            }
        }
        
        // Research management
        public bool StartResearch(string techId)
        {
            // Check if we have research lab
            if (!HasBuildingType("research_lab")) return false;
            
            // Check research requirements and cost
            // Implementation depends on research system
            
            Debug.Log($"AI started research: {techId}");
            return true;
        }
        
        // Resource management
        bool CanAfford(Dictionary<ResourceType, int> cost)
        {
            foreach (var kvp in cost)
            {
                if (!resourceStockpile.ContainsKey(kvp.Key) || 
                    resourceStockpile[kvp.Key] < kvp.Value)
                {
                    return false;
                }
            }
            return true;
        }
        
        void SpendResources(Dictionary<ResourceType, int> cost)
        {
            foreach (var kvp in cost)
            {
                if (resourceStockpile.ContainsKey(kvp.Key))
                {
                    resourceStockpile[kvp.Key] -= kvp.Value;
                }
            }
            
            OnResourceChanged?.Invoke(this);
        }
        
        void AddResources(Dictionary<ResourceType, int> amount)
        {
            foreach (var kvp in amount)
            {
                if (resourceStockpile.ContainsKey(kvp.Key))
                {
                    resourceStockpile[kvp.Key] += kvp.Value;
                }
            }
            
            OnResourceChanged?.Invoke(this);
        }
        
        // Intelligence gathering
        void UpdateEnemyPositions()
        {
            // Clear old positions
            knownEnemyPositions.Clear();
            
            // Get all enemy units
            List<SelectableUnit> allUnits = GameManager.Instance.GetAllUnits();
            foreach (SelectableUnit unit in allUnits)
            {
                if (unit.GetComponent<CombatUnit>()?.faction != factionId)
                {
                    knownEnemyPositions.Add(unit.transform.position);
                }
            }
        }
        
        void UpdateResourceIntel()
        {
            // Update knowledge of resource nodes
            // This would scan for resource producers in the world
        }
        
        // Decision conditions
        bool ShouldBuildResourceGenerator()
        {
            float resourceThreshold = 0.3f;
            
            foreach (var kvp in resourceStockpile)
            {
                float desired = GetDesiredResourceAmount(kvp.Key);
                if (kvp.Value < desired * resourceThreshold)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        bool ShouldUpgradeBuildings()
        {
            // Upgrade if economy is strong and not under threat
            float economyStrength = GetEconomyStrength();
            float threatLevel = GetThreatLevel();
            
            return economyStrength > 0.7f && threatLevel < 0.3f;
        }
        
        bool ShouldTrainUnits()
        {
            // Train units based on aggression level and current military strength
            float militaryStrength = EstimateOurStrength();
            float desiredStrength = aiProfile.aggressionLevel * 100f;
            
            return militaryStrength < desiredStrength && ownedUnits.Count < 50;
        }
        
        bool ShouldFormCombatGroup()
        {
            // Form groups if we have enough idle units
            int idleUnits = GetIdleUnits().Count;
            return idleUnits >= 5 && combatGroups.Count < 5;
        }
        
        bool ShouldLaunchAttack()
        {
            // Launch attack if we have strong military and high aggression
            float militaryStrength = EstimateOurStrength();
            float enemyStrength = EstimateEnemyStrength();
            
            return militaryStrength > enemyStrength * 1.5f && 
                   aiProfile.aggressionLevel > 0.7f &&
                   combatGroups.Count > 0;
        }
        
        bool ShouldExpand()
        {
            // Expand if economy is strong and not under threat
            float economyStrength = GetEconomyStrength();
            float threatLevel = GetThreatLevel();
            
            return economyStrength > 0.8f && 
                   threatLevel < 0.2f && 
                   ownedBuildings.Count < 20;
        }
        
        bool ShouldResearch()
        {
            // Research if we have research facility and resources
            return HasBuildingType("research_lab") && 
                   resourceStockpile[ResourceType.Data] > 100;
        }
        
        // Utility methods
        string GetPriorityResourceGenerator()
        {
            // Find most needed resource
            ResourceType mostNeeded = ResourceType.Credits;
            float lowestPercentage = 1f;
            
            foreach (var kvp in resourceStockpile)
            {
                float desired = GetDesiredResourceAmount(kvp.Key);
                float percentage = kvp.Value / desired;
                
                if (percentage < lowestPercentage)
                {
                    lowestPercentage = percentage;
                    mostNeeded = kvp.Key;
                }
            }
            
            return mostNeeded.ToString().ToLower() + "_generator";
        }
        
        string GetPriorityUnitType()
        {
            // Choose unit type based on situation
            if (GetThreatLevel() > 0.5f)
            {
                return "assault_trooper"; // Defensive units
            }
            else if (aiProfile.aggressionLevel > 0.8f)
            {
                return "heavy_tank"; // Offensive units
            }
            else
            {
                return "basic_infantry"; // Balanced units
            }
        }
        
        string GetPriorityResearch()
        {
            // Choose research based on strategy
            if (aiProfile.economyFocus > aiProfile.aggressionLevel)
            {
                return "advanced_economy";
            }
            else
            {
                return "advanced_weapons";
            }
        }
        
        int GetUnitTrainingCount()
        {
            return Mathf.RoundToInt(aiProfile.aggressionLevel * 5);
        }
        
        Vector3 FindBuildLocationNearBase()
        {
            Vector3 location = homeBasePosition + Random.insideUnitSphere * 15f;
            location.y = 0;
            
            // Check if location is valid
            if (buildingSystem != null && buildingSystem.CanPlaceBuilding("generator", location))
            {
                return location;
            }
            
            return Vector3.zero;
        }
        
        Vector3 FindExpansionLocation()
        {
            Vector3 direction = Random.onUnitSphere;
            direction.y = 0;
            direction.Normalize();
            
            return homeBasePosition + direction * Random.Range(30f, 50f);
        }
        
        void FormCombatGroup()
        {
            AIUnitGroup newGroup = new AIUnitGroup();
            newGroup.groupType = AIGroupType.Attack;
            newGroup.position = homeBasePosition;
            newGroup.objective = "Patrol";
            
            // Add idle units
            List<SelectableUnit> idleUnits = GetIdleUnits();
            for (int i = 0; i < Mathf.Min(5, idleUnits.Count); i++)
            {
                newGroup.AddUnit(idleUnits[i]);
            }
            
            combatGroups.Add(newGroup);
            newGroup.ExecuteOrders();
        }
        
        void LaunchAttack()
        {
            if (combatGroups.Count == 0) return;
            
            // Find enemy target
            Vector3 target = knownEnemyPositions.Count > 0 ? 
                knownEnemyPositions[0] : 
                homeBasePosition + new Vector3(50, 0, 50);
            
            // Send all combat groups to attack
            foreach (AIUnitGroup group in combatGroups)
            {
                if (group.groupType == AIGroupType.Attack)
                {
                    group.SetAttackTarget(target);
                }
            }
        }
        
        void UpgradeRandomBuilding()
        {
            if (ownedBuildings.Count == 0) return;
            
            Building building = ownedBuildings[Random.Range(0, ownedBuildings.Count)];
            building.Upgrade(1.2f, 1.1f);
        }
        
        // Getters
        public List<SelectableUnit> GetIdleUnits()
        {
            List<SelectableUnit> idleUnits = new List<SelectableUnit>();
            
            foreach (SelectableUnit unit in ownedUnits)
            {
                UnitMovement movement = unit.GetComponent<UnitMovement>();
                if (movement != null && !movement.IsMoving())
                {
                    idleUnits.Add(unit);
                }
            }
            
            return idleUnits;
        }
        
        public List<SelectableUnit> GetUnitsByType(string unitType)
        {
            List<SelectableUnit> units = new List<SelectableUnit>();
            
            foreach (SelectableUnit unit in ownedUnits)
            {
                if (unit.unitId.Contains(unitType.ToLower()))
                {
                    units.Add(unit);
                }
            }
            
            return units;
        }
        
        public bool HasBuildingType(string buildingType)
        {
            foreach (Building building in ownedBuildings)
            {
                if (building.buildingId.Contains(buildingType))
                {
                    return true;
                }
            }
            return false;
        }
        
        public float GetEconomyStrength()
        {
            float totalResources = 0f;
            foreach (var kvp in resourceStockpile)
            {
                totalResources += kvp.Value;
            }
            
            float maxResources = 10000f; // Arbitrary max
            return Mathf.Clamp01(totalResources / maxResources);
        }
        
        public float GetThreatLevel()
        {
            // Calculate threat based on nearby enemy units
            float threat = 0f;
            
            foreach (Vector3 enemyPos in knownEnemyPositions)
            {
                float distance = Vector3.Distance(homeBasePosition, enemyPos);
                if (distance < 30f)
                {
                    threat += (30f - distance) / 30f;
                }
            }
            
            return Mathf.Clamp01(threat);
        }
        
        public float EstimateOurStrength()
        {
            float strength = 0f;
            
            foreach (SelectableUnit unit in ownedUnits)
            {
                CombatUnit combat = unit.GetComponent<CombatUnit>();
                if (combat != null)
                {
                    UnitStats stats = combat.GetComponent<UnitStats>();
                    if (stats != null)
                    {
                        strength += stats.currentHealth * stats.damage;
                    }
                }
            }
            
            return strength;
        }
        
        public float EstimateEnemyStrength()
        {
            float strength = 0f;
            
            // Estimate based on known enemies
            foreach (EnemyIntel intel in enemyIntel)
            {
                strength += intel.estimatedStrength;
            }
            
            return strength;
        }
        
        float GetDesiredResourceAmount(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Credits: return 5000;
                case ResourceType.Energy: return 3000;
                case ResourceType.Nanites: return 1000;
                case ResourceType.Data: return 500;
                default: return 1000;
            }
        }
        
        public Vector3 GetHomeBasePosition()
        {
            return homeBasePosition;
        }
        
        public void SetActive(bool active)
        {
            isActive = active;
        }
        
        public string GetFactionStatus()
        {
            string status = $"=== {factionName} Status ===\n";
            status += $"Active: {isActive}\n";
            status += $"Buildings: {ownedBuildings.Count}\n";
            status += $"Units: {ownedUnits.Count}\n";
            status += $"Combat Groups: {combatGroups.Count}\n";
            status += $"Production Queue: {productionQueue.Count}/{maxProductionQueue}\n";
            
            status += $"Resources:\n";
            foreach (var kvp in resourceStockpile)
            {
                float desired = GetDesiredResourceAmount(kvp.Key);
                float percentage = kvp.Value / desired;
                status += $"  {kvp.Key}: {kvp.Value:F0}/{desired:F0} ({percentage:P0})\n";
            }
            
            status += $"AI Profile:\n";
            status += $"  Aggression: {aiProfile.aggressionLevel:F2}\n";
            status += $"  Economy Focus: {aiProfile.economyFocus:F2}\n";
            status += $"  Defense Focus: {aiProfile.defenseFocus:F2}\n";
            
            return status;
        }
        
        void OnDrawGizmosSelected()
        {
            // Draw base radius
            Gizmos.color = factionColor;
            Gizmos.DrawWireSphere(homeBasePosition, baseRadius);
            
            // Draw owned buildings
            foreach (Building building in ownedBuildings)
            {
                if (building != null)
                {
                    Gizmos.color = factionColor;
                    Gizmos.DrawWireCube(building.transform.position, Vector3.one * 2);
                }
            }
            
            // Draw combat groups
            foreach (AIUnitGroup group in combatGroups)
            {
                Gizmos.color = group.groupType == AIGroupType.Attack ? Color.red : Color.green;
                Gizmos.DrawWireSphere(group.position, 5f);
                
                // Draw group movement
                if (group.targetPosition != Vector3.zero)
                {
                    Gizmos.DrawLine(group.position, group.targetPosition);
                }
            }
            
            // Draw known enemy positions
            Gizmos.color = Color.red;
            foreach (Vector3 enemyPos in knownEnemyPositions)
            {
                Gizmos.DrawSphere(enemyPos, 1f);
            }
        }
    }
    
    [System.Serializable]
    public class EnemyIntel
    {
        public string factionId;
        public Vector3 lastKnownPosition;
        public float lastUpdateTime;
        public float estimatedStrength;
        public List<string> knownUnits = new List<string>();
        public List<string> knownBuildings = new List<string>();
        
        public EnemyIntel(string faction)
        {
            factionId = faction;
            lastKnownPosition = Vector3.zero;
            lastUpdateTime = Time.time;
            estimatedStrength = 0f;
        }
    }
    
    [System.Serializable]
    public class ResourceNodeIntel
    {
        public Vector3 position;
        public ResourceType resourceType;
        public float estimatedAmount;
        public bool isBeingHarvested;
        
        public ResourceNodeIntel(Vector3 pos, ResourceType type, float amount)
        {
            position = pos;
            resourceType = type;
            estimatedAmount = amount;
            isBeingHarvested = false;
        }
    }
    
    public enum AIGroupType
    {
        Attack,
        Defense,
        Scout
    }
    
    [System.Serializable]
    public class AIUnitGroup
    {
        public AIGroupType groupType;
        public Vector3 position;
        public Vector3 targetPosition;
        public Vector3 target;
        public string objective;
        private List<SelectableUnit> units = new List<SelectableUnit>();
        
        public void AddUnit(SelectableUnit unit)
        {
            if (unit != null && !units.Contains(unit)) units.Add(unit);
        }
        
        public void RemoveUnit(SelectableUnit unit)
        {
            units.Remove(unit);
        }
        
        public void SetAttackTarget(Vector3 targetPos)
        {
            target = targetPos;
            targetPosition = targetPos;
        }
        
        public void Retreat(Vector3 toPosition)
        {
            targetPosition = toPosition;
            foreach (var u in units)
            {
                if (u != null)
                {
                    var movement = u.GetComponent<UnitMovement>();
                    if (movement != null) movement.SetDestination(toPosition);
                }
            }
        }
        
        public void ExecuteOrders()
        {
            if (targetPosition != Vector3.zero)
            {
                foreach (var u in units)
                {
                    if (u != null)
                    {
                        var movement = u.GetComponent<UnitMovement>();
                        if (movement != null) movement.SetDestination(targetPosition);
                    }
                }
            }
        }
        
        public int GetUnitCount()
        {
            return units != null ? units.Count : 0;
        }
    }
}