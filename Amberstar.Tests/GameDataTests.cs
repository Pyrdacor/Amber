using Amber.Common;
using Amber.Serialization;
using Amberstar.GameData;

namespace Amberstar.Tests;

/// <summary>
/// Loads the original game data of all versions in the repository.
/// </summary>
public sealed class GameDataTests
{
	[Theory]
	[MemberData(nameof(TestData.DataFolders), MemberType = typeof(TestData))]
	public void AllMapsLoad(string folder)
	{
		var assetProvider = TestData.CreateAssetProvider(folder);
		var maps = assetProvider.MapLoader.LoadAllMaps();

		Assert.NotEmpty(maps);

		foreach (var (index, map) in maps)
		{
			Assert.Equal(index, map.Index);
			Assert.True(map.Width > 0 && map.Height > 0, $"Map {index} has no size.");

			int tileCount = map switch
			{
				IMap2D map2D => map2D.Tiles.Length,
				IMap3D map3D => map3D.Tiles.Length,
				_ => throw new InvalidDataException($"Map {index} is neither 2D nor 3D.")
			};

			Assert.Equal(map.Width * map.Height, tileCount);
		}
	}

	[Theory]
	[MemberData(nameof(TestData.DataFolders), MemberType = typeof(TestData))]
	public void AllMapEventsAreKnownAndPlacedEventsExist(string folder)
	{
		var assetProvider = TestData.CreateAssetProvider(folder);

		foreach (var (index, map) in assetProvider.MapLoader.LoadAllMaps())
		{
			Assert.All(map.Events, @event => Assert.InRange(@event.Type, EventType.MapExit, EventType.Outro));

			var tileEvents = map switch
			{
				IMap2D map2D => map2D.Tiles.Select(tile => (int)tile.Event),
				IMap3D map3D => map3D.Tiles.Select(tile => (int)tile.Event),
				_ => []
			};

			foreach (int eventIndex in tileEvents.Where(e => e != 0).Distinct())
				Assert.True(eventIndex <= map.Events.Count, $"Map {index} references the missing event {eventIndex}.");
		}
	}

	[Theory]
	[MemberData(nameof(TestData.DataFolders), MemberType = typeof(TestData))]
	public void AllMapTextsLoad(string folder)
	{
		var assetProvider = TestData.CreateAssetProvider(folder);

		foreach (var index in assetProvider.MapLoader.LoadAllMaps().Keys)
		{
			var text = assetProvider.TextLoader.LoadText(new AssetIdentifier(AssetType.MapText, index));

			for (int i = 0; i < text.TextBlockCount; i++)
				Assert.NotNull(text.GetTextBlock(i).GetString());
		}
	}

	[Theory]
	[MemberData(nameof(TestData.DataFolders), MemberType = typeof(TestData))]
	public void AllCharactersAndChestsLoad(string folder)
	{
		var assetProvider = TestData.CreateAssetProvider(folder);

		Assert.NotEmpty(assetProvider.PersonLoader.LoadAllPersons());
		Assert.NotEmpty(assetProvider.MonsterLoader.LoadAllMonsters());
		Assert.NotEmpty(assetProvider.ChestLoader.LoadAllChests());
		Assert.NotEmpty(assetProvider.PersonLoader.GetPartyMemberCopies());
	}

	[Theory]
	[MemberData(nameof(TestData.DataFolders), MemberType = typeof(TestData))]
	public void InitialSavegameRoundtrip(string folder)
	{
		var assetProvider = TestData.CreateAssetProvider(folder);
		var savegame = assetProvider.SavegameLoader.LoadSavegame();

		foreach (bool encrypt in new[] { false, true })
		{
			var writer = new DataWriter();
			savegame.Write(writer, encrypt);
			var data = writer.ToArray();

			var copy = assetProvider.SavegameLoader.Create();
			copy.Read(new DataReader(data));

			var copyWriter = new DataWriter();
			copy.Write(copyWriter, encrypt);

			Assert.Equal(data, copyWriter.ToArray());
			Assert.Equal(savegame.MapIndex, copy.MapIndex);
			Assert.Equal(savegame.PartyX, copy.PartyX);
			Assert.Equal(savegame.PartyY, copy.PartyY);
			Assert.Equal(savegame.EventBits, copy.EventBits);
		}
	}
}
