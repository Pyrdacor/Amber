﻿using Amber.Common;
using Amber.Compression;

namespace Amber.Serialization;

public static class FileWriter
{
		static byte[] AlignData(byte[] data)
    {
        if (data.Length % 2 == 0)
            return data;

        var buffer = new byte[data.Length + 1];
        Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
        return buffer;
    }

    public static void Write(DataWriter writer, IFileContainer fileContainer,
        Action<int, int, int?>? compressionPrinter = null)
    {
        var fileType = fileContainer.Header.AsFileType();

        switch (fileType)
        {
            case FileType.JH:
                WriteJH(writer, AlignData(fileContainer.Files[1].ToArray()), (ushort)(fileContainer.Header & 0xffff), false);
                break;
            case FileType.LOB:
                WriteLob(writer, fileContainer.Files[1].ToArray());
                break;
            case FileType.VOL1:
                WriteVol1(writer, fileContainer.Files[1].ToArray());
                break;
            case FileType.AMBR:
            case FileType.AMNC:
            case FileType.AMNP:
            case FileType.AMPC:
                WriteContainer(writer, fileContainer.Files.ToDictionary(f => (uint)f.Key, f => f.Value.ToArray()), fileType, null,
                    compressionPrinter);
                break;
            case FileType.JHPlusAMBR:
            {
                var ambrWriter = new DataWriter();
                WriteContainer(ambrWriter, fileContainer.Files.ToDictionary(f => (uint)f.Key, f => f.Value.ToArray()), FileType.AMBR);
                WriteJH(writer, AlignData(ambrWriter.ToArray()), (ushort)(fileContainer.Header & 0xffff), false);
                break;
            }
            case FileType.JHPlusLOB:
                WriteJH(writer, fileContainer.Files[1].ToArray(), (ushort)(fileContainer.Header & 0xffff), true, false);
                break;
            default: // raw
                writer.Write(fileContainer.Files[1].ToArray());
                break;
        }
    }

    public static void WriteJH(DataWriter writer, byte[] fileData, ushort encryptKey, bool additionalLobCompression,
        bool noHeader = false)
    {
        if (additionalLobCompression)
        {
            var lobWriter = new DataWriter();
            WriteLob(lobWriter, fileData, (uint)FileType.LOB);
            fileData = AlignData(lobWriter.ToArray());
        }

        var encryptedData = JH.Crypt(fileData, encryptKey);

        if (!noHeader)
        {
            uint header = (uint)FileType.JH | (uint)((ushort)((uint)FileType.JH >> 16) ^ encryptKey);
            writer.Write(header);
        }

        writer.Write(encryptedData);
    }

    public static void WriteLob(DataWriter writer, byte[] fileData, Action<int, int, int?>? compressionPrinter = null)
    {
        WriteLob(writer, AlignData(fileData), (uint)FileType.LOB, compressionPrinter);
    }

    public static void WriteVol1(DataWriter writer, byte[] fileData, Action<int, int, int?>? compressionPrinter = null)
    {
        WriteLob(writer, AlignData(fileData), (uint)FileType.VOL1, compressionPrinter);
    }

    static void WriteLob(DataWriter writer, byte[] fileData, uint header, Action<int, int, int?>? compressionPrinter = null)
    {
        var compressedData = Lob.Compress(fileData);
        compressionPrinter?.Invoke(compressedData.Length, fileData.Length, null);

        if (fileData.Length % 2 == 1 || compressedData.Length % 2 == 1)
            throw new AmberException(ExceptionScope.Application, "Lob source or compressed data is not word-aligned.");

        writer.Write(header);
        writer.Write((uint)fileData.Length);
        writer.Write((uint)compressedData.Length);
        writer.Write(compressedData);
    }

    public static void WriteContainer(DataWriter writer, FileType fileType, params byte[][] filesData)
    {
        if (filesData == null)
            WriteContainer(writer, new Dictionary<uint, byte[]>(), fileType);
        else
            WriteContainer(writer, filesData.Select((f, i) => new { f, i }).ToDictionary(f => 1 + (uint)f.i, f => f.f), fileType);
    }

    public static void WriteContainer(DataWriter writer, FileType fileType,
        Action<int, int, int?>? compressionPrinter, params byte[][] filesData)
    {
        if (filesData == null)
            WriteContainer(writer, new Dictionary<uint, byte[]>(), fileType, null, compressionPrinter);
        else
            WriteContainer(writer, filesData.Select((f, i) => new { f, i }).ToDictionary(f => 1 + (uint)f.i, f => f.f), fileType, null,
                compressionPrinter);
    }

