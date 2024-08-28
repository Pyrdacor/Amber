using Amber.Common;
using Amber.Renderer;
using Amberstar.GameData.Serialization;
using ButtonType = Amberstar.GameData.Serialization.Button;

namespace Amberstar.Game.UI;

internal class Button
{
	public const int Width = 32;
	public const int Height = 16;
	const int HighlightDelay = 250;
	readonly Game game;
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
		highlightOverlay.DisplayLayer = (byte)Math.Min(byte.MaxValue, displayLayer + 5);
		highlightOverlay.PaletteIndex = paletteIndex.Value;
		highlightOverlay.Visible = false;

		disabledOverlay = layer.SpriteFactory.Create();
		disabledOverlay.Position = new(x, y);
		disabledOverlay.Size = new(Width, Height);
		disabledOverlay.TextureOffset = textureAtlas.GetOffset(game.GraphicIndexProvider.GetUIGraphicIndex(UIGraphic.ChequeredIcon));
		disabledOverlay.DisplayLayer = (byte)Math.Min(byte.MaxValue, displayLayer + 10);
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
		highlightOverlay.Visible = false;
		disabledOverlay.Visible = false;
	}
}
