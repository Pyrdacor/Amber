using Amberstar.GameData;

namespace Amberstar.Game.Events
{
	internal interface ITeleportEvent
	{
		byte X { get; }

		byte Y { get; }

		Direction Direction { get; }

		ushort MapIndex { get; }
	}
}
