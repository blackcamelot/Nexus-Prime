using System.Collections.Generic;
using UnityEngine;

namespace NexusPrime.Campaign
{
    [CreateAssetMenu(fileName = "Mission", menuName = "Nexus Prime/Mission")]
    public class Mission : ScriptableObject
    {
        public string missionId;
        public string missionName;
        [TextArea(2, 5)]
        public string briefing;
        public string sceneName;
        public int missionOrder;
        public List<MissionObjective> objectives = new List<MissionObjective>();
        public MissionReward rewards;
        public string nextMissionId;
    }
}
