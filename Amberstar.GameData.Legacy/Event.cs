using Amber.Common;
using Amber.Serialization;
using Amberstar.GameData.Events;
using System.Runtime.InteropServices;

namespace Amberstar.GameData.Legacy;


[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct EventData
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
// There are some exceptions regarding chest events and door
// exit events but the code is quite confusing.

public abstract class Event(EventData eventData) : IEvent
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
			EventType.TravelExit => new TravelExitEvent(eventData),
			EventType.Door => new DoorEvent(eventData),
			EventType.ShowPictureText => new ShowPictureTextEvent(eventData),
			EventType.Chest => new ChestEvent(eventData),
			EventType.ExecuteTrap => new ExecuteTrapEvent(eventData),
			EventType.None or > EventType.Invalid => throw new AmberException(ExceptionScope.Data, $"Unsupported event type: {(int)eventData.Type}"),
			_ => throw new NotImplementedException()
		};
	}
}

public class MapExitEvent(EventData eventData) : Event(eventData), IMapExitEvent
{
	public byte X => eventData.Byte1;

	public byte Y => eventData.Byte2;

	public byte Direction => eventData.Byte3;

	public word MapIndex => eventData.Word6;
}

public class TravelExitEvent(EventData eventData) : Event(eventData), ITravelExitEvent
{
	public byte X => eventData.Byte1;

	public byte Y => eventData.Byte2;

	public byte Direction => eventData.Byte3;

	public word MapIndex => eventData.Word6;
}

public class DoorEvent(EventData eventData) : Event(eventData), IDoorEvent
{
	public byte LockpickReduction => eventData.Byte1;

	public TrapType TrapType => (TrapType)eventData.Byte2;

	public byte TrapDamage => eventData.Byte3;

	public word ItemIndex => eventData.Word6;
}

public class ShowPictureTextEvent(EventData eventData) : Event(eventData), IShowPictureTextEvent
{
	public byte Picture => eventData.Byte1;

	public byte TextIndex => eventData.Byte2;

	public ShowPictureTextTrigger Trigger => (ShowPictureTextTrigger)eventData.Byte3;

	public word SetWordBit => eventData.Word6;
}


public class ExecuteTrapEvent(EventData eventData) : Event(eventData), IExecuteTrapEvent
{
	public TrapType TrapType => (TrapType)eventData.Byte1;

	public byte Damage => eventData.Byte3;

	public byte TextIndex => eventData.Byte4;
}