using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Amber.Assets.Common;

namespace Amberworlds.GameData.Default;

internal static class ImageLoader
{
    public static IGraphic Load(string path)
    {
        using var bitmap = (Bitmap)Bitmap.FromFile(path);
        var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        var pixelData = new byte[bitmap.Width * bitmap.Height * 4];

        Marshal.Copy(data.Scan0, pixelData, 0, pixelData.Length);

        bitmap.UnlockBits(data);

        int n = pixelData.Length / 4;
        byte temp;

        for (int i = 0; i < n; i++)
        {
            temp = pixelData[i * 4 + 0];
            pixelData[i * 4 + 0] = pixelData[i * 4 + 2];
            pixelData[i * 4 + 2] = temp;
        }

        return new Graphic(bitmap.Width, bitmap.Height, pixelData, GraphicFormat.RGBA);
    }
}
