namespace Amberstar.GameData;

[Flags]
public enum ItemSlotFlags : byte
{
    /// <summary>
    /// If set, allows more information through
    /// the item detail view (eye button).
    /// </summary>
    Identified = 0x80,
}

[Flags]
public enum ItemFlags : byte
{
	None = 0,
    /// <summary>
    /// If set, the item is cursed and will apply the
    /// negative bonus as malus when equipped.
    /// </summary>
	Cursed = 0x01,
    /// <summary>
    /// If set, the item can be thrown away.
    /// </summary>
	NotImportant = 0x02,
    /// <summary>
    /// If set, up to 99 items of the same kind can
    /// be stacked in one inventory slot.
    /// </summary>
	Stackable = 0x04,
    /// <summary>
    /// Destroys the item after using it. Like reading,
    /// opening a lock, drinking, etc.
    /// </summary>
	DestroyAfterUsage = 0x08,
    /// <summary>
    /// If set, the equipment can be equipped and unequipped
    /// during a fight (weapons, shields, rings, ...). Armor
    /// can't be changed in a fight.
    /// Note: The released source code checks the opposite but
    /// the program and the item data use it like this.
    /// </summary>
    EquipDuringFight = 0x10,
}
