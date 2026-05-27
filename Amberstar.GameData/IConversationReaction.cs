using System.Collections.ObjectModel;

namespace Amberstar.GameData;

public enum InteractionTriggerType : byte
{
    Say = 1, // keyword
    Show, // item
    Give, // item
    Pay, // gold
    Feed, // food
    Join, // party
}

public record InteractionTrigger(InteractionTriggerType Type, word Argument = 0);

public interface IConversationInteraction
{
    ReadOnlyCollection<IConversationReaction> Reactions { get; }
}

public interface IConversationReaction
{

}

/// <summary>
/// The person responds with a text message.
/// </summary>
public interface ISayReaction : IConversationReaction
{
    int MessageIndex { get; init; }
}

/// <summary>
/// The person teaches you a new word which can be used in future
/// conversations.
/// </summary>
public interface ITeachWordReaction : IConversationReaction
{
    int WordIndex { get; init; }
}

/// <summary>
/// The person gives you an item.
/// 
/// The source item must be in the person's inventory or equipped by it.
/// A copy of the item is used so the source item remains.
/// </summary>
public interface IGiveItemReaction : IConversationReaction
{
    /// <summary>
    /// This is the slot index of the item in the character's item data.
    /// So index 0 is the first equipped item and index 9 is the first
    /// inventory item slot.
    /// </summary>
    int ItemSlotIndex { get; init; }
}

/// <summary>
/// The person gives you some gold.
/// </summary>
public interface IGiveGoldReaction : IConversationReaction
{
    word Amount { get; init; }
}

/// <summary>
/// The person gives you some food.
/// </summary>
public interface IGiveFoodReaction : IConversationReaction
{
    word Amount { get; init; }
}

/// <summary>
/// The person completes the given quest.
/// 
/// A quest index is usually stored inside persons and unlocks a second
/// pair of interactions for the person.
/// </summary>
public interface ICompleteQuestReaction : IConversationReaction
{
    byte QuestIndex { get; init; }
}

public enum ChangeStatAction
{
    // Note: Based on offset, the affected size is either byte, word or long.
    // Offset < 70: Byte
    // Offset >= 204: Long
    // In-between: Word
    Increase = 1,
    Decrease,
    // Note: The bit operations only affect a single byte, so bit index should be only 0 to 7!
    ClearBit,
    SetBit,
    ToggleBit
}

/// <summary>
/// The person changes some stat of the player.
/// 
/// A quest index is usually stored inside persons and unlocks a second
/// pair of interactions for the person.
/// </summary>
public interface IChangeStatReaction : IConversationReaction
{
    /// <summary>
    /// Offset into the character data.
    /// </summary>
    byte StatOffset { get; init; }

    ChangeStatAction Action { get; init; }

    /// <summary>
    /// Amount, bit index, etc.
    /// </summary>
    word Value { get; init; }
}