using System.Collections.Generic;
using UnityEngine;

namespace NexusPrime.Technology
{
    [CreateAssetMenu(fileName = "TechTreeData", menuName = "Nexus Prime/Tech Tree Data")]
    public class TechTreeData : ScriptableObject
    {
        [Header("Tech Tree Configuration")]
        public string treeName = "Main Tech Tree";
        public int maxTier = 5;
        public bool branchingAllowed = true;
        
        [Header("Technologies")]
        public List<Technology> technologies = new List<Technology>();
        
        [Header("Categories")]
        public List<TechCategoryData> categories = new List<TechCategoryData>();
        
        [Header("Visual Settings")]
        public Vector2 nodeSpacing = new Vector2(200, 150);
        public Color connectionColor = new Color(0.2f, 0.8f, 1f, 0.5f);
        public Color availableColor = new Color(0, 1, 0, 0.3f);
        public Color unavailableColor = new Color(1, 0, 0, 0.3f);
        public Color researchedColor = new Color(0, 0.5f, 1f, 0.3f);
        
        [Header("Research Bonuses")]
        public float baseResearchSpeed = 1.0f;
        public Dictionary<TechCategory, float> categoryResearchSpeeds = new Dictionary<TechCategory, float>();
        
        public TechTreeData()
        {
            InitializeDefaultCategories();
            InitializeCategorySpeeds();
        }
        
        void InitializeDefaultCategories()
        {
            categories.Clear();
            
            // Core categories
            categories.Add(new TechCategoryData(TechCategory.Construction, "Construction", 
                "Building and base development technologies", Color.yellow));
            categories.Add(new TechCategoryData(TechCategory.Economy, "Economy", 
                "Resource gathering and management technologies", Color.green));
            categories.Add(new TechCategoryData(TechCategory.Military, "Military", 
                "Combat units and warfare technologies", Color.red));
            categories.Add(new TechCategoryData(TechCategory.Defense, "Defense", 
                "Defensive structures and protection technologies", new Color(1, 0.5f, 0)));
            categories.Add(new TechCategoryData(TechCategory.Energy, "Energy", 
                "Power generation and management technologies", Color.cyan));
            categories.Add(new TechCategoryData(TechCategory.Cybernetics, "Cybernetics", 
                "Human-machine interface technologies", Color.magenta));
            categories.Add(new TechCategoryData(TechCategory.Nanotechnology, "Nanotechnology", 
                "Molecular-scale manufacturing technologies", new Color(0, 0.5f, 0)));
            categories.Add(new TechCategoryData(TechCategory.ArtificialIntelligence, "AI", 
                "Artificial intelligence and automation", new Color(0.5f, 0, 0.5f)));
        }
        
        void InitializeCategorySpeeds()
        {
            categoryResearchSpeeds.Clear();
            
            foreach (TechCategory category in System.Enum.GetValues(typeof(TechCategory)))
            {
                categoryResearchSpeeds[category] = 1.0f;
            }
        }
        
        public Technology GetTechnology(string techId)
        {
            foreach (Technology tech in technologies)
            {
                if (tech.techId == techId)
                {
                    return tech;
                }
            }
            return null;
        }
        
        public List<Technology> GetTechnologiesByCategory(TechCategory category)
        {
            List<Technology> result = new List<Technology>();
            
            foreach (Technology tech in technologies)
            {
                if (tech.category == category)
                {
                    result.Add(tech);
                }
            }
            
            return result;
        }
        
        public List<Technology> GetTechnologiesByTier(int tier)
        {
            List<Technology> result = new List<Technology>();
            
            foreach (Technology tech in technologies)
            {
                // Calculate tier based on requirements
                int techTier = CalculateTechTier(tech);
                if (techTier == tier)
                {
                    result.Add(tech);
                }
            }
            
            return result;
        }
        
        public List<Technology> GetAvailableTechnologies(List<string> researchedTechs)
        {
            List<Technology> available = new List<Technology>();
            
            foreach (Technology tech in technologies)
            {
                if (IsTechnologyAvailable(tech, researchedTechs))
                {
                    available.Add(tech);
                }
            }
            
            return available;
        }
        
