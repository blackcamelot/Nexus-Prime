using System.Collections.Generic;
using UnityEngine;
using NexusPrime.Units;

namespace NexusPrime.AI
{
    public class AITacticalController : MonoBehaviour
    {
        [Header("References")]
        public AIFactionController factionController;

        public float EstimateEnemyStrength()
        {
            if (factionController == null) return 0f;
            float strength = 0f;
            foreach (var unit in factionController.enemyIntel)
            {
                strength += unit.estimatedStrength;
            }
            return strength + factionController.knownEnemyPositions.Count * 10f;
        }

        public float EstimateOurStrength()
        {
            if (factionController == null) return 0f;
            float strength = 0f;
            foreach (var u in factionController.ownedUnits)
            {
                if (u == null) continue;
                var stats = u.GetComponent<UnitStats>();
                if (stats != null) strength += stats.maxHealth * 0.01f;
            }
            return strength;
        }
    }
}
