using System;
using UnityEngine;

public class LowerLimb6MotorBase: MonoBehaviour
{
    public bool command;

    public virtual void WalkStraight()
    {
        throw new NotImplementedException();
    }
    public virtual void WalkBack()
    {
        throw new NotImplementedException();
    }
    public virtual void WalkRight()
    {
        throw new NotImplementedException();
    }
    public virtual void WalkLeft()
    {
        throw new NotImplementedException();
    }

    public virtual void WalkStop()
    {
        throw new NotImplementedException();
    }
}