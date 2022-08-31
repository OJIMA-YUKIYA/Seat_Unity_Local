using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Runtime.InteropServices; // for javascript

using EposCmd.Net;
using EposCmd.Net.DeviceCmdSet.Operation;
using System.Threading;
using System;

public class Epos4cmd : MonoBehaviour
{
    // Start is called before the first frame update

    [SerializeField]double lift = 1.26 * 3.0; // Unit mm
    [SerializeField] double roll = 0.15 * 6.0; // Unit deg
    [SerializeField] double pitch = 0.13 * 1.5; // Unit deg
    static private int counta;

    

    public void movethread()
    {
        DeviceManager connector1 = new DeviceManager("EPOS4", "MAXON SERIAL V2", "USB", "USB0");
        Device epos1, epos2, epos3;
        epos1 = connector1.CreateDevice(1);
        epos2 = connector1.CreateDevice(2);
        epos3 = connector1.CreateDevice(3);
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
        ProfilePositionMode ppm1 = epos1.Operation.ProfilePositionMode;
        ProfilePositionMode ppm2 = epos2.Operation.ProfilePositionMode;
        ProfilePositionMode ppm3 = epos3.Operation.ProfilePositionMode;
        ppm1.SetPositionProfile(1000, 500, 500);
        ppm2.SetPositionProfile(1000, 500, 500);
        ppm3.SetPositionProfile(1000, 500, 500);
        ppm1.MoveToPosition(0, true, true);
        ppm2.MoveToPosition(0, true, true);
        ppm3.MoveToPosition(0, true, true);


        double d = 346.41; // Unit mm
        double h = 300.0; // Unit mmm

        double tan_roll = Math.Tan(roll / 180.0 * Math.PI);
        double tan_pitch = Math.Tan(pitch / 180.0 * Math.PI);
        // Unit inc   2000 inc == 1 rotation == 2 mm
        double dt = 0.35; // Unit second

        Thread.Sleep(3000);

        double accel_rate = 1.0;

        int old_counta = 0;

        while (true)
        {
            ppm1.MoveToPosition(0, true, true);
            ppm2.MoveToPosition(0, true, true);
            ppm3.MoveToPosition(0, true, true);
            Thread.Sleep((int)(dt * 1000));
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
            ppm1.MoveToPosition((int)(-l1_inc), true, false);
            ppm2.SetPositionProfile((uint)Math.Abs(2 * l2_r / dt * 60),
                (uint)Math.Abs(4 * l2_r / dt / dt * 60 * accel_rate),
                (uint)Math.Abs(4 * l2_r / dt / dt * 60 * accel_rate));
            ppm2.MoveToPosition((int)(-l2_inc), true, false);
            ppm3.SetPositionProfile((uint)Math.Abs(2 * l3_r / dt * 60),
                (uint)Math.Abs(4 * l3_r / dt / dt * 60 * accel_rate),
                (uint)Math.Abs(4 * l3_r / dt / dt * 60 * accel_rate));
            ppm3.MoveToPosition((int)(-l3_inc), true, false);
            Thread.Sleep((int)(dt * 1000));

            ppm1.MoveToPosition(0, true, true);
            ppm2.MoveToPosition(0, true, true);
            ppm3.MoveToPosition(0, true, true);

            Thread.Sleep((int)(dt * 1000));

            l1 = lift + d * (-tan_roll) / 2.0 + h * tan_pitch / 3.0; // Unit mm
            l2 = lift - d * (-tan_roll) / 2.0 + h * tan_pitch / 3.0; // Unit mm
            l3 = lift - 2.0 * h * tan_pitch / 3.0; // Unit mm
            l1_inc = l1 / 2.0 * 2000; // Convert Unit mm to Unit inc
            l2_inc = l2 / 2.0 * 2000; // Convert Unit mm to Unit inc
            l3_inc = l3 / 2.0 * 2000; // Convert Unit mm to Unit inc
            l1_r = l1 / 2.0; // Convert Unit mm to Unit rotation
            l2_r = l2 / 2.0; // Convert Unit mm to Unit rotation
            l3_r = l3 / 2.0; // Convert Unit mm to Unit rotation

            ppm1.SetPositionProfile((uint)Math.Abs(2 * l1_r / dt * 60), (uint)Math.Abs(4 * l1_r / dt / dt * 60 * accel_rate), (uint)Math.Abs(4 * l1_r / dt / dt * 60 * accel_rate));
            ppm1.MoveToPosition((int)(-l1_inc), true, false);
            ppm2.SetPositionProfile((uint)Math.Abs(2 * l2_r / dt * 60), (uint)Math.Abs(4 * l2_r / dt / dt * 60 * accel_rate), (uint)Math.Abs(4 * l2_r / dt / dt * 60 * accel_rate));
            ppm2.MoveToPosition((int)(-l2_inc), true, false);
            ppm3.SetPositionProfile((uint)Math.Abs(2 * l3_r / dt * 60), (uint)Math.Abs(4 * l3_r / dt / dt * 60 * accel_rate), (uint)Math.Abs(4 * l3_r / dt / dt * 60 * accel_rate));
            ppm3.MoveToPosition((int)(-l3_inc), true, false);
            Thread.Sleep((int)(dt * 1000));

            if (old_counta == counta)
            {
                ppm1.MoveToPosition(0, true, true);
                ppm2.MoveToPosition(0, true, true);
                ppm3.MoveToPosition(0, true, true);
                Thread.Sleep(1000);
                return;
            }
            //if(connector1.LastError)
            old_counta = counta;
        }
    }

    void Start()
    {
        counta = 0;
        Thread th = new Thread(movethread);
        th.Start();
    }

    // Update is called once per frame
    void Update()
    {
        counta++;
        //Debug.Log(AddNumbers(1,1));
        //Debug.Log(counta);
        if (counta > 10000)
        {
            counta = 0;
        }
    }

    void Stop()
    {
        Debug.Log("Stop");
    }
}
