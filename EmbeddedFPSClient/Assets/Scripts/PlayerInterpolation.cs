using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerInterpolation : MonoBehaviour
{
    [Header("Public Fields")]
    public PlayerUpdateData CurrentData;
    public PlayerUpdateData PreviousData;

    private float lastInputTime;

    public void SetFramePosition(PlayerUpdateData data)
    {
        RefreshToPosition(data, CurrentData);
    }

    public void RefreshToPosition(PlayerUpdateData data, PlayerUpdateData prevData)
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

