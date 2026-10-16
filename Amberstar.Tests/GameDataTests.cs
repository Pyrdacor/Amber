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
	public void AllMapEventsHaveAHandler(string folder)
	{
		var assetProvider = TestData.CreateAssetProvider(folder);

		foreach (var (index, map) in assetProvider.MapLoader.LoadAllMaps())
		{
			for (int i = 0; i < map.Events.Count; i++)
			{
				var exception = Record.Exception(() => Game.Events.Event.CreateEvent(map.Events[i], i + 1));
				Assert.True(exception == null, $"Event {i + 1} ({map.Events[i].Type}) of map {index} has no handler.");
			}
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
	public void ClassDataIsPlausible(string folder)
	{
		var assetProvider = TestData.CreateAssetProvider(folder);

		foreach (var @class in Enum.GetValues<Class>().Where(@class => @class >= Class.Warrior && @class <= Class.BlackWizzard))
		{
			var classData = assetProvider.ClassDataLoader.LoadClassData(@class);
			var experience = Enumerable.Range(0, IClassData.MaxLevel).Select(classData.GetExperienceForNextLevel).ToArray();

			Assert.True(experience.Zip(experience.Skip(1)).All(pair => pair.First < pair.Second), $"The experience of {@class} is not increasing.");
			Assert.All(Enum.GetValues<Skill>().Skip(1), skill => Assert.InRange(classData.GetSkillMaxValue(skill), 0, 100));
			Assert.True(classData.HitPointsPerLevel > 0);
		}

		Assert.Equal(30u, assetProvider.ClassDataLoader.LoadClassData(Class.Warrior).GetExperienceForNextLevel(0));
		Assert.Equal(SpellSchoolFlags.None, assetProvider.ClassDataLoader.LoadClassData(Class.Warrior).SpellSchools);
		Assert.Equal(SpellSchoolFlags.White, assetProvider.ClassDataLoader.LoadClassData(Class.WhiteWizzard).SpellSchools);
	}

	[Theory]
	[MemberData(nameof(TestData.DataFolders), MemberType = typeof(TestData))]
	public void SpellDataIsPlausible(string folder)
	{
		var assetProvider = TestData.CreateAssetProvider(folder);

		foreach (var school in new[] { SpellSchool.White, SpellSchool.Gray, SpellSchool.Black })
		{
			for (int spell = 1; spell <= ISpellData.SpellsPerSchool; spell++)
				Assert.InRange(assetProvider.SpellDataLoader.LoadSpellData(school, spell).SpellLearningPointCost, 0, 100);
		}

		// HEILUNG 1 can be cast everywhere and costs 1 spell learning point
		var healing = assetProvider.SpellDataLoader.LoadSpellData(SpellSchool.White, 1);
		Assert.Equal(0x1f, healing.LocationFlags);
		Assert.Equal(1, healing.SpellLearningPointCost);
	}

	[Theory]
	[MemberData(nameof(TestData.DataFolders), MemberType = typeof(TestData))]
	public void AllMerchantWaresLoad(string folder)
	{
		var assetProvider = TestData.CreateAssetProvider(folder);
		var keys = assetProvider.GetAssetKeys(AssetType.Merchant);

		Assert.NotEmpty(keys);

		foreach (var key in keys)
		{
			var wares = assetProvider.PlaceLoader.LoadMerchantWares(key);
			Assert.Equal(12, wares.Length);
			Assert.Contains(wares, ware => ware != null);
		}
	}

	[Theory]
	[MemberData(nameof(TestData.DataFolders), MemberType = typeof(TestData))]
	public void PartyMemberRoundtrip(string folder)
	{
		var assetProvider = TestData.CreateAssetProvider(folder);
		var partyMembers = assetProvider.PersonLoader.GetPartyMemberCopies();

		Assert.NotEmpty(partyMembers);

		foreach (var (index, partyMember) in partyMembers)
		{
			var reader = assetProvider.GetAsset(new AssetIdentifier(AssetType.Person, index))!.GetReader();
			reader.Position = 0;
			var original = reader.ReadBytes(reader.Size);
			var written = assetProvider.PersonLoader.WritePartyMember(partyMember);

			Assert.True(original.SequenceEqual(written), $"Party member {index} ({partyMember.Name}) differs at offset 0x{original.Zip(written).TakeWhile(pair => pair.First == pair.Second).Count():x}.");

			var copy = assetProvider.PersonLoader.ReadPartyMember(index, written);
			Assert.Equal(partyMember.Name, copy.Name);
			Assert.Equal(partyMember.TotalWeight, copy.TotalWeight);
		}
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
