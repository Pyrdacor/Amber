namespace Amberstar.GameData;

public enum EventType : byte
{
	None,
	MapExit,
	Door,
	ShowPictureText,
	Chest,
	TrapDoor,
	Teleporter,
	WindGate,
	Spinner,
	DamageField,
	AntiMagic,
	HPRegeneration,
	SPRegeneration,
	ExecuteTrap,
	RiddleMouth,
	AttributeChange,
	ChangeTile,
	Encounter,
	Place,
	UseItem,
	DoorExit,
	TravelExit,
	Altar, // to assemble the Amberstar
	Outro, // triggers end sequence
	Invalid
}

public interface IEvent
{
	EventType Type { get; }

	bool SaveEvent { get; }

	public const int DataSize = 10;
}

public interface IEventProvider
{
	/// <summary>
	/// For persons, this is the map they are currently located.
	/// For maps it is the map itself.
	/// </summary>
	IMap Map { get; }

	List<IEvent> Events { get; }
}
