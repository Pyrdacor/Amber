using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Events;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

// TODO: Guilds, merchants, healers, sages and magic merchants. Until then only the gold functions work.
internal sealed class UnfinishedPlaceScreen : PlaceScreen
{
	public override ScreenType Type { get; } = ScreenType.Place;

	protected override Image80x80 Image => PlaceEvent?.PlaceType switch
	{
		>= PlaceType.FirstGuild and <= PlaceType.LastGuild => Image80x80.Guild,
		PlaceType.Healer => Image80x80.Healer,
		PlaceType.Sage => Image80x80.Sage,
		PlaceType.Library => Image80x80.Library,
		_ => Image80x80.Merchant
	};

	protected override Message LeaveMessage => PlaceEvent.PlaceType switch
	{
		>= PlaceType.FirstGuild and <= PlaceType.LastGuild => Message.LeaveGoldForGuild,
		PlaceType.Healer => Message.GiveGoldToHealer,
		PlaceType.Sage => Message.GiveGoldToMerchant,
		_ => Message.GiveItemsToMerchant
	};

	public override void Open(Action? closeAction)
	{
		base.Open(closeAction);

		ShowMessage(Game.AssetProvider.TextLoader.FromString($"{PlaceEvent.PlaceType} is not implemented yet."), false);
	}

	protected override void SetupPlaceButtons(ButtonGrid buttonGrid)
	{
	}

	protected override void PlaceButtonClicked(int index)
	{
	}
}
