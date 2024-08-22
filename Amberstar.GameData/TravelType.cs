namespace Amberstar.GameData;

public enum TravelType : byte
{
	Walk,
	Horse,
	Raft,
	Ship,
	MagicDisc,
	Eagle,
	SuperChicken,
	COUNT
}

public static class TravelTypeExtensions
{
	/// <summary>
	/// Note: According to original code, if the player switches the travel type, its first move
	/// will always increase the time and only then setup the counter so further moves use this.
	/// </summary>
	public static int MovesPerTimeProgress(this TravelType travelType) => travelType switch
	{
		TravelType.Walk => 4,
		TravelType.Horse => 8,
		TravelType.Raft => 6,
		TravelType.Ship => 8,
		TravelType.MagicDisc => 4,
		TravelType.Eagle => 12,
		TravelType.SuperChicken => 12,
		_ => 0
	};


}