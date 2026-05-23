using Unity.Entities;

/// <summary>
/// Gatling spin state. Chỉ gắn trên weapon entity có firingPattern = Gatling.
/// GatlingSpinSystem xử lý spin up/down logic.
/// </summary>
public struct GatlingSpin : IComponentData
{
    public float gatlingRotationSpeed;
    public float currentGatlingRotation;
    public float gatlingRotationSpeedChange;
    public float accumulatedGatlingAngle;

    // Audio entity cho gatling spin sound
    public Entity audioGatlingEffect;
}
