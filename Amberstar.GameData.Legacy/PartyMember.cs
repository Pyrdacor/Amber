using Amber.Assets.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy;

internal class PartyMember : BattleCharacter, IPartyMember
{
    private byte defaultBattlePosition;
    private word attackPerRoundLevel;
    private word hitPointsPerLevel;
    private word spellPointsPerLevel;
    private word spellLearningPointsPerLevel;
    private word saveBit;
    private IConversationData? conversationData;
    // The original character record. Unknown values are kept from it when saving.
    private byte[] rawData = [];

    public static PartyMember Load(IAsset asset, ITextLoader textLoader)
    {
        var partyMember = new PartyMember();
        var reader = asset.GetReader();

        BattleCharacter.Load(partyMember, reader);
        partyMember.conversationData = Legacy.ConversationData.Load(reader, textLoader);

        reader.Position = 0;
        partyMember.rawData = reader.ReadBytes(reader.Size);

        reader.Position = 0x1a;
        partyMember.LearnedSpellSchools = (SpellSchoolFlags)reader.ReadByte();

        reader.Position = 0x42;
        partyMember.defaultBattlePosition = reader.ReadByte();

        reader.Position = 0x46;
        partyMember.PossibleClasses = (ClassFlags)reader.ReadWord();

        reader.Position = 0x70;
        partyMember.attackPerRoundLevel = reader.ReadWord();
        partyMember.hitPointsPerLevel = reader.ReadWord();
        partyMember.spellPointsPerLevel = reader.ReadWord();
        partyMember.spellLearningPointsPerLevel = reader.ReadWord();

        reader.Position = 0x8e;
        partyMember.SpellLearningPoints = reader.ReadWord();

        reader.Position = 0xc4;
        partyMember.saveBit = reader.ReadWord();

        reader.Position = 0xcc;
        partyMember.ExperiencePoints = reader.ReadDword();
        partyMember.LearnedWhiteSpells = reader.ReadDword();
        partyMember.LearnedGraySpells = reader.ReadDword();
        partyMember.LearnedBlackSpells = reader.ReadDword();
        reader.Position += 3 * 4; // Skip unused spells
        partyMember.LearnedSpecialSpells = reader.ReadDword();
        partyMember.TotalWeight = reader.ReadDword();

        // We ensure that some equipment related values are correctly set.


        return partyMember;
    }

    public ClassFlags PossibleClasses { get; set; }
    public byte DefaultBattlePosition { get => defaultBattlePosition; init => defaultBattlePosition = value; }
    public word AttackPerRoundLevel { get => attackPerRoundLevel; set => attackPerRoundLevel = value; }
    public word HitPointsPerLevel { get => hitPointsPerLevel; set => hitPointsPerLevel = value; }
    public word SpellPointsPerLevel { get => spellPointsPerLevel; set => spellPointsPerLevel = value; }
    public word SpellLearningPointsPerLevel { get => spellLearningPointsPerLevel; set => spellLearningPointsPerLevel = value; }
    public word SpellLearningPoints { get; set; }
    public word SaveBit { get => saveBit; set => saveBit = value; }
    public dword ExperiencePoints { get; set; }
    public SpellSchoolFlags LearnedSpellSchools { get; set; }
    public dword LearnedWhiteSpells { get; set; }
    public dword LearnedGraySpells { get; set; }
    public dword LearnedBlackSpells { get; set; }
    public dword LearnedSpecialSpells { get; set; }
    public dword TotalWeight { get; set; }
    public IConversationData ConversationData { get => conversationData ?? throw new NullReferenceException("conversationData is null"); init => conversationData = value; }


