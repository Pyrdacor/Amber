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

    public static PartyMember Load(IAsset asset, ITextLoader textLoader)
    {
        var partyMember = new PartyMember();
        var reader = asset.GetReader();

        BattleCharacter.Load(partyMember, reader);
        partyMember.conversationData = Legacy.ConversationData.Load(reader, textLoader);

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

        reader.Position = 0xcc;
        partyMember.ExperiencePoints = reader.ReadDword();
        partyMember.LearnedWhiteSpells = reader.ReadDword();
        partyMember.LearnedGraySpells = reader.ReadDword();
        partyMember.LearnedBlackSpells = reader.ReadDword();
        reader.Position += 3 * 4; // Skip unused spells
        partyMember.LearnedSpecialSpells = reader.ReadDword();

        // We ensure that some equipment related values are correctly set.


        return partyMember;
    }

    public ClassFlags PossibleClasses { get; set; }
    public byte DefaultBattlePosition { get => defaultBattlePosition; init => defaultBattlePosition = value; }
    public word AttackPerRoundLevel { get => attackPerRoundLevel; init => attackPerRoundLevel = value; }
    public word HitPointsPerLevel { get => hitPointsPerLevel; init => hitPointsPerLevel = value; }
    public word SpellPointsPerLevel { get => spellPointsPerLevel; init => spellPointsPerLevel = value; }
    public word SpellLearningPointsPerLevel { get => spellLearningPointsPerLevel; init => spellLearningPointsPerLevel = value; }
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
