using System;
using System.Collections.Generic;
using UnityEngine;

namespace NexusPrime.Technology
{
    public class TechTreeSystem : MonoBehaviour
    {
        [Header("Tech Tree Data")]
        public TechTreeData techTreeData;
        public TextAsset techTreeJSON;
        
        [Header("Research Settings")]
        public int maxSimultaneousResearch = 1;
        public float researchSpeedMultiplier = 1.0f;
        
        [Header("Current Research")]
        public List<ResearchProject> activeResearch = new List<ResearchProject>();
        public List<string> completedResearch = new List<string>();
        
        [Header("UI")]
        public TechTreeUI techTreeUI;
        
        // Events
        public delegate void ResearchEventHandler(string techId, bool completed);
        public event ResearchEventHandler OnResearchStarted;
        public event ResearchEventHandler OnResearchCompleted;
        public event ResearchEventHandler OnResearchProgress;
        
        // Internal
        private Dictionary<string, Technology> technologyDictionary = new Dictionary<string, Technology>();
        private ResourceManager resourceManager;
        private PlayerData playerData;
        
        void Awake()
        {
            resourceManager = FindObjectOfType<ResourceManager>();
            
            if (GameManager.Instance != null)
            {
                playerData = GameManager.Instance.playerData;
            }
            
            LoadTechTree();
        }
        
        void Start()
        {
            // Load completed research from player data
            if (playerData != null)
            {
                completedResearch = new List<string>(playerData.researchedTechs);
            }
            
            UpdateAvailableTechnologies();
            
            Debug.Log($"Tech Tree System initialized with {technologyDictionary.Count} technologies");
        }
        
        void Update()
        {
            UpdateResearchProjects();
        }
        
        void LoadTechTree()
        {
            if (techTreeData != null)
            {
                // Load from ScriptableObject
                foreach (Technology tech in techTreeData.technologies)
                {
                    if (!technologyDictionary.ContainsKey(tech.techId))
                    {
                        technologyDictionary.Add(tech.techId, tech);
                    }
                }
            }
            else if (techTreeJSON != null)
            {
                // Load from JSON
                LoadFromJSON(techTreeJSON.text);
            }
            else
            {
                Debug.LogError("No tech tree data provided!");
                CreateDefaultTechTree();
            }
        }
        
