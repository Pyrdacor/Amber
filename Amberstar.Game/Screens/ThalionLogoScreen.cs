using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;

namespace Amberstar.Game.Screens;

/// <summary>
/// The logo of Thalion (THALION.68K in the original, a separate program which starts before the intro). The logo is
/// simulated like the original (see <see cref="IThalionLogo"/>) and its frames are shown. A key ends it.
/// </summary>
internal sealed class ThalionLogoScreen : Screen
{
	const int VerticalBlanksPerSecond = 50;
	const byte DisplayLayer = 200;
	// the scan codes of the Atari ST keyboard for the letters (for the secret words) and other keys
	static readonly byte[] LetterScanCodes =
	[
		0x1e, 0x30, 0x2e, 0x20, 0x12, 0x21, 0x22, 0x23, 0x17, 0x24, 0x25, 0x26, 0x32,
		0x31, 0x18, 0x19, 0x10, 0x13, 0x1f, 0x14, 0x16, 0x2f, 0x11, 0x2d, 0x15, 0x2c
	];
	const byte SpaceScanCode = 0x39;
	const byte OtherScanCode = 0x01;

	IThalionLogo logo = null!;
	long verticalBlankTicks = 0;
	int song = 0;
	Image? image;

	public override ScreenType Type { get; } = ScreenType.ThalionLogo;

	public override ScreenFadeType FadeType { get; } = ScreenFadeType.None;
	public override bool CloseOnEscape { get; } = false;
	public override bool AllowInventoryAccess { get; } = false;
	public override bool AllowCharacterSelection { get; } = false;
	public override bool UpdateWhilePaused { get; } = true;

	public override void Open(Action? closeAction)
	{
		base.Open(closeAction);

		logo = Game.AssetProvider.ThalionLogo!;
		logo.Reset();
		song = 0;
		verticalBlankTicks = 0;
		Game.Cursor.Visible = false;
		image = new Image(Game, new Position(0, 0), new Size(IThalionLogo.Width, IThalionLogo.Height),
			Game.GraphicIndexProvider.GetThalionLogoIndex(), DisplayLayer, Layer.TrueColor, 0, true);
		ShowFrame();
	}

	public override void Close()
	{
		image?.Destroy();
		image = null;
		Game.Cursor.Visible = true;

		base.Close();
	}

	static byte ToScanCode(Key key)
	{
		if (key == Key.Space)
			return SpaceScanCode;

		int letter = key - Key.LetterA;

		return letter >= 0 && letter < LetterScanCodes.Length ? LetterScanCodes[letter] : OtherScanCode;
	}

	public override bool KeyDown(Key key, KeyModifiers keyModifiers)
	{
		logo.KeyPressed(ToScanCode(key));

		return true;
	}

	public override bool KeyUp(Key key, KeyModifiers keyModifiers)
	{
		logo.KeyReleased();

		return true;
	}

	/// <summary>
	/// The mouse button ends the logo (like the fire button of the joystick).
	/// </summary>
	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		logo.Skip();

		return true;
	}

	public override void Update(long elapsedTicks)
	{
		base.Update(elapsedTicks);

		verticalBlankTicks += elapsedTicks * VerticalBlanksPerSecond;
		int newVerticalBlanks = (int)(verticalBlankTicks / Game.TicksPerSecond);
		verticalBlankTicks %= Game.TicksPerSecond;

		if (newVerticalBlanks == 0 || logo.Finished)
			return;

		for (int i = 0; i < newVerticalBlanks && !logo.Finished; i++)
		{
			logo.VerticalBlank();

			if (logo.Song != song)
			{
				song = logo.Song;
				Game.PlaySong(song == 0 ? 0 : Game.ThalionLogoSong + song);
			}
		}

		ShowFrame();

		if (logo.Finished)
			Game.ThalionLogoFinished();
	}

	void ShowFrame()
	{
		Game.GetRenderLayer(Layer.TrueColor).Config.Texture!.UpdateGraphic(Game.GraphicIndexProvider.GetThalionLogoIndex(), logo.Frame);
	}
}
