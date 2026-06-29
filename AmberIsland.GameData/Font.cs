using System.Text;
using Amber.IO.Common.Serialization;
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
    byte Width,
    byte Advance    
)
{
    public void Write(IDataWriter writer)
    {
        Char.Write(writer);
        writer.Write(Width);
        writer.Write(Advance);
    }

    public static FontGlyph Read(IDataReader reader)
    {
        var @char = Utf8Char.Read(reader);
        var width = reader.ReadByte();
        var advance = reader.ReadByte();

        return new
        (
            @char,
            width,
            advance
        );
    }
}

public readonly record struct Font
(
    Sprite Atlas,
    byte GlyphWidth,
    byte GlyphHeight,
    // NOTE: Glyphs must be in the same order as their character value in UTF-8!
    FontGlyph[] Glyphs
)
{
    public void Write(IDataWriter writer)
    {
        var atlasWriter = new DataWriter();
        Atlas.Write(atlasWriter);

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
        var atlasDataSize = reader.ReadDword();
        var atlas = Sprite.Read(new DataReader(reader.ReadBytes((int)atlasDataSize)));
        var glyphWidth = reader.ReadByte();
        var glyphHeight = reader.ReadByte();
        var glyphCount = reader.ReadWord();
        var glyphs = new FontGlyph[glyphCount];

        for (int i = 0; i < glyphCount; i++)
            glyphs[i] = FontGlyph.Read(reader);

        return new
        (
            atlas,
            glyphWidth,
            glyphHeight,
            glyphs
        );
    }
}
