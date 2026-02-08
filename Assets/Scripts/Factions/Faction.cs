using System.Collections.Generic;
using UnityEngine;

namespace NexusPrime.Factions
{
    [System.Serializable]
    public class Faction
    {
        public string factionId;
        public string displayName;
        public Color factionColor;
        public bool isPlayerControlled;
        public bool isActive = true;
        public List<string> alliedFactionIds = new List<string>();
        public List<string> enemyFactionIds = new List<string>();

        public Faction(string id, string name, Color color, bool playerControlled = false)
        {
            factionId = id;
            displayName = name;
            factionColor = color;
            isPlayerControlled = playerControlled;
        }

        public bool IsAlly(string otherFactionId)
        {
            return factionId == otherFactionId || alliedFactionIds.Contains(otherFactionId);
        }

        public bool IsEnemy(string otherFactionId)
        {
            return enemyFactionIds.Contains(otherFactionId);
        }
    }
}
