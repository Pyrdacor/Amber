using Amber.Common;
using Amber.Renderer.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

public enum ScreenType
{
	CharacterCreation,
	Map2D,
	Map3D,
	Inventory,
	CharacterStats,
	Camp,
	BattlePositions,
	Door,
	Chest,
	PictureText,
	TextBox,
	Conversation,
	Place,
    ItemView,
    ItemDetails,
    SelectWord,
    InputWord,
    // Inventory sub screens
    InventoryDropItem,
	// Door/chest sub screens
	LockedUseItem,
	ChestGiveItem,
    ChestExamineItem,
    ChestGiveGold,
    // TODO ...
}

public enum ScreenFadeType
{
	None,
	In,
	Out,
	Both,
}

internal abstract class Screen
{
	Action? closeAction;
	Game? game;
    readonly Stack<Position> anchors = [];
    readonly List<Control> createdControlsInInit = [];
    readonly List<Control> createdControlsInOpen = [];
    readonly List<bool> createdControlsInInitVisibility = [];
    readonly List<bool> createdControlsInOpenVisibility = [];
    bool initialized = false;

    private IEnumerable<Control> CreatedControls => createdControlsInOpen.Concat(createdControlsInInit);

    public abstract ScreenType Type { get; }

	public virtual ScreenFadeType FadeType { get; } = ScreenFadeType.Both;

    public virtual bool Transparent { get; } = false;

    public Game Game => game!;

    public virtual bool AllowCharacterSelection { get; } = true;

    public virtual bool AllowInventoryAccess { get; } = true;

    public virtual bool CloseOnEscape { get; } = true;

    public virtual bool CloseOnRightClick { get; } = false;

    public virtual bool CloseOnSpace => CloseOnRightClick;

    protected void SetCloseAction(Action? closeAction) => this.closeAction = closeAction;

	public virtual void Init()
	{
        // Default: empty
    }

    public void PreInit(Game game)
    {
        this.game = game;
    }

    public void AfterInit()
    {
        initialized = true;

        createdControlsInInitVisibility.Clear();
        createdControlsInInitVisibility.AddRange(createdControlsInInit.Select(control => control.Visible));
        // Hide all created controls so far, otherwise they are
        // already visible during fading when opening the screen
        // for the first time.
        createdControlsInInit.ForEach(control => control.Visible = false);
    }

    public virtual void Destroy()
	{
        createdControlsInInit.ForEach(control => control.Destroy());
        createdControlsInInit.Clear();
        createdControlsInInitVisibility.Clear();
    }

    public void PreOpen()
    {
        int index = 0;
        createdControlsInInit.ForEach(control => control.Visible = createdControlsInInitVisibility[index++]);
    }

    public virtual void Open(Action? closeAction)
	{
        SetCloseAction(closeAction);
	}

	public virtual void Close()
	{
        createdControlsInInit.ForEach(control => control.Visible = false);
        createdControlsInOpen.ForEach(control => control.Destroy());
        createdControlsInOpen.Clear();
        createdControlsInOpenVisibility.Clear();

        closeAction?.Invoke();
	}

	public virtual void ScreenPushed(Screen screen)
	{
        if (!screen.Transparent)
        {
            createdControlsInInitVisibility.Clear();
            createdControlsInInitVisibility.AddRange(createdControlsInInit.Select(control => control.Visible));
            createdControlsInOpenVisibility.Clear();
            createdControlsInOpenVisibility.AddRange(createdControlsInOpen.Select(control => control.Visible));
            createdControlsInInit.ForEach(control => control.Visible = false);
            createdControlsInOpen.ForEach(control => control.Visible = false);
        }
    }

	public virtual void ScreenPopped(Screen screen)
	{
        if (!screen.Transparent)
        {
            int index = 0;
            createdControlsInInit.ForEach(control => control.Visible = createdControlsInInitVisibility[index++]);
            index = 0;
            createdControlsInOpen.ForEach(control => control.Visible = createdControlsInOpenVisibility[index++]);
        }
    }

	public virtual void Update(long elapsedTicks)
	{
		// default: empty
	}

