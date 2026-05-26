using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;

namespace Amberstar.Game.Screens;

internal abstract class ItemPickerScreen : Screen
{
    ItemGridScreen? parentScreen;
    List<ItemContainer> items = [];
    int? pickedItem = null;
    Label? itemNameTooltip;

    public override bool Transparent => true;

    public abstract Rect MouseTrapArea { get; }

    public abstract Message Message { get; }

    public abstract Rect ItemTooltipArea { get; }

    public virtual CursorType HoverCursorType { get; } = CursorType.Sword;

    public virtual bool HideItemsAfterPicking { get; } = true;

    public override void Init()
	{
        base.Init();

        var (x, y, width, height) = ItemTooltipArea;
        itemNameTooltip = AddLabel(x, y, width, height, displayLayer: 100);
        itemNameTooltip.Alignment = TextAlignment.Center;

        // Note: During Init the ActiveScreen is still the last one.
        parentScreen = Game.ScreenHandler.ActiveScreen as ItemGridScreen;

        items = [.. parentScreen?.ItemContainers ?? []];
    }

    public override void Open(Action? closeAction)
    {
        base.Open(closeAction);

        parentScreen?.ShowMessage(Message, false, false);
        items.ForEach(item => item.Visible = true);

        Game.TrapMouse(MouseTrapArea);
        Game.Cursor.CursorType = HoverCursorType;
    }

    public override void Close()
    {
        parentScreen?.HideMessage();
        Game.UntrapMouse();
        Game.Cursor.CursorType = CursorType.Sword;

        if (HideItemsAfterPicking)
            items.ForEach(item => item.Visible = false);

        itemNameTooltip!.Destroy();

        base.Close();

        parentScreen?.PickItem(Type, pickedItem);
    }

    public override bool KeyDown(Key key, KeyModifiers keyModifiers)
    {
        if (key == Key.Escape || key == Key.Space)
        {
            pickedItem = null;
            Game?.ScreenHandler.PopScreen();
            return true;
        }

        return base.KeyDown(key, keyModifiers);
    }

	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
        if (buttons == MouseButtons.Left)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];

                if (!item.Empty && item.Contains(position))
                {
                    pickedItem = i;
                    Game.ScreenHandler.PopScreen();
                    return true;
                }
            }
        }
        else if (buttons == MouseButtons.Right)
        {
            pickedItem = null;
            Game?.ScreenHandler.PopScreen();
            return true;
        }

        return base.MouseDown(position, buttons, keyModifiers);
    }

    public override void MouseMove(Position position, MouseButtons buttons)
    {
        base.MouseMove(position, buttons);

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];

            if (!item.Empty && item.Contains(position))
            {
                itemNameTooltip!.SetText(item.Item!.GetName(Game.AssetProvider.TextLoader), 320 - itemNameTooltip!.Position.X, 15, 0, parentScreen!.ButtonGridPaletteIndex);
                itemNameTooltip.Visible = true;
                return;
            }
        }

        itemNameTooltip!.Visible = false;
    }
}
