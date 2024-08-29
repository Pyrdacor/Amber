using Amber.Assets.Common;
using Amber.Common;

namespace Amberstar.GameData.Serialization;

public enum UIGraphic
{
	Skull,
	FeedbackIcon,
	ChequeredIcon,
	Night,
	Dawn,
	Day,
	Dusk,
	CompassNorth,
	CompassEast,
	CompassSouth,
	CompassWest,
	Amberstar,
	Windchain,
	Light,
	ArmorProtection,
	WeaponPower,
	AntiMagic,
	Clairvoyance,
	Invisibility,
	EmptyItemSlot,
	DamageSplash,
	HealingCross,
	Ouch,
	MagicAnimation, // 3 frames with 32x32 each (displayed over the portraits)
	EmptyCharSlot,
	HPBar,
	SPBar,
	Sword,
	Shield,
	LastUIGraphic // TODO: point to last
}

public enum ButtonType
{
	EmptyArea,
	ArrowUp,
	ArrowDown,
	ArrowRight,
	ArrowLeft,
	ButHorse,
	BuyRaft,
	Sleep, // Zzzz
	Stats, // person with shard on the right
	Equipment, // 3x3 grid
	Ear,
	Eye,
	UseMagic,
	FindTrap,
	DisarmTrap,
	PickLock,
	RotateLeft,
	Mouth,
	GiveGold,
	Camp,
	GiveFood,
	UseTransport,
	BuyShip,
	Save,
	Load,
	Music,
	NoMusic,
	ThumbsUp,
	ThumbsDown,
	Inventory, // small bag
	Exit, // as text
	GatherGold, // gold pile with two arrows pointing to it
	UseItem,
	GiveItem,
	DropItem,
	ExamineItem,
	Sword,
	Shield,
	PartyPositions,
	MoveForward,
	MoveBackward,
	StrafeRight,
	StrafeLeft,
	TurnRight,
	TurnLeft,
	ArrowUpRight,
	ArrowUpLeft,
	ArrowDownRight,
	ArrowDownLeft,
	RotateRight,
	DistributeGold,
	DistributeFood,
	EnterDoor, // person and arrow pointing to door
	Advance,
	BuyItem,
	SellItem,
	Empty,
	Map,
	ReadScroll,
	Ok,
	Disk, // options
	GiveItemToNPC,
	GiveFoodToNPC,
	GiveGoldToNPC,
	BuyFood,
	Quit,
	Flee,
	AskToJoin,
	Play,
	Forward,
	LastButton = Forward
}

public enum StatusIcon
{
	Dead,
	Attack,
	Parry,
	UseMagic,
	Flee,
	Move,
	UseItem,
	HandStop,
	HandOpen,
	Stunned,
	Poisoned,
	Petrified,
	Diseased,
	Aging,
	Irritated,
	Mad,
	Sleeping,
	Panicked,
	Blind,
	Overloaded,
	LastStatusIcon = Overloaded
}

public enum Image80x80
{
	Camp = 1,
	Graveyard,
	Guild,
	Merchant,
	PotionMerchant,
	Monster,
	HorseStable,
	Healer,
	RatKing,
	Sage,
	HolyPerson, // ?
	LockedChest,
	LockedDoor,
	ShipDealer,
	Inn,
	Marmion,
	Riddlemouth,
	DeadPeople, // ?
	MagicPrison, // ?
	Dragon,
	OpenChest,
	CrystalOrb,
	Library,
	MonsterOrb, // ?
	Castle, // ?
	Amberstar,
	LastImage = Amberstar
}

