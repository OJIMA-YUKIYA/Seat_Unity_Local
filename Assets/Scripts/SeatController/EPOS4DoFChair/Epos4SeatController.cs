using System;
using System.Collections;
using System.Threading;
using EposCmd.Net;
using EposCmd.Net.DeviceCmdSet.Operation;
using UnityEngine;


public class Epos4SeatController : SeatBaseController
{
    [SerializeField] double lift = 1.26 * 3.0; // Unit mm
    [SerializeField] double roll = 0.15 * 6.0; // Unit deg
    [SerializeField] double pitch = 0.13 * 1.5; // Unit deg
    [SerializeField] int offsetMotor1 = 0; // Unit deg
    [SerializeField] int offsetMotor2 = 0; // Unit deg
    [SerializeField] int offsetMotor3 = 0; // Unit deg
    ProfilePositionMode ppm1;
    ProfilePositionMode ppm2;
    ProfilePositionMode ppm3;
    private Coroutine coroutine;

    void Start()
    {
        coroutine = StartCoroutine(MotorDriveAsync());
    }

    IEnumerator MotorDriveAsync()
    {
        MoveInit();
        MoveFirst();
        yield return new WaitForSeconds(3f);

    }

    // Update is called once per frame
    void Update()
    {
    }

    private DeviceManager connector;

    void MoveInit()
    {
        connector = new DeviceManager("EPOS4", "MAXON SERIAL V2", "USB", "USB0");
        var epos1 = connector.CreateDevice(1);
        var epos2 = connector.CreateDevice(2);
        var epos3 = connector.CreateDevice(3);
        ppm1 = epos1.Operation.ProfilePositionMode;
        ppm2 = epos2.Operation.ProfilePositionMode;
        ppm3 = epos3.Operation.ProfilePositionMode;
        StateMachine sm1 = epos1.Operation.StateMachine;
        StateMachine sm2 = epos2.Operation.StateMachine;
        StateMachine sm3 = epos3.Operation.StateMachine;
        if (sm1.GetFaultState())
        {
            sm1.ClearFault();
        }

        if (sm2.GetFaultState())
        {
            sm2.ClearFault();
        }

        if (sm3.GetFaultState())
        {
            sm3.ClearFault();
        }

        sm1.SetEnableState();
        sm2.SetEnableState();
        sm3.SetEnableState();
    }

    void MoveFirst()
    {
        ppm1.SetPositionProfile(1000, 500, 500);
        ppm2.SetPositionProfile(1000, 500, 500);
        ppm3.SetPositionProfile(1000, 500, 500);
        ppm1.MoveToPosition(0 + offsetMotor1, true, true);
        ppm2.MoveToPosition(0 + offsetMotor2, true, true);
        ppm3.MoveToPosition(0 + offsetMotor3, true, true);
    }

    double d = 346.41; // Unit mm
    double h = 300.0; // Unit mmm


    // Unit inc   2000 inc == 1 rotation == 2 mm
    double dt = 0.35; // Unit second
    double accel_rate = 1.0;
    public override void WalkStraight()
    {
        coroutine = StartCoroutine(WalkStraightAsync());
    }

    public override void WalkStop()
    {
        StopCoroutine(coroutine);
    }

