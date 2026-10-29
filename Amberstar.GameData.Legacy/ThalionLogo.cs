using Amber.Assets.Common;

namespace Amberstar.GameData.Legacy;

/// <summary>
/// The logo of Thalion (THALION.68K). The program is the same in all versions. Its routines are ported 1:1 and work
/// on a copy of the memory of the original (the program is at address 0, the screens and buffers follow it), so the
/// addresses are the ones of the disassembled program.
///
/// The main program runs between the vertical blanks and waits for them (it is an iterator here). In each vertical
/// blank the routines of the current phases are called ($6a6). The shown frame is the screen which was set in the
/// previous vertical blank with the palette and the raster colors of the current one.
/// </summary>
internal sealed class ThalionLogo : IThalionLogo
{
	const int MemorySize = 0x40000;
	// the screens and buffers (the first screen is at $d46b & $ff00)
	const int Screen1 = 0xd400;
	const int Screen2 = Screen1 + ScreenSize;
	const int Screen3 = Screen2 + ScreenSize;
	const int ScreenSize = 32000;
	const int LineSize = 160;
	const int BlockCount = 4000;
	// variables of the program
	const int ScreenPointer1 = 0x4276;
	const int ScreenPointer2 = 0x426e;
	const int ScreenPointer3 = 0x4272;
	const int ShownScreen = 0x1ca; // L1ca: the screen which is drawn and shown next
	const int OtherScreen = 0x1ce;
	const int RestoreBuffer = 0x1d2; // L1d2: what has to be restored in the drawn screen
	const int OtherRestoreBuffer = 0x1d6;
	const int ShiftTable = 0x1da;
	const int ShiftSource = 0x1de;
	const int ShiftMask = 0x1e2;
	const int ShiftLines = 0x1e6;
	const int RandomSeed = 0x818;
	const int Drops = 0xef2; // 20 drops with 18 bytes
	const int DropSize = 18;
	const int DropCount = 20;
	const int NextDrop = 0x105a;
	const int SpawnTries = 0x105e;
	const int ActiveDrops = 0x1060;
	const int PhaseDone = 0x1062;
	const int Leaving = 0x1063;
	const int NoMoreDrops = 0x1064;
	const int FadeCounter = 0x11c6;
	const int FadeChanged = 0x11c7;
	const int Palette = 0x1890;
	const int TargetPalette = 0x18b0;
	const int RasterTable = 0xc602; // colors 2 and 3 of 51 lines (normal and dimmed)
	const int RasterTableEnd = 0xc6ce;
	const int RasterLines = 51;
	const int BouncePointer = 0xad6;
	const int BounceLoop = 0xada;
	const int TextY = 0xade;
	const int RasterStart = 0xae0;
	const int RasterEnd = 0xae2;
	const int PiecePositions = 0xa9c; // 7 pairs of the current and the target value
	// the routines of the phases (L69e and L6a2)
	enum Routine { None, FadeIn, Rain, Pieces, FadeOut, Text, Leave }
	// the 2 secret words (scan codes until $ff)
	const int SecretSong = 0x616;
	const int SecretBounce = 0x61e;
	const int SecretBounceLoop = 0x4212;
	const int SpaceScanCode = 0x39;
	// the music module follows the player ($9aca)
	const int Music = 0xa4f2;

	readonly byte[] program;
	readonly byte[] memory = new byte[MemorySize];
	readonly ushort[] palette = new ushort[16];
	readonly byte[] frame = new byte[IThalionLogo.Width * IThalionLogo.Height * 4];
	IEnumerator<bool>? main;
	Routine routine1 = Routine.None;
	Routine routine2 = Routine.None;
	int screenBase = Screen1;
	bool raster = false;
	// the raster of the shown frame: the first line, the first table entry and the number of lines
	int rasterLine = 0;
	int rasterEntry = 0;
	int rasterCount = 0;
	int key = 0;
	// a released key is seen until the next vertical blank (like the key in the interrupt of the keyboard)
	bool keyReleased = false;
	bool skip = false;

	public ThalionLogo(byte[] program)
	{
		this.program = program;
		Frame = Graphic.FromRGBA(IThalionLogo.Width, IThalionLogo.Height, frame);
		Reset();
	}

	public bool Finished { get; private set; }

	public IGraphic Frame { get; private set; }

	public int Song { get; private set; }

	public ISong GetSong(int song) => new HippelCosoYmSong(program[Music..], song);

	public void Reset()
	{
		Array.Clear(memory);
		Array.Copy(program, memory, Math.Min(program.Length, MemorySize));
		Array.Clear(palette);
		Array.Clear(frame);
		routine1 = routine2 = Routine.None;
		screenBase = Screen1;
		raster = false;
		key = 0;
		keyReleased = false;
		skip = false;
		Song = 0;
		Finished = false;
		main = MainProgram().GetEnumerator();
	}

	public void KeyPressed(int scanCode)
	{
		key = scanCode & 0x7f;
		keyReleased = false;
	}

