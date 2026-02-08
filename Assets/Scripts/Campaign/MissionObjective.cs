using System;
using UnityEngine;

namespace NexusPrime.Campaign
{
    [Serializable]
    public class MissionObjective
    {
        public string objectiveId;
        public string description;
        public bool isOptional;
        public bool isCompleted;
        public string progressHint;
    }
}
