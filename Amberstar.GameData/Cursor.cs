using Amber.Assets.Common;
using Amber.Common;

namespace Amberstar.GameData;

public enum CursorType
{
	Sword,
	ArrowUp2D,
	ArrowDown2D,
	ArrowRight2D,
	ArrowLeft2D,
	ArrowUpLeft2D,
	ArrowUpRight2D,
	ArrowDownRight2D,
	ArrowDownLeft2D,
	ArrowBackward3D,
	ArrowForward3D,
	ArrowRight3D,
	ArrowLeft3D,
	ArrowTurnRight3D,
	ArrowTurnLeft3D,
	Disk,
	Zzz,
	Eye,
	Mouth,
	Ear,
	FullTurnRight,
	LittleArrowUp,
	LittleArrowDown,
	FullTurnLeft,
	UserDefined, // empty
	Gold,
	Food,
	LastCursor = Food
}

public interface ICursor
{
	Position Hotspot { get; }
	IGraphic Graphic { get; }
}
