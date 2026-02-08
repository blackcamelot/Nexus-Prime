using System;
using System.Collections.Generic;
using UnityEngine;

namespace NexusPrime.Core
{
    [System.Serializable]
    public class PlayerData
    {
        // Player Identity
        public string playerId;
        public string playerName;
        public DateTime creationDate;
        public DateTime lastPlayed;
        
        // Progress
        public int playerLevel;
        public int experience;
        public int totalPlayTime; // in seconds
        
        // Resources
        public int credits;
        public int energy;
        public int nanites;
        public int data;
        public int influence;
        
        // Faction and Alignment
        public string factionId;
        public float alignmentScore; // -100 to 100
        
        // Unlocks
        public List<string> researchedTechs;
        public List<string> unlockedUnits;
        public List<string> unlockedBuildings;
        public List<string> completedMissions;
        
        // Multiplayer Stats
        public int wins;
        public int losses;
        public int draws;
        public int rating;
        
        // Settings
        public GameSettings gameSettings;
        
        // Campaign Progress
        public int currentCampaignMission;
        public Dictionary<string, MissionProgress> missionProgress;
        
        public PlayerData()
        {
            playerId = Guid.NewGuid().ToString();
            creationDate = DateTime.Now;
            playerName = "Commander";
            playerLevel = 1;
            experience = 0;
            
            credits = 1000;
            energy = 500;
            nanites = 100;
            data = 50;
            influence = 0;
            
            factionId = "nexus";
            alignmentScore = 0;
            
            researchedTechs = new List<string>();
            unlockedUnits = new List<string>();
            unlockedBuildings = new List<string>();
            completedMissions = new List<string>();
            
            wins = 0;
            losses = 0;
            draws = 0;
            rating = 1000;
            
            gameSettings = new GameSettings();
            missionProgress = new Dictionary<string, MissionProgress>();
            
            currentCampaignMission = 1;
        }
        
        public void InitializeNewPlayer()
        {
            // Starting techs
            researchedTechs.Add("basic_construction");
            researchedTechs.Add("basic_combat");
            
            // Starting units
            unlockedUnits.Add("assault_trooper");
            unlockedUnits.Add("scout");
            
            // Starting buildings
            unlockedBuildings.Add("command_center");
            unlockedBuildings.Add("generator");
            unlockedBuildings.Add("barracks");
        }
        
        public bool HasResearched(string techId)
        {
            return researchedTechs.Contains(techId);
        }
        
        public bool HasUnlockedUnit(string unitId)
        {
            return unlockedUnits.Contains(unitId);
        }
        
        public bool HasUnlockedBuilding(string buildingId)
        {
            return unlockedBuildings.Contains(buildingId);
        }
        
        public void AddExperience(int amount)
        {
            experience += amount;
            
            // Level up every 1000 XP
            int newLevel = (experience / 1000) + 1;
            if (newLevel > playerLevel)
            {
                playerLevel = newLevel;
                Debug.Log($"Level up! Now level {playerLevel}");
            }
        }
        
        public void CompleteMission(string missionId, int score, float time)
        {
            if (!completedMissions.Contains(missionId))
            {
                completedMissions.Add(missionId);
            }
            
            if (!missionProgress.ContainsKey(missionId))
            {
                missionProgress[missionId] = new MissionProgress();
            }
            
            missionProgress[missionId].bestScore = Mathf.Max(missionProgress[missionId].bestScore, score);
            missionProgress[missionId].bestTime = Mathf.Min(missionProgress[missionId].bestTime, time);
            missionProgress[missionId].completionCount++;
        }
    }
    
    [System.Serializable]
    public class GameSettings
    {
        public float musicVolume = 0.8f;
        public float sfxVolume = 0.8f;
        public float voiceVolume = 1.0f;
        public bool subtitles = true;
        public int graphicsQuality = 2;
        public bool showTutorial = true;
        public string language = "English";
        public bool screenShake = true;
        public bool damageNumbers = true;
    }
    
    [System.Serializable]
    public class MissionProgress
    {
        public int bestScore;
        public float bestTime;
        public int completionCount;
        public bool hasBonusObjectives;
        
        public MissionProgress()
        {
            bestScore = 0;
            bestTime = float.MaxValue;
            completionCount = 0;
            hasBonusObjectives = false;
        }
    }
}