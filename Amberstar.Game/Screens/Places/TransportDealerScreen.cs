using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Events;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

/// <summary>
/// Horse, raft and ship dealers. The bought transport is placed at
/// a fixed position (usually in front of the town).
/// </summary>
internal sealed class TransportDealerScreen : PlaceScreen
{
	const int BuyButtonIndex = 3;
	bool transportCreated = false;

	public override ScreenType Type { get; } = ScreenType.TransportDealer;

	protected override Image80x80 Image => PlaceEvent?.PlaceType == PlaceType.HorseDealer ? Image80x80.HorseStable : Image80x80.ShipDealer;

	protected override Message LeaveMessage { get; } = Message.GiveGoldToMerchant;

	ITransportDealerData Data => (ITransportDealerData)Place.Data;

	// Horses are needed for every party member.
	int TotalPrice => Data.Transport == TransportType.Horse ? Data.Price * CountPartyMembers() : Data.Price;

	public override void Open(Action? closeAction)
	{
		base.Open(closeAction);

		// Only one transport at the same place and there must be a free transport slot.
		transportCreated = Game.State.GetTransportAtLocation(Data.SpawnX, Data.SpawnY, Data.SpawnMapIndex) != null ||
			!Game.State.HasFreeTransportSlot;
		RequestButtonSetup();
	}

	protected override void SetupPlaceButtons(ButtonGrid buttonGrid)
	{
		if (Place == null)
			return;

		var buttonType = Data.Transport switch
		{
			TransportType.Horse => ButtonType.ButHorse,
			TransportType.Raft => ButtonType.BuyRaft,
			_ => ButtonType.BuyShip
		};

		buttonGrid.SetButton(BuyButtonIndex, buttonType);
		buttonGrid.EnableButton(BuyButtonIndex, !transportCreated && PlaceGold >= TotalPrice);
	}

	protected override void PlaceButtonClicked(int index)
	{
		if (index != BuyButtonIndex)
			return;

		var (priceMessage, readyMessage) = Data.Transport switch
		{
			TransportType.Horse => (Message.HorsesPrice, Message.HorsesReady),
			TransportType.Raft => (Message.RaftPrice, Message.RaftReady),
			_ => (Message.ShipPrice, Message.ShipReady)
		};

		AskToBuy(Game.LoadMessageText(priceMessage), TotalPrice, () =>
		{
			Game.State.TryAddTransport(Data.Transport, Data.SpawnX, Data.SpawnY, Data.SpawnMapIndex);
			transportCreated = true;
			RequestButtonSetup();
			ShowMessage(readyMessage);
		});
	}
}
