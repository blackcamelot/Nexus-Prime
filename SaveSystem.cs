using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using System.Security.Cryptography;

namespace NexusPrime.Core
{
    public static class SaveSystem
    {
        private static string saveDirectory = Application.persistentDataPath + "/Saves/";
        private static string playerDataFile = "playerdata.dat";
        private static string encryptionKey = "NexusPrimeSaveKey2023!";
        
        public static void Initialize()
        {
            if (!Directory.Exists(saveDirectory))
            {
                Directory.CreateDirectory(saveDirectory);
            }
            
            if (!Directory.Exists(saveDirectory + "Screenshots/"))
            {
                Directory.CreateDirectory(saveDirectory + "Screenshots/");
            }
            
            Debug.Log($"Save directory: {saveDirectory}");
        }
        
        public static void SavePlayerData(PlayerData playerData)
        {
            try
            {
                string json = JsonUtility.ToJson(playerData, true);
                byte[] encryptedData = Encrypt(json);
                
                string filePath = saveDirectory + playerDataFile;
                File.WriteAllBytes(filePath, encryptedData);
                
                Debug.Log("Player data saved successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save player data: {e.Message}");
            }
        }
        
        public static PlayerData LoadPlayerData()
        {
            string filePath = saveDirectory + playerDataFile;
            
            if (!File.Exists(filePath))
            {
                Debug.Log("No player data found, creating new");
                return null;
            }
            
            try
            {
                byte[] encryptedData = File.ReadAllBytes(filePath);
                string json = Decrypt(encryptedData);
                PlayerData playerData = JsonUtility.FromJson<PlayerData>(json);
                
                Debug.Log("Player data loaded successfully");
                return playerData;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load player data: {e.Message}");
                return null;
            }
        }
        
        public static void SaveGame(string saveName, GameManager gameManager)
        {
            try
            {
                GameSaveData saveData = new GameSaveData();
                
                // Collect game data
                saveData.saveName = saveName;
                saveData.saveTime = DateTime.Now;
                saveData.gameTime = Time.time;
                saveData.playerData = gameManager.playerData;
                
                // Save units
                saveData.units = new List<UnitSaveData>();
                foreach (var unit in gameManager.GetAllUnits())
                {
                    UnitSaveData unitData = new UnitSaveData();
                    unitData.unitId = unit.unitId;
                    unitData.position = unit.transform.position;
                    unitData.rotation = unit.transform.rotation;
                    unitData.health = unit.currentHealth;
                    unitData.experience = unit.experience;
                    unitData.owner = unit.ownerFaction;
                    
                    saveData.units.Add(unitData);
                }
                
                // Save buildings
                saveData.buildings = new List<BuildingSaveData>();
                foreach (var building in gameManager.GetAllBuildings())
                {
                    BuildingSaveData buildingData = new BuildingSaveData();
                    buildingData.buildingId = building.buildingId;
                    buildingData.position = building.transform.position;
                    buildingData.rotation = building.transform.rotation;
                    buildingData.health = building.currentHealth;
                    buildingData.owner = building.ownerFaction;
                    buildingData.productionProgress = building.productionProgress;
                    
                    saveData.buildings.Add(buildingData);
                }
                
                // Save resources
                saveData.resources = new Dictionary<string, int>();
                if (gameManager.resourceManager != null)
                {
                    // Get resources from resource manager
                }
                
                // Convert to JSON and encrypt
                string json = JsonUtility.ToJson(saveData, true);
                byte[] encryptedData = Encrypt(json);
                
                // Save to file
                string filePath = saveDirectory + saveName + ".save";
                File.WriteAllBytes(filePath, encryptedData);
                
                // Save thumbnail
                SaveThumbnail(saveName);
                
                Debug.Log($"Game saved: {saveName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save game: {e.Message}");
            }
        }
        
        public static bool LoadGame(string saveName, GameManager gameManager)
        {
            string filePath = saveDirectory + saveName + ".save";
            
            if (!File.Exists(filePath))
            {
                Debug.LogError($"Save file not found: {saveName}");
                return false;
            }
            
            try
            {
                byte[] encryptedData = File.ReadAllBytes(filePath);
                string json = Decrypt(encryptedData);
                GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);
                
                // Apply save data
                gameManager.playerData = saveData.playerData;
                
                // TODO: Recreate units and buildings from save data
                
                Debug.Log($"Game loaded: {saveName}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load game: {e.Message}");
                return false;
            }
        }
        
        public static List<string> GetSaveFiles()
        {
            List<string> saveFiles = new List<string>();
            
            if (!Directory.Exists(saveDirectory))
                return saveFiles;
            
            string[] files = Directory.GetFiles(saveDirectory, "*.save");
            foreach (string file in files)
            {
                saveFiles.Add(Path.GetFileNameWithoutExtension(file));
            }
            
            return saveFiles;
        }
        
        public static void DeleteSave(string saveName)
        {
            string filePath = saveDirectory + saveName + ".save";
            string thumbPath = saveDirectory + "Screenshots/" + saveName + ".png";
            
            try
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
                
                if (File.Exists(thumbPath))
                    File.Delete(thumbPath);
                
                Debug.Log($"Save deleted: {saveName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete save: {e.Message}");
            }
        }
        
        public static Texture2D LoadThumbnail(string saveName)
        {
            string thumbPath = saveDirectory + "Screenshots/" + saveName + ".png";
            
            if (!File.Exists(thumbPath))
                return null;
            
            try
            {
                byte[] imageData = File.ReadAllBytes(thumbPath);
                Texture2D thumbnail = new Texture2D(2, 2);
                thumbnail.LoadImage(imageData);
                return thumbnail;
            }
            catch
            {
                return null;
            }
        }
        
        private static void SaveThumbnail(string saveName)
        {
            // TODO: Capture screenshot and save as thumbnail
            // This requires proper camera setup and rendering
        }
        
        private static byte[] Encrypt(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(encryptionKey.PadRight(32).Substring(0, 32));
                aes.IV = new byte[16]; // Simple IV for demo
            
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }
                        return ms.ToArray();
                    }
                }
            }
        }
        
        private static string Decrypt(byte[] cipherText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(encryptionKey.PadRight(32).Substring(0, 32));
                aes.IV = new byte[16];
                
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                
                using (MemoryStream ms = new MemoryStream(cipherText))
                {
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader sr = new StreamReader(cs))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
        }
    }
    
    [System.Serializable]
    public class GameSaveData
    {
        public string saveName;
        public DateTime saveTime;
        public float gameTime;
        public PlayerData playerData;
        public List<UnitSaveData> units;
        public List<BuildingSaveData> buildings;
        public Dictionary<string, int> resources;
        public List<string> researchedTechs;
    }
    
    [System.Serializable]
    public class UnitSaveData
    {
        public string unitId;
        public Vector3 position;
        public Quaternion rotation;
        public float health;
        public int experience;
        public string owner;
    }
    
    [System.Serializable]
    public class BuildingSaveData
    {
        public string buildingId;
        public Vector3 position;
        public Quaternion rotation;
        public float health;
        public string owner;
        public float productionProgress;
    }
}