	public void KeyReleased() => keyReleased = true;

	public void Skip() => skip = true;

	#region Memory

	int Byte(int address) => memory[address];

	void SetByte(int address, int value) => memory[address] = (byte)value;

	int Word(int address) => (memory[address] << 8) | memory[address + 1];

	int SignedWord(int address) => (short)Word(address);

	void SetWord(int address, int value)
	{
		memory[address] = (byte)(value >> 8);
		memory[address + 1] = (byte)value;
	}

	int Long(int address) => (Word(address) << 16) | Word(address + 2);

	void SetLong(int address, int value)
	{
		SetWord(address, value >> 16);
		SetWord(address + 2, value);
	}

	bool Flag(int address) => memory[address] != 0;

	void SetFlag(int address, bool value) => memory[address] = (byte)(value ? 0xff : 0);

	void Clear(int start, int end) => Array.Clear(memory, start, end - start);

	#endregion

	#region Vertical blank

	public void VerticalBlank()
	{
		if (Finished)
			return;

		// The screen which was set in the last vertical blank is shown now.
		int shownScreen = screenBase;

		if (raster)
			SetRaster();

		CallRoutine(routine1);
		CallRoutine(routine2);

		if (main != null && !main.MoveNext())
		{
			main = null;
			Finished = true;
		}

		if (keyReleased)
		{
			key = 0;
			keyReleased = false;
		}

		Render(shownScreen);
	}

	void CallRoutine(Routine routine)
	{
		switch (routine)
		{
			case Routine.FadeIn:
				FadeRoutine(true);
				break;
			case Routine.FadeOut:
				FadeRoutine(false);
				break;
			case Routine.Leave:
				LeaveRoutine();
				break;
			case Routine.Rain:
				RainRoutine();
				break;
			case Routine.Pieces:
				PiecesRoutine();
				break;
			case Routine.Text:
				TextRoutine();
				break;
		}
	}

	/// <summary>
	/// The colors are 3 bits per channel. Colors 2 and 3 are changed by the raster interrupt in the lines of the name.
	/// </summary>
	void Render(int screen)
	{
		static (byte, byte, byte) ToRgb(int color)
		{
			static byte Channel(int value) => (byte)(((value & 0x7) << 1) * 17);

			return (Channel(color >> 8), Channel(color >> 4), Channel(color));
		}

		var colors = new (byte R, byte G, byte B)[16];

		for (int i = 0; i < 16; i++)
			colors[i] = ToRgb(palette[i]);

		for (int y = 0; y < IThalionLogo.Height; y++)
		{
			if (raster && y >= rasterLine && y < rasterLine + rasterCount)
			{
				int entry = RasterTable + rasterEntry + (y - rasterLine) * 4;
				colors[2] = ToRgb(Word(entry));
				colors[3] = ToRgb(Word(entry + 2));
			}

			for (int block = 0; block < 20; block++)
			{
				int offset = screen + y * LineSize + block * 8;
				int plane0 = Word(offset), plane1 = Word(offset + 2), plane2 = Word(offset + 4), plane3 = Word(offset + 6);

				for (int x = 0; x < 16; x++)
				{
					int bit = 15 - x;
					int color = ((plane0 >> bit) & 1) | (((plane1 >> bit) & 1) << 1) | (((plane2 >> bit) & 1) << 2) | (((plane3 >> bit) & 1) << 3);
					int target = (y * IThalionLogo.Width + block * 16 + x) * 4;
					(frame[target], frame[target + 1], frame[target + 2]) = colors[color];
					frame[target + 3] = 0xff;
				}
			}
		}

		Frame = Graphic.FromRGBA(IThalionLogo.Width, IThalionLogo.Height, frame);
	}

	/// <summary>
	/// The colors of the palette are written to the hardware (movem.l to $ff8240).
	/// </summary>
	void SetPalette(int address)
	{
		for (int i = 0; i < 16; i++)
			palette[i] = (ushort)Word(address + i * 2);
	}

	void SetScreen(int address) => screenBase = address & ~0xff;

	#endregion

	#region Main program

	/// <summary>
	/// Waits for the end of a phase. A key or the fire button ends the logo (L674).
	/// </summary>
	IEnumerable<bool> WaitForPhase(Action<bool> result)
	{
		while (true)
		{
			if (key != 0 || skip)
			{
				result(false);
				yield break;
			}

			if (Flag(PhaseDone))
			{
				result(true);
				yield break;
			}

			yield return true;
		}
	}

