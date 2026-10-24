using Amber.Common;
using Amber.Renderer.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

/// <summary>
/// The altar where the 12 pieces of the Amberstar are laid around the 13th piece in a given order.
///
/// The routines are not part of the released original source code. This follows the program:
/// The player browses through the remaining pieces and lays the selected one. Each piece has
/// a fixed place, a sign and a name. A voice asks for the next piece by a description.
/// The right piece rewards the active party member, a wrong one turns him into ashes.
/// There is no way to leave the altar before the Amberstar is assembled.
/// </summary>
internal sealed class AltarScreen : ButtonGridScreen
{
	const int RepeatButtonIndex = 1;
	const int PreviousButtonIndex = 6;
	const int LayButtonIndex = 7;
	const int NextButtonIndex = 8;
	const int WelcomeTextIndex = 2;
	const int RightPieceTextIndex = 0;
	const int WrongPieceTextIndex = 1;
	const int AssembledTextIndex = 3;
	const int AllPieces = (1 << IAltarPuzzle.PieceCount) - 1;
	static readonly Position AltarPosition = new(16, 49);
	static readonly Position CenterOffset = new(41, 25);
	static readonly Position SelectedPiecePosition = new(224, 57);
	static readonly Rect PieceNameArea = new(208, 118, 96, 7);
	const byte SelectedPieceBackgroundColorIndex = 2;
	IAltarPuzzle? puzzle;
	readonly Image?[] pieces = new Image?[IAltarPuzzle.PieceCount];
	Image? selectedPiece;
	Image? selectedSign;
	Label? pieceName;
	IColoredRect? selectedPieceBackground;
	int step = 0;
	int selectedPieceIndex = 0;
	int remainingPieces = AllPieces;

	public override ScreenType Type { get; } = ScreenType.Altar;

	// The altar can't be left before the Amberstar is assembled.
	public override bool CloseOnEscape { get; } = false;

