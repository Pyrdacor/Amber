using Amberstar.GameData;

namespace Amberstar.Game;

/// <summary>
/// Slot specific item data used to create items (e.g. given or identified items).
/// </summary>
internal sealed record ItemSlotData(byte SpellCharges, ItemSlotFlags SlotFlags) : IItemSlot;