	public virtual bool KeyDown(Key key, KeyModifiers keyModifiers)
	{
		if (AllowInventoryAccess && key >= Key.F1 && key <= Key.F6)
        {
            int characterSlotIndex = 1 + (key - Key.F1);

            if (Game.State.HasPartyMemberInSlot(characterSlotIndex))
                Game.OpenInventory(1 + (key - Key.F1));

            return true;
        }

		if (CloseOnEscape && key == Key.Escape && keyModifiers == KeyModifiers.None)
		{
			Game.ScreenHandler.PopScreen();
			return true;
		}

        if (CloseOnSpace && key == Key.Space && keyModifiers == KeyModifiers.None)
        {
            Game.ScreenHandler.PopScreen();
            return true;
        }

        return false;
	}

	public virtual bool KeyUp(Key key, KeyModifiers keyModifiers)
	{
		// default: empty
		return false;
	}

	public virtual bool KeyChar(char ch, KeyModifiers keyModifiers)
	{
		if (AllowCharacterSelection && keyModifiers == KeyModifiers.None && ch >= '1' && ch <= '6')
		{
			Game.State.SetActivePartyMember(ch - '0');
			return true;
        }

		return false;
	}

	public virtual bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		if (CloseOnRightClick && buttons == MouseButtons.Right)
		{
            Game?.ScreenHandler.PopScreen();
            return true;
        }

        foreach (var button in CreatedControls.OfType<Button>().Where(button => button.Visible && !button.Disabled))
        {
            if (button.MouseClick(position, buttons))
                return true;
        }

