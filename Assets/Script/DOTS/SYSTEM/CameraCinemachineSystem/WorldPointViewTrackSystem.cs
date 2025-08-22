using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
public partial class WorldPointViewTrackSystem : SystemBase
{
    private Entity playerEntity;
    protected override void OnCreate()
    {
    }
    protected override void OnStartRunning()
    {
        playerEntity = SystemAPI.GetSingletonEntity<PlayerTag>();
    }
    protected override void OnUpdate()
    {
        LocalTransform localTransform = SystemAPI.GetComponent<LocalTransform>(playerEntity);
        WorldPointView.Instance.transform.position = math.lerp(WorldPointView.Instance.transform.position, localTransform.Position, SystemAPI.Time.DeltaTime);
        WorldPointView.Instance.transform.rotation = math.slerp(WorldPointView.Instance.transform.rotation, localTransform.Rotation, SystemAPI.Time.DeltaTime);
    }
    protected override void OnStopRunning()
    {
        base.OnStopRunning();
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
}
