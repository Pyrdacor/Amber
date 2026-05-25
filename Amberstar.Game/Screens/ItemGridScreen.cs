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
}