	IEnumerable<bool> MainProgram()
	{
		bool continued = true;

		// L1e8: the random numbers start with the timer data (0 here)
		SetLong(RandomSeed, 0);
		Song = 1;
		routine1 = Routine.None;
		SetLong(ScreenPointer1, Screen1);
		SetLong(ScreenPointer2, Screen2);
		SetLong(ScreenPointer3, Screen3);
		Clear(Screen1, 0x26350);
		BuildRasterTable();
		Clear(0xc6ce, 0xc79a);
		FillScreen(Screen1);
		DrawBackgroundDrops();
		CopyScreen(Screen1, Screen2);
		SetLong(ShownScreen, Screen2);
		SetLong(OtherScreen, Screen1);
		SetLong(RestoreBuffer, 0x2575e);
		SetLong(OtherRestoreBuffer, 0x24b6c);
		SwapScreens();
		SetFlag(PhaseDone, false);
		routine1 = Routine.FadeIn;
		PreshiftGraphics();

		foreach (var wait in WaitForPhase(r => continued = r))
			yield return wait;

		if (continued)
		{
			// the drops which fall down
			routine1 = Routine.None;
			int drop = Drops + 2;

			for (int i = 0; i < DropCount; i++)
			{
				SetWord(drop, i);
				drop += DropSize;
			}

			SetLong(NextDrop, Drops);

			for (int i = 0; i <= 10; i++)
				SpawnDrop();

			SetFlag(NoMoreDrops, false);
			SetFlag(PhaseDone, false);
			routine1 = Routine.Rain;

			foreach (var wait in WaitForPhase(r => continued = r))
				yield return wait;
		}

		if (continued)
		{
			// the pieces of the logo move together
			Array.Copy(memory, 0x1aa, memory, 0x24b6c, 32);
			Array.Copy(memory, 0x1aa, memory, 0x2575e, 32);
			Song = 6;
			SetFlag(PhaseDone, false);
			routine1 = Routine.Pieces;

			foreach (var wait in WaitForPhase(r => continued = r))
				yield return wait;
		}

		if (continued)
		{
			// the pieces get white, then the logo is drawn into the screen
			Song = 8;
			SetByte(FadeCounter, 0);
			SetFlag(PhaseDone, false);
			Array.Copy(memory, 0x18d0, memory, 0x18c0, 16);
			routine1 = Routine.FadeIn;

			while (Byte(FadeCounter) == 0)
				yield return true;

			DrawLogo(Long(OtherScreen), Long(ShownScreen));

			foreach (var wait in WaitForPhase(r => continued = r))
				yield return wait;
		}

		if (continued)
		{
			// the logo gets its colors
			SetFlag(PhaseDone, false);
			Array.Copy(memory, 0x18e0, memory, 0x18c0, 16);
			routine1 = Routine.FadeOut;
			SwapScreens();
			SetByte(FadeCounter, 0);

			while (Byte(FadeCounter) == 0)
				yield return true;

			CopyScreen(Long(ShownScreen), Long(OtherScreen));
			RemapColors(Long(OtherScreen));
			CopyScreen(Long(OtherScreen), Long(ShownScreen));
			CopyScreen(Long(ScreenPointer1), Long(ScreenPointer3));

			foreach (var wait in WaitForPhase(r => continued = r))
				yield return wait;
		}

		if (continued)
		{
			// the name bounces in (colors 2 and 3 are changed by the raster interrupt)
			routine1 = Routine.None;
			Array.Copy(memory, 0x18f0, memory, 0x1892, 8);

			for (int i = 0; i < 4; i++)
				palette[1 + i] = (ushort)Word(0x18f0 + i * 2);

			Array.Copy(memory, 0x1aa, memory, 0x24b6c, 32);
			Array.Copy(memory, 0x1aa, memory, 0x2575e, 32);
			SetFlag(PhaseDone, false);
			raster = true;
			Song = 2;
			routine2 = Routine.Text;

			foreach (var wait in WaitForPhase(r => continued = r))
				yield return wait;
		}

		if (continued)
		{
			// L5b0: the space key ends the logo, the secret words play another song or let the name jump
			int secretSong = SecretSong;
			int secretBounce = SecretBounce;
			key = 0;

			while (true)
			{
				while (key == 0 && !skip)
					yield return true;

				if (skip || key == SpaceScanCode)
					break;

				int pressed = key;
				key = 0;

				if (pressed == Byte(secretSong))
				{
					secretSong++;

					if (Byte(secretSong) == 0xff)
					{
						Song = 0;
						Song = 4;
						secretSong = SecretSong;
						secretBounce = SecretBounce;
					}

					continue;
				}

				if (pressed == Byte(secretBounce++))
				{
					if (Byte(secretBounce) == 0xff)
					{
						SetLong(BounceLoop, SecretBounceLoop);
						secretSong = SecretSong;
						secretBounce = SecretBounce;
					}

					continue;
				}

				secretSong = SecretSong;
				secretBounce = SecretBounce;
			}
		}

		// L626: the palette fades out
		routine1 = Routine.None;
		SetFlag(Leaving, true);
		SetFlag(PhaseDone, false);
		Array.Copy(memory, 0x1aa, memory, TargetPalette, 32);
		Song = 0;
		routine1 = Routine.Leave;

		while (!Flag(PhaseDone))
			yield return true;

		raster = false;
		routine1 = routine2 = Routine.None;
	}

