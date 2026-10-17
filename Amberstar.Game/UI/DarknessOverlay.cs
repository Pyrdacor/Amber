using Amber.Common;
using Amber.Renderer.Common;

namespace Amberstar.Game.UI;

/// <summary>
/// Darkens an area of the screen except for a light circle (the night circle of 2D maps).
/// Each pixel row is darkened left and right of the circle. Rows with the same darkened
/// parts are drawn as one rectangle.
/// </summary>
internal sealed class DarknessOverlay(Game game, Rect area, byte displayLayer)
{
	readonly List<IColoredRect> rects = [];
	Position? shownCenter;
	int shownRadius = -1;
	bool visible = true;

	/// <summary>
	/// Shows the darkness with a light circle. Without a center the whole area is dark.
	/// </summary>
	public void Show(Position? center, int radius)
	{
		if (shownRadius == radius && shownCenter == center && rects.Count != 0)
			return;

		shownCenter = center;
		shownRadius = radius;

		var spans = new List<(int Y, int Height, int LeftWidth, int RightX)>();

		for (int y = area.Top; y < area.Bottom; y++)
		{
			int leftWidth = area.Size.Width;
			int rightX = area.Right;

			if (center != null)
			{
				double dy = y + 0.5 - center.Value.Y;

				if (Math.Abs(dy) < radius)
				{
					int halfWidth = (int)Math.Round(Math.Sqrt(radius * radius - dy * dy));
					int left = Math.Clamp(center.Value.X - halfWidth, area.Left, area.Right);
					rightX = Math.Clamp(center.Value.X + halfWidth, area.Left, area.Right);
					leftWidth = left - area.Left;
				}
			}

			if (spans.Count != 0 && spans[^1].LeftWidth == leftWidth && spans[^1].RightX == rightX)
				spans[^1] = spans[^1] with { Height = spans[^1].Height + 1 };
			else
				spans.Add((y, 1, leftWidth, rightX));
		}

		int rectIndex = 0;

		void AddRect(int x, int y, int width, int height)
		{
			if (width <= 0 || height <= 0)
				return;

			IColoredRect rect;

			if (rectIndex < rects.Count)
				rect = rects[rectIndex];
			else
			{
				rect = game.CreateColoredRect(Layer.UI, new(x, y), new(width, height), Color.Black)!;
				rect.DisplayLayer = displayLayer;
				rects.Add(rect);
			}

			rect.Position = new(x, y);
			rect.Size = new(width, height);
			rect.Visible = visible;
			rectIndex++;
		}

		foreach (var (y, height, leftWidth, rightX) in spans)
		{
			AddRect(area.Left, y, leftWidth, height);

			if (rightX > area.Left + leftWidth)
				AddRect(rightX, y, area.Right - rightX, height);
		}

		for (int i = rects.Count - 1; i >= rectIndex; i--)
		{
			rects[i].Visible = false;
			rects.RemoveAt(i);
		}
	}

	/// <summary>
	/// Removes the darkness (full light).
	/// </summary>
	public void Hide()
	{
		rects.ForEach(rect => rect.Visible = false);
		rects.Clear();
		shownCenter = null;
		shownRadius = -1;
	}

	/// <summary>
	/// Hides the darkness temporarily (e.g. while another screen is shown).
	/// </summary>
	public bool Visible
	{
		get => visible;
		set
		{
			visible = value;
			rects.ForEach(rect => rect.Visible = value);
		}
	}

	internal int ScriptRadius => shownRadius;

	internal bool ScriptShown => rects.Count != 0;
}
