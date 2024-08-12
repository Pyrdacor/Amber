using Amber.Common;
using Amber.Serialization;
using Amberstar.GameData.Events;

namespace Amberstar.GameData.Legacy;

// 24 bytes (12 words)
[System.Runtime.CompilerServices.InlineArray(12)]
internal struct PlaceDataArray
{
	private word _element0;
}

internal abstract class PlaceData(PlaceType placeType, PlaceDataArray placeData) : IPlaceData
{
	protected readonly PlaceDataArray placeData = placeData;

	public PlaceType Type { get; } = placeType;

	public virtual word Price { get; } = placeData[0];

	public static unsafe PlaceData ReadPlaceData(PlaceType placeType, IDataReader reader)
	{
		var data = reader.ReadBytes(sizeof(PlaceDataArray));
		PlaceDataArray placeData;

		fixed (byte* ptr = data)
		{
			placeData = *(PlaceDataArray*)ptr;
		}

		return placeType switch
		{
			>= PlaceType.Guild1 and <= PlaceType.Guild8 => new GuildData((Class)((int)placeType - (int)PlaceType.Guild1), placeData),
			PlaceType.Merchant => new MerchantData(placeData),
			PlaceType.FoodDealer => new FoodDealerData(placeData),
			PlaceType.HorseDealer or PlaceType.RaftDealer or PlaceType.ShipDealer => new TransportDealerData(placeType, placeData),
			PlaceType.Healer => new HealerData(placeData),
			PlaceType.Sage => new SageData(placeData),
			PlaceType.Inn => new InnData(placeData),
			PlaceType.Library => new LibraryData(placeData),
			_ => throw new AmberException(ExceptionScope.Data, $"Unsupported place type: {(int)placeType}"),
		};
	}
	
}

internal class FoodDealerData(PlaceDataArray placeData) : PlaceData(PlaceType.FoodDealer, placeData)
{

}

public enum Class
{
	// TODO ...
}

internal class GuildData : PlaceData
{
	public GuildData(Class @class, PlaceDataArray placeData)
		: base((PlaceType)((int)PlaceType.Guild1 + (int)@class), placeData)
	{

	}

	public word JoinPrice => Price;

	public word LevelPrice => placeData[1];
}

public enum Transport
{
	Horse,
	Raft,
	Ship
}

internal class TransportDealerData : PlaceData
{
	public TransportDealerData(PlaceType placeType, PlaceDataArray placeData)
		: base(placeType, placeData)
	{
		Transport = placeType switch
		{
			PlaceType.HorseDealer => Transport.Horse,
			PlaceType.RaftDealer => Transport.Raft,
			PlaceType.ShipDealer => Transport.Ship,
			_ => throw new AmberException(ExceptionScope.Application, "Invalid transport place type")
		};

		if ((word)Transport != placeData[4])
			throw new AmberException(ExceptionScope.Data, "Mismatching transport type in place data");
	}

	public Transport Transport { get; }

	public word SpawnX => placeData[1];

	public word SpawnY => placeData[2];

	public word SpawnMapIndex => placeData[3];
}

internal class HealerData(PlaceDataArray placeData) : PlaceData(PlaceType.Healer, placeData)
{
	// TODO
	// Price to heal condition:
	// 1 -> [5]
	// 4 -> [6]
	// 8 -> [0]
	// 9 -> [1]
	// 10 -> [2]
	// 11 -> [3]
	// 12 -> [4]
	// 13 -> [7]
	// 14 -> [8]
	// 15 -> [9]
	// Others seem not to be healable (maybe battle-temp curses etc)

	public word RemoveCursePrice => placeData[10];
}

internal class SageData(PlaceDataArray placeData) : PlaceData(PlaceType.Sage, placeData)
{

}

internal class InnData(PlaceDataArray placeData) : PlaceData(PlaceType.Inn, placeData)
{

}

internal class MerchantData(PlaceDataArray placeData) : PlaceData(PlaceType.Merchant, placeData)
{

}

internal class LibraryData(PlaceDataArray placeData) : PlaceData(PlaceType.Library, placeData)
{

}
