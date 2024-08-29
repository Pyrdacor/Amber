using Amber.Common;
using Amber.Renderer;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.UI;

internal class Button
{
	public const int Width = 32;
	public const int Height = 16;
	const int HighlightDelay = 80;
	readonly Game game;
	readonly IColoredRect background;
	readonly ISprite sprite;
	readonly ISprite highlightOverlay;
	readonly ISprite disabledOverlay;
	bool pressed = false;

	public event Action? ClickAction;

	public byte PaletteIndex
	{
		get => sprite.PaletteIndex;
		set
		{
			sprite.PaletteIndex = value;
			highlightOverlay.PaletteIndex = value;
		}
	}

	public bool Disabled
	{
		get => disabledOverlay.Visible;
		set => disabledOverlay.Visible = value;
	}

	public Button(Game game, int x, int y, ButtonType buttonType, byte displayLayer, byte? paletteIndex = null)
	{
		this.game = game;
		var layer = game.GetRenderLayer(Layer.UI);
		var textureAtlas = layer.Config.Texture!;
		paletteIndex ??= game.PaletteIndexProvider.UIPaletteIndex;

		if (displayLayer < 2)
			displayLayer = 2;
		if (displayLayer > byte.MaxValue - 4)
			displayLayer = byte.MaxValue - 4;

		background = layer.ColoredRectFactory!.Create();
		background.Position = new(x + 2, y + 2);
		background.Size = new(Width - 4, Height - 4);
		background.Color = Color.Black;
		background.DisplayLayer = (byte)(displayLayer - 2);
		background.Visible = true;

		sprite = layer.SpriteFactory!.Create();
		sprite.Position = new(x, y);
		sprite.Size = new(Width, Height);
		sprite.TextureOffset = textureAtlas.GetOffset(game.GraphicIndexProvider.GetButtonIndex(buttonType));
		sprite.DisplayLayer = displayLayer;
		sprite.PaletteIndex = paletteIndex.Value;
		sprite.Visible = true;

		highlightOverlay = layer.SpriteFactory.Create();
		highlightOverlay.Position = new(x, y);
		highlightOverlay.Size = new(Width, Height);
		highlightOverlay.TextureOffset = textureAtlas.GetOffset(game.GraphicIndexProvider.GetUIGraphicIndex(UIGraphic.FeedbackIcon));
		highlightOverlay.DisplayLayer = (byte)(displayLayer + 2);
		highlightOverlay.PaletteIndex = paletteIndex.Value;
		highlightOverlay.Visible = false;

		disabledOverlay = layer.SpriteFactory.Create();
		disabledOverlay.Position = new(x, y);
		disabledOverlay.Size = new(Width, Height);
		disabledOverlay.TextureOffset = textureAtlas.GetOffset(game.GraphicIndexProvider.GetUIGraphicIndex(UIGraphic.ChequeredIcon));
		disabledOverlay.DisplayLayer = (byte)(displayLayer + 4);
		disabledOverlay.PaletteIndex = paletteIndex.Value;
		disabledOverlay.Visible = false;
	}

	public void SetType(ButtonType buttonType)
	{
		var layer = game.GetRenderLayer(Layer.UI);
		var textureAtlas = layer.Config.Texture!;
		sprite.TextureOffset = textureAtlas.GetOffset(game.GraphicIndexProvider.GetButtonIndex(buttonType));
	}

	public bool MouseClick(Position position)
	{
		if (Disabled)
			return false;

		var upperLeft = sprite.Position;
		var lowerRight = new Position(upperLeft.X + Width, upperLeft.Y + Height);

		if (position.X < upperLeft.X || position.Y < upperLeft.Y || position.X >= lowerRight.X || position.Y >= lowerRight.Y)
			return false;

		Press();

		return true;
	}

	public void Press()
	{
		if (pressed || Disabled)
			return;

		highlightOverlay.Visible = true;
		pressed = true;
		game.AddDelayedAction(TimeSpan.FromMilliseconds(HighlightDelay), () => {
			highlightOverlay.Visible = false;
			pressed = false;
			ClickAction?.Invoke();
		});
	}

	public void Destroy()
	{
		sprite.Visible = false;
		background.Visible = false;
		highlightOverlay.Visible = false;
		disabledOverlay.Visible = false;
	}
}
