namespace Amberstar.Game;

[Flags]
public enum GameOptions : uint
{
    /// <summary>
    /// The original had only one save slot, but we can support more.
    /// </summary>
    MultipleSaveSlots = (1u << 0),

    /// <summary>
    /// In the original when dragging items, they are masked (only white color, as it is a cursor).
    /// We support dragging the item directly without masking it.
    /// </summary>
    UnmaskedDraggedItem = (1u << 1),

    /// <summary>
    /// Dungeon maps in style of Ambermoon.
    /// </summary>
    AmbermoonStyleDungeonMap = (1u << 2),

    /// <summary>
    /// In original you have to click a button to activate item pickup.
    /// For example in chests or inventory.
    /// 
    /// If this option is active, the button is replace:
    /// - In inventory by a new "Assembly all food" button
    /// - In chest by a new "Distribute all items" button
    /// 
    /// Items can then just be dragged & dropped directly while
    /// no other mode like "Examine item" is active.
    /// </summary>
    AdvancedItemPickup = (1u << 3),

    /// <summary>
    /// Free 3D movement like in Ambermoon.
    /// </summary>
    Free3DMovement = (1u << 4),

    /// <summary>
    /// If active each screen transition which usually uses fading,
    /// will instead show the original loading screen with the dwarf.
    /// </summary>
    ShowDwarfLoadingScreen = (1u << 5),

    /// <summary>
    /// Normally you have to put your gold on the table in places.
    /// If this is set, the gold of the whole party is directly
    /// available and the button is removed. You can't forget the
    /// gold this way as well.
    /// </summary>
    NoPlaceGoldGathering = (1u << 6),

    /// <summary>
    /// The light time (e.g. torches) is quite short in original
    /// and this is a problem in early game. This will double the
    /// duration of light spells.
    /// </summary>
    IncreasedLightDuration = (1u << 7),

    /// <summary>
    /// TODO: Most likely tricky. Especially as there are piles instead
    /// of chests which should be removed and you have no flag for it.
    /// Maybe you can determine it by tile change events. But maybe
    /// just leave this for some extension/mod.
    /// 
    /// In the original you can only pick up items from chests, but
    /// not place items there. With this option you can. It breaks
    /// compatibility with original savegames.
    /// </summary>
    AllowChestItemStorage = (1u << 31),

    None = 0,
    All = uint.MaxValue,
    Default = All & ~Free3DMovement & ~ShowDwarfLoadingScreen & ~AllowChestItemStorage & ~IncreasedLightDuration,
    Original = ShowDwarfLoadingScreen,
}

partial class Game
{
    // TODO: Add more and more of them.
    static readonly GameOptions supportedGameOptions = GameOptions.UnmaskedDraggedItem;

    internal IConfiguration Configuration { get; }

    public bool IsOptionSet(GameOptions option) => supportedGameOptions.HasFlag(option) && Configuration.GameOptions.HasFlag(option);
}
