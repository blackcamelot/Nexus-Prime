using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NexusPrime.Campaign
{
    public class CampaignManager : MonoBehaviour
    {
        public static CampaignManager Instance;

        [Header("Missions")]
        public List<Mission> missions = new List<Mission>();

        private Dictionary<string, Mission> missionLookup;
        private int currentMissionIndex;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                BuildLookup();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void BuildLookup()
        {
            missionLookup = new Dictionary<string, Mission>();
            for (int i = 0; i < missions.Count; i++)
            {
                if (missions[i] != null && !string.IsNullOrEmpty(missions[i].missionId))
                    missionLookup[missions[i].missionId] = missions[i];
            }
        }

        public Mission GetMission(string missionId)
        {
            if (missionLookup == null) BuildLookup();
            return missionLookup != null && missionLookup.TryGetValue(missionId, out var m) ? m : null;
        }

        public Mission GetCurrentMission()
        {
            if (currentMissionIndex >= 0 && currentMissionIndex < missions.Count)
                return missions[currentMissionIndex];
            return null;
        }

        public void StartMission(string missionId)
        {
            var mission = GetMission(missionId);
            if (mission == null || string.IsNullOrEmpty(mission.sceneName)) return;
            currentMissionIndex = missions.IndexOf(mission);
            SceneManager.LoadScene(mission.sceneName);
        }

        public void CompleteCurrentMission()
        {
            var mission = GetCurrentMission();
            if (mission == null) return;
            if (Core.GameManager.Instance != null && Core.GameManager.Instance.playerData != null)
                Core.GameManager.Instance.playerData.CompleteMission(mission.missionId, 1000, Time.time);
            if (!string.IsNullOrEmpty(mission.nextMissionId))
                StartMission(mission.nextMissionId);
        }
    }
}
