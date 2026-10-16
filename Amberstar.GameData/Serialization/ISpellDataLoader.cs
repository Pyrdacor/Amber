namespace Amberstar.GameData.Serialization;

public interface ISpellDataLoader
{
	/// <summary>
	/// Loads the data of a spell by its school and its 1-based number.
	/// </summary>
	ISpellData LoadSpellData(SpellSchool school, int spellNumber);
}
