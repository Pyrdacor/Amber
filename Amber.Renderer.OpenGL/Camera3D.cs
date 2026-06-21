using System.Numerics;
using Amber.Renderer.Common;

namespace Amber.Renderer.OpenGL;

public class Camera3D : ICamera3D
{
    public Vector3 Position { get; set; }
    public Vector3 Direction { get; private set; }
    public Vector3 Up { get; set; } = new(0, 1, 0);

    private float yaw;
    private float pitch;

    public float Yaw
    {
        get => yaw;
        set
        {
            yaw = value;
            UpdateDirection();
        }
    }

    public float Pitch
    {
        get => pitch;
        set
        {
            pitch = Math.Clamp(value, -89f, 89f);
            UpdateDirection();
        }
    }

    public Camera3D(Vector3 position, float yaw = -90.0f, float pitch = 0.0f)
    {
        Position = position;
        this.yaw = yaw;
        this.pitch = pitch;
        UpdateDirection();
    }

    private void UpdateDirection()
    {
        float yawRad = yaw * MathF.PI / 180f;
        float pitchRad = pitch * MathF.PI / 180f;

        Direction = Vector3.Normalize(new(
            MathF.Cos(pitchRad) * MathF.Cos(yawRad),
            MathF.Sin(pitchRad),
            MathF.Cos(pitchRad) * MathF.Sin(yawRad)
        ));
    }

    public Matrix4 ViewMatrix => Matrix4.LookAt(Position, Position + Direction, Up);

    public void MoveForward(float distance)
    {
        Position += Direction * distance;
    }

    public void MoveRight(float distance)
    {
        var right = Vector3.Normalize(Vector3.Cross(Direction, Up));
        Position += right * distance;
    }

    public void MoveUp(float distance)
    {
        Position += Up * distance;
    }

    /// <summary>
    /// Move forward/backward only on the XZ plane (no flying).
    /// Useful for dungeon-style movement.
    /// </summary>
    public void Walk(float distance)
    {
        var forward = Vector3.Normalize(new(Direction.X, 0, Direction.Z));
        Position += forward * distance;
    }

    /// <summary>
    /// Strafe left/right on the XZ plane.
    /// </summary>
    public void Strafe(float distance)
    {
        var forward = Vector3.Normalize(new(Direction.X, 0, Direction.Z));
        var right = Vector3.Normalize(Vector3.Cross(forward, Up));
        Position += right * distance;
    }

    /// <summary>
    /// Turn left/right by the given degrees.
    /// </summary>
    public void Turn(float degrees)
    {
        Yaw += degrees;
    }
}