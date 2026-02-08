namespace NexusPrime.Core
{
    public enum GameState
    {
        MainMenu,
        Loading,
        Playing,
        Paused,
        GameOver,
        Campaign,
        Multiplayer,
        Sandbox,
        Building,
        Researching,
        InCombat
    }
    
    public enum GameMode
    {
        Campaign,
        Skirmish,
        Multiplayer,
        Sandbox,
        Tutorial
    }
    
    public enum Difficulty
    {
        Easy,
        Normal,
        Hard,
        Insane
    }
}