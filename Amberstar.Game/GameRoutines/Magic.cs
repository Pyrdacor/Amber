using Amber.Common;
using Amberstar.Game.Screens;
using Amberstar.GameData;
using Amberstar.GameData.Events;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game;

/// <summary>
/// Casting spells outside of battles (MAGIC.ASM of the original).
/// </summary>
partial class Game
{
	// conditions of party members which are not alive (like $e000 in the original)
	const Condition NotAliveConditions = Condition.Dead | Condition.Ashes | Condition.Dust;

	/// <summary>
	/// An item whose spell is cast (Magic_object_flag in the original).
	/// </summary>
	sealed record MagicItem(IPartyMember Owner, ItemSlot ItemSlot, bool Equipped);

	/// <summary>
	/// An item of a party member is chosen for a spell.
	/// </summary>
	sealed record SpellItemSelection(Message Message, bool EquipmentOnly, Action<ItemSlot?, bool> Selected);

	MagicItem? magicItem;
	SpellItemSelection? spellItemSelection;
	ISpellData? currentSpellData;
	// party members whose attacks were doubled by haste (Hurry_bitlist in the original)
	readonly HashSet<IPartyMember> hastedPartyMembers = [];
	// Note: X-ray vision is not part of the original source code. Here it lasts some steps.
	const int XRayVisionSteps = 10;

	/// <summary>
	/// The remaining steps in which the party sees through walls.
	/// </summary>
	internal int XRayVisionStepsLeft { get; private set; }

	/// <summary>
	/// Counts down the x-ray vision when the party moved.
	/// </summary>
	internal void XRayVisionStep()
	{
		if (XRayVisionStepsLeft > 0)
			XRayVisionStepsLeft--;
	}

	/// <summary>
	/// The party member who casts the spell.
	/// </summary>
	internal IPartyMember? SpellCaster { get; private set; }

	/// <summary>
	/// Where the spell is cast (Spell_area_type in the original).
	/// </summary>
	internal SpellLocation SpellLocation { get; private set; }

	/// <summary>
	/// The spell which was chosen in the spell selection.
	/// </summary>
	internal (SpellSchool School, int SpellNumber)? SelectedSpell { get; set; }

	/// <summary>
	/// The party member slot which was chosen as the target of a spell.
	/// </summary>
	internal int? SelectedPartyMemberSlot { get; set; }

	/// <summary>
	/// The message while an item for a spell is chosen.
	/// </summary>
	internal Message? SpellItemMessage => spellItemSelection?.Message;

	internal bool SpellItemEquipmentOnly => spellItemSelection?.EquipmentOnly == true;

	/// <summary>
	/// Called by the inventory when an item for a spell was chosen.
	/// </summary>
	internal void SpellItemPicked(ItemSlot? itemSlot, bool equipped)
	{
		var selection = spellItemSelection;
		spellItemSelection = null;

		if (selection != null)
			AfterScreenClosed(() => selection.Selected(itemSlot, equipped));
	}

	/// <summary>
	/// Number of spells of each school (Spells_per_class in the original).
	/// </summary>
	internal static int GetSpellCount(SpellSchool school) => school switch
	{
		SpellSchool.White => 28,
		SpellSchool.Gray => 26,
		SpellSchool.Black => 22,
		SpellSchool.Special => 19,
		_ => 0
	};

	internal static bool CanCastSpellAt(ISpellData spellData, SpellLocation location) => (spellData.LocationFlags & (1 << (int)location)) != 0;

	internal ISpellData LoadSpellData(SpellSchool school, int spellNumber) => AssetProvider.SpellDataLoader.LoadSpellData(school, spellNumber);

	internal IText LoadSpellName(SpellSchool school, int spellNumber) =>
		AssetProvider.TextLoader.LoadText(new AssetIdentifier(AssetType.SpellName, ((int)school - 1) * ISpellData.SpellsPerSchool + spellNumber));

	/// <summary>
	/// Gets the spell schools of which the party member knows any spell.
	/// </summary>
	internal static List<SpellSchool> GetSpellSchoolsWithKnownSpells(IPartyMember partyMember)
	{
		return Enum.GetValues<SpellSchool>()
			.Where(school => partyMember.LearnedSpellSchools.HasFlag((SpellSchoolFlags)(1 << (int)school)) && GetSpellCount(school) != 0)
			.Where(school => Enumerable.Range(1, GetSpellCount(school)).Any(spell => HasLearnedSpell(partyMember, school, spell)))
			.ToList();
	}