	/// <summary>
	/// The colors 2 and 3 of the lines of the name and their dimmed colors (each channel - 3, at least 0).
	/// </summary>
	void BuildRasterTable()
	{
		for (int i = 0; i < RasterLines; i++)
		{
			int color = Word(0x4128 + i * 2);
			SetWord(RasterTable + i * 4, color);

			color = (color & 0x700) > 0x200 ? color - 0x300 : color & 0x077;
			color = (color & 0x070) > 0x020 ? color - 0x030 : color & 0x707;
			color = (color & 0x007) > 0x002 ? color - 0x003 : color & 0x770;
			SetWord(RasterTable + i * 4 + 2, color);
		}
	}

	/// <summary>
	/// The screen is filled with color 2 (the pattern of the registers in L2cc).
	/// </summary>
	void FillScreen(int screen)
	{
		for (int offset = 0; offset < ScreenSize; offset += 8)
		{
			SetLong(screen + offset, 0x0000ffff);
			SetLong(screen + offset + 4, 0);
		}
	}

	/// <summary>
	/// Random drops are drawn on the window. A drop marks the blocks below it in the other screen, so that no
	/// other drop starts there (L2e6).
	/// </summary>
	void DrawBackgroundDrops()
	{
		int screen = Screen1;
		int map = Screen2;

		for (int i = 0; i < BlockCount; i++)
		{
			if (Word(map) == 0)
			{
				int sprite = Random() & 0x1f;

				if (sprite <= 8)
				{
					int graphic = Long(0x2640 + sprite * 4);
					DrawBackgroundDrop(screen, graphic, map, Random() & 7);
				}
			}

			screen += 8;
			map += 8;
		}
	}

	/// <summary>
	/// L147e: draws a drop (lines - 1, then 4 planes per line) shifted to the right (masked).
	/// </summary>
	void DrawBackgroundDrop(int screen, int graphic, int map, int shift)
	{
		int lines = Word(graphic) + 1;
		graphic += 2;

		for (int line = 0; line < lines; line++)
		{
			int plane0 = Word(graphic) >> shift, plane1 = Word(graphic + 2) >> shift;
			int plane2 = Word(graphic + 4) >> shift, plane3 = Word(graphic + 6) >> shift;
			graphic += 8;
			int mask = ~(plane0 | plane1 | plane2 | plane3);
			SetWord(screen, (Word(screen) & mask) | plane0);
			SetWord(screen + 2, (Word(screen + 2) & mask) | plane1);
			SetWord(screen + 4, (Word(screen + 4) & mask) | plane2);
			SetWord(screen + 6, (Word(screen + 6) & mask) | plane3);
			SetWord(map, 1);
			map += LineSize;
			screen += LineSize;
		}
	}

	void CopyScreen(int source, int target) => Array.Copy(memory, source, memory, target, ScreenSize);

	/// <summary>
	/// L81c: the screens and their restore buffers are swapped, the new screen is shown in the next frame.
	/// </summary>
	void SwapScreens()
	{
		int buffer = Long(OtherRestoreBuffer);
		SetLong(OtherRestoreBuffer, Long(RestoreBuffer));
		SetLong(RestoreBuffer, buffer);
		int screen = Long(OtherScreen);
		SetLong(OtherScreen, Long(ShownScreen));
		SetLong(ShownScreen, screen);
		SetScreen(screen);
	}

	/// <summary>
	/// L7a8: the random numbers (seed * $bb40e62d + 1, the bits from 8).
	/// </summary>
	int Random()
	{
		uint seed = (uint)Long(RandomSeed);

		if (seed == 0)
			seed = 0xff6de62d;

		// a signed multiplication of the absolute values (L7e0)
		const int Factor = unchecked((int)0xbb40e62d);
		int a = (int)seed;
		bool negative = (a < 0) != (Factor < 0);
		uint product = (uint)Math.Abs((long)a) * (uint)Math.Abs((long)Factor);

		if (negative)
			product = (uint)-(int)product;

		seed = product + 1;
		SetLong(RandomSeed, (int)seed);

		return (int)(seed >> 8);
	}

	/// <summary>
	/// The graphics are prepared for each shift (L8fa, L952 and L9a4).
	/// </summary>
	void PreshiftGraphics()
	{
		int target = 0x26350;
		SetLong(ShiftTable, 0x1066);
		SetLong(ShiftSource, 0x18f8);
		SetLong(ShiftMask, 0x2398);

		for (int i = 0; i < 10; i++)
		{
			target = PreshiftDrop(target);
			SetLong(ShiftSource, Long(ShiftSource) + 0x110);
			SetLong(ShiftMask, Long(ShiftMask) + 0x44);
			SetLong(ShiftTable, Long(ShiftTable) + 0x10);
		}

		SetWord(ShiftLines, 0x37);
		SetLong(ShiftTable, 0x1106);
		SetLong(ShiftSource, 0x2ba6);
		target = PreshiftPiece(target, 16, 8);
		SetWord(ShiftLines, 0x36);
		SetLong(ShiftTable, 0x1146);
		SetLong(ShiftSource, 0x2f26);
		target = PreshiftPiece(target, 16, 8);
		SetWord(ShiftLines, 0x6c);
		SetLong(ShiftTable, 0x1186);
		SetLong(ShiftSource, 0x3296);
		PreshiftPiece(target, 10, 5);
	}

