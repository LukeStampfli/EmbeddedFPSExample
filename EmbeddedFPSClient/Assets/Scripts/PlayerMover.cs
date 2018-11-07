using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMover : MonoBehaviour
{
    public PlayerUpdateData CurrentData;
    private PlayerUpdateData previousData;
    private float lastInputTime;

    public void SetFramePosition(PlayerUpdateData data)
    {
        RefreshToPosition(data, CurrentData);
    }

    public void RefreshToPosition(PlayerUpdateData data, PlayerUpdateData prevData)
    {
        previousData = prevData;
        CurrentData = data;
        lastInputTime = Time.fixedTime;
    }

    public void Update()
    {
        float timeSinceLastInput = Time.time - lastInputTime;
        float t = timeSinceLastInput / Time.fixedDeltaTime;
        transform.position = Vector3.LerpUnclamped(previousData.Position, CurrentData.Position, t);
        transform.rotation = Quaternion.SlerpUnclamped(previousData.LookDirection,CurrentData.LookDirection, t);
    }

}

