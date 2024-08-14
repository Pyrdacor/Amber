using Amber.Assets.Common;
using Amber.Common;
using Amber.IO.Common.FileSystem;
using Amber.IO.FileFormats.Serialization;
using Amber.Serialization;
using Amberstar.Assets;
using Amiga.FileFormats.LHA;

namespace Amberstar.GameData.Atari;

public sealed class AtariAssetProvider : BaseAssetProvider
{
	readonly IReadOnlyFileSystem fileSystem;
	readonly Dictionary<AssetType, Dictionary<int, Asset>> assets = [];
	ProgramData? programData;
	const string ProgramFileName = "AMBRSTAR.68K";


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

		public Dictionary<int, IDataReader> PlacesData { get; }
		public Dictionary<int, string> PlaceNames { get; }
		public List<string> TextFragments { get; }
		public byte[] GlyphMappings { get; }
		public string Version { get; }
		public const int OpenBracket = 1580;
		public const int ClosingBracket = 1581;
		public const int ExclamationMark = 631;
		public const int CarriageReturn = 1576;
		public const int ParagraphMarker = 1577;
		public const int SingleQuote = 1300;
		public const int Comma = 166;
		public const int DoubleColon = 155;
		public const int SemiColon = 1302;
		public const int FullStop = 170;
		public const int QuestionMark = 743;

		public static bool IsEndPunctuation(int word)
		{
			return word == ExclamationMark || word == ClosingBracket ||
				   word == SingleQuote || word == Comma ||
				   word == DoubleColon || word == SemiColon ||
				   word == FullStop || word == QuestionMark;
		}
	}

	public AtariAssetProvider(IReadOnlyFileSystem fileSystem)
		: base(fileSystem)
	{
		this.fileSystem = fileSystem;
	}

	public override IAsset? GetAsset(AssetIdentifier identifier)
	{
		var fileName = FileNameByAssetType(identifier.Type);

		if (fileName != ProgramFileName)
			return base.GetAsset(identifier);

		if (programData == null)
		{
			var file = fileSystem.GetFile(fileName);

			if (file == null)
				return null;

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

			programData = new ProgramData(programDataReader);
		}

		bool hasAssetList = assets.TryGetValue(identifier.Type, out var assetList);

		if (hasAssetList && assetList!.TryGetValue(identifier.Index, out var asset))
			return asset;

		if (!hasAssetList)
		{
			Dictionary<int, Asset> CreateAssets<TSource>(Dictionary<int, TSource> source)
			{
				static IDataReader GetReader(TSource entry)
				{
					if (entry is string str)
						return new DataReader(DataReader.Encoding.GetBytes(str));
					else if (entry is IDataReader reader)
						return reader;
					else
						throw new NotSupportedException("Unsupported source type.");
				}

				return source.ToDictionary(e => e.Key, e => new Asset(new AssetIdentifier(identifier.Type, e.Key), GetReader(e.Value)));
			}

			assetList = identifier.Type switch
			{
				AssetType.Place => CreateAssets(programData.PlacesData),
				AssetType.PlaceName => CreateAssets(programData.PlaceNames),
				_ => throw new AmberException(ExceptionScope.Application, $"Unsupported asset type {identifier.Type} for Atari asset provider")
			};
			assets.Add(identifier.Type, assetList);
		}

		return assetList!.TryGetValue(identifier.Index, out asset) ? asset : null;
	}

	private Dictionary<int, Asset> LoadPlaceAssets()
	{
		// TODO
		throw new NotImplementedException();
	}

	protected override string FileNameByAssetType(AssetType assetType)
	{
		return assetType switch
		{
			AssetType.Place => ProgramFileName,
			AssetType.PlaceName => ProgramFileName,
			_ => base.FileNameByAssetType(assetType),
		};
	}
}
