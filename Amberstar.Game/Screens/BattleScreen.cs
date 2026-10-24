using Amber.Common;
using Amber.Renderer.Common;
using Amberstar.Game.Battle;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

/// <summary>
/// The combat screen (Combat_Mod in the original, disassembled from the program).
///
/// The left side shows the combat background with the monsters, the right side the tactical battle field
/// with 6x5 icons (rows 0-2 monsters, rows 3-4 party). The actions of the party members are chosen with
/// the buttons (the active member is chosen by its portrait). Ok executes the round. The events of the
/// round are shown one after another: in the slow mode each text waits for a click.
///
/// The animations are timed by vertical blanks (50 per second) like in the original: random idle animations
/// of the participants while the actions are chosen, the attack frames of monsters, the particles of spells
/// and the damage numbers on the tactical battle field.
/// </summary>
internal sealed class BattleScreen : ButtonGridScreen, IWaitForClickScreen
{
	enum TargetSelection
	{
		None,
		Move,
		Attack,
		Spell
	}

	const int FleeButtonIndex = 0;
	const int OkButtonIndex = 2;
	const int MoveButtonIndex = 3;
	const int SlowButtonIndex = 4;
	const int FastButtonIndex = 5;
	const int AttackButtonIndex = 6;
	const int ParryButtonIndex = 7;
	const int MagicButtonIndex = 8;
	static readonly Position BackgroundPosition = new(16, 49);
	static readonly Position TacticalPosition = new(208, 49);
	static readonly Rect TextArea = new(16, 177, 176, 16);
	const int TacticalIconSize = 16;
	const long TicksPerFastText = Game.TicksPerSecond;
	const int VerticalBlanksPerSecond = 50;
	const int VerticalBlanksPerIdleStep = 8;
	const int VerticalBlanksPerSlowAttackFrame = 6;
	const int VerticalBlanksPerFastAttackFrame = 4;
	const int VerticalBlanksPerSpellFrame = 4;
	const int MonsterDamageVerticalBlanks = 100;
	const int PartyDamageVerticalBlanks = 70;
	const byte BackgroundDisplayLayer = 1;
	const byte MonsterDisplayLayer = 10;
	const byte AttackingMonsterDisplayLayer = 26;
	const byte TacticalDisplayLayer = 10;
	const byte MarkerDisplayLayer = 20;
	const byte DamageDisplayLayer = 22;
	const byte SpellParticleDisplayLayer = 30;
	const int DamageDigitDistance = 5;
	const int SpellParticleSize = 16;
	static readonly int[] IconAnimationFrames = [0, 1, 2, 1];

	Battle.Battle? battle;
	int background = 1;
	readonly Image?[] tacticalIcons = new Image?[Battle.Battle.Columns * Battle.Battle.Rows];
	// The battle field as shown. The battle executes a whole round at once and the shown battle field follows its events.
	readonly BattleParticipant?[] shownBattleField = new BattleParticipant?[Battle.Battle.Columns * Battle.Battle.Rows];
	readonly List<Image> monsterImages = [];
	readonly List<IColoredRect> markers = [];
	Label? text;
	TargetSelection targetSelection = TargetSelection.None;
	(SpellSchool School, int SpellNumber, SpellTarget Target)? selectedSpell = null;
	bool slowMode = false;
	bool executing = false;
	readonly Queue<BattleEvent> pendingEvents = [];
	Action? clickAction = null;
	long verticalBlankTicks = 0;
	// idle animation (index into IconAnimationFrames, 0 = not animated)
	readonly Dictionary<BattleParticipant, int> idleFrames = [];
	int idleVerticalBlanks = 0;
	// attack animation of a monster
	MonsterParticipant? attackingMonster = null;
	int attackFrame = 0;
	int attackDirection = 0;
	int attackVerticalBlanks = 0;
	readonly List<SpellAnimation> spellAnimations = [];
	readonly Dictionary<BattleParticipant, DamageDisplay> damageDisplays = [];
	bool waitingForAnimations = false;

	sealed class SpellAnimation(MonsterParticipant target, int animation, int path)
	{
		public MonsterParticipant Target => target;
		public int Animation => animation;
		public int Path => path;
		public int VerticalBlanks { get; set; } = 0;
		public List<Image> Particles { get; } = [];
	}

	sealed class DamageDisplay(int verticalBlanks)
	{
		public int VerticalBlanks { get; set; } = verticalBlanks;
		public List<Image> Digits { get; } = [];
	}

	public override ScreenType Type { get; } = ScreenType.Battle;

	internal override byte ButtonGridPaletteIndex => Game?.PaletteIndexProvider.GetCombatPaletteIndex(background) ?? 0;

	// The battle can't be left with escape and the inventory is not available.
	public override bool CloseOnEscape { get; } = false;
	public override bool AllowInventoryAccess { get; } = false;
	// The game time is paused during battles but the animations run.
	public override bool UpdateWhilePaused { get; } = true;

