using Amber.Common;
using Amber.IO.Common.Serialization;

namespace Amber.IO.FileFormats.Serialization;

public class PrgFile
{
	internal PrgFile(byte[] textSegment, byte[] dataSegment, int bssSegmentSize, List<uint> relocTable)
	{
		TextSegment = textSegment;
		DataSegment = dataSegment;
		BssSegmentSize = bssSegmentSize;
		RelocTable = relocTable;

    }

	public byte[] TextSegment { get; }
	public byte[] DataSegment { get; }
	public int BssSegmentSize { get; }
    public List<uint> RelocTable { get; }
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

    // See: https://freemint.github.io/tos.hyp/en/gemdos_programs.html
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
        uint symbolTableSize = reader.ReadDword();

        if (segmentTextLength > int.MaxValue || segmentDataLength > int.MaxValue || segmentBssLength > int.MaxValue || symbolTableSize > int.MaxValue)
			Throw("Invalid PRG file");

		reader.Position += 4; // skip reserved long

		uint flags = reader.ReadDword(); // Bit0: 1 (TOS 1.4 Fast-Load)
		int relocInfo = reader.ReadWord(); // 0 = reloc info exists
		
		var textSegmentData = reader.ReadBytes((int)segmentTextLength);
		var dataSegmentData = reader.ReadBytes((int)segmentDataLength);

		// skip symbol table
		reader.Position += (int)symbolTableSize;

        List<uint> relocTable = [];

        if (relocInfo == 0)
        {
            // The reloc info starts with a 32-bit integer giving the offset from text segment
            // start to the first reloc entry. After that, single bytes are used as offsets to
            // the next one. If 255 is not enough, a value of 1 is used (which normally is not
            // allowed). So if a 1 is given, it basically means 254 plus the next byte. You
            // can repeat 1-bytes for large gaps. A byte of 0 means the end of the reloc info.
            uint offset = reader.ReadDword();

            // Note: If there are no relocactions at all the first long would be zero.
            if (offset != 0)
            {
                uint GetAddress(uint offset)
                {
                    uint address = textSegmentData[offset++];
                    address <<= 8;
                    address |= textSegmentData[offset++];
                    address <<= 8;
                    address |= textSegmentData[offset++];
                    address <<= 8;
                    address |= textSegmentData[offset];

                    return address;
                }

                relocTable.Add(GetAddress(offset));

                while (true)
                {
                    uint next = reader.ReadByte();

                    if (next == 0)
                        break;

                    if (next == 1)
                        offset += 254;
                    else
                    {
                        offset += next;
                        relocTable.Add(GetAddress(offset));
                    }
                }
            }
        }

        return new PrgFile
		(
			textSegmentData,
			dataSegmentData,
			(int)segmentBssLength,
            relocTable
        );
	}
}
