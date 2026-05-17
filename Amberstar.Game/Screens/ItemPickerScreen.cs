using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;

namespace Amberstar.Game.Screens;

internal abstract class ItemPickerScreen<TParentScreen> : Screen
    where TParentScreen : ItemGridScreen
{
	Game? game;
    TParentScreen? parentScreen;
    List<ItemContainer> items = [];
    bool itemsWereHidden = true;
    int? pickedItem = null;

    public override bool Transparent => true;

    public abstract Rect MouseTrapArea { get; }

    public abstract Message Message { get; }

    public override void Init(Game game)
	{
        base.Init(game);

        this.game = game;

        // Note: During Init the ActiveScreen is still the last one.
        parentScreen = game.ScreenHandler.ActiveScreen as TParentScreen;

        items = [.. parentScreen?.ItemContainers ?? []];
    }

    public override void Open(Game game, Action? closeAction)
    {
        base.Open(game, closeAction);

        parentScreen?.ShowMessage(Message, false, false);
        itemsWereHidden = items.FirstOrDefault()?.Visible == false;
        items.ForEach(item => item.Visible = true);

        game.TrapMouse(MouseTrapArea);
    }

    public override void Close(Game game)
    {
        parentScreen?.HideMessage();
        game.UntrapMouse();

        if (itemsWereHidden)
            items.ForEach(item => item.Visible = false);

        base.Close(game);

        parentScreen?.PickItem(pickedItem);
    }

    public override void KeyDown(Key key, KeyModifiers keyModifiers)
    {
        if (key == Key.Escape || key == Key.Space)
        {
            pickedItem = null;
            game?.ScreenHandler.PopScreen();
            return;
        }

        base.KeyDown(key, keyModifiers);
    }

	public override void MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
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
                    return;
                }
            }
        }
        else if (buttons == MouseButtons.Right)
        {
            pickedItem = null;
            game?.ScreenHandler.PopScreen();
            return;
        }

        base.MouseDown(position, buttons, keyModifiers);
    }
}
