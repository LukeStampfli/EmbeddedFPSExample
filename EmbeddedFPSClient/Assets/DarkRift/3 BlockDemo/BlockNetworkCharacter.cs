using UnityEngine;

/// <summary>
///     Manages the movement of another player's character.
/// </summary>
internal class BlockNetworkCharacter : MonoBehaviour
{
    /// <summary>
    ///     The speed to lerp the player's position.
    /// </summary>
    [SerializeField]
    [Tooltip("The speed to lerp the player's position")]
    public float moveLerpSpeed = 10f;

    /// <summary>
    ///     The speed to lerp the player's rotation.
    /// </summary>
    [SerializeField]
    [Tooltip("The speed to lerp the player's rotation")]
    public float rotateLerpSpeed = 50f;

    /// <summary>
    ///     The position to lerp to.
    /// </summary>
    public Vector3 NewPosition { get; set; }

    /// <summary>
    ///     The rotation to lerp to.
    /// </summary>
    public Vector3 NewRotation { get; set; }

    void Awake()
    {
        //Set initial values
        NewPosition = transform.position;
        NewRotation = transform.eulerAngles;
    }

    void Update()
    {
        //Move and rotate to new values
        transform.position = Vector3.Lerp(transform.position, NewPosition, Time.deltaTime * moveLerpSpeed);
        transform.eulerAngles = new Vector3(
            Mathf.LerpAngle(transform.eulerAngles.x, NewRotation.x, Time.deltaTime * rotateLerpSpeed),
            Mathf.LerpAngle(transform.eulerAngles.y, NewRotation.y, Time.deltaTime * rotateLerpSpeed),
            Mathf.LerpAngle(transform.eulerAngles.z, NewRotation.z, Time.deltaTime * rotateLerpSpeed)
        );
    }
}
    
