using System.ComponentModel;
using Amber.Assets.Common;
using Amber.Common;
using Amber.IO.Common.FileSystem;
using Amber.IO.FileFormats.Serialization;
using Amber.Serialization;
using Amberstar.GameData.Serialization;
using Amiga.FileFormats.LHA;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Amberstar.GameData.Legacy;

public enum LegacyPlatform
{
	Source, // published source data files
	Atari,
	Amiga,
	//Dos // TODO: unsupported for now
}

public enum EmbeddedDataOffset
{
	Version,
	GlyphMappings,
	RaceNames, // RACEDAT.DAT
	ClassNames, // CLASSDAT.DAT
	SpellSchoolNames,
	Graphics,
	TextConversionTab,
	Windows,
	Cursors,
	UITexts,
	Music
}

public class AssetProvider : IAssetProvider
{
	// The UI palette seems to be existent in multiple places inside the program data.
	// But we keep things simple.
	private static readonly IGraphic UIPalette = Legacy.PaletteLoader.LoadPalette(new DataReader(
	[
		0x00, 0x00,
		0x07, 0x50,
		0x03, 0x33,
		0x02, 0x22,
		0x01, 0x11,
		0x07, 0x42,
		0x06, 0x31,
		0x02, 0x00,
		0x05, 0x66,
		0x03, 0x45,
		0x07, 0x54,
		0x06, 0x43,
		0x05, 0x32,
		0x04, 0x21,
		0x03, 0x10,
		0x07, 0x65
	]));
    private static readonly IGraphic ItemPalette = Legacy.PaletteLoader.LoadPalette(new DataReader(
    [
        0x00, 0x00,
        0x06, 0x51,
        0x03, 0x33,
        0x02, 0x22,
        0x01, 0x11,
        0x05, 0x31,
        0x04, 0x21,
        0x01, 0x24,
        0x02, 0x36,
        0x06, 0x10,
        0x03, 0x10,
        0x05, 0x41,
        0x03, 0x40,
        0x04, 0x51,
        0x04, 0x44,
        0x07, 0x65
    ]));
    private static readonly IGraphic AutomapPalette = Legacy.PaletteLoader.LoadPalette(new DataReader(
    [
        0x00, 0x00,
        0x05, 0x00,
        0x03, 0x33,
        0x02, 0x22,
        0x01, 0x11,
        0x04, 0x44,
        0x05, 0x55,
        0x02, 0x10,
        0x03, 0x20,
        0x04, 0x31,
        0x05, 0x42,
        0x00, 0x12,
        0x01, 0x23,
        0x03, 0x51,
        0x02, 0x30,
        0x06, 0x66
    ]));

    static readonly Dictionary<LegacyPlatform, string> programFileNames = new()
	{
		{ LegacyPlatform.Atari, "AMBRSTAR.68K" },
		{ LegacyPlatform.Amiga, "AMBERDEV.UDO" },
	};

	static readonly Dictionary<LegacyPlatform, Func<IReadOnlyFile, ProgramData>> programFileLoaders = new()
	{
		{ LegacyPlatform.Atari, LoadAtariProgramData },
		{ LegacyPlatform.Amiga, LoadAmigaProgramData },
	};

	readonly IReadOnlyFileSystem fileSystem;
	readonly Dictionary<AssetType, Dictionary<int, Asset>> assets = [];
	readonly Dictionary<AssetType, IFileContainer> assetContainers = [];
	readonly Lazy<ProgramData> programData;
	readonly Lazy<ITextLoader> textLoader;
	readonly Lazy<IPlaceLoader> placeLoader;
	readonly Lazy<ILayoutLoader> layoutLoader;
	readonly Lazy<IUIGraphicLoader> uIGraphicLoader;
	readonly Lazy<IMapLoader> mapLoader;
	readonly Lazy<IPaletteLoader> paletteLoader;
	readonly Lazy<IGraphicLoader> graphicLoader;
	readonly Lazy<ITilesetLoader> tilesetLoader;
	readonly Lazy<IFontLoader> fontLoader;
	readonly Lazy<ISavegameLoader> savegameLoader;
	readonly Lazy<ILabDataLoader> labDataLoader;
	readonly Lazy<IChestLoader> chestLoader;
    readonly Lazy<ICursorLoader> cursorLoader;
    readonly Lazy<IMonsterLoader> monsterLoader;
    readonly Lazy<IPersonLoader> personLoader;
    readonly Lazy<IItemLoader> itemLoader;
    readonly Lazy<ISongLoader> songLoader;

