using Amber.Assets.Common;
using Amber.Common;
using Amber.Renderer.Common;
using Amberworlds.Game.Screens;
using Amberworlds.GameData;

namespace Amberworlds.Game;

public partial class Game
{
    public const int MaxPartyMembers = 6;
    public const long TicksPerSecond = 60;
    public const long DefaultFadeTime = 1000;
    readonly IGameData gameData;
    readonly IRenderer renderer;
    readonly ISprite heroImage;
    readonly List<ISurface3D> walls = [];
    double totalTime = 0.0;
    long lastGameTicks = 0;
    long gameTicks = 0;
    long lastMoveTicks = 0;

    internal ScreenHandler ScreenHandler { get; }

    public Game(IGameData gameData, IRenderer renderer)
    {
        this.gameData = gameData;
        this.renderer = renderer;

        var graphics = new Dictionary<int, IGraphic>
        {
            { 0, gameData.GetHeroImage() }
        };
        var layer = renderer.LayerFactory.Create(LayerType.Images, new()
        {
            BaseZ = 0.0f,
            LayerFeatures = LayerFeatures.DisplayLayers,
            RenderTarget = LayerRenderTarget.Window,
            Texture = renderer.TextureFactory.CreateAtlas(graphics),
        });
        layer.Visible = true;
        renderer.AddLayer(layer);

        graphics = new Dictionary<int, IGraphic>
        {
            { 0, gameData.GetWallImage() }
        };
        layer = renderer.LayerFactory.Create(LayerType.Texture3D, new()
        {
            BaseZ = 0.0f,
            LayerFeatures = LayerFeatures.Fog,
            RenderTarget = LayerRenderTarget.Window,
            Texture = renderer.TextureFactory.CreateAtlas(graphics),
            Palette = renderer.TextureFactory.Create(gameData.GetWallPalette()),
        });
        layer.Visible = true;
        renderer.AddLayer(layer);

        var layer3D = (layer as ILayer3D)!;

        heroImage = CreateSprite(Layer.UI, new(16, 4), new(32, 32), 0, new(1254, 1254));

        var surface = layer3D.Surface3DFactory!.Create();
        surface.Face = SurfaceFace.Front;
        surface.TextureOffset = new();
        surface.TextureSize = new(128, 128);
        surface.Size = new(100, 100);
        surface.Position = new(20, 20, -280);
        surface.Visible = true;
        walls.Add(surface);

        surface = layer3D.Surface3DFactory!.Create();
        surface.Face = SurfaceFace.Front;
        surface.TextureOffset = new();
        surface.TextureSize = new(128, 128);
        surface.Size = new(100, 100);
        surface.Position = new(120, 20, -280);
        surface.Visible = true;
        walls.Add(surface);

        surface = layer3D.Surface3DFactory!.Create();
        surface.Face = SurfaceFace.Left;
        surface.TextureOffset = new();
        surface.TextureSize = new(128, 128);
        surface.Size = new(100, 100);
        surface.Position = new(120, 20, -280);
        surface.Visible = true;
        walls.Add(surface);

        layer3D.FogColor = Color.Black;
        layer3D.FogStartDistance = 30;
        layer3D.FogEndDistance = 40;

        ScreenHandler = new(this);

        ScreenHandler.PushScreen(ScreenType.Map3D);
    }

    public void Update(double delta)
    {
        totalTime += delta;
        gameTicks = (long)Math.Round(totalTime * TicksPerSecond);

        long elapsed = Paused ? 0 : gameTicks - lastGameTicks;
        lastGameTicks = gameTicks;

        ScreenHandler.ActiveScreen?.Update(elapsed);

        if (lastMoveTicks < lastGameTicks - 1)
        {
            if (pressedKeys.Contains(Key.Up))
                renderer.Camera.MoveForward(5);
            if (pressedKeys.Contains(Key.Down))
                renderer.Camera.MoveForward(-5);
            if (pressedKeys.Contains(Key.Left))
                renderer.Camera.Turn(-1.5f);
            if (pressedKeys.Contains(Key.Right))
                renderer.Camera.Turn(1.5f);

            lastMoveTicks = lastGameTicks;
        }
    }
}
