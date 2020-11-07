using System.Collections.Generic;
using System.Linq;
using DarkRift;
using UnityEngine;
using UnityEngine.UI;

struct ReconciliationInfo
{
    public ReconciliationInfo(uint frame, PlayerUpdateData data, PlayerInputData input)
    {
        Frame = frame;
        Data = data;
        Input = input;
    }

    public uint Frame;
    public PlayerUpdateData Data;
    public PlayerInputData Input;
}

[RequireComponent(typeof(PlayerLogic))]
[RequireComponent(typeof(PlayerInterpolation))]
public class ClientPlayer : MonoBehaviour
{

    [Header("Public Fields")]
    public ushort Id;
    public string Name;
    public bool IsOwn;
    public int Health;

    [Header("Variables")]
    public float SensitivityX;
    public float SensitivityY;

    [Header("References")]
    public PlayerInterpolation Interpolation;
    public PlayerLogic Logic;
    public Text NameText;
    public Image HealthBarFill;
    public GameObject HealthBarObject;

    [Header("Prefabs")]
    public GameObject ShotPrefab;

    private Queue<PlayerUpdateData> updateBuffer = new Queue<PlayerUpdateData>();
    private Queue<ReconciliationInfo> reconciliationHistory = new Queue<ReconciliationInfo>();

    private float yaw;
    private float pitch;

    public void Initialize(ushort id, string name)
    {
        Id = id;
        Name = name;
        NameText.text = Name;
        SetHealth(100);
        if (ConnectionManager.Instance.PlayerId == id)
        {
            IsOwn = true;
            Camera.main.transform.SetParent(transform);
            Camera.main.transform.localPosition = new Vector3(0,0,0);
            Camera.main.transform.localRotation = Quaternion.identity;
            Interpolation.CurrentData = new PlayerUpdateData(Id,0, Vector3.zero, Quaternion.identity);
        }
    }

    public void SetHealth(int value)
    {
        Health = value;
        HealthBarFill.fillAmount = value / 100f;
    }

    void LateUpdate()
    {
        Vector3 point = Camera.main.WorldToScreenPoint(transform.position + new Vector3(0, 1, 0));
        if (point.z > 2)
        {
            HealthBarObject.transform.position = point;
        }
        else
        {
            HealthBarObject.transform.position = new Vector3(10000,0,0);
        }
    }

    void FixedUpdate()
    {
        if (IsOwn)
        {
           
            bool[]inputs = new bool[6];
            inputs[0] = Input.GetKey(KeyCode.W);
            inputs[1] = Input.GetKey(KeyCode.A);
            inputs[2] = Input.GetKey(KeyCode.S);
            inputs[3] = Input.GetKey(KeyCode.D);
            inputs[4] = Input.GetKey(KeyCode.Space);
            inputs[5] = Input.GetMouseButton(0);

            if (inputs[5])
            {
                GameObject go = Instantiate(ShotPrefab);
                go.transform.position = Interpolation.CurrentData.Position;
                go.transform.rotation = transform.rotation;
                Destroy(go,1f);
            }

            yaw += Input.GetAxis("Mouse X") * SensitivityX;
            pitch += Input.GetAxis("Mouse Y") * SensitivityY;

            Quaternion rot = Quaternion.Euler(pitch, yaw,0);

            PlayerInputData inputData = new PlayerInputData(inputs,rot, GameManager.Instance.LastRecievedServerTick-1);

            transform.position = Interpolation.CurrentData.Position;
            PlayerUpdateData updateData = Logic.GetNextFrameData(inputData,Interpolation.CurrentData);
            Interpolation.SetFramePosition(updateData);

            using (Message m = Message.Create((ushort)Tags.GamePlayerInput, inputData))
            {
                ConnectionManager.Instance.Client.SendMessage(m, SendMode.Reliable);
            }

            reconciliationHistory.Enqueue(new ReconciliationInfo(GameManager.Instance.ClientTick,updateData, inputData));
        }
    }

    public void OnServerDataUpdate(PlayerUpdateData data)
    {
        if (IsOwn)
        {
            while (reconciliationHistory.Any() && reconciliationHistory.Peek().Frame < GameManager.Instance.LastRecievedServerTick)
            {
                reconciliationHistory.Dequeue();
            }

            if (reconciliationHistory.Any() && reconciliationHistory.Peek().Frame == GameManager.Instance.LastRecievedServerTick)
            {
                ReconciliationInfo info = reconciliationHistory.Dequeue();
                if (Vector3.Distance(info.Data.Position, data.Position) > 0.05f)
                {

                    List<ReconciliationInfo> infos = reconciliationHistory.ToList();
                    Interpolation.CurrentData = data;
                    transform.position = data.Position;
                    transform.rotation = data.LookDirection;
                    for (int i = 0; i < infos.Count; i++)
                    {
                        PlayerUpdateData u = Logic.GetNextFrameData(infos[i].Input, Interpolation.CurrentData);
                        Interpolation.SetFramePosition(u);
                    }
                }
            }
        }
        else
        {
            Interpolation.SetFramePosition(data);
        }
    }
}