    private ProgramData Data => programData.Value;
	public ITextLoader TextLoader => textLoader.Value;
	public IPlaceLoader PlaceLoader => placeLoader.Value;
	public ILayoutLoader LayoutLoader => layoutLoader.Value;
	public IUIGraphicLoader UIGraphicLoader => uIGraphicLoader.Value;
	public IMapLoader MapLoader => mapLoader.Value;
	public IPaletteLoader PaletteLoader => paletteLoader.Value;
	public IGraphicLoader GraphicLoader => graphicLoader.Value;
	public ITilesetLoader TilesetLoader => tilesetLoader.Value;
	public IFontLoader FontLoader => fontLoader.Value;
	public ISavegameLoader SavegameLoader => savegameLoader.Value;
	public ILabDataLoader LabDataLoader => labDataLoader.Value;
	public IChestLoader ChestLoader => chestLoader.Value;
    public ICursorLoader CursorLoader => cursorLoader.Value;
    public IMonsterLoader MonsterLoader => monsterLoader.Value;
    public IPersonLoader PersonLoader => personLoader.Value;
    public IItemLoader ItemLoader => itemLoader.Value;
    public ISongLoader SongLoader => songLoader.Value;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public AssetProvider(IReadOnlyFileSystem fileSystem)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	{
		this.fileSystem = fileSystem;

        foreach (var platform in Enum.GetValues<LegacyPlatform>().Skip(1))
        {
			var programFile = fileSystem.GetFile(programFileNames[platform]);

			if (programFile != null)
			{
				programData = new(() => programFileLoaders[platform](programFile));
				Platform = platform;
				break;
			}
        }

		if (Platform == LegacyPlatform.Source)
			programData = new(() => LoadProgramDataFromSource(fileSystem));

		var builtinPalettes = new Dictionary<BuiltinPalette, IGraphic>()
		{
			{ BuiltinPalette.UI, UIPalette },
            { BuiltinPalette.Item, ItemPalette },
            { BuiltinPalette.Automap, AutomapPalette },
        };

		textLoader = new(() => new TextLoader(this, Data.TextFragments));
		placeLoader = new(() => new PlaceLoader(this));
		layoutLoader = new(() => new LayoutLoader(this, Data.LayoutBlocks,
			Data.LayoutBottomCorners, Data.LayoutBottomCornerMasks, Data.PortraitArea));
		uIGraphicLoader = new(() => new UIGraphicLoader(this, Data.LayoutBlocks[77])); // layout block 77 is the empty item slot
		mapLoader = new(() => new MapLoader(this));		
		paletteLoader = new(() => new PaletteLoader(this, builtinPalettes));
		graphicLoader = new(() => new GraphicLoader(this));
		tilesetLoader = new(() => new TilesetLoader(this));
		fontLoader = new(() => new FontLoader(this));
		savegameLoader = new(() => new SavegameLoader(this));
		labDataLoader = new(() => new LabDataLoader(this));
		chestLoader = new(() => new ChestLoader(this));
        cursorLoader = new(() => new CursorLoader(this));
        monsterLoader = new(() => new MonsterLoader(this));
        personLoader = new(() => new PersonLoader(this, textLoader, GetAssetKeys(AssetType.Person)));
        itemLoader = new(() => new ItemLoader());
        songLoader = new(() => new SongLoader(this, Platform));
    }

