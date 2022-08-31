using UnityEngine;
using System.Collections;

public class SerialCommunication : MonoBehaviour
{
    public SerialHandler serialHandler;
    
    private float timeElapsed, timeArduino;
    private  int[] v = new int[16]{ 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15 };

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
                serialHandler.Write( v[4*i].ToString() + "," + v[4*i + 1].ToString() + "," + v[4*i+2].ToString() + "," + v[4*i+3].ToString() + ",");
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