	/// <summary>
	/// L8fa: a drop (34 lines) with its mask for the shifts 0 to 3 (12 bytes per line: the mask twice and 4 planes).
	/// </summary>
	int PreshiftDrop(int target)
	{
		int table = Long(ShiftTable);

		for (int shift = 0; shift < 4; shift++)
		{
			int source = Long(ShiftSource);
			int mask = Long(ShiftMask);
			SetLong(table, target);
			table += 4;

			for (int line = 0; line < 34; line++)
			{
				int maskWord = Word(mask);
				mask += 2;
				SetLong(target, (maskWord << 16) | maskWord);
				SetLong(target + 4, Long(source));
				SetLong(target + 8, Long(source + 4));
				source += 8;
				target += 12;
			}
		}

		// L139c: each word is shifted (the bits which are shifted out are lost)
		table = Long(ShiftTable) + 4;

		for (int shift = 1; shift < 4; shift++)
		{
			int copy = Long(table);
			table += 4;

			for (int line = 0; line < 34; line++)
			{
				for (int word = 0; word < 6; word++)
				{
					int address = copy + line * 12 + word * 2;
					SetWord(address, Word(address) >> shift);
				}
			}
		}

		return target;
	}

	/// <summary>
	/// L952 and L9a4: a piece of the logo (one plane) for the shifts 0 to 15, each line gets one more word.
	/// </summary>
	int PreshiftPiece(int target, int bytesPerLine, int words)
	{
		int table = Long(ShiftTable);
		int lines = Word(ShiftLines) + 1;

		for (int shift = 0; shift < 16; shift++)
		{
			int source = Long(ShiftSource);
			SetLong(table, target);
			table += 4;

			for (int line = 0; line < lines; line++)
			{
				Array.Copy(memory, source, memory, target, bytesPerLine);
				source += bytesPerLine;
				target += bytesPerLine;
				SetWord(target, 0);
				target += 2;
			}
		}

		// L1870: the lines are shifted to the right
		table = Long(ShiftTable) + 4;

		for (int shift = 1; shift < 16; shift++)
		{
			int address = Long(table);
			table += 4;

			for (int line = 0; line < lines; line++)
			{
				int carry = 0;

				for (int word = 0; word <= words; word++)
				{
					int value = Word(address) << 16 >>> shift;
					SetWord(address, (value >> 16) | carry);
					carry = value & 0xffff;
					address += 2;
				}
			}
		}

		return target;
	}

	/// <summary>
	/// L164a: the logo (240x168 at 32,16) is drawn into the screen where the pieces are (plane 3 of the other screen).
	/// </summary>
	void DrawLogo(int screen, int maskScreen)
	{
		int target = screen + 2576;
		int mask = maskScreen + 2582;
		int source = 0x427a;

		for (int line = 0; line < 168; line++)
		{
			for (int block = 0; block < 15; block++)
			{
				int inverted = ~Word(mask);

				for (int plane = 0; plane < 3; plane++)
					SetWord(target + plane * 2, (Word(target + plane * 2) & inverted) | Word(source + plane * 2));

				SetWord(target + 6, Word(source + 6));
				source += 8;
				target += 8;
				mask += 8;
			}

			target += 40;
			mask += 40;
		}
	}

	/// <summary>
	/// L862: the colors 1 to 4 of the drops get the colors 10, 11, 13 and 14 (colors 2 and 3 are used for the name).
	/// </summary>
	void RemapColors(int screen)
	{
		for (int offset = screen; offset < screen + BlockCount * 8; offset += 8)
		{
			int plane0 = Word(offset), plane1 = Word(offset + 2), plane2 = Word(offset + 4), plane3 = Word(offset + 6);

			for (int bit = 15; bit >= 0; bit--)
			{
				int color = ((plane0 >> bit) & 1) | (((plane1 >> bit) & 1) << 1) | (((plane2 >> bit) & 1) << 2) | (((plane3 >> bit) & 1) << 3);
				int newColor = color switch
				{
					1 => 10,
					2 => 11,
					3 => 13,
					4 => 14,
					_ => -1
				};

				if (newColor == -1)
					continue;

				int clear = ~(1 << bit);
				plane0 = (plane0 & clear) | ((newColor & 1) << bit);
				plane1 = (plane1 & clear) | (((newColor >> 1) & 1) << bit);
				plane2 = (plane2 & clear) | (((newColor >> 2) & 1) << bit);
				plane3 = (plane3 & clear) | (((newColor >> 3) & 1) << bit);
			}

			SetWord(offset, plane0);
			SetWord(offset + 2, plane1);
			SetWord(offset + 4, plane2);
			SetWord(offset + 6, plane3);
		}
	}

	#endregion

	#region Fading

