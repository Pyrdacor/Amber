using Amber.Assets.Common;

namespace Amberstar.GameData;

/// <summary>
/// The graphics of the intro. The title pictures (one per fading step) have their own colors, the others use the
/// palette of the background.
/// </summary>
public enum IntroGraphic
{
	TitlePicture,
	Background,
	Logo,
	TextPage
}

/// <summary>
/// The intro (INTRO.68K, a separate program of the original). The title picture is shown until the space key
/// is pressed. Then the credits are shown page by page on a stone background with the logo of the game while
/// the music plays, until the space key is pressed.
/// </summary>
public interface IIntroData
{
	public const int PictureWidth = 320;
	public const int PictureHeight = 200;

	/// <summary>
	/// The title picture (RGBA) for each fading step from dark (0) to the normal picture
	/// (<see cref="TitleFadeSteps"/> - 1). It has 48 colors per line like the pictures of Spectrum 512
	/// (the palette is changed 3 times in each line).
	/// </summary>
	IReadOnlyList<IGraphic> TitlePictures { get; }
	public const int TitleFadeSteps = 7;

	/// <summary>
	/// The stone background (320x200).
	/// </summary>
	IGraphic Background { get; }

	/// <summary>
	/// Gets the palette of the background while it fades: 0 is the normal palette, 1 to <see cref="FadeSteps"/> - 1
	/// fade in from black (each color channel is increased by 1 per step), -1 to -<see cref="FadeSteps"/> fade out
	/// (each channel is decreased by 1 per step).
	/// </summary>
	IGraphic GetBackgroundPalette(int fadeStep = 0);
	public const int FadeSteps = 7;

	/// <summary>
	/// The logo of the game (320x36, color 0 is transparent) which is drawn line by line at the top.
	/// </summary>
	IGraphic Logo { get; }
	public const int LogoY = 17;

	/// <summary>
	/// The texts of the credits (lines separated by '\n').
	/// </summary>
	IReadOnlyList<string> Texts { get; }

	/// <summary>
	/// The credit pages: the part of the background from <see cref="TextAreaY"/> with the text of the page.
	/// </summary>
	IReadOnlyList<IGraphic> TextPages { get; }
	public const int TextAreaY = 75;
	public const int TextAreaHeight = 112;

	ISong? Music { get; }
}
