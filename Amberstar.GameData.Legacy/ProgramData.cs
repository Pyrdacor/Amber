using Amber.Assets.Common;
using Amber.Common;
using Amber.Serialization;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy
{
	internal class ProgramData
	{
		public ProgramData(IDataReader dataReader, Func<EmbeddedDataOffset, IDataReader, bool> dataSeeker)
		{
			Graphic ReadGraphic(int width, int height)
			{
				return Graphic.FromBitPlanes(width, height, dataReader.ReadBytes(width * height / 2), 4);
			}

			#region Read Version
			if (!dataSeeker(EmbeddedDataOffset.Version, dataReader))
				throw new AmberException(ExceptionScope.Application, "Could not find the version string in the program file.");
			Version = ReadString(dataReader, 0x0D);
			#endregion
			#region Read Glyph Mappings
			if (!dataSeeker(EmbeddedDataOffset.GlyphMappings, dataReader))
				throw new AmberException(ExceptionScope.Application, "Could not find the glyph mappings in the program file.");
			GlyphMappings = dataReader.ReadBytes(224); // 256 - 32 (control chars)
			#endregion
			#region Read Text Fragments
			int textCount = dataReader.ReadWord();
			int unknownCount = dataReader.ReadByte();
			dataReader.Position += unknownCount - 1;
			TextFragments = new List<string>(textCount)
			{
				"" // Index 0 is just an empty string
			};

			for (int i = 1; i < textCount; i++)
			{
				int length = dataReader.ReadByte();

				TextFragments.Add(DataReader.Encoding.GetString(dataReader.ReadBytes(length - 1)));
			}

			if (dataReader.ReadByte() != 0)
				throw new AmberException(ExceptionScope.Application, "Invalid text fragment terminator.");

			#endregion
			#region Read all kind of names
			if (!dataSeeker(EmbeddedDataOffset.Names, dataReader))
				throw new AmberException(ExceptionScope.Application, "Could not find the class names in the program file.");

			ClassNames = [];
			SkillNames = [];
			CharInfoTexts = [];
			SpellSchoolNames = [];
			SpellNames = [];

			for (int i = 0; i < 11; i++)
				ClassNames.Add(i, new DataReader(dataReader.ReadBytes(2))); // one word per class name

			for (int i = 0; i < 10; i++)
				SkillNames.Add(i, new DataReader(dataReader.ReadBytes(2))); // one word per skill name

			for (int i = 0; i < 5; i++)
				CharInfoTexts.Add(i, new DataReader(dataReader.ReadBytes(2))); // one word per char info text

			dataReader.Position += 12; // TODO: unknown data

			for (int i = 0; i < 7; i++)
				RaceNames.Add(i, new DataReader(dataReader.ReadBytes(2))); // one word per race name

			dataReader.Position += 2; // either an additional empty race name or an empty string for condition "none"

			// Note: First the physical conditions, then the mental conditions.
			// Note: There are only 5 mental conditions and I guess overloaded is not shown in the UI as a text.
			//       So the last 3 mental conditions are again "dead", "ashes" and "dust" (most likely copied over
			//       from the physical conditions). And I guess they are not used/shown at all.
			for (int i = 1; i <= 16; i++)
				ConditionNames.Add(i, new DataReader(dataReader.ReadBytes(2))); // one word per condition name

			for (int i = 0; i < 19; i++)
				ItemTypeNames.Add(i, new DataReader(dataReader.ReadBytes(2))); // one word per item type name

			// TODO: hopefully we have something better later
			if (!dataSeeker(EmbeddedDataOffset.SpellSchoolNames, dataReader))
				throw new AmberException(ExceptionScope.Application, "Could not find the spell names in the program file.");

			for (int i = 1; i <= 7; i++)
				SpellSchoolNames.Add(i, new DataReader(dataReader.ReadBytes(2))); // one word per spell school name

			for (int i = 1; i <= 7 * 30; i++)
				SpellNames.Add(i, new DataReader(dataReader.ReadBytes(2))); // one word per spell name

			#endregion
			#region Read embedded graphics
			if (!dataSeeker(EmbeddedDataOffset.Graphics, dataReader))
				throw new AmberException(ExceptionScope.Application, "Could not find the graphics in the program file.");

			#region Load layout bottom corner info
			int cornerCount = 8 * 16;
			LayoutBottomCorners = new List<word>(cornerCount);
			for (int i = 0; i < cornerCount; i++)
				LayoutBottomCorners.Add(dataReader.ReadWord());
			int maskCount = 8 * 4;
			LayoutBottomCornerMasks = new List<word>(maskCount);
			for (int i = 0; i < maskCount; i++)
				LayoutBottomCornerMasks.Add(dataReader.ReadWord());
			#endregion
			#region Read portrait area
			// The status block is the area right of the player portrait.
			// It has a size of 16x36 pixels and starts at y=1 (so end at y=37 where the layout starts).
			// The upper part is the status icon box and the lower part is the LP and SP bar area.
			// The status icon is displayed at y=1 (on screen, so relative y=0) and has a height of 16 pixels.
			// The bars are displayed at y=18 (on screen, so relative y=17).
			// The portraits are displayed 32 pixels to the left of the status block and has a size of 32x34.
			// The portraits also are located at y=1, so they end at y=35.
			PortraitArea = new Graphic(320, 37, GraphicFormat.PaletteIndices);
			var statusBlockMid = ReadGraphic(16, 36);
			var statusBlockLeft = ReadGraphic(16, 36);
			var statusBlockRight = ReadGraphic(16, 36);
			var statusBlockTop = ReadGraphic(32, 1);
			var statusBlockBottom = ReadGraphic(32, 1);

			PortraitArea.AddOverlay(0, 0, statusBlockLeft);

			for (int i = 1; i <= 6; i++)
				PortraitArea.AddOverlay(i * 48, 0, statusBlockMid);

			PortraitArea.AddOverlay(6 * 48 + 16, 0, statusBlockRight);

			// Top and bottom edges
			for (int i = 0; i < 6; i++)
			{
				PortraitArea.AddOverlay(16 + i * 48, 0, statusBlockTop);
				PortraitArea.AddOverlay(16 + i * 48, 35, statusBlockBottom);
			}

			#endregion
			#region Read layout definitions
			// There are 11 layouts
			for (int i = 1; i <= 11; i++)
				// A layout definition consists of 220 bytes (20 blocks per row, 11 rows)
				// - First block line has 4 pixels offset (first 4 pixels are not used, so 16x12 pixel blocks)
				// - Last block line has 9 pixels less height (last 9 pixels are not used, so 16x7 pixel blocks)
				// - The 9 block lines in-between are rendered as full 16x16 blocks
				// So a layout is 320x163 pixels in size and is displayed at y=37 (so it ends at y=200).
				Layouts.Add(i, new DataReader(dataReader.ReadBytes(220)));
			#endregion
			#region Read UI graphics
			void AddUIGraphic(UIGraphic graphic)
			{
				var size = graphic.GetSize();
				int frameCount = graphic.GetFrameCount();
				UIGraphics.Add((int)graphic, new DataReader(dataReader.ReadBytes(frameCount * (int)size.Width * (int)size.Height / 2)));
			}
			for (int i = 0; i <= (int)UIGraphic.Invisibility; i++)
				AddUIGraphic((UIGraphic)i);
			#endregion
			#region Read layout block graphics
			if (dataReader.PeekWord() != 0xaaaa)
				throw new AmberException(ExceptionScope.Application, "Could not find the layouts in the program file.");

			// Add layout blocks
			// 16.896 bytes (each should be a 4bit 16x16 image, so 128 bytes per block).
			// This means there should be 132 blocks.
			LayoutBlocks = new Dictionary<int, Graphic>(132);

			for (int i = 0; i < 132; i++)
				LayoutBlocks.Add(i, ReadGraphic(16, 16));
			#endregion
			#endregion
			#region Read buttons
			void AddButton(int button)
			{
				Buttons.Add(button, new DataReader(dataReader.ReadBytes(32 * 16 / 2)));
			}
			for (int i = 0; i <= (int)ButtonType.LastButton; i++)
				AddButton(i);
			#endregion
			#region Read status icons
			void AddStatusIcon(int statusIcon)
			{
				StatusIcons.Add(statusIcon, new DataReader(dataReader.ReadBytes(16 * 16 / 2)));
			}
			for (int i = 0; i <= (int)StatusIcon.LastStatusIcon; i++)
				AddStatusIcon(i);
			#endregion
			#region Read more UI gfx
			for (int i = (int)UIGraphic.DamageSplash; i <= (int)UIGraphic.EmptyCharSlot; i++)
				AddUIGraphic((UIGraphic)i);
			#endregion
			#region Read item graphics
			void AddItemGraphic(int index)
			{
				ItemGraphics.Add(index, new DataReader(dataReader.ReadBytes(16 * 16 / 2)));
			}
			for (int i = 0; i <= (int)ItemGraphic.LastItemGraphic; i++)
				AddItemGraphic(i);
			#endregion
			#region Read even more UI gfx
			for (int i = (int)UIGraphic.HPBar; i <= (int)UIGraphic.LastUIGraphic; i++)
				AddUIGraphic((UIGraphic)i);
			#endregion
			#region Read sky gradients
			// TODO: read gfx between the above and the sky gradients
			dataReader.Position = (int)dataReader.FindByteSequence([0x00, 0x56, 0x00, 0x56], dataReader.Position);
			for (int i = 0; i < 3; i++)
				SkyGradients.Add(i, new DataReader(dataReader.ReadBytes(84 * 2)));
			#endregion
			#region Read font
			if (!dataSeeker(EmbeddedDataOffset.TextConversionTab, dataReader))
				throw new AmberException(ExceptionScope.Application, "Could not find the font information in the program file.");
			Fonts.Add(0, new DataReader(dataReader.ReadBytes(0x3fe)));
			#endregion
			#region Windows
			if (!dataSeeker(EmbeddedDataOffset.Windows, dataReader))
				throw new AmberException(ExceptionScope.Application, "Could not find the window graphics in the program file.");
			Windows.Add(0, new DataReader(dataReader.ReadBytes(0x800)));
			Windows.Add(1, new DataReader(dataReader.ReadBytes(0x800)));
			#endregion
			#region Cursors
			if (!dataSeeker(EmbeddedDataOffset.Cursors, dataReader))
				throw new AmberException(ExceptionScope.Application, "Could not find the window graphics in the program file.");
			void AddCursor(int cursor)
			{
				Cursors.Add(cursor, new DataReader(dataReader.ReadBytes(68)));
			}
			for (int i = 0; i <= (int)CursorType.LastCursor; i++)
				AddCursor(i);
			#endregion

			// TODO: places
		}

		private static string ReadString(IDataReader dataReader, byte endByte = 0)
		{
			var bytes = ReadUntilByte(dataReader, endByte);
			return DataReader.Encoding.GetString(bytes);
		}

		private static byte[] ReadUntilByte(IDataReader dataReader, byte endByte)
		{
			var buffer = new List<byte>();

			while (dataReader.Position < dataReader.Size)
			{
				var b = dataReader.ReadByte();

				if (b == endByte)
					break;

				buffer.Add(b);
			}

			return buffer.ToArray();
		}

		private static bool FindAndGotoText(IDataReader dataReader, int offset, string text)
		{
			return FindAndGotoByteSequence(dataReader, offset, DataReader.Encoding.GetBytes(text));
		}

		private static bool FindAndGotoByteSequence(IDataReader dataReader, int offset, params byte[] sequence)
		{
			if (sequence == null || sequence.Length == 0)
				return false;

			dataReader.Position = offset;

			while (dataReader.Position <= dataReader.Size - sequence.Length)
			{
				while (dataReader.Position < dataReader.Size)
				{
					if (dataReader.ReadByte() != sequence[0])
						continue;

					if (sequence.Length == 1)
					{
						dataReader.Position--;
						return true;
					}

					int secondBytePosition = dataReader.Position;

					for (int i = 1; i < sequence.Length; i++)
					{
						if (dataReader.ReadByte() != sequence[i])
						{
							dataReader.Position = secondBytePosition;
							break;
						}

						if (i == sequence.Length - 1)
						{
							dataReader.Position = secondBytePosition - 1;
							return true;
						}
					}
				}
			}

			return false;
		}

		public Dictionary<int, IDataReader> Layouts { get; } = [];
		public Dictionary<int, IDataReader> PlacesData { get; } = [];
		public Dictionary<int, IDataReader> PlaceNames { get; } = [];
		public Dictionary<int, IDataReader> SpellSchoolNames { get; } = [];
		public Dictionary<int, IDataReader> SpellNames { get; } = [];
		public Dictionary<int, IDataReader> ClassNames { get; } = [];
		public Dictionary<int, IDataReader> SkillNames { get; } = [];
		public Dictionary<int, IDataReader> CharInfoTexts { get; } = [];
		public Dictionary<int, IDataReader> RaceNames { get; } = [];
		public Dictionary<int, IDataReader> ConditionNames { get; } = [];
		public Dictionary<int, IDataReader> ItemTypeNames { get; } = [];
		public Dictionary<int, Graphic> LayoutBlocks { get; } = [];
		public List<word> LayoutBottomCorners { get; } = [];
		public List<word> LayoutBottomCornerMasks { get; } = [];
		public Graphic PortraitArea { get; }
		public Dictionary<int, IDataReader> UIGraphics { get; } = [];
		public Dictionary<int, IDataReader> Buttons { get; } = [];
		public Dictionary<int, IDataReader> StatusIcons { get; } = [];
		public Dictionary<int, IDataReader> ItemGraphics { get; } = [];
		public Dictionary<int, IDataReader> SkyGradients { get; } = [];
		public Dictionary<int, IDataReader> Fonts { get; } = [];
		public Dictionary<int, IDataReader> Windows { get; } = [];
		public Dictionary<int, IDataReader> Cursors { get; } = [];
		public List<string> TextFragments { get; } = [];
		public byte[] GlyphMappings { get; } = [];
		public string Version { get; } = string.Empty;
	}
}
