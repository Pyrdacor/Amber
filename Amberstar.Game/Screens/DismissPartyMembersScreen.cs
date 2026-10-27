using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

/// <summary>
/// Before saving, party members can be dismissed (Save_game in EYE.ASM of the original).
/// The portraits of the members 2 to 6 are shown, a click marks a member (chequered
/// portrait) or unmarks it. The OK icon removes the marked members and saves the game,
/// the exit icon aborts saving.
/// </summary>
internal sealed class DismissPartyMembersScreen : Screen
{
	const byte DisplayLayer = 50;
	const int PortraitWidth = 32;
	const int PortraitHeight = 34;
	const int ActiveNameColorIndex = 8;
	const int NameColorIndex = 9;
	const int MessageColorIndex = 15;
	// portrait and name positions of the members 2 to 6 (Portrait_pos_list)
	static readonly (Position Portrait, Position Name)[] positions =
	[
		(new(16, 49), new(16, 85)),
		(new(112, 73), new(106, 109)),
		(new(16, 97), new(16, 133)),
		(new(112, 121), new(106, 157)),
		(new(16, 145), new(16, 181)),
	];
	static readonly Rect mouseArea = new(16, 49, 176, 143);
	static readonly Rect messageArea = new(208, 49, 96, 80);
	static readonly Position okPosition = new(128, 176);
	static readonly Position exitPosition = new(160, 176);
	readonly bool[] dismiss = new bool[Game.MaxPartyMembers];
	readonly List<Image>?[] chequered = new List<Image>?[Game.MaxPartyMembers];
	Label? message;
	Button? okButton;
	Button? exitButton;
	bool waitForClick = false;

	public override ScreenType Type { get; } = ScreenType.DismissPartyMembers;

	// the button grid of the options stays visible
	public override bool Transparent => true;

	public override void Open(Action? closeAction)
	{
		base.Open(closeAction);

		Array.Clear(dismiss);
		Array.Clear(chequered);
		waitForClick = false;

		var graphics = Game.GraphicIndexProvider;
		byte palette = Game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI];

		for (int slot = 2; slot <= Game.MaxPartyMembers; slot++)
		{
			var partyMember = Game.State.GetPartyMember(slot);

			if (partyMember == null)
				continue;

			var (portrait, namePosition) = positions[slot - 2];
			var image = AddImage(portrait.X, portrait.Y, PortraitWidth, PortraitHeight,
				graphics.GetPersonPortraitIndex(Game.State.PartyCharacterIndices[slot - 1]), DisplayLayer);
			image.PaletteIndex = palette;
			var name = AddLabel(namePosition.X, namePosition.Y, 96, 7, DisplayLayer);
			name.SetText(partyMember.Name, slot == Game.State.ActivePartyMemberIndex ? ActiveNameColorIndex : NameColorIndex,
				TextManager.TransparentPaper, palette);
			name.Visible = true;
		}

		okButton = AddButton(okPosition.X, okPosition.Y, ButtonType.Ok, DisplayLayer);
		okButton.ClickAction += Confirm;
		exitButton = AddButton(exitPosition.X, exitPosition.Y, ButtonType.Exit, DisplayLayer);
		exitButton.ClickAction += () => Game.ScreenHandler.PopScreen();

		var (x, y, width, height) = messageArea;
		message = AddLabel(x, y, width, height, DisplayLayer);
		ShowDefaultMessage();

		Game.Cursor.CursorType = CursorType.Sword;
		Game.TrapMouse(mouseArea);
	}

	public override void Close()
	{
		Game.UntrapMouse();

		base.Close();
	}

	void ShowMessage(IText text)
	{
		Game.ScriptTextShown(text);
		message!.SetText(text, messageArea.Size.Width, MessageColorIndex, TextManager.TransparentPaper,
			Game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI]);
		message.Visible = true;
	}

	void ShowDefaultMessage() => ShowMessage(Game.LoadMessageText(Message.DismissPartyMembers));

	// a message is shown until the next click (PlusMes in the original)
	void ShowClickMessage(IText text)
	{
		ShowMessage(text);
		waitForClick = true;
		Game.Cursor.CursorType = CursorType.Zzz;
	}

	void EndClickWait()
	{
		waitForClick = false;
		Game.Cursor.CursorType = CursorType.Sword;
		ShowDefaultMessage();
	}

	/// <summary>
	/// Marks or unmarks the party member in the given slot (Portrait_selected in the original).
	/// This is also used by scenario scripts.
	/// </summary>
	internal void ToggleMember(int slot)
	{
		var partyMember = Game.State.GetPartyMember(slot);

		if (slot < 2 || partyMember == null)
			return;

		if (slot == Game.State.ActivePartyMemberIndex)
			ShowClickMessage(Game.LoadMessageText(Message.LeaderCannotBeDismissed));
		else if (partyMember.HasAnyConditionOf(Condition.Dead | Condition.Ashes | Condition.Dust))
			ShowClickMessage(Game.LoadMessageText(Message.CannotDismissDead));
		else if (partyMember.HasAnyConditionOf(Condition.Mad))
			ShowClickMessage(Game.AssetProvider.TextLoader.FromString($"{partyMember.Name} {Game.GetFullText(Game.LoadMessageText(Message.RefusesToLeave))}"));
		else if (partyMember.HasAnyConditionOf(Condition.Petrified))
			ShowClickMessage(Game.LoadMessageText(Message.CannotDismissPetrified));
		else
		{
			dismiss[slot - 1] = !dismiss[slot - 1];

			if (dismiss[slot - 1])
			{
				// the chequered pattern covers the portrait (16 + 16 + 2 lines)
				var position = positions[slot - 2].Portrait;
				int chequeredIndex = Game.GraphicIndexProvider.GetUIGraphicIndex(UIGraphic.ChequeredIcon);
				byte palette = Game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI];
				var images = new List<Image>();

				foreach (var (offsetY, height) in new[] { (0, 16), (16, 16), (32, 2) })
				{
					var image = AddImage(position.X, position.Y + offsetY, PortraitWidth, height, chequeredIndex, DisplayLayer + 10);
					image.PaletteIndex = palette;
					images.Add(image);
				}

				chequered[slot - 1] = images;
			}
			else
			{
				chequered[slot - 1]?.ForEach(image => image.Destroy());
				chequered[slot - 1] = null;
			}
		}
	}

	/// <summary>
	/// Removes the marked members and saves the game (Do_SaveGame in the original).
	/// </summary>
	internal void Confirm()
	{
		for (int slot = 2; slot <= Game.MaxPartyMembers; slot++)
		{
			if (dismiss[slot - 1])
				Game.State.RemovePartyMember(slot);
		}

		Game.UpdatePartyMembers();
		Game.SaveGame();
		Game.ScreenHandler.PopScreen();
	}

	public override bool KeyDown(Key key, KeyModifiers keyModifiers)
	{
		if (waitForClick)
		{
			if (key == Key.Space || key == Key.Escape || key == Key.Enter)
				EndClickWait();

			return true;
		}

		if (key == Key.Escape)
			Game.ScreenHandler.PopScreen();
		else if (key == Key.Enter)
			Confirm();

		return true;
	}

	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		if (waitForClick)
		{
			EndClickWait();
			return true;
		}

		if (buttons != MouseButtons.Left)
			return true;

		if (okButton!.MouseClick(position) || exitButton!.MouseClick(position))
			return true;

		for (int i = 0; i < positions.Length; i++)
		{
			if (new Rect(positions[i].Portrait, new Size(PortraitWidth, PortraitHeight)).Contains(position))
			{
				ToggleMember(2 + i);
				break;
			}
		}

		return true;
	}
}
