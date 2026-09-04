using System.Numerics;

namespace Amber.Renderer.Common;

public interface ICamera3D
{
    Vector3 Position { get; set; }
    Vector3 Direction { get; }
    Vector3 Up { get; set; }

    float Yaw { get; set; }

    float Pitch { get; set; }

    /// <summary>
    /// Move forward/backward in the direction the camera is facing (including up/down if pitched).
    /// </summary>
    void MoveForward(float distance);

    /// <summary>
    /// Move right/left, perpendicular to the direction the camera is facing.
    /// </summary>
    void MoveRight(float distance);

    /// <summary>
    /// Move straight up/down, independent of the camera's facing direction.
    /// </summary>
    void MoveUp(float distance);

    /// <summary>
    /// Move forward/backward only on the XZ plane (no flying).
    /// Useful for dungeon-style movement.
    /// </summary>
    void Walk(float distance);

    /// <summary>
    /// Strafe left/right on the XZ plane.
    /// </summary>
    void Strafe(float distance);

    /// <summary>
    /// Turn left/right by the given degrees.
    /// </summary>
    void Turn(float degrees);
}