        return false;
	}

	public virtual bool MouseUp(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
        // default: empty
        return false;
    }

	public virtual void MouseMove(Position position, MouseButtons buttons)
	{
        // default: empty
    }

	public virtual bool MouseWheel(Position position, float scrollX, float scrollY, MouseButtons buttons)
	{
		// default: empty
		return false;
	}

    // TODO: Remove these and replace usage with Image/Label
	#region Helper functions
	protected static void ShowSprites(IEnumerable<ISprite?> sprites, bool show = true)
    {
        foreach (var sprite in sprites)
        {
            if (sprite != null)
                sprite.Visible = show;
        }
    }

    protected static void ShowTexts(IEnumerable<IRenderText?> texts, bool show = true)
    {
        foreach (var text in texts)
        {
            if (text != null)
                text.Visible = show;
        }
    }

	protected static void DeleteSprites(IEnumerable<ISprite?> sprites) => ShowSprites(sprites, false);

    protected static void DeleteTexts(IEnumerable<IRenderText?> texts)
    {
        foreach (var text in texts)
        {
			text?.Delete();
        }
    }
    #endregion

    #region Controls

    public void SetAnchorToWindow(Window window)
    {
        var clientArea = window.ClientArea;
        SetAnchor(clientArea.Left, clientArea.Top);
    }

    public void SetAnchor(int x, int y)
    {
        anchors.Clear();
        PushAnchor(x, y);
    }

    public void PushAnchor(int x, int y)
    {
        anchors.Push(new(x, y));
    }

    public void PopAnchor()
    {
        anchors.Pop();
    }

    public Button AddButton(int x, int y, ButtonType buttonType, byte displayLayer = 0)
    {
        int baseX = 0;
        int baseY = 0;

        if (anchors.Count != 0)
            (baseX, baseY) = anchors.Peek();

        var button = new Button(Game, baseX + x, baseY + y, buttonType, displayLayer);
        var controls = initialized ? createdControlsInOpen : createdControlsInInit;

        controls.Add(button);

        return button;

    }

    public Button AddButton(ref int x, int y, ButtonType buttonType, byte displayLayer = 0)
    {
        var button = AddButton(x, y, buttonType, displayLayer);
        x += Button.Width;
        return button;
    }

    public Button AddButton(int x, ref int y, ButtonType buttonType, byte displayLayer = 0)
    {
        var button = AddButton(x, y, buttonType, displayLayer);
        y += Button.Height;
        return button;
    }

    public Button AddButton(ref int x, ref int y, ButtonType buttonType, byte displayLayer = 0)
    {
        var button = AddButton(x, y, buttonType, displayLayer);
        x += Button.Width;
        y += Button.Height;
        return button;
    }

    public Image AddImage(int x, int y, int width, int height, int textureIndex, byte displayLayer = 0, bool opaque = false, Layer layer = Layer.UI)
    {
        int baseX = 0;
        int baseY = 0;

        if (anchors.Count != 0)
            (baseX, baseY) = anchors.Peek();

        var image = new Image(Game, baseX + x, baseY + y, width, height, textureIndex, displayLayer, layer, null, opaque);
        var controls = initialized ? createdControlsInOpen : createdControlsInInit;

        controls.Add(image);

        return image;
    }

    public Image AddImage(ref int x, int y, int width, int height, int textureIndex, byte displayLayer = 0, bool opaque = false, Layer layer = Layer.UI)
    {
        var image = AddImage(x, y, width, height, textureIndex, displayLayer, opaque, layer);
        x += width;
        return image;
    }

    public Image AddImage(int x, ref int y, int width, int height, int textureIndex, byte displayLayer = 0, bool opaque = false, Layer layer = Layer.UI)
    {
        var image = AddImage(x, y, width, height, textureIndex, displayLayer, opaque, layer);
        y += height;
        return image;
    }

    public Image AddImage(ref int x, ref int y, int width, int height, int textureIndex, byte displayLayer = 0, bool opaque = false, Layer layer = Layer.UI)
    {
        var image = AddImage(x, y, width, height, textureIndex, displayLayer, opaque, layer);
        x += width;
        y += height;
        return image;
    }

    public ItemContainer AddItem(int x, int y, IItem? item = null, int count = 1, byte displayLayer = 0)
    {
        int baseX = 0;
        int baseY = 0;

        if (anchors.Count != 0)
            (baseX, baseY) = anchors.Peek();

        var itemContainer = new ItemContainer(Game, new(baseX + x, baseY + y), item == null ? 0 : count, item, displayLayer)
        {
            DisplayLayer = displayLayer,
            Visible = true
        };
        var controls = initialized ? createdControlsInOpen : createdControlsInInit;

        controls.Add(itemContainer);

        return itemContainer;
    }

    public ItemContainer AddItem(ref int x, int y, IItem? item = null, int count = 1, byte displayLayer = 0)
    {
        var itemContainer = AddItem(x, y, item, count, displayLayer);
        x += 16;
        return itemContainer;
    }

    public ItemContainer AddItem(int x, ref int y, IItem? item = null, int count = 1, byte displayLayer = 0)
    {
        var itemContainer = AddItem(x, y, item, count, displayLayer);
        y += 16;
        return itemContainer;
    }

    public ItemContainer AddItem(ref int x, ref int y, IItem? item = null, int count = 1, byte displayLayer = 0)
    {
        var itemContainer = AddItem(x, y, item, count, displayLayer);
        x += 16;
        y += 16;
        return itemContainer;
    }

    public Label AddLabel(int x, int y, int width, int height, byte displayLayer = 0)
    {
        int baseX = 0;
        int baseY = 0;

        if (anchors.Count != 0)
            (baseX, baseY) = anchors.Peek();

        var label = new Label(Game)
        {
            Area = new(baseX + x, baseY + y, width, height),
            DisplayLayer = displayLayer,
            Visible = true
        };
        var controls = initialized ? createdControlsInOpen : createdControlsInInit;

        controls.Add(label);

        return label;
    }

    public Label AddLabel(ref int x, int y, int width, int height, byte displayLayer = 0)
    {
        var label = AddLabel(x, y, width, height, displayLayer);
        x += width;
        return label;
    }

    public Label AddLabel(int x, ref int y, int width, int height, byte displayLayer = 0)
    {
        var label = AddLabel(x, y, width, height, displayLayer);
        y += height;
        return label;
    }

    public Label AddLabel(ref int x, ref int y, int width, int height, byte displayLayer = 0)
    {
        var label = AddLabel(x, y, width, height, displayLayer);
        x += width;
        y += height;
        return label;
    }

    public Label AddLabel(int x, int y, IText text, int? width = null, int? height = null, byte displayLayer = 0)
    {
        int baseX = 0;
        int baseY = 0;

        if (anchors.Count != 0)
            (baseX, baseY) = anchors.Peek();

        width ??= Game.GetMaxLineWidth(text);
        height ??= 7;
        var label = new Label(Game)
        {
            Area = new(baseX + x, baseY + y, width.Value, height.Value),
            DisplayLayer = displayLayer,
            Visible = true
        };
        label.SetText(text, width, 15, TextManager.TransparentPaper, Game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI]);
        var controls = initialized ? createdControlsInOpen : createdControlsInInit;

        controls.Add(label);

        return label;
    }

    public Label AddLabel(ref int x, int y, IText text, int? width = null, int? height = null, byte displayLayer = 0)
    {
        var label = AddLabel(x, y, text, width, height, displayLayer);
        x += label.Area.Size.Width;
        return label;
    }

    public Label AddLabel(int x, ref int y, IText text, int? width = null, int? height = null, byte displayLayer = 0)
    {
        var label = AddLabel(x, y, text, width, height, displayLayer);
        y += label.Area.Size.Height;
        return label;
    }

    public Label AddLabel(ref int x, ref int y, IText text, int? width = null, int? height = null, byte displayLayer = 0)
    {
        var label = AddLabel(x, y, text, width, height, displayLayer);
        var size = label.Area.Size;
        x += size.Width;
        y += size.Height;
        return label;
    }

    public Label AddLabel(int x, int y, string text, int? width = null, int? height = null, byte displayLayer = 0)
    {
        int GetMaxLineWidth()
        {
            int maxLineLength = text.Split('\n').Max(line => line.Length);
            return maxLineLength * 6;
        }

        int baseX = 0;
        int baseY = 0;

        if (anchors.Count != 0)
            (baseX, baseY) = anchors.Peek();

        width ??= GetMaxLineWidth();
        height ??= 7;
        var label = new Label(Game)
        {
            Area = new(baseX + x, baseY + y, width.Value, height.Value),
            DisplayLayer = displayLayer,
            Visible = true
        };
        label.SetText(text, 15, TextManager.TransparentPaper, Game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI]);
        var controls = initialized ? createdControlsInOpen : createdControlsInInit;

        controls.Add(label);

        return label;
    }

    public Label AddLabel(ref int x, int y, string text, int? width = null, int? height = null, byte displayLayer = 0)
    {
        var label = AddLabel(x, y, text, width, height, displayLayer);
        x += label.Area.Size.Width;
        return label;
    }

    public Label AddLabel(int x, ref int y, string text, int? width = null, int? height = null, byte displayLayer = 0)
    {
        var label = AddLabel(x, y, text, width, height, displayLayer);
        y += label.Area.Size.Height;
        return label;
    }

    public Label AddLabel(ref int x, ref int y, string text, int? width = null, int? height = null, byte displayLayer = 0)
    {
        var label = AddLabel(x, y, text, width, height, displayLayer);
        var size = label.Area.Size;
        x += size.Width;
        y += size.Height;
        return label;
    }

    public List AddList(int x, int y, int width, int height, byte displayLayer = 0, int? backgroundColorIndex = null, byte? paletteIndex = null)
    {
        int baseX = 0;
        int baseY = 0;

        if (anchors.Count != 0)
            (baseX, baseY) = anchors.Peek();

        var list = new List(Game, baseX + x, baseY + y, width, height, displayLayer, backgroundColorIndex, paletteIndex)
        {
            Visible = true
        };

        var controls = initialized ? createdControlsInOpen : createdControlsInInit;

        controls.Add(list);

        return list;
    }

    public Input AddInput(int x, int y, int width, byte displayLayer = 0, int? maxLength = null)
    {
        int baseX = 0;
        int baseY = 0;

        if (anchors.Count != 0)
            (baseX, baseY) = anchors.Peek();

        var list = new Input(Game, baseX + x, baseY + y, width, displayLayer, maxLength)
        {
            Visible = true
        };

        var controls = initialized ? createdControlsInOpen : createdControlsInInit;

        controls.Add(list);

        return list;
    }

    public Window AddWindow(int x, int y, int widthInTiles, int heightInTiles, bool dark = false, byte displayLayer = 0, byte? paletteIndex = null)
    {
        int baseX = 0;
        int baseY = 0;

        if (anchors.Count != 0)
            (baseX, baseY) = anchors.Peek();

        var window = new Window(Game, baseX + x, baseY + y, widthInTiles, heightInTiles, dark, displayLayer, paletteIndex);
        var controls = initialized ? createdControlsInOpen : createdControlsInInit;

        controls.Add(window);

        return window;
    }

    #endregion
}