    IEnumerator WalkStraightAsync()
    {
        while (true)
        {
            yield return WalkStraightFunc();
            yield return new WaitForSeconds((float)dt);
        }
    }
    IEnumerator WalkStraightFunc()
    {
        double tan_roll = Math.Tan(roll / 180.0 * Math.PI);
        double tan_pitch = Math.Tan(pitch / 180.0 * Math.PI);
        ppm1.MoveToPosition(0 + offsetMotor1, true, true);
        ppm2.MoveToPosition(0 + offsetMotor2, true, true);
        ppm3.MoveToPosition(0 + offsetMotor3, true, true);
        yield return new WaitForSeconds((float)dt);
        double l1 = lift + d * tan_roll / 2.0 + h * tan_pitch / 3.0; // Unit mm
        double l2 = lift - d * tan_roll / 2.0 + h * tan_pitch / 3.0; // Unit mm
        double l3 = lift - 2.0 * h * tan_pitch / 3.0; // Unit mm
        double l1_inc = l1 / 2.0 * 2000; // Convert Unit mm to Unit inc
        double l2_inc = l2 / 2.0 * 2000; // Convert Unit mm to Unit inc
        double l3_inc = l3 / 2.0 * 2000; // Convert Unit mm to Unit inc
        double l1_r = l1 / 2.0; // Convert Unit mm to Unit rotation
        double l2_r = l2 / 2.0; // Convert Unit mm to Unit rotation
        double l3_r = l3 / 2.0; // Convert Unit mm to Unit rotation

        ppm1.SetPositionProfile((uint)Math.Abs(2 * l1_r / dt * 60),
            (uint)Math.Abs(4 * l1_r / dt / dt * 60 * accel_rate),
            (uint)Math.Abs(4 * l1_r / dt / dt * 60 * accel_rate));
        ppm1.MoveToPosition((int)(-l1_inc) + offsetMotor1, true, false);
        ppm2.SetPositionProfile((uint)Math.Abs(2 * l2_r / dt * 60),
            (uint)Math.Abs(4 * l2_r / dt / dt * 60 * accel_rate),
            (uint)Math.Abs(4 * l2_r / dt / dt * 60 * accel_rate));
        ppm2.MoveToPosition((int)(-l2_inc) + offsetMotor2, true, false);
        ppm3.SetPositionProfile((uint)Math.Abs(2 * l3_r / dt * 60),
            (uint)Math.Abs(4 * l3_r / dt / dt * 60 * accel_rate),
            (uint)Math.Abs(4 * l3_r / dt / dt * 60 * accel_rate));
        ppm3.MoveToPosition((int)(-l3_inc) + offsetMotor3, true, false);
        yield return new WaitForSeconds((float)dt);

        ppm1.MoveToPosition(0 + offsetMotor1, true, true);
        ppm2.MoveToPosition(0 + offsetMotor2, true, true);
        ppm3.MoveToPosition(0 + offsetMotor3, true, true);

        yield return new WaitForSeconds((float)dt);

        l1 = lift + d * (-tan_roll) / 2.0 + h * tan_pitch / 3.0; // Unit mm
        l2 = lift - d * (-tan_roll) / 2.0 + h * tan_pitch / 3.0; // Unit mm
        l3 = lift - 2.0 * h * tan_pitch / 3.0; // Unit mm
        l1_inc = l1 / 2.0 * 2000; // Convert Unit mm to Unit inc
        l2_inc = l2 / 2.0 * 2000; // Convert Unit mm to Unit inc
        l3_inc = l3 / 2.0 * 2000; // Convert Unit mm to Unit inc
        l1_r = l1 / 2.0; // Convert Unit mm to Unit rotation
        l2_r = l2 / 2.0; // Convert Unit mm to Unit rotation
        l3_r = l3 / 2.0; // Convert Unit mm to Unit rotation

        ppm1.SetPositionProfile((uint)Math.Abs(2 * l1_r / dt * 60),
            (uint)Math.Abs(4 * l1_r / dt / dt * 60 * accel_rate),
            (uint)Math.Abs(4 * l1_r / dt / dt * 60 * accel_rate));
        ppm1.MoveToPosition((int)(-l1_inc) + offsetMotor1, true, false);
        ppm2.SetPositionProfile((uint)Math.Abs(2 * l2_r / dt * 60),
            (uint)Math.Abs(4 * l2_r / dt / dt * 60 * accel_rate),
            (uint)Math.Abs(4 * l2_r / dt / dt * 60 * accel_rate));
        ppm2.MoveToPosition((int)(-l2_inc) + offsetMotor2, true, false);
        ppm3.SetPositionProfile((uint)Math.Abs(2 * l3_r / dt * 60),
            (uint)Math.Abs(4 * l3_r / dt / dt * 60 * accel_rate),
            (uint)Math.Abs(4 * l3_r / dt / dt * 60 * accel_rate));
        ppm3.MoveToPosition((int)(-l3_inc) + offsetMotor3, true, false);
    }

    private void OnDestroy()
    {
      
        ppm1.Advanced.Dispose();
        ppm2.Advanced.Dispose();
        ppm3.Advanced.Dispose();
        StopCoroutine(coroutine);
    }
}