using System.Collections.Generic;
using UnityEngine;

namespace NexusPrime.AI
{
    public class AIStrategicPlanner : MonoBehaviour
    {
        [Header("References")]
        public AIFactionController factionController;

        public Vector3 GetNextExpansionTarget()
        {
            if (factionController != null)
                return factionController.homeBasePosition + Random.insideUnitSphere * factionController.baseRadius * 2f;
            return Vector3.zero;
        }

        public string GetPriorityObjective()
        {
            return "expand";
        }
    }
}
