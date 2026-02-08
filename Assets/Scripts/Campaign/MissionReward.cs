using System.Collections.Generic;
using UnityEngine;

namespace NexusPrime.Campaign
{
    [System.Serializable]
    public class MissionReward
    {
        public int experience;
        public int credits;
        public string[] unlockTechIds;
        public string[] unlockUnitIds;
        public string[] unlockBuildingIds;
    }
}