	internal Battle.Battle? CurrentBattle => battle;

	/// <summary>
	/// The texts which were shown (used by scenario scripts).
	/// </summary>
	internal List<string> ShownTexts { get; } = [];

	public bool WaitingForClick => clickAction != null;

	internal bool Executing => executing;

	public override void Open(Action? closeAction)
	{
		// The buttons depend on the battle.
		battle = Game!.CurrentBattle!;
		background = Game.CurrentBattleBackground;

		base.Open(closeAction);

		targetSelection = TargetSelection.None;
		executing = false;
		pendingEvents.Clear();
		clickAction = null;
		ShownTexts.Clear();
		idleFrames.Clear();
		attackingMonster = null;
		waitingForAnimations = false;

		Game.SetLayout(Layout.Battle, ButtonGridPaletteIndex);
		Game.Cursor.CursorType = CursorType.Sword;
		Game.State.ActivePartyMemberChanged += ActivePartyMemberChanged;

		var palette = ButtonGridPaletteIndex;
		var backgroundImage = AddImage(BackgroundPosition.X, BackgroundPosition.Y, 176, 112,
			Game.GraphicIndexProvider.GetCombatBackgroundIndex(background), BackgroundDisplayLayer, true);
		backgroundImage.PaletteIndex = palette;

		for (int i = 0; i < tacticalIcons.Length; i++)
		{
			int x = TacticalPosition.X + (i % Battle.Battle.Columns) * TacticalIconSize;
			int y = TacticalPosition.Y + (i / Battle.Battle.Columns) * TacticalIconSize;
			var icon = AddImage(x, y, TacticalIconSize, TacticalIconSize, Game.GraphicIndexProvider.GetTacticalIconIndex(0), TacticalDisplayLayer);
			icon.PaletteIndex = palette;
			icon.Visible = false;
			tacticalIcons[i] = icon;
		}

		var (textX, textY, textWidth, textHeight) = TextArea;
		text = AddLabel(textX, textY, textWidth, textHeight, 30);

		battle.PrepareRound();
		TakeBattleField();
		UpdateBattleField();
		UpdateStatusIcons();

		// With the monster advantage the first round starts at once.
		if (battle.MonsterAdvantage)
			ExecuteRound();
	}

	public override void Close()
	{
		Game.State.ActivePartyMemberChanged -= ActivePartyMemberChanged;
		ClearMonsterImages();
		ClearMarkers();
		ClearAnimations();

		base.Close();
	}

	void ActivePartyMemberChanged()
	{
		targetSelection = TargetSelection.None;
		ClearMarkers();
		RequestButtonSetup();
		ShowActionMarker();
	}

	int ActiveSlot => Game.State.ActivePartyMemberIndex;

	protected override void SetupButtons(ButtonGrid buttonGrid)
	{
		buttonGrid.SetButton(FleeButtonIndex, ButtonType.Flee);
		buttonGrid.SetButton(1, ButtonType.Empty);
		buttonGrid.SetButton(OkButtonIndex, ButtonType.Ok);
		buttonGrid.SetButton(MoveButtonIndex, ButtonType.PartyPositions);
		buttonGrid.SetButton(SlowButtonIndex, ButtonType.Play);
		buttonGrid.SetButton(FastButtonIndex, ButtonType.Forward);
		buttonGrid.SetButton(AttackButtonIndex, ButtonType.Sword);
		buttonGrid.SetButton(ParryButtonIndex, ButtonType.Shield);
		buttonGrid.SetButton(MagicButtonIndex, ButtonType.UseMagic);

		bool input = battle != null && !executing;
		int slot = ActiveSlot;

		buttonGrid.EnableButton(OkButtonIndex, input);
		buttonGrid.EnableButton(FleeButtonIndex, input && battle!.CanFlee(slot));
		buttonGrid.EnableButton(MoveButtonIndex, input && battle!.CanMove(slot));
		buttonGrid.EnableButton(AttackButtonIndex, input && battle!.CanAttack(slot));
		buttonGrid.EnableButton(ParryButtonIndex, input && battle!.CanParry(slot));
		buttonGrid.EnableButton(MagicButtonIndex, input && battle!.CanCastSpells(slot));
		buttonGrid.EnableButton(SlowButtonIndex, input && !slowMode);
		buttonGrid.EnableButton(FastButtonIndex, input && slowMode);
	}

	protected override void ButtonClicked(int index)
	{
		if (battle == null || executing)
			return;

		int slot = ActiveSlot;
		targetSelection = TargetSelection.None;
		ClearMarkers();

		switch (index)
		{
			case FleeButtonIndex:
				SetAction(slot, new(BattleActionType.Flee));
				break;
			case OkButtonIndex:
				ExecuteRound();
				break;
			case MoveButtonIndex:
				StartTargetSelection(TargetSelection.Move, Message.MoveWhere, battle.GetMoveTargets(slot));
				break;
			case SlowButtonIndex:
			case FastButtonIndex:
				slowMode = index == SlowButtonIndex;
				RequestButtonSetup();
				break;
			case AttackButtonIndex:
				StartTargetSelection(TargetSelection.Attack, Message.AttackWhom, battle.GetAttackTargets(slot, out _));
				break;
			case ParryButtonIndex:
				SetAction(slot, BattleAction.Parry);
				break;
			case MagicButtonIndex:
				Game.SelectBattleSpell(spell =>
				{
					if (spell != null)
						ChooseSpell(slot, spell.Value.School, spell.Value.SpellNumber);
				});
				break;
		}
	}

