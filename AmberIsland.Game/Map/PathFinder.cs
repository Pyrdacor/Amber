using Amber.Common;

namespace AmberIsland.Game.Map;

public sealed class PathFinder(int width, int height, Func<Position, bool> checkWalkable)
{
    private const int CostStraight = 10;
    private const int CostDiagonal = 14;

    private static readonly Position[] Neighbors =
    [
        new(-1, -1), new(0, -1), new(1, -1),
        new(-1,  0),             new(1,  0),
        new(-1,  1), new(0,  1), new(1,  1)
    ];

    public List<Position>? FindPath(Position start, Position goal)
    {
        var open = new PriorityQueue<Position, int>();
        var cameFrom = new Dictionary<Position, Position>();
        var gScore = new Dictionary<Position, int> { [start] = 0 };

        open.Enqueue(start, Heuristic(start, goal));

        while (open.Count > 0)
        {
            var current = open.Dequeue();

            if (current == goal)
                return Reconstruct(cameFrom, current);

            foreach (var offset in Neighbors)
            {
                var next = current + offset;

                if (!InBounds(next) || !checkWalkable(next))
                    continue;

                // Avoid cutting corners when moving diagonally
                bool diagonal = offset.X != 0 && offset.Y != 0;
                if (diagonal &&
                    (!checkWalkable(new Position(current.X + offset.X, current.Y)) || !checkWalkable(new Position(current.X, current.Y + offset.Y))))
                    continue;

                int moveCost = diagonal ? CostDiagonal : CostStraight;
                int tentative = gScore[current] + moveCost;

                if (!gScore.TryGetValue(next, out int existing) || tentative < existing)
                {
                    cameFrom[next] = current;
                    gScore[next] = tentative;
                    open.Enqueue(next, tentative + Heuristic(next, goal));
                }
            }
        }

        return null; // no path
    }

    private static int Heuristic(Position a, Position b)
    {
        int dx = Math.Abs(a.X - b.X);
        int dy = Math.Abs(a.Y - b.Y);

        // Straight moves cost 10, diagonal moves cost 14 (≈ 10·√2)
        return CostStraight * (dx + dy) - (2 * CostStraight - CostDiagonal) * Math.Min(dx, dy);
    }

    private bool InBounds(Position p)
        => p.X >= 0 && p.X < width && p.Y >= 0 && p.Y < height;

    private List<Position> Reconstruct(
        Dictionary<Position, Position> cameFrom, Position current)
    {
        var path = new List<Position> { current };

        while (cameFrom.TryGetValue(current, out var prev))
        {
            current = prev;
            path.Add(current);
        }

        path.Reverse();

        return SmoothPath(path);
    }

    private List<Position> SmoothPath(List<Position> path)
    {
        if (path.Count <= 2)
            return path;

        var result = new List<Position> { path[0] };
        int anchor = 0;

        for (int i = 2; i < path.Count; i++)
        {
            // If the anchor can't see path[i], then path[i-1] was necessary.
            if (!HasLineOfSight(path[anchor], path[i]))
            {
                result.Add(path[i - 1]);
                anchor = i - 1;
            }
        }

        result.Add(path[^1]);
        return result;
    }

    private bool HasLineOfSight(Position a, Position b)
    {
        int dx = Math.Abs(b.X - a.X);
        int dy = Math.Abs(b.Y - a.Y);
        int x = a.X, y = a.Y;
        int stepX = a.X < b.X ? 1 : -1;
        int stepY = a.Y < b.Y ? 1 : -1;
        int error = dx - dy;

        while (true)
        {
            if (!checkWalkable(new Position(x, y)))
                return false;

            if (x == b.X && y == b.Y)
                return true;

            int e2 = 2 * error;
            // Diagonal step: also verify we don't slip through a wall corner,
            // matching the corner rule your A* uses.
            if (e2 > -dy && e2 < dx)
            {
                if (!checkWalkable(new Position(x + stepX, y)) ||
                    !checkWalkable(new Position(x, y + stepY)))
                    return false;
            }

            if (e2 > -dy) { error -= dy; x += stepX; }
            if (e2 < dx) { error += dx; y += stepY; }
        }
    }
}