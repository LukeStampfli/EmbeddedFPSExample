using System.Collections.Generic;
using System.Linq;
using DarkRift;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerLogic))]
[RequireComponent(typeof(PlayerInterpolation))]
public class ClientPlayer : MonoBehaviour
{
    private PlayerLogic playerLogic;

    private PlayerInterpolation interpolation;

    // Store look direction.
    private float yaw;
    private float pitch;

    private ushort id;
    private string playerName;
    private bool isOwn;

    private int health;

    [Header("Settings")]
    [SerializeField]
    private float sensitivityX;
    [SerializeField]
    private float sensitivityY;

    void Awake()
    {
        playerLogic = GetComponent<PlayerLogic>();
        interpolation = GetComponent<PlayerInterpolation>();
    }

    void Start()
    {
        Camera.main.transform.SetParent(transform);
        Camera.main.transform.localPosition = new Vector3(0, 0, 0);
        Camera.main.transform.localRotation = Quaternion.identity;

        Interpolation.CurrentData = new PlayerUpdateData(Id, 0, Vector3.zero, Quaternion.identity);
    }

    void FixedUpdate()
    {
        bool[] inputs = new bool[6];
        inputs[0] = Input.GetKey(KeyCode.W);
        inputs[1] = Input.GetKey(KeyCode.A);
        inputs[2] = Input.GetKey(KeyCode.S);
        inputs[3] = Input.GetKey(KeyCode.D);
        inputs[4] = Input.GetKey(KeyCode.Space);
        inputs[5] = Input.GetMouseButton(0);

        yaw += Input.GetAxis("Mouse X") * sensitivityX;
        pitch += Input.GetAxis("Mouse Y") * sensitivityY;

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);

        PlayerInputData inputData = new PlayerInputData(inputs, rotation, GameManager.Instance.LastRecievedServerTick - 1);

        transform.position = interpolation.CurrentData.Position;
        PlayerStateData nextStateData = playerLogic.GetNextFrameData(inputData, interpolation.CurrentData);
        interpolation.SetFramePosition(nextStateData);

        using (Message message = Message.Create((ushort)Tags.GamePlayerInput, inputData))
        {
            ConnectionManager.Instance.Client.SendMessage(message, SendMode.Reliable);
        }
    }
}