	/// <summary>
	/// The targets of a spell are chosen with the spell (L17de4, L17f02, L1801a and L18140 in the original).
	/// </summary>
	void ChooseSpell(int slot, SpellSchool school, int spellNumber)
	{
		var spellData = Game.LoadSpellData(school, spellNumber);
		var target = BattleAction.GetSpellTarget(spellData);
		selectedSpell = (school, spellNumber, target);

		IEnumerable<Position> targetCells = target switch
		{
			SpellTarget.Member => battle!.PartyMembers.Where(battle.IsOnBattleField).Select(partyMember => partyMember.Position),
			SpellTarget.Monster or SpellTarget.MonsterGroup => battle!.Monsters.Select(monster => monster.Position),
			_ => []
		};

		if (target is SpellTarget.Member or SpellTarget.Monster or SpellTarget.MonsterGroup)
		{
			var message = Game.GetSpellTargetMessage(school, spellNumber, target) ?? Message.WhichMonster;
			StartTargetSelection(TargetSelection.Spell, message, targetCells);
			return;
		}

		SetAction(slot, new(BattleActionType.Magic, SpellSchool: school, SpellNumber: spellNumber));
	}

	List<BattleParticipant>? GetSpellTargets(Position cell)
	{
		var (_, _, target) = selectedSpell!.Value;

		return target switch
		{
			SpellTarget.Member when battle![cell] is PartyMemberParticipant partyMember => [partyMember],
			SpellTarget.Monster when battle![cell] is MonsterParticipant monster => [monster],
			SpellTarget.MonsterGroup when cell.Y < Battle.Battle.MonsterRows && battle!.Monsters.Any(monster => monster.Position.Y == cell.Y) =>
				[.. battle.Monsters.Where(monster => monster.Position.Y == cell.Y)],
			_ => null
		};
	}

	/// <summary>
	/// Chooses a spell and its target like the magic button (used by scenario scripts).
	/// </summary>
	internal string? ScriptChooseSpell(SpellSchool school, int spellNumber, Position? target)
	{
		if (battle == null || executing)
			return "the battle round is executed";

		int slot = ActiveSlot;

		if (!battle.CanCastSpells(slot))
			return "the active member can't cast spells";

		var caster = Game.State.ActivePartyMember!;
		var spellData = Game.LoadSpellData(school, spellNumber);

		if (!Game.HasLearnedSpell(caster, school, spellNumber) || !Game.CanCastSpellAt(spellData, SpellLocation.Battle) ||
			caster.SpellPoints.CurrentValue < spellData.SpellPointCost)
			return $"the spell {school} {spellNumber} can't be cast";

		ChooseSpell(slot, school, spellNumber);

		if (targetSelection == TargetSelection.Spell)
		{
			if (target == null)
				return "the spell needs a target";

			if (!SelectTarget(target.Value))
				return $"{target} is no target of the spell";
		}

		return null;
	}

	void SetAction(int slot, BattleAction action)
	{
		battle!.SetPartyAction(slot, action);
		UpdateStatusIcons();
		ShowActionMarker();
	}

	void StartTargetSelection(TargetSelection selection, Message message, IEnumerable<Position> targets)
	{
		targetSelection = selection;
		ShowText(Game.LoadMessageText(message).GetLines(int.MaxValue).Aggregate("", (a, b) => a + b));

		foreach (var target in targets)
			AddMarker(target, Color.White);
	}

	/// <summary>
	/// Chooses the target cell of a move or an attack (also used by scenario scripts).
	/// Returns false if the cell is no valid target.
	/// </summary>
	internal bool SelectTarget(Position cell)
	{
		if (battle == null || targetSelection == TargetSelection.None)
			return false;

		int slot = ActiveSlot;
		var selection = targetSelection;
		bool valid;

		if (selection == TargetSelection.Spell)
		{
			var targets = GetSpellTargets(cell);
			valid = targets != null;

			if (valid)
			{
				var (school, spellNumber, _) = selectedSpell!.Value;
				battle.SetPartyAction(slot, new(BattleActionType.Magic, SpellSchool: school, SpellNumber: spellNumber, Targets: targets));
			}
		}
		else if (selection == TargetSelection.Move)
		{
			valid = battle.GetMoveTargets(slot).Contains(cell);

			if (valid)
				battle.SetPartyAction(slot, new(BattleActionType.Move, cell));
		}
		else
		{
			valid = battle.GetAttackTargets(slot, out bool ranged).Contains(cell);

			if (valid)
				battle.SetPartyAction(slot, new(BattleActionType.Attack, cell, ranged));
		}

		targetSelection = TargetSelection.None;
		ClearMarkers();
		ShowText("");
		UpdateStatusIcons();
		ShowActionMarker();

		return valid;
	}

