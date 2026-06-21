using Amber.Assets.Common;

namespace Amberworlds.GameData;

public interface IGameData
{
    IGraphic GetHeroImage();
    IGraphic GetWallImage();
    IGraphic GetWallPalette();
}
