using Amber.Common;
using Amber.Serialization;
using Amberstar.GameData.Events;
using System.Runtime.InteropServices;

namespace Amberstar.GameData.Legacy;


[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct EventData
{
	public EventType Type;
	public byte Byte1;
	public byte Byte2;
	public byte Byte3;
	public byte Byte4;
	public byte Save;
	public word Word6;
	public word Word8;
}

// Events always have 10 bytes. 6 bytes and 2 words.
// It seems like the last byte (at offset 5) is a
// "save event index". If it is non-zero the save index
// is calculated as "(mapIndex - 1) * 65 + the given value".
// Then the lowest 3 bits give the bit index and the higher
// bits give the byte index (so right shift by 3).
// Saving means disabling the event so it can't be triggered
// again. So this is not done via a distinct event like in
// Ambermoon but through the 6th byte in the event. Note
// that this can also deactivate other events of course.
//
// There are two exceptions:
// - Chest events are always triggered even if deactivated through saving.
//   Most likely as empty chests will not be shown anyway and this way you
//   can enable saving but still return to the chest to get more items.
// - DoorExit events are not triggered but the chained additional event is,
//   even if the door was deactived through saving.

internal abstract class Event(EventData eventData) : IEvent
{
	protected readonly EventData eventData = eventData;

	public EventType Type => eventData.Type;

	public virtual bool SaveEvent => eventData.Save != 0;

	public static unsafe Event ReadEvent(IDataReader reader)
	{
		var data = reader.ReadBytes(sizeof(EventData));
		EventData eventData;

		fixed (byte* ptr = data)
		{
			eventData = *(EventData*)ptr;
		}

		return eventData.Type switch
		{
			EventType.MapExit => new MapExitEvent(eventData),			
			EventType.Door => new DoorEvent(eventData),
			EventType.ShowPictureText => new ShowPictureTextEvent(eventData),
			EventType.Chest => new ChestEvent(eventData),
			EventType.TrapDoor => new TrapDoorEvent(eventData),
			EventType.Teleporter => new TeleporterEvent(eventData),
			EventType.WindGate => new WindGateEvent(eventData),
			EventType.Spinner => new SpinnerEvent(eventData),
			EventType.DamageField => new DamageFieldEvent(eventData),
			EventType.AntiMagic => new AntiMagicEvent(eventData),
			EventType.HPRegeneration => new HPRegenerationEvent(eventData),
			EventType.SPRegeneration => new SPRegenerationEvent(eventData),
			EventType.ExecuteTrap => new ExecuteTrapEvent(eventData),
			EventType.RiddleMouth => new RiddleMouthEvent(eventData),
			EventType.AttributeChange => new AttributeChangeEvent(eventData),
			EventType.ChangeTile => new ChangeTileEvent(eventData),
			EventType.Encounter => new EncounterEvent(eventData),
			EventType.Place => new PlaceEvent(eventData),
			EventType.UseItem => new UseItemEvent(eventData),
			EventType.DoorExit => new DoorExitEvent(eventData),
			EventType.TravelExit => new TravelExitEvent(eventData),
			_ => throw new AmberException(ExceptionScope.Data, $"Unsupported event type: {(int)eventData.Type}")
		};
	}
}

internal class MapExitEvent(EventData eventData) : Event(eventData), IMapExitEvent
{
	public byte X => eventData.Byte1;

	public byte Y => eventData.Byte2;

	public Direction Direction => (Direction)eventData.Byte3;

	public word MapIndex => eventData.Word6;
}

internal class TravelExitEvent(EventData eventData) : Event(eventData), ITravelExitEvent
{
	public byte X => eventData.Byte1;

	public byte Y => eventData.Byte2;

	public Direction Direction => (Direction)eventData.Byte3;

	public word MapIndex => eventData.Word6;
}

internal class DoorEvent(EventData eventData) : Event(eventData), IDoorEvent
{
	public byte LockpickReduction => eventData.Byte1;

	public TrapType TrapType => (TrapType)eventData.Byte2;

	public byte TrapDamage => eventData.Byte3;

	public word ItemIndex => eventData.Word6;
}

internal class ShowPictureTextEvent(EventData eventData) : Event(eventData), IShowPictureTextEvent
{
	public byte Picture => eventData.Byte1;

	public byte TextIndex => eventData.Byte2;

	public ShowPictureTextTrigger Trigger => (ShowPictureTextTrigger)eventData.Byte3;

	public word SetWordBit => eventData.Word6;
}

internal class ChestEvent(EventData eventData) : Event(eventData), IChestEvent
{
	public byte LockpickReduction => eventData.Byte1;

	public TrapType TrapType => (TrapType)eventData.Byte2;

	public byte TrapDamage => eventData.Byte3;

	public bool Hidden => eventData.Byte4 != 0;

	public word ChestIndex => eventData.Word6;

	public word TextIndex => eventData.Word6;
}

internal class TrapDoorEvent(EventData eventData) : Event(eventData), ITrapDoorEvent
{
	public byte X => eventData.Byte1;

	public byte Y => eventData.Byte2;

	public bool Floor => eventData.Byte3 != 0;

	public byte TextIndex => eventData.Byte4;

	public word MapIndex => eventData.Word6;

	public word MaxFallDamage => eventData.Word8;
}

internal class TeleporterEvent(EventData eventData) : Event(eventData), ITeleporterEvent
{
	public byte X => eventData.Byte1;

	public byte Y => eventData.Byte2;

	public Direction Direction => (Direction)eventData.Byte3;

	public byte TextIndex => eventData.Byte4;

	public word MapIndex => eventData.Word6;
}

internal class WindGateEvent(EventData eventData) : Event(eventData), IWindGateEvent
{
	public byte X => eventData.Byte1;

	public byte Y => eventData.Byte2;

	public Direction Direction => (Direction)eventData.Byte3;

	public byte TextIndex => eventData.Byte4;

	public word MapIndex => eventData.Word6;
}

internal class SpinnerEvent(EventData eventData) : Event(eventData), ISpinnerEvent
{
	public Direction Direction => (Direction)eventData.Byte1;

	public byte TextIndex => eventData.Byte4;
}

internal class DamageFieldEvent(EventData eventData) : Event(eventData), IDamageFieldEvent
{
	public byte Damage => eventData.Byte1;

	public TargetGender TargetGender => (TargetGender)eventData.Byte2;

	public byte TextIndex => eventData.Byte4;
}

internal class AntiMagicEvent(EventData eventData) : Event(eventData), IAntiMagicEvent
{
	public ActiveSpellRemoval ActiveSpell => (ActiveSpellRemoval)eventData.Byte1;

	public byte TextIndex => eventData.Byte4;
}

internal class HPRegenerationEvent(EventData eventData) : Event(eventData), IHPRegenerationEvent
{
	public byte Amount => eventData.Byte1;

	public byte TextIndex => eventData.Byte4;

	public bool Fill => Amount == 0;
}

internal class SPRegenerationEvent(EventData eventData) : Event(eventData), ISPRegenerationEvent
{
	public byte Amount => eventData.Byte1;

	public byte TextIndex => eventData.Byte4;

	public bool Fill => Amount == 0;
}

internal class ExecuteTrapEvent(EventData eventData) : Event(eventData), IExecuteTrapEvent
{
	public TrapType TrapType => (TrapType)eventData.Byte1;

	public byte Damage => eventData.Byte3;

	public byte TextIndex => eventData.Byte4;

	public bool AffectAllPlayers =>
		TrapType == TrapType.DamageTrap ||
		TrapType == TrapType.PoisonGasCloud ||
		TrapType == TrapType.BlindingFlash ||
		TrapType == TrapType.ParalyzingGasCloud;

	public byte TrapEffectTextIndex => (byte)(6 + (int)TrapType);
}

internal class RiddleMouthEvent(EventData eventData) : Event(eventData), IRiddleMouthEvent
{
	public byte X => eventData.Byte1;

	public byte Y => eventData.Byte2;

	public byte RiddleTextIndex => eventData.Byte3;

	public byte SolvedTextIndex => eventData.Byte4;

	public word WordIndex => eventData.Word6;

	public word IconIndex => eventData.Word8;
}

internal class AttributeChangeEvent(EventData eventData) : Event(eventData), IAttributeChangeEvent
{
	public byte Attribute => eventData.Byte1;

	public bool Add => eventData.Byte2 != 0;

	public bool Random => eventData.Byte3 != 0;

	public byte TextIndex => eventData.Byte4;

	public bool AffectAllPlayers => eventData.Word6 == 0;

	public word Amount => eventData.Word8;
}

internal class ChangeTileEvent(EventData eventData) : Event(eventData), IChangeTileEvent
{
	public byte X => eventData.Byte1;

	public byte Y => eventData.Byte2;

	public byte TextIndex => eventData.Byte4;

	public word IconIndex => eventData.Word6;
}

internal class UseItemEvent(EventData eventData) : Event(eventData), IUseItemEvent
{
	public byte X => eventData.Byte1;

	public byte Y => eventData.Byte2;

	public byte TextIndex => eventData.Byte4;

	public word ItemIndex => eventData.Word6;

	public word IconIndex => eventData.Word8;
}

internal class DoorExitEvent(EventData eventData) : Event(eventData), IDoorExitEvent
{
	public byte LockpickReduction => eventData.Byte1;

	public TrapType TrapType => (TrapType)eventData.Byte2;

	public byte TrapDamage => eventData.Byte3;

	public byte OpenedEventIndex => eventData.Byte4;

	public word ItemIndex => eventData.Word6;
}

internal class EncounterEvent(EventData eventData) : Event(eventData), IEncounterEvent
{
	public byte Chance => eventData.Byte1;

	public byte Quest => eventData.Byte2;

	public byte QuestTextIndex => eventData.Byte3;

	public byte NoQuestTextIndex => eventData.Byte4;

	public word MonsterGroupIndex => eventData.Word6;
}

internal class PlaceEvent(EventData eventData) : Event(eventData), IPlaceEvent
{
	public byte OpeningHour => eventData.Byte1;

	public byte ClosingHour => eventData.Byte2;

	public PlaceType PlaceType => (PlaceType)eventData.Byte3;

	public byte ClosedTextIndex => eventData.Byte4;

	public word PlaceIndex => eventData.Word6;

	public word WaresIndex => eventData.Word8;

	public bool AlwaysOpen => OpeningHour == 0;
}