	internal override byte ButtonGridPaletteIndex => Game?.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.Puzzle] ?? 0;

	/// <summary>
	/// The 0-based piece which is selected to be laid next.
	/// </summary>
	internal int SelectedPieceIndex => selectedPieceIndex;

	/// <summary>
	/// The number of pieces which were laid at the right places.
	/// </summary>
	internal int Step => step;

	public override void Open(Action? closeAction)
	{
		base.Open(closeAction);

		puzzle = Game.AssetProvider.AltarPuzzle!;
		step = 0;
		selectedPieceIndex = 0;
		remainingPieces = AllPieces;

		Game.SetLayout(Layout.Map2D, ButtonGridPaletteIndex);
		Game.Cursor.CursorType = CursorType.Sword;
		Game.PlaySong(Game.AltarSong);

		var graphicIndexProvider = Game.GraphicIndexProvider;
		var palette = ButtonGridPaletteIndex;

		Image AddPuzzleImage(Position position, Size size, PuzzleGraphic graphic, int index, byte displayLayer, bool opaque = false)
		{
			var image = AddImage(position.X, position.Y, size.Width, size.Height, graphicIndexProvider.GetPuzzleGraphicIndex(graphic, index), displayLayer, opaque);
			image.PaletteIndex = palette;
			return image;
		}

		AddPuzzleImage(AltarPosition, new(176, 144), PuzzleGraphic.Background, 0, 0, true);
		AddPuzzleImage(AltarPosition + CenterOffset, new(96, 96), PuzzleGraphic.Center, 0, 1);

		for (int i = 0; i < IAltarPuzzle.PieceCount; i++)
		{
			pieces[i] = AddPuzzleImage(AltarPosition + puzzle.PiecePositions[i], new(32, 32), PuzzleGraphic.FirstPiece, i, 2);
			pieces[i]!.Visible = false;
		}

		var backgroundColor = Game.PaletteColorProvider.GetPaletteColor(palette, SelectedPieceBackgroundColorIndex);
		selectedPieceBackground = Game.CreateColoredRect(Layer.UI, SelectedPiecePosition, new(64, 32), backgroundColor);

		if (selectedPieceBackground != null)
			selectedPieceBackground.DisplayLayer = 0;

		selectedPiece = AddPuzzleImage(SelectedPiecePosition, new(32, 32), PuzzleGraphic.FirstPiece, 0, 1);
		selectedSign = AddPuzzleImage(SelectedPiecePosition + new Position(32, 0), new(32, 32), PuzzleGraphic.FirstSign, 0, 1);

		var (x, y, width, height) = PieceNameArea;
		pieceName = AddLabel(x, y, width, height, 2);
		pieceName.Alignment = TextAlignment.Center;

		UpdateSelectedPiece();
		ShowText(WelcomeTextIndex, AskForPiece);
	}

	public override void Close()
	{
		Game.Destroy(selectedPieceBackground);
		selectedPieceBackground = null;

		base.Close();
	}

	public override void ScreenPushed(Screen screen)
	{
		base.ScreenPushed(screen);

		if (!screen.Transparent && selectedPieceBackground != null)
			selectedPieceBackground.Visible = false;
	}

	public override void ScreenPopped(Screen screen)
	{
		base.ScreenPopped(screen);

		if (!screen.Transparent)
		{
			Game.SetLayout(Layout.Map2D, ButtonGridPaletteIndex);
			Game.Cursor.CursorType = CursorType.Sword;

			if (selectedPieceBackground != null)
				selectedPieceBackground.Visible = true;
		}
	}

	protected override void SetupButtons(ButtonGrid buttonGrid)
	{
		for (int i = 0; i < 9; i++)
			buttonGrid.SetButton(i, ButtonType.Empty);

		buttonGrid.SetButton(RepeatButtonIndex, ButtonType.Ear);
		buttonGrid.SetButton(PreviousButtonIndex, ButtonType.ArrowLeft);
		buttonGrid.SetButton(LayButtonIndex, ButtonType.UseItem);
		buttonGrid.SetButton(NextButtonIndex, ButtonType.ArrowRight);

		// Only one piece is left in the last step.
		bool canBrowse = step < IAltarPuzzle.PieceCount - 1;
		buttonGrid.EnableButton(PreviousButtonIndex, canBrowse);
		buttonGrid.EnableButton(NextButtonIndex, canBrowse);
	}

	protected override void ButtonClicked(int index)
	{
		switch (index)
		{
			case RepeatButtonIndex:
				AskForPiece();
				break;
			case PreviousButtonIndex:
				SelectPiece(-1);
				break;
			case LayButtonIndex:
				LayPiece();
				break;
			case NextButtonIndex:
				SelectPiece(1);
				break;
		}
	}

	private void ShowText(int index, Action? followAction = null)
	{
		var text = Game.AssetProvider.TextLoader.LoadText(new(AssetType.PuzzleText, 1)).GetTextBlock(index);
		Game.ShowTextMessage(text, followAction == null ? null : () => Game.AfterScreenClosed(followAction));
	}

	private void AskForPiece() => ShowText(puzzle!.StepTextIndices[step]);

	/// <summary>
	/// Selects the previous or next remaining piece. Nothing happens if there is none.
	/// </summary>
	private bool SelectPiece(int direction)
	{
		for (int index = selectedPieceIndex + direction; index >= 0 && index < IAltarPuzzle.PieceCount; index += direction)
		{
			if ((remainingPieces & (1 << index)) != 0)
			{
				selectedPieceIndex = index;
				UpdateSelectedPiece();
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Browses to the remaining piece with the given name like the arrow buttons (used by scenario scripts).
	/// </summary>
	internal bool ScriptSelectPiece(string name)
	{
		int piece = Enumerable.Range(0, IAltarPuzzle.PieceCount).FirstOrDefault(index => puzzle!.PieceNames[index].Equals(name, StringComparison.OrdinalIgnoreCase), -1);

		if (piece == -1 || (remainingPieces & (1 << piece)) == 0)
			return false;

		while (selectedPieceIndex != piece)
			SelectPiece(piece < selectedPieceIndex ? -1 : 1);

		return true;
	}

	internal string SelectedPieceName => puzzle!.PieceNames[selectedPieceIndex];

	private void UpdateSelectedPiece()
	{
		var graphicIndexProvider = Game.GraphicIndexProvider;

		selectedPiece!.SetTextureIndex(graphicIndexProvider.GetPuzzleGraphicIndex(PuzzleGraphic.FirstPiece, selectedPieceIndex));
		selectedSign!.SetTextureIndex(graphicIndexProvider.GetPuzzleGraphicIndex(PuzzleGraphic.FirstSign, puzzle!.PieceSigns[selectedPieceIndex]));
		pieceName!.SetText(puzzle.PieceNames[selectedPieceIndex], TextManager.DefaultInkColorIndex, TextManager.TransparentPaper, ButtonGridPaletteIndex);
		pieceName.Visible = true;
	}

	private void LayPiece()
	{
		var partyMember = Game.State.ActivePartyMember!;

		if (selectedPieceIndex != puzzle!.PieceOrder[step])
		{
			ShowText(WrongPieceTextIndex, () => Game.BurnAtAltar(partyMember));
			return;
		}

		pieces[selectedPieceIndex]!.Visible = true;

		ShowText(RightPieceTextIndex, () =>
		{
			Game.RewardAltarPiece(partyMember, step);

			if (++step == IAltarPuzzle.PieceCount)
			{
				Game.AssembleAmberstar();
				ShowText(AssembledTextIndex, () => Game.ScreenHandler.PopScreen());
				return;
			}

			remainingPieces &= ~(1 << selectedPieceIndex);

			if (!SelectPiece(1))
				SelectPiece(-1);

			RequestButtonSetup();
			AskForPiece();
		});
	}
}
