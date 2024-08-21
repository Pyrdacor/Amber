using Amber.Renderer;
using Amberstar.GameData;

namespace Amberstar.Game
{
	internal interface IRenderText
	{
		IRenderText Wrap(int width, int height);
		void Draw(int x, int y, byte displayLayer);
		void Delete();
	}

	internal class TextManager(Game game, IFont font,
		IFontInfoProvider fontInfoProvider)
	{
		const int DefaultInkColorIndex = 8;
		const int DefaultPaperColorIndex = 0;
		const int TransparentPaper = -1;

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
			IFontInfoProvider fontInfoProvider
		) : IRenderText
		{
			readonly List<TextLine> textLines = textLines;
			readonly List<ISprite> glyphs = [];

			void EnsureGlyphs()
			{
				var layer = game.Renderer.Layers[(int)Layer.Text];
				byte paletteIndex = game.PaletteIndexProvider.GetTextPaletteIndex();

				if (glyphs.Count == 0 && textLines.Count != 0)
				{
					foreach (var textBlock in textLines.SelectMany(line => line.TextBlocks))
					{
						foreach (char ch in textBlock.Text)
						{
							if (ch == 14 || ch == ' ' || ch == '\t')
								continue;

							var glyph = layer.SpriteFactory!.Create();
							glyph.PaletteIndex = paletteIndex;
							glyph.Size = new(font.GlyphWidth, font.GlyphHeight);
							glyphs.Add(glyph);
						}
					}
				}
			}

			public IRenderText Wrap(int width, int height)
			{
				// TODO
				/*int x = 0;
				int y = 0;

				foreach (var line in textLines)
				{
					foreach (var textBlock in line.TextBlocks)
					{

					}
				}*/
				return this;
			}

			public void Draw(int x, int y, byte displayLayer)
			{
				EnsureGlyphs();

				var textureAtlas = game.Renderer.Layers[(int)Layer.Text].Config.Texture!;
				int startX = x;
				int glyphIndex = 0;

				foreach (var line in textLines)
				{
					foreach (var textBlock in line.TextBlocks)
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
								// TODO: colors
								var glyph = glyphs[glyphIndex++];
								glyph.DisplayLayer = displayLayer;
								glyph.Position = new(x, y);
								glyph.TextureOffset = textureAtlas.GetOffset(mapper[ch]);
								glyph.Visible = true;
								x += font.Advance;
							}
						}
					}

					x = startX;
					y += font.LineHeight;
				}
			}

			public void Delete()
			{
				glyphs.ForEach(glyph => glyph.Visible = false);
			}
		}

		public IRenderText Create(string text)
		{
			var textLines = new List<TextLine>();
			var coloredTextBlocks = new List<TextBlock>();
			string currentTextBlock = string.Empty;
			int currentInk = DefaultInkColorIndex;
			int currentPaper = DefaultPaperColorIndex;
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

			return new Text(game, textLines, font, fontInfoProvider);
		}
	}
}
