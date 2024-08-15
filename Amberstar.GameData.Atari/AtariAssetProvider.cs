using Amber.Assets.Common;
using Amber.Common;
using Amber.IO.Common.FileSystem;
using Amber.IO.FileFormats.Serialization;
using Amber.Serialization;
using Amberstar.Assets;
using Amberstar.GameData.Serialization;
using Amiga.FileFormats.LHA;

namespace Amberstar.GameData.Atari;

public sealed class AtariAssetProvider : BaseAssetProvider
{
	const string ProgramFileName = "AMBRSTAR.68K";
	readonly Lazy<ProgramData> programData;
	readonly IReadOnlyFileSystem fileSystem;
	readonly Dictionary<AssetType, Dictionary<int, Asset>> assets = [];
	readonly Lazy<ITextLoader> textLoader;
	readonly Lazy<IPlaceLoader> placeLoader;

	private ProgramData Data => programData.Value;
	public ITextLoader TextLoader => textLoader.Value;
	public IPlaceLoader PlaceLoader => placeLoader.Value;

	private class ProgramData
	{
		public ProgramData(IDataReader dataReader)
		{
			#region Read Version
			int offset = 0x250;

			if (!FindAndGotoText(dataReader, offset, "Version"))
				throw new AmberException(ExceptionScope.Application, "Could not find the version string in the program file.");

			Version = ReadString(dataReader, 0x0D);
			#endregion
			#region Read Glyph Mappings
			offset = 0x700;

			if (!FindAndGotoByteSequence(dataReader, offset, 0x20, 0xff, 0xff, 0xff))
				throw new AmberException(ExceptionScope.Application, "Could not find the version string in the program file.");

			GlyphMappings = dataReader.ReadBytes(224); // 256 - 32 (control chars)
			#endregion
			#region Read Text Fragments
			int textCount = dataReader.ReadWord();
			int unknownCount = dataReader.ReadByte();
			dataReader.Position += unknownCount - 1;
			TextFragments = new List<string>(textCount)
			{
				"" // Index 0 is just an empty string
			};

			for (int i = 1; i < textCount; i++)
			{
				int length = dataReader.ReadByte();

				TextFragments.Add(DataReader.Encoding.GetString(dataReader.ReadBytes(length - 1)));
			}

			if (dataReader.ReadByte() != 0)
				throw new AmberException(ExceptionScope.Application, "Invalid text fragment terminator.");

			#endregion
			#region Read all kind of names
			offset = 0x2D000;

			// TODO: hopefully we have something better later
			if (!FindAndGotoByteSequence(dataReader, offset, 0x00, 0x13, 0x00, 0x14, 0x00, 0x15))
				throw new AmberException(ExceptionScope.Application, "Could not find the class names in the program file.");

			ClassNames = [];
			SkillNames = [];
			CharInfoTexts = [];
			SpellSchoolNames = [];
			SpellNames = [];

			for (int i = 0; i < 11; i++)
				ClassNames.Add(i, new DataReader(dataReader.ReadBytes(2))); // one word per class name

			for (int i = 0; i < 10; i++)
				SkillNames.Add(i, new DataReader(dataReader.ReadBytes(2))); // one word per skill name

			for (int i = 0; i < 5; i++)
				CharInfoTexts.Add(i, new DataReader(dataReader.ReadBytes(2))); // one word per char info text

			dataReader.Position += 12; // TODO: unknown data

			for (int i = 0; i < 7; i++)
				RaceNames.Add(i, new DataReader(dataReader.ReadBytes(2))); // one word per race name

			dataReader.Position += 2; // either an additional empty race name or an empty string for condition "none"

			// Note: First the physical conditions, then the mental conditions.
			// Note: There are only 5 mental conditions and I guess overloaded is not shown in the UI as a text.
			//       So the last 3 mental conditions are again "dead", "ashes" and "dust" (most likely copied over
			//       from the physical conditions). And I guess they are not used/shown at all.
			for (int i = 1; i <= 16; i++)
				ConditionNames.Add(i, new DataReader(dataReader.ReadBytes(2))); // one word per condition name

			for (int i = 0; i < 19; i++)
				ItemTypeNames.Add(i, new DataReader(dataReader.ReadBytes(2))); // one word per item type name

			// TODO: hopefully we have something better later
			offset = dataReader.Position;
			if (!FindAndGotoByteSequence(dataReader, offset, 0x00, 0x38, 0x00, 0x56, 0x00, 0x71))
				throw new AmberException(ExceptionScope.Application, "Could not find the spell names in the program file.");

			for (int i = 1; i <= 7; i++)
				SpellSchoolNames.Add(i, new DataReader(dataReader.ReadBytes(2))); // one word per spell school name

			for (int i = 1; i <= 7 * 30; i++)
				SpellNames.Add(i, new DataReader(dataReader.ReadBytes(2))); // one word per spell name

			#endregion

			#region Read Layouts
			offset = 0x18000;
			if (!FindAndGotoByteSequence(dataReader, offset, 0x55, 0x55, 0x00, 0x00, 0xA6, 0x49, 0xBE, 0x79))
				throw new AmberException(ExceptionScope.Application, "Could not find the layouts in the program file.");

			dataReader.Position -= 0x7c;

			if (dataReader.PeekWord() != 0xaaaa)
				throw new AmberException(ExceptionScope.Application, "Could not find the layouts in the program file.");

			// There are 11 layouts
			for (int i = 1; i <= 11; i++)
				// A layout definition consists of 220 bytes (20 blocks per row, 11 rows)
				// - First block line has 4 pixels offset (first 4 pixels are not used, so 16x12 pixel blocks)
				// - Last block line has 9 pixels less height (last 9 pixels are not used, so 16x7 pixel blocks)
				// - The 9 block lines in-between are rendered as full 16x16 blocks
				Layouts.Add(i, new DataReader(dataReader.ReadBytes(220)));

			#endregion
			// TODO: places
		}