	public LegacyPlatform Platform { get; } = LegacyPlatform.Source;

	private static ProgramData LoadAtariProgramData(IReadOnlyFile file)
	{
		using var fileReader = file.Stream.GetReader();

		var prgFile = PrgReader.Read(fileReader);
		IDataReader programDataReader;

		try
		{
			// Is it a LHA file?
			var stream = new MemoryStream(prgFile.DataSegment);
			var lha = LHAReader.LoadLHAFile(stream);
			var dataFile = lha.RootDirectory.GetFiles().FirstOrDefault();

			if (dataFile == null)
				throw new Exception();

			// Again a prg file?
			try
			{
				prgFile = PrgReader.Read(new DataReader(dataFile.Data));

				programDataReader = new DataReader(prgFile.DataSegment);
			}
			catch
			{
				// No? Use raw data.
				programDataReader = new DataReader(dataFile.Data);
			}
		}
		catch
		{
			// No? Use raw data.
			programDataReader = new DataReader(prgFile.DataSegment);
		}

        return new ProgramData(programDataReader, FindAtariOffset);
	}

	private static ProgramData LoadAmigaProgramData(IReadOnlyFile file)
	{
        using var fileReader = file.Stream.GetReader();

        // AMBERDEV.UDO is a lob compressed (GEMDOS) Prg file.
        // The FileReader will take care of the decompression.
        var fileContainer = new FileReader().ReadFile("AMBERDEV.UDO", fileReader);

		var reader = fileContainer.Files.Values.First();
        var prgFile = PrgReader.Read(reader);

        // In contrast to the Atari version, the Amiga version does not use
        // the data segment for the program data, but instead has it directly
        // in the text segment. We use the reloc table to find the offsets
		// of the embedded data files.
        var relocTable = prgFile.RelocTable;

        bool FindOffset(EmbeddedDataOffset embeddedDataFile, IDataReader dataReader)
		{
			return FindAmigaOffset(embeddedDataFile, dataReader, relocTable);
        }

        return new ProgramData(new DataReader(prgFile.TextSegment), FindOffset);
    }

	private static ProgramData LoadProgramDataFromSource(IReadOnlyFileSystem fileSystem)
	{
		// TODO: From single files like LAYOUT.ICN etc
		throw new NotImplementedException();
	}

	private Dictionary<int, Asset> GetProgramDataAssetList(AssetType type)
	{
		Dictionary<int, Asset> CreateAssets(Dictionary<int, IDataReader> source)
		{
			return source.ToDictionary(e => e.Key, e => new Asset(new(type, e.Key), e.Value));
		}

		return type switch
		{
			AssetType.Place => CreateAssets(Data.PlacesData),
			AssetType.PlaceName => CreateAssets(Data.PlaceNames),
			AssetType.SpellName => CreateAssets(Data.SpellNames),
			AssetType.SpellSchoolName => CreateAssets(Data.SpellSchoolNames),
			AssetType.ClassName => CreateAssets(Data.ClassNames),
			AssetType.RaceName => CreateAssets(Data.RaceNames),
			AssetType.AttributeName => CreateAssets(Data.AttributeNames),
			AssetType.SkillName => CreateAssets(Data.SkillNames),
			AssetType.CharInfoText => CreateAssets(Data.CharInfoTexts),
			AssetType.LanguageName => CreateAssets(Data.LanguageNames),
			AssetType.ConditionName => CreateAssets(Data.ConditionNames),
			AssetType.ItemTypeName => CreateAssets(Data.ItemTypeNames),
            AssetType.Layout => CreateAssets(Data.Layouts),
			AssetType.UIGraphic => CreateAssets(Data.UIGraphics),
			AssetType.Button => CreateAssets(Data.Buttons),
			AssetType.StatusIcon => CreateAssets(Data.StatusIcons),
			AssetType.ItemGraphic => CreateAssets(Data.ItemGraphics),
			AssetType.SkyGradient => CreateAssets(Data.SkyGradients),
			AssetType.Font => CreateAssets(Data.Fonts),
			AssetType.Window => CreateAssets(Data.Windows),
			AssetType.Cursor => CreateAssets(Data.Cursors),
			AssetType.UIText => CreateAssets(Data.UITexts),
			AssetType.Music => CreateAssets(Data.Songs),
			AssetType.Message => Data.MessageData.Select((data, index) => (data, index)).ToDictionary(x => 1 + x.index, x => new Asset(new(AssetType.Message, 1 + x.index), x.data)),
            _ => throw new AmberException(ExceptionScope.Application, $"Unsupported asset type {type} for legacy asset provider")
		};
	}

