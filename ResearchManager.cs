using System.Collections.Generic;
using UnityEngine;

namespace NexusPrime.Technology
{
    public class ResearchManager : MonoBehaviour
    {
        [Header("References")]
        public TechTreeSystem techTreeSystem;
        public ResourceManager resourceManager;
        public UIManager uiManager;
        
        [Header("Research Stations")]
        public List<ResearchStation> researchStations = new List<ResearchStation>();
        public int maxResearchStations = 3;
        
        [Header("Research Bonuses")]
        public float globalResearchSpeed = 1.0f;
        public Dictionary<TechCategory, float> categoryBonuses = new Dictionary<TechCategory, float>();
        public List<ResearchBonus> activeBonuses = new List<ResearchBonus>();
        
        [Header("Auto Research")]
        public bool autoResearchEnabled = false;
        public TechCategory autoResearchCategory = TechCategory.General;
        public List<string> researchPriority = new List<string>();
        
        // Events
        public delegate void ResearchEventHandler(string techId, float progress);
        public event ResearchEventHandler OnResearchProgress;
        public event ResearchEventHandler OnResearchStationAdded;
        
        // Internal
        private float researchEfficiency = 1.0f;
        private List<string> queuedResearch = new List<string>();
        
        void Start()
        {
            InitializeCategoryBonuses();
            
            if (techTreeSystem == null)
            {
                techTreeSystem = FindObjectOfType<TechTreeSystem>();
            }
            
            if (resourceManager == null)
            {
                resourceManager = FindObjectOfType<ResourceManager>();
            }
            
            if (uiManager == null)
            {
                uiManager = FindObjectOfType<UIManager>();
            }
            
            // Subscribe to events
            if (techTreeSystem != null)
            {
                techTreeSystem.OnResearchStarted += OnResearchStarted;
                techTreeSystem.OnResearchCompleted += OnResearchCompleted;
                techTreeSystem.OnResearchProgress += OnResearchProgressUpdate;
            }
            
            Debug.Log("Research Manager initialized");
        }
        
        void Update()
        {
            UpdateResearchEfficiency();
            UpdateAutoResearch();
            UpdateResearchStations();
        }
        
        void InitializeCategoryBonuses()
        {
            foreach (TechCategory category in System.Enum.GetValues(typeof(TechCategory)))
            {
                categoryBonuses[category] = 1.0f;
            }
        }
        
        public bool StartResearch(string techId)
        {
            if (techTreeSystem == null) return false;
            
            // Check if we have available research stations
            if (GetAvailableResearchStations() <= 0)
            {
                Debug.LogWarning("No available research stations");
                return false;
            }
            
            // Start research through tech tree system
            bool success = techTreeSystem.StartResearch(techId);
            
            if (success)
            {
                // Assign to research station
                AssignResearchToStation(techId);
                
                // Update UI
                if (uiManager != null)
                {
                    uiManager.UpdateResearchDisplay();
                }
            }
            
            return success;
        }
        
        public bool QueueResearch(string techId)
        {
            if (techTreeSystem == null) return false;
            
            Technology tech = techTreeSystem.GetTechnology(techId);
            if (tech == null) return false;
            
            // Check if already in queue
            if (queuedResearch.Contains(techId))
            {
                Debug.LogWarning($"Research already queued: {techId}");
                return false;
            }
            
            // Check if already researched
            if (techTreeSystem.IsResearched(techId))
            {
                Debug.LogWarning($"Research already completed: {techId}");
                return false;
            }
            
            // Check if already researching
            if (techTreeSystem.IsResearching(techId))
            {
                Debug.LogWarning($"Already researching: {techId}");
                return false;
            }
            
            queuedResearch.Add(techId);
            Debug.Log($"Research queued: {tech.techName}");
            
            return true;
        }
        
        public bool CancelResearch(string techId)
        {
            if (techTreeSystem == null) return false;
            
            bool success = techTreeSystem.CancelResearch(techId);
            
            if (success)
            {
                // Free research station
                FreeResearchStation(techId);
                
                // Remove from queue if present
                queuedResearch.Remove(techId);
                
                // Update UI
                if (uiManager != null)
                {
                    uiManager.UpdateResearchDisplay();
                }
            }
            
            return success;
        }
        