    public static void WriteContainer(DataWriter writer, Dictionary<uint, byte[]> filesData, FileType fileType,
        int? minimumFileCount = null, Action<int, int, int?>? compressionPrinter = null)
    {
        switch (fileType)
        {
            case FileType.JHPlusAMBR:
                throw new AmberException(ExceptionScope.Data, $"File type '{fileType}' is no valid container format. Use the Write method instead for this file type.");
            case FileType.AMNC:
            case FileType.AMNP:
            case FileType.AMBR:
            case FileType.AMPC:
            {
                if (filesData.Count >= 0xffff) // -1 cause JH uses the 1-based index as a word
                    throw new AmberException(ExceptionScope.Data, $"In a container file there can only be {0xffff-1} files at max.");

                if (filesData.ContainsKey(0))
                    throw new AmberException(ExceptionScope.Data, "The first file must have index 1 and not 0.");

                var writerWithoutHeader = new DataWriter();
                int totalFileNumber = (int)filesData.Keys.Max();
                if (minimumFileCount != null && minimumFileCount > totalFileNumber)
                    totalFileNumber = minimumFileCount.Value;
                var fileSizes = Enumerable.Repeat(0, totalFileNumber).ToList();
                var sortedFileEntries = new List<KeyValuePair<uint, byte[]>>(filesData);
                sortedFileEntries.Sort((a, b) => a.Key.CompareTo(b.Key));

                foreach (var file in sortedFileEntries)
                {
                    if (file.Value.Length == 0)
                    {
                        fileSizes[(int)file.Key - 1] = 0;
                        continue;
                    }

                    var fileData = file.Value;
                    int prevOffset = writerWithoutHeader.Position;

                    /*
                     * AMNC | Multiple file container (data uses [JH](JH.md) encoding). The C stands for "crypted". | 0x414d4e43 ('AMNC')
                       AMNP | Multiple file container (data uses [JH](JH.md) encoding and the files are often [LOB](LOB.md) encoded in addition). The P stands for "packed". | 0x414d4e50 ('AMNP')
                       AMBR | Multiple file container (no encryption). The R stands for "raw". | 0x414d4252 ('AMNR')
                       AMPC | Another multiple file container (only compressed, not JH encrypted) | 0x414d5043 ('AMPC')
                     */
                    if (fileType == FileType.AMNC)
                        WriteJH(writerWithoutHeader, fileData, (ushort)file.Key, false);
                    else if (fileType == FileType.AMBR)
                    {
                        writerWithoutHeader.Write(fileData);
                    }
                    else if (fileType == FileType.AMPC)
                    {
                        int position = writerWithoutHeader.Position;
                        WriteLob(writerWithoutHeader, fileData);
                        compressionPrinter?.Invoke(fileData.Length, writerWithoutHeader.Position - position, (int)file.Key);
                    }
                    else // AMNP
                    {
                        // this may be lob compressed if size is better
                        var lobWriter = new DataWriter();
                        WriteLob(lobWriter, fileData);
                        var data = lobWriter.Size - 4 < fileData.Length ? lobWriter.ToArray() : fileData;
                        bool lob = data != fileData;
                        compressionPrinter?.Invoke(fileData.Length, data.Length, (int)file.Key);
                        // this is always JH encoded
                        var jhWriter = new DataWriter();
                        byte[] header = lob ? data.Take(8).ToArray() : new byte[4] { 0, 0, 0, 0 };
                        byte[] encodedData = lob ? data.Skip(8).ToArray() : data;
                        WriteJH(jhWriter, encodedData, (ushort)file.Key, false, true);
                        writerWithoutHeader.Write(header);
                        writerWithoutHeader.Write(encodedData);
                    }

                    fileSizes[(int)file.Key - 1] = writerWithoutHeader.Position - prevOffset;
                }

                writer.Write((uint)fileType);

                writer.Write((ushort)totalFileNumber);
                fileSizes.ForEach(fileSize => writer.Write((uint)fileSize));

                writer.Write(writerWithoutHeader.ToArray());
            }
            break;
        default:
            throw new AmberException(ExceptionScope.Data, $"File type '{fileType}' is no container format.");
        }
    }
}
