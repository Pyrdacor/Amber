using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

/// <summary>
/// After a level-up, 10 points are distributed to the skills of the active party member
/// (SkillW in the original). A left click on a skill increases it, a right click decreases it
/// again. The window can only be closed if all points are used or no skill can be increased.
/// </summary>
internal sealed class SkillTrainingScreen() : WindowScreen(WindowX, WindowY, WindowWidthInTiles, WindowHeightInTiles, WindowDisplayLayer)
{
	const int WindowX = 32;
	const int WindowY = 37;
	const int WindowWidthInTiles = 10;
	const int WindowHeightInTiles = 8;
	const byte WindowDisplayLayer = 110;
	const byte ControlDisplayLayer = 120;
	const int TrainingPoints = 10;
	const int SkillCount = 10;
	const int RowsY = 8;
	const int RowHeight = 7;
	readonly Label[] skillLabels = new Label[SkillCount];
	readonly int[] trainedPoints = new int[SkillCount];
	Label? remainingLabel;
	Button? exitButton;
	int remainingPoints = 0;

	public override ScreenType Type { get; } = ScreenType.SkillTraining;

	public override bool CloseOnEscape { get; } = false;

	public override void Init()
	{
		base.Init();

		var width = ClientArea.Size.Width;
		var title = AddLabel(0, 0, Game.LoadUIText(UIText.Skills), width, 7, ControlDisplayLayer);
		title.Alignment = TextAlignment.Center;

		for (int i = 0; i < SkillCount; i++)
			skillLabels[i] = AddLabel(2, RowsY + i * RowHeight, width - 2, 7, ControlDisplayLayer);

		remainingLabel = AddLabel(48, RowsY + SkillCount * RowHeight + 6, width - 48, 7, ControlDisplayLayer);

		exitButton = AddButton(0, 80, ButtonType.Exit, ControlDisplayLayer);
		exitButton.ClickAction += () =>
		{
			if (CanFinish)
				Game.ScreenHandler.PopScreen();
		};
	}

	public override void Open(Action? closeAction)
	{
		base.Open(closeAction);

		Array.Clear(trainedPoints);
		remainingPoints = TrainingPoints;
		UpdateTexts();
	}

	static IPartyMember PartyMember(Game game) => game.State.ActivePartyMember!;

	CharacterValue GetSkill(int index) => PartyMember(Game).Skills[(Skill)(index + 1)];

	bool CanIncrease(int index) => remainingPoints > 0 && GetSkill(index).CurrentValue < GetSkill(index).MaxValue;

	bool CanFinish => remainingPoints == 0 || !Enumerable.Range(0, SkillCount).Any(CanIncrease);

	private void UpdateTexts()
	{
		for (int i = 0; i < SkillCount; i++)
		{
			var name = Game.GetFullText(Game.AssetProvider.TextLoader.LoadText(new AssetIdentifier(AssetType.SkillName, i)));

			if (name.Length > 7)
				name = name[..7];

			var skill = GetSkill(i);
			skillLabels[i].SetText($"{name,-7} {skill.TotalCurrent:00}%/{skill.MaxValue:00}% {trainedPoints[i]:00}%");
		}

		remainingLabel!.SetText($"BONUS : {remainingPoints:00}%");
		exitButton!.Disabled = !CanFinish;
	}

	/// <summary>
	/// Trains a skill (0-based index) by one point or takes back one point.
	/// This is also used by scenario scripts.
	/// </summary>
	internal bool TrainSkill(int index, bool increase)
	{
		if (index < 0 || index >= SkillCount)
			return false;

		var skill = GetSkill(index);

		if (increase)
		{
			if (!CanIncrease(index))
				return false;

			skill.CurrentValue++;
			trainedPoints[index]++;
			remainingPoints--;
		}
		else
		{
			if (trainedPoints[index] == 0)
				return false;

			skill.CurrentValue--;
			trainedPoints[index]--;
			remainingPoints++;
		}

		UpdateTexts();

		return true;
	}

	internal bool ScriptFinish()
	{
		if (!CanFinish)
			return false;

		Game.ScreenHandler.PopScreen();

		return true;
	}

	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		var (x, y) = ClientArea.Position;

		for (int i = 0; i < SkillCount; i++)
		{
			if (new Rect(x, y + RowsY + i * RowHeight, ClientArea.Size.Width, RowHeight).Contains(position))
			{
				TrainSkill(i, buttons == MouseButtons.Left);
				return true;
			}
		}

		return base.MouseDown(position, buttons, keyModifiers);
	}
}
