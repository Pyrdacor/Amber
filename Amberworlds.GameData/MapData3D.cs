namespace Amberworlds.GameData;

[Flags]
public enum MapInteractionType3D : byte
{
    EnterMap        = 0, // This is not combineable
    EnterTile       = 1 << 0,
    Interact        = 1 << 1, // Touch, Look, Speak, etc
    Item            = 1 << 2, // Use item
    Spell           = 1 << 3, // Use spell
    ObjectTouch     = 1 << 4, // Other map object touches it
    ObjectOnTile    = 1 << 5, // Other map object fully on same tile
    FallOntoTile    = 1 << 6, // Only if fallen from other map or higher layer onto it
    Levitating      = 1 << 7, // Only if climb or levitated to the tile (tile can be blocking)
}

public struct MapInteraction3D
{
    public MapInteractionType3D Type;
    public byte LayerIndex;
    public byte Parameter; // item index, spell index, map object index
    public byte ActionIndex;
}

public enum MapActionType3D : byte
{
    Teleport,
    Text,
    Trap,
    Reward,
    ChangeTile,
    DestroyTile,
    DestroyTileFloor,
    Levitate, // Automatically let's the player levitate upwards
    ChangeBuffs,
    ActivateInteraction, // or deactivate
    ActivateCharacter, // or deactivate
    SetMapVariable, // mapIndex + index
    SetNPCVariable, // npcIndex + index
    StartBattle,
    ChangeMusic,
    SpawnTransport,
    AddPartyMember,
    RemovePartyMember,
    KillPartyMember,
    Delay,
    Shake,

}

public struct MapAction3D
{
    public MapActionType3D Type;
    public byte ConditionIndex;
    public byte NextActionIndex; // 255 = none
    public byte FailActionIndex; // 255 = none
}

public enum MapConditionType3D
{
    Variable, // Always of the current map
    InteractionActive,
    CharacterActive,
    PartyMemberInParty,
    ItemOwned,
    SpellKnown,
    WordKnown,
    Condition, // Ailment
    Time,
    Random,
    Decision, // Yes/No popup -> Yes
    CorrectWord, // Word input popup
    CorrectNumber, // Number input popup
    Level, // min, max, avg, all, any, specific party member
    Attribute, // same...
    Skill,
}

public struct MapSwitch3D
{
    public byte OffTileIndex; // Mandatory
    public byte OnTileIndex; // Mandatory
    public byte OffActionIndex; // If 255, the switch can't be re-triggered
    public byte OnActionIndex; // Mandatory
}

[Flags]
public enum MapCharaterFlags3D : byte
{
    None                = 0,
    Monster             = 1 << 0, // Otherwise person
    TextPopup           = 1 << 1, // If interacted, just display a text
    RandomMovement      = 1 << 2, // Else persons follow a path and monsters only chase the player if they see him
}

public struct MapCharacter3D
{
    public byte Index; // Person, Monster or Text index
    public MapCharaterFlags3D Flags;
    public byte CharacterIndex;

}

public unsafe struct MapData3D
{
    public byte Width;
    public byte Height;
    public byte LayerCount;
    public byte InteractionCount;
    public byte DoorCount;
    public byte ChestCount;
    public byte RiddlemouthCount;
    public byte PlaceCount;
    public byte SwitchCount;
    public byte CharacterCount;
    // Width*Height bytes per layer.
    // Each byte is the index into the tileset.
    public byte* Layers;
    // One entry per InteractionCount.
    public MapInteraction3D* Interactions;
    public byte* DoorIndices;
    public byte* ChestIndices;
    public byte* RiddlemouthIndices;
    public byte* PlaceIndices;
    public MapSwitch3D* Switches;
    public MapCharacter3D* Characters;
}
