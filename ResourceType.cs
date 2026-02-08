namespace NexusPrime.Economy
{
    public enum ResourceType
    {
        Credits,
        Energy,
        Nanites,
        Data,
        Influence,
        Population,
        Special,
        Exotic
    }
    
    public static class ResourceTypeExtensions
    {
        public static string GetDisplayName(this ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Credits: return "Credits";
                case ResourceType.Energy: return "Energy";
                case ResourceType.Nanites: return "Nanites";
                case ResourceType.Data: return "Data";
                case ResourceType.Influence: return "Influence";
                case ResourceType.Population: return "Population";
                case ResourceType.Special: return "Special Materials";
                case ResourceType.Exotic: return "Exotic Particles";
                default: return type.ToString();
            }
        }
        
        public static string GetDescription(this ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Credits: return "Universal currency for trade and construction";
                case ResourceType.Energy: return "Power required for all buildings and advanced units";
                case ResourceType.Nanites: return "Microscopic machines used for construction and repair";
                case ResourceType.Data: return "Information and research points for technology";
                case ResourceType.Influence: return "Political power for diplomacy and alliances";
                case ResourceType.Population: return "Workforce required for manual operations";
                case ResourceType.Special: return "Rare materials for advanced technologies";
                case ResourceType.Exotic: return "Unstable particles for experimental weapons";
                default: return "Unknown resource type";
            }
        }
        
        public static string GetAbbreviation(this ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Credits: return "CR";
                case ResourceType.Energy: return "EN";
                case ResourceType.Nanites: return "NT";
                case ResourceType.Data: return "DT";
                case ResourceType.Influence: return "IF";
                case ResourceType.Population: return "POP";
                case ResourceType.Special: return "SP";
                case ResourceType.Exotic: return "EX";
                default: return "??";
            }
        }
    }
}