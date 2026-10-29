using Amber.Assets.Common;

namespace Amberstar.GameData;

/// <summary>
/// The logo of Thalion (THALION.68K, a separate program of the original which starts before the intro). Rain falls on
/// a window, the logo is assembled and the name bounces in until a key is pressed. The program draws every frame
/// into the screen memory, so it is simulated like the original and gives the shown frames.
/// </summary>
public interface IThalionLogo
{
	public const int Width = 320;
	public const int Height = 200;

	/// <summary>
	/// Starts the logo from the beginning.
	/// </summary>
	void Reset();

	/// <summary>
	/// Runs the logo for one vertical blank (50 per second) and updates <see cref="Frame"/>.
	/// </summary>
	void VerticalBlank();

	/// <summary>
	/// A key was pressed (scan code of the Atari ST keyboard) or released. Any key ends the logo
	/// (in the end only the space key, other keys are checked for 2 secret words).
	/// </summary>
	void KeyPressed(int scanCode);
	void KeyReleased();

	/// <summary>
	/// The fire button of the joystick (or the mouse button in the port) ends the logo.
	/// </summary>
	void Skip();

	bool Finished { get; }

	/// <summary>
	/// The shown frame (RGBA).
	/// </summary>
	IGraphic Frame { get; }

	/// <summary>
	/// The song which is played (1-based, 0 if no song is played).
	/// </summary>
	int Song { get; }

	ISong GetSong(int song);
}
