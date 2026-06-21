using System.Numerics;

namespace Amber.Renderer.Common;

public interface ICamera3D
{
    Vector3 Position { get; set; }
    Vector3 Direction { get; }
    Vector3 Up { get; set; }

    float Yaw { get; set; }

    float Pitch { get; set; }

    void MoveForward(float distance);

    void MoveRight(float distance);

    void MoveUp(float distance);

    void Walk(float distance);

    void Strafe(float distance);

    void Turn(float degrees);
}
