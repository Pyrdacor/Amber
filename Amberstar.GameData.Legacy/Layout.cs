using Amber.Assets.Common;

namespace Amberstar.GameData.Legacy;

internal class Layout
{
    public Layout(byte[] definition, Dictionary<int, Graphic> layoutBlocks,
			List<word> bottomCorners, List<word> bottomCornerMasks)
    {
        var graphic = new Graphic(320, 163, true);
			int line = 0;
        int x = 0;
        int y = 0;
        int defIndex = 0;

        void ProcessLine()
        {
            int offsetY = line == 0 ? 4 : 0;
            int height = line == 0 ? 12 : line == 10 ? 7 : 16;

            for (int i = 0; i < 20; i++)
            {
                var block = layoutBlocks[definition[defIndex++] - 1];

                if (height != 16)
                    block = block.GetPart(0, offsetY, 16, height);

                graphic.AddOverlay(x, y, block);
                x += 16;
            }

				x = 0;
				y += height;
            line++;
        }

        for (int i = 0; i < 11; i++)
            ProcessLine();

        // Add lower corners
        int maskIndex = 0;
        int cornerIndex = 0;
        // Lower left corner
        for (int by = 0; by < 16; by++)
        {
            graphic.ApplyBitMaskedPlanarValues
            (
                0, 184 + by - 37, // x, y
                [bottomCornerMasks[maskIndex], bottomCornerMasks[maskIndex], bottomCornerMasks[maskIndex], bottomCornerMasks[maskIndex++]], // same mask for all planes
                [bottomCorners[cornerIndex++], bottomCorners[cornerIndex++], bottomCorners[cornerIndex++], bottomCorners[cornerIndex++]],
                4 // planes
            );
        }
			// Lower right corner
			for (int by = 0; by < 16; by++)
			{
				graphic.ApplyBitMaskedPlanarValues
				(
					304, 184 + by - 37, // x, y
					[bottomCornerMasks[maskIndex], bottomCornerMasks[maskIndex], bottomCornerMasks[maskIndex], bottomCornerMasks[maskIndex++]], // same mask for all planes
					[bottomCorners[cornerIndex++], bottomCorners[cornerIndex++], bottomCorners[cornerIndex++], bottomCorners[cornerIndex++]],
					4 // planes
				);
			}

			Graphic = graphic;
		}

    public IGraphic Graphic { get; }
}