        public bool CancelQueuedResearch(string techId)
        {
            if (queuedResearch.Contains(techId))
            {
                queuedResearch.Remove(techId);
                Debug.Log($"Queued research cancelled: {techId}");
                return true;
            }
            return false;
        }
        
        void UpdateAutoResearch()
        {
            if (!autoResearchEnabled) return;
            if (techTreeSystem == null) return;
            
            // Check if we have available research capacity
            if (GetAvailableResearchStations() > 0)
            {
                // Find next research to start
                string nextResearch = GetNextAutoResearch();
                if (!string.IsNullOrEmpty(nextResearch))
                {
                    StartResearch(nextResearch);
                }
            }
        }
        
        string GetNextAutoResearch()
        {
            // Check priority list first
            foreach (string techId in researchPriority)
            {
                if (CanResearch(techId) && !queuedResearch.Contains(techId))
                {
                    return techId;
                }
            }
            
            // Then check by category
            List<Technology> availableTechs = techTreeSystem.GetAvailableTechnologies();
            foreach (Technology tech in availableTechs)
            {
                if (tech.category == autoResearchCategory && 
                    CanResearch(tech.techId) && 
                    !queuedResearch.Contains(tech.techId))
                {
                    return tech.techId;
                }
            }
            
            // Then any available research
            foreach (Technology tech in availableTechs)
            {
                if (CanResearch(tech.techId) && !queuedResearch.Contains(tech.techId))
                {
                    return tech.techId;
                }
            }
            
            return null;
        }
        
        bool CanResearch(string techId)
        {
            if (techTreeSystem == null) return false;
            
            Technology tech = techTreeSystem.GetTechnology(techId);
            if (tech == null) return false;
            
            // Check resources
            if (resourceManager != null && !resourceManager.CheckResources(tech.researchCost))
            {
                return false;
            }
            
            // Check if available
            if (!techTreeSystem.IsAvailable(techId))
            {
                return false;
            }
            
            return true;
        }
        
        void UpdateResearchEfficiency()
        {
            researchEfficiency = globalResearchSpeed;
            
            // Apply station bonuses
            foreach (ResearchStation station in researchStations)
            {
                if (station.IsOperational())
                {
                    researchEfficiency *= station.efficiency;
                }
            }
            
            // Apply category bonuses
            foreach (var kvp in categoryBonuses)
            {
                // This would be applied per research project based on category
            }
            
            // Apply active bonuses
            foreach (ResearchBonus bonus in activeBonuses)
            {
                if (bonus.IsActive())
                {
                    researchEfficiency *= bonus.researchSpeedMultiplier;
                }
            }
            
            // Update tech tree system
            if (techTreeSystem != null)
            {
                techTreeSystem.SetResearchSpeed(researchEfficiency);
            }
        }
        
        void UpdateResearchStations()
        {
            // Update each research station
            foreach (ResearchStation station in researchStations)
            {
                station.UpdateStation();
            }
        }
        
        void AssignResearchToStation(string techId)
        {
            // Find available research station
            foreach (ResearchStation station in researchStations)
            {
                if (!station.IsResearching() && station.IsOperational())
                {
                    station.StartResearch(techId);
                    return;
                }
            }
            
            Debug.LogWarning($"No available research station for: {techId}");
        }
        
        void FreeResearchStation(string techId)
        {
            // Find station researching this tech
            foreach (ResearchStation station in researchStations)
            {
                if (station.currentResearch == techId)
                {
                    station.CancelResearch();
                    return;
                }
            }
        }
        
        public void AddResearchStation(ResearchStation station)
        {
            if (!researchStations.Contains(station))
            {
                researchStations.Add(station);
                
                OnResearchStationAdded?.Invoke(station.techId, 0f);
                
                Debug.Log($"Research station added: {station.stationName}");
            }
        }
        
        public void RemoveResearchStation(ResearchStation station)
        {
            if (researchStations.Contains(station))
            {
                // Cancel any ongoing research
                if (!string.IsNullOrEmpty(station.currentResearch))
                {
                    CancelResearch(station.currentResearch);
                }
                
                researchStations.Remove(station);
                Debug.Log($"Research station removed: {station.stationName}");
            }
        }
        
        public int GetAvailableResearchStations()
        {
            int available = 0;
            
            foreach (ResearchStation station in researchStations)
            {
                if (!station.IsResearching() && station.IsOperational())
                {
                    available++;
                }
            }
            
            return available;
        }
        
