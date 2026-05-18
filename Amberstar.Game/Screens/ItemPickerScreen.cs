using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;

namespace Amberstar.Game.Screens;

internal abstract class ItemPickerScreen : Screen
{
	Game? game;
    ItemGridScreen? parentScreen;
    List<ItemContainer> items = [];
    bool itemsWereHidden = true;
    int? pickedItem = null;
    Label? itemNameTooltip;

    public override bool Transparent => true;

    public abstract Rect MouseTrapArea { get; }

    public abstract Message Message { get; }

    public abstract Rect ItemTooltipArea { get; }

    public virtual CursorType HoverCursorType { get; } = CursorType.Sword;

    public override void Init(Game game)
	{
        base.Init(game);

        this.game = game;
        itemNameTooltip = new(game)
        {
            DisplayLayer = 100,
            Alignment = TextAlignment.Center,
            Area = ItemTooltipArea,
        };

        // Note: During Init the ActiveScreen is still the last one.
        parentScreen = game.ScreenHandler.ActiveScreen as ItemGridScreen;

        items = [.. parentScreen?.ItemContainers ?? []];
    }

    public override void Open(Game game, Action? closeAction)
    {
        base.Open(game, closeAction);

        parentScreen?.ShowMessage(Message, false, false);
        itemsWereHidden = items.FirstOrDefault()?.Visible == false;
        items.ForEach(item => item.Visible = true);

        game.TrapMouse(MouseTrapArea);
        game.Cursor.CursorType = HoverCursorType;
    }

    public override void Close(Game game)
    {
        parentScreen?.HideMessage();
        game.UntrapMouse();
        game.Cursor.CursorType = CursorType.Sword;

        if (itemsWereHidden)
            items.ForEach(item => item.Visible = false);

        itemNameTooltip!.Destroy();

        base.Close(game);

        parentScreen?.PickItem(Type, pickedItem);
    }

    public override bool KeyDown(Key key, KeyModifiers keyModifiers)
    {
        if (key == Key.Escape || key == Key.Space)
        {
            pickedItem = null;
            game?.ScreenHandler.PopScreen();
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
                    game!.ScreenHandler.PopScreen();
                    return true;
                }
            }
        }
        else if (buttons == MouseButtons.Right)
        {
            pickedItem = null;
            game?.ScreenHandler.PopScreen();
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
                itemNameTooltip!.SetText(item.Item!.GetName(game!.AssetProvider.TextLoader), 320 - itemNameTooltip!.Position.X, 15, 0, parentScreen!.ButtonGridPaletteIndex);
                itemNameTooltip.Visible = true;
                return;
            }
        }

        itemNameTooltip!.Visible = false;
    }
}
