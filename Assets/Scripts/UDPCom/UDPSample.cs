using UnityEngine;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System;
using UnityEngine.UI;
using System.Collections;


public class UDPSample:MonoBehaviour
{

	public string host = "192.168.4.1";
	public int port = 61000;
	private UdpClient client;

    //private IMediaProducer m_Texture;

	// Use this for initialization
	void Start ()
	{
		client = new UdpClient ();
		client.Connect (host, port);


		byte[] sceneCommand = new byte[6];
		sceneCommand [0] = 0x31;  // change scene command
		sceneCommand [1] = 0x01;  // number of data
		sceneCommand [2] = 0x00;  // number of data
		sceneCommand [3] = 0x00;  // number of data
		sceneCommand [4] = 0x00;  // number of data
		sceneCommand [5] = 0x00;  // scene number
	}

	// Update is called once per frame
	void Update ()
	{
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //Debug.Log(m_Control.GetCurrentTimeMs() * 0.001f);
            // Debug.Log(m_time);
            byte[] sceneCommand = new byte[6];
            sceneCommand[0] = 0x31;  // change scene command
            sceneCommand[1] = 0x01;  // number of data
            sceneCommand[2] = 0x00;  // number of data
            sceneCommand[3] = 0x00;  // number of data
            sceneCommand[4] = 0x00;  // number of data

            //byte[] sceneCommand = new byte[6];
            //sceneCommand[0] = 0x31;  // change scene command
            //sceneCommand[1] = 0x01;  // number of data
            //sceneCommand[2] = 0x00;  // number of data
            //sceneCommand[3] = 0x00;  // number of data
            //sceneCommand[4] = 0x00;  // number of data
            sceneCommand[5] = 0x01;  // scene number
            client.Send(sceneCommand, sceneCommand.Length);
            Debug.Log(sceneCommand);

        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            //Debug.Log(m_Control.GetCurrentTimeMs() * 0.001f);
            // Debug.Log(m_time);
            byte[] sceneCommand = new byte[6];
            sceneCommand[0] = 0x31;  // change scene command
            sceneCommand[1] = 0x01;  // number of data
            sceneCommand[2] = 0x00;  // number of data
            sceneCommand[3] = 0x00;  // number of data
            sceneCommand[4] = 0x00;  // number of data

            //byte[] sceneCommand = new byte[6];
            //sceneCommand[0] = 0x31;  // change scene command
            //sceneCommand[1] = 0x01;  // number of data
            //sceneCommand[2] = 0x00;  // number of data
            //sceneCommand[3] = 0x00;  // number of data
            //sceneCommand[4] = 0x00;  // number of data
            sceneCommand[5] = 0x00;  // scene number
            client.Send(sceneCommand, sceneCommand.Length);
            Debug.Log(sceneCommand);
        }
    }
}



//メモ//
//ferrtop2.mp4:
//シーンチェンジフレーム：1,465,1150,2930,3270,3555,3915,4525,