using Amber.Common;

namespace AmberIsland.Game.Map;

internal sealed class PathFollower(List<Position> tilePath, int tileWidth, int tileHeight, float speed, Position endPosition)
{
    private readonly List<Vector> waypoints = tilePath
            .Skip(1)
            .Take(tilePath.Count - 2)
            .Select(p => new Vector(
                p.X * tileWidth + tileWidth / 2f,
                p.Y * tileHeight + tileHeight / 2f))
            .Concat([new Vector(endPosition)])
            .ToList();
    private int index;

    public bool Finished => index >= waypoints.Count;

    public float Speed { get; set; } = speed;

    public Vector Update(Vector currentPos, float deltaSeconds, ref Vector direction)
    {
        if (Finished)
            return currentPos;

        Vector target = waypoints[index];
        Vector toTarget = target - currentPos;
        float distance = toTarget.Length();
        float move = Speed * deltaSeconds;

        if (distance <= move)
        {
            index++; // reached this waypoint, go to next
            return target;
        }

        direction = toTarget / distance;

        return currentPos + direction * move; // normalized step
    }
}