	private Asset? GetAssetFromProgramData(AssetIdentifier identifier)
	{
		bool hasAssetList = assets.TryGetValue(identifier.Type, out var assetList);

		if (hasAssetList && assetList!.TryGetValue(identifier.Index, out var asset))
			return asset;

		if (!hasAssetList)
		{
			assetList = GetProgramDataAssetList(identifier.Type);
			assets.Add(identifier.Type, assetList);
		}

		return assetList!.GetValueOrDefault(identifier.Index);
	}

	public ICollection<int> GetAssetKeys(AssetType type)
	{
		if (assetContainers.TryGetValue(type, out var container))
			return container.Files.Keys;

		var fileName = FileNameByAssetType(type);

		if (fileName.Length == 0)
			return [];

		if (fileName == programFileNames[Platform])
			return GetProgramDataAssetList(type).Keys;

		var file = fileSystem.GetFile(fileName);

		if (file == null)
			return [];

		container = new FileReader().ReadFile(fileName, file.Stream.GetReader());
		assetContainers.Add(type, container);

		return container.Files.Keys;
	}

	public virtual IAsset? GetAsset(AssetIdentifier identifier)
	{
		bool hasAssetList = assets.TryGetValue(identifier.Type, out var assetList);

		if (hasAssetList && assetList!.TryGetValue(identifier.Index, out var asset))
			return asset;

		if (!assetContainers.TryGetValue(identifier.Type, out var container))
		{
			var fileName = FileNameByAssetType(identifier.Type);

			if (fileName.Length == 0)
				return null;

			if (fileName == programFileNames[Platform])
				return GetAssetFromProgramData(identifier);

			var file = fileSystem.GetFile(fileName);

			if (file == null)
				return null;

			container = new FileReader().ReadFile(fileName, file.Stream.GetReader());
			assetContainers.Add(identifier.Type, container);
		}

		if (!hasAssetList)
		{
			assetList = [];
			assets.Add(identifier.Type, assetList);
		}

		if (!container.Files.TryGetValue(identifier.Index, out var reader))
			return null;

		asset = new Asset(identifier, reader);
		assetList!.Add(identifier.Index, asset);

		return asset;
	}

	protected virtual string FileNameByAssetType(AssetType assetType)
	{
		return assetType switch
		{
			AssetType.Map => "MAP_DATA.AMB",
			AssetType.MapText => "MAPTEXT.AMB",
			AssetType.ItemText => "CODETXT.AMB",
			AssetType.PuzzleText => "PUZZLE.TXT",
			AssetType.Palette => "COL_PALL.AMB",
			AssetType.Graphics80x80 => "PICS80.AMB",
			AssetType.Tileset => "ICON_DAT.AMB",
			AssetType.Savegame => "PARTYDAT.SAV",
			AssetType.LabData => "LAB_DATA.AMB",
			AssetType.LabBlock => "LABBLOCK.AMB",
			AssetType.Background => "BACKGRND.AMB",
            AssetType.Monster => "MON_DATA.AMB",
            AssetType.Person => "CHARDATA.AMB",
            AssetType.Chest => "CHESTDAT.AMB",
            _ => Platform == LegacyPlatform.Source ? "" : programFileNames[Platform],
		};
	}