	/// <summary>
	/// The spell location of the map with the party (Set_spell_area in the original).
	/// </summary>
	internal SpellLocation GetMapSpellLocation()
	{
		var map = AssetProvider.MapLoader.LoadMap(State.GetIndexOfMapWithPlayer());

		if (map.Flags.HasFlag(MapFlags.Wilderness))
			return SpellLocation.Wilderness;

		return map.Flags.HasFlag(MapFlags.City) ? SpellLocation.City : SpellLocation.Cave;
	}

	/// <summary>
	/// The active party member chooses and casts a spell (Cast_spell in the original).
	/// </summary>
	internal void CastSpell(SpellLocation location, Action? finished = null)
	{
		var caster = State.ActivePartyMember;

		if (caster == null || GetSpellSchoolsWithKnownSpells(caster).Count == 0)
		{
			finished?.Invoke();
			return;
		}

		SpellCaster = caster;
		SpellLocation = location;
		SelectedSpell = null;
		magicItem = null;

		ScreenHandler.PushScreen(ScreenType.SpellSelect, () => AfterScreenClosed(() =>
		{
			if (SelectedSpell is var (school, spellNumber))
				ExecuteSpell(school, spellNumber, finished ?? (() => { }));
			else
				finished?.Invoke();
		}));
	}

	/// <summary>
	/// Casts the spell of an item (UseItem_entry with Magic_object_flag in the original).
	/// </summary>
	internal void CastItemSpell(IPartyMember owner, ItemSlot itemSlot, bool equipped, SpellLocation location, Action finished)
	{
		var item = itemSlot.Item!;
		var spellData = LoadSpellData(item.SpellSchool, item.SpellIndex);

		if (!CanCastSpellAt(spellData, location))
		{
			ShowTextMessage(LoadMessageText(Message.SpellDoesNotWorkHere), finished);
			return;
		}

		SpellCaster = owner;
		SpellLocation = location;
		magicItem = new(owner, itemSlot, equipped);
		ExecuteSpell(item.SpellSchool, item.SpellIndex, finished);
	}

	/// <summary>
	/// The screen handler invokes the follow action of a screen while it is closed.
	/// Further screens are opened afterwards.
	/// </summary>
	internal void AfterScreenClosed(Action action) => AddDelayedAction(0, action);

	const int SparkleSteps = 12;
	const long TicksPerSparkleStep = 3;

	/// <summary>
	/// Shows magic sparkles over the portrait of a party member (Do_sparkle in the original).
	/// </summary>
	void ShowMagicSparkle(IPartyMember partyMember)
	{
		var (slotIndex, _) = State.PartyMembersWithSlot.FirstOrDefault(p => p.PartyMember == partyMember, (-1, null));

		if (slotIndex == -1)
			return;

		var layer = GetRenderLayer(Layer.UI);
		var sparkle = layer.SpriteFactory!.CreateAnimated();
		sparkle.Position = new(16 + slotIndex * 48, 1);
		sparkle.Size = new(32, 32);
		sparkle.TextureOffset = layer.Config.Texture!.GetOffset(GraphicIndexProvider.GetUIGraphicIndex(UIGraphic.MagicAnimation));
		sparkle.DisplayLayer = 50;
		sparkle.PaletteIndex = PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI];
		sparkle.FrameCount = 3;
		sparkle.CurrentFrameIndex = 0;
		sparkle.Visible = true;

		int step = 0;

		void NextStep()
		{
			if (++step == SparkleSteps)
			{
				sparkle.Visible = false;
				return;
			}

			// like Magic_anim_counter in the original
			sparkle.CurrentFrameIndex = step * 3 / SparkleSteps;
			AddDelayedAction(TicksPerSparkleStep, NextStep);
		}

