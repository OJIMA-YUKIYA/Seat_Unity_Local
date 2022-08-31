using UnityEngine;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System;
using UnityEngine.UI;
using System.Collections;


public class UDPCom1:MonoBehaviour
{

	public string host = "192.168.4.1";
	public int port = 61000;
	private UdpClient client;

    public string sendText;

    //出力パルス（送信）
    public int[] targetPulseUp = new int[4] { 0, 0, 0, 0 };//昇降／前進時の目標パルス（左ペダル、左スライダ、右ペダル、右スライダ）[pulse]
    public int[] targetPulseDown = new int[4] { 0, 0, 0, 0, };//下降／後退時の目標パルス（左ペダル、左スライダ、右ペダル、右スライダ）[pulse]
    public int[] driveTimeUp = new int[4] { 560, 560, 560, 560 };//昇降／前進時の駆動時間（左ペダル、左スライダ、右ペダル、右スライダ）[pulse]
    public int[] driveTimeDown = new int[4] { 280, 840, 280, 840 };//下降／後退時の駆動時間（左ペダル、左スライダ、右ペダル、右スライダ）[pulse]

    //ペダル
    [SerializeField, Range(-55, 25)]
    float leftPedalUp = 25;//左ペダル上昇目標値[mm]
    [SerializeField, Range(-55, 25)]
    float leftPedalDown = 0;//左ペダル下降目標値[mm]
    [SerializeField, Range(-55, 25)]
    float rightPedalUp = 25;//右ペダル上昇目標値[mm]
    [SerializeField, Range(-55, 25)]
    float rightPedalDown = 0;//右ペダル下降目標値[mm]

    private const float maxAngle = 25f; //ペダル最大角度[mm]
    private const float minAngle = -55f; //ペダル最小角度[mm]
    private const float resolutionPedal = 0.0144f; //[degrees/pulse]
    private const float footLength = 155f;//回転部から踵端までの長さ
    private float rightPedalUp_f = 0f;//右ペダル昇降時の目標パルス格納用（小数で）
    private float rightPedalDown_f = 0f;//右ペダル下降時の目標パルス格納用（小数で）
    private float leftPedalUp_f = 0f;//左ペダル昇降時の目標パルス格納用（小数で）
    private float leftPedalDown_f = 0f;//左ペダル下降時の目標パルス格納用（小数で）
   
    //スライダ
    [SerializeField, Range(-50, 50)]
    float leftSliderForward = 36;////左スライダ前進目標値[mm]
    [SerializeField, Range(-50, 50)]
    float leftSliderBackward = -24;//左スライダ後退目標値[mm]
    [SerializeField, Range(-50, 50)]
    float rightSliderForward = 36;//左スライダ後退目標値[mm]
    [SerializeField, Range(-50, 50)]
    float rightSliderBackward = -24;//左スライダ後退目標値[mm]
    public const float maxPosition = 90f;  //[mm]
    private const float minPosition = -90f;  //[mm]
    private const float resolutionSlider = 0.012f; //[mm/pulse]

    // Use this for initialization
    void Start ()
	{
		client = new UdpClient ();
		client.Connect (host, port);


		
	}

	// Update is called once per frame
	void Update ()
	{
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //ペダル目標値計算
            leftPedalUp_f = -(Mathf.Asin(leftPedalUp / footLength) * Mathf.Rad2Deg / resolutionPedal);
            leftPedalDown_f = -(Mathf.Asin(leftPedalDown / footLength) * Mathf.Rad2Deg) / resolutionPedal;
            rightPedalUp_f = Mathf.Asin(rightPedalUp / footLength) * Mathf.Rad2Deg / resolutionPedal;
            rightPedalDown_f = Mathf.Asin(rightPedalDown / footLength) * Mathf.Rad2Deg / resolutionPedal;
            //目標パルスを整数型で格納
            targetPulseUp[0] = (int)leftPedalUp_f;//-(Up)
            targetPulseDown[0] = (int)leftPedalDown_f;
            targetPulseUp[1] = (int)(leftSliderForward / resolutionSlider);
            targetPulseDown[1] = (int)(leftSliderBackward / resolutionSlider);
            targetPulseUp[2] = (int)rightPedalUp_f;
            targetPulseDown[2] = (int)rightPedalDown_f;
            targetPulseUp[3] = (int)(rightSliderForward / resolutionSlider);
            targetPulseDown[3] = (int)(rightSliderBackward / resolutionSlider);

            //送信するデータを文字列でまとめる
            sendText = "start" + ",";
            for (int i = 0; i < 4; i++)
            {
                sendText += targetPulseUp[i].ToString() + "," + targetPulseDown[i].ToString() + ",";
            }
            sendText += "e";//終わりの目印
            
            byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText);//送信する文字列をbyteに変換
            client.Send(sendByte, sendByte.Length);//送信
            
            Debug.Log(sendText);
            Debug.Log(sendByte);

        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            //送信するデータを文字列でまとめる
            sendText = "stop" + ",";

            sendText += "e";//終わりの目印

            byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText);//送信する文字列をbyteに変換
            client.Send(sendByte, sendByte.Length);//送信

            Debug.Log(sendText);
            Debug.Log(sendByte);
            /*
            sendText = "stop" + "," + "e";
            byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText);//送信する文字列をbyteに変換
            client.Send(sendByte, sendByte.Length);//送信

            Debug.Log(sendText);
            */
        }
    }
}



//メモ//
//ferrtop2.mp4:
//シーンチェンジフレーム：1,465,1150,2930,3270,3555,3915,4525,