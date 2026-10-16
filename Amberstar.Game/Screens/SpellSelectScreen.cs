using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

/// <summary>
/// Lets the spell caster choose the spell school and the spell (SCSelW and SpSelW in the original).
/// Possible spells show how often they can be cast. Spells which can't be cast at the current
/// location or which need too many spell points can't be chosen.
/// </summary>
internal sealed class SpellSelectScreen() : WindowScreen(WindowX, WindowY, WindowWidthInTiles, WindowHeightInTiles, WindowDisplayLayer)
{
	const int WindowX = 32;
	const int WindowY = 40;
	const int WindowWidthInTiles = 12;
	const int WindowHeightInTiles = 10;
	const byte WindowDisplayLayer = 110;
	const byte ControlDisplayLayer = 120;
	const int SpellNameLength = 22;
	const byte WrongLocationColorIndex = 1;
	const byte NotEnoughSpellPointsColorIndex = 2;
	List? list;
	readonly List<SpellSchool> schools = [];
	readonly List<(int SpellNumber, bool Possible)> spells = [];
	SpellSchool? shownSchool;

	public override ScreenType Type { get; } = ScreenType.SpellSelect;

	public override bool Dark { get; } = true;

	public override void Init()
	{
		base.Init();

		var (clientWidth, clientHeight) = ClientArea.Size;
		list = AddList(0, 0, clientWidth, clientHeight - Button.Height, ControlDisplayLayer, backgroundColorIndex: 3);
		list.ItemClicked += (index, _) => Select(index);

		int x = 0;
		int y = list.Area.Size.Height;

		var upButton = AddButton(ref x, y, ButtonType.ArrowUp, ControlDisplayLayer);
		upButton.ClickAction += () => list?.Scroll(-1);
		upButton.RightClickAction += () => list?.ScrollToBegin();

		var downButton = AddButton(ref x, y, ButtonType.ArrowDown, ControlDisplayLayer);
		downButton.ClickAction += () => list?.Scroll(1);
		downButton.RightClickAction += () => list?.ScrollToEnd();

		x = clientWidth - Button.Width;
		var exitButton = AddButton(ref x, y, ButtonType.Exit, ControlDisplayLayer);
		exitButton.ClickAction += () => Game.ScreenHandler.PopScreen();
	}

	public override void Open(Action? closeAction)
	{
		base.Open(closeAction);

		Game.SelectedSpell = null;
		schools.Clear();
		schools.AddRange(Game.GetSpellSchoolsWithKnownSpells(Game.SpellCaster!));

		if (schools.Count == 1)
			ShowSpells(schools[0]);
		else
			ShowSchools();
	}

	private void ShowSchools()
	{
		shownSchool = null;
		list!.Clear();

		foreach (var school in schools)
			list.AddItem(Game.AssetProvider.TextLoader.LoadText(new AssetIdentifier(AssetType.SpellSchoolName, (int)school)));
	}

	private void ShowSpells(SpellSchool school)
	{
		var caster = Game.SpellCaster!;
		int spellPoints = caster.SpellPoints.CurrentValue;

		shownSchool = school;
		spells.Clear();
		list!.Clear();

		for (int spellNumber = 1; spellNumber <= Game.GetSpellCount(school); spellNumber++)
		{
			if (!Game.HasLearnedSpell(caster, school, spellNumber))
				continue;

			var spellData = Game.LoadSpellData(school, spellNumber);
			var name = Game.GetFullText(Game.LoadSpellName(school, spellNumber)).PadRight(SpellNameLength);

			if (!Game.CanCastSpellAt(spellData, Game.SpellLocation))
			{
				list.AddItem(name, WrongLocationColorIndex);
				spells.Add((spellNumber, false));
			}
			else if (spellPoints < spellData.SpellPointCost)
			{
				list.AddItem(name, NotEnoughSpellPointsColorIndex);
				spells.Add((spellNumber, false));
			}
			else
			{
				int count = spellData.SpellPointCost == 0 ? 99 : Math.Min(99, spellPoints / spellData.SpellPointCost);
				list.AddItem($"{name}({count,2})");
				spells.Add((spellNumber, true));
			}
		}
	}

	private bool Select(int index)
	{
		if (shownSchool == null)
		{
			if (index < 0 || index >= schools.Count)
				return false;

			ShowSpells(schools[index]);
			return true;
		}

		if (index < 0 || index >= spells.Count || !spells[index].Possible)
			return false;

		Game.SelectedSpell = (shownSchool.Value, spells[index].SpellNumber);
		Game.ScreenHandler.PopScreen();
		return true;
	}

	/// <summary>
	/// Chooses a spell like a click on the school and the spell.
	/// </summary>
	internal string? ScriptSelectSpell(SpellSchool school, int spellNumber)
	{
		if (shownSchool == null && !Select(schools.IndexOf(school)))
			return $"the spell school {school} can't be chosen";

		if (shownSchool != school)
			return $"the spell school {shownSchool} is shown";

		int index = spells.FindIndex(spell => spell.SpellNumber == spellNumber);

		return Select(index) ? null : $"spell {spellNumber} of {school} can't be chosen";
	}

	public override bool KeyDown(Key key, KeyModifiers keyModifiers)
	{
		switch (key)
		{
			case Key.Escape:
				Game.ScreenHandler.PopScreen();
				return true;
			case Key.Up:
				list?.Scroll(-1);
				return true;
			case Key.Down:
				list?.Scroll(1);
				return true;
		}

		return base.KeyDown(key, keyModifiers);
	}

	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		if (buttons == MouseButtons.Left && list!.MouseClick(position))
			return true;

		if (buttons == MouseButtons.Right)
		{
			Game.ScreenHandler.PopScreen();
			return true;
		}

		return base.MouseDown(position, buttons, keyModifiers);
	}

	public override bool MouseWheel(Position position, float scrollX, float scrollY, MouseButtons buttons)
	{
		list?.MouseWheel(Math.Sign(scrollY));
		return true;
	}
}
