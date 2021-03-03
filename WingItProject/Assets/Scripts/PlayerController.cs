using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Camera mainCam;
    public GameObject MouseMarker;

    [Header("Movement")]
    public bool isGrounded;
    public bool canMove;
    public bool moving;
    public bool jumped = false;
    [SerializeField] private float playerSpeed = 0f;
    [SerializeField] private float playerSprintSpeed = 0f;
    [SerializeField] private float sprintTransitionSpeed = 0.1f;
    [SerializeField] private float jumpHeight;
    [SerializeField, Range(0f, 1f)] private float desiredRotSpeed;
    [SerializeField, Range(0f, -50f)] private float gravity = 9.8f;
    [SerializeField] private Vector3 velocity;
    private Vector3 desiredMoveDirection;
    private float[] previousInput = { 0f, 0f };

    [Header("Dash")]
    public bool dashed;
    [SerializeField] private float dashSpeed;
    [SerializeField] private float dashTime;

    [Header("Attack")]
    [SerializeField] private Vector3 attackOrigin;
    [SerializeField] private float attackDistance;
    [SerializeField] private float attackArc;

    [Header("Animation")]
    public Animator anim;
    [SerializeField] private Vector2 idleActionRandTime = new Vector2(30, 60);
    private bool idleAction = false;
    private Coroutine idle;
    public float sprintTransTime = 0f;

    [Header("Audio")]
    public AudioSource audioFootsteps;

    public static PlayerController instance;

    [ExecuteInEditMode]
    void OnDrawGizmos()
    {
        // Draws the bounds of the attack in the editor based of the attack variables
        #region AttackGizmo

        // Sets the gizmo colour
        Gizmos.color = Color.blue;

        // Gets the current player position and adds on the attack origin vector to find position the attack will originate from
        Vector3 pos = transform.position + attackOrigin;

        // Draws two lines exteneding outwards based of the attack origin and the distance of the attack
        Gizmos.DrawLine(pos, pos + new Vector3(-attackArc, 0, attackDistance));
        Gizmos.DrawLine(pos, pos + new Vector3(attackArc, 0, attackDistance));

        // If the attack vectors are not perpendicular then a line is draw between them to represent the distance between
        if (attackArc != 0)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(pos + new Vector3(-attackArc, 0, attackDistance), pos + new Vector3(attackArc, 0, attackDistance));
        }

        #endregion
    }

    void Awake()
    {
        // Creates the static variable instance to the current player controller so that other scripts within the scene can reference the player easier
        instance = GetComponent<PlayerController>();
    }

    void Start()
    {
        // Sets the current camera to main in case a camera doesn't get assigned in the editor
        if (mainCam == null) { mainCam = Camera.main; }
    }
    
    void Update()
    {
        MouseMarkerPlacement();
        InputMagnitude();
        Gravity();
        FootstepAudio();
    }
    
    void MouseMarkerPlacement()
    {
        
        RaycastHit hit; 
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

        if(Physics.Raycast (ray, out hit, Mathf.Infinity))
        {          
            Vector3 markerPos = hit.point;
            if(hit.collider.tag == "Ground") 
            {
                MouseMarker.transform.position = new Vector3(markerPos.x, 0.1f, markerPos.z);               
            } 
        }
        
    }


    // Adds the force of the gravity variable over time to the vertical axis of the player so the get push down if in mid air
    void Gravity()
    {
        // isGrounded is set to the character controller is grounded variable
        isGrounded = characterController.isGrounded;
        
        // if the player is not grounded increase the vertical velocity
        if (!isGrounded) { velocity.y += gravity * Time.deltaTime; }

        // if the player is grounded reset the velocity
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = 0f;

            anim.SetTrigger("isGrounded");
        }

        // applies the new velocity vector to the character controller
        characterController.Move(velocity * Time.deltaTime);
    }


    // Applies jump force to the player when called
    void Jump()
    {
        // Applies a positive force to the y of the velocity so that the player jumps and so the Gravity() method can add a downwards force over time
        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        anim.SetTrigger("jumped");
    }


    // Movement method - Applies movement to the player
    private void Move(Vector2 Input, float Speed, bool Dash)
    {
        /* "ClampMagnitude" acts the similar to a ".normalized except it just clamps the values so diagonal movement can never exceed 1"
         * 
         * To get the forward direction in relation to the camera angle, I use the custom "CameraTransform" Vector3 function to return the current
         * forward or right direction as a normalized vector with no value in the Y axis. This is quicker than writing this out multiple times
         * 
         * Lastly this directional value is mutiplied by a speed variable to change the pace the character moves at */
        desiredMoveDirection = (Vector3.ClampMagnitude(((CameraTransform("Forward") * Input.normalized.y) + (CameraTransform("Right") * Input.normalized.x)), 1f) * Speed);


        // Lerps from the current rotation to the direction the player is moving, by a speed variable to make the character face that direction
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(desiredMoveDirection), desiredRotSpeed);


        // Inputs the final movement vector to the character controller component
        if (!Dash) { characterController.Move(desiredMoveDirection * Time.deltaTime); }

        if (Dash) { characterController.Move(desiredMoveDirection * dashSpeed * Time.deltaTime); }
    }


    // Recieves the inputs that the player makes and acts upon them calling the coresponding methods
    void InputMagnitude()
    {
        // When the movement axis are activated the Move() method is called
        #region Movement

        // Get the input of the both the horizontal and vertical axes, based on keyboard / controller joystick input, to be stored as a Vector2
        Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        // This stops the player character rotating back to forward relative to the camera when the player isn't moving
        if (input.x != 0f || input.y != 0f) 
        {
            // Changes the move speed whether the player is sprinting or not
            float finalSpeed;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.Joystick1Button8)) // If the player is sprinting then set the correct move speed
            {
                finalSpeed = playerSprintSpeed;

                if (sprintTransTime < 0.999f)
                {
                    sprintTransTime = Mathf.Lerp(sprintTransTime, 1f, sprintTransitionSpeed); // Lerps between from the walk anim to the sprint anim
                } 
                else
                {
                    sprintTransTime = 1f;
                }
                
                anim.SetFloat("moveSpeed", sprintTransTime);
            }
            else // If the player is walking then set the correct move speed
            {
                finalSpeed = playerSpeed;

                if (sprintTransTime > 0.001f)
                {
                    sprintTransTime = Mathf.Lerp(sprintTransTime, 0f, sprintTransitionSpeed); // Lerps between from the sprint anim to the walk anim
                }
                else 
                {
                    sprintTransTime = 0f;
                }

                anim.SetFloat("moveSpeed", sprintTransTime);
            }
            
            // Stops the Idle Action coroutine if the player is moving
            if (idleAction) 
            { 
                StopCoroutine(idle);
                idleAction = false;
            }

            // Calls the Move() Method with the player inputs as a Vector2 and the final speed as a float
            Move(input, finalSpeed, false);

            // Sets the animator parameter "moving" to true and the script variable "moving" to true
            anim.SetBool("moving", true);
            moving = true;      
            
        }
        
        if (input.x == 0f && input.y == 0f)
        {
            // Sets the animator parameter "moving" to false and the script variable "moving" to false
            anim.SetBool("moving", false);
            moving = false;

            // Starts the Idle Action coroutine if the player is not moving
            if (!idleAction)
            {
                idle = StartCoroutine(TriggerIdle());
                idleAction = true;
            }
        }

        #endregion

        // When the dash button is pressed the Dash() coroutine is triggered
        if ((Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Joystick1Button2)) && !jumped && (input.x != 0f || input.y != 0f) && !dashed)
        {
            dashed = true;

            StartCoroutine(Dash(input));
        }

        // When the jump button is pressed down the Jump() method is called
        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Joystick1Button0)) && !jumped) 
        {
            jumped = true;

            Jump();
        }
    }


    // Gets called when the player makes no movement input to trigger the secondary idle animation
    IEnumerator TriggerIdle()
    {
        // Generates a random integer used as a variable for the wait time before the rest of the corountine can continue
        float time = Random.Range(idleActionRandTime.x, idleActionRandTime.y);
        yield return new WaitForSeconds(time);

        // Triggers the players' animator parameter "idleAction" to activate the secondary idle animation
        anim.SetTrigger("idleAction");

        // Sets idle action back to false so that the coroutine will be run again in the InputMagnitude() method
        idleAction = false;

        // Ends the coroutine
        yield break;
    }


    // Dash coroutine
    IEnumerator Dash(Vector2 Input)
    {
        // Gets the starting time of the dash
        float startTime = Time.time;

        anim.SetFloat("moveSpeed", 1f);

        // While the current time is smaller than the (start time + the dash time) then run the code within the while loop
        while (Time.time < startTime + dashTime)
        {
            /* Moves the character in the last faced direction and tells the Move() method that this 
               movement is currently a dash so it uses the dash speed rather than the default speed */
            Move(new Vector2(Input.x, Input.y), playerSpeed, true);

            // Forces the coroutine to wait until the next frame until it can run again. This makes the while loop act as a update while active
            yield return null;
        }

        // Tells the rest of the script that the dash has finished
        dashed = false;

        // Ends the coroutine
        yield break;
    }


    void FootstepAudio()
    {
        if ((!moving || jumped) && audioFootsteps.isPlaying)
        {
            audioFootsteps.Stop();
        }
        else if(moving && !audioFootsteps.isPlaying)
        {
            audioFootsteps.Play();
        }
    }


    // Used to get the normalized forward direction of the player based of the camera direction so that forward is always away from the player, etc...
    Vector3 CameraTransform(string direction)
    {
        if (direction == "Forward")
        { 
            Vector3 forward = mainCam.transform.forward;
            forward.y = 0f;
            forward.Normalize();
            return forward;
        }

        if (direction == "Right")
        {
            Vector3 right = mainCam.transform.right;
            right.y = 0f;
            right.Normalize();
            return right;
        }

        // Returns a blank vector in case an incorrect string is used within the script so that the game doesn't crash
        return Vector3.zero;
    }


    public void Damaged(float damage)
    {

    }

}