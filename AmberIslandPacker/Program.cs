// TODO: For now we just pack a single file into a container :>

using System.Drawing;
using System.Runtime.InteropServices;
using Amber.IO.FileFormats.Compression;
using Amber.IO.FileFormats.Serialization;
using AmberIsland.GameData;

//string filename = @"D:\Projects\Amber\AmberIsland\assets\tilesets\tileset_data";
//string filename = @"D:\Projects\Amber\AmberIsland\assets\maps\map_data";
string filename = @"D:\Projects\Amber\AmberIsland\assets\monsters\bat_blue.png";
//string filename = @"D:\Projects\Amber\AmberIsland\assets\monsters\bat";
//string containerPath = @"D:\Projects\Amber\AmberIsland\assets\tileset.aifc";
//string containerPath = @"D:\Projects\Amber\AmberIsland\assets\map.aifc";
string containerPath = @"D:\Projects\Amber\AmberIsland\assets\monsteratlas.aifc";
//string containerPath = @"D:\Projects\Amber\AmberIsland\assets\monster.aifc";

//var fileData = File.ReadAllBytes(filename);
//var fileData = Deflate.Compress(File.ReadAllBytes(filename));
var fileData = ImageToSprite(filename);
//var fileData = File.ReadAllBytes(filename);
using var stream = File.Create(containerPath);
FileContainer.Write(stream, new() { { 1u, fileData } });



byte[] ImageToSprite(string filename)
{
    using var bitmap = (Bitmap)Image.FromFile(filename);
    var bmp = bitmap.LockBits(new(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

    var bgra = new byte[bitmap.Width * bitmap.Height * 4];
    Marshal.Copy(bmp.Scan0, bgra, 0, bgra.Length);

    bitmap.UnlockBits(bmp);

    List<ColorRgb> palette = [];
    var colorIndices = new byte[bitmap.Width * bitmap.Height];
    int index = 0;

    for (int i = 0; i < colorIndices.Length; i++)
    {
        if (bgra[index + 3] == 0)
        {
            colorIndices[i] = 0;
            index += 4;
        }
        else
        {
            byte b = bgra[index++];
            byte g = bgra[index++];
            byte r = bgra[index++];
            index += 1;
            var color = new ColorRgb(r, g, b);
            int colorIndex = palette.IndexOf(color);

            if (colorIndex == -1)
            {
                palette.Add(color);
                colorIndices[i] = (byte)palette.Count;
            }
            else
            {
                colorIndices[i] = (byte)(1 + colorIndex);
            }
        }
    }

    var sprite = new Sprite((ushort)bitmap.Width, (ushort)bitmap.Height, palette.ToArray(), colorIndices);
    var writer = new DataWriter();
    sprite.Write(writer);

    return writer.ToArray();
}