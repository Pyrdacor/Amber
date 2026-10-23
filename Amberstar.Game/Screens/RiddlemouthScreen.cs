using Amber.Common;
using Amberstar.Game.Events;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

/// <summary>
/// The riddlemouth asks a riddle which is answered by selecting a word.
///
/// Like in the original (Riddle_mouth in EVENTS.ASM) the right answer teaches
/// the word, optionally changes a map tile, shows a text and disables the event.
/// </summary>
internal sealed class RiddlemouthScreen : ButtonGridScreen, IWaitForClickScreen
{
	const int ExitButtonIndex = 2;
	const int AnswerButtonIndex = 6;
	const int AskButtonIndex = 8;
	static readonly Rect messageDisplayArea = new(16, 49, 176, 144);
	Label? message;
	RiddleMouthEvent? riddleMouthEvent;
	bool waitForClick = false;
	Action? afterClickAction = null;

	public override ScreenType Type { get; } = ScreenType.Riddlemouth;

	internal override byte ButtonGridPaletteIndex => Game?.PaletteIndexProvider.Get80x80ImagePaletteIndex(Image80x80.Riddlemouth) ?? 0;

	public bool WaitingForClick => waitForClick;

	readonly TextScrollHandler textScrollHandler = new();

	public override void Init()
	{
		base.Init();

		// long messages are scrolled before they are closed
		textScrollHandler.ScrollEnded += EndClickWait;

		var image = AddImage(224, 49, 80, 80, Game.GraphicIndexProvider.Get80x80ImageIndex(Image80x80.Riddlemouth), 0, true);
		image.PaletteIndex = ButtonGridPaletteIndex;

		var (x, y, width, height) = messageDisplayArea;
		message = AddLabel(x, y, width, height, 20);
	}

	public override void Open(Action? closeAction)
	{
		base.Open(closeAction);

		riddleMouthEvent = (Game.EventHandler.CurrentEvent as RiddleMouthEvent)!;
		waitForClick = false;
		afterClickAction = null;

		Game.SetLayout(Layout.Riddlemouth, ButtonGridPaletteIndex);
		Game.Cursor.CursorType = CursorType.Sword;

		Ask();
	}

	public override void Close()
	{
		message!.Visible = false;
		Game.UntrapMouse();

		base.Close();
	}

	public override void ScreenPopped(Screen screen)
	{
		base.ScreenPopped(screen);

		if (!screen.Transparent)
		{
			Game.SetLayout(Layout.Riddlemouth, ButtonGridPaletteIndex);
			Game.Cursor.CursorType = CursorType.Sword;
		}

		if (screen is SelectWordScreen && !string.IsNullOrWhiteSpace(Game.CurrentWord))
			CheckAnswer(Game.CurrentWord);
	}

	protected override void SetupButtons(ButtonGrid buttonGrid)
	{
		for (int i = 0; i < 9; i++)
			buttonGrid.SetButton(i, ButtonType.Empty);

		buttonGrid.SetButton(ExitButtonIndex, ButtonType.Exit);
		buttonGrid.SetButton(AnswerButtonIndex, ButtonType.Mouth);
		buttonGrid.SetButton(AskButtonIndex, ButtonType.Ear);
	}

	protected override void ButtonClicked(int index)
	{
		switch (index)
		{
			case ExitButtonIndex:
				Game.ScreenHandler.PopScreen();
				break;
			case AnswerButtonIndex:
				Game.CurrentWord = null;
				Game.ScreenHandler.PushScreen(ScreenType.SelectWord);
				break;
			case AskButtonIndex:
				Ask();
				break;
		}
	}

	private void Ask()
	{
		if (riddleMouthEvent!.RiddleTextIndex >= TextEventExtensions.NoText)
			return;

		ShowText(Game.GetMapText(Game.EventHandler.CurrentEventMapIndex, riddleMouthEvent.RiddleTextIndex), false);
	}

	/// <summary>
	/// Checks the given answer. This is also used by scenario scripts.
	/// </summary>
	internal void CheckAnswer(string word)
	{
		var wordIndex = Game.AssetProvider.TextLoader.FindWord(word.Trim());

		if (wordIndex != riddleMouthEvent!.WordIndex)
		{
			ShowText(Game.LoadMessageText(Message.WrongAnswer), true);
			return;
		}

		Game.State.LearnWord(wordIndex.Value);

		if (riddleMouthEvent.IconIndex != 0)
			Game.ChangeMapTile(riddleMouthEvent.X, riddleMouthEvent.Y, riddleMouthEvent.IconIndex, riddleMouthEvent.SaveEvent);

		Game.SaveEvent(riddleMouthEvent);

		void Close() => Game.ScreenHandler.PopScreen();

		if (riddleMouthEvent.SolvedTextIndex < TextEventExtensions.NoText)
			ShowText(Game.GetMapText(Game.EventHandler.CurrentEventMapIndex, riddleMouthEvent.SolvedTextIndex), true, Close);
		else
			Close();
	}

	private void ShowText(IText text, bool waitForClick, Action? afterClickAction = null)
	{
		Game.ScriptTextShown(text);
		message!.SetText(text, messageDisplayArea.Size.Width, 15, TextManager.TransparentPaper, ButtonGridPaletteIndex);
		message.Visible = true;

		if (waitForClick)
		    textScrollHandler.Attach(message);

		this.waitForClick = waitForClick;
		this.afterClickAction = afterClickAction;

		if (waitForClick)
		{
			Game.Cursor.CursorType = CursorType.Zzz;
			Game.TrapMouse(messageDisplayArea);
		}
	}

	private void EndClickWait()
	{
		waitForClick = false;
		textScrollHandler.Detach();
		Game.Cursor.CursorType = CursorType.Sword;
		message!.Visible = false;
		Game.UntrapMouse();

		var action = afterClickAction;
		afterClickAction = null;
		action?.Invoke();
	}

	public override bool KeyDown(Key key, KeyModifiers keyModifiers)
	{
		if (waitForClick)
		{
			if (textScrollHandler.KeyDown(key, keyModifiers))
			    return true;

			if (key == Key.Space || key == Key.Escape || key == Key.Enter)
				EndClickWait();

			return true;
		}

		return base.KeyDown(key, keyModifiers);
	}

	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		if (waitForClick)
		{
			if (textScrollHandler.MouseDown(buttons))
			    return true;

			EndClickWait();
			return true;
		}

		return base.MouseDown(position, buttons, keyModifiers);
	}
}