	/// <summary>
	/// Every 4 vertical blanks each channel of the palette moves by 1 to the target palette ($11c8 and $127a).
	/// When it is reached, the phase is done.
	/// </summary>
	void FadeRoutine(bool up)
	{
		if (Flag(PhaseDone))
			return;

		if (!NextFadeStep())
			return;

		SetFlag(FadeChanged, false);
		FadeColors(TargetPalette, Palette, 16, up);
		SetPalette(Palette);

		if (!Flag(FadeChanged))
			SetFlag(PhaseDone, true);
	}

	/// <summary>
	/// $12d2: the palette and the colors of the raster fade to black.
	/// </summary>
	void LeaveRoutine()
	{
		if (Flag(PhaseDone))
			return;

		if (!NextFadeStep())
			return;

		SetFlag(FadeChanged, false);
		FadeColors(TargetPalette, Palette, 16, false);
		SetPalette(Palette);
		FadeColors(RasterTableEnd, RasterTable, RasterLines * 2, false);

		if (!Flag(FadeChanged))
			SetFlag(PhaseDone, true);
	}

	bool NextFadeStep()
	{
		SetByte(FadeCounter, Byte(FadeCounter) + 1);

		if (Byte(FadeCounter) != 4)
			return false;

		SetByte(FadeCounter, 0);

		return true;
	}

	void FadeColors(int target, int current, int count, bool up)
	{
		for (int i = 0; i < count; i++)
		{
			int targetColor = Word(target + i * 2);
			int color = Word(current + i * 2);

			foreach (int mask in new[] { 0x007, 0x070, 0x700 })
			{
				if ((targetColor & mask) != (color & mask))
				{
					color += up ? (mask & -mask) : -(mask & -mask);
					SetFlag(FadeChanged, true);
				}
			}

			SetWord(current + i * 2, color);
		}
	}

	#endregion

	#region Rain

	/// <summary>
	/// Ld72: a new drop starts at a random line in its column if there is enough space below it (3 tries). When no more
	/// drops start and all drops have landed, the phase is done.
	/// </summary>
	void SpawnDrop()
	{
		if (Flag(NoMoreDrops))
		{
			if (Word(ActiveDrops) == 0)
				SetFlag(PhaseDone, true);

			return;
		}

		int drop = Long(NextDrop);
		SetWord(SpawnTries, 0);

		do
		{
			if (Word(drop) == 0)
			{
				int y = Random() & 0xff;

				if (y >= 0xe6)
					y -= 0x1a;

				y -= 0x1e;

				while (true)
				{
					int blocked = ReserveSpace(y, Word(drop + 2));

					if (blocked == -1)
					{
						SetWord(ActiveDrops, Word(ActiveDrops) + 1);
						SetWord(drop + 10, y);
						SetWord(drop + 6, -33);
						SetWord(drop, -1);
						SetWord(drop + 4, Random() & 3);
						SetWord(drop + 12, 0);
						int speed = Random() & 0x3f;

						if (speed >= 0x30)
							speed -= 0x10;

						speed += 5;
						SetWord(drop + 14, speed);
						SetWord(drop + 16, speed);
						SetWord(SpawnTries, 3);
						break;
					}

					y = blocked;

					if (y >= 200)
					{
						SetWord(SpawnTries, Word(SpawnTries) + 1);
						break;
					}
				}
			}

			drop += 3 * DropSize;

			if (drop >= NextDrop)
				drop -= DropCount * DropSize;
		}
		while (Word(SpawnTries) != 3);

		SetLong(NextDrop, drop);
	}

	/// <summary>
	/// Le4e: the 34 lines of a drop must be free in the map of the column (20 bytes per line). Returns -1 if they were
	/// reserved, or the line after the first used line.
	/// </summary>
	int ReserveSpace(int y, int column)
	{
		int lines = 33;

		if (y < 0)
		{
			lines += y;
			y = 0;
		}
		else if (y + lines >= 200)
		{
			lines -= y + lines - 199;
		}

		int address = Long(ScreenPointer3) + y * 20 + column;
		int line = y;

		for (int i = 0; i <= lines; i++)
		{
			line++;

			if (Byte(address + i * 20) != 0)
				return line;
		}

		for (int i = 0; i <= lines; i++)
			SetByte(address + i * 20, 1);

		return -1;
	}

