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
	EventItem,
	DoorExit,
	TravelExit,
	Invalid
}

public interface IEvent
{
	EventType Type { get; }

	bool SaveEvent { get; }

	public const int DataSize = 10;
}
