using Amber.Common;
using Amber.Renderer;
using Amberstar.GameData;

namespace Amberstar.Game
{
	internal interface IRenderText
	{
		bool SupportsScrolling { get; }
		event Action? ScrollEnded;

		void Show(int x, int y, byte displayLayer);
		void ShowInArea(int x, int y, int width, int height, byte displayLayer);
		void Delete();
		bool Scroll(int lines);
		bool ScrollFullHeight();
	}

	internal class TextManager(Game game, IFont font,
		IFontInfoProvider fontInfoProvider)
	{
		public const int DefaultInkColorIndex = 8;
		public const int DefaultPaperColorIndex = 0;
		public const int TransparentPaper = -1;
		const int TicksPerScroll = 4; // TODO
		const byte DefaultPaletteIndex = 0; // UI

		struct TextBlock(int textColorIndex, int paperColorIndex, string text, bool runes)
		{
			public int TextColorIndex = textColorIndex;
			public int PaperColorIndex = paperColorIndex;
			public bool Runes = runes;
			public string Text = text;
		}

		struct TextLine(List<TextBlock> textBlocks)
		{
			public List<TextBlock> TextBlocks = textBlocks;
		}

		class Text
		(
			Game game, List<TextLine> textLines, IFont font,
			IFontInfoProvider fontInfoProvider, byte paletteIndex
		) : IRenderText
		{
			readonly ILayer layer = game.Renderer.Layers[(int)Layer.Text];
			readonly List<TextLine> textLines = textLines;
			readonly List<List<ISprite>> glyphs = [];
			readonly List<long> startedScrollAction = [];
			int maxScroll = 0;
			int areaX = 0;
			int areaY = 0;
			int areaHeight = 0;
			int scrollOffsetInPixels = 0;
			int scrollOffsetInLines = 0;
			byte displayLayer = 0;

			public event Action? ScrollEnded;

			public bool SupportsScrolling { get; private set; } = false;

			private static readonly Dictionary<char, int> UnicodeToAtariST = new()
			{
				{ 'Ç', 0x80 },
				{ 'ü', 0x81 },
				{ 'é', 0x82 },
				{ 'â', 0x83 },
				{ 'ä', 0x84 },
				{ 'à', 0x85 },
				{ 'å', 0x86 },
				{ 'ç', 0x87 },
				{ 'ê', 0x88 },
				{ 'ë', 0x89 },
				{ 'è', 0x8A },
				{ 'ï', 0x8B },
				{ 'î', 0x8C },
				{ 'ì', 0x8D },
				{ 'Ä', 0x8E },
				{ 'Å', 0x8F },
				{ 'É', 0x90 },
				{ 'æ', 0x91 },
				{ 'Æ', 0x92 },
				{ 'ô', 0x93 },
				{ 'ö', 0x94 },
				{ 'ò', 0x95 },
				{ 'û', 0x96 },
				{ 'ù', 0x97 },
				{ 'ÿ', 0x98 },
				{ 'Ö', 0x99 },
				{ 'Ü', 0x9A },
				{ '¢', 0x9B },
				{ '£', 0x9C },
				{ '¥', 0x9D },
				{ 'ß', 0x9E },
				{ 'ƒ', 0x9F },
				{ 'á', 0xA0 },
				{ 'í', 0xA1 },
				{ 'ó', 0xA2 },
				{ 'ú', 0xA3 },
				{ 'ñ', 0xA4 },
				{ 'Ñ', 0xA5 },
				{ '¿', 0xA8 },
				{ '¬', 0xAA },
				{ '¡', 0xAD },
				{ '«', 0xAE },
				{ '»', 0xAF },
				{ 'ã', 0xB0 },
				{ 'õ', 0xB1 },
				{ 'Ø', 0xB2 },
				{ 'ø', 0xB3 },
				{ 'œ', 0xB4 },
				{ 'Œ', 0xB5 },
				{ 'À', 0xB6 },
				{ 'Ã', 0xB7 },
				{ 'Õ', 0xB8 },
				{ 'ĳ', 0xC0 },
				{ 'Ĳ', 0xC1 },
				{ '§', 0xDD },
				{ '°', 0xF8 },
				{ '²', 0xFD },
				{ '³', 0xFE },
			};

			private static char ConvertChar(char ch)
			{
				// Note: This is not related to original Amberstar code but
				// the encoding on the Atari/Amiga was different so we have
				// to map some characters like german Umlauts here.
				return (char)UnicodeToAtariST.GetValueOrDefault(ch, ch);
			}

			private ISprite CreateTextSprite(int x, int y, int glyphIndex, int colorIndex, int paperIndex)
			{
				// TODO: colors
				var textureAtlas = layer.Config.Texture!;
				var glyph = layer.SpriteFactory!.Create();
				glyph.DisplayLayer = displayLayer;
				glyph.Position = new(x, y);
				glyph.Size = new(font.GlyphWidth, font.GlyphHeight);
				glyph.TextureOffset = textureAtlas.GetOffset(glyphIndex);
				int clipHeight = areaHeight == 0 ? int.MaxValue : areaHeight;
				glyph.ClipRect = new(areaX, areaY, int.MaxValue, clipHeight);
				glyph.MaskColorIndex = (byte)colorIndex;
				glyph.PaletteIndex = paletteIndex;
				glyph.Visible = true;

				return glyph;
			}

			private void SetupTextLine(int x, int y, int line, TextLine textLine)
			{
				List<ISprite> glyphLine;

				if (line == glyphs.Count)
				{
					glyphLine = new List<ISprite>();
					glyphs.Add(glyphLine);
				}
				else
				{
					glyphs[line].ForEach(glyph => glyph.Visible = false);
					glyphs[line].Clear();
					glyphLine = glyphs[line];
				}

				foreach (var textBlock in textLine.TextBlocks)
				{
					var mapper = textBlock.Runes
						? fontInfoProvider.RuneGlyphTextureIndices
						: fontInfoProvider.TextGlyphTextureIndices;

					foreach (char ch in textBlock.Text)
					{
						if (ch == 14)
							x -= font.Advance;
						else if (ch == ' ' || ch == '\t')
							x += font.Advance;
						else
						{
							glyphLine.Add(CreateTextSprite(x, y, mapper[ConvertChar(ch)], textBlock.TextColorIndex, textBlock.PaperColorIndex));
							x += font.Advance;
						}
					}
				}
			}

			private void InternalShow(int x, int y, byte displayLayer, int lineCount)
			{
				areaX = x;
				areaY = y;
				scrollOffsetInPixels = 0;
				scrollOffsetInLines = 0;
				this.displayLayer = displayLayer;

				if (startedScrollAction.Count != 0)
				{
					game.DeleteDelayedActions(startedScrollAction.ToArray());
					startedScrollAction.Clear();
				}

				lineCount = Math.Min(lineCount, textLines.Count);

				for (int i = 0; i < lineCount; i++)
				{
					SetupTextLine(x, y, i, textLines[i]);
					y += font.LineHeight;
				}
			}

			public void ShowInArea(int x, int y, int width, int height, byte displayLayer)
			{
				int diff = MathUtil.Limit(0, font.LineHeight - font.GlyphHeight, height - 1);
				int numDisplayedRows = (height + diff) / font.LineHeight;
				areaHeight = height;

				maxScroll = Math.Max(0, textLines.Count - numDisplayedRows);
				SupportsScrolling = numDisplayedRows < textLines.Count;
				InternalShow(x, y, displayLayer, numDisplayedRows);
			}

			public void Show(int x, int y, byte displayLayer)
			{
				areaHeight = 0;
				SupportsScrolling = false;
				InternalShow(x, y, displayLayer, textLines.Count);
			}

			public bool Scroll(int lines)
			{
				if (maxScroll == 0)
					return false;

				int scrollAmount = Math.Max(1, font.LineHeight / 2);

				if (lines > maxScroll)
					lines = maxScroll;

				maxScroll -= lines;

				void ScrollText() => ScrollTextBy(scrollAmount);

				void ScrollTextBy(int amount)
				{
					int diff = MathUtil.Limit(0, font.LineHeight - font.GlyphHeight, areaHeight - 1);
					int numDisplayedRows = (areaHeight + diff) / font.LineHeight;
					scrollOffsetInPixels += amount;

					if (scrollOffsetInPixels >= font.LineHeight)
					{
						// Line leaves upper bound.
						scrollOffsetInPixels -= font.LineHeight;
						scrollOffsetInLines++;

						var firstLine = glyphs[0];
						glyphs.RemoveAt(0);

						foreach (var glyph in glyphs.SelectMany(g => g))
						{
							glyph.Position = new(glyph.Position.X, glyph.Position.Y - amount);
						}

						glyphs.Add(firstLine);

						int offset = numDisplayedRows * font.LineHeight - scrollOffsetInPixels;
						int lineIndex = scrollOffsetInLines + numDisplayedRows - 1;
						SetupTextLine(areaX, areaY + offset, glyphs.Count - 1, textLines[lineIndex]);
					}
					else
					{
						// Just move them up.
						foreach (var glyph in glyphs.SelectMany(g => g))
						{
							glyph.Position = new(glyph.Position.X, glyph.Position.Y - amount);
						}

						if (glyphs.Count == numDisplayedRows)
						{
							int newLineIndex = scrollOffsetInLines + numDisplayedRows;

							// We must show the additional line
							if (newLineIndex < textLines.Count)
								SetupTextLine(areaX, areaY - scrollOffsetInPixels + numDisplayedRows * font.LineHeight, glyphs.Count, textLines[newLineIndex]);
						}
					}
				}

				void ScrollEnd() => ScrollEnded?.Invoke();

				int totalScrolls = lines * font.LineHeight / scrollAmount;

				var startedScrollActions = new List<long>(totalScrolls + 1);

				for (int i = 0; i < totalScrolls; i++)
				{
					startedScrollActions.Add(game.AddDelayedAction(i * TicksPerScroll, ScrollText));
				}

				int totalScrollAmount = totalScrolls * scrollAmount;

				if (totalScrollAmount < lines * font.LineHeight)
				{
					int amount = lines * font.LineHeight - totalScrollAmount;
					startedScrollActions.Add(game.AddDelayedAction(totalScrolls * TicksPerScroll, () => ScrollTextBy(amount)));
					startedScrollActions.Add(game.AddDelayedAction((totalScrolls + 1) * TicksPerScroll, ScrollEnd));
				}
				else
				{
					startedScrollActions.Add(game.AddDelayedAction(totalScrolls * TicksPerScroll, ScrollEnd));
				}

				return true;
			}

			public bool ScrollFullHeight()
			{
				int diff = MathUtil.Limit(0, font.LineHeight - font.GlyphHeight, areaHeight - 1);
				int numDisplayedRows = (areaHeight + diff) / font.LineHeight;
				return Scroll(numDisplayedRows);
			}

			public void Delete()
			{
				if (startedScrollAction.Count != 0)
				{
					game.DeleteDelayedActions(startedScrollAction.ToArray());
					startedScrollAction.Clear();
				}

				foreach (var glyph in glyphs.SelectMany(g => g))
					glyph.Visible = false;
			}
		}

		public IRenderText Create(IText text, int maxWidth,
			int defaultTextColorIndex = DefaultInkColorIndex,
			int defaultPaperColorIndex = DefaultPaperColorIndex,
			byte paletteIndex = DefaultPaletteIndex)
		{
			int maxWidthInCharacters = maxWidth / font.Advance;
			var lines = text.GetLines(maxWidthInCharacters);
			var textLines = new List<TextLine>();
			var coloredTextBlocks = new List<TextBlock>();
			string currentTextBlock = string.Empty;
			int currentInk = defaultTextColorIndex;
			int currentPaper = defaultPaperColorIndex;
			bool runes = false;

			foreach (var line in lines)
			{
				for (int i = 0; i < line.Length; i++)
				{
					var ch = line[i];

					if (ch == 1) // Set ink
					{
						int ink = line[++i];

						if (ink == currentInk)
							continue;

						EndBlock();

						currentInk = ink;
					}
					else if (ch == 2) // Set paper
					{
						int paper = line[++i];

						if (paper == 255) // transparent
							paper = TransparentPaper;

						if (paper == currentPaper)
							continue;

						EndBlock();

						currentPaper = paper;
					}
					else if (ch == '~')
					{
						EndBlock();

						runes = !runes;
					}
					else if (ch == '#' || ch == '\n')
					{
						EndBlock();
						EndLine();
					}
					else
					{
						currentTextBlock += ch;
					}
				}

				EndBlock();
				EndLine();
			}

			void EndBlock()
			{
				if (currentTextBlock.Length > 0)
				{
					coloredTextBlocks.Add(new(currentInk, currentPaper, currentTextBlock, runes));
					currentTextBlock = string.Empty;
				}
			}

			void EndLine()
			{
				textLines.Add(new(new(coloredTextBlocks)));
				coloredTextBlocks.Clear();
			}

			EndBlock();
			EndLine();

			return new Text(game, textLines, font, fontInfoProvider, paletteIndex);
		}

		public IRenderText Create(string text,
			int defaultTextColorIndex = DefaultInkColorIndex,
			int defaultPaperColorIndex = DefaultPaperColorIndex,
			byte paletteIndex = DefaultPaletteIndex)
		{
			var textLines = new List<TextLine>();
			var coloredTextBlocks = new List<TextBlock>();
			string currentTextBlock = string.Empty;
			int currentInk = defaultTextColorIndex;
			int currentPaper = defaultPaperColorIndex;
			bool runes = false;

			for (int i = 0; i < text.Length; i++)
			{
				var ch = text[i];

				if (ch == 1) // Set ink
				{
					int ink = text[++i];

					if (ink == currentInk)
						continue;

					EndBlock();

					currentInk = ink;
				}
				else if (ch == 2) // Set paper
				{
					int paper = text[++i];

					if (paper == 255) // transparent
						paper = TransparentPaper;

					if (paper == currentPaper)
						continue;

					EndBlock();

					currentPaper = paper;
				}
				else if (ch == '~')
				{
					EndBlock();

					runes = !runes;
				}
				else if (ch == '#' || ch == '\n')
				{
					EndBlock();
					EndLine();
				}
				else
				{
					currentTextBlock += ch;
				}
			}

			void EndBlock()
			{
				if (currentTextBlock.Length > 0)
				{
					coloredTextBlocks.Add(new(currentInk, currentPaper, currentTextBlock, runes));
					currentTextBlock = string.Empty;
				}
			}

			void EndLine()
			{
				textLines.Add(new(new(coloredTextBlocks)));
				coloredTextBlocks.Clear();
			}

			EndBlock();
			EndLine();

			return new Text(game, textLines, font, fontInfoProvider, paletteIndex);
		}
	}
}