        public int GetTotalResearchStations()
        {
            return researchStations.Count;
        }
        
        public float GetResearchEfficiency()
        {
            return researchEfficiency;
        }
        
        public float GetCategoryBonus(TechCategory category)
        {
            if (categoryBonuses.ContainsKey(category))
            {
                return categoryBonuses[category];
            }
            return 1.0f;
        }
        
        public void SetCategoryBonus(TechCategory category, float bonus)
        {
            if (categoryBonuses.ContainsKey(category))
            {
                categoryBonuses[category] = Mathf.Max(0.1f, bonus);
            }
        }
        
        public void AddResearchBonus(ResearchBonus bonus)
        {
            if (!activeBonuses.Contains(bonus))
            {
                activeBonuses.Add(bonus);
                bonus.Activate();
            }
        }
        
        public void RemoveResearchBonus(ResearchBonus bonus)
        {
            if (activeBonuses.Contains(bonus))
            {
                activeBonuses.Remove(bonus);
                bonus.Deactivate();
            }
        }
        
        public List<string> GetQueuedResearch()
        {
            return new List<string>(queuedResearch);
        }
        
        public void ClearResearchQueue()
        {
            queuedResearch.Clear();
            Debug.Log("Research queue cleared");
        }
        
        public void ReorderResearchQueue(int oldIndex, int newIndex)
        {
            if (oldIndex >= 0 && oldIndex < queuedResearch.Count &&
                newIndex >= 0 && newIndex < queuedResearch.Count)
            {
                string techId = queuedResearch[oldIndex];
                queuedResearch.RemoveAt(oldIndex);
                queuedResearch.Insert(newIndex, techId);
            }
        }
        
        void OnResearchStarted(string techId, bool completed)
        {
            if (!completed)
            {
                Debug.Log($"Research started: {techId}");
                
                // Remove from queue if present
                queuedResearch.Remove(techId);
                
                // Update UI
                if (uiManager != null)
                {
                    uiManager.ShowResearchStartedNotification(techId);
                }
            }
        }
        
        void OnResearchCompleted(string techId, bool completed)
        {
            if (completed)
            {
                Debug.Log($"Research completed: {techId}");
                
                // Update UI
                if (uiManager != null)
                {
                    uiManager.ShowResearchCompleteNotification(techId);
                }
                
                // Check for next queued research
                if (queuedResearch.Count > 0)
                {
                    string nextResearch = queuedResearch[0];
                    StartResearch(nextResearch);
                }
            }
        }
        
        void OnResearchProgressUpdate(string techId, bool completed)
        {
            if (!completed)
            {
                float progress = techTreeSystem.GetResearchProgress(techId);
                OnResearchProgress?.Invoke(techId, progress);
            }
        }
        
        public void EnableAutoResearch(bool enable)
        {
            autoResearchEnabled = enable;
            Debug.Log($"Auto research {(enable ? "enabled" : "disabled")}");
        }
        
        public void SetAutoResearchCategory(TechCategory category)
        {
            autoResearchCategory = category;
            Debug.Log($"Auto research category set to: {category}");
        }
        
        public void AddResearchPriority(string techId)
        {
            if (!researchPriority.Contains(techId))
            {
                researchPriority.Add(techId);
            }
        }
        
        public void RemoveResearchPriority(string techId)
        {
            researchPriority.Remove(techId);
        }
        
        public void ClearResearchPriority()
        {
            researchPriority.Clear();
        }
        
        public string GetCurrentResearchStatus()
        {
            if (techTreeSystem == null) return "No tech tree system";
            
            List<ResearchProject> activeProjects = techTreeSystem.GetActiveResearchProjects();
            if (activeProjects.Count == 0)
            {
                return "No active research";
            }
            
            string status = $"Active Research ({activeProjects.Count}):\n";
            foreach (ResearchProject project in activeProjects)
            {
                float progress = project.GetProgressPercentage() * 100f;
                status += $"- {project.techName}: {progress:F1}% ({project.GetTimeRemainingString()})\n";
            }
            
            if (queuedResearch.Count > 0)
            {
                status += $"\nQueued Research ({queuedResearch.Count}):\n";
                for (int i = 0; i < queuedResearch.Count; i++)
                {
                    Technology tech = techTreeSystem.GetTechnology(queuedResearch[i]);
                    if (tech != null)
                    {
                        status += $"{i + 1}. {tech.techName}\n";
                    }
                }
            }
            
            status += $"\nResearch Efficiency: {researchEfficiency:F2}x";
            
            return status;
        }
        
