using Amber.Common;
using Amberstar.Game.Events;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Events;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

/// <summary>
/// Base class of all places like merchants, guilds or inns (PLACES.ASM in the original).
///
/// Every place has a gold pile. The party can put all its gold onto it, pays from it
/// and takes the rest with it again. The lower button row is the same for all places:
/// pool gold, distribute gold and give gold to a party member. The upper right button
/// leaves the place. If something is left behind, the player is asked before.
/// </summary>
internal abstract class PlaceScreen : ButtonGridScreen, IWaitForClickScreen
{
	protected const int ExitButtonIndex = 2;
	protected const int PoolGoldButtonIndex = 6;
	protected const int DistributeGoldButtonIndex = 7;
	protected const int GiveGoldButtonIndex = 8;
	protected static readonly Rect MessageArea = new(112, 49, 192, 48);
	protected static readonly Rect GoldArea = new(112, 113, 64, 16);
	static readonly Rect headlineArea = new(16, 39, 288, 7);
	Image? image;
	Label? headline;
	Label? message;
	Label? goldLabel;
	Label? goldDisplay;
	int placeGold = 0;
	bool waitForClick = false;
	Action? afterClickAction = null;
	TransferType transfer = TransferType.None;

	protected enum TransferType
	{
		None,
		Gold,
		Food
	}

	protected PlaceEvent PlaceEvent { get; private set; } = null!;

	protected IPlace Place { get; private set; } = null!;

	protected abstract Image80x80 Image { get; }

	/// <summary>
	/// Question if the player leaves while something is left behind.
	/// </summary>
	protected abstract Message LeaveMessage { get; }

	protected virtual bool SomethingLeftBehind => placeGold != 0;

	protected int PlaceGold
	{
		get => placeGold;
		set
		{
			placeGold = value;
			goldDisplay?.SetText($"{placeGold:00000}", 15, TextManager.TransparentPaper, ButtonGridPaletteIndex);
			RequestButtonSetup();
		}
	}

	internal int ScriptPlaceGold => placeGold;

	internal override byte ButtonGridPaletteIndex => Game?.PaletteIndexProvider.Get80x80ImagePaletteIndex(Image) ?? 0;

	public bool WaitingForClick => waitForClick;

	public override void Init()
	{
		base.Init();

		image = AddImage(16, 49, 80, 80, Game.GraphicIndexProvider.Get80x80ImageIndex(Image), 0, true);

		var (x, y, width, height) = headlineArea;
		headline = AddLabel(x, y, width, height, 20);

		(x, y, width, height) = MessageArea;
		message = AddLabel(x, y, width, height, 20);

		(x, y, width, height) = GoldArea;
		goldLabel = AddLabel(x, y, Game.LoadUIText(UIText.Gold), width, 7);
		goldLabel.Alignment = TextAlignment.Center;

		goldDisplay = AddLabel(x, y + 8, "00000", width, 7);
		goldDisplay.Alignment = TextAlignment.Center;
		goldDisplay.PaletteIndex = ButtonGridPaletteIndex;
	}

	public override void Open(Action? closeAction)
	{
		PlaceEvent = (Game.EventHandler.CurrentEvent as PlaceEvent)!;
		Place = Game.AssetProvider.PlaceLoader.LoadPlace(PlaceEvent.PlaceType, PlaceEvent.PlaceIndex);

		base.Open(closeAction);

		waitForClick = false;
		afterClickAction = null;
		transfer = TransferType.None;
		message!.Visible = false;

		// The image can depend on the place type.
		var palette = ButtonGridPaletteIndex;
		image!.SetTextureIndex(Game.GraphicIndexProvider.Get80x80ImageIndex(Image));
		image.PaletteIndex = palette;
		goldLabel!.PaletteIndex = palette;
		goldDisplay!.PaletteIndex = palette;
		RequestButtonGridPaletteUpdate();
		headline!.SetText(Place.Name, 15, TextManager.TransparentPaper, palette);
		headline.Alignment = TextAlignment.Center;
		PlaceGold = 0;

		Game.SetLayout(Layout.Place, palette);
		Game.Cursor.CursorType = CursorType.Sword;
	}

