using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerContoller : MonoBehaviour
{
    //Settings
    public float movementSpeed = 2f;
    public float sprintSpeed = 2f;
    public float sensitivity = 15f;
    public float jumpForce = 20f;
    public LayerMask groundMask;

    [Space]
    public Transform cameraTransform;
    public Rigidbody body;

    //Variables
    bool grounded;

    bool inMenu = false;
    bool inBadChunk = false;
    float xRot;

    //Setup
    private void Start()
    {
        cameraTransform = transform.GetChild(0);
        body = GetComponent<Rigidbody>();

        GameplayManager.UpdateSettings += SensitvityUpdate;
        SensitvityUpdate();
    }

    private void Update()
    {
        ChunkCheck();
        PauseCheck();
        GroundCheck();

        if (inMenu)
            return;

        if (inBadChunk)
        {
            body.isKinematic = true;
            return;
        }
        else
            body.isKinematic = false;

        Movement();
        Rotate();
    }

    public bool isAbleToEdit()
    {
        return (!inMenu && !inBadChunk);
    }

    //Lock Player
    private void ChunkCheck()
    {
        if (!WorldGenerationManager.singleton.IsInsideLoadedChunk())
        {
            inBadChunk = true;
        }
        else
        {
            inBadChunk = false;
            WorldGenerationManager.singleton.UpdatePlayerPosition(transform.position);
        }
    }

    //Pause Check
    private void PauseCheck()
    {
        inMenu = UI_Manager.current.CurrentMenuIndex != 0;

        if (Input.GetKeyDown(KeyCode.Escape) && UI_Manager.current.CurrentMenuIndex == 0)
        {
            if(UI_Manager.current.CurrentMenuIndex == 0)
                UI_Manager.current.SetMenu(1);
            else
                UI_Manager.current.SetMenu(0);
        }
    }

    //Ground Check
    private void GroundCheck()
    {
        float rayDist = 1.1f;
        Debug.DrawLine(transform.position, transform.position + (Vector3.down * rayDist));

        if (Physics.SphereCast(transform.position, 0.2f, Vector3.down, out RaycastHit hit, rayDist, groundMask))
        {
            grounded = true;
        }
        else
        {
            grounded = false;
        }
    }

    //Movement
    private void Movement()
    {
        //Input
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputY = Input.GetAxisRaw("Vertical");
        Vector3 inputVector = new Vector3(inputX, 0, inputY).normalized;

        //Move Force
        Vector3 move = transform.TransformDirection(inputVector);
        if (Input.GetKey(KeyCode.LeftShift))
            move *= sprintSpeed;
        else
            move *= movementSpeed;

        move.y = body.velocity.y;

        //Jump
        if(grounded)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                body.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
        }

        //Set
        body.velocity = move;
    }

    //Rotation
    private void Rotate()
    {
        //Input
        float inputX = Input.GetAxisRaw("Mouse Y");
        float inputY = Input.GetAxisRaw("Mouse X");
        Vector2 inputVector = new Vector2(inputX, inputY);

        //X Rotation
        xRot -= inputVector.x * sensitivity * Time.deltaTime;
        xRot = Mathf.Clamp(xRot, -90f, 90f);

        //Y Rotation
        float yRot = inputVector.y * sensitivity * Time.deltaTime;

        //Set
        transform.Rotate(new Vector3(0, yRot, 0));
        cameraTransform.localRotation = Quaternion.Euler(xRot, 0, 0);
    }


    //Update Sensitivity
    private void SensitvityUpdate()
    {
        sensitivity = SettingsManager.MouseSensitivity * 100;
    }

    //Handle Events
    private void OnDestroy()
    {
        GameplayManager.UpdateSettings -= SensitvityUpdate;
    }
}