	/// <summary>
	/// $c52: the drops are drawn into the screen (its background is restored first). Falling drops slow down before
	/// they reach their line, landed drops stay in both screens.
	/// </summary>
	void RainRoutine()
	{
		SwapScreens();
		RestoreDrops(Long(RestoreBuffer));
		int screen = Long(ShownScreen);
		int restore = Long(RestoreBuffer);
		int drop = Drops;

		for (int i = 0; i < DropCount; i++, drop += DropSize)
		{
			int state = SignedWord(drop);

			if (state <= 0)
				continue;

			DrawDrop(screen, restore, 0x1066 + Word(drop + 12), SignedWord(drop + 6), Word(drop + 2), Word(drop + 4));

			if (state != 3)
			{
				// landed: the drop is not restored
				SetLong(restore + 2, 0);
				SetWord(drop, state - 1);

				if (state - 1 == 0)
				{
					SetWord(ActiveDrops, Word(ActiveDrops) - 1);
					SpawnDrop();
				}
			}
			else
			{
				int distance = SignedWord(drop + 10) - SignedWord(drop + 6);
				int speed = distance switch
				{
					< 16 => Long(0xeb2 + distance * 4),
					< 32 => 0x10000,
					< 64 => 0x18000,
					_ => 0x20000
				};
				int y = Long(drop + 6) + speed;
				SetLong(drop + 6, y);

				if ((short)(y >> 16) >= SignedWord(drop + 10))
				{
					SetWord(drop, 2);
				}
				else
				{
					SetWord(drop + 14, Word(drop + 14) - 1);

					if (Word(drop + 14) == 0)
					{
						SetWord(drop + 14, Word(drop + 16));

						if (Word(drop + 12) != 0x90)
							SetWord(drop + 12, Word(drop + 12) + 0x10);
					}
				}
			}

			restore += 278;
		}

		drop = Drops;

		for (int i = 0; i < DropCount; i++, drop += DropSize)
		{
			if (SignedWord(drop) < 0)
				SetWord(drop, 3);
		}

		if (SignedWord(ActiveDrops) <= 10)
			SetFlag(NoMoreDrops, true);
	}

	/// <summary>
	/// L13d4: the backgrounds of the drops are restored (11 entries of 278 bytes, the last one first).
	/// </summary>
	void RestoreDrops(int buffer)
	{
		for (int entry = buffer + 10 * 278; entry >= buffer; entry -= 278)
		{
			int lines = Word(entry) + 1;
			int address = Long(entry + 2);

			if (address == 0)
				continue;

			SetLong(entry + 2, 0);

			for (int line = 0; line < lines; line++)
			{
				SetLong(address, Long(entry + 6 + line * 8));
				SetLong(address + 4, Long(entry + 10 + line * 8));
				address += LineSize;
			}
		}
	}

	/// <summary>
	/// L1400: the background is saved and the drop (34 lines, a frame of the animation and one of 4 shifts) is
	/// drawn into the planes 0 to 2 (plane 3 has the pieces of the logo).
	/// </summary>
	void DrawDrop(int screen, int restore, int frameTable, int y, int column, int shift)
	{
		int lines = 33;
		int skip = 0;

		if (y < 0)
		{
			lines += y;
			skip = -y * 12;
			y = 0;
		}
		else if (y + lines >= 200)
		{
			lines -= y + lines - 199;
		}

		int address = screen + y * LineSize + column * 8;
		SetWord(restore, lines);
		SetLong(restore + 2, address);

		for (int line = 0; line <= lines; line++)
		{
			SetLong(restore + 6 + line * 8, Long(address + line * LineSize));
			SetLong(restore + 10 + line * 8, Long(address + line * LineSize + 4));
		}

		int source = Long(frameTable + shift * 4) + skip;

		for (int line = 0; line <= lines; line++)
		{
			int inverted = ~Long(source);
			int planes01 = Long(source + 4);
			int planes23 = Long(source + 8);
			source += 12;
			SetLong(address, (Long(address) & inverted) | planes01);
			SetWord(address + 4, (Word(address + 4) & inverted) | (planes23 >>> 16));
			address += LineSize;
		}
	}

	#endregion

	#region Pieces

	/// <summary>
	/// $9f0: the 4 pieces of the logo (plane 3) move to their positions (1 pixel per vertical blank).
	/// </summary>
	void PiecesRoutine()
	{
		SwapScreens();
		int restore = Long(RestoreBuffer);
		restore = ClearPlane3(restore, 11);
		restore = ClearPlane3(restore, 9);
		restore = ClearPlane3(restore, 9);
		ClearPlane3(restore, 6);

		restore = Long(RestoreBuffer);
		int screen = Long(ShownScreen);
		restore = DrawBigPiece(screen, restore, Word(PiecePositions + 24));
		restore = DrawPiece(screen, restore, Word(PiecePositions), SignedWord(PiecePositions + 4), 0x1106, 55, 9);
		restore = DrawPiece(screen, restore, Word(PiecePositions + 8), SignedWord(PiecePositions + 12), 0x1146, 54, 9);
		DrawPiece(screen, restore, Word(PiecePositions + 16), SignedWord(PiecePositions + 20), 0x1186, 108, 6);

		bool moved = false;

		for (int i = 0; i < 7; i++)
		{
			int position = PiecePositions + i * 4;
			int current = SignedWord(position);
			int target = SignedWord(position + 2);

			if (current != target)
			{
				moved = true;
				SetWord(position, current + Math.Sign(target - current));
			}
		}

		if (!moved)
			SetFlag(PhaseDone, true);
	}

