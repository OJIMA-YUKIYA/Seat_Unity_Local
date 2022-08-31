using UnityEngine;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System;
using UnityEngine.UI;
using System.Collections;
//volume追加(2021/03/09)

public class footVibration3:MonoBehaviour
{

	public string host = "192.168.4.1";
	public int port = 61000;
	private UdpClient client;

    public string sendText;

    public int soundTime = 10;//振動時間[ms]
    public int frequency = 100;//振動周波数[Hz]
    public int interval = 10;//前後振動間隔[ms] 
    public int volume = 125;//音の大きさ（０～２５５）
  //  public LowerLimbMotionController_6Motor controller;

    // Use this for initialization
    void Start ()
	{
		client = new UdpClient ();
		client.Connect (host, port);


    }

	// Update is called once per frame
	void Update ()
	{


        if (Input.GetKeyDown(KeyCode.Space))//歩行開始
        {
            //送信するデータを文字列でまとめる
            sendText = "start" + "," + soundTime.ToString() + "," + frequency.ToString() + "," + interval.ToString() + "," + volume.ToString() + "," + "/";
            byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText);//送信する文字列をbyteに変換
            client.Send(sendByte, sendByte.Length);//送信
            Debug.Log(sendText);



        }

        if (Input.GetKeyDown(KeyCode.S))//歩行停止
        {
            sendText = "stop" + "," + "/";

            byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText);//送信する文字列をbyteに変換
            client.Send(sendByte, sendByte.Length);//送信
            Debug.Log(sendText);
          


        }
        if (Input.GetKeyDown(KeyCode.U))//歩行開始
        {
            //送信するデータを文字列でまとめる
            sendText = "update" + "," + soundTime.ToString() + "," + frequency.ToString() + "," + interval.ToString() + "," + volume.ToString() + "," + "/";
            byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText);//送信する文字列をbyteに変換
            client.Send(sendByte, sendByte.Length);//送信
            Debug.Log(sendText);



        }
        if (Input.GetKeyDown(KeyCode.F))//歩行開始
        {
            //送信するデータを文字列でまとめる
            sendText = "ship" + "," + soundTime.ToString() + "," + frequency.ToString() + "," + interval.ToString() + "," + volume.ToString() + "," + "/";
            byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText);//送信する文字列をbyteに変換
            client.Send(sendByte, sendByte.Length);//送信
            Debug.Log(sendText);



        }

    }
   
}


