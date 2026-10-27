using Amber.Common;
using Amber.Renderer.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Events;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

/// <summary>
/// The healer heals conditions of one party member (the active one at first, another one
/// can be selected by clicking its portrait). The healable conditions are shown in the item
/// slots and are healed by clicking them. The healer also destroys cursed equipped items.
/// </summary>
internal sealed class HealerScreen : PlaceScreen
{
	const int RemoveCurseButtonIndex = 3;
	const int SlotCount = 12;
	// Conditions which can be healed in the order of the slots
	static readonly Condition[] healableConditions =
	[
		Condition.Stunned, Condition.Poisoned, Condition.Petrified, Condition.Diseased, Condition.Aging,
		Condition.Mad, Condition.Blind, Condition.Dead, Condition.Ashes, Condition.Dust
	];
	readonly Image[] conditionImages = new Image[SlotCount];
	ISprite? ankh;
	readonly Condition[] slotConditions = new Condition[SlotCount];
	int patientSlot = 1;

	public override ScreenType Type { get; } = ScreenType.Healer;

	protected override Image80x80 Image { get; } = Image80x80.Healer;

	protected override Message LeaveMessage { get; } = Message.GiveGoldToHealer;

	IHealerData Data => (IHealerData)Place.Data;

	IPartyMember Patient => Game.State.GetPartyMember(patientSlot)!;

	static Position GetSlotPosition(int slot) => new(16 + (slot % 6) * 32, 37 + 108 + (slot / 6) * 32);

	public override void Init()
	{
		base.Init();

		for (int i = 0; i < SlotCount; i++)
		{
			var (x, y) = GetSlotPosition(i);
			conditionImages[i] = AddImage(x, y, 16, 16, 0, 10);
			conditionImages[i].Visible = false;
		}
	}

	public override void Open(Action? closeAction)
	{
		patientSlot = Game.State.ActivePartyMemberIndex;

		base.Open(closeAction);

		ShowPatient();
	}

	public override void Close()
	{
		foreach (var image in conditionImages)
			image.Visible = false;

		if (ankh != null)
		{
			ankh.Visible = false;
			ankh = null;
		}

		base.Close();
	}

	public override void ScreenPopped(Screen screen)
	{
		base.ScreenPopped(screen);

		if (!screen.Transparent)
			ShowPatient();
	}

	internal int ScriptPatient => patientSlot;

	private void ShowPatient()
	{
		var conditions = Patient.GetConditions();
		int slot = 0;

		foreach (var condition in healableConditions.Where(condition => conditions.HasFlag(condition)))
		{
			slotConditions[slot] = condition;
			var image = conditionImages[slot++];
			image.SetTextureIndex(Game.GraphicIndexProvider.GetStatusIconIndex(condition.ToStatusIcons()[0]));
			image.PaletteIndex = Game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI];
			image.Visible = true;
		}

		for (; slot < SlotCount; slot++)
		{
			slotConditions[slot] = Condition.None;
			conditionImages[slot].Visible = false;
		}

		Game.ResetStatusIcons();
		Game.SetStatusIcon(patientSlot - 1, StatusIcon.HandOpen, true, true);
		Game.UpdatePartyMember(patientSlot - 1);

		// The ankh is shown over the portrait of the patient (Display_Ankh in the original).
		var ankhPosition = new Position(16 + (patientSlot - 1) * 48, 1);
		ankh ??= Game.CreateSprite(Layer.UI, ankhPosition, UIGraphic.HealingCross.GetSize(),
			Game.GraphicIndexProvider.GetUIGraphicIndex(UIGraphic.HealingCross), Game.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI]);
		ankh!.Position = ankhPosition;
		ankh.DisplayLayer = 40;
		ankh.Visible = true;

		RequestButtonSetup();
	}

	private int GetHealingPrice(Condition condition) => condition switch
	{
		Condition.Stunned => Data.HealStunned,
		Condition.Poisoned => Data.HealPoison,
		Condition.Petrified => Data.HealPetrify,
		Condition.Diseased => Data.HealDisease,
		Condition.Aging => Data.HealAging,
		Condition.Mad => Data.HealMadness,
		Condition.Blind => Data.HealBlindness,
		Condition.Dead => Data.HealDeath,
		Condition.Ashes => Data.HealAshesCost,
		_ => Data.HealDustCost
	};

	private List<EquipmentSlot> GetCursedEquipment() => Patient.Equipment
		.Where(slot => slot.Value.Item != null && slot.Value.Item.Flags.HasFlag(ItemFlags.Cursed))
		.Select(slot => slot.Key)
		.ToList();

	protected override void SetupPlaceButtons(ButtonGrid buttonGrid)
	{
		buttonGrid.SetButton(RemoveCurseButtonIndex, ButtonType.UseMagic);
		buttonGrid.EnableButton(RemoveCurseButtonIndex, Place != null && GetCursedEquipment().Count != 0);
	}

	protected override void PlaceButtonClicked(int index)
	{
		if (index != RemoveCurseButtonIndex)
			return;

		var cursedEquipment = GetCursedEquipment();
		int price = cursedEquipment.Count * Data.RemoveCursePrice;

		if (price > PlaceGold)
		{
			ShowMessage(Message.NotEnoughGoldForHealer);
			return;
		}

		AskToBuy(Game.LoadMessageText(Message.DestroyCursedItemsPrice), price, () =>
		{
			// The equipment of the patient is shown while the items are destroyed (RemCurseYes in the original).
			Game.EquipmentToDestroy = cursedEquipment;
			Game.State.SetCurrentInventory(patientSlot);
			Game.ScreenHandler.PushScreen(ScreenType.Inventory);
		});
	}

	/// <summary>
	/// Heals the condition in the given slot (0-based). This is also used by scenario scripts.
	/// </summary>
	internal void HealConditionInSlot(int slot)
	{
		if (slot < 0 || slot >= SlotCount || slotConditions[slot] == Condition.None)
			return;

		var condition = slotConditions[slot];
		int price = GetHealingPrice(condition);

		if (price > PlaceGold)
		{
			ShowMessage(Message.NotEnoughGoldForHealer);
			return;
		}

		AskToBuy(Game.LoadMessageText(Message.HealingPrice), price, () =>
		{
			var patient = Patient;
			patient.RemoveCondition(condition);

			// resurrection
			if (condition is Condition.Dead or Condition.Ashes or Condition.Dust && !patient.IsDead())
				patient.HitPoints.CurrentValue = 1;

			ShowPatient();
		});
	}

	protected override bool PlaceMouseDown(Position position, MouseButtons buttons)
	{
		if (buttons != MouseButtons.Left)
			return false;

		int? portraitSlot = Game.TestPartyPortraitHit(position);

		if (portraitSlot != null)
		{
			patientSlot = portraitSlot.Value;
			ShowPatient();
			return true;
		}

		for (int i = 0; i < SlotCount; i++)
		{
			if (new Rect(GetSlotPosition(i), new Size(16, 16)).Contains(position))
			{
				HealConditionInSlot(i);
				return true;
			}
		}

		return false;
	}
}