	/// <summary>
	/// L17c6, L1686 and L1732: plane 3 of a drawn piece is cleared.
	/// </summary>
	int ClearPlane3(int restore, int words)
	{
		int lines = Word(restore) + 1;
		int address = Long(restore + 2);

		if (address != 0)
		{
			for (int line = 0; line < lines; line++)
			{
				for (int word = 0; word < words; word++)
					SetWord(address + word * 8, 0);

				address += LineSize;
			}
		}

		return restore + 6;
	}

	/// <summary>
	/// L180a: the lower piece (176x120 at x 64) comes from the bottom (it replaces plane 3).
	/// </summary>
	int DrawBigPiece(int screen, int restore, int y)
	{
		int source = 0x36d8;
		int lines = 119;

		if (y + lines >= 200)
			lines -= y + lines - 199;

		int address = screen + y * LineSize + 38;
		SetWord(restore, lines);
		SetLong(restore + 2, address);

		for (int line = 0; line <= lines; line++)
		{
			for (int word = 0; word < 11; word++)
			{
				SetWord(address + word * 8, Word(source));
				source += 2;
			}

			address += LineSize;
		}

		return restore + 6;
	}

	/// <summary>
	/// L16be and L175e: a piece (one plane, shifted) is or'ed into plane 3.
	/// </summary>
	int DrawPiece(int screen, int restore, int x, int y, int shiftTable, int lines, int words)
	{
		int skip = 0;

		if (y < 0)
		{
			lines += y;
			skip = -y * words * 2;
			y = 0;
		}

		SetWord(restore, lines);
		int address = screen + y * LineSize + (x & 0xfff0) / 2 + 6;
		int source = Long(shiftTable + (x & 0xf) * 4) + skip;
		SetLong(restore + 2, address);

		for (int line = 0; line <= lines; line++)
		{
			for (int word = 0; word < words; word++)
			{
				SetWord(address + word * 8, Word(address + word * 8) | Word(source));
				source += 2;
			}

			address += LineSize;
		}

		return restore + 6;
	}

	#endregion

	#region Name

	/// <summary>
	/// $b82: the name moves by the next value of the bounce table (until $80, then the loop follows, the first time the
	/// phase is done). The raster interrupt sets the colors 2 and 3 of the lines of the name (at the line of the shown
	/// frame, Timer B at $c0a).
	/// </summary>
	void SetRaster()
	{
		palette[2] = (ushort)Word(0x1894);
		palette[3] = (ushort)Word(0x1896);
		int bounce = Long(BouncePointer);
		int y = (sbyte)Byte(bounce++);

		if (Byte(bounce) == 0x80)
		{
			bounce = Long(BounceLoop);

			if (!Flag(Leaving))
			{
				SetFlag(PhaseDone, true);
				SetFlag(Leaving, true);
			}
		}

		SetLong(BouncePointer, bounce);
		int shownY = SignedWord(TextY);
		SetWord(TextY, y);
		int count = RasterLines;
		int start = 0;

		if (shownY <= 0)
		{
			int skip = -shownY + 1;
			count -= skip;
			start = skip;
			shownY = 1;
		}

		rasterLine = shownY;
		rasterEntry = start * 4;
		rasterCount = count;
	}

	/// <summary>
	/// $ab8: the name (192x51, 2 planes, masked) is drawn at its new line, the area of the last frame is restored first.
	/// </summary>
	void TextRoutine()
	{
		SwapScreens();
		int restore = Long(RestoreBuffer);
		int lines = Word(restore) + 1;
		int offset = Word(restore + 2);
		int address = Long(restore + 4);

		if (offset != 0)
		{
			int source = Long(ScreenPointer3) + offset;

			for (int line = 0; line < lines; line++)
			{
				Array.Copy(memory, source, memory, address, 96);
				source += LineSize;
				address += LineSize;
			}
		}

		DrawText(Long(ShownScreen), Long(RestoreBuffer), SignedWord(TextY));
	}

	/// <summary>
	/// L1504: the name is drawn into the planes 0 and 1 (the planes 2 and 3 are cleared under it).
	/// </summary>
	void DrawText(int screen, int restore, int y)
	{
		int lines = 50;
		int skip = 0;

		if (y < 0)
		{
			lines += y;
			skip = -y * 48;
			y = 0;
		}

		int offset = y * LineSize + 0x20;
		int address = screen + offset;
		SetWord(restore, lines);
		SetWord(restore + 2, offset);
		SetLong(restore + 4, address);
		int source = 0x913a + skip;

		for (int line = 0; line <= lines; line++)
		{
			for (int block = 0; block < 12; block++)
			{
				int plane0 = Word(source), plane1 = Word(source + 2);
				source += 4;
				int mask = ~(plane0 | plane1);
				SetWord(address, (Word(address) & mask) | plane0);
				SetWord(address + 2, (Word(address + 2) & mask) | plane1);
				SetWord(address + 4, Word(address + 4) & mask);
				SetWord(address + 6, Word(address + 6) & mask);
				address += 8;
			}

			address += 64;
		}
	}

	#endregion
}