	public override void Close()
	{
		message!.Visible = false;
		Game.UntrapMouse();
		Game.ResetStatusIcons();

		base.Close();
	}

	public override void ScreenPopped(Screen screen)
	{
		base.ScreenPopped(screen);

		if (!screen.Transparent)
		{
			Game.SetLayout(Layout.Place, ButtonGridPaletteIndex);
			Game.Cursor.CursorType = CursorType.Sword;
		}

		if (screen is GiveGoldScreen && Game.CurrentAmount > 0)
			StartTransfer(TransferType.Gold, Game.CurrentAmount);
		else if (screen is GiveFoodScreen && Game.CurrentAmount > 0)
			StartTransfer(TransferType.Food, Game.CurrentAmount);
	}

	protected abstract void SetupPlaceButtons(ButtonGrid buttonGrid);

	protected abstract void PlaceButtonClicked(int index);

	/// <summary>
	/// Called after the place was left.
	/// </summary>
	protected virtual void Left()
	{
	}

	protected sealed override void SetupButtons(ButtonGrid buttonGrid)
	{
		for (int i = 0; i < 9; i++)
			buttonGrid.SetButton(i, ButtonType.Empty);

		SetupPlaceButtons(buttonGrid);

		buttonGrid.SetButton(ExitButtonIndex, ButtonType.Exit);
		buttonGrid.SetButton(PoolGoldButtonIndex, ButtonType.GatherGold);
		buttonGrid.SetButton(DistributeGoldButtonIndex, ButtonType.DistributeGold);
		buttonGrid.SetButton(GiveGoldButtonIndex, ButtonType.GiveGold);

		// Mad or petrified party members don't give their gold.
		bool anyGold = Game.State.PartyMembers.Any(partyMember => partyMember.Gold != 0 && !partyMember.HasAnyConditionOf(Condition.Mad | Condition.Petrified));
		buttonGrid.EnableButton(PoolGoldButtonIndex, anyGold && placeGold < short.MaxValue);
		buttonGrid.EnableButton(DistributeGoldButtonIndex, placeGold != 0);
		buttonGrid.EnableButton(GiveGoldButtonIndex, placeGold != 0);
	}

	protected sealed override void ButtonClicked(int index)
	{
		if (waitForClick || transfer != TransferType.None)
			return;

		switch (index)
		{
			case ExitButtonIndex:
				Leave();
				break;
			case PoolGoldButtonIndex:
				PlaceGold = Game.PoolGold(placeGold);
				break;
			case DistributeGoldButtonIndex:
				PlaceGold = Game.DistributeGold(placeGold);
				break;
			case GiveGoldButtonIndex:
				Game.CurrentAmount = 0;
				Game.CurrentMaxAmount = placeGold;
				Game.ScreenHandler.PushScreen(ScreenType.PlaceGiveGold);
				break;
			default:
				PlaceButtonClicked(index);
				break;
		}
	}

	private void Leave()
	{
		void DoLeave()
		{
			Game.ScreenHandler.PopScreen();
			Left();
		}

		if (SomethingLeftBehind)
			Game.AskForConfirmation(Game.LoadMessageText(LeaveMessage), DoLeave);
		else
			DoLeave();
	}

	/// <summary>
	/// Shows a message in the message area.
	/// </summary>
	protected void ShowMessage(IText text, bool waitForClick = true, Action? afterClickAction = null)
	{
		Game.ScriptTextShown(text);
		message!.SetText(text, MessageArea.Size.Width, 15, TextManager.TransparentPaper, ButtonGridPaletteIndex);
		message.Visible = true;

		this.waitForClick = waitForClick;
		this.afterClickAction = afterClickAction;

		if (waitForClick)
		{
			Game.Cursor.CursorType = CursorType.Zzz;
			Game.TrapMouse(MessageArea);
		}
	}

	protected void ShowMessage(Message message, bool waitForClick = true, Action? afterClickAction = null) =>
		ShowMessage(Game.LoadMessageText(message), waitForClick, afterClickAction);

