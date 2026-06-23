using Amber.Common;

namespace AmberIsland.GameData;

public readonly record struct Animation
(
    Position FirstFrameOffset, // Inside the atlas
    Size FrameSize,
    uint FrameCount,
    // If null, animation has no directions.
    // If given, per different direction (up, right, left in that order)
    // the offset is added (1 to 3 times) to FirstFrameOffset to get the
    // start of the animation for that direction.
    Position? DirectionOffset
);