internal class ScreenHandler(Game Game) : IDisposable
{
	readonly Stack<Screen> screens = [];
	readonly Dictionary<ScreenType, Screen> createdScreens = [];

	public Screen? ActiveScreen => screens.Count == 0 ? null : screens.Peek();
	public Screen? LastScreen => screens.Skip(1).FirstOrDefault();

	public event Action<Screen>? ScreenChanged;

	public Screen Create(ScreenType screenType)
	{
		Screen screen = screenType switch
		{
			ScreenType.Map2D => new Map2DScreen(),
			ScreenType.Map3D => new Map3DScreen(),
            ScreenType.Door => new DoorScreen(),
            ScreenType.Chest => new ChestScreen(),
            ScreenType.PictureText => new PictureTextScreen(),
			ScreenType.TextBox => new TextBoxScreen(),
            ScreenType.Inventory => new InventoryScreen(),
            ScreenType.CharacterStats => new CharacterStatsScreen(),
            ScreenType.Conversation => new ConversationScreen(),
			ScreenType.Place => new PlaceScreen(),
			ScreenType.ItemView => new ItemScreen(),
			ScreenType.ItemDetails => new ItemDetailsScreen(),
            ScreenType.SelectWord => new SelectWordScreen(),
            ScreenType.InputWord => new InputWordScreen(),
            // Inventory sub screens
            ScreenType.InventoryDropItem => new InventoryScreen.DropItemScreen(),
			// Door/chest sub screens
			ScreenType.LockedUseItem => new DoorScreen.UseItemScreen(), // Note: Doesn't matter if DoorScreen.UseItemScreen or ChestScreen.UseItemScreen
            ScreenType.ChestGiveItem => new ChestScreen.GiveItemScreen(),
			ScreenType.ChestExamineItem => new ChestScreen.ExamineItemScreen(),
			ScreenType.ChestGiveGold => new ChestScreen.GiveGoldScreen(),
            _ => throw new NotImplementedException()
		};

        screen.PreInit(Game);
		screen.Init();
        screen.AfterInit();
		createdScreens.Add(screenType, screen);

		return screen;
	}