	public override bool MouseDown(Position position, MouseButtons buttons, KeyModifiers keyModifiers)
	{
		if (clickAction != null)
		{
			var action = clickAction;
			clickAction = null;
			action();
			return true;
		}

		if (!executing && targetSelection != TargetSelection.None)
		{
			var cell = new Position((position.X - TacticalPosition.X) / TacticalIconSize, (position.Y - TacticalPosition.Y) / TacticalIconSize);

			if (position.X >= TacticalPosition.X && position.Y >= TacticalPosition.Y &&
				cell.X < Battle.Battle.Columns && cell.Y < Battle.Battle.Rows)
			{
				SelectTarget(cell);
				return true;
			}

			// a click somewhere else cancels the selection
			if (buttons == MouseButtons.Right)
			{
				targetSelection = TargetSelection.None;
				ClearMarkers();
				ShowText("");
				return true;
			}
		}

		return base.MouseDown(position, buttons, keyModifiers);
	}

	public override void Update(long elapsedTicks)
	{
		base.Update(elapsedTicks);

		verticalBlankTicks += elapsedTicks * VerticalBlanksPerSecond;
		int verticalBlanks = (int)(verticalBlankTicks / Game.TicksPerSecond);
		verticalBlankTicks %= Game.TicksPerSecond;

		if (verticalBlanks == 0 || battle == null)
			return;

		bool changed = UpdateIdleAnimation(verticalBlanks);
		changed |= UpdateAttackAnimation(verticalBlanks);

		if (changed)
			UpdateBattleField();

		UpdateSpellAnimations(verticalBlanks);
		UpdateDamageDisplays(verticalBlanks);

		if (waitingForAnimations && !AnimationRunning)
		{
			waitingForAnimations = false;
			ShowNextEvent();
		}
	}

	/// <summary>
	/// Chooses an action of the active member like the buttons (used by scenario scripts).
	/// </summary>
	internal string? ScriptChooseAction(string action, Position? target)
	{
		if (battle == null || executing)
			return "the battle round is executed";

		int? button = action.ToLower() switch
		{
			"flee" => FleeButtonIndex,
			"move" => MoveButtonIndex,
			"attack" => AttackButtonIndex,
			"parry" => ParryButtonIndex,
			"magic" => MagicButtonIndex,
			"slow" => SlowButtonIndex,
			"fast" => FastButtonIndex,
			"ok" => OkButtonIndex,
			_ => null
		};

		if (button == null)
			return $"unknown battle action {action}";

		if (!ScriptIsButtonEnabled(button.Value))
			return $"the action {action} is not possible";

		// Buttons react after a short highlight, so the action is chosen directly.
		ButtonClicked(button.Value);

		if (targetSelection != TargetSelection.None)
		{
			if (target == null)
				return $"the action {action} needs a target";

			if (!SelectTarget(target.Value))
				return $"{target} is no target of {action}";
		}

		return null;
	}

	/// <summary>
	/// Leaves the battle as if the party fled (used by scenario scripts).
	/// </summary>
	internal void ScriptLeave()
	{
		if (battle == null)
			return;

		pendingEvents.Clear();
		clickAction = null;
		executing = false;
		waitingForAnimations = false;
		battle.Abort();
		Game.ScreenHandler.PopScreen();
	}

	#region Round

	/// <summary>
	/// Executes a round and shows its events.
	/// </summary>
	internal void ExecuteRound()
	{
		if (battle == null || executing)
			return;

		executing = true;
		targetSelection = TargetSelection.None;
		ClearMarkers();
		RequestButtonSetup();

		// The idle animations stop while the round is executed.
		idleFrames.Clear();
		TakeBattleField();

		Game.UpdateBattleSpells(battle);
		battle.ActiveSlot = ActiveSlot;

		foreach (var battleEvent in battle.ExecuteRound())
			pendingEvents.Enqueue(battleEvent);

		ShowNextEvent();
	}

