using UnityEngine;

namespace NexusPrime.AI
{
    [CreateAssetMenu(fileName = "AIProfile", menuName = "Nexus Prime/AI Profile")]
    public class AIProfile : ScriptableObject
    {
        [Header("Personality Traits")]
        [Range(0f, 2f)] public float aggressionLevel = 0.5f;
        [Range(0f, 1f)] public float expansionRate = 0.5f;
        [Range(0f, 1f)] public float researchFocus = 0.5f;
        [Range(0f, 1f)] public float defenseFocus = 0.5f;
        [Range(0f, 1f)] public float economyFocus = 0.5f;
        [Range(0f, 1f)] public float riskTolerance = 0.5f;
        
        [Header("Tactical Behavior")]
        [Range(0.1f, 5f)] public float reactionTime = 1.0f;
        [Range(0f, 1f)] public float flankingPreference = 0.3f;
        [Range(0f, 1f)] public float ambushPreference = 0.2f;
        [Range(0f, 1f)] public float directAttackPreference = 0.5f;
        [Range(0f, 1f)] public float retreatThreshold = 0.3f;
        
        [Header("Economic Behavior")]
        [Range(0f, 1f)] public float resourceStockpiling = 0.5f;
        [Range(0f, 1f)] public float tradePreference = 0.3f;
        [Range(0f, 1f)] public float upgradeFocus = 0.4f;
        
        [Header("Unit Composition")]
        [Range(0f, 1f)] public float infantryFocus = 0.4f;
        [Range(0f, 1f)] public float vehicleFocus = 0.3f;
        [Range(0f, 1f)] public float airFocus = 0.2f;
        [Range(0f, 1f)] public float specialFocus = 0.1f;
        
        [Header("Adaptive Behavior")]
        public bool adaptToPlayer = true;
        [Range(0f, 1f)] public float adaptationSpeed = 0.3f;
        [Range(0f, 1f)] public float learningRate = 0.5f;
        
        [Header("Special Behaviors")]
        public bool usesSpecialTactics = true;
        public bool usesDeception = false;
        public bool prioritizesHeroUnits = true;
        public bool attacksResourceLines = true;
        
        [Header("Difficulty Scaling")]
        [Range(0.5f, 3f)] public float difficultyMultiplier = 1.0f;
        
        // Dynamic values (changed during gameplay)
        [System.NonSerialized] public float currentAggression;
        [System.NonSerialized] public float currentRiskTolerance;
        [System.NonSerialized] public string preferredStrategy = "Balanced";
        
        // Performance tracking
        [System.NonSerialized] public int battlesWon;
        [System.NonSerialized] public int battlesLost;
        [System.NonSerialized] public float winRate;
        
        void OnEnable()
        {
            InitializeDynamicValues();
        }
        
        public void InitializeDynamicValues()
        {
            currentAggression = aggressionLevel;
            currentRiskTolerance = riskTolerance;
            battlesWon = 0;
            battlesLost = 0;
            winRate = 0f;
            preferredStrategy = CalculateInitialStrategy();
        }
        
        string CalculateInitialStrategy()
        {
            float maxFocus = Mathf.Max(economyFocus, aggressionLevel, defenseFocus, researchFocus);
            
            if (maxFocus == economyFocus) return "Economic";
            if (maxFocus == aggressionLevel) return "Aggressive";
            if (maxFocus == defenseFocus) return "Defensive";
            if (maxFocus == researchFocus) return "Technological";
            
            return "Balanced";
        }
        
        public void AdaptToSituation(float playerStrengthRatio, float resourceRatio, float threatLevel)
        {
            if (!adaptToPlayer) return;
            
            // Adjust aggression based on relative strength
            if (playerStrengthRatio > 1.5f) // Player is stronger
            {
                currentAggression = Mathf.Max(0.2f, aggressionLevel - 0.3f);
                currentRiskTolerance = Mathf.Max(0.1f, riskTolerance - 0.2f);
                preferredStrategy = "Defensive";
            }
            else if (playerStrengthRatio < 0.7f) // Player is weaker
            {
                currentAggression = Mathf.Min(1.8f, aggressionLevel + 0.3f);
                currentRiskTolerance = Mathf.Min(0.9f, riskTolerance + 0.2f);
                preferredStrategy = "Aggressive";
            }
            else // Roughly equal
            {
                currentAggression = aggressionLevel;
                currentRiskTolerance = riskTolerance;
                preferredStrategy = CalculateCurrentStrategy(resourceRatio, threatLevel);
            }
            
            // Adjust based on resource situation
            if (resourceRatio < 0.3f)
            {
                preferredStrategy = "Economic";
            }
            
            // Adjust based on threat level
            if (threatLevel > 0.7f)
            {
                preferredStrategy = "Defensive";
            }
        }
        
