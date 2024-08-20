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
		EmptyItemSlot,
		DamageSplash,
		HealingCross,
		Ouch,
		MagicAnimation, // 3 frames with 32x32 each (displayed over the portraits)
		EmptyCharSlot,
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

	public enum Image80x80
	{
		Unknown1 = 1,
		Unknown2,
		Unknown3,
		Unknown4,
		Unknown5,
		Unknown6,
		Unknown7,
		Unknown8,
		Unknown9,
		Unknown10,
		Unknown11,
		Unknown12,
		Unknown13,
		Unknown14,
		Unknown15,
		Unknown16,
		Unknown17,
		Unknown18,
		Unknown19,
		Unknown20,
		Unknown21,
		Unknown22,
		Unknown23,
		Unknown24,
		Unknown25,
		Unknown26,
		LastImage = Unknown25
	}

	public enum ItemGraphic
	{
		RedCross, // second hand slot
		Chain,
		PearlChain,
		// TODO ...
		LastItemGraphic = PearlChain
	}

	public interface IUIGraphicLoader
	{
		IGraphic LoadGraphic(UIGraphic graphic);
		IGraphic LoadButtonGraphic(Button button);
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
			/*{ UIGraphic.Sword, new Size(16, 16) },
			{ UIGraphic.Shield, new Size(16, 16) },*/
			{ UIGraphic.LastUIGraphic, Info(16, 128) } // TODO
		};

		public static Size GetSize(this UIGraphic graphic) => uiGraphicInfos[graphic].Item2;

		public static int GetFrameCount(this UIGraphic graphic) => uiGraphicInfos[graphic].Item1;
	}
}
