namespace Amberstar.GameData;

[Flags]
public enum SpecialItems : word
{
	None = 0,
	Compass = 1 << 0,
	Unknown = 1 << 1,
	MagicalPicture = 1 << 2,
	WindChain = 1 << 3,
	MapLocator = 1 << 4,
	Clock = 1 << 5,
}