	/// <summary>
	/// Animations are started at once. The other events wait until the attack and spell animations are finished.
	/// </summary>
	void ShowNextEvent()
	{
		while (pendingEvents.TryPeek(out var battleEvent))
		{
			switch (battleEvent)
			{
				case BattleAttackAnimationEvent attackAnimationEvent:
					if (AnimationRunning)
						break;
					pendingEvents.Dequeue();
					StartAttackAnimation(attackAnimationEvent.Monster);
					continue;
				case BattleSpellAnimationEvent spellAnimationEvent:
					pendingEvents.Dequeue();
					StartSpellAnimation(spellAnimationEvent.Target, spellAnimationEvent.Animation);
					continue;
				case BattleDamageEvent damageEvent:
					pendingEvents.Dequeue();
					ShowDamage(damageEvent.Target, damageEvent.Damage);
					continue;
			}

			if (AnimationRunning)
			{
				waitingForAnimations = true;
				return;
			}

			pendingEvents.Dequeue();

			switch (battleEvent)
			{
				case BattleTextEvent textEvent:
					ShowText(FormatText(textEvent));
					WaitForText();
					return;
				case BattleSpellEvent spellEvent:
					ShowText(FormatSpell(spellEvent));
					WaitForText();
					return;
				case BattleMoveEvent moveEvent:
					if (shownBattleField[CellIndex(moveEvent.From)] == moveEvent.Participant)
						shownBattleField[CellIndex(moveEvent.From)] = null;
					shownBattleField[CellIndex(moveEvent.To)] = moveEvent.Participant;
					UpdateBattleField();
					break;
				case BattleRemoveEvent removeEvent:
					int index = Array.IndexOf(shownBattleField, removeEvent.Participant);
					if (index >= 0)
						shownBattleField[index] = null;
					UpdateBattleField();
					break;
				default:
					UpdateBattleField();
					break;
			}
		}

		if (AnimationRunning)
		{
			waitingForAnimations = true;
			return;
		}

		RoundFinished();
	}

	void WaitForText()
	{
		// The damage of a hit is shown with its text.
		while (pendingEvents.TryPeek(out var battleEvent) && battleEvent is BattleDamageEvent damageEvent)
		{
			pendingEvents.Dequeue();
			ShowDamage(damageEvent.Target, damageEvent.Damage);
		}

		UpdateBattleField();
		Game.UpdatePartyMembers();

		if (slowMode)
			clickAction = ShowNextEvent;
		else
			Game.AddDelayedAction(TicksPerFastText, () =>
			{
				if (executing && clickAction == null)
					ShowNextEvent();
			});
	}

	void RoundFinished()
	{
		executing = false;
		ShowText("");
		TakeBattleField();
		UpdateBattleField();
		Game.PartyMembersChanged();

		if (battle!.Result != BattleResult.Ongoing)
		{
			Game.ScreenHandler.PopScreen();
			return;
		}

		battle.NewActiveMemberNeeded = false;
		battle.PrepareRound();
		UpdateStatusIcons();
		RequestButtonSetup();
		ShowActionMarker();
	}

	string MessageText(Message message) => string.Join(" ", Game.LoadMessageText(message).GetLines(int.MaxValue));

	string FormatText(BattleTextEvent textEvent)
	{
		if (textEvent.Message == Message.Hits)
			return $"{textEvent.Actor!.Name} {MessageText(Message.Hits)} {textEvent.Target!.Name} {MessageText(Message.With)} {textEvent.Value} {MessageText(Message.LP)}";

		var message = MessageText(textEvent.Message);

		return textEvent.Actor == null ? message : $"{textEvent.Actor.Name} {message}";
	}

	string FormatSpell(BattleSpellEvent spellEvent) => spellEvent.Caster is MonsterParticipant
		? $"{spellEvent.Caster.Name} {MessageText(Message.CastsASpell)}"
		: $"{spellEvent.Caster.Name} {MessageText(Message.CastsSpell)} {Game.GetFullText(Game.LoadSpellName(spellEvent.School, spellEvent.SpellNumber)).Trim()}";

	void ShowText(string message)
	{
		if (message.Length != 0)
		{
			ShownTexts.Add(message);
			Game.ScriptTextShown(message);
		}

		// Long texts are wrapped into the 2 lines of the text area.
		text!.SetText(Game.AssetProvider.TextLoader.FromString(message), TextArea.Size.Width, TextManager.DefaultInkColorIndex,
			TextManager.TransparentPaper, ButtonGridPaletteIndex);
		text.Visible = message.Length != 0;
	}

	#endregion

	#region Display

	void UpdateStatusIcons()
	{
		if (battle == null)
			return;

		foreach (var partyMember in battle.PartyMembers)
		{
			var icon = battle.GetPartyAction(partyMember.Slot).Type switch
			{
				BattleActionType.Move => StatusIcon.Move,
				BattleActionType.Flee => StatusIcon.Flee,
				BattleActionType.Attack => StatusIcon.Attack,
				BattleActionType.Magic => StatusIcon.UseMagic,
				BattleActionType.None => StatusIcon.Sleeping,
				BattleActionType.Stunned => StatusIcon.Stunned,
				BattleActionType.Petrified => StatusIcon.Petrified,
				_ => StatusIcon.Parry
			};

			if (partyMember.HasAnyConditionOf(Condition.Dead | Condition.Ashes | Condition.Dust))
				icon = StatusIcon.Dead;

			Game.SetStatusIcon(partyMember.Slot - 1, icon);
		}
	}

	static int CellIndex(Position cell) => cell.X + cell.Y * Battle.Battle.Columns;