public enum ItemGraphic
{
	RedCross, // second hand slot
	Chain,
	PearlChain,
	Brooch,
	Gem,
	LeatherArmor,
	Robe,
	Shoes,
	Boots,
	Belt,
	HornHelm,
	SunHelm,
	IronHelm,
	Rope,
	Unknown, // TODO
	RatHead,
	CrystalOrb,
	Arrow,
	Bolt,
	ShortBow,
	LongBow,
	CrossBow,
	Axe,
	BattleAxe,
	Knife,
	Flail,
	Mace,
	Hammer,
	Stick,
	Sabre,
	Sling,
	Trident,
	Dagger,
	ShortSword,
	LongSword,
	BroadSword,
	ChainMail,
	PlateMail,
	BandedArmor,
	KnightArmor,
	Buckler,
	RoundShield,
	SmallShield,
	LargeShield,
	Key,
	Lockpick,
	Cat,
	MorningStar,
	ThrowingAxe,
	Whip,
	ThrowingSickle,
	Unknown2, // TODO
	Wand,
	Club,
	Unknown3, // TODO
	Bone,
	Broom,
	PieceOfAmberstar,
	Unknown4, // TODO: palette?
	Harp,
	HolyHorn,
	Clover,
	YellowGem,
	Cloth,
	BlueMushroom,
	Egg,
	Amberstar,
	MagicWand,
	MagicDisc,
	Crowbar,
	EmptyPotion,
	UnknownPotion1, // TODO...
	UnknownPotion2,
	UnknownPotion3,
	UnknownPotion4,
	UnknownPotion5,
	UnknownPotion6,
	UnknownPotion7,
	UnknownPotion8,
	UnknownPotion9,
	UnknownPotion10,
	UnknownPotion11,
	IronRing1,
	GoldenRing,
	Unknown5, // TODO
	SaphireRing,
	Collar,
	Flower,
	SmallRing,
	IronRing2,
	Trophy,
	Unknown6, // TODO
	UnknownGem1, // TODO
	RainbowGem,
	UnknownGem2, // TODO
	UnknownGem3, // TODO
	WishingCoins,
	Unknown7, // TODO
	Flute, // ?
	NoteWithPen,
	Hat,
	Crystal,
	Unknown8, // TODO
	Unknown9, // TODO
	Book,
	Unknown10, // TODO
	Letter,
	SilverCutlery,
	RuneAlphabet,
	Ration,
	TextScroll,
	Map,
	Unknown11, // TODO
	Unknown12, // TODO
	Clock,
	MapLocator,
	Mushroom2,
	GoldenLetter,
	Letter2,
	Letter3,
	Bottle,
	MagicPicture,
	WoodenStaff,
	AnotherPotion, // TODO
	Pickaxe,
	Shovel,
	Unknown13, // TODO
	Unknown14, // TODO
	LastItemGraphic = Unknown14
}

public interface IUIGraphicLoader
{
	IGraphic LoadGraphic(UIGraphic graphic);
	IGraphic LoadButtonGraphic(ButtonType button);
	IGraphic LoadStatusIcon(StatusIcon icon);
}

public static class UIGraphicExtensions
{
	// frame count and size
	private static Tuple<int, Size> Info(uint width, uint height, int frameCount = 1) => Tuple.Create(frameCount, new Size(width, height));
	private static readonly Dictionary<UIGraphic, Tuple<int, Size>> uiGraphicInfos = new()
	{
		{ UIGraphic.Skull, Info(32, 34) },
		{ UIGraphic.FeedbackIcon, Info(32, 16) },
		{ UIGraphic.ChequeredIcon, Info(32, 16) },
		{ UIGraphic.Night, Info(32, 32) },
		{ UIGraphic.Dawn, Info(32, 32) },
		{ UIGraphic.Day, Info(32, 32) },
		{ UIGraphic.Dusk, Info(32, 32) },
		{ UIGraphic.CompassNorth, Info(32, 32) },
		{ UIGraphic.CompassEast, Info(32, 32) },
		{ UIGraphic.CompassSouth, Info(32, 32) },
		{ UIGraphic.CompassWest, Info(32, 32) },
		{ UIGraphic.Amberstar, Info(32, 32) },
		{ UIGraphic.Windchain, Info(32, 16) },
		{ UIGraphic.Light, Info(16, 16) },
		{ UIGraphic.ArmorProtection, Info(16, 16) },
		{ UIGraphic.WeaponPower, Info(16, 16) },
		{ UIGraphic.AntiMagic, Info(16, 16) },
		{ UIGraphic.Clairvoyance, Info(16, 16) },
		{ UIGraphic.Invisibility, Info(16, 16) },
		{ UIGraphic.EmptyItemSlot, Info(16, 16) },
		{ UIGraphic.DamageSplash, Info(32, 32) },
		{ UIGraphic.HealingCross, Info(32, 32) },
		{ UIGraphic.Ouch, Info(32, 32) },
		{ UIGraphic.MagicAnimation, Info(32, 32, 3) },
		{ UIGraphic.EmptyCharSlot, Info(32, 34) },
		{ UIGraphic.HPBar, Info(16, 17) },
		{ UIGraphic.SPBar, Info(16, 17) },
		{ UIGraphic.Sword, Info(16, 10) },
		{ UIGraphic.Shield, Info(16, 10) },
		{ UIGraphic.LastUIGraphic, Info(16, 1) } // TODO
	};

	public static Size GetSize(this UIGraphic graphic) => uiGraphicInfos[graphic].Item2;

	public static int GetFrameCount(this UIGraphic graphic) => uiGraphicInfos[graphic].Item1;
}
