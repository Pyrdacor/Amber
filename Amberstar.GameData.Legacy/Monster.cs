using Amber.Assets.Common;

namespace Amberstar.GameData.Legacy;

internal class Monster : BattleCharacter, IMonster
{
    private byte battleGraphicIndex;
    private byte spellCastChance;
    private byte magicHitBonus;
    private byte morale;
    private word defeatExperience;
    private MonsterFlags monsterFlags;
    private MonsterElementalFlags elementalFlags;
    private readonly List<MonsterSpell> spells = [];

    public static Monster Load(IAsset asset)
    {
        var monster = new Monster();
        var reader = asset.GetReader();

        BattleCharacter.Load(monster, reader);

        reader.Position = 0x3e;
        monster.battleGraphicIndex = reader.ReadByte();
        monster.spellCastChance = reader.ReadByte();
        monster.magicHitBonus = reader.ReadByte();
        monster.morale = reader.ReadByte();

        reader.Position = 0x44;
        monster.monsterFlags = (MonsterFlags)reader.ReadByte();
        monster.elementalFlags = (MonsterElementalFlags)reader.ReadByte();

        reader.Position = 0x44;
        monster.defeatExperience = reader.ReadWord();

        reader.Position = 0x100;
        var monsterSpellSchools = reader.ReadBytes(25);
        var monsterSpellIndices = reader.ReadBytes(25);

        for (int i = 0; i < 25; i++)
        {
            if (monsterSpellSchools[i] != 0)
            {
                monster.spells.Add(new((SpellSchool)monsterSpellSchools[i], monsterSpellIndices[i]));
            }
        }

        return monster;
    }

    public byte BattleGraphicIndex { get => battleGraphicIndex; init => battleGraphicIndex = value; }
    public byte Morale { get => morale; init => morale = value; }
    public byte SpellCastChance { get => spellCastChance; init => spellCastChance = value; }
    public byte MagicHitBonus { get => magicHitBonus; init => magicHitBonus = value; }
    public word DefeatExperience { get => defeatExperience; init => defeatExperience = value; }
    public MonsterFlags MonsterFlags { get => monsterFlags; init => monsterFlags = value; }
    public MonsterElementalFlags ElementalFlags { get => elementalFlags; init => elementalFlags = value; }
    public MonsterSpell[] Spells { get => [.. spells]; init => spells = [.. value]; }

    public IMonster Clone()
    {
        var clone = new Monster();

        // Clone base class state (Character and BattleCharacter)
        base.CloneInto(clone);

        clone.battleGraphicIndex = battleGraphicIndex;
        clone.spellCastChance = spellCastChance;
        clone.magicHitBonus = magicHitBonus;
        clone.morale = morale;
        clone.defeatExperience = defeatExperience;
        clone.monsterFlags = monsterFlags;
        clone.elementalFlags = elementalFlags;
        clone.spells.AddRange(spells.Select(spell => spell with { }));

        return clone;
    }
}
