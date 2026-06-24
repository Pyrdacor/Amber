using System.Text;
using Amber.IO.Common.Serialization;
using Amber.IO.FileFormats.Serialization;

namespace AmberIsland.GameData;

public sealed class FileContainer : IDisposable
{
    [Flags]
    private enum FileContainerFlags : byte
    {
        None = 0,
        StoreFileIndices = 1 << 0
    }

    private const string Magic = "AIFC";
    private const byte SupportedFileVersion = 0;

    private readonly struct FileLocation(uint offset, uint size)
    {
        public readonly uint Offset = offset;
        public readonly uint Size = size;
    }

    private Stream? stream = null;
    private readonly Dictionary<uint, FileLocation> files = [];

    public static FileContainer Read(Stream stream)
    {
        Span<byte> nameBuffer = stackalloc byte[4];

        stream.ReadExactly(nameBuffer);

        if (Encoding.ASCII.GetString(nameBuffer) != Magic)
            throw new IOException("Invalid Amber Island File Container");

        int fileVersion = stream.ReadByte();

        if (fileVersion > SupportedFileVersion)
            throw new IOException("File version not supported by this reader");

        var flags = (FileContainerFlags)(byte)stream.ReadByte();

        ushort ReadWord()
        {
            int word = stream.ReadByte();
            word <<= 8;
            word |= stream.ReadByte();
            return (ushort)word;
        }

        int fileCount = ReadWord();
        List<ushort>? fileIndices = null;

        if (flags.HasFlag(FileContainerFlags.StoreFileIndices))
        {
            fileIndices = new(fileCount);

            for (uint i = 0; i < fileCount; i++)
            {
                fileIndices.Add(ReadWord());
            }
        }

        uint offset = stream.CanSeek ? (uint)(stream.Position + fileCount * 2) : 0;
        var fileContainer = new FileContainer();

        for (uint i = 0; i < fileCount; i++)
        {
            uint size = ReadWord();
            uint index = fileIndices == null ? (1 + i) : fileIndices[(int)i];

            fileContainer.files.Add(index, new FileLocation(offset, size));

            offset += size;
        }

        if (stream.CanSeek)
            fileContainer.stream = stream;
        else
        {
            fileContainer.stream = new MemoryStream();
            stream.CopyTo(fileContainer.stream);
            fileContainer.stream.Position = 0;
        }

        return fileContainer;
    }

    public static Dictionary<uint, byte[]> ReadAllFiles(string containerPath)
    {
        using var stream = File.OpenRead(containerPath);
        return ReadAllFiles(stream);
    }

    /// <summary>
    /// Reads a whole container into memory, returning the raw bytes of every
    /// contained file keyed by its file index. The passed stream is not disposed.
    /// </summary>
    public static Dictionary<uint, byte[]> ReadAllFiles(Stream stream)
    {
        var container = Read(stream);
        var result = new Dictionary<uint, byte[]>(container.files.Count);

        foreach (var entry in container.files)
        {
            var reader = container.GetFileReader(entry.Key);
            result[entry.Key] = reader.ReadBytes((int)entry.Value.Size);
        }

        return result;
    }

    public static Dictionary<uint, byte[]> ReadFiles(string containerPath, params uint[] fileIndices)
    {
        using var stream = File.OpenRead(containerPath);
        return ReadFiles(stream, fileIndices);
    }

    public static Dictionary<uint, byte[]> ReadFiles(Stream stream, params uint[] fileIndices)
    {
        if (fileIndices == null || fileIndices.Length == 0)
            return [];

        using var container = Read(stream);
        var result = new Dictionary<uint, byte[]>(fileIndices.Length);

        foreach (var fileIndex in fileIndices)
        {
            if (container.files.TryGetValue(fileIndex, out var location))
            {
                var reader = container.GetFileReader(fileIndex);
                result[fileIndex] = reader.ReadBytes((int)location.Size);
            }
        }

        return result;
    }

    public static Dictionary<uint, IDataReader> StreamAllFiles(Stream stream)
    {
        var container = Read(stream);
        var result = new Dictionary<uint, IDataReader>(container.files.Count);

        foreach (var entry in container.files)
        {
            var reader = container.GetFileReader(entry.Key);
            result[entry.Key] = reader;
        }

        return result;
    }

    public static void Write(Stream stream, Dictionary<uint, byte[]> files, bool flushAfterWrite = true)
    {
        stream.Write(Encoding.ASCII.GetBytes(Magic));
        stream.WriteByte(SupportedFileVersion);

        bool needExplicitFileIndices = files.Keys.Min() != 1 || files.Keys.Max() != files.Count;
        var flags = FileContainerFlags.None;

        if (needExplicitFileIndices)
        {
            flags |= FileContainerFlags.StoreFileIndices;
        }

        stream.WriteByte((byte)flags);

        var writer = new DataWriter();

        writer.Write((ushort)files.Count);

        var orderedFiles = files.OrderBy(file => file.Key).ToList();

        if (needExplicitFileIndices)
        {
            foreach (var file in orderedFiles)
            {
                writer.Write((ushort)file.Key);
            }
        }

        foreach (var file in orderedFiles)
        {
            writer.Write((ushort)file.Value.Length);
        }

        foreach (var file in orderedFiles)
        {
            writer.Write(file.Value);
        }

        stream.Write(writer.ToArray());

        if (flushAfterWrite)
            stream.Flush();
    }

    public void Dispose()
    {
        files.Clear();
        stream?.Dispose();
        stream = null;
    }

    public IDataReader GetFileReader(uint fileIndex)
    {
        if (stream == null)
            throw new KeyNotFoundException("File container was already disposed");

        if (!files.TryGetValue(fileIndex, out var fileInfo))
            throw new KeyNotFoundException($"No file with index {fileIndex} was found in file container");

        return new StreamedDataReader(stream, fileInfo.Offset, (int)fileInfo.Size);
    }
}
