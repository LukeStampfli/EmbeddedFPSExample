using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLogic : MonoBehaviour
{
    public CharacterController CharacterController;
    public float WalkSpeed;
    public float GravityConstant;
    public float JumpStrenght;
    private Vector3 gravity;

    public PlayerUpdateData GetNextFrameData(PlayerInputData input, PlayerUpdateData currentUpdateData)
    {
        bool w = input.Keyinputs[0];
        bool a = input.Keyinputs[1];
        bool s = input.Keyinputs[2];
        bool d = input.Keyinputs[3];
        bool space = input.Keyinputs[4];
        bool left = input.Keyinputs[5];

        Vector3 nextrotation = currentUpdateData.LookDirection + input.LookDirectionDelta;
        gravity = new Vector3(0,currentUpdateData.Gravity,0);

        float rotation = nextrotation.y;

        Vector3 movement = Vector3.zero;
        if (w)
        {
            movement += Vector3.forward;
        }
        if (a)
        {
            movement += Vector3.left;
        }
        if (s)
        {
            movement += Vector3.back;
        }
        if (d)
        {
            movement += Vector3.right;
        }

        movement = Quaternion.Euler(0, rotation, 0) * movement;
        movement.Normalize();
        movement = movement * WalkSpeed;

        movement = movement * Time.fixedDeltaTime;
        CharacterController.Move(new Vector3(0,-0.001f,0));
        if (CharacterController.isGrounded)
        {
            if (space)
            {
                gravity = new Vector3(0, JumpStrenght, 0);
            }
        }
        else
        {
            gravity -= new Vector3(0, GravityConstant, 0);
        }

        movement = movement + gravity * Time.fixedDeltaTime;
        CharacterController.Move(movement);

        return new PlayerUpdateData(currentUpdateData.Id,gravity.y, transform.position, nextrotation);
    }

}
