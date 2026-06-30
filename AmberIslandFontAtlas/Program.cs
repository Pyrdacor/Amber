using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Text.Json;

if (args.Length < 3)
{
	Console.WriteLine("Usage: AmberIslandFontAtlas <fontPath> <fontSize> <outputPng> [outputMetrics] [glyphGap]");
	Console.WriteLine();
	Console.WriteLine("  fontPath      Path to a TTF or OTF font file");
	Console.WriteLine("  fontSize      Font size in pixels");
	Console.WriteLine("  outputPng     Output PNG atlas path");
	Console.WriteLine("  outputMetrics Output metrics JSON path (default: same as outputPng with .json)");
	Console.WriteLine("  glyphGap      Minimum gap in pixels between two glyphs in a row (default: 1)");
	return;
}

string fontPath = args[0];
int fontSize = int.Parse(args[1]);
string outputPng = args[2];
string outputMetrics = args.Length > 3 ? args[3] : Path.ChangeExtension(outputPng, ".json");
int minGap = args.Length > 4 ? int.Parse(args[4]) : 1;

if (!File.Exists(fontPath))
{
	Console.Error.WriteLine($"Font file not found: {fontPath}");
	return;
}

const int FirstChar = 0x20; // space
const int LastChar = 0x7E;  // ~
int charCount = LastChar - FirstChar + 1; // 95

var fontCollection = new PrivateFontCollection();
fontCollection.AddFontFile(Path.GetFullPath(fontPath));
var fontFamily = fontCollection.Families[0];
using var font = new Font(fontFamily, fontSize, FontStyle.Regular, GraphicsUnit.Pixel);

var format = new StringFormat(StringFormat.GenericTypographic);
format.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;

// Phase 1: Render each character individually and measure actual pixel bounds.
var advanceWidths = new int[charCount];
var pixelWidths = new int[charCount];
int maxPixelWidth = 0;
int maxHeight = 0;

int tempSize = fontSize * 3;
using var tempBitmap = new Bitmap(tempSize, tempSize, PixelFormat.Format32bppArgb);
using var tempGraphics = Graphics.FromImage(tempBitmap);
tempGraphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

for (int i = 0; i < charCount; i++)
{
	char c = (char)(FirstChar + i);
	string s = c.ToString();

	var measured = tempGraphics.MeasureString(s, font, 0, format);
	int measuredAdvance = (int)Math.Ceiling(measured.Width);
	int measuredHeight = (int)Math.Ceiling(measured.Height);

	tempGraphics.Clear(Color.Transparent);
	tempGraphics.DrawString(s, font, Brushes.White, 0, 0, format);

	int rightmost = ScanRightmostPixel(tempBitmap, tempSize, measuredHeight);
	pixelWidths[i] = rightmost + 1;

	// MeasureString's GenericTypographic advance is unreliable for slanted (italic) glyphs:
	// it can come out smaller than the glyph's actual rendered ink width, which would make
	// the next character's quad start before this one ends. Never advance less than the ink width.
	advanceWidths[i] = minGap + Math.Max(measuredAdvance, pixelWidths[i]);

	maxPixelWidth = Math.Max(maxPixelWidth, Math.Max(pixelWidths[i], advanceWidths[i]));
	maxHeight = Math.Max(maxHeight, measuredHeight);
}

// Cell width: next multiple of 16 >= max needed width
int cellWidth = ((maxPixelWidth + 15) / 16) * 16;
int cellHeight = maxHeight;

// Atlas layout: 16 columns
int columns = 16;
int rows = (charCount + columns - 1) / columns;
int atlasWidth = columns * cellWidth;
int atlasHeight = rows * cellHeight;

// Phase 2: Render the atlas
using var atlas = new Bitmap(atlasWidth, atlasHeight, PixelFormat.Format32bppArgb);
using var g = Graphics.FromImage(atlas);
g.Clear(Color.Transparent);
g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
g.SmoothingMode = SmoothingMode.HighQuality;

for (int i = 0; i < charCount; i++)
{
	char c = (char)(FirstChar + i);
	int col = i % columns;
	int row = i / columns;
	float x = col * cellWidth;
	float y = row * cellHeight;

	g.DrawString(c.ToString(), font, Brushes.White, x, y, format);
}

atlas.Save(outputPng, ImageFormat.Png);

// Phase 3: Write metrics
var metrics = new
{
	fontFile = Path.GetFileName(fontPath),
	fontFamily = fontFamily.Name,
	fontSize,
	firstCharCode = FirstChar,
	lastCharCode = LastChar,
	charCount,
	cellWidth,
	cellHeight,
	atlasColumns = columns,
	atlasRows = rows,
	atlasWidth,
	atlasHeight,
	characters = Enumerable.Range(0, charCount).Select(i => new
	{
		charCode = FirstChar + i,
		character = ((char)(FirstChar + i)).ToString(),
		advanceWidth = advanceWidths[i],
		pixelWidth = pixelWidths[i]
	}).ToArray()
};

var json = JsonSerializer.Serialize(metrics, new JsonSerializerOptions { WriteIndented = true });
File.WriteAllText(outputMetrics, json);

Console.WriteLine($"Font: {fontFamily.Name}, {fontSize}px");
Console.WriteLine($"Atlas: {atlasWidth}x{atlasHeight} ({columns}x{rows} cells of {cellWidth}x{cellHeight})");
Console.WriteLine($"Characters: {charCount} (U+{FirstChar:X4} - U+{LastChar:X4})");
Console.WriteLine($"PNG: {outputPng}");
Console.WriteLine($"Metrics: {outputMetrics}");

static int ScanRightmostPixel(Bitmap bitmap, int maxX, int maxY)
{
	var bd = bitmap.LockBits(new Rectangle(0, 0, maxX, maxY), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

	try
	{
		int rightmost = 0;
		int stride = bd.Stride;

		for (int y = 0; y < maxY; y++)
		{
			for (int x = maxX - 1; x > rightmost; x--)
			{
				byte alpha = Marshal.ReadByte(bd.Scan0, y * stride + x * 4 + 3);

				if (alpha > 0)
				{
					rightmost = x;
					break;
				}
			}
		}

		return rightmost;
	}
	finally
	{
		bitmap.UnlockBits(bd);
	}
}