        public bool IsTechnologyAvailable(Technology tech, List<string> researchedTechs)
        {
            // Already researched
            if (researchedTechs.Contains(tech.techId))
                return false;
            
            // Check requirements
            foreach (string requiredTech in tech.requiredTechs)
            {
                if (!researchedTechs.Contains(requiredTech))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        public int CalculateTechTier(Technology tech)
        {
            int maxTier = 0;
            
            foreach (string requiredTech in tech.requiredTechs)
            {
                Technology reqTech = GetTechnology(requiredTech);
                if (reqTech != null)
                {
                    int reqTier = CalculateTechTier(reqTech) + 1;
                    maxTier = Mathf.Max(maxTier, reqTier);
                }
            }
            
            return maxTier;
        }
        
        public List<string> GetPrerequisites(string techId)
        {
            List<string> prerequisites = new List<string>();
            GetPrerequisitesRecursive(techId, prerequisites);
            return prerequisites;
        }
        
        void GetPrerequisitesRecursive(string techId, List<string> prerequisites)
        {
            Technology tech = GetTechnology(techId);
            if (tech == null) return;
            
            foreach (string requiredTech in tech.requiredTechs)
            {
                if (!prerequisites.Contains(requiredTech))
                {
                    prerequisites.Add(requiredTech);
                    GetPrerequisitesRecursive(requiredTech, prerequisites);
                }
            }
        }
        
        public List<string> GetDependents(string techId)
        {
            List<string> dependents = new List<string>();
            
            foreach (Technology tech in technologies)
            {
                foreach (string requiredTech in tech.requiredTechs)
                {
                    if (requiredTech == techId && !dependents.Contains(tech.techId))
                    {
                        dependents.Add(tech.techId);
                        break;
                    }
                }
            }
            
            return dependents;
        }
        
        public float GetResearchSpeed(TechCategory category)
        {
            if (categoryResearchSpeeds.ContainsKey(category))
            {
                return baseResearchSpeed * categoryResearchSpeeds[category];
            }
            return baseResearchSpeed;
        }
        
        public void SetResearchSpeed(TechCategory category, float speed)
        {
            if (categoryResearchSpeeds.ContainsKey(category))
            {
                categoryResearchSpeeds[category] = speed;
            }
        }
        
        public void AddTechnology(Technology tech)
        {
            if (GetTechnology(tech.techId) == null)
            {
                technologies.Add(tech);
            }
            else
            {
                Debug.LogWarning($"Technology already exists: {tech.techId}");
            }
        }
        
        public void RemoveTechnology(string techId)
        {
            Technology tech = GetTechnology(techId);
            if (tech != null)
            {
                technologies.Remove(tech);
            }
        }
        
        public void UpdateTechnology(Technology updatedTech)
        {
            for (int i = 0; i < technologies.Count; i++)
            {
                if (technologies[i].techId == updatedTech.techId)
                {
                    technologies[i] = updatedTech;
                    return;
                }
            }
            
            Debug.LogWarning($"Technology not found for update: {updatedTech.techId}");
        }
        
        public void ValidateTechTree()
        {
            List<string> errors = new List<string>();
            
            // Check for duplicate tech IDs
            Dictionary<string, int> idCount = new Dictionary<string, int>();
            foreach (Technology tech in technologies)
            {
                if (idCount.ContainsKey(tech.techId))
                {
                    idCount[tech.techId]++;
                }
                else
                {
                    idCount[tech.techId] = 1;
                }
            }
            
            foreach (var kvp in idCount)
            {
                if (kvp.Value > 1)
                {
                    errors.Add($"Duplicate tech ID: {kvp.Key} ({kvp.Value} occurrences)");
                }
            }
            
            // Check for circular dependencies
            foreach (Technology tech in technologies)
            {
                if (HasCircularDependency(tech.techId, new HashSet<string>()))
                {
                    errors.Add($"Circular dependency detected for: {tech.techId}");
                }
            }
            
            // Check for missing prerequisite technologies
            foreach (Technology tech in technologies)
            {
                foreach (string requiredTech in tech.requiredTechs)
                {
                    if (GetTechnology(requiredTech) == null)
                    {
                        errors.Add($"Missing prerequisite: {requiredTech} for {tech.techId}");
                    }
                }
            }
            
            // Log errors
            if (errors.Count > 0)
            {
                Debug.LogError($"Tech Tree Validation Failed:");
                foreach (string error in errors)
                {
                    Debug.LogError($"  - {error}");
                }
            }
            else
            {
                Debug.Log("Tech Tree Validation Passed");
            }
        }
        
        bool HasCircularDependency(string techId, HashSet<string> visited)
        {
            if (visited.Contains(techId))
                return true;
            
            visited.Add(techId);
            
            Technology tech = GetTechnology(techId);
            if (tech == null) return false;
            
            foreach (string requiredTech in tech.requiredTechs)
            {
                if (HasCircularDependency(requiredTech, new HashSet<string>(visited)))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        public string ExportToJSON()
        {
            TechTreeJSONExport export = new TechTreeJSONExport();
            export.treeName = treeName;
            export.maxTier = maxTier;
            export.technologies = new List<TechnologyJSON>();
            
            foreach (Technology tech in technologies)
            {
                TechnologyJSON techJSON = new TechnologyJSON();
                techJSON.id = tech.techId;
                techJSON.name = tech.techName;
                techJSON.description = tech.description;
                techJSON.cost = tech.researchCost;
                techJSON.time = tech.researchTime;
                techJSON.requirements = tech.requiredTechs;
                techJSON.unlocks = tech.unlocks;
                techJSON.icon = tech.iconPath;
                techJSON.category = tech.category;
                techJSON.position = tech.treePosition;
                
                export.technologies.Add(techJSON);
            }
            
            return JsonUtility.ToJson(export, true);
        }
        
        public void ImportFromJSON(string json)
        {
            try
            {
                TechTreeJSONExport export = JsonUtility.FromJson<TechTreeJSONExport>(json);
                
                treeName = export.treeName;
                maxTier = export.maxTier;
                technologies.Clear();
                
                foreach (TechnologyJSON techJSON in export.technologies)
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
                    tech.treePosition = techJSON.position;
                    
                    technologies.Add(tech);
                }
                
                Debug.Log($"Tech tree imported: {treeName} with {technologies.Count} technologies");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to import tech tree from JSON: {e.Message}");
            }
        }
        
        [System.Serializable]
        class TechTreeJSONExport
        {
            public string treeName;
            public int maxTier;
            public List<TechnologyJSON> technologies;
        }
        
        [System.Serializable]
        class TechnologyJSON
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
            public Vector2 position;
        }
    }
    
    [System.Serializable]
    public class TechCategoryData
    {
        public TechCategory category;
        public string displayName;
        public string description;
        public Color color;
        public Sprite icon;
        
        public TechCategoryData(TechCategory cat, string name, string desc, Color col)
        {
            category = cat;
            displayName = name;
            description = desc;
            color = col;
        }
    }
}