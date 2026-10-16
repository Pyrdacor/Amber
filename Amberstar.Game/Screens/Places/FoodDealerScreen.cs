using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

/// <summary>
/// The food dealer sells food rations. Bought food is placed on a
/// separate food pile which can be distributed like the gold.
/// </summary>
internal sealed class FoodDealerScreen : PlaceScreen
{
	const int BuyButtonIndex = 3;
	const int DistributeFoodButtonIndex = 4;
	const int GiveFoodButtonIndex = 5;
	const int MaxFoodPerPurchase = 99;
	static readonly Rect foodArea = new(240, 113, 64, 16);
	Label? foodDisplay;
	int placeFood = 0;

	public override ScreenType Type { get; } = ScreenType.FoodDealer;

	protected override Image80x80 Image { get; } = Image80x80.Merchant;

	protected override Message LeaveMessage { get; } = Message.GiveItemsToMerchant;

	protected override bool SomethingLeftBehind => base.SomethingLeftBehind || placeFood != 0;

	protected override int PlaceFood
	{
		get => placeFood;
		set
		{
			placeFood = value;
			foodDisplay?.SetText($"{placeFood:0000}", 15, TextManager.TransparentPaper, ButtonGridPaletteIndex);
			RequestButtonSetup();
		}
	}

	internal int ScriptPlaceFood => placeFood;

	public override void Init()
	{
		base.Init();

		var (x, y, width, _) = foodArea;
		var foodLabel = AddLabel(x, y, Game.LoadUIText(UIText.Food), width, 7);
		foodLabel.Alignment = TextAlignment.Center;
		foodLabel.PaletteIndex = ButtonGridPaletteIndex;

		foodDisplay = AddLabel(x, y + 8, "0000", width, 7);
		foodDisplay.Alignment = TextAlignment.Center;
		foodDisplay.PaletteIndex = ButtonGridPaletteIndex;
	}

	public override void Open(Action? closeAction)
	{
		base.Open(closeAction);

		PlaceFood = 0;

		var gold = Game.GetFullText(Game.LoadUIText(UIText.Gold));
		ShowMessage(Game.AssetProvider.TextLoader.FromString($"{Game.GetFullText(Game.LoadMessageText(Message.FoodPrice))} {Place.Data.Price} {gold}"), false);
	}

	public override void ScreenPopped(Screen screen)
	{
		base.ScreenPopped(screen);

		if (screen is BuyFoodScreen && Game.CurrentAmount > 0)
		{
			int amount = Game.CurrentAmount;
			var food = Game.GetFullText(Game.LoadUIText(UIText.Food));
			var question = Game.AssetProvider.TextLoader.FromString($"{food} x {amount} {Game.GetFullText(Game.LoadMessageText(Message.PriceForItem))}");

			AskToBuy(question, amount * Place.Data.Price, () => PlaceFood += amount);
		}
	}

	protected override void SetupPlaceButtons(ButtonGrid buttonGrid)
	{
		buttonGrid.SetButton(BuyButtonIndex, ButtonType.BuyFood);
		buttonGrid.SetButton(DistributeFoodButtonIndex, ButtonType.DistributeFood);
		buttonGrid.SetButton(GiveFoodButtonIndex, ButtonType.GiveFood);

		buttonGrid.EnableButton(BuyButtonIndex, Place != null && PlaceGold >= Place.Data.Price && placeFood < short.MaxValue);
		buttonGrid.EnableButton(DistributeFoodButtonIndex, placeFood != 0);
		buttonGrid.EnableButton(GiveFoodButtonIndex, placeFood != 0);
	}

	protected override void PlaceButtonClicked(int index)
	{
		switch (index)
		{
			case BuyButtonIndex:
				Game.CurrentAmount = 0;
				Game.CurrentMaxAmount = Math.Min(PlaceGold / Place.Data.Price, MaxFoodPerPurchase);
				Game.ScreenHandler.PushScreen(ScreenType.PlaceBuyFood);
				break;
			case DistributeFoodButtonIndex:
				PlaceFood = Game.DistributeFood(placeFood);
				break;
			case GiveFoodButtonIndex:
				Game.CurrentAmount = 0;
				Game.CurrentMaxAmount = placeFood;
				Game.ScreenHandler.PushScreen(ScreenType.PlaceGiveFood);
				break;
		}
	}

	internal class BuyFoodScreen : InputAmountScreen
	{
		public override ScreenType Type { get; } = ScreenType.PlaceBuyFood;
		protected override ItemGraphic Graphic { get; } = ItemGraphic.Ration;
		protected override IText InputLabelText => Game.LoadUIText(UIText.Food);
		protected override Message Message { get; } = Message.HowManyToBuy;
	}
}
