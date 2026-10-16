using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;

namespace Amberstar.Game.Screens;

/// <summary>
/// Shows a text (e.g. "Who should be healed?") and lets the player click the portrait
/// of a party member (VictimSel_Mod in the original). Any other click cancels it.
/// The chosen party member slot is stored in <see cref="Game.SelectedPartyMemberSlot"/>.
/// </summary>
internal sealed class SelectPartyMemberScreen() : WindowScreen(WindowX, WindowY, WindowWidthInTiles, WindowHeightInTiles, WindowDisplayLayer)
{
	const int WindowX = 16;
	const int WindowY = 52;
	const int WindowWidthInTiles = 18;
	const int WindowHeightInTiles = 4;
	const byte WindowDisplayLayer = 200;
	const byte TextDisplayLayer = 210;
	Label? text;

	public override ScreenType Type { get; } = ScreenType.SelectPartyMember;

	public override bool Dark { get; } = true;

	public override bool TrapMouse { get; } = false;

	public override void Init()
	{
		base.Init();

		var (_, _, width, height) = ClientArea;
		text = AddLabel(0, 0, "", width, height, TextDisplayLayer);
	}

	public override void Open(Action? closeAction)
	{
		if (Game.CurrentText == null)
			throw new InvalidOperationException($"{nameof(SelectPartyMemberScreen)} needs {nameof(Game.CurrentText)} to be set beforehand.");

		base.Open(closeAction);

		Game.SelectedPartyMemberSlot = null;
		Game.ScriptTextShown(Game.CurrentText);
		text!.SetText(Game.CurrentText);
		text.Visible = true;
		Game.TrapMouseInPortraitArea();
		Game.Cursor.CursorType = CursorType.Sword;
	}

	public override void Close()
	{
		Game.UntrapMouse();

		base.Close();
	}

	public override bool KeyDown(Key key, KeyModifiers keyModifiers)
	{
		if (key == Key.Escape)
			Game.ScreenHandler.PopScreen();

		return true;
	}

	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		if (buttons == MouseButtons.Left)
			Game.SelectedPartyMemberSlot = Game.TestPartyPortraitHit(position);

		Game.ScreenHandler.PopScreen();
		return true;
	}
}
