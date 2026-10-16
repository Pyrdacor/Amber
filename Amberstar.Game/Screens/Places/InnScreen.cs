using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

/// <summary>
/// The inn rents a guestroom for the whole party. After leaving the inn the
/// party rests in the room (camp with full regeneration in the original).
/// </summary>
internal sealed class InnScreen : PlaceScreen
{
	const int GuestroomButtonIndex = 3;
	bool roomRented = false;

	public override ScreenType Type { get; } = ScreenType.Inn;

	protected override Image80x80 Image { get; } = Image80x80.Inn;

	protected override Message LeaveMessage { get; } = Message.GiveGoldToInn;

	int TotalPrice => Place.Data.Price * CountPartyMembers();

	public override void Open(Action? closeAction)
	{
		roomRented = false;

		base.Open(closeAction);
	}

	protected override void SetupPlaceButtons(ButtonGrid buttonGrid)
	{
		buttonGrid.SetButton(GuestroomButtonIndex, ButtonType.Camp);
		buttonGrid.EnableButton(GuestroomButtonIndex, !roomRented && PlaceGold >= TotalPrice);
	}

	protected override void PlaceButtonClicked(int index)
	{
		if (index == GuestroomButtonIndex)
			AskToBuy(Game.LoadMessageText(Message.InnRoomPrice), TotalPrice, () => roomRented = true);
	}

	protected override void Left()
	{
		if (roomRented)
			Game.OpenCamp(inn: true);
	}
}