	void TakeBattleField()
	{
		for (int i = 0; i < shownBattleField.Length; i++)
			shownBattleField[i] = battle?[i % Battle.Battle.Columns, i / Battle.Battle.Columns];
	}

	/// <summary>
	/// The shown cell of a participant or null if it is not shown.
	/// </summary>
	Position? GetShownCell(BattleParticipant participant)
	{
		int index = Array.IndexOf(shownBattleField, participant);

		return index < 0 ? null : new Position(index % Battle.Battle.Columns, index / Battle.Battle.Columns);
	}

	void UpdateBattleField()
	{
		UpdateTacticalIcons();
		UpdateMonsterImages();
	}

	/// <summary>
	/// The party members show their class and the monsters their graphic (+9) with 3 animation frames each.
	/// </summary>
	void UpdateTacticalIcons()
	{
		if (battle == null)
			return;

		for (int i = 0; i < tacticalIcons.Length; i++)
		{
			var participant = shownBattleField[i];
			var icon = tacticalIcons[i]!;

			if (participant == null)
			{
				icon.Visible = false;
				continue;
			}

			int baseIcon = participant is MonsterParticipant monster
				? (monster.Monster.BattleGraphicIndex + 9) * 3
				: (int)((PartyMemberParticipant)participant).PartyMember.Class * 3;

			icon.SetTextureIndex(Game.GraphicIndexProvider.GetTacticalIconIndex(baseIcon + IconAnimationFrames[idleFrames.GetValueOrDefault(participant)]));
			icon.Visible = true;
		}
	}

	void ClearMonsterImages()
	{
		monsterImages.ForEach(image => image.Destroy());
		monsterImages.Clear();
	}

	/// <summary>
	/// The monsters are drawn from the back row to the front row. Their size depends on the row (L18454 in the original).
	/// </summary>
	void UpdateMonsterImages()
	{
		ClearMonsterImages();

		if (battle == null)
			return;

		var combatData = Game.AssetProvider.CombatData;

		for (int row = 0; row < Battle.Battle.MonsterRows; row++)
		{
			for (int column = 0; column < Battle.Battle.Columns; column++)
			{
				if (shownBattleField[CellIndex(new(column, row))] is not MonsterParticipant monster)
					continue;

				int graphic = monster.Monster.BattleGraphicIndex;
				var position = combatData.GetMonsterPosition(column, row);
				var frames = Game.AssetProvider.GraphicLoader.LoadMonsterGraphic(graphic);
				int frame = GetMonsterFrame(monster, position);

				if (frame >= frames.Length)
					continue;

				var size = new Size(frames[frame].Width, frames[frame].Height);
				int x = BackgroundPosition.X + position.X - size.Width / 2;
				int y = BackgroundPosition.Y + position.Y - size.Height;
				// The attacking monster is drawn in front of the others.
				byte displayLayer = monster == attackingMonster ? AttackingMonsterDisplayLayer : (byte)(MonsterDisplayLayer + row * 5);
				var image = new Image(Game, new Position(x, y), size, Game.GraphicIndexProvider.GetMonsterGraphicIndex(graphic, frame),
					displayLayer, Layer.UI, ButtonGridPaletteIndex);
				monsterImages.Add(image);
			}
		}
	}

	/// <summary>
	/// Each frame group (size) of a monster graphic starts with the 3 idle frames. The attack uses the frames 1 to count-1.
	/// </summary>
	int GetMonsterFrame(MonsterParticipant monster, MonsterGraphicPosition position)
	{
		var frameCounts = Game.AssetProvider.CombatData.GetMonsterFrameCounts(monster.Monster.BattleGraphicIndex);
		int baseFrame = frameCounts.Take(position.FrameGroup).Sum();
		int count = frameCounts[position.FrameGroup];
		int frame = monster == attackingMonster ? attackFrame : IconAnimationFrames[idleFrames.GetValueOrDefault(monster)];

		return baseFrame + Math.Clamp(frame, 0, Math.Max(0, count - 1));
	}

	/// <summary>
	/// The center of a monster graphic on the screen.
	/// </summary>
	Position GetMonsterCenter(MonsterParticipant monster, Position cell)
	{
		var position = Game.AssetProvider.CombatData.GetMonsterPosition(cell.X, cell.Y);
		var frames = Game.AssetProvider.GraphicLoader.LoadMonsterGraphic(monster.Monster.BattleGraphicIndex);
		int frame = GetMonsterFrame(monster, position);
		int height = frame < frames.Length ? frames[frame].Height : 0;

		return new(BackgroundPosition.X + position.X, BackgroundPosition.Y + position.Y - height / 2);
	}

	void ClearMarkers()
	{
		markers.ForEach(marker => marker.Visible = false);
		markers.Clear();
	}

