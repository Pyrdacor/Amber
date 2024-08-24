using Amber.Assets.Common;
using Amber.Common;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy
{
	internal class FontLoader(Amber.Assets.Common.IAssetProvider assetProvider) : IFontLoader
	{
		Font? font;

		public IFont LoadFont()
		{
			if (font != null)
				return font;

			var asset = assetProvider.GetAsset(new(AssetType.Font, 0));

			if (asset == null)
				throw new AmberException(ExceptionScope.Data, $"Font was not found.");

			var reader = asset.GetReader();

			var textConversionTable = reader.ReadBytes(224);
			var runeConversionTable = reader.ReadBytes(224);
			int totalGlyphDataSize = 290 + 5 + 150; // 5 bytes per glyph 8x5 pixels
			int sizePerGlyph = 5; // 5 bytes
			int glyphCount = totalGlyphDataSize / sizePerGlyph;
			var glyphGraphics = new Dictionary<int, IGraphic>(glyphCount);

			for (int i = 0; i < glyphCount; i++)
				glyphGraphics.Add(i, Graphic.FromBitPlanes(8, 5, reader.ReadBytes(sizePerGlyph), 1));

			reader.AlignToWord();
			var colorTable = reader.ReadToEnd(); // not needed

			return font = new Font(textConversionTable, runeConversionTable, glyphGraphics);
		}
	}
}
