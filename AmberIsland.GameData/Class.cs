namespace AmberIsland.GameData;

public enum Class
{
    Adventurer,
    // tbd
}

[Flags]
public enum Classes
{
    None = 0,
    Adventurer = 1 << 0,
}
