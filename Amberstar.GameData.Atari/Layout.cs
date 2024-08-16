using Amber.Assets.Common;
using Amberstar.GameData.Legacy;

namespace Amberstar.GameData.Atari
{
	internal class Layout
	{
        private readonly word[] BottomCorners =
        [
			 0x0000, 0x8000, 0x0000, 0x0000, 0x0000, 0x8000, 0x0000, 0x0000,
             0x8000, 0x8000, 0x0000, 0x0000, 0x8000, 0x8000, 0x0000, 0x0000,
             0x8000, 0x8000, 0x0000, 0x0000, 0x0000, 0x0000, 0x8000, 0x0000,
			 0x0000, 0x0000, 0x8000, 0x0000, 0x0000, 0x0000, 0x8000, 0x0000,
             0x4000, 0x4000, 0x0000, 0x0000, 0x0000, 0x0000, 0x4000, 0x0000,
             0x2000, 0x2000, 0x0000, 0x0000, 0x0000, 0x0000, 0x2000, 0x0000,
			 0x0000, 0x0000, 0x1000, 0x0000, 0x0400, 0x0400, 0x0800, 0x0000,
			 0x0000, 0x0000, 0x0300, 0x0000, 0x0062, 0x0063, 0x0094, 0x0000,
             0x0001, 0x0001, 0x0000, 0x0000, 0x0000, 0x0000, 0x0001, 0x0000,
			 0x0000, 0x0000, 0x0001, 0x0000, 0x0000, 0x0000, 0x0001, 0x0000,
			 0x0000, 0x0000, 0x0001, 0x0000, 0x0000, 0x0000, 0x0001, 0x0000,
             0x0001, 0x0001, 0x0000, 0x0000, 0x0000, 0x0000, 0x0001, 0x0000,
             0x0002, 0x0002, 0x0000, 0x0000, 0x0000, 0x0000, 0x0002, 0x0000,
             0x0004, 0x0004, 0x0000, 0x0000, 0x0000, 0x0000, 0x0004, 0x0000,
			 0x0000, 0x0000, 0x0008, 0x0000, 0x0020, 0x0020, 0x0010, 0x0000,
             0x0080, 0x0080, 0x0040, 0x0000, 0x4c00, 0xcc00, 0x3300, 0x0000,
		];

        private readonly word[] BottomMasks =
        [
            0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,0x8000,
            0xc000,0xc000,0xe000,0xe000,0xf000,0xfc00,0xff00,0xfff7,
			0x0001,0x0001,0x0001,0x0001,0x0001,0x0001,0x0001,0x0001,
            0x0003,0x0003,0x0007,0x0007,0x000f,0x003f,0x00ff,0xffff,
        ];

        public Layout(byte[] definition, Dictionary<int, Graphic> layoutBlocks)
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
                    [BottomMasks[maskIndex], BottomMasks[maskIndex], BottomMasks[maskIndex], BottomMasks[maskIndex++]], // same mask for all planes
                    [BottomCorners[cornerIndex++], BottomCorners[cornerIndex++], BottomCorners[cornerIndex++], BottomCorners[cornerIndex++]],
                    4 // planes
                );
            }
			// Lower right corner
			for (int by = 0; by < 16; by++)
			{
				graphic.ApplyBitMaskedPlanarValues
				(
					304, 184 + by - 37, // x, y
					[BottomMasks[maskIndex], BottomMasks[maskIndex], BottomMasks[maskIndex], BottomMasks[maskIndex++]], // same mask for all planes
					[BottomCorners[cornerIndex++], BottomCorners[cornerIndex++], BottomCorners[cornerIndex++], BottomCorners[cornerIndex++]],
					4 // planes
				);
			}

			Graphic = graphic;
		}

        public IGraphic Graphic { get; }
	}
}
