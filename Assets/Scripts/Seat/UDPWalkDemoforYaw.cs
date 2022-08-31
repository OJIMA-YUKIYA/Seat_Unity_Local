using UnityEngine;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System;
using UnityEngine.UI;
using System.Collections;


public class UDPWalkDemoforYaw:MonoBehaviour
{
    [SerializeField]
    WalkDemoMainController mainController;
    public string host = "133.10.88.199";//"133.10.79.255";
	public int port = 61000;
	private UdpClient client;
    public int number=0; //0:stop 1;straight 2:left 3:right 4:waschback

    public bool start;
    public bool stop;
    public bool update;
    public bool command;

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
		sceneCommand [5] = (byte)number;   // scene number

    }

	// Update is called once per frame
	void Update ()
	{




        SendUdp();
      
        


	}
    public void SendUdp()
    {
        if (mainController.walkStraight)//歩行開始
        {
            number = 1;
            byte[] sceneCommand = new byte[6];
            sceneCommand[0] = 0x31;  // change scene command
            sceneCommand[1] = 0x01;  // number of data
            sceneCommand[2] = 0x00;  // number of data
            sceneCommand[3] = 0x00;  // number of data
            sceneCommand[4] = 0x00;  // number of data
            sceneCommand[5] = 0x01 ;  // scene number
            client.Send(sceneCommand, sceneCommand.Length);
            Debug.Log(sceneCommand);
            Debug.Log("WalkStart");
            
            command = true;//mainController返信用
        }

        if (mainController.walkLeft)//歩行開始
        {

            number = 2;
            byte[] sceneCommand = new byte[6];
            sceneCommand[0] = 0x31;  // change scene command
            sceneCommand[1] = 0x01;  // number of data
            sceneCommand[2] = 0x00;  // number of data
            sceneCommand[3] = 0x00;  // number of data
            sceneCommand[4] = 0x00;  // number of data
            sceneCommand[5] = (byte)number;  // scene number
            client.Send(sceneCommand, sceneCommand.Length);
            Debug.Log(sceneCommand);
            Debug.Log("WalkLeft");

            command = true;//mainController返信用
        }

        if (mainController.walkRight)//歩行開始
        {
            number = 3;
            byte[] sceneCommand = new byte[6];
            sceneCommand[0] = 0x31;  // change scene command
            sceneCommand[1] = 0x01;  // number of data
            sceneCommand[2] = 0x00;  // number of data
            sceneCommand[3] = 0x00;  // number of data
            sceneCommand[4] = 0x00;  // number of data
            sceneCommand[5] = (byte)number;  // scene number

            client.Send(sceneCommand, sceneCommand.Length);
            Debug.Log(sceneCommand);
            Debug.Log("WalkRight");

            command = true;//mainController返信用
        }

        if (mainController.walkStop)//歩行開始
        {
            number = 4;
            byte[] sceneCommand = new byte[6];
            sceneCommand[0] = 0x31;  // change scene command
            sceneCommand[1] = 0x01;  // number of data
            sceneCommand[2] = 0x00;  // number of data
            sceneCommand[3] = 0x00;  // number of data
            sceneCommand[4] = 0x00;  // number of data
            sceneCommand[5] = (byte)number;  // scene number

            client.Send(sceneCommand, sceneCommand.Length);
            Debug.Log(sceneCommand);
            Debug.Log("WalkStop");
 
            command = true;//mainController返信用
        }
       if (mainController.walkBack)//歩行開始
        {
            number = 5;
            byte[] sceneCommand = new byte[6];
            sceneCommand[0] = 0x31;  // change scene command
            sceneCommand[1] = 0x01;  // number of data
            sceneCommand[2] = 0x00;  // number of data
            sceneCommand[3] = 0x00;  // number of data
            sceneCommand[4] = 0x00;  // number of data
            sceneCommand[5] = (byte)number;  // scene number

            client.Send(sceneCommand, sceneCommand.Length);
            Debug.Log(sceneCommand);
            Debug.Log("WalkBack");

            command = true;//mainController返信用
        }
    }
}



//メモ//
//ferrtop2.mp4:
//シーンチェンジフレーム：1,465,1150,2930,3270,3555,3915,4525,