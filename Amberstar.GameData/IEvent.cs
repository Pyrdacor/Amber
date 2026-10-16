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

	/// <summary>
	/// Index of the event bit inside the map's part of the event bits.
	/// The bit index in the savegame is (mapIndex - 1) * 65 + SaveIndex.
	/// If the bit is set, the event is disabled. 0 means the event is never disabled.
	/// Note that this is not the event index, so events can also disable other events.
	/// </summary>
	byte SaveIndex { get; }

	bool SaveEvent => SaveIndex != 0;

	public const int DataSize = 10;
}

public interface IMapEventProvider
{
	IMap Map { get; }

	List<IEvent> Events { get; }
}
