using System.Collections.Generic;
using UnityEngine;

namespace NexusPrime.Factions
{
    [CreateAssetMenu(fileName = "FactionData", menuName = "Nexus Prime/Faction Data")]
    public class FactionData : ScriptableObject
    {
        public string factionId;
        public string displayName;
        public Color factionColor;
        [TextArea(2, 4)]
        public string description;
        public Sprite factionIcon;
        public string[] startingUnitIds;
        public string[] startingBuildingIds;
        public string[] bonusTechIds;
    }
}