		AddDelayedAction(TicksPerSparkleStep, NextStep);
	}

	/// <summary>
	/// Executes the spell (Do_magic in the original).
	/// </summary>
	void ExecuteSpell(SpellSchool school, int spellNumber, Action finished)
	{
		currentSpellData = LoadSpellData(school, spellNumber);

		void Done()
		{
			magicItem = null;
			UpdatePartyMembers();
			finished();
		}

		switch (school)
		{
			case SpellSchool.White:
				ExecuteWhiteSpell((WhiteSpell)spellNumber, Done);
				break;
			case SpellSchool.Gray:
				ExecuteGraySpell((GraySpell)spellNumber, Done);
				break;
			case SpellSchool.Special:
				ExecuteSpecialSpell((SpecialSpell)spellNumber, Done);
				break;
			default:
				// All black magic spells are combat spells.
				ShowNotImplementedMessage("Casting combat spells", Done);
				break;
		}
	}

	/// <summary>
	/// Uses the spell points or the magic item and checks if the spell succeeds (Handle_spell in the original).
	/// If the spell fails, a message is shown and false is returned.
	/// </summary>
	bool TrySpell(Action finished)
	{
		if (SpellSucceeds())
			return true;

		ShowTextMessage(LoadMessageText(Message.SpellFails), finished);

		return false;
	}

	/// <summary>
	/// Uses the spell points or the magic item. Spells of items always succeed,
	/// otherwise the skill to use magic is probed.
	/// </summary>
	bool SpellSucceeds()
	{
		if (magicItem != null)
		{
			UseMagicItemCharge(magicItem);
			return true;
		}

		var caster = SpellCaster!;
		caster.SpellPoints.CurrentValue = (word)Math.Max(0, caster.SpellPoints.CurrentValue - currentSpellData!.SpellPointCost);

		return Probe(caster.Skills[Skill.UseMagic].TotalCurrent);
	}

	/// <summary>
	/// Removes a charge of a magic item or the item itself (Remove_used_magic_item in the original).
	/// Stackable items and items without charges left are removed. 255 charges are infinite.
	/// </summary>
	void UseMagicItemCharge(MagicItem magicItem)
	{
		var (owner, itemSlot, equipped) = magicItem;
		var item = itemSlot.Item!;

		if (!item.IsStackable() && item.SpellCharges != byte.MaxValue)
		{
			if (item.SpellCharges > 1)
			{
				var newItem = AssetProvider.ItemFactory.Create(item, new ItemSlotData((byte)(item.SpellCharges - 1), item.SlotFlags));
				itemSlot.SetItem(newItem, itemSlot.Count);
				return;
			}
		}
		else if (item.SpellCharges == byte.MaxValue)
		{
			return;
		}

		DestroyUsedItem(owner, itemSlot, equipped);
	}

	/// <summary>
	/// Lets the player choose a party member as the target of a spell (VictimSel_Mod in the original).
	/// Nothing happens if no party member is chosen.
	/// </summary>
	void SelectSpellTarget(Message message, Action finished, Action<int, IPartyMember> selected)
	{
		CurrentText = LoadMessageText(message);
		SelectedPartyMemberSlot = null;

		ScreenHandler.PushScreen(ScreenType.SelectPartyMember, () => AfterScreenClosed(() =>
		{
			int? slot = SelectedPartyMemberSlot;
			var target = slot == null ? null : State.GetPartyMember(slot.Value);

			if (target == null)
				finished();
			else
				selected(slot!.Value, target);
		}));
	}

	/// <summary>
	/// Lets the player choose a party member and then an item of the party member in the
	/// inventory (e.g. RemCurse2_Mod in the original). The inventory is opened if needed.
	/// Nothing happens if no valid item is chosen.
	/// </summary>
	void SelectSpellItem(Message targetMessage, Message itemMessage, bool equipmentOnly, Func<IItem, bool> isValid,
		Action finished, Action<IPartyMember, ItemSlot, bool, Action> selected)
	{
		SelectSpellTarget(targetMessage, finished, (slot, target) =>
		{
			bool openInventory = ScreenHandler.ActiveScreen is not InventoryScreen;

			void Finish() => AfterScreenClosed(() =>
			{
				if (ScreenHandler.ActiveScreen is InventoryScreen inventoryScreen)
				{
					if (openInventory)
						ScreenHandler.PopScreen();
					else
						inventoryScreen.Refresh();
				}

				finished();
			});

			spellItemSelection = new(itemMessage, equipmentOnly, (itemSlot, equipped) =>
			{
				if (itemSlot?.Item == null || itemSlot.Count == 0 || !isValid(itemSlot.Item))
					Finish();
				else
					selected(target, itemSlot, equipped, Finish);
			});

			if (openInventory)
			{
				State.SetCurrentInventory(slot);
				ScreenHandler.PushScreen(ScreenType.Inventory);
			}
			else
			{
				OpenInventory(slot);
				ScreenHandler.PushScreen(ScreenType.InventorySpellItem);
			}
		});
	}

	#region White magic

	void ExecuteWhiteSpell(WhiteSpell spell, Action finished)
	{
		switch (spell)
		{
			case WhiteSpell.Healing1:
				HealSpell(1, 5, finished);
				break;
			case WhiteSpell.Healing2:
				HealSpell(2, 8, finished);
				break;
			case WhiteSpell.Healing3:
				HealSpell(4, 16, finished);
				break;
			case WhiteSpell.Healing4:
				HealSpell(8, 32, finished);
				break;
			case WhiteSpell.Healing5:
				HealSpell(16, 64, finished);
				break;
			case WhiteSpell.Salvation:
				if (TrySpell(finished))
				{
					ForeachPartyMember(partyMember =>
					{
						if (partyMember.HitPoints.CurrentValue < partyMember.HitPoints.TotalMax)
						{
							partyMember.FillHitPoints();
							ShowMagicSparkle(partyMember);
						}
					}, NotAliveConditions);
					finished();
				}
				break;
			case WhiteSpell.Reincarnation:
				ConvertCorpseSpell(Message.WhoToResurrect, Condition.Dead, Condition.Ashes, finished);
				break;
			case WhiteSpell.ConversionOfAshes:
				ConvertCorpseSpell(Message.WhoseAshesToTransform, Condition.Ashes, Condition.Dust, finished);
				break;
			case WhiteSpell.ConversionOfDust:
				ConvertCorpseSpell(Message.WhoseDustToTransform, Condition.Dust, null, finished);
				break;
			case WhiteSpell.NeutralizePoison:
				HealConditionSpell(Message.WhosePoisonToCure, Condition.Poisoned, finished);
				break;
			case WhiteSpell.HealStun:
				HealConditionSpell(Message.WhoseParalysisToCure, Condition.Stunned, finished);
				break;
			case WhiteSpell.HealSickness:
				HealConditionSpell(Message.WhoseDiseaseToCure, Condition.Diseased, finished);
				break;
			case WhiteSpell.Rejuvation:
				HealConditionSpell(Message.WhoseAgingToCure, Condition.Aging, finished);
				break;
			case WhiteSpell.Depetrification:
				HealConditionSpell(Message.WhosePetrificationToCure, Condition.Petrified, finished);
				break;
			case WhiteSpell.WakeUp:
				// Note: The original code uses the wrong pointer here and doesn't work.
				if (TrySpell(finished))
				{
					ForeachPartyMember(partyMember =>
					{
						partyMember.RemoveCondition(Condition.Sleeping);
						ShowMagicSparkle(partyMember);
					}, NotAliveConditions);
					finished();
				}
				break;
			case WhiteSpell.CalmPanic:
				HealConditionSpell(Message.WhosePanicToRemove, Condition.Panicked, finished);
				break;
			case WhiteSpell.RemoveIrritation:
				HealConditionSpell(Message.WhoseIrritationToRemove, Condition.Irritated, finished);
				break;
			case WhiteSpell.HealBlindness:
				HealConditionSpell(Message.WhoseBlindnessToCure, Condition.Blind, finished);
				break;
			case WhiteSpell.HealMadness:
				HealConditionSpell(Message.WhoseMadnessToCure, Condition.Mad, finished);
				break;
			case WhiteSpell.ProvideFood:
				if (TrySpell(finished))
				{
					ForeachPartyMember(partyMember =>
					{
						partyMember.Food++;
						partyMember.TotalWeight += FoodWeight;
					}, NotAliveConditions);
					finished();
				}
				break;
			case WhiteSpell.RemoveCurse:
				// Remove_curse destroys a cursed equipped item.
				SelectSpellItem(Message.WhoseCursedItemToDestroy, Message.WhichCurseToRemove, true, item => item.Flags.HasFlag(ItemFlags.Cursed),
					finished, (target, itemSlot, _, done) =>
				{
					if (TrySpell(done))
					{
						// Note: The original doesn't remove the mali of the item.
						DestroyUsedItem(target, itemSlot, true, itemSlot.Count);
						done();
					}
				});
				break;
			default:
				// stun, sleep, fear, irritation, blind, destroy undead and holy word
				ShowNotImplementedMessage("Casting combat spells", finished);
				break;
		}
	}

	/// <summary>
	/// Heals a random amount of hit points (Heal_1 to Heal_5 in the original).
	/// </summary>
	void HealSpell(int minAmount, int range, Action finished)
	{
		int amount = minAmount + Random(range - 1);

		SelectSpellTarget(Message.WhoToHeal, finished, (_, target) =>
		{
			if (target.HasAnyConditionOf(NotAliveConditions))
				finished();
			else if (TrySpell(finished))
			{
				// Increase_LP
				if (target.HitPoints.CurrentValue < target.HitPoints.TotalMax)
				{
					target.HealHitPoints((word)amount);
					ShowMagicSparkle(target);
				}

				finished();
			}
		});
	}

	/// <summary>
	/// Heals a condition of a party member (Common_heal_condition in the original).
	/// </summary>
	void HealConditionSpell(Message message, Condition condition, Action finished)
	{
		SelectSpellTarget(message, finished, (_, target) =>
		{
			if (!target.HasAnyConditionOf(condition))
				finished();
			else if (TrySpell(finished))
			{
				target.RemoveCondition(condition);
				ShowMagicSparkle(target);
				finished();
			}
		});
	}

	/// <summary>
	/// Turns dust to ashes, ashes to a corpse and a corpse to a living person (Resurrect, Change_ashes
	/// and Change_dust in the original). If the spell fails, the next worse condition is added.
	/// </summary>
	void ConvertCorpseSpell(Message message, Condition condition, Condition? failCondition, Action finished)
	{
		SelectSpellTarget(message, finished, (_, target) =>
		{
			if (!target.HasAnyConditionOf(condition))
			{
				finished();
				return;
			}

			bool success = SpellSucceeds();

			if (success)
			{
				target.RemoveCondition(condition);

				// Resurrection gives one hit point.
				if (!target.IsDead())
					target.HitPoints.CurrentValue = 1;
			}
			else if (failCondition != null)
			{
				target.AddCondition(failCondition.Value);
			}

			ShowMagicSparkle(target);

			if (success)
				finished();
			else
				ShowTextMessage(LoadMessageText(Message.SpellFails), finished);
		});
	}

	#endregion

	#region Gray magic

	/// <summary>
	/// Indices of the active spells (Spell_1_data to Spell_6_data in the original).
	/// </summary>
	internal enum ActiveSpellType
	{
		Light,
		ArmorProtection,
		WeaponPower,
		AntiMagic,
		Clairvoyance,
		Invisibility
	}

	/// <summary>
	/// The active spells last for some time steps of 5 minutes (Every_step in the original).
	/// </summary>
	void UpdateActiveSpells()
	{
		var activeSpells = State.ActiveSpells;

		for (int i = 0; i < activeSpells.Length; i++)
		{
			if (activeSpells[i].Duration != 0)
				activeSpells[i].Duration--;
		}
	}

	void ActivateSpell(ActiveSpellType type, int value, int duration)
	{
		State.ActiveSpells[(int)type] = new ActiveSpell { Value = value, Duration = duration };
	}

	void ExecuteGraySpell(GraySpell spell, Action finished)
	{
		// Light, armor, weapon, anti-magic, clairvoyance and invisibility
		// spells have 3 levels with a duration of 24, 96 or 192 steps.
		if (spell >= GraySpell.Light1 && spell <= GraySpell.Invisibility3)
		{
			if (TrySpell(finished))
			{
				int index = spell - GraySpell.Light1;
				int level = index % 3;
				var type = (ActiveSpellType)(index / 3);
				int duration = level switch { 0 => 24, 1 => 96, _ => 192 };
				int value = type switch
				{
					ActiveSpellType.Light or ActiveSpellType.Clairvoyance => 2 + level * 2,
					ActiveSpellType.Invisibility => 0,
					_ => 5 + level * 5
				};

				ActivateSpell(type, value, duration);
				finished();
			}

			return;
		}

		switch (spell)
		{
			case GraySpell.MagicSphere:
				if (TrySpell(finished))
				{
					ActivateSpell(ActiveSpellType.Light, 6, 192);
					ActivateSpell(ActiveSpellType.ArmorProtection, 15, 192);
					ActivateSpell(ActiveSpellType.WeaponPower, 15, 192);
					ActivateSpell(ActiveSpellType.AntiMagic, 15, 192);
					ActivateSpell(ActiveSpellType.Clairvoyance, 6, 192);
					ActivateSpell(ActiveSpellType.Invisibility, 0, 192);
					finished();
				}
				break;
			case GraySpell.Haste:
				SelectSpellTarget(Message.WhoToHaste, finished, (_, target) =>
				{
					if (hastedPartyMembers.Contains(target))
						finished();
					else if (TrySpell(finished))
					{
						Haste(target);
						finished();
					}
				});
				break;
			case GraySpell.MassHaste:
			{
				var notHasted = State.PartyMembers.Where(partyMember => !hastedPartyMembers.Contains(partyMember) && !partyMember.HasAnyConditionOf(NotAliveConditions)).ToList();

				if (notHasted.Count == 0)
				{
					finished();
					break;
				}

				if (TrySpell(finished))
				{
					notHasted.ForEach(Haste);
					finished();
				}
				break;
			}
			case GraySpell.MagicCompass:
				if (TrySpell(finished))
					ScreenHandler.PushScreen(ScreenType.MagicCompass, () => AfterScreenClosed(finished));
				break;
			case GraySpell.Identification:
				SelectSpellItem(Message.WhoseItemToExamine, Message.WhichItem, false, item => !item.SlotFlags.HasFlag(ItemSlotFlags.Identified),
					finished, (_, itemSlot, _, done) =>
				{
					if (TrySpell(done))
					{
						IdentifyItem(itemSlot);
						CurrentItem = itemSlot.Item;
						ScreenHandler.PushScreen(ScreenType.ItemView, done);
					}
				});
				break;
			case GraySpell.Levitation:
				Levitate(finished);
				break;
			// Note: Teleport and x-ray vision are not part of the original source code.
			case GraySpell.Teleport:
				if (ScreenHandler.FindMapScreen() is not Map2DScreen mapScreen || mapScreen.FindTeleportTarget() is not Position target)
					ShowTextMessage(LoadMessageText(Message.SpellDoesNotWorkHere), finished);
				else if (TrySpell(finished))
				{
					finished();
					mapScreen.TeleportParty(target);
				}
				break;
			case GraySpell.XRayVision:
				if (TrySpell(finished))
				{
					XRayVisionStepsLeft = XRayVisionSteps;
					finished();
				}
				break;
			default:
				ShowNotImplementedMessage($"The spell {spell}", finished);
				break;
		}
	}

	/// <summary>
	/// Goes up through a trap door in the ceiling at the party position or down through
	/// a trap door in the floor in front of the party without falling (Levitate in the original).
	/// </summary>
	void Levitate(Action finished)
	{
		var mapScreen = ScreenHandler.FindMapScreen();
		var position = State.PartyPosition;
		var trapDoor = mapScreen?.GetActiveEvent(position.X, position.Y) as ITrapDoorEvent;

		if (trapDoor?.Floor != false)
		{
			var front = State.PartyDirection switch
			{
				Direction.Up => new Position(position.X, position.Y - 1),
				Direction.Right => new Position(position.X + 1, position.Y),
				Direction.Down => new Position(position.X, position.Y + 1),
				_ => new Position(position.X - 1, position.Y)
			};

			trapDoor = mapScreen?.GetActiveEvent(front.X, front.Y) as ITrapDoorEvent;

			if (trapDoor?.Floor != true)
				trapDoor = null;
		}

		if (trapDoor == null || !TrySpell(finished))
		{
			if (trapDoor == null)
				finished();

			return;
		}

		if (trapDoor.SaveIndex != 0)
			State.SetEventBit(State.GetIndexOfMapWithPlayer(), trapDoor.SaveIndex, true);

		finished();
		Teleport(trapDoor.X, trapDoor.Y, Direction.Keep, trapDoor.MapIndex, true);
	}

	/// <summary>
	/// Doubles the attacks per round (Hurry in the original).
	/// </summary>
	void Haste(IPartyMember partyMember)
	{
		hastedPartyMembers.Add(partyMember);
		partyMember.AttacksPerRound = (byte)Math.Min(byte.MaxValue, partyMember.AttacksPerRound * 2);
		ShowMagicSparkle(partyMember);
	}

	#endregion

	#region Special magic

	void ExecuteSpecialSpell(SpecialSpell spell, Action finished)
	{
		switch (spell)
		{
			case SpecialSpell.SpellPoints1:
				SelectSpellTarget(Message.WhoseSpellPointsToRestore, finished, (_, target) =>
				{
					if (TrySpell(finished))
					{
						target.HealSpellPoints((word)Random(1, 25));
						ShowMagicSparkle(target);
						finished();
					}
				});
				break;
			case SpecialSpell.SpellPoints2:
				SelectSpellTarget(Message.WhoseSpellPointsToRestore, finished, (_, target) =>
				{
					if (TrySpell(finished))
					{
						if (target.SpellPoints.CurrentValue < target.SpellPoints.TotalMax)
						{
							target.FillSpellPoints();
							ShowMagicSparkle(target);
						}

						finished();
					}
				});
				break;
			case SpecialSpell.Youth:
				SelectSpellTarget(Message.WhoToRejuvenate, finished, (_, target) =>
				{
					if (TrySpell(finished))
					{
						int age = target.Age.CurrentValue - Random(1, 10);

						// The party member can't be younger than 15.
						if (age >= 15)
						{
							target.Age.CurrentValue = (word)age;
							ShowMagicSparkle(target);
						}

						finished();
					}
				});
				break;
			case SpecialSpell.Mapshow:
				// Map_view (the spell points are used even if the map can't be shown)
				if (TrySpell(finished))
				{
					if (ShownMap?.Flags.HasFlag(MapFlags.CanUseMapViewSpell) == true)
						ScreenHandler.PushScreen(ScreenType.Minimap, () => AfterScreenClosed(finished));
					else
						ShowTextMessage(LoadMessageText(Message.CrystalBallBlockedByMagic), finished);
				}
				break;
			case SpecialSpell.WeaponBalm:
				// Weapon_gunk increases the damage of a weapon.
				SelectSpellItem(Message.ApplyPotionToWhoseWeapon, Message.ApplyBalmToWhat, false,
					item => item.Type is ItemType.CloseRangedWeapon or ItemType.LongRangedWeapon, finished, (target, itemSlot, equipped, done) =>
				{
					if (TrySpell(done))
					{
						var item = itemSlot.Item!;
						var newItem = item.WithDamage((byte)Math.Min(127, item.Damage + Random(1, 4)));

						// the boni of equipped items contain the damage
						if (equipped)
							ApplyEquipmentBoni(target, item, false);

						itemSlot.SetItem(newItem, itemSlot.Count);

						if (equipped)
							ApplyEquipmentBoni(target, newItem, true);

						done();
					}
				});
				break;
			case >= SpecialSpell.Stun and <= SpecialSpell.FleshToStone2:
			case SpecialSpell.BanishDemon:
				ShowNotImplementedMessage("Casting combat spells", finished);
				break;
			case SpecialSpell.EagleCall:
				if (ScreenHandler.FindMapScreen() is not Map2DScreen mapScreen || !mapScreen.CanCallEagle())
					ShowTextMessage(LoadMessageText(Message.SpellDoesNotWorkHere), finished);
				else if (TrySpell(finished))
				{
					mapScreen.CallEagle();
					finished();
				}
				break;
			case SpecialSpell.PickLock:
				// Lockpicks only work at locked doors and chests (Use_item_DOOR in the original).
				ShowTextMessage(LoadMessageText(Message.SpellDoesNotWorkHere), finished);
				break;
			default:
				// TODO: The music spell is not part of the original source code.
				ShowNotImplementedMessage($"The spell {spell}", finished);
				break;
		}
	}

	#endregion
}
