using Amber.IO.Common.Serialization;
using System.Collections.ObjectModel;

namespace Amberstar.GameData.Legacy;

internal class BattleCharacter : Character, IBattleCharacter
{
    private readonly Dictionary<Skill, CharacterValue> skills = [];
    private readonly Dictionary<Attribute, CharacterValue> attributes = [];

    public static void Load(BattleCharacter character, IDataReader reader)
    {
        Character.Load(character, reader);

        // Skills
        reader.Position = 0x06;
        var currentAttack = reader.ReadByte();
        var currentParry = reader.ReadByte();
        var currentSwim = reader.ReadByte();
        var currentListen = reader.ReadByte();
        var currentFindTraps = reader.ReadByte();
        var currentDisarmTraps = reader.ReadByte();
        var currentPickLocks = reader.ReadByte();
        var currentSearch = reader.ReadByte();
        var currentReadMagic = reader.ReadByte();
        var currentUseMagic = reader.ReadByte();
        var maxAttack = reader.ReadByte();
        var maxParry = reader.ReadByte();
        var maxSwim = reader.ReadByte();
        var maxListen = reader.ReadByte();
        var maxFindTraps = reader.ReadByte();
        var maxDisarmTraps = reader.ReadByte();
        var maxPickLocks = reader.ReadByte();
        var maxSearch = reader.ReadByte();
        var maxReadMagic = reader.ReadByte();
        var maxUseMagic = reader.ReadByte();

        // Used hand & fingers, def & dmg
        reader.Position = 0x1c;
        character.UsedHands = reader.ReadByte();
        character.UsedFingers = reader.ReadByte();
        character.Defense = reader.ReadByte();
        character.Damage = reader.ReadByte();
        character.MagicBonusWeapon = reader.ReadByte();
        character.MagicBonusArmor = reader.ReadByte();

        // Physical & mental conditions
        reader.Position = 0x3a;
        character.PhysicalConditions = (PhysicalCondition)reader.ReadByte();
        character.MentalConditions = (MentalCondition)reader.ReadByte();

        // Battle graphic index
        reader.Position = 0x43;
        character.AttacksPerRound = reader.ReadByte();

        // Attributes
        reader.Position = 0x48;
        var currentStrength = reader.ReadWord();
        var currentIntelligence = reader.ReadWord();
        var currentDexterity = reader.ReadWord();
        var currentSpeed = reader.ReadWord();
        var currentStamina = reader.ReadWord();
        var currentCharisma = reader.ReadWord();
        var currentLuck = reader.ReadWord();
        var currentAntiMagic = reader.ReadWord();
        var currentAge = reader.ReadWord();
        var currentUnusedAttribute = reader.ReadWord();
        var maxStrength = reader.ReadWord();
        var maxIntelligence = reader.ReadWord();
        var maxDexterity = reader.ReadWord();
        var maxSpeed = reader.ReadWord();
        var maxStamina = reader.ReadWord();
        var maxCharisma = reader.ReadWord();
        var maxLuck = reader.ReadWord();
        var maxAntiMagic = reader.ReadWord();
        var maxAge = reader.ReadWord();
        var maxUnusedAttribute = reader.ReadWord();

        reader.Position = 0x86;
        var currentHitPoints = reader.ReadWord();
        var maxHitPoints = reader.ReadWord();
        var currentSpellPoints = reader.ReadWord();
        var maxSpellPoints = reader.ReadWord();

        reader.Position = 0x94;
        character.BonusDefense = reader.ReadWord();
        character.BonusDamage = reader.ReadWord();
        var bonusHitPoints = reader.ReadWord();
        var bonusSpellPoints = reader.ReadWord();
        var bonusStrength = reader.ReadWord();
        var bonusIntelligence = reader.ReadWord();
        var bonusDexterity = reader.ReadWord();
        var bonusSpeed = reader.ReadWord();
        var bonusStamina = reader.ReadWord();
        var bonusCharisma = reader.ReadWord();
        var bonusLuck = reader.ReadWord();
        var bonusAntiMagic = reader.ReadWord();
        var bonusAge = reader.ReadWord();
        var bonusUnusedAttribute = reader.ReadWord();
        var bonusAttack = reader.ReadByte();
        var bonusParry = reader.ReadByte();
        var bonusSwim = reader.ReadByte();
        var bonusListen = reader.ReadByte();
        var bonusFindTraps = reader.ReadByte();
        var bonusDisarmTraps = reader.ReadByte();
        var bonusPickLocks = reader.ReadByte();
        var bonusSearch = reader.ReadByte();
        var bonusReadMagic = reader.ReadByte();
        var bonusUseMagic = reader.ReadByte();

        character.attributes[Attribute.Strength] = new(currentStrength, maxStrength, bonusStrength);
        character.attributes[Attribute.Intelligence] = new(currentIntelligence, maxIntelligence, bonusIntelligence);
        character.attributes[Attribute.Dexterity] = new(currentDexterity, maxDexterity, bonusDexterity);
        character.attributes[Attribute.Speed] = new(currentSpeed, maxSpeed, bonusSpeed);
        character.attributes[Attribute.Stamina] = new(currentStamina, maxStamina, bonusStamina);
        character.attributes[Attribute.Charisma] = new(currentCharisma, maxCharisma, bonusCharisma);
        character.attributes[Attribute.Luck] = new(currentLuck, maxLuck, bonusLuck);
        character.attributes[Attribute.AntiMagic] = new(currentAntiMagic, maxAntiMagic, bonusAntiMagic);

        character.skills[Skill.Attack] = new(currentAttack, maxAttack, bonusAttack);
        character.skills[Skill.Parry] = new(currentParry, maxParry, bonusParry);
        character.skills[Skill.Swim] = new(currentSwim, maxSwim, bonusSwim);
        character.skills[Skill.Listen] = new(currentListen, maxListen, bonusListen);
        character.skills[Skill.FindTraps] = new(currentFindTraps, maxFindTraps, bonusFindTraps);
        character.skills[Skill.DisarmTraps] = new(currentDisarmTraps, maxDisarmTraps, bonusDisarmTraps);
        character.skills[Skill.PickLocks] = new(currentPickLocks, maxPickLocks, bonusPickLocks);
        character.skills[Skill.Search] = new(currentSearch, maxSearch, bonusSearch);
        character.skills[Skill.ReadMagic] = new(currentReadMagic, maxReadMagic, bonusReadMagic);
        character.skills[Skill.UseMagic] = new(currentUseMagic, maxUseMagic, bonusUseMagic);

        character.HitPoints = new(currentHitPoints, maxHitPoints, bonusHitPoints);
        character.SpellPoints = new(currentSpellPoints, maxSpellPoints, bonusSpellPoints);
    }

