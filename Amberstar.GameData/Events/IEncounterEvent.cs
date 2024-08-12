namespace Amberstar.GameData.Events;

/// <summary>
/// Starts a battle against some monster group.
/// </summary>
public interface IEncounterEvent : IEvent
{
	/// <summary>
	/// Byte 1
	/// 
	/// 0 to 100%
	/// </summary>
	byte Chance { get; }

	/// <summary>
	/// Byte 2
	/// 
	/// 0 means "no quest".
	/// </summary>
	byte Quest { get; }

	/// <summary>
	/// Byte 3
	/// 
	/// Text is used if there is a quest.
	/// </summary>
	byte QuestTextIndex { get; }

	/// <summary>
	/// Byte 4
	/// 
	/// Text is used if there is no quest.
	/// </summary>
	byte NoQuestTextIndex { get; }

	/// <summary>
	/// Word 6
	/// </summary>
	word MonsterGroupIndex { get; }
}