	public bool PushScreen(ScreenType screenType, Action? followAction = null)
	{
		var currentScreen = ActiveScreen;

		if (currentScreen?.Type == screenType)
		{
			followAction?.Invoke();
			return false;
		}

		if (!createdScreens.TryGetValue(screenType, out var screen))
			screen = Create(screenType);

		void Push()
		{
			screens.Push(screen!);

			currentScreen?.ScreenPushed(screen!);
            screen!.PreOpen();
            screen.Open(followAction);

			ScreenChanged?.Invoke(screen);
        }

        bool transparent = currentScreen?.Transparent == true || screen.Transparent;
		bool fadeOut = currentScreen != null && (currentScreen.FadeType == ScreenFadeType.Out || currentScreen.FadeType == ScreenFadeType.Both);
        bool fadeIn = screen != null && (screen.FadeType == ScreenFadeType.In || screen.FadeType == ScreenFadeType.Both);

        if (!transparent && (fadeIn || fadeOut))
        {
			Game.Fade(Game.DefaultFadeTime, null, Push);
		}
		else
		{
			Push();
		}

        return true;
	}

	public Screen? PopScreen()
	{
		if (screens.Count == 0)
			return null;

        var screen = screens.Pop();
        var prevScreen = ActiveScreen;

        void Pop()
		{
			screen.Close();
            prevScreen?.ScreenPopped(screen);

            ScreenChanged?.Invoke(screen);
        }

        bool transparent = prevScreen?.Transparent == true || screen.Transparent;
        bool fadeIn = prevScreen != null && (prevScreen.FadeType == ScreenFadeType.In || prevScreen.FadeType == ScreenFadeType.Both);
        bool fadeOut = screen.FadeType == ScreenFadeType.Out || screen.FadeType == ScreenFadeType.Both;

        if (!transparent && (fadeIn || fadeOut))
        {
            Game.Fade(Game.DefaultFadeTime, null, Pop);
        }
        else
        {
            Pop();
        }

        return screen;
	}

	public void ClearAllScreens()
	{
		while (screens.Count != 0)
		{
			screens.Pop().Close();
		}
	}

	public void ReplaceScreen(ScreenType screenType, Action? followAction = null)
	{
		PopScreen();
		PushScreen(screenType, followAction);
	}

	public Screen? FindScreen(ScreenType screenType)
	{
		foreach (var screen in screens)
		{
			if (screen.Type == screenType)
				return screen;
		}

		return null;
	}

	public void Dispose()
	{
		createdScreens.Values.ToList().ForEach(screen => screen.Destroy());
		createdScreens.Clear();
	}
}
