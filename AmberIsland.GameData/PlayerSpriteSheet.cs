using Amber.Common;
using Amber.IO.Common.Serialization;

namespace AmberIsland.GameData;

public enum PlayerStateSpriteVariant
{
    // Player
    Player_Normal = 0,
    // Outfit
    Outfit_Boxers = 32, // Male
    Outfit_Underwear, // Female
    Outfit_Armor,
    Outfit_Robe,
    // Cloaks
    // Not used yet, starts at 64
    // Face items
    // Not used yet, starts at 96
    // Hair
    Hair_Bob = 128,
    Hair_Dapper,
    // Hat
    Hat_Cap = 160,
    Hat_Wizard,
    // Primary tool
    PTool_Axe = 192,
    PTool_Mace,
    PTool_Sword,
    // Secondary tool
    STool_WoodenShield = 224,
    STool_IronShield,
    STool_PaladinShield,
}

public readonly record struct PlayerStateSprites
(
    // Byte-sized
    PlayerState State,
    // Word-sized
    ushort OffsetX, // For the whole atlas part (in frames!)
    ushort OffsetY,
    // Collections
    byte[] FrameIndices, // Relative to OffsetX/Y
    Dictionary<PlayerStateSpriteVariant, byte[]> PossiblePaletteIndices // Inside the atlas sprite palettes
)
{
    public static readonly Dictionary<string, PlayerStateSpriteVariant> VariantFileIdentifiers = new()
    {
        ["humn"] = PlayerStateSpriteVariant.Player_Normal,
        // Outfit
        ["boxr"] = PlayerStateSpriteVariant.Outfit_Boxers,
        ["undi"] = PlayerStateSpriteVariant.Outfit_Underwear,
        ["fstr"] = PlayerStateSpriteVariant.Outfit_Armor,
        ["pfpn"] = PlayerStateSpriteVariant.Outfit_Robe,
        // Hair
        ["bob1"] = PlayerStateSpriteVariant.Hair_Bob,
        ["dap1"] = PlayerStateSpriteVariant.Hair_Dapper,
        // Hat
        ["pfht"] = PlayerStateSpriteVariant.Hat_Cap,
        ["pnty"] = PlayerStateSpriteVariant.Hat_Wizard,
        // Primary tool
        ["ax01"] = PlayerStateSpriteVariant.PTool_Axe,
        ["mc01"] = PlayerStateSpriteVariant.PTool_Mace,
        ["sw01"] = PlayerStateSpriteVariant.PTool_Sword,
        // Secondary tool
        ["sh01"] = PlayerStateSpriteVariant.STool_WoodenShield,
        ["sh02"] = PlayerStateSpriteVariant.STool_IronShield,
        ["sh03"] = PlayerStateSpriteVariant.STool_PaladinShield,
    };

    // Note: FrameSize is fixed for all of them (64x64)
    public static readonly Size FrameSize = new(64, 64);

    public void Write(IDataWriter writer)
    {
        // Byte-sized
        writer.Write((byte)State);

        // Word-sized
        writer.Write(OffsetX);
        writer.Write(OffsetY);

        // Collections
        writer.Write((byte)FrameIndices.Length);
        writer.Write(FrameIndices);
        writer.Write((byte)PossiblePaletteIndices.Count);

        foreach (var kvp in PossiblePaletteIndices.OrderBy(kvp => kvp.Key))
        {
            writer.Write((byte)kvp.Key);
            writer.Write((byte)kvp.Value.Length);
            writer.Write(kvp.Value);
        }
    }

    public static PlayerStateSprites Read(IDataReader reader)
    {
        // Byte-sized
        var state = (PlayerState)reader.ReadByte();

        // Word-sized
        var offsetX = reader.ReadWord();
        var offsetY = reader.ReadWord();

        // Collections
        var frameCount = reader.ReadByte();
        var frameIndices = reader.ReadBytes(frameCount);
        var possiblePaletteCount = reader.ReadByte();
        var possiblePaletteIndices = new Dictionary<PlayerStateSpriteVariant, byte[]>(possiblePaletteCount);

        for (int i = 0; i < possiblePaletteCount; i++)
        {
            var variant = (PlayerStateSpriteVariant)reader.ReadByte();
            var paletteIndices = reader.ReadBytes(reader.ReadByte());
            possiblePaletteIndices.Add(variant, paletteIndices);
        }

        return new PlayerStateSprites
        (
            // Byte-sized
            state,
            // Word-sized
            offsetX,
            offsetY,
            // Collections
            frameIndices,
            possiblePaletteIndices
        );
    }
}

/// <summary>
/// Player spritesheets contain all merged
/// player animations with many frames and
/// in general all 4 directions. They can
/// often be used with multiple palettes to
/// allow different skin or equipment colors.
/// </summary>
public readonly record struct PlayerSpriteSheet
(
    PlayerStateSprites[] StateSprites,
    SpriteWithPalettes Atlas
)
{
    public void Write(IDataWriter writer)
    {
        writer.Write((byte)StateSprites.Length);

        for (int i = 0; i < StateSprites.Length; i++)
            StateSprites[i].Write(writer);

        Atlas.Write(writer);
    }

    public static PlayerSpriteSheet Read(IDataReader reader)
    {
        var stateSpriteCount = reader.ReadByte();
        var stateSprites = new PlayerStateSprites[stateSpriteCount];

        for (int i = 0; i < stateSpriteCount; i++)
            stateSprites[i] = PlayerStateSprites.Read(reader);

        var atlas = SpriteWithPalettes.Read(reader);

        return new PlayerSpriteSheet
        (
            stateSprites,
            atlas
        );
    }
}