        void LoadFromJSON(string json)
        {
            try
            {
                TechTreeJSONData jsonData = JsonUtility.FromJson<TechTreeJSONData>(json);
                
                foreach (TechnologyJSON techJSON in jsonData.technologies)
                {
                    Technology tech = new Technology();
                    tech.techId = techJSON.id;
                    tech.techName = techJSON.name;
                    tech.description = techJSON.description;
                    tech.researchCost = techJSON.cost;
                    tech.researchTime = techJSON.time;
                    tech.requiredTechs = techJSON.requirements;
                    tech.unlocks = techJSON.unlocks;
                    tech.iconPath = techJSON.icon;
                    tech.category = techJSON.category;
                    
                    if (!technologyDictionary.ContainsKey(tech.techId))
                    {
                        technologyDictionary.Add(tech.techId, tech);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse tech tree JSON: {e.Message}");
                CreateDefaultTechTree();
            }
        }
        
        void CreateDefaultTechTree()
        {
            // Create basic tech tree
            Technology basicTech = new Technology();
            basicTech.techId = "basic_construction";
            basicTech.techName = "Basic Construction";
            basicTech.description = "Unlocks basic building capabilities";
            basicTech.researchCost = new Dictionary<ResourceType, int>();
            basicTech.researchTime = 30f;
            basicTech.requiredTechs = new string[0];
            basicTech.unlocks = new string[] { "command_center", "generator" };
            basicTech.iconPath = "Icons/Tech/Construction";
            basicTech.category = TechCategory.Construction;
            
            technologyDictionary.Add(basicTech.techId, basicTech);
        }
        
        public bool StartResearch(string techId)
        {
            if (!technologyDictionary.ContainsKey(techId))
            {
                Debug.LogError($"Technology not found: {techId}");
                return false;
            }
            
            Technology tech = technologyDictionary[techId];
            
            // Check if already researched
            if (completedResearch.Contains(techId))
            {
                Debug.LogWarning($"Technology already researched: {techId}");
                return false;
            }
            
            // Check if already researching
            foreach (ResearchProject project in activeResearch)
            {
                if (project.techId == techId)
                {
                    Debug.LogWarning($"Already researching: {techId}");
                    return false;
                }
            }
            
            // Check max simultaneous research
            if (activeResearch.Count >= maxSimultaneousResearch)
            {
                Debug.LogWarning($"Max simultaneous research reached: {maxSimultaneousResearch}");
                return false;
            }
            
            // Check requirements
            if (!CheckRequirements(techId))
            {
                Debug.LogWarning($"Requirements not met for: {techId}");
                return false;
            }
            
            // Check resources
            if (resourceManager != null && !resourceManager.CheckResources(tech.researchCost))
            {
                Debug.LogWarning($"Not enough resources for: {techId}");
                return false;
            }
            
            // Spend resources
            if (resourceManager != null)
            {
                if (!resourceManager.SpendResources(tech.researchCost))
                {
                    return false;
                }
            }
            
            // Create research project
            ResearchProject project = new ResearchProject();
            project.techId = techId;
            project.techName = tech.techName;
            project.totalTime = tech.researchTime / researchSpeedMultiplier;
            project.remainingTime = project.totalTime;
            project.startTime = Time.time;
            
            activeResearch.Add(project);
            
            // Update UI
            if (techTreeUI != null)
            {
                techTreeUI.UpdateResearchDisplay();
            }
            
            // Fire event
            OnResearchStarted?.Invoke(techId, false);
            
            Debug.Log($"Started research: {tech.techName}");
            return true;
        }
        
        public bool CancelResearch(string techId)
        {
            ResearchProject project = activeResearch.Find(p => p.techId == techId);
            if (project == null) return false;
            
            // Calculate refund (partial or full)
            float progressPercentage = 1f - (project.remainingTime / project.totalTime);
            Dictionary<ResourceType, int> refundAmount = new Dictionary<ResourceType, int>();
            
            Technology tech = technologyDictionary[techId];
            foreach (var cost in tech.researchCost)
            {
                // Refund 50% of remaining cost
                int refund = Mathf.RoundToInt(cost.Value * (1f - progressPercentage) * 0.5f);
                refundAmount[cost.Key] = refund;
            }
            
            // Add refund to resources
            if (resourceManager != null)
            {
                resourceManager.AddResources(refundAmount);
            }
            
            // Remove from active research
            activeResearch.Remove(project);
            
            // Update UI
            if (techTreeUI != null)
            {
                techTreeUI.UpdateResearchDisplay();
            }
            
            Debug.Log($"Cancelled research: {tech.techName}. Refunded: {FormatResources(refundAmount)}");
            return true;
        }
        
        void UpdateResearchProjects()
        {
            for (int i = activeResearch.Count - 1; i >= 0; i--)
            {
                ResearchProject project = activeResearch[i];
                
                // Update remaining time
                project.remainingTime -= Time.deltaTime * researchSpeedMultiplier;
                project.remainingTime = Mathf.Max(0, project.remainingTime);
                
                // Fire progress event
                OnResearchProgress?.Invoke(project.techId, false);
                
                // Check if completed
                if (project.remainingTime <= 0)
                {
                    CompleteResearch(project);
                    activeResearch.RemoveAt(i);
                }
            }
        }
        
        void CompleteResearch(ResearchProject project)
        {
            string techId = project.techId;
            
            if (!technologyDictionary.ContainsKey(techId))
            {
                Debug.LogError($"Cannot complete unknown technology: {techId}");
                return;
            }
            
            Technology tech = technologyDictionary[techId];
            
            // Add to completed research
            if (!completedResearch.Contains(techId))
            {
                completedResearch.Add(techId);
                
                // Update player data
                if (playerData != null && !playerData.researchedTechs.Contains(techId))
                {
                    playerData.researchedTechs.Add(techId);
                }
            }
            
            // Apply unlocks
            ApplyUnlocks(tech);
            
            // Update available technologies
            UpdateAvailableTechnologies();
            
            // Update UI
            if (techTreeUI != null)
            {
                techTreeUI.UpdateTechTreeDisplay();
            }
            
            // Fire event
            OnResearchCompleted?.Invoke(techId, true);
            
            Debug.Log($"Research completed: {tech.techName}");
            
            // Show notification
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowNotification(
                    "Research Complete",
                    $"{tech.techName} has been researched!",
                    NotificationType.Success
                );
            }
        }
        
        void ApplyUnlocks(Technology tech)
        {
            // Apply unlocks to game systems
            foreach (string unlock in tech.unlocks)
            {
                // Parse unlock string
                // Format: "system:item" or just "item"
                string[] parts = unlock.Split(':');
                
                if (parts.Length == 2)
                {
                    string system = parts[0];
                    string item = parts[1];
                    
                    switch (system.ToLower())
                    {
                        case "unit":
                            UnlockUnit(item);
                            break;
                        case "building":
                            UnlockBuilding(item);
                            break;
                        case "ability":
                            UnlockAbility(item);
                            break;
                        case "upgrade":
                            ApplyUpgrade(item);
                            break;
                        default:
                            Debug.LogWarning($"Unknown unlock system: {system}");
                            break;
                    }
                }
                else
                {
                    // Default to unit unlock
                    UnlockUnit(unlock);
                }
            }
            
            // Apply any immediate effects
            ApplyTechEffects(tech);
        }
        
        void UnlockUnit(string unitId)
        {
            if (playerData != null && !playerData.unlockedUnits.Contains(unitId))
            {
                playerData.unlockedUnits.Add(unitId);
                Debug.Log($"Unit unlocked: {unitId}");
            }
        }
        
        void UnlockBuilding(string buildingId)
        {
            if (playerData != null && !playerData.unlockedBuildings.Contains(buildingId))
            {
                playerData.unlockedBuildings.Add(buildingId);
                Debug.Log($"Building unlocked: {buildingId}");
            }
        }
        
        void UnlockAbility(string abilityId)
        {
            Debug.Log($"Ability unlocked: {abilityId}");
            // TODO: Implement ability unlocking
        }
        
        void ApplyUpgrade(string upgradeId)
        {
            Debug.Log($"Upgrade applied: {upgradeId}");
            // TODO: Implement upgrade application
        }
        
        void ApplyTechEffects(Technology tech)
        {
            // Apply any immediate stat bonuses
            // This could modify global stats, resource production, etc.
            
            Debug.Log($"Applied tech effects for: {tech.techName}");
        }
        
        bool CheckRequirements(string techId)
        {
            if (!technologyDictionary.ContainsKey(techId)) return false;
            
            Technology tech = technologyDictionary[techId];
            
            foreach (string requiredTech in tech.requiredTechs)
            {
                if (!completedResearch.Contains(requiredTech))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        void UpdateAvailableTechnologies()
        {
            // Mark technologies as available if requirements are met
            foreach (var kvp in technologyDictionary)
            {
                Technology tech = kvp.Value;
                tech.isAvailable = CheckRequirements(kvp.Key) && !completedResearch.Contains(kvp.Key);
            }
        }
        
        public Technology GetTechnology(string techId)
        {
            if (technologyDictionary.ContainsKey(techId))
            {
                return technologyDictionary[techId];
            }
            return null;
        }
        
        public List<Technology> GetAvailableTechnologies()
        {
            List<Technology> available = new List<Technology>();
            
            foreach (var kvp in technologyDictionary)
            {
                if (kvp.Value.isAvailable && !completedResearch.Contains(kvp.Key))
                {
                    available.Add(kvp.Value);
                }
            }
            
            return available;
        }
        
        public List<Technology> GetCompletedTechnologies()
        {
            List<Technology> completed = new List<Technology>();
            
            foreach (string techId in completedResearch)
            {
                if (technologyDictionary.ContainsKey(techId))
                {
                    completed.Add(technologyDictionary[techId]);
                }
            }
            
            return completed;
        }
        
        public List<Technology> GetTechnologiesByCategory(TechCategory category)
        {
            List<Technology> techs = new List<Technology>();
            
            foreach (var kvp in technologyDictionary)
            {
                if (kvp.Value.category == category)
                {
                    techs.Add(kvp.Value);
                }
            }
            
            return techs;
        }
        
        public bool IsResearching(string techId)
        {
            return activeResearch.Exists(p => p.techId == techId);
        }
        
        public bool IsResearched(string techId)
        {
            return completedResearch.Contains(techId);
        }
        
        public bool IsAvailable(string techId)
        {
            if (!technologyDictionary.ContainsKey(techId)) return false;
            return technologyDictionary[techId].isAvailable;
        }
        
        public float GetResearchProgress(string techId)
        {
            ResearchProject project = activeResearch.Find(p => p.techId == techId);
            if (project == null) return 0f;
            
            return 1f - (project.remainingTime / project.totalTime);
        }
        
        public ResearchProject GetActiveResearchProject(string techId)
        {
            return activeResearch.Find(p => p.techId == techId);
        }
        
        public List<ResearchProject> GetActiveResearchProjects()
        {
            return new List<ResearchProject>(activeResearch);
        }
        
        public void SetResearchSpeed(float multiplier)
        {
            researchSpeedMultiplier = Mathf.Max(0.1f, multiplier);
        }
        
        public void CheatCompleteResearch(string techId)
        {
            if (!technologyDictionary.ContainsKey(techId)) return;
            
            Technology tech = technologyDictionary[techId];
            completedResearch.Add(techId);
            ApplyUnlocks(tech);
            
            Debug.Log($"Cheat: Research completed: {tech.techName}");
        }
        
        public void CheatUnlockAllResearch()
        {
            foreach (var kvp in technologyDictionary)
            {
                if (!completedResearch.Contains(kvp.Key))
                {
                    completedResearch.Add(kvp.Key);
                    ApplyUnlocks(kvp.Value);
                }
            }
            
            Debug.Log("Cheat: All research unlocked");
        }
        
        public void ResetResearch()
        {
            activeResearch.Clear();
            completedResearch.Clear();
            
            if (playerData != null)
            {
                playerData.researchedTechs.Clear();
            }
            
            UpdateAvailableTechnologies();
            
            Debug.Log("Research reset");
        }
        
        string FormatResources(Dictionary<ResourceType, int> resources)
        {
            List<string> parts = new List<string>();
            foreach (var kvp in resources)
            {
                parts.Add($"{kvp.Value} {kvp.Key}");
            }
            return string.Join(", ", parts);
        }
        
        [System.Serializable]
        public class ResearchProject
        {
            public string techId;
            public string techName;
            public float totalTime;
            public float remainingTime;
            public float startTime;
            
            public float GetProgressPercentage()
            {
                return totalTime > 0 ? 1f - (remainingTime / totalTime) : 0f;
            }
            
            public string GetTimeRemainingString()
            {
                int minutes = Mathf.FloorToInt(remainingTime / 60);
                int seconds = Mathf.FloorToInt(remainingTime % 60);
                return $"{minutes:00}:{seconds:00}";
            }
        }
        
        [System.Serializable]
        private class TechTreeJSONData
        {
            public List<TechnologyJSON> technologies;
        }
        
        [System.Serializable]
        private class TechnologyJSON
        {
            public string id;
            public string name;
            public string description;
            public Dictionary<ResourceType, int> cost;
            public float time;
            public string[] requirements;
            public string[] unlocks;
            public string icon;
            public TechCategory category;
        }
    }
}