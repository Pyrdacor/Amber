using System.Text;
using Amber.IO.Common.Serialization;
using Amber.IO.FileFormats.Compression;
using Amber.IO.FileFormats.Serialization;

namespace AmberIsland.GameData;

public enum FontIndex : uint
{
    DamageFont = 1,
    // TODO ...
}

public readonly record struct Utf8Char
(
    byte[] Bytes
)
{
    public Utf8Char(char ch) : this(Encoding.UTF8.GetBytes([ch]))
    {
    }

    public void Write(IDataWriter writer)
    {
        writer.Write((byte)Bytes.Length);
        writer.Write(Bytes);
    }

    public static Utf8Char Read(IDataReader reader)
    {
        var amount = reader.ReadByte();
        var bytes = reader.ReadBytes(amount);

        return new(bytes);
    }

    public char ToChar() => Encoding.UTF8.GetChars(Bytes)[0];
}

public readonly record struct FontGlyph
(
    Utf8Char Char,
    byte Advance    
)
{
    public void Write(IDataWriter writer)
    {
        Char.Write(writer);
        writer.Write(Advance);
    }

    public static FontGlyph Read(IDataReader reader)
    {
        var @char = Utf8Char.Read(reader);
        var advance = reader.ReadByte();

        return new
        (
            @char,
            advance
        );
    }
}

public readonly record struct Font
(
    uint AtlasWidth,
    uint AtlasHeight,
    byte[] AtlasAlphaValues,
    ushort GlyphWidth,
    ushort GlyphHeight,
    // NOTE: Glyphs must be in the same order as their character value in UTF-8!
    FontGlyph[] Glyphs
)
{
    public void Write(IDataWriter writer)
    {
        writer.Write(AtlasWidth);
        writer.Write(AtlasHeight);

        var atlasWriter = new DataWriter();
        atlasWriter.Write(Deflate.Compress(AtlasAlphaValues));

        writer.Write((uint)atlasWriter.Size);
        writer.Write(atlasWriter.ToArray());
        writer.Write(GlyphWidth);
        writer.Write(GlyphHeight);

        writer.Write((ushort)Glyphs.Length);

        foreach (var glyph in Glyphs)
            glyph.Write(writer);
    }

    public static Font Read(IDataReader reader)
    {
        var atlasWidth = reader.ReadDword();
        var atlasHeight = reader.ReadDword();
        var atlasDataSize = reader.ReadDword();
        var atlasAlphaValues = Deflate.Decompress(reader.ReadBytes((int)atlasDataSize));
        var glyphWidth = reader.ReadWord();
        var glyphHeight = reader.ReadWord();
        var glyphCount = reader.ReadWord();
        var glyphs = new FontGlyph[glyphCount];

        for (int i = 0; i < glyphCount; i++)
            glyphs[i] = FontGlyph.Read(reader);

        return new
        (
            atlasWidth,
            atlasHeight,
            atlasAlphaValues,
            glyphWidth,
            glyphHeight,
            glyphs
        );
    }
}