    protected void CloneInto(BattleCharacter character)
    {
        // Clone base character fields
        base.CloneInto(character);

        // Simple scalar properties
        character.UsedHands = UsedHands;
        character.UsedFingers = UsedFingers;
        character.Defense = Defense;
        character.Damage = Damage;
        character.BonusDefense = BonusDefense;
        character.BonusDamage = BonusDamage;
        character.PhysicalConditions = PhysicalConditions;
        character.MentalConditions = MentalConditions;
        character.MagicBonusWeapon = MagicBonusWeapon;
        character.MagicBonusArmor = MagicBonusArmor;
        character.AttacksPerRound = AttacksPerRound;

        // Copy hit/spell points
        character.HitPoints = HitPoints.Copy();
        character.SpellPoints = SpellPoints.Copy();

        // Copy dictionaries
        character.skills.Clear();
        foreach (var kv in skills)
            character.skills[kv.Key] = kv.Value.Copy();

        character.attributes.Clear();
        foreach (var kv in attributes)
            character.attributes[kv.Key] = kv.Value.Copy();
    }

    public byte UsedHands { get; set; }
    public byte UsedFingers { get; set; }
    public byte Defense { get; set; }
    public byte Damage { get; set; }
    public word BonusDefense { get; set; }
    public word BonusDamage { get; set; }
    public PhysicalCondition PhysicalConditions { get; set; }
    public MentalCondition MentalConditions { get; set; }
    public byte MagicBonusWeapon { get; set; }
    public byte MagicBonusArmor { get; set; }
    public byte AttacksPerRound { get; set; }
    public CharacterValue HitPoints { get; private set; } = new(0, 0, 0);
    public CharacterValue SpellPoints { get; private set; } = new(0, 0, 0);
    public ReadOnlyDictionary<Skill, CharacterValue> Skills => skills.AsReadOnly();
    public ReadOnlyDictionary<Attribute, CharacterValue> Attributes => attributes.AsReadOnly();
}
