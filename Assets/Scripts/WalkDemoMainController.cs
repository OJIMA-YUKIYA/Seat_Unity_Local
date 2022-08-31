using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkDemoMainController : MonoBehaviour
{
    public bool walkStraight;
    public bool walkLeft;
    public bool walkRight;
    public bool walkStop;
    public bool walkBack;

    [SerializeField]
    UDPWalkDemoforYaw UdpYaw;
    [SerializeField]
    SeatBaseController seatController;
    [SerializeField]
    LowerLimb6MotorBase lowerLimbMotionController;
    [SerializeField]
    SeatController seatController2;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow)){
            walkStraight = true;
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            walkLeft = true;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            walkRight = true;
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            walkStop = true;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            walkBack = true;
        }

        if (walkStraight)
        {
            seatController.WalkStraight();
            lowerLimbMotionController.WalkStraight();
            walkStraight = false;
        }
        if (walkBack)
        {
            seatController.WalkBack();
            lowerLimbMotionController.WalkBack();
            walkBack = false;
        }
        if (walkRight)
        {
            seatController.WalkRight();
            lowerLimbMotionController.WalkRight();
            walkRight = false;
        }
        if (walkLeft)
        {
            seatController.WalkLeft();
            lowerLimbMotionController.WalkLeft();
            walkLeft = false;
        }
        if (walkStop)
        {
            seatController.WalkStop();
            lowerLimbMotionController.WalkStop();
            walkStop = false;
        }
        //if (UdpYaw.command && seatController.command && lowerLimbMotionController.command )
        // if (seatController.command && lowerLimbMotionController.command)
         if (seatController.command && lowerLimbMotionController.command)
        {
            seatController.command = false;
            lowerLimbMotionController.command = false;
        }
    }

}
