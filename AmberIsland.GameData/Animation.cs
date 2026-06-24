using Amber.Common;
using Amber.IO.Common.Serialization;

namespace AmberIsland.GameData;

public readonly record struct Animation
(
    Size FrameSize,
    uint[] FrameIndices,
    ushort FramesPerMinute,
    // If null, animation has no directions.
    // If given, per different direction (up, right, left in that order)
    // the offset is added (1 to 3 times) to FirstFrameOffset to get the
    // start of the animation for that direction.
    Position? DirectionOffset
)
{
    public void Write(IDataWriter writer)
    {
        writer.Write((ushort)FrameSize.Width);
        writer.Write((ushort)FrameSize.Height);
        writer.Write(FramesPerMinute);

        // A null offset is stored as (0, 0).
        var offset = DirectionOffset ?? Position.Zero;
        writer.Write((ushort)offset.X);
        writer.Write((ushort)offset.Y);

        writer.Write((ushort)FrameIndices.Length);

        foreach (var index in FrameIndices)
            writer.Write((ushort)index);
    }

    public static Animation Read(IDataReader reader)
    {
        ushort width = reader.ReadWord();
        ushort height = reader.ReadWord();
        ushort framesPerMinute = reader.ReadWord();

        ushort offsetX = reader.ReadWord();
        ushort offsetY = reader.ReadWord();
        // (0, 0) means "no directions".
        Position? directionOffset = offsetX == 0 && offsetY == 0
            ? null
            : new Position(offsetX, offsetY);

        int frameCount = reader.ReadWord();
        var frameIndices = new uint[frameCount];

        for (int i = 0; i < frameCount; i++)
            frameIndices[i] = reader.ReadWord();

        return new Animation(new Size(width, height), frameIndices, framesPerMinute, directionOffset);
    }
}
