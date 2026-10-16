using Amber.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Events;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

/// <summary>
/// Guilds let classless party members join (if the class is possible for them) and
/// let members of the guild's class advance to the next level if they have enough
/// experience. After each level the skills are trained (see <see cref="SkillTrainingScreen"/>).
/// </summary>
internal sealed class GuildScreen : PlaceScreen
{
	const int JoinButtonIndex = 0;
	const int LevelUpButtonIndex = 3;
	bool showSummary = false;

	public override ScreenType Type { get; } = ScreenType.Guild;

	protected override Image80x80 Image { get; } = Image80x80.Guild;

	protected override Message LeaveMessage { get; } = Message.LeaveGoldForGuild;

	IGuildData Data => (IGuildData)Place.Data;

	Class GuildClass => (Class)(int)PlaceEvent.PlaceType;

	IClassData ClassData => Game.AssetProvider.ClassDataLoader.LoadClassData(GuildClass);

	static IPartyMember Member(Game game) => game.State.ActivePartyMember!;

	bool HasEnoughExperience(IPartyMember partyMember) => partyMember.ExperiencePoints >= ClassData.GetExperienceForNextLevel(partyMember.Level);

	bool CanJoin(IPartyMember partyMember) => partyMember.Class == Class.None &&
		partyMember.PossibleClasses.HasFlag((ClassFlags)(1 << (int)GuildClass));

	bool CanLevelUp(IPartyMember partyMember) => partyMember.Class == GuildClass && partyMember.Level < IClassData.MaxLevel;

	public override void Open(Action? closeAction)
	{
		showSummary = false;

		base.Open(closeAction);

		ShowInfo();
	}

	public override void ScreenPopped(Screen screen)
	{
		base.ScreenPopped(screen);

		if (screen is SkillTrainingScreen && showSummary)
		{
			showSummary = false;
			ShowLevelSummary();
		}
		else if (screen is not ConfirmationScreen)
		{
			ShowInfo();
		}
	}

	/// <summary>
	/// Shows the prices and the needed experience like Update_guild_messages in the original.
	/// </summary>
	private void ShowInfo()
	{
		var partyMember = Member(Game);
		var gold = Game.GetFullText(Game.LoadUIText(UIText.Gold));
		var lines = new List<string>();
		bool enoughExperience = HasEnoughExperience(partyMember);

		if (CanJoin(partyMember) && enoughExperience)
			lines.Add($"{Game.GetFullText(Game.LoadMessageText(Message.GuildMembershipPrice))} {Data.JoinPrice} {gold}");

		if (CanLevelUp(partyMember) && enoughExperience)
			lines.Add($"{Game.GetFullText(Game.LoadMessageText(Message.LevelUpPrice))} {Data.LevelPrice} {gold}");

		if ((CanJoin(partyMember) || CanLevelUp(partyMember)) && !enoughExperience)
		{
			long neededExperience = ClassData.GetExperienceForNextLevel(partyMember.Level) - partyMember.ExperiencePoints;
			lines.Add($"{partyMember.Name} {Game.GetFullText(Game.LoadMessageText(Message.ExperienceNeededForNextLevel))} {neededExperience}");
		}

		if (lines.Count == 0)
			HideMessage();
		else
			ShowMessage(Game.AssetProvider.TextLoader.FromString(string.Join(" ", lines)), false);

		RequestButtonSetup();
	}

	protected override void SetupPlaceButtons(ButtonGrid buttonGrid)
	{
		buttonGrid.SetButton(JoinButtonIndex, ButtonType.EnterDoor);
		buttonGrid.SetButton(LevelUpButtonIndex, ButtonType.Advance);

		if (Place == null)
			return;

		var partyMember = Member(Game);
		bool enoughExperience = HasEnoughExperience(partyMember);

		buttonGrid.EnableButton(JoinButtonIndex, CanJoin(partyMember) && enoughExperience && PlaceGold >= Data.JoinPrice);
		buttonGrid.EnableButton(LevelUpButtonIndex, CanLevelUp(partyMember) && enoughExperience && PlaceGold >= Data.LevelPrice);
	}

