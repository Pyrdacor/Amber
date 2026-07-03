namespace AmberIsland.GameData;

public enum PlayerState : byte
{
    Idle,
    IdleWithWeapon,
    Walking,
    Running,
    Pushing,
    Pulling,
    Jumping,
    SwingingForth,
    SwingingBack,
    Stabbing,
    ShieldAttacking,
    ReceivingDamage,
    Dodging,
    Blocking, // crouch
    JumpingForward, // lunge
    Die,
}
