using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

/// <summary>
/// The music spell lets the player choose a song (the module at $307ce in the program, not part of the
/// released source code). The names are shown in 2 columns, the played song is highlighted.
/// A click plays a song, a right click closes the window.
/// </summary>
internal sealed class JukeboxScreen() : WindowScreen(WindowX, WindowY, WindowWidthInTiles, WindowHeightInTiles, WindowDisplayLayer)
{
	const int WindowX = 16;
	const int WindowY = 64;
	const int WindowWidthInTiles = 14;
	const int WindowHeightInTiles = 7;
	const byte WindowDisplayLayer = 110;
	const byte ControlDisplayLayer = 120;
	// the songs of the first column, the positions of the columns and the lines (absolute)
	const int FirstColumnSongs = 10;
	static readonly int[] ColumnX = [32, 128];
	const int FirstLineY = 85;
	const int LineHeight = 7;
	const int ColumnWidth = 96;
	const int CurrentSongColorIndex = 1;
	const int SongColorIndex = 15;
	readonly List<(Label Label, Rect Area, int Song)> songs = [];

	public override ScreenType Type { get; } = ScreenType.Jukebox;

	public override bool CloseOnRightClick { get; } = true;

	public override void Open(Action? closeAction)
	{
		base.Open(closeAction);

		songs.Clear();
		var names = Game.AssetProvider.SongLoader.SongNames;
		var client = ClientArea.Position;

		for (int i = 0; i < names.Count; i++)
		{
			int column = i < FirstColumnSongs ? 0 : 1;
			int line = column == 0 ? i : i - FirstColumnSongs;
			var area = new Rect(ColumnX[column], FirstLineY + line * LineHeight, ColumnWidth, LineHeight);
			var label = AddLabel(area.Position.X - client.X, area.Position.Y - client.Y, ColumnWidth, LineHeight, ControlDisplayLayer);
			songs.Add((label, area, i + 1));
		}

		UpdateSongs();
	}

	void UpdateSongs()
	{
		var names = Game.AssetProvider.SongLoader.SongNames;
		byte palette = Game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI];

		foreach (var (label, _, song) in songs)
		{
			int color = song == Game.CurrentSong ? CurrentSongColorIndex : SongColorIndex;
			label.SetText(names[song - 1], color, TextManager.TransparentPaper, palette);
			label.Visible = true;
		}
	}

	internal void ScriptChooseSong(int song)
	{
		Game.PlaySong(song);
		UpdateSongs();
	}

	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		if (buttons == MouseButtons.Left)
		{
			foreach (var (_, area, song) in songs)
			{
				if (area.Contains(position))
				{
					ScriptChooseSong(song);
					return true;
				}
			}
		}

		return base.MouseDown(position, buttons, keyModifiers);
	}
}
