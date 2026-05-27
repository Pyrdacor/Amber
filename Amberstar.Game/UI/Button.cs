using Amber.Common;
using Amber.Renderer;
using Amber.Renderer.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.UI;

internal class Button : Control
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
	bool disabled = false;
    bool highlighted = false;
    ButtonType buttonType = ButtonType.Empty;

    public event Action? ClickAction;
    public event Action? RightClickAction;

    public override byte PaletteIndex
	{
		get => sprite.PaletteIndex;
		set
		{
			sprite.PaletteIndex = value;
			highlightOverlay.PaletteIndex = value;
		}
	}

    public override byte DisplayLayer
    {
        get => sprite.DisplayLayer;
        set
        {
            if (value < 2)
                value = 2;
            if (value > byte.MaxValue - 4)
                value = byte.MaxValue - 4;

			if (value == sprite.DisplayLayer)
				return;

            background.DisplayLayer = (byte)(value - 2);
            sprite.DisplayLayer = value;
            highlightOverlay.DisplayLayer = (byte)(value + 2);
            disabledOverlay.DisplayLayer = (byte)(value + 4);
        }
    }

    public bool Disabled
	{
		get => disabled || buttonType == ButtonType.Empty;
		set
		{
			disabled = value;
			disabledOverlay.Visible = value && Visible;
		}
	}

	public ButtonType ButtonType => buttonType;

	public override bool Visible
	{
		get => sprite?.Visible ?? false;
		set
		{
            sprite.Visible = value;
            background.Visible = value;
            highlightOverlay.Visible = value && highlighted;
            disabledOverlay.Visible = value && disabled;
        }
	}

	public Position Position
	{
		get => sprite.Position;
		set
		{
			sprite.Position = value;
            background.Position = value;
            highlightOverlay.Position = value;
            disabledOverlay.Position = value;
        }
	}

	public Rect Area => new(Position, new(Width, Height));

    public Button(Game game, int x, int y, ButtonType buttonType, byte displayLayer, byte? paletteIndex = null)
	{
		this.game = game;
		this.buttonType = buttonType;
        var layer = game.GetRenderLayer(Layer.UI);
		var textureAtlas = layer.Config.Texture!;
		paletteIndex ??= game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI];

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
		sprite.Opaque = true;
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
        this.buttonType = buttonType;
    }

	public bool MouseClick(Position position, MouseButtons mouseButtons = MouseButtons.Left)
	{
		if (Disabled)
			return false;

		var upperLeft = sprite.Position;
		var lowerRight = new Position(upperLeft.X + Width, upperLeft.Y + Height);

		if (position.X < upperLeft.X || position.Y < upperLeft.Y || position.X >= lowerRight.X || position.Y >= lowerRight.Y)
			return false;

		Press(mouseButtons == MouseButtons.Right);

		return true;
	}

	public bool TryPress(bool rightClick = false)
	{
        if (Disabled)
            return false;

        Press(rightClick);

        return true;
    }

	public void Press(bool rightClick = false)
	{
		if (pressed || Disabled)
			return;

		highlighted = true;
        highlightOverlay.Visible = true;
		pressed = true;
		game.AddDelayedAction(TimeSpan.FromMilliseconds(HighlightDelay), () => {
			highlighted = false;
            highlightOverlay.Visible = false;
			pressed = false;

			if (rightClick)
                RightClickAction?.Invoke();
			else
				ClickAction?.Invoke();
		});
	}

	public override void Destroy()
	{
		sprite.Visible = false;
		background.Visible = false;
		highlightOverlay.Visible = false;
		disabledOverlay.Visible = false;
	}
}