	private static bool FindAndGotoText(IDataReader dataReader, int offset, string text)
	{
		return FindAndGotoByteSequence(dataReader, offset, DataReader.Encoding.GetBytes(text));
	}

	private static bool FindAndGotoByteSequence(IDataReader dataReader, int offset, params byte[] sequence)
	{
		if (sequence == null || sequence.Length == 0)
			return false;

		dataReader.Position = offset;

		while (dataReader.Position <= dataReader.Size - sequence.Length)
		{
			while (dataReader.Position < dataReader.Size)
			{
				if (dataReader.ReadByte() != sequence[0])
					continue;

				if (sequence.Length == 1)
				{
					dataReader.Position--;
					return true;
				}

				int secondBytePosition = dataReader.Position;

				for (int i = 1; i < sequence.Length; i++)
				{
					if (dataReader.ReadByte() != sequence[i])
					{
						dataReader.Position = secondBytePosition;
						break;
					}

					if (i == sequence.Length - 1)
					{
						dataReader.Position = secondBytePosition - 1;
						return true;
					}
				}
			}
		}

		return false;
	}

	private static bool FindAtariOffset(EmbeddedDataOffset embeddedDataFile, IDataReader dataReader)
	{
		switch (embeddedDataFile)
		{
			case EmbeddedDataOffset.Version:
				return FindAndGotoText(dataReader, 0x250, "Version");
			case EmbeddedDataOffset.GlyphMappings:
				return FindAndGotoByteSequence(dataReader, 0x700, 0x20, 0xff, 0xff, 0xff);
            case EmbeddedDataOffset.RaceNames:
				if (!FindAndGotoByteSequence(dataReader, 0x28000, 0xff, 0x00, 0x01, 0x00, 0x02))
					return false;
				dataReader.Position++;
				return true;
            case EmbeddedDataOffset.ClassNames:
				return FindAndGotoByteSequence(dataReader, 0x2a000, 0x00, 0x13, 0x00, 0x14, 0x00, 0x15);
			case EmbeddedDataOffset.SpellSchoolNames:
				return FindAndGotoByteSequence(dataReader, 0x2a000, 0x00, 0x38, 0x00, 0x56, 0x00, 0x71);
			case EmbeddedDataOffset.Graphics:
				if (!FindAndGotoText(dataReader, 0x12000, "Illegal window handle"))
					return false;
				while (dataReader.PeekWord() != 0)
					dataReader.Position += 2;
				if (dataReader.PeekDword() == 0x0000_0080)
					dataReader.Position++;
				return dataReader.PeekDword() == 0x0000_8000;
			case EmbeddedDataOffset.TextConversionTab:
                if (!FindAndGotoByteSequence(dataReader, dataReader.Size - 0x4000, 0x01, 0x3b, 0x00, 0x00, 0x00, 0xc2))
					return false;
				dataReader.Position += 6;
				return true;
            case EmbeddedDataOffset.Windows:
				if (!FindAndGotoByteSequence(dataReader, 0x14000, 0x00, 0x0c, 0x00, 0x1d, 0x00, 0x1d))
					return false;
				dataReader.Position -= 0x86;
				return true;
			case EmbeddedDataOffset.Cursors:
				if (!FindAndGotoByteSequence(dataReader, 0x35000, 0x70, 0x00, 0x78, 0x00, 0x5c))
					return false;
				dataReader.Position -= 6;
				return true;
            case EmbeddedDataOffset.UITexts:
                if (!FindAndGotoByteSequence(dataReader, 0x10000, 0x23, 0x21, 0x12, 0x14))
                    return false;
                if (!FindAndGotoByteSequence(dataReader, dataReader.Position + 4 + 30, 0x07, 0x65)) // Two palettes are there, search into end of first
                    return false;
				return FindAndGotoByteSequence(dataReader, dataReader.Position + 32, 0x01, 0x08); // Set ink 8 (01 08) is start of the texts
			case EmbeddedDataOffset.Music:
				return FindAndGotoText(dataReader, 0x2a000, "COSO");
            default:
				return false;
		}
	}

