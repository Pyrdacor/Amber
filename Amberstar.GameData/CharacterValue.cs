namespace Amberstar.GameData;

public record CharacterValue(word CurrentValue, word MaxValue, word BonusValue)
{
    public word CurrentValue { get; set; } = CurrentValue;
    public word MaxValue { get; set; } = MaxValue;
    public word BonusValue { get; set; } = BonusValue;

    public word TotalCurrent => (word)(CurrentValue + BonusValue);
    public word TotalMax => (word)(MaxValue + BonusValue);

    public CharacterValue Copy() => new(CurrentValue, MaxValue, BonusValue);
}