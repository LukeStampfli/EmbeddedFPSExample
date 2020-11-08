using System.Collections.Generic;
using DarkRift;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Room : MonoBehaviour
{
    private Scene scene;
    private PhysicsScene physicsScene;

    [Header("Public Fields")]
    public string Name;
    public List<ClientConnection> ClientConnections = new List<ClientConnection>();
    public byte MaxSlots;

    public void Initialize(string name, byte maxslots)
    {
        Name = name;
        MaxSlots = maxslots;

        CreateSceneParameters csp = new CreateSceneParameters(LocalPhysicsMode.Physics3D);
        scene = SceneManager.CreateScene("Room_" + name, csp);
        physicsScene = scene.GetPhysicsScene();

        SceneManager.MoveGameObjectToScene(gameObject, scene);
    }
}
