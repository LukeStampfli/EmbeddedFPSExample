using System.Collections;
using System.Collections.Generic;
using DarkRift;
using UnityEngine;

[RequireComponent(typeof(PlayerLogic))]
[RequireComponent(typeof(PlayerMover))]
public class ClientPlayer : MonoBehaviour
{
    public ushort Id;
    public string Name;
    public bool IsOwn;

    public PlayerMover Mover;
    public PlayerLogic Logic;

    public void Initialize(ushort id, string name)
    {
        Id = id;
        Name = name;
        if (GlobalManager.Instance.PlayerId == id)
        {
            IsOwn = true;
            Camera.main.transform.SetParent(transform);
            Camera.main.transform.localPosition = new Vector3(0,1,0);
            Camera.main.transform.localRotation = Quaternion.identity;
        }
    }


    public void FixedUpdate()
    {
        if (IsOwn)
        {
            bool[]inputs = new bool[5];
            inputs[0] = Input.GetKey(KeyCode.W);
            inputs[1] = Input.GetKey(KeyCode.A);
            inputs[2] = Input.GetKey(KeyCode.S);
            inputs[3] = Input.GetKey(KeyCode.D);
            inputs[4] = Input.GetKey(KeyCode.Space);

            PlayerInputData inputData = new PlayerInputData(inputs, Quaternion.identity);

            using (Message m = Message.Create((ushort)Tags.GamePlayerInput, inputData))
            {
                GlobalManager.Instance.Client.SendMessage(m, SendMode.Reliable);
            }

            PlayerUpdateData updateData = Logic.GetNextFrameData(inputData,Mover.CurrentData);
            Mover.SetFramePosition(updateData);
        }
    }
}