        string CalculateCurrentStrategy(float resourceRatio, float threatLevel)
        {
            // Weight different factors
            float economyWeight = economyFocus * (1f - resourceRatio);
            float aggressionWeight = currentAggression * (1f - threatLevel);
            float defenseWeight = defenseFocus * threatLevel;
            float researchWeight = researchFocus;
            
            float maxWeight = Mathf.Max(economyWeight, aggressionWeight, defenseWeight, researchWeight);
            
            if (maxWeight == economyWeight) return "Economic";
            if (maxWeight == aggressionWeight) return "Aggressive";
            if (maxWeight == defenseWeight) return "Defensive";
            if (maxWeight == researchWeight) return "Technological";
            
            return "Balanced";
        }
        
        public void RecordBattleResult(bool won)
        {
            if (won)
            {
                battlesWon++;
            }
            else
            {
                battlesLost++;
            }
            
            int totalBattles = battlesWon + battlesLost;
            winRate = totalBattles > 0 ? (float)battlesWon / totalBattles : 0f;
            
            // Learn from defeat
            if (!won && adaptToPlayer)
            {
                LearnFromDefeat();
            }
        }
        
        void LearnFromDefeat()
        {
            // Adjust behavior based on defeats
            riskTolerance = Mathf.Max(0.1f, riskTolerance - 0.1f * learningRate);
            
            // If losing too much, become more defensive
            if (winRate < 0.3f)
            {
                defenseFocus = Mathf.Min(1f, defenseFocus + 0.2f * learningRate);
                aggressionLevel = Mathf.Max(0.2f, aggressionLevel - 0.1f * learningRate);
            }
        }
        
        public string GetUnitComposition()
        {
            float total = infantryFocus + vehicleFocus + airFocus + specialFocus;
            
            if (total <= 0) return "50% Infantry, 30% Vehicles, 15% Air, 5% Special";
            
            float infantryPercent = (infantryFocus / total) * 100;
            float vehiclePercent = (vehicleFocus / total) * 100;
            float airPercent = (airFocus / total) * 100;
            float specialPercent = (specialFocus / total) * 100;
            
            return $"{infantryPercent:F0}% Infantry, {vehiclePercent:F0}% Vehicles, {airPercent:F0}% Air, {specialPercent:F0}% Special";
        }
        
        public string GetTacticalStyle()
        {
            if (aggressionLevel > 1.2f) return "Highly Aggressive";
            if (aggressionLevel > 0.8f) return "Aggressive";
            if (aggressionLevel < 0.3f) return "Very Defensive";
            if (aggressionLevel < 0.6f) return "Defensive";
            
            if (flankingPreference > 0.7f) return "Flanking Specialist";
            if (ambushPreference > 0.7f) return "Ambush Expert";
            if (directAttackPreference > 0.7f) return "Direct Assault";
            
            return "Balanced Tactics";
        }
        
        public string GetEconomicStyle()
        {
            if (economyFocus > 0.8f) return "Economic Powerhouse";
            if (economyFocus < 0.3f) return "Military Focused";
            
            if (resourceStockpiling > 0.7f) return "Resource Hoarder";
            if (tradePreference > 0.7f) return "Trader";
            if (upgradeFocus > 0.7f) return "Upgrade Focused";
            
            return "Balanced Economy";
        }
        
        public float GetAttackDecisionScore(float advantage, float distance, float reinforcementTime)
        {
            // Calculate whether to attack based on multiple factors
            float score = 0f;
            
            // Advantage factor
            score += advantage * 0.4f;
            
            // Distance factor (closer is better)
            score += (1f - Mathf.Clamp01(distance / 100f)) * 0.3f;
            
            // Reinforcement time factor (shorter is better)
            score += (1f - Mathf.Clamp01(reinforcementTime / 60f)) * 0.2f;
            
            // Personality factor
            score += currentAggression * 0.1f;
            
            // Apply risk tolerance
            score *= currentRiskTolerance;
            
            return score;
        }
        
        public bool ShouldAttack(float advantage, float distance, float reinforcementTime)
        {
            float score = GetAttackDecisionScore(advantage, distance, reinforcementTime);
            return score > 0.5f; // Threshold for attack
        }
        