	protected void HideMessage()
	{
		message!.Visible = false;
	}

	/// <summary>
	/// Asks if something should be bought for the given price.
	/// The price is paid from the gold pile of the place.
	/// </summary>
	protected void AskToBuy(IText question, int price, Action bought)
	{
		var gold = Game.LoadUIText(UIText.Gold).GetString();
		var text = Game.AssetProvider.TextLoader.FromString($"{question.GetString()} {price} {gold}");

		Game.AskForConfirmation(text, () =>
		{
			placeGold -= price;
			bought();
			// updates the display and the buttons after the purchase
			PlaceGold = placeGold;
		});
	}

	protected int CountPartyMembers() => Game.State.PartyMembers.Count();

	private void EndClickWait()
	{
		waitForClick = false;
		Game.Cursor.CursorType = CursorType.Sword;
		message!.Visible = false;
		Game.UntrapMouse();

		var action = afterClickAction;
		afterClickAction = null;
		action?.Invoke();
	}

	/// <summary>
	/// Food on the place (only used by the food dealer).
	/// </summary>
	protected virtual int PlaceFood
	{
		get => 0;
		set { }
	}

	private void StartTransfer(TransferType transferType, int amount)
	{
		int takers = transferType == TransferType.Gold ? Game.SetHandIconsByGold(amount) : Game.SetHandIconsByFood(amount);

		if (takers == 0)
		{
			Game.ResetStatusIcons();
			ShowMessage(transferType == TransferType.Gold ? Message.NoMemberCanCarryThatMuchGold : Message.NoMemberHasRoomForItem);
			return;
		}

		transfer = transferType;
		Game.TrapMouseInPortraitArea();
		Game.Cursor.CursorType = transferType == TransferType.Gold ? CursorType.Gold : CursorType.Food;
	}

	private void EndTransfer()
	{
		transfer = TransferType.None;
		Game.CurrentAmount = 0;
		Game.Cursor.CursorType = CursorType.Sword;
		Game.UntrapMouse();
		Game.ResetStatusIcons();
	}

	public override bool KeyDown(Key key, KeyModifiers keyModifiers)
	{
		if (waitForClick)
		{
			if (key == Key.Space || key == Key.Escape || key == Key.Enter)
				EndClickWait();

			return true;
		}

		if (transfer != TransferType.None)
		{
			if (key == Key.Escape)
				EndTransfer();

			return true;
		}

		// Escape uses the exit button so nothing is left behind by accident.
		if (key == Key.Escape)
		{
			Leave();
			return true;
		}

		return base.KeyDown(key, keyModifiers);
	}

	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		if (waitForClick)
		{
			EndClickWait();
			return true;
		}

		if (transfer != TransferType.None)
		{
			var transferType = transfer;
			int amount = Game.CurrentAmount;
			EndTransfer();

			int? slot = buttons == MouseButtons.Left ? Game.TestPartyPortraitHit(position) : null;

			if (slot != null)
			{
				if (transferType == TransferType.Gold && Game.TryAddGold(slot.Value, amount))
					PlaceGold -= amount;
				else if (transferType == TransferType.Food && Game.TryAddFood(slot.Value, amount))
					PlaceFood -= amount;
			}

			return true;
		}

		return base.MouseDown(position, buttons, keyModifiers);
	}

	internal class GiveGoldScreen : InputAmountScreen
	{
		public override ScreenType Type { get; } = ScreenType.PlaceGiveGold;
		protected override ItemGraphic Graphic { get; } = ItemGraphic.WishingCoins;
		protected override IText InputLabelText => Game.LoadUIText(UIText.Gold);
		protected override Message Message { get; } = Message.TransferHowManyGold;
	}

	internal class GiveFoodScreen : InputAmountScreen
	{
		public override ScreenType Type { get; } = ScreenType.PlaceGiveFood;
		protected override ItemGraphic Graphic { get; } = ItemGraphic.Ration;
		protected override IText InputLabelText => Game.LoadUIText(UIText.Food);
		protected override Message Message { get; } = Message.TransferHowManyFood;
	}
}