        void OnDestroy()
        {
            // Unsubscribe from events
            if (techTreeSystem != null)
            {
                techTreeSystem.OnResearchStarted -= OnResearchStarted;
                techTreeSystem.OnResearchCompleted -= OnResearchCompleted;
                techTreeSystem.OnResearchProgress -= OnResearchProgressUpdate;
            }
        }
    }
    
    [System.Serializable]
    public class ResearchStation
    {
        public string stationId;
        public string stationName;
        public float efficiency = 1.0f;
        public float maxEfficiency = 2.0f;
        public bool isOperational = true;
        public string currentResearch;
        public float researchProgress;
        
        public ResearchStation(string id, string name)
        {
            stationId = id;
            stationName = name;
            efficiency = 1.0f;
            isOperational = true;
            currentResearch = string.Empty;
            researchProgress = 0f;
        }
        
        public bool StartResearch(string techId)
        {
            if (!IsOperational() || IsResearching()) return false;
            
            currentResearch = techId;
            researchProgress = 0f;
            
            Debug.Log($"Research station '{stationName}' started researching: {techId}");
            return true;
        }
        
        public void CancelResearch()
        {
            if (string.IsNullOrEmpty(currentResearch)) return;
            
            string oldResearch = currentResearch;
            currentResearch = string.Empty;
            researchProgress = 0f;
            
            Debug.Log($"Research station '{stationName}' cancelled research: {oldResearch}");
        }
        
        public void UpdateStation()
        {
            if (!IsOperational() || !IsResearching()) return;
            
            // Update research progress
            // This would be called by ResearchManager
        }
        
        public bool IsResearching()
        {
            return !string.IsNullOrEmpty(currentResearch);
        }
        
        public bool IsOperational()
        {
            return isOperational;
        }
        
        public void SetOperational(bool operational)
        {
            isOperational = operational;
        }
        
        public void SetEfficiency(float newEfficiency)
        {
            efficiency = Mathf.Clamp(newEfficiency, 0.1f, maxEfficiency);
        }
        
        public void UpgradeEfficiency(float multiplier)
        {
            efficiency = Mathf.Min(maxEfficiency, efficiency * multiplier);
        }
        
        public string GetStatus()
        {
            string status = $"{stationName}: ";
            
            if (!isOperational)
            {
                status += "Offline";
            }
            else if (IsResearching())
            {
                status += $"Researching {currentResearch} ({researchProgress * 100:F1}%)";
            }
            else
            {
                status += "Idle";
            }
            
            status += $", Efficiency: {efficiency:F2}x";
            
            return status;
        }
    }
    
    [System.Serializable]
    public class ResearchBonus
    {
        public string bonusId;
        public string bonusName;
        public float researchSpeedMultiplier = 1.0f;
        public float duration = 0f; // 0 = permanent
        public float timeRemaining = 0f;
        public bool isActive = false;
        
        public ResearchBonus(string id, string name, float multiplier, float dur = 0f)
        {
            bonusId = id;
            bonusName = name;
            researchSpeedMultiplier = multiplier;
            duration = dur;
            timeRemaining = dur;
            isActive = false;
        }
        
        public void Activate()
        {
            isActive = true;
            timeRemaining = duration;
            
            Debug.Log($"Research bonus activated: {bonusName} ({researchSpeedMultiplier}x)");
        }
        
        public void Deactivate()
        {
            isActive = false;
            
            Debug.Log($"Research bonus deactivated: {bonusName}");
        }
        
        public void Update(float deltaTime)
        {
            if (!isActive || duration <= 0) return;
            
            timeRemaining -= deltaTime;
            if (timeRemaining <= 0)
            {
                Deactivate();
            }
        }
        
        public bool IsActive()
        {
            return isActive;
        }
        
        public bool IsPermanent()
        {
            return duration <= 0;
        }
        
        public string GetTimeRemainingString()
        {
            if (duration <= 0) return "Permanent";
            
            int minutes = Mathf.FloorToInt(timeRemaining / 60);
            int seconds = Mathf.FloorToInt(timeRemaining % 60);
            return $"{minutes:00}:{seconds:00}";
        }
    }
}