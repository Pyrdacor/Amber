using Amber.Common;

namespace Amberstar.GameData;

/// <summary>
/// The tiles which are hidden behind tiles which block the sight on 2D maps (SHADOW.DAT in the original).
/// </summary>
public interface IShadowTable
{
	/// <summary>
	/// Maximum horizontal distance of a tile which casts a shadow.
	/// </summary>
	public const int MaxDistanceX = 8;

	/// <summary>
	/// Maximum vertical distance of a tile which casts a shadow.
	/// </summary>
	public const int MaxDistanceY = 6;

	/// <summary>
	/// Gets the positions relative to the party which are hidden by a tile at the given
	/// non-negative relative position. The positions must be mirrored for other quadrants.
	/// </summary>
	IReadOnlyList<Position> GetShadow(int distanceX, int distanceY);
}
