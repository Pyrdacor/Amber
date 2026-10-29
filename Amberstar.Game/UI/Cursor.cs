using Amber.Common;
using Amber.Renderer.Common;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.UI;

internal class Cursor
{
	public const int Width = 16;
	public const int Height = 16;
	readonly Game game;
	readonly ISprite sprite;
	// the item which is shown instead of the cursor (e.g. while its receiver is chosen)
	ISprite? itemSprite;
	bool visible = true;
	readonly Dictionary<CursorType, Position> cursorHotspots = [];
	CursorType cursorType = CursorType.Sword;
	Position hotspot = new(0, 0);
	Position position = new(0, 0);

	public bool Visible
	{
		get => visible;
		set
		{
			visible = value;
			sprite.Visible = value && itemSprite == null;

			if (itemSprite != null)
				itemSprite.Visible = value;
		}
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

			if (itemSprite != null)
				itemSprite.Position = ItemPosition;
		}
	}

	// the cursor is in the center of the item
	Position ItemPosition => new(position.X - Width / 2, position.Y - Height / 2);

	/// <summary>
	/// The cursor shows the item (while its receiver is chosen). The original shows the item masked in white,
	/// the option UnmaskedDraggedItem shows it like dragged items with its colors. Null shows the cursor again.
	/// </summary>
	public void ShowItem(IItem? item)
	{
		if (item == null)
		{
			if (itemSprite != null)
			{
				itemSprite.Visible = false;
				itemSprite = null;
			}

			sprite.Visible = visible;
			return;
		}

		var layer = game.GetRenderLayer(Layer.UI);

		itemSprite ??= layer.SpriteFactory!.Create();
		itemSprite.Size = new(Width, Height);
		itemSprite.TextureOffset = layer.Config.Texture!.GetOffset(game.GraphicIndexProvider.GetItemGraphicIndex(item.GraphicIndex));
		itemSprite.DisplayLayer = byte.MaxValue;
		itemSprite.PaletteIndex = game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.Item];
		itemSprite.MaskColorIndex = game.IsOptionSet(GameOptions.UnmaskedDraggedItem) ? null : (byte)0xf;
		itemSprite.Position = ItemPosition;
		itemSprite.Visible = visible;
		sprite.Visible = false;
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
		var paletteIndex = game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI];

		sprite = layer.SpriteFactory!.Create();
		sprite.Position = new(0, 0);
		sprite.Size = new(Width, Height);
		sprite.TextureOffset = textureAtlas.GetOffset(game.GraphicIndexProvider.GetCursorGraphicIndex(CursorType));
		sprite.DisplayLayer = byte.MaxValue; // always on top
		sprite.PaletteIndex = paletteIndex;
		sprite.TransparentColorIndex = 14;
		sprite.Visible = true;
	}

	public void Destroy()
	{
		ShowItem(null);
		sprite.Visible = false;
	}
}