    /// <summary>
    /// Writes the character record. All values which are not known
    /// are taken from the original record.
    /// </summary>
    public byte[] Write()
    {
        var data = (byte[])rawData.Clone();

        void WriteByte(int offset, int value) => data[offset] = (byte)value;

        void WriteWord(int offset, int value)
        {
            data[offset] = (byte)(value >> 8);
            data[offset + 1] = (byte)value;
        }

        void WriteDword(int offset, uint value)
        {
            WriteWord(offset, (int)(value >> 16));
            WriteWord(offset + 2, (int)(value & 0xffff));
        }

        var character = (IPartyMember)this;
        var skills = Enum.GetValues<Skill>().Skip(1).ToArray();
        var attributes = Enum.GetValues<Attribute>().Skip(1).ToArray();

        WriteByte(0x05, (int)character.Class);

        for (int i = 0; i < skills.Length; i++)
        {
            var skill = Skills[skills[i]];
            WriteByte(0x06 + i, skill.CurrentValue);
            WriteByte(0x10 + i, skill.MaxValue);
            WriteWord(0xb0 + i * 2, skill.BonusValue);
        }

        WriteByte(0x1a, (int)LearnedSpellSchools);
        WriteByte(0x1b, Level);
        WriteByte(0x1c, UsedHands);
        WriteByte(0x1d, UsedFingers);
        WriteByte(0x1e, Defense);
        WriteByte(0x1f, Damage);
        WriteByte(0x20, MagicBonusWeapon);
        WriteByte(0x21, MagicBonusArmor);

        var itemSlots = Enum.GetValues<EquipmentSlot>().Select(slot => Equipment[slot]).Concat(Inventory).ToArray();

        for (int i = 0; i < itemSlots.Length; i++)
        {
            var itemSlot = itemSlots[i];
            int offset = 0x132 + i * 40;
            WriteByte(0x22 + i, itemSlot.Item == null ? 0 : itemSlot.Count);

            if (itemSlot.Item == null || itemSlot.Count == 0)
            {
                Array.Clear(data, offset, 40);
            }
            else
            {
                var writer = new Amber.Serialization.DataWriter();
                itemSlot.Item.Write(writer);
                writer.ToArray().CopyTo(data, offset);
            }
        }

        WriteByte(0x3a, (int)PhysicalConditions);
        WriteByte(0x3b, (int)MentalConditions);
        WriteByte(0x42, defaultBattlePosition);
        WriteByte(0x43, AttacksPerRound);
        WriteWord(0x46, (int)PossibleClasses);

        // The age and an unused attribute are not part of the attributes.
        for (int i = 0; i < attributes.Length; i++)
        {
            var attribute = Attributes[attributes[i]];
            WriteWord(0x48 + i * 2, attribute.CurrentValue);
            WriteWord(0x5c + i * 2, attribute.MaxValue);
            WriteWord(0x9c + i * 2, attribute.BonusValue);
        }

        // The age is the 9th attribute.
        var age = ((IPerson)this).Age;
        WriteWord(0x58, age.CurrentValue);
        WriteWord(0x6c, age.MaxValue);
        WriteWord(0x70, attackPerRoundLevel);
        WriteWord(0x72, hitPointsPerLevel);
        WriteWord(0x74, spellPointsPerLevel);
        WriteWord(0x76, spellLearningPointsPerLevel);
        WriteWord(0x86, HitPoints.CurrentValue);
        WriteWord(0x88, HitPoints.MaxValue);
        WriteWord(0x8a, SpellPoints.CurrentValue);
        WriteWord(0x8c, SpellPoints.MaxValue);
        WriteWord(0x8e, SpellLearningPoints);
        WriteWord(0x90, Gold);
        WriteWord(0x92, Food);
        WriteWord(0x94, BonusDefense);
        WriteWord(0x96, BonusDamage);
        WriteWord(0x98, HitPoints.BonusValue);
        WriteWord(0x9a, SpellPoints.BonusValue);
        WriteWord(0xc4, saveBit);
        WriteDword(0xcc, ExperiencePoints);
        WriteDword(0xd0, LearnedWhiteSpells);
        WriteDword(0xd4, LearnedGraySpells);
        WriteDword(0xd8, LearnedBlackSpells);
        WriteDword(0xe8, LearnedSpecialSpells);
        WriteDword(0xec, TotalWeight);

        return data;
    }

    public IPartyMember Clone()
    {
        var clone = new PartyMember();

        // Clone base class state (Character and BattleCharacter)
        base.CloneInto(clone);

        // Copy PartyMember-specific fields
        clone.defaultBattlePosition = defaultBattlePosition;
        clone.attackPerRoundLevel = attackPerRoundLevel;
        clone.hitPointsPerLevel = hitPointsPerLevel;
        clone.spellPointsPerLevel = spellPointsPerLevel;
        clone.spellLearningPointsPerLevel = spellLearningPointsPerLevel;
        clone.conversationData = conversationData?.Clone();
        clone.saveBit = saveBit;
        clone.rawData = rawData;

        // Copy properties
        clone.PossibleClasses = PossibleClasses;
        clone.SpellLearningPoints = SpellLearningPoints;
        clone.ExperiencePoints = ExperiencePoints;
        clone.LearnedSpellSchools = LearnedSpellSchools;
        clone.LearnedWhiteSpells = LearnedWhiteSpells;
        clone.LearnedGraySpells = LearnedGraySpells;
        clone.LearnedBlackSpells = LearnedBlackSpells;
        clone.LearnedSpecialSpells = LearnedSpecialSpells;
        clone.TotalWeight = TotalWeight;

        return clone;
    }
}
