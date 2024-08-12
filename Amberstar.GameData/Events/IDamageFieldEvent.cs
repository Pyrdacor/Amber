namespace Amberstar.GameData.Events;

public enum TargetGender
{
    Male,
    Female,
    Both
}

public interface IDamageFieldEvent : IEvent
{
	/// <summary>
	/// Byte 1
	/// 
	/// A random value between 1 and this value
	/// is calculated for each target.
	/// </summary>
	byte Damage { get; }

	/// <summary>
	/// Byte 2
	/// </summary>
	TargetGender TargetGender { get; }

	/// <summary>
	/// Byte 4
	/// 
	/// If non-zero, a text is shown before the damaging.
	/// </summary>
	byte TextIndex { get; }
}
