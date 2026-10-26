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
	public void ShadowTableLoads(string folder)
	{
		var shadowTable = TestData.CreateAssetProvider(folder).ShadowTable;

		// A tile diagonal to the party hides the tiles behind it on the diagonal.
		Assert.Equal([new(2, 2), new(3, 3), new(4, 4), new(5, 5), new(6, 6), new(7, 7), new(8, 8)], shadowTable.GetShadow(1, 1));
		Assert.Equal(98, shadowTable.GetShadow(1, 0).Count);
		Assert.Empty(shadowTable.GetShadow(0, 0));
	}

	[Theory]
	[MemberData(nameof(TestData.DataFolders), MemberType = typeof(TestData))]
	public void OutroDataLoads(string folder)
	{
		var assetProvider = TestData.CreateAssetProvider(folder);
		var outro = assetProvider.OutroData;

		Assert.NotNull(outro);
		Assert.Equal((320, 200), (outro.ThroneRoom.Width, outro.ThroneRoom.Height));
		Assert.Equal((320, 200), (outro.Balcony.Width, outro.Balcony.Height));
		Assert.Equal(7, outro.Chancellor.Count);
		Assert.Equal([94, 104, 69], outro.PartyFigures.Select(figure => figure.Height));
		Assert.Equal(6, outro.PartyFigurePositions.Count);
		Assert.Equal(12, outro.Dragon.Count);
		Assert.Equal(112, outro.Dragon[0].Width);
		Assert.Equal(8, outro.Eagle.Count);
		Assert.Equal(3, outro.BalconyPeople.Count);
		Assert.Equal(new Position(176, 102), outro.BalconyPeoplePositions[1]);
		Assert.Equal(0xff, outro.BalconyScript[^1]);
		// the chancellor's speech and the texts on the balcony
		Assert.Equal(3, outro.ChancellorTexts.Count);
		Assert.Equal(3, outro.BalconyTexts.Count);
		Assert.Contains("AMBERMOON", outro.BalconyTexts[2]);
		Assert.NotNull(outro.GetGlyph('A'));
		Assert.NotNull(outro.Music);
	}

	[Theory]
	[MemberData(nameof(TestData.DataFolders), MemberType = typeof(TestData))]
	public void CombatDataLoads(string folder)
	{
		var assetProvider = TestData.CreateAssetProvider(folder);

		// group 4: two giant rats in the front row (columns 1 and 4), one in the middle and a third monster type in the back
		var group = assetProvider.MonsterGroupLoader.LoadMonsterGroup(4);
		Assert.Equal(new MonsterGroupRow(6, 0x12), group.Rows[0]);
		Assert.True(group.Rows[0]!.HasColumn(1));
		Assert.True(group.Rows[0]!.HasColumn(4));
		Assert.False(group.Rows[0]!.HasColumn(0));
		Assert.Equal(7, group.Rows[2]!.MonsterIndex);
		Assert.Null(assetProvider.MonsterGroupLoader.LoadMonsterGroup(1).Rows[1]);

		var giantRat = assetProvider.MonsterLoader.LoadMonster(6);
		Assert.Equal(15, giantRat.DefeatExperience);
		Assert.Equal(50, giantRat.Morale);

		// fire beam, hurricane
		Assert.Equal((1, 2), (assetProvider.CombatData.GetSpellDamage(1).Min, assetProvider.CombatData.GetSpellDamage(1).Max));
		Assert.Equal((128, 256), (assetProvider.CombatData.GetSpellDamage(20).Min, assetProvider.CombatData.GetSpellDamage(20).Max));
		Assert.Equal(MonsterElementalFlags.FireImmune, assetProvider.CombatData.GetElementImmunity(0));
		Assert.Equal(MonsterElementalFlags.WaterImmune, assetProvider.CombatData.GetElementImmunity(1));

		// graphics
		var background = assetProvider.GraphicLoader.LoadCombatBackground(1);
		Assert.Equal((176, 112), (background.Width, background.Height));
		var ratGraphic = assetProvider.GraphicLoader.LoadMonsterGraphic(1);
		Assert.Equal(14, ratGraphic.Length);
		Assert.Equal([3, 3, 8], assetProvider.CombatData.GetMonsterFrameCounts(1));
		Assert.Equal((16, 16), (ratGraphic[0].Width, ratGraphic[0].Height));
		Assert.Equal(new MonsterGraphicPosition(54, 70, 0), assetProvider.CombatData.GetMonsterPosition(0, 0));
		Assert.Equal(new MonsterGraphicPosition(136, 110, 2), assetProvider.CombatData.GetMonsterPosition(5, 2));
		Assert.Equal(93, assetProvider.GraphicLoader.LoadTacticalIcons().Length);
		Assert.NotEqual(assetProvider.CombatData.GetPalette(1).GetData(), assetProvider.CombatData.GetPalette(2).GetData());

		// animations: 15 spell animations with 6 icons, 2 particle paths and the digits of the damage numbers
		Assert.Equal(90, assetProvider.GraphicLoader.LoadSpellAnimationIcons().Length);
		Assert.Equal(2, assetProvider.CombatData.SpellAnimationPathCount);
		Assert.Equal(new SpellParticle(1, 3, 1), assetProvider.CombatData.GetSpellParticles(0, 3)[1]);
		Assert.Equal(new SpellParticle(15, 4, 5), assetProvider.CombatData.GetSpellParticles(1, 16)[0]);
		var one = assetProvider.CombatData.GetDamageDigit(1).GetData();
		// .### in the 2nd line of the digit 1
		Assert.Equal([0, ICombatData.DamageDigitColorIndex, ICombatData.DamageDigitColorIndex, ICombatData.DamageDigitColorIndex], one[4..8]);

		// 19 songs use the 17 music modules (Play_song in the original)
		Assert.Equal(19, assetProvider.SongLoader.SongCount);
		Assert.Equal(7, assetProvider.SongLoader.GetMusicIndex(6));
		Assert.Equal(14, assetProvider.SongLoader.GetMusicIndex(16));
		Assert.Equal(17, assetProvider.SongLoader.GetMusicIndex(19));
		Assert.NotNull(assetProvider.SongLoader.LoadSong(17, 0));

		// the combat messages are the last message file
		Assert.NotEmpty(assetProvider.TextLoader.LoadText(new(Amber.Common.AssetType.Message, (int)Message.GoesBlind)).GetString());
	}

	[Theory]
	[MemberData(nameof(TestData.DataFolders), MemberType = typeof(TestData))]
	public void AltarPuzzleLoads(string folder)
	{
		var assetProvider = TestData.CreateAssetProvider(folder);
		var puzzle = assetProvider.AltarPuzzle;

		Assert.NotNull(puzzle);
		// Knowledge (Mork), the ally (Bralkur), the master of the sea (Sobek), ...
		Assert.Equal(["MORK", "BRALKUR", "SOBEK", "GALA", "SANSRI", "NUT", "LORD TARBOS", "GEB", "BALA", "HARACHTE", "MARMION", "TALMIT"],
			puzzle.PieceOrder.Select(piece => puzzle.PieceNames[piece]));
		Assert.Equal(Enumerable.Range(4, IAltarPuzzle.PieceCount), puzzle.StepTextIndices);
		Assert.Equal(Enumerable.Range(0, IAltarPuzzle.PieceCount), puzzle.PieceSigns.Order());
		Assert.Equal(new Position(81, 1), puzzle.PiecePositions[0]);

		var graphics = assetProvider.GraphicLoader.LoadPuzzleGraphics();

		Assert.Equal((int)PuzzleGraphic.Count, graphics.Length);
		Assert.Equal(176, graphics[(int)PuzzleGraphic.Background].Width);
		Assert.Equal(96, graphics[(int)PuzzleGraphic.Center].Height);
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