	protected override void PlaceButtonClicked(int index)
	{
		var partyMember = Member(Game);

		if (index == JoinButtonIndex)
		{
			PlaceGold -= Data.JoinPrice;
			JoinGuild(partyMember);
			IncreaseLevel(partyMember);
		}
		else if (index == LevelUpButtonIndex)
		{
			PlaceGold -= Data.LevelPrice;
			IncreaseLevel(partyMember);
		}
	}

	private void JoinGuild(IPartyMember partyMember)
	{
		var classData = ClassData;

		partyMember.Class = GuildClass;

		foreach (var skill in Enum.GetValues<Skill>().Skip(1))
			partyMember.Skills[skill].MaxValue = classData.GetSkillMaxValue(skill);

		// TODO: The original also copies the 7 unknown special values of the class.
		partyMember.AttackPerRoundLevel = classData.AttacksPerRoundLevel;
		partyMember.HitPointsPerLevel = classData.HitPointsPerLevel;
		partyMember.SpellPointsPerLevel = classData.SpellPointsPerLevel;
		partyMember.SpellLearningPointsPerLevel = classData.SpellLearningPointsPerLevel;
		partyMember.LearnedSpellSchools = classData.SpellSchools;
	}

	/// <summary>
	/// Like Increase_level in the original.
	/// </summary>
	private void IncreaseLevel(IPartyMember partyMember)
	{
		partyMember.Level++;

		if (partyMember.AttackPerRoundLevel != 0)
			partyMember.AttacksPerRound = (byte)(partyMember.Level / partyMember.AttackPerRoundLevel);

		int stamina = partyMember.Attributes[GameData.Attribute.Stamina].TotalCurrent;
		partyMember.HitPoints.MaxValue += (word)(stamina / 10 + partyMember.HitPointsPerLevel);

		if (partyMember.LearnedSpellSchools != SpellSchoolFlags.None)
		{
			int intelligence = partyMember.Attributes[GameData.Attribute.Intelligence].TotalCurrent;
			partyMember.SpellPoints.MaxValue += (word)(intelligence / 20 + partyMember.SpellPointsPerLevel);
			partyMember.SpellLearningPoints += (word)(intelligence / 20 + partyMember.SpellLearningPointsPerLevel);
		}

		Game.UpdatePartyMembers();

		showSummary = true;
		ShowMessage(Message.WhichAbilitiesToTrain, false);
		Game.ScreenHandler.PushScreen(ScreenType.SkillTraining);
	}

	private void ShowLevelSummary()
	{
		var partyMember = Member(Game);
		string Text(Message message) => Game.GetFullText(Game.LoadMessageText(message));
		var summary = $"{partyMember.Name} {Text(Message.HasLevel)} {partyMember.Level} {Text(Message.LevelReached)} " +
			$"{Text(Message.MaxHPNow)} {partyMember.HitPoints.TotalMax}";

		if (partyMember.LearnedSpellSchools != SpellSchoolFlags.None)
			summary += $" {Text(Message.MaxSPNow)} {partyMember.SpellPoints.TotalMax} {Text(Message.SLPNow)} {partyMember.SpellLearningPoints}";

		ShowMessage(Game.AssetProvider.TextLoader.FromString(summary), true, ShowInfo);
	}

	protected override bool PlaceMouseDown(Position position, MouseButtons buttons)
	{
		if (buttons != MouseButtons.Left)
			return false;

		int? portraitSlot = Game.TestPartyPortraitHit(position);

		if (portraitSlot == null)
			return false;

		var partyMember = Game.State.GetPartyMember(portraitSlot.Value);

		// Only members who are able to act can be selected.
		if (partyMember != null && !partyMember.HasAnyConditionOf(Condition.Dead | Condition.Ashes | Condition.Dust |
			Condition.Petrified | Condition.Stunned | Condition.Mad | Condition.Sleeping))
		{
			Game.State.SetActivePartyMember(portraitSlot.Value);
			Game.UpdatePartyMembers();
			ShowInfo();
		}

		return true;
	}
}
