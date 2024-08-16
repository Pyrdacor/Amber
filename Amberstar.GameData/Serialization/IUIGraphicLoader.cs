using Amber.Assets.Common;
using Amber.Common;

namespace Amberstar.GameData.Serialization
{
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
		//Sword,
		//Shield,
		LastUIGraphic // TODO: point to last
	}

	public enum Button
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

	public interface IUIGraphicLoader
	{
		IGraphic LoadGraphic(UIGraphic graphic);
		IGraphic LoadButtonGraphic(Button button);
		IGraphic LoadStatusIcon(StatusIcon icon);
	}

	public static class UIGraphicExtensions
	{
		private static readonly Dictionary<UIGraphic, Size> uiGraphicDimensions = new()
		{
			{ UIGraphic.Skull, new Size(32, 34) },
			{ UIGraphic.FeedbackIcon, new Size(32, 16) },
			{ UIGraphic.ChequeredIcon, new Size(32, 16) },
			{ UIGraphic.Night, new Size(32, 32) },
			{ UIGraphic.Dawn, new Size(32, 32) },
			{ UIGraphic.Day, new Size(32, 32) },
			{ UIGraphic.Dusk, new Size(32, 32) },
			{ UIGraphic.CompassNorth, new Size(32, 32) },
			{ UIGraphic.CompassEast, new Size(32, 32) },
			{ UIGraphic.CompassSouth, new Size(32, 32) },
			{ UIGraphic.CompassWest, new Size(32, 32) },
			{ UIGraphic.Amberstar, new Size(32, 32) },
			{ UIGraphic.Windchain, new Size(32, 16) },
			{ UIGraphic.Light, new Size(16, 16) },
			{ UIGraphic.ArmorProtection, new Size(16, 16) },
			{ UIGraphic.WeaponPower, new Size(16, 16) },
			{ UIGraphic.AntiMagic, new Size(16, 16) },
			{ UIGraphic.Clairvoyance, new Size(16, 16) },
			{ UIGraphic.Invisibility, new Size(16, 16) },
			/*{ UIGraphic.Sword, new Size(16, 16) },
			{ UIGraphic.Shield, new Size(16, 16) },*/
			{ UIGraphic.LastUIGraphic, new Size(32, 32 * 10 * 10) } // TODO
		};

		public static Size GetSize(this UIGraphic graphic) => uiGraphicDimensions[graphic];
	}
}
