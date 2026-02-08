using UnityEngine;
using System.Collections.Generic;
using System;

namespace NexusPrime.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;
        
        [Header("Game State")]
        public GameState currentState;
        public float gameSpeed = 1.0f;
        public bool isPaused = false;
        
        [Header("Player Data")]
        public PlayerData playerData;
        public string playerFaction;
        
        [Header("Managers")]
        public ResourceManager resourceManager;
        public TechTreeSystem techTreeSystem;
        public BuildingSystem buildingSystem;
        public FactionManager factionManager;
        public CampaignManager campaignManager;
        
        [Header("Game Settings")]
        public float dayNightCycleSpeed = 60f;
        public int maxUnits = 200;
        public int maxBuildings = 50;
        
        private List<SelectableUnit> allUnits = new List<SelectableUnit>();
        private List<Building> allBuildings = new List<Building>();
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGame();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializeGame()
        {
            Debug.Log("Initializing Nexus Prime Game Manager");
            
            // Initialize systems
            resourceManager = FindObjectOfType<ResourceManager>();
            if (resourceManager == null)
            {
                GameObject rm = new GameObject("ResourceManager");
                resourceManager = rm.AddComponent<ResourceManager>();
                rm.transform.SetParent(transform);
            }
            
            techTreeSystem = FindObjectOfType<TechTreeSystem>();
            if (techTreeSystem == null)
            {
                GameObject tts = new GameObject("TechTreeSystem");
                techTreeSystem = tts.AddComponent<TechTreeSystem>();
                tts.transform.SetParent(transform);
            }
            
            buildingSystem = FindObjectOfType<BuildingSystem>();
            if (buildingSystem == null)
            {
                GameObject bs = new GameObject("BuildingSystem");
                buildingSystem = bs.AddComponent<BuildingSystem>();
                bs.transform.SetParent(transform);
            }
            
            factionManager = FindObjectOfType<FactionManager>();
            if (factionManager == null)
            {
                GameObject fm = new GameObject("FactionManager");
                factionManager = fm.AddComponent<FactionManager>();
                fm.transform.SetParent(transform);
            }
            
            campaignManager = FindObjectOfType<CampaignManager>();
            if (campaignManager == null)
            {
                GameObject cm = new GameObject("CampaignManager");
                campaignManager = cm.AddComponent<CampaignManager>();
                cm.transform.SetParent(transform);
            }
            
            // Load or create player data
            playerData = SaveSystem.LoadPlayerData();
            if (playerData == null)
            {
                playerData = new PlayerData();
                playerData.InitializeNewPlayer();
                SaveSystem.SavePlayerData(playerData);
            }
            
            // Set initial state
            currentState = GameState.MainMenu;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            
            Debug.Log("Game Manager Initialized Successfully");
        }
        
        void Update()
        {
            if (isPaused) return;
            
            // Update game time
            Time.timeScale = gameSpeed;
            
            // Check win/lose conditions
            CheckGameConditions();
        }
        
        public void RegisterUnit(SelectableUnit unit)
        {
            if (!allUnits.Contains(unit))
            {
                allUnits.Add(unit);
            }
        }
        
        public void UnregisterUnit(SelectableUnit unit)
        {
            if (allUnits.Contains(unit))
            {
                allUnits.Remove(unit);
            }
        }
        
        public void RegisterBuilding(Building building)
        {
            if (!allBuildings.Contains(building))
            {
                allBuildings.Add(building);
            }
        }
        
        public void UnregisterBuilding(Building building)
        {
            if (allBuildings.Contains(building))
            {
                allBuildings.Remove(building);
            }
        }
        
        public List<SelectableUnit> GetAllUnits()
        {
            return new List<SelectableUnit>(allUnits);
        }
        
        public List<Building> GetAllBuildings()
        {
            return new List<Building>(allBuildings);
        }
        
        public void SaveGame(string saveName = "autosave")
        {
            SaveSystem.SaveGame(saveName, this);
            Debug.Log($"Game saved: {saveName}");
        }
        
        public void LoadGame(string saveName = "autosave")
        {
            if (SaveSystem.LoadGame(saveName, this))
            {
                Debug.Log($"Game loaded: {saveName}");
            }
            else
            {
                Debug.LogError($"Failed to load game: {saveName}");
            }
        }
        
        public void PauseGame()
        {
            isPaused = true;
            Time.timeScale = 0f;
            currentState = GameState.Paused;
        }
        
        public void ResumeGame()
        {
            isPaused = false;
            Time.timeScale = gameSpeed;
            currentState = GameState.Playing;
        }
        
        public void QuitToMenu()
        {
            SaveGame();
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            currentState = GameState.MainMenu;
        }
        
        public void QuitGame()
        {
            SaveGame();
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
        
        private void CheckGameConditions()
        {
            // Check if player has no command center
            bool hasCommandCenter = false;
            foreach (Building building in allBuildings)
            {
                if (building.buildingType == "command_center" && building.IsAlive())
                {
                    hasCommandCenter = true;
                    break;
                }
            }
            
            if (!hasCommandCenter && allBuildings.Count > 0)
            {
                GameOver(false); // Player lost
            }
        }
        
        private void GameOver(bool victory)
        {
            currentState = GameState.GameOver;
            isPaused = true;
            Time.timeScale = 0f;
            
            // Show game over screen
            UIManager.Instance.ShowGameOverScreen(victory);
            
            Debug.Log($"Game Over: {(victory ? "Victory!" : "Defeat!")}");
        }
        
        public void Victory()
        {
            GameOver(true);
        }
        
        public void Defeat()
        {
            GameOver(false);
        }
    }
}