        public bool ShouldRetreat(float healthPercentage, float enemyStrengthRatio)
        {
            // Check health threshold
            if (healthPercentage < retreatThreshold)
                return true;
            
            // Check if heavily outnumbered
            if (enemyStrengthRatio > 3f)
                return true;
            
            // Risk tolerance affects retreat decision
            return enemyStrengthRatio > (2f - currentRiskTolerance);
        }
        
        public string GetProfileDescription()
        {
            string description = $"AI Personality: {preferredStrategy}\n";
            description += $"Tactical Style: {GetTacticalStyle()}\n";
            description += $"Economic Style: {GetEconomicStyle()}\n";
            description += $"Unit Composition: {GetUnitComposition()}\n";
            description += $"Aggression Level: {currentAggression:F2}\n";
            description += $"Risk Tolerance: {currentRiskTolerance:F2}\n";
            
            if (adaptToPlayer)
            {
                description += $"Adaptation: Active (Speed: {adaptationSpeed:F2})\n";
            }
            
            description += $"Performance: {battlesWon}W/{battlesLost}L ({winRate:P0})\n";
            
            return description;
        }
        
        public AIProfile Clone()
        {
            AIProfile clone = CreateInstance<AIProfile>();
            
            // Copy all serialized fields
            clone.aggressionLevel = aggressionLevel;
            clone.expansionRate = expansionRate;
            clone.researchFocus = researchFocus;
            clone.defenseFocus = defenseFocus;
            clone.economyFocus = economyFocus;
            clone.riskTolerance = riskTolerance;
            
            clone.reactionTime = reactionTime;
            clone.flankingPreference = flankingPreference;
            clone.ambushPreference = ambushPreference;
            clone.directAttackPreference = directAttackPreference;
            clone.retreatThreshold = retreatThreshold;
            
            clone.resourceStockpiling = resourceStockpiling;
            clone.tradePreference = tradePreference;
            clone.upgradeFocus = upgradeFocus;
            
            clone.infantryFocus = infantryFocus;
            clone.vehicleFocus = vehicleFocus;
            clone.airFocus = airFocus;
            clone.specialFocus = specialFocus;
            
            clone.adaptToPlayer = adaptToPlayer;
            clone.adaptationSpeed = adaptationSpeed;
            clone.learningRate = learningRate;
            
            clone.usesSpecialTactics = usesSpecialTactics;
            clone.usesDeception = usesDeception;
            clone.prioritizesHeroUnits = prioritizesHeroUnits;
            clone.attacksResourceLines = attacksResourceLines;
            
            clone.difficultyMultiplier = difficultyMultiplier;
            
            // Initialize dynamic values
            clone.InitializeDynamicValues();
            
            return clone;
        }
        
        public void ApplyDifficultyMultiplier()
        {
            // Apply difficulty multiplier to key stats
            aggressionLevel *= difficultyMultiplier;
            expansionRate *= difficultyMultiplier;
            reactionTime /= difficultyMultiplier;
            
            // Clamp values
            aggressionLevel = Mathf.Clamp(aggressionLevel, 0.1f, 2f);
            expansionRate = Mathf.Clamp(expansionRate, 0.1f, 2f);
            reactionTime = Mathf.Clamp(reactionTime, 0.2f, 3f);
        }
        
        public static AIProfile CreateDefaultProfile(string name, AIDifficulty difficulty)
        {
            AIProfile profile = CreateInstance<AIProfile>();
            profile.name = name;
            
            switch (difficulty)
            {
                case AIDifficulty.Easy:
                    profile.difficultyMultiplier = 0.7f;
                    profile.aggressionLevel = 0.4f;
                    profile.reactionTime = 1.5f;
                    profile.adaptToPlayer = false;
                    break;
                    
                case AIDifficulty.Normal:
                    profile.difficultyMultiplier = 1.0f;
                    profile.aggressionLevel = 0.6f;
                    profile.reactionTime = 1.0f;
                    break;
                    
                case AIDifficulty.Hard:
                    profile.difficultyMultiplier = 1.5f;
                    profile.aggressionLevel = 0.8f;
                    profile.reactionTime = 0.7f;
                    profile.adaptToPlayer = true;
                    profile.adaptationSpeed = 0.5f;
                    break;
                    
                case AIDifficulty.Insane:
                    profile.difficultyMultiplier = 2.0f;
                    profile.aggressionLevel = 1.0f;
                    profile.reactionTime = 0.5f;
                    profile.adaptToPlayer = true;
                    profile.adaptationSpeed = 0.8f;
                    profile.learningRate = 0.7f;
                    break;
            }
            
            profile.ApplyDifficultyMultiplier();
            profile.InitializeDynamicValues();
            
            return profile;
        }
    }
}