	/// <summary>
	/// Draws a frame around a cell of the tactical battle field.
	/// </summary>
	void AddMarker(Position cell, Color color)
	{
		int x = TacticalPosition.X + cell.X * TacticalIconSize;
		int y = TacticalPosition.Y + cell.Y * TacticalIconSize;

		void AddLine(int lineX, int lineY, int width, int height)
		{
			var line = Game.CreateColoredRect(Layer.UI, new(lineX, lineY), new(width, height), color);

			if (line != null)
			{
				line.DisplayLayer = MarkerDisplayLayer;
				markers.Add(line);
			}
		}

		AddLine(x, y, TacticalIconSize, 1);
		AddLine(x, y + TacticalIconSize - 1, TacticalIconSize, 1);
		AddLine(x, y, 1, TacticalIconSize);
		AddLine(x + TacticalIconSize - 1, y, 1, TacticalIconSize);
	}

	/// <summary>
	/// Marks the target of the active member's move or attack.
	/// </summary>
	void ShowActionMarker()
	{
		if (battle == null || executing)
			return;

		ClearMarkers();
		var action = battle.GetPartyAction(ActiveSlot);

		if (action.Type is BattleActionType.Move or BattleActionType.Attack)
			AddMarker(action.Target, action.Type == BattleActionType.Move ? Color.Green : Color.Red);
	}

	#endregion

	#region Animations

	bool AnimationRunning => attackingMonster != null || spellAnimations.Count != 0;

	/// <summary>
	/// Every 8 vertical blanks the running idle animations advance and a random participant may start one.
	/// Stunned and sleeping participants are not animated (L16678 in the original).
	/// </summary>
	bool UpdateIdleAnimation(int verticalBlanks)
	{
		if (executing)
			return false;

		idleVerticalBlanks += verticalBlanks;
		bool changed = false;

		while (idleVerticalBlanks >= VerticalBlanksPerIdleStep)
		{
			idleVerticalBlanks -= VerticalBlanksPerIdleStep;
			int countdown = Game.Random(15);

			for (int row = 0; row < Battle.Battle.Rows; row++)
			{
				for (int column = 0; column < Battle.Battle.Columns; column++)
				{
					var participant = shownBattleField[CellIndex(new(column, row))];

					if (participant == null)
						continue;

					int frame = idleFrames.GetValueOrDefault(participant);

					if (frame != 0)
					{
						idleFrames[participant] = (frame + 1) % IconAnimationFrames.Length;
						changed = true;
					}

					countdown--;

					if ((countdown & 0xf) == 0 && idleFrames.GetValueOrDefault(participant) == 0 &&
						!participant.HasAnyConditionOf(Condition.Stunned | Condition.Sleeping))
					{
						idleFrames[participant] = 1;
						changed = true;
					}
				}
			}
		}

		return changed;
	}

	/// <summary>
	/// The monster shows its attack frames forth and back (L16766 and L166da in the original).
	/// </summary>
	void StartAttackAnimation(MonsterParticipant monster)
	{
		var cell = GetShownCell(monster);

		if (cell == null)
			return;

		var position = Game.AssetProvider.CombatData.GetMonsterPosition(cell.Value.X, cell.Value.Y);
		var frameCounts = Game.AssetProvider.CombatData.GetMonsterFrameCounts(monster.Monster.BattleGraphicIndex);

		if (frameCounts[position.FrameGroup] <= 1)
			return;

		attackingMonster = monster;
		attackFrame = 0;
		attackDirection = 1;
		attackVerticalBlanks = 0;
		UpdateBattleField();
		Game.ScriptBattleAnimationShown($"attack {monster.Name}");
	}

	bool UpdateAttackAnimation(int verticalBlanks)
	{
		if (attackingMonster == null)
			return false;

		int verticalBlanksPerFrame = slowMode ? VerticalBlanksPerSlowAttackFrame : VerticalBlanksPerFastAttackFrame;
		attackVerticalBlanks += verticalBlanks;

		if (attackVerticalBlanks < verticalBlanksPerFrame)
			return false;

		attackVerticalBlanks -= verticalBlanksPerFrame;
		attackFrame += attackDirection;
		var cell = GetShownCell(attackingMonster);

		if (attackFrame <= 0 || cell == null)
		{
			attackingMonster = null;
			attackFrame = 0;
			return true;
		}

		var position = Game.AssetProvider.CombatData.GetMonsterPosition(cell.Value.X, cell.Value.Y);
		int count = Game.AssetProvider.CombatData.GetMonsterFrameCounts(attackingMonster.Monster.BattleGraphicIndex)[position.FrameGroup];

		if (attackFrame >= count)
		{
			attackFrame = count - 1;
			attackDirection = -1;
		}

		return true;
	}

	/// <summary>
	/// 6 particles fly around the monster for 24 frames on one of the particle paths (L19650 and L18502 in the original).
	/// </summary>
	void StartSpellAnimation(MonsterParticipant target, int animation)
	{
		if (animation < 1 || GetShownCell(target) == null)
			return;

		var combatData = Game.AssetProvider.CombatData;
		var spellAnimation = new SpellAnimation(target, animation, Game.Random(combatData.SpellAnimationPathCount - 1));
		spellAnimations.Add(spellAnimation);
		ShowSpellParticles(spellAnimation);
		Game.ScriptBattleAnimationShown($"spell {target.Name} {animation}");
	}

	void ShowSpellParticles(SpellAnimation spellAnimation)
	{
		spellAnimation.Particles.ForEach(particle => particle.Destroy());
		spellAnimation.Particles.Clear();

		int frame = spellAnimation.VerticalBlanks / VerticalBlanksPerSpellFrame;
		var cell = GetShownCell(spellAnimation.Target);

		if (cell == null)
			return;

		var center = GetMonsterCenter(spellAnimation.Target, cell.Value);
		int bottom = BackgroundPosition.Y + 112;

		foreach (var particle in Game.AssetProvider.CombatData.GetSpellParticles(spellAnimation.Path, frame))
		{
			int x = center.X - SpellParticleSize / 2 + particle.X;
			int y = center.Y - SpellParticleSize / 2 + particle.Y;
			// The particles are clipped at the bottom of the combat background.
			int height = Math.Min(SpellParticleSize, bottom - y);

			if (height <= 0)
				continue;

			spellAnimation.Particles.Add(new Image(Game, new Position(x, y), new Size(SpellParticleSize, height),
				Game.GraphicIndexProvider.GetSpellAnimationIconIndex(spellAnimation.Animation, particle.Icon),
				SpellParticleDisplayLayer, Layer.UI, ButtonGridPaletteIndex));
		}
	}

	void UpdateSpellAnimations(int verticalBlanks)
	{
		foreach (var spellAnimation in spellAnimations.ToList())
		{
			int frame = spellAnimation.VerticalBlanks / VerticalBlanksPerSpellFrame;
			spellAnimation.VerticalBlanks += verticalBlanks;
			int newFrame = spellAnimation.VerticalBlanks / VerticalBlanksPerSpellFrame;

			if (newFrame >= ICombatData.SpellAnimationFrameCount || GetShownCell(spellAnimation.Target) == null)
			{
				spellAnimation.Particles.ForEach(particle => particle.Destroy());
				spellAnimations.Remove(spellAnimation);
			}
			else if (newFrame != frame)
			{
				ShowSpellParticles(spellAnimation);
			}
		}
	}

	/// <summary>
	/// The damage is shown with 3 digits in the tactical cell of the participant (L1605c and L165c2 in the original).
	/// Monsters show it for 100 vertical blanks and party members for 70.
	/// </summary>
	void ShowDamage(BattleParticipant target, int damage)
	{
		RemoveDamageDisplay(target);
		var cell = GetShownCell(target);

		if (cell == null)
			return;

		var damageDisplay = new DamageDisplay(target is MonsterParticipant ? MonsterDamageVerticalBlanks : PartyDamageVerticalBlanks);
		int x = TacticalPosition.X + cell.Value.X * TacticalIconSize;
		int y = TacticalPosition.Y + cell.Value.Y * TacticalIconSize + TacticalIconSize / 2;
		var size = new Size(ICombatData.DamageDigitWidth, ICombatData.DamageDigitHeight);
		int[] digits = [damage / 100 % 10, damage / 10 % 10, damage % 10];

		for (int i = 0; i < digits.Length; i++)
		{
			int textureIndex = Game.GraphicIndexProvider.GetDamageDigitIndex(digits[i]);
			var position = new Position(x + i * DamageDigitDistance, y);
			var shadow = new Image(Game, position + new Position(1, 1), size, textureIndex, DamageDisplayLayer, Layer.UI, ButtonGridPaletteIndex)
			{
				MaskColorIndex = 0
			};
			damageDisplay.Digits.Add(shadow);
			damageDisplay.Digits.Add(new Image(Game, position, size, textureIndex, DamageDisplayLayer + 1, Layer.UI, ButtonGridPaletteIndex));
		}

		damageDisplays.Add(target, damageDisplay);
		Game.ScriptBattleAnimationShown($"damage {target.Name} {damage}");
	}

	void RemoveDamageDisplay(BattleParticipant target)
	{
		if (damageDisplays.Remove(target, out var damageDisplay))
			damageDisplay.Digits.ForEach(digit => digit.Destroy());
	}

	void UpdateDamageDisplays(int verticalBlanks)
	{
		foreach (var (participant, damageDisplay) in damageDisplays.ToList())
		{
			damageDisplay.VerticalBlanks -= verticalBlanks;

			if (damageDisplay.VerticalBlanks <= 0 || GetShownCell(participant) == null)
				RemoveDamageDisplay(participant);
		}
	}

	void ClearAnimations()
	{
		attackingMonster = null;
		waitingForAnimations = false;
		idleFrames.Clear();
		spellAnimations.ForEach(spellAnimation => spellAnimation.Particles.ForEach(particle => particle.Destroy()));
		spellAnimations.Clear();

		foreach (var participant in damageDisplays.Keys.ToList())
			RemoveDamageDisplay(participant);
	}

	#endregion
}
