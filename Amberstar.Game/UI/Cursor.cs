using Amber.Common;
using Amber.Renderer;
using Amberstar.GameData;

namespace Amberstar.Game.UI;

internal class Cursor
{
	public const int Width = 16;
	public const int Height = 16;
	readonly Game game;
	readonly ISprite sprite;
	readonly Dictionary<CursorType, Position> cursorHotspots = [];
	CursorType cursorType = CursorType.Sword;
	Position hotspot = new(0, 0);
	Position position = new(0, 0);

	public bool Visible
	{
		get => sprite.Visible;
		set => sprite.Visible = value;
	}

	public CursorType CursorType
	{
		get => cursorType;
		set
		{
			if (cursorType == value)
				return;

			cursorType = value;
			var layer = game.GetRenderLayer(Layer.UI);
			var textureAtlas = layer.Config.Texture!;
			sprite.TextureOffset = textureAtlas.GetOffset(game.GraphicIndexProvider.GetCursorGraphicIndex(cursorType));

			if (!cursorHotspots.TryGetValue(cursorType, out var hotspot))
			{
				hotspot = game.AssetProvider.CursorLoader.LoadCursor(cursorType).Hotspot;
				cursorHotspots.Add(cursorType, hotspot);
			}

			this.hotspot = hotspot;
			sprite.Position = new(position.X - hotspot.X, position.Y - hotspot.Y);
		}
	}

	public Position Position
	{
		get => position;
		set
		{
			if (position == value)
				return;

			position = value;
			sprite.Position = new(position.X - hotspot.X, position.Y - hotspot.Y);
		}
	}

	public byte PaletteIndex
	{
		get => sprite.PaletteIndex;
		set => sprite.PaletteIndex = value;
	}

	public Cursor(Game game)
	{
		this.game = game;
		var layer = game.GetRenderLayer(Layer.UI);
		var textureAtlas = layer.Config.Texture!;
		var paletteIndex = game.PaletteIndexProvider.UIPaletteIndex;

		sprite = layer.SpriteFactory!.Create();
		sprite.Position = new(0, 0);
		sprite.Size = new(Width, Height);
		sprite.TextureOffset = textureAtlas.GetOffset(game.GraphicIndexProvider.GetCursorGraphicIndex(CursorType));
		sprite.DisplayLayer = byte.MaxValue; // always on top
		sprite.PaletteIndex = paletteIndex;
		sprite.TransparentColorIndex = 14;
		sprite.MaskColorIndex = 15;
		sprite.Visible = true;
	}

	public void Destroy()
	{
		sprite.Visible = false;
	}
}
