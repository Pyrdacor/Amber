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
	List<IEvent> Events { get; }
}
