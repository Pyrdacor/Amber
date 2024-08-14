using Amber.Common;
using Amber.Serialization;

namespace Amber.IO.FileFormats.Serialization;

public class PrgFile
{
	internal PrgFile(byte[] textSegment, byte[] dataSegment, int bssSegmentSize, List<string> symbolNames)
	{
		TextSegment = textSegment;
		DataSegment = dataSegment;
		BssSegmentSize = bssSegmentSize;
		SymbolNames = symbolNames;
	}

	public byte[] TextSegment { get; }
	public byte[] DataSegment { get; }
	public int BssSegmentSize { get; }
	public List<string> SymbolNames { get; }
}

/// <summary>
/// PRG file reader.
/// 
/// Markus Fritze created a packer format. Tools like PFXPAK can create it.
/// They start with the header 0x60 0x1A.
/// </summary>
public static class PrgReader
{
	const int HeaderSize = 28;

	public static PrgFile Read(IDataReader reader)
	{
		int position = reader.Position;

		int RemainingSize() => reader.Size - reader.Position;

		void Throw(string message)
		{
			reader.Position = position;
			throw new AmberException(ExceptionScope.Data, message);
		}

		if (RemainingSize() < HeaderSize || reader.PeekWord() != 0x601A)
			Throw("Invalid PRG file");

		reader.Position += 2;

		uint segmentTextLength = reader.ReadDword();
		uint segmentDataLength = reader.ReadDword();
		uint segmentBssLength = reader.ReadDword();

		if (segmentTextLength > int.MaxValue || segmentDataLength > int.MaxValue || segmentBssLength > int.MaxValue)
			Throw("Invalid PRG file");

		uint symbolTableSize = reader.ReadDword();
		byte[] symbolTable = [];
		var symbolNames = new List<string>();

		// TODO: Symbol tables are not fully implemented yet.
		if (false && symbolTableSize != 0)
		{
			// TODO...
			if (symbolTableSize > int.MaxValue)
				Throw("Invalid symbol table size");

			// Symbol table starts behind header + TEXT segment + DATA segment
			reader.Position = HeaderSize + (int)segmentTextLength + (int)segmentDataLength;
			// 4-times the symbol table size of space.
			symbolTable = reader.ReadBytes((int)symbolTableSize);

			int sourceOffset = 0;
			int targetOffset = 0;
			int nextEntryOffset = 14;

			while (sourceOffset < symbolTableSize)
			{
				for (int i = 0; i < 14; i++)
					symbolTable[targetOffset++] = symbolTable[sourceOffset + i];

				nextEntryOffset = sourceOffset + 14;

				var symbolName = Encoding.ASCII.GetString(new ReadOnlySpan<byte>(symbolTable, sourceOffset, 8)).TrimEnd('\0');

				if (symbolTable.Length == 8)
				{
					if (symbolTable[targetOffset - 5] == 0x48) // Extended GST-Format?
					{
						sourceOffset = nextEntryOffset;
						nextEntryOffset += 14;

						// Copy max 14 chars of the extension
						symbolName += Encoding.ASCII.GetString(new ReadOnlySpan<byte>(symbolTable, sourceOffset, 14)).TrimEnd('\0');
					}
				}

				symbolNames.Add(symbolName);
				sourceOffset = nextEntryOffset;
			}
		}

		reader.Position += 4; // skip reserved long

		uint flags = reader.ReadDword(); // Bit0: 1 (TOS 1.4 Fast-Load)
		int relocInfo = reader.ReadWord(); // ?
		// TODO?

		return new PrgFile
		(
			reader.ReadBytes((int)segmentTextLength),
			reader.ReadBytes((int)segmentDataLength),
			(int)segmentBssLength,
			[] // TODO: maybe later add symbols but we only need the data segment most of the time anyway
		);
	}
}
