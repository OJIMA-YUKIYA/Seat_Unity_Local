using UnityEngine;
using System.Collections;

public class SerialCommunication2 : MonoBehaviour
{
    public SerialHandler serialHandler;
    
    private float timeElapsed, timeArduino;
 
    public int[] targetPulseUp = new int[4] { 0, 0, 0, 0 };
    public int[] targetPulseDown = new int[4] { 0, 0, 0, 0, };
    public int[] driveTimeUp = new int[4] { 560, 560, 560, 560 };
    public int[] driveTimeDown = new int[4] { 280, 840, 280, 840 };



    void Start ()
    {
        serialHandler.OnDataReceived += OnDataReceived;
    }
	
	void Update () {

    }

    void FixedUpdate()
    {

        if (Input.GetKeyDown(KeyCode.S))
        {
 
 
            serialHandler.Write("s" +  "," +  "e");
            Debug.Log("START");
        }else if (Input.GetKeyDown(KeyCode.L))
        {

            serialHandler.Write("l" + ","  + "e");
            Debug.Log("LAST");

        }
        else if (Input.GetKeyDown(KeyCode.U))
        {
            serialHandler.Write("u" + ",");
            for (int i = 0; i < 4; i++) {
                serialHandler.Write( targetPulseUp[i].ToString() + "," + targetPulseDown[i].ToString() + "," + driveTimeUp[i].ToString() + "," + driveTimeDown[i].ToString() + ",");
            }
            serialHandler.Write("e");
            Debug.Log("Update");
        }
        else {
        }
    }

    void OnDataReceived(string message)
    {
        var data = message.Split(new string[] { "," }, System.StringSplitOptions.None);
       
        // if (data.Length != 2) return;
        /*
        float tmp = timeArduino;
        if (!float.TryParse(data[0], out tmp))
        {
            return;
        }
        timeArduino = tmp;
        */
        
        try
        {
            Debug.Log(message);
             //Debug.Log(data.Length.ToString() + ", " + data[0] + ", " + data[1]+"," + data[2] + "," + data[3]);
             
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(e.Message);
        }
        
    }
}