	private static bool FindAmigaOffset(EmbeddedDataOffset embeddedDataFile, IDataReader dataReader, List<uint> relocTable)
	{
		const int VersionRelocIndex = 0x000001ed;
        const int GlyphMappingRelocIndex = 0x000010bb;
        const int RaceNameRelocIndex = 0x00001697;
        const int ClassNameRelocIndex = 0x0000169c;
        const int SpellSchoolNameRelocIndex = 0x00001d91;
        const int GraphicRelocIndex = 0x00000a15;
        const int PAreaRelocIndex = 0x00000aa9;
        const int WindowRelocIndex = 0x000028b4;
        const int CursorRelocIndex = 0x00002a13;
		const int UITextRelocIndex = 0x00001107;
        const int MusicRelocIndex = 0x000001c8;

        switch (embeddedDataFile)
		{
            case EmbeddedDataOffset.Version:
				return FindAndGotoText(dataReader, (int)relocTable[VersionRelocIndex], "Version");
            case EmbeddedDataOffset.GlyphMappings:
                dataReader.Position = (int)relocTable[GlyphMappingRelocIndex] + 2; // skip 0d 00
                return true;
            case EmbeddedDataOffset.RaceNames:
                dataReader.Position = (int)relocTable[RaceNameRelocIndex];
                return true;
            case EmbeddedDataOffset.ClassNames:
                dataReader.Position = (int)relocTable[ClassNameRelocIndex];
                return true;
            case EmbeddedDataOffset.SpellSchoolNames:
                dataReader.Position = (int)relocTable[SpellSchoolNameRelocIndex];
                return true;
            case EmbeddedDataOffset.Graphics:
                dataReader.Position = (int)relocTable[GraphicRelocIndex];
                return true;
            case EmbeddedDataOffset.TextConversionTab:
				// Follows directly after the PArea which has 8 bytes.
                dataReader.Position = (int)relocTable[PAreaRelocIndex] + 8;
                return true;
            case EmbeddedDataOffset.Windows:
                dataReader.Position = (int)relocTable[WindowRelocIndex];
                return true;
            case EmbeddedDataOffset.Cursors:
				dataReader.Position = (int)relocTable[CursorRelocIndex];
				return true;
            case EmbeddedDataOffset.UITexts:
                dataReader.Position = (int)relocTable[UITextRelocIndex];
                return true;
            case EmbeddedDataOffset.Music:
                dataReader.Position = (int)relocTable[MusicRelocIndex];
                return true;
            default:
                return false;
        }

        // TODO: Remove later
        // This can be used to search for offsets in the Amiga exe
        // Just comment out the switch above and uncomment this code.
        /*int n = 0;

		foreach (var addr in relocTable)
		{
			if (addr > dataReader.Size - 4)
			{
				n++;
				continue;
			}

            dataReader.Position = (int)addr;

			// Prepare the byte sequence (char literals or hex values) to search for.
			// This is just an example for the version string, adjust as needed.
            if (dataReader.ReadByte() == '[' && 
				dataReader.ReadByte() == 0x20 && 
				dataReader.ReadByte() == 'A' &&
                dataReader.ReadByte() == 'M' &&
                dataReader.ReadByte() == 'B' &&
                dataReader.ReadByte() == 'E')
			{
				// n gives you the reloc index which is used to reference the data.
				// relocTable[n] gives you the actual offset in the text segment.
				Console.WriteLine(n);
            }

			n++;
        }
		
        return false;*/
    }

    private static bool FindSourceOffset(EmbeddedDataOffset embeddedDataFile, IDataReader dataReader)
	{
		return true; // TODO
	}
}
