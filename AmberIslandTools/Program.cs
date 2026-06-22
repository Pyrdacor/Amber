#define CHECK

using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using Amber.IO.FileFormats.Compression;
using Amber.IO.FileFormats.Serialization;
using AmberIsland.GameData;

unsafe
{

#if CHECK
    var imgFile = @"D:\Projects\Amber\AmberIsland\assets\player\temp\000.aig";
    var palFile = @"D:\Projects\Amber\AmberIsland\assets\player\temp\000.aip";

    using var palReader = new StreamedDataReader(File.OpenRead(palFile));
    int colorCount = palReader.ReadByte();
    var colors = new ColorRgb[colorCount];

    for (int i = 0; i < colorCount; i++)
        colors[i] = ColorRgb.Read(palReader);

    using var imgReader = new BinaryReader(File.OpenRead(imgFile));

    int width = imgReader.ReadByte();
    width <<= 8;
    width |= imgReader.ReadByte();

    int height = imgReader.ReadByte();
    height <<= 8;
    height |= imgReader.ReadByte();

    colorCount = imgReader.ReadByte();

    if (colorCount != 0)
    {
        Console.WriteLine("Did not expect embedded colors.");

        colors = new ColorRgb[colorCount];

        for (int i = 0; i < colorCount; i++)
            colors[i] = ColorRgb.Read(palReader);
    }

    var data = imgReader.ReadBytes((int)(imgReader.BaseStream.Length - imgReader.BaseStream.Position));
    data = Deflate.Decompress(data);

    if (data.Length != width * height)
    {
        Console.WriteLine("Unexpected data size");
    }
    else
    {
        var bgra = new byte[data.Length * 4];

        for (int i = 0; i < data.Length; i++)
        {
            if (data[i] != 0)
            {
                var color = colors[data[i] - 1];
                bgra[i * 4 + 0] = color.B;
                bgra[i * 4 + 1] = color.G;
                bgra[i * 4 + 2] = color.R;
                bgra[i * 4 + 3] = 255;
            }
        }

        using var bitmap = new Bitmap(width, height);
        var bd = bitmap.LockBits(new Rectangle(0, 0, width, height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        Marshal.Copy(bgra, 0, bd.Scan0, bgra.Length);

        bitmap.UnlockBits(bd);

        var tempfile = Path.GetTempFileName();

        bitmap.Save(tempfile);

        Process.Start("mspaint", tempfile);
    }
#elif CREATE
    var directory = @"D:\Projects\Amber\AmberIsland\assets\character_base\char_a_p1";
    var schema = "char_a_p1_0bas_humn_v{0}.png";
    int fileCount = 11;
    Size frameSize = new(64, 64);
    Size extractSize = new(32, 40);
    Sprite? firstSprite = null;
    List<PaletteRgb> palettes = [];

    for (int i = 0; i < fileCount; i++)
    {
        string file = Path.Combine(directory, string.Format(schema, i.ToString("00")));

        using var bitmap = (Bitmap)Image.FromFile(file);
        var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        int framesPerRow = bitmap.Width / frameSize.Width;
        int frameRows = bitmap.Height / frameSize.Height;
        int width = framesPerRow * extractSize.Width;
        int height = frameRows * extractSize.Height;
        var rgba = new byte[extractSize.Width * 4];
        List<ColorRgb> palette = [];
        var colorIndices = new byte[width * height];
        int targetStride = extractSize.Width * 4;

        for (int fy = 0; fy < frameRows; fy++)
        {
            for (int fx = 0; fx < framesPerRow; fx++)
            {
                int totalX = fx * frameSize.Width + (frameSize.Width - extractSize.Width) / 2;
                int totalY = fy * frameSize.Height + (frameSize.Height - extractSize.Height) / 2;
                nint source = data.Scan0 + totalY * bitmap.Width * 4 + totalX * 4;
                
                for (int y = 0; y < extractSize.Height; y++)
                {
                    Marshal.Copy(source, rgba, 0, targetStride);
                    source += data.Stride;

                    for (int x = 0; x < extractSize.Width; x++)
                    {
                        var a = rgba[x * 4 + 3];

                        if (a == 0)
                        {
                            colorIndices[fx * extractSize.Width + x + (fy * extractSize.Height + y) * width] = 0;
                        }
                        else
                        {
                            var b = rgba[x * 4 + 0];
                            var g = rgba[x * 4 + 1];
                            var r = rgba[x * 4 + 2];
                            var color = new ColorRgb(r, g, b);

                            int index = palette.IndexOf(color);

                            if (index == -1)
                            {
                                colorIndices[fx * extractSize.Width + x + (fy * extractSize.Height + y) * width] = (byte)(1 + palette.Count);
                                palette.Add(color);
                            }
                            else
                            {
                                colorIndices[fx * extractSize.Width + x + (fy * extractSize.Height + y) * width] = (byte)(1 + index);
                            }
                        }
                    }
                }
            }
        }

        bitmap.UnlockBits(data);

        var sprite = new Sprite
        {
            Width = (ushort)width,
            Height = (ushort)height,
            ColorIndices = colorIndices,
            Colors = [] // Don't embed palette!
        };

        Directory.CreateDirectory(@"D:\Projects\Amber\AmberIsland\assets\player\temp");
        using var outFile = File.Create($@"D:\Projects\Amber\AmberIsland\assets\player\temp\{i:000}.aig");
        using var outWriter = new StreamBoundDataWriter(outFile);
        sprite.Write(outWriter);

        using var palFile = File.Create($@"D:\Projects\Amber\AmberIsland\assets\player\temp\{i:000}.aip");
        using var palWriter = new StreamBoundDataWriter(palFile);
        palWriter.Write((byte)palette.Count);

        foreach (var color in palette)
        {
            color.Write(palWriter);
        }

        if (i == 0)
            firstSprite = sprite;

        palettes.Add(new PaletteRgb(palette.ToArray()));
    }

    if (firstSprite is Sprite playerSprite)
    {
        var spriteWithPalettes = SpriteWithPalettes.FromSpriteAndPalettes(playerSprite, palettes.ToArray());

        {
            using var playerOutFile = File.Create($@"D:\Projects\Amber\AmberIsland\assets\player\player.aisp");
            using var playerOutWriter = new StreamBoundDataWriter(playerOutFile);
            spriteWithPalettes.Write(playerOutWriter);
        }

        var data = File.ReadAllBytes($@"D:\Projects\Amber\AmberIsland\assets\player\player.aisp");
        using var containerStream = File.Create(@"D:\Projects\Amber\AmberIsland\assets\player.aifc");

        FileContainer.Write(containerStream, new() { { 1u, data } });
    }
#endif
}

