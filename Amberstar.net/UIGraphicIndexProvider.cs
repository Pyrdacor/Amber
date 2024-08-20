﻿using Amberstar.Game;
using Amberstar.GameData.Serialization;

namespace Amberstar.net
{
	internal class UIGraphicIndexProvider
	(
		int buttonOffset,
		int statusIconOffset,
		int uiGraphicOffset,
		int image80x80Offset,
		int itemGraphicOffset
	) : IUIGraphicIndexProvider
	{
		public int Get80x80ImageIndex(Image80x80 image) => image80x80Offset + (int)image;

		public int GetButtonIndex(Button button) => buttonOffset + (int)button;

		public int GetItemGraphicIndex(ItemGraphic graphic) => itemGraphicOffset + (int)graphic;

		public int GetStatusIconIndex(StatusIcon statusIcon) => statusIconOffset + (int)statusIcon;

		public int GetUIGraphicIndex(UIGraphic graphic) => uiGraphicOffset + (int)graphic;
	}
}