		private static string ReadString(IDataReader dataReader, byte endByte = 0)
		{
			var bytes = ReadUntilByte(dataReader, endByte);
			return DataReader.Encoding.GetString(bytes);
		}

		private static byte[] ReadUntilByte(IDataReader dataReader, byte endByte)
		{
			var buffer = new List<byte>();

			while (dataReader.Position < dataReader.Size)
			{
				var b = dataReader.ReadByte();

				if (b == endByte)
					break;

				buffer.Add(b);
			}

			return buffer.ToArray();
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

		public Dictionary<int, IDataReader> Layouts { get; } = [];
		public Dictionary<int, IDataReader> PlacesData { get; } = [];
		public Dictionary<int, IDataReader> PlaceNames { get; } = [];
		public Dictionary<int, IDataReader> SpellSchoolNames { get; } = [];
		public Dictionary<int, IDataReader> SpellNames { get; } = [];
		public Dictionary<int, IDataReader> ClassNames { get; } = [];
		public Dictionary<int, IDataReader> SkillNames { get; } = [];
		public Dictionary<int, IDataReader> CharInfoTexts { get; } = [];
		public Dictionary<int, IDataReader> RaceNames { get; } = [];
		public Dictionary<int, IDataReader> ConditionNames { get; } = [];
		public Dictionary<int, IDataReader> ItemTypeNames { get; } = [];		
		public List<string> TextFragments { get; } = [];
		public byte[] GlyphMappings { get; } = [];
		public string Version { get; } = string.Empty;
	}

	public AtariAssetProvider(IReadOnlyFileSystem fileSystem)
		: base(fileSystem)
	{
		this.fileSystem = fileSystem;
		programData = new(() => LoadProgramData(ProgramFileName));
		textLoader = new(() => new TextLoader(this, Data.TextFragments));
		placeLoader = new(() => new PlaceLoader(this));
	}

	public override IAsset? GetAsset(AssetIdentifier identifier)
	{
		var fileName = FileNameByAssetType(identifier.Type);

		if (fileName != ProgramFileName)
			return base.GetAsset(identifier);

		bool hasAssetList = assets.TryGetValue(identifier.Type, out var assetList);

		if (hasAssetList && assetList!.TryGetValue(identifier.Index, out var asset))
			return asset;

		if (!hasAssetList)
		{
			Dictionary<int, Asset> CreateAssets(Dictionary<int, IDataReader> source)
			{
				return source.ToDictionary(e => e.Key, e => new Asset(new AssetIdentifier(identifier.Type, e.Key), e.Value));
			}

			assetList = identifier.Type switch
			{
				AssetType.Place => CreateAssets(Data.PlacesData),
				AssetType.PlaceName => CreateAssets(Data.PlaceNames),
				AssetType.SpellName => CreateAssets(Data.SpellNames),
				AssetType.SpellSchoolName => CreateAssets(Data.SpellSchoolNames),
				AssetType.ClassName => CreateAssets(Data.ClassNames),
				AssetType.SkillName => CreateAssets(Data.SkillNames),
				AssetType.CharInfoText => CreateAssets(Data.CharInfoTexts),
				AssetType.RaceName => CreateAssets(Data.RaceNames),
				AssetType.ConditionName => CreateAssets(Data.ConditionNames),
				AssetType.ItemTypeName => CreateAssets(Data.ItemTypeNames),
				_ => throw new AmberException(ExceptionScope.Application, $"Unsupported asset type {identifier.Type} for Atari asset provider")
			};

			assets.Add(identifier.Type, assetList);
		}

		return assetList!.GetValueOrDefault(identifier.Index);
	}

	private ProgramData LoadProgramData(string fileName)
	{
		var file = fileSystem.GetFile(fileName);

		if (file == null)
			throw new AmberException(ExceptionScope.Data, $"File {fileName} not found.");

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

				// TODO: REMOVE
				File.WriteAllBytes(@"D:\Projects\Amber\German\AmberfilesST\ExtractedDataSegment.bin", prgFile.DataSegment);

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

		return new ProgramData(programDataReader);
	}

	protected override string FileNameByAssetType(AssetType assetType)
	{
		return assetType switch
		{
			AssetType.Place => ProgramFileName,
			AssetType.PlaceName => ProgramFileName,
			AssetType.SpellName => ProgramFileName,
			AssetType.SpellSchoolName => ProgramFileName,
			AssetType.ClassName => ProgramFileName,
			AssetType.SkillName => ProgramFileName,
			AssetType.CharInfoText => ProgramFileName,
			AssetType.RaceName => ProgramFileName,
			AssetType.ConditionName => ProgramFileName,
			AssetType.ItemTypeName => ProgramFileName,
			_ => base.FileNameByAssetType(assetType),
		};
	}
}
