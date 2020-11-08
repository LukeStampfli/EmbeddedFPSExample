using UnityEngine;

public class PlayerInterpolation : MonoBehaviour
{
    private float lastInputTime;

    public PlayerStateData CurrentData { get; set; }
    public PlayerStateData PreviousData { get; private set; }

    public void SetFramePosition(PlayerStateData data)
    {
        RefreshToPosition(data, CurrentData);
    }

    public void RefreshToPosition(PlayerStateData data, PlayerStateData prevData)
    {
        PreviousData = prevData;
        CurrentData = data;
        lastInputTime = Time.fixedTime;
    }

    public void Update()
    {
        float timeSinceLastInput = Time.time - lastInputTime;
        float t = timeSinceLastInput / Time.fixedDeltaTime;
        transform.position = Vector3.LerpUnclamped(PreviousData.Position, CurrentData.Position, t);
        transform.rotation = Quaternion.SlerpUnclamped(PreviousData.LookDirection,CurrentData.LookDirection, t);
    }
}

