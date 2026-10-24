using Amberstar.GameData;

namespace Amberstar.Game;

/// <summary>
/// The altar where the Amberstar is assembled. The routines are not part of the
/// released original source code but of the program (disassembled).
/// </summary>
partial class Game
{
	internal const uint PieceOfAmberstarItemIndex = 110;
	const int AltarRewardValue = 10;
	internal const int AltarSong = 13;
	const int AmberstarAssembledSong = 6;

	/// <summary>
	/// Rewards the active party member for a piece which was laid at the right place.
	/// Each step raises another value by 10 without any limit like in the original.
	/// </summary>
	internal void RewardAltarPiece(IPartyMember partyMember, int step)
	{
		static word Add(word value) => (word)Math.Min(word.MaxValue, value + AltarRewardValue);

		void AddSkill(Skill skill) => partyMember.Skills[skill].CurrentValue = Add(partyMember.Skills[skill].CurrentValue);
		void AddAttribute(GameData.Attribute attribute) => partyMember.Attributes[attribute].CurrentValue = Add(partyMember.Attributes[attribute].CurrentValue);

		switch (step)
		{
			case 0: AddSkill(Skill.UseMagic); break;
			case 1: AddAttribute(GameData.Attribute.AntiMagic); break;
			case 2: AddSkill(Skill.ReadMagic); break;
			case 3: partyMember.HitPoints.MaxValue = Add(partyMember.HitPoints.MaxValue); break;
			case 4: partyMember.SpellPoints.MaxValue = Add(partyMember.SpellPoints.MaxValue); break;
			case 5: AddAttribute(GameData.Attribute.Luck); break;
			case 6: AddAttribute(GameData.Attribute.Intelligence); break;
			case 7: AddAttribute(GameData.Attribute.Speed); break;
			case 8: AddAttribute(GameData.Attribute.Stamina); break;
			case 9: AddAttribute(GameData.Attribute.Strength); break;
			case 10: AddAttribute(GameData.Attribute.Dexterity); break;
			case 11: AddAttribute(GameData.Attribute.Charisma); break;
		}

		UpdatePartyMembers();
	}

	/// <summary>
	/// A lightning bolt turns the party member into ashes when a wrong piece is laid.
	/// </summary>
	internal void BurnAtAltar(IPartyMember partyMember)
	{
		partyMember.HitPoints.CurrentValue = 0;
		partyMember.AddCondition(Condition.Ashes);
		PartyMembersChanged();
	}

	/// <summary>
	/// All pieces were laid: the pieces are removed from the inventories and the party gets the Amberstar.
	/// </summary>
	internal void AssembleAmberstar()
	{
		foreach (var partyMember in State.PartyMembers)
		{
			foreach (var slot in partyMember.Inventory)
			{
				if (slot.Count != 0 && slot.Item?.Index == PieceOfAmberstarItemIndex)
					DestroyUsedItem(partyMember, slot, false, slot.Count);
			}
		}

		PlaySong(AmberstarAssembledSong);
		State.SpecialItems |= SpecialItems.Amberstar;
		UpdatePartyMembers();
	}
}
