using System.Collections.Generic;
using UnityEngine;

namespace NexusPrime.Factions
{
    public class FactionManager : MonoBehaviour
    {
        public static FactionManager Instance;

        [Header("Factions")]
        public List<Faction> factions = new List<Faction>();

        private Dictionary<string, Faction> factionLookup;

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
            factionLookup = new Dictionary<string, Faction>();
            foreach (var f in factions)
            {
                if (f != null && !string.IsNullOrEmpty(f.factionId))
                    factionLookup[f.factionId] = f;
            }
        }

        public Faction GetFaction(string factionId)
        {
            if (factionLookup == null) BuildLookup();
            return factionLookup != null && factionLookup.TryGetValue(factionId, out var f) ? f : null;
        }

        public void RegisterFaction(Faction faction)
        {
            if (faction == null || string.IsNullOrEmpty(faction.factionId)) return;
            if (factionLookup == null) factionLookup = new Dictionary<string, Faction>();
            factionLookup[faction.factionId] = faction;
            if (!factions.Contains(faction)) factions.Add(faction);
        }

        public bool AreAllies(string factionA, string factionB)
        {
            var fa = GetFaction(factionA);
            var fb = GetFaction(factionB);
            return fa != null && fb != null && fa.IsAlly(factionB);
        }

        public bool AreEnemies(string factionA, string factionB)
        {
            var fa = GetFaction(factionA);
            return fa != null && fa.IsEnemy(factionB);
        }
    }
}
