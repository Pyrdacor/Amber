using Amberstar.Game.Screens;

namespace Amberstar.Tests;

/// <summary>
/// The world map Lyramion consists of 8x8 maps with the 1-based indices 1 to 64.
/// </summary>
public sealed class WorldMapTests
{
	[Theory]
	[InlineData(1, 1, 0, 2)]
	[InlineData(1, 0, 1, 9)]
	[InlineData(1, 1, 1, 10)]
	[InlineData(8, 1, 0, 1)]
	[InlineData(8, 0, 1, 16)]
	[InlineData(1, -1, 0, 8)]
	[InlineData(1, 0, -1, 57)]
	[InlineData(57, 0, 1, 1)]
	[InlineData(64, 1, 1, 1)]
	public void NeighborMapIndices(int mapIndex, int offsetX, int offsetY, int expectedIndex)
	{
		Assert.Equal(expectedIndex, Map2DScreen.GetWorldMapIndex(mapIndex, offsetX, offsetY));
	}
}
