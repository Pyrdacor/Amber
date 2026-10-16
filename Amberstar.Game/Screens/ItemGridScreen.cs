using Amberstar.Game.UI;
using Amberstar.GameData;

namespace Amberstar.Game.Screens;

internal abstract class ItemGridScreen : ButtonGridScreen
{
	internal abstract void ShowMessage(Message messageIndex, bool waitForClick = true, bool closeAfterClick = false);

	internal abstract void HideMessage();

	internal abstract void PickItem(ScreenType sourceScreen, int? index);

    public override void Update(long elapsedTicks)
    {
        base.Update(elapsedTicks);

        foreach (var item in ItemContainers)
            item.Update();
    }

	internal abstract ItemContainer[] ItemContainers { get; }

	/// <summary>
	/// Item containers of the backpack (some actions are not possible with equipped items).
	/// </summary>
	internal virtual ItemContainer[] BackpackItemContainers => ItemContainers;

	/// <summary>
	/// Item containers of the equipment (e.g. to remove curses).
	/// </summary>
	internal virtual ItemContainer[] EquipmentItemContainers => ItemContainers;
}