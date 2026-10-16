using System.Numerics;
using Amber.Renderer.Common;

namespace Amber.Renderer.Headless;

internal sealed class Camera3D : ICamera3D
{
	float yaw = -90.0f;
	float pitch = 0.0f;

	public Camera3D()
	{
		UpdateDirection();
	}

	public Vector3 Position { get; set; } = new(5, 5, 0);
	public Vector3 Direction { get; private set; }
	public Vector3 Up { get; set; } = new(0, 1, 0);

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
			pitch = Math.Clamp(value, -89.0f, 89.0f);
			UpdateDirection();
		}
	}

	void UpdateDirection()
	{
		float yawRad = yaw * MathF.PI / 180.0f;
		float pitchRad = pitch * MathF.PI / 180.0f;

		Direction = Vector3.Normalize(new
		(
			MathF.Cos(pitchRad) * MathF.Cos(yawRad),
			MathF.Sin(pitchRad),
			MathF.Cos(pitchRad) * MathF.Sin(yawRad)
		));
	}

	public void MoveForward(float distance) => Position += Direction * distance;

	public void MoveRight(float distance) => Position += Vector3.Normalize(Vector3.Cross(Direction, Up)) * distance;

	public void MoveUp(float distance) => Position += Up * distance;

	public void Walk(float distance) => Position += Vector3.Normalize(new(Direction.X, 0, Direction.Z)) * distance;

	public void Strafe(float distance)
	{
		var forward = Vector3.Normalize(new(Direction.X, 0, Direction.Z));
		Position += Vector3.Normalize(Vector3.Cross(forward, Up)) * distance;
	}

	public void Turn(float degrees) => Yaw += degrees;
}
