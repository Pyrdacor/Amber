using System.Drawing;
using System.Runtime.InteropServices;
using Amber.Assets.Common;

namespace Amberworlds.GameData.Default;

public class GameData : IGameData
{
    private readonly IGraphic heroImage;
    private readonly IGraphic wallImage;
    private readonly IGraphic wallPalette;

    public GameData()
    {
        heroImage = ImageLoader.Load(@"D:\Projects\Amber\Amberworlds.GameData.Default\hero.png");
        wallImage = new Graphic(128, 128, File.ReadAllBytes(@"D:\Projects\Amber\Amberworlds.GameData.Default\wall.img"), GraphicFormat.PaletteIndices);
        wallPalette = Graphic.FromPalette(File.ReadAllBytes(@"D:\Projects\Amber\Amberworlds.GameData.Default\wall.pal"));

        var foo = new byte[128 * 128 * 4];
        var img = wallImage.GetData();
        var pal = wallPalette.GetData();

        for (int i = 0; i < 128 * 128; i++)
        {
            int p = img[i] * 4;

            foo[i * 4 + 0] = pal[p + 2];
            foo[i * 4 + 1] = pal[p + 1];
            foo[i * 4 + 2] = pal[p + 0];
            foo[i * 4 + 3] = 255;//pal[p + 3];
        }

        using var bmp = new Bitmap(128, 128);
        var data = bmp.LockBits(new Rectangle(0, 0, 128, 128), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        Marshal.Copy(foo, 0, data.Scan0, foo.Length);

        bmp.UnlockBits(data);

        bmp.Save(@"D:\Projects\Amber\Amberworlds.GameData.Default\wall_prev.png");

    }

    public IGraphic GetHeroImage() => heroImage;

    public IGraphic GetWallImage() => wallImage;

    public IGraphic GetWallPalette() => wallPalette;
}
