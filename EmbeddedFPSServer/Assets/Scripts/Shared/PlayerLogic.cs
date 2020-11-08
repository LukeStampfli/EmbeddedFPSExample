using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerLogic : MonoBehaviour
{
    private Vector3 gravity;

    [Header("Settings")]
    [SerializeField]
    private float walkSpeed;
    [SerializeField]
    private float gravityConstant;
    [SerializeField]
    private float jumpStrength;

    public CharacterController CharacterController { get; private set; }

    void Awake()
    {
        CharacterController = GetComponent<CharacterController>();
    }

    public PlayerStateData GetNextFrameData(PlayerInputData input, PlayerStateData currentStateData)
    {
        bool w = input.Keyinputs[0];
        bool a = input.Keyinputs[1];
        bool s = input.Keyinputs[2];
        bool d = input.Keyinputs[3];
        bool space = input.Keyinputs[4];

        Vector3 rotation = input.LookDirection.eulerAngles;
        gravity = new Vector3(0, currentStateData.Gravity, 0);

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

        movement = Quaternion.Euler(0, rotation.y, 0) * movement; // Move towards the look direction.
        movement.Normalize();
        movement = movement * walkSpeed;

        movement = movement * Time.fixedDeltaTime;
        movement = movement + gravity * Time.fixedDeltaTime;

        // The following code fixes character controller issues from unity. It makes sure that the controller stays connected to the ground by adding a little bit of down movement.
        CharacterController.Move(new Vector3(0, -0.001f, 0));

        if (CharacterController.isGrounded)
        {
            if (space)
            {
                gravity = new Vector3(0, jumpStrength, 0);
            }
        }
        else
        {
            gravity -= new Vector3(0, gravityConstant, 0);
        }

        CharacterController.Move(movement);

        return new PlayerStateData(currentStateData.Id, gravity.y, transform.localPosition, input.LookDirection);
    }
}