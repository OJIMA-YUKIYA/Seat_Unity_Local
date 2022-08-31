using UnityEngine;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Video;
using System.Collections.Generic;
//4モータ用
//一歩目遅延時間もパラメータ通信する
//目標設定1周期2分割->4分割に変更
//目標設定1周期2分割に戻す
//UDPReceiver（グラフ描画，csv保存）なし
//動画付き調整法用, 
//調整部分コメントアウト
//送信データモータ1つにつき７つ


public class LowerLimbMotionController_4Motor4 : MonoBehaviour
{

    public string host = "192.168.4.1";
    public int port = 61000;
    private UdpClient client;

   
    public string sendText;
    //public int walkCycle = 1400; //歩行周期[ms]
    private int numMotor = 4;//モータ数

  
    //ペダル
    private const float maxAngle = 25f; //ペダル最大角度[mm]
    private const float minAngle = -55f; //ペダル最小角度[mm]
    private const float resolutionPedal = 0.0144f; //[degrees/pulse]
    private const float footLength = 155f;//回転部から踵端までの長さ
    private float rightPedalUp1_f = 0f;//右ペダル昇降時の目標パルス格納用（小数で）
    private float rightPedalDown1_f = 0f;//右ペダル下降時の目標パルス格納用（小数で）
    private float leftPedalUp1_f = 0f;//左ペダル昇降時の目標パルス格納用（小数で）
    private float leftPedalDown1_f = 0f;//左ペダル下降時の目標パルス格納用（小数で）
    private float rightPedalUp2_f = 0f;//右ペダル昇降時の目標パルス格納用（小数で）
    private float rightPedalDown2_f = 0f;//右ペダル下降時の目標パルス格納用（小数で）
    private float leftPedalUp2_f = 0f;//左ペダル昇降時の目標パルス格納用（小数で）
    private float leftPedalDown2_f = 0f;//左ペダル下降時の目標パルス格納用（小数で）

    //スライダ
    public const int maxPosition = 90;  //[mm]
    private const int minPosition = -90;  //[mm]
    private const float resolutionSlider = 0.012f; //[mm/pulse]

    [SerializeField, Range(-55, 105)]
    private float leftPedalUp1 = 0;//左ペダル下端上昇目標値[mm]
    [SerializeField, Range(-55, 105)]
    private float leftPedalDown1 = 0;//左ペダル下端下降目標値[mm]
    [SerializeField, Range(-90, 90)]
    float leftSliderForward1 = 36;////左スライダ前進目標値[mm]
    [SerializeField, Range(-90, 90)]
    float leftSliderBackward1 = -24;//左スライダ後退目標値[mm]


    [SerializeField, Range(-55, 105)]
    private float rightPedalUp1 = 0;//右ペダル下端上昇目標値[mm]
    [SerializeField, Range(-55, 105)]
    private float rightPedalDown1 = 0;//右ペダル下端下降目標値[mm]

    [SerializeField, Range(-90, 90)]
    float rightSliderForward1 = 36;//右スライダ後退目標値[mm]
    [SerializeField, Range(-90, 90)]
    float rightSliderBackward1 = -24;//右スライダ後退目標値[mm]




    //出力パルス（送信）
    public int[] targetPulseUp1 = new int[4] { 0, 0, 0, 0,};//上昇／前進時の目標パルス（左ペダル、左スライダ、右ペダル、右スライダ）[pulse]
    public int[] targetPulseDown1 = new int[4] { 0, 0, 0, 0};//下降／後退時の目標パルス（左ペダル、左スライダ、右ペダル、右スライダ）[pulse]
                                                                    //駆動時間（送信）
    public int[] driveTimeUp1 = new int[4] { 560, 560, 560, 560};//上昇／前進時の駆動時間（左ペダル、左スライダ、右ペダル、右スライダ）[ms]
    public int[] driveTimeDown1 = new int[4] { 280, 840, 280, 840};//下降／後退時の駆動時間（左ペダル、左スライダ、右ペダル、右スライダ）[ms]
                                                                              //待機時間（送信）
    public int[] delayTimeUp1 = new int[4] { 560, 0, 560, 0};//上昇／前進始めモータ停止時間（左ペダル、左スライダ、右ペダル、右スライダ）[ms]
    public int[] delayTimeDown1 = new int[4] { 0, 0, 0, 0};//下降／後退始めモータ停止時間（左ペダル、左スライダ、右ペダル、右スライダ）[ms]
    public int[] delayTimeFirst = new int[4] { 0, 280, 700, 980};//一歩目モータ停止時間（左ペダル、左スライダ、右ペダル、右スライダ）[ms]

    public bool walk;
    private float startTime;
   
    public bool start;
    public bool stop;
    public bool update;



    // Use this for initialization
    void Start()
    {
        client = new UdpClient();
        client.Connect(host, port);//UDP接続
                                  
    }



    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.Space) || start)//歩行開始
        {
            targetCalculate();//目標値計算

            //送信するデータを文字列でまとめる
            sendText = "start" + ",";
            for (int i = 0; i < numMotor; i++)
            {

                sendText += targetPulseUp1[i].ToString() + "," + targetPulseDown1[i].ToString() + ",";
                sendText += driveTimeUp1[i].ToString() + "," + driveTimeDown1[i].ToString() + ",";
                sendText += delayTimeUp1[i].ToString() + "," + delayTimeDown1[i].ToString() + ",";
                sendText += delayTimeFirst[i].ToString() + ",";
            }
            sendText += "/";//終わりの目印
            byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText);//送信する文字列をbyteに変換
            client.Send(sendByte, sendByte.Length);//送信
            Debug.Log(sendText);
            startTime = Time.time;

            walk = true;
            start = false;
            //Play();

        }

        if (Input.GetKeyDown(KeyCode.S) || stop)//歩行停止
        {
            sendText = "stop" + "," + "/";

            byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText);//送信する文字列をbyteに変換
            client.Send(sendByte, sendByte.Length);//送信
            Debug.Log(sendText);
            walk = false;
            stop = false;
            //Pause();

        }
        if (Input.GetKeyDown(KeyCode.U) || update)//パラメータ変更
        {
            dataUpdate();

        }
        if (Input.GetKeyDown(KeyCode.R))//正面に戻す
        {

            targetCalculate();//目標値計算
            //送信するデータを文字列でまとめる
            sendText = "reset" + ",";
            sendText += "/";//終わりの目印
            byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText);//送信する文字列をbyteに変換
            client.Send(sendByte, sendByte.Length);//送信
            Debug.Log(sendText);
        }
        

    }

    void OnApplicationQuit()
        {
           
            if (walk)
            {
                sendText = "stop" + "," + "/";

                byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText);//送信する文字列をbyteに変換
                client.Send(sendByte, sendByte.Length);//送信
                Debug.Log(sendText);
            }


        }
    void dataUpdate()
    {

        targetCalculate();//目標値計算
                          //送信するデータを文字列でまとめる
        sendText = "update" + ",";
        for (int i = 0; i < numMotor; i++)
        {
            sendText += targetPulseUp1[i].ToString() + "," + targetPulseDown1[i].ToString() + ",";
            sendText += driveTimeUp1[i].ToString() + "," + driveTimeDown1[i].ToString() + ",";
            sendText += delayTimeUp1[i].ToString() + "," + delayTimeDown1[i].ToString() + ",";
            sendText += delayTimeFirst[i].ToString() + ",";
        }
        sendText += "/";//終わりの目印
        byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText);//送信する文字列をbyteに変換
        client.Send(sendByte, sendByte.Length);//送信
        Debug.Log(sendText);
        update = false;

    }
    void targetCalculate()//振幅値（mm）→出力パルス変換
        {
            //ペダル目標値計算
            leftPedalUp1_f = -(Mathf.Asin(leftPedalUp1 / footLength) * Mathf.Rad2Deg / resolutionPedal);
            leftPedalDown1_f = -(Mathf.Asin(leftPedalDown1 / footLength) * Mathf.Rad2Deg) / resolutionPedal;
            rightPedalUp1_f = Mathf.Asin(rightPedalUp1 / footLength) * Mathf.Rad2Deg / resolutionPedal;
            rightPedalDown1_f = Mathf.Asin(rightPedalDown1 / footLength) * Mathf.Rad2Deg / resolutionPedal;

            //目標パルスを整数型で格納
            targetPulseUp1[0] = (int)leftPedalUp1_f;//-(Up)
            targetPulseDown1[0] = (int)leftPedalDown1_f;
            targetPulseUp1[1] = (int)(leftSliderForward1 / resolutionSlider);
            targetPulseDown1[1] = (int)(leftSliderBackward1 / resolutionSlider);
            targetPulseUp1[2] = (int)rightPedalUp1_f;
            targetPulseDown1[2] = (int)rightPedalDown1_f;
            targetPulseUp1[3] = (int)(rightSliderForward1 / resolutionSlider);
            targetPulseDown1[3] = (int)(rightSliderBackward1 / resolutionSlider);
          
            /*
            //歩行駆動時間計算
            driveTimeUp[0] = (int)(walkCycle * walkRatio* pedalRatio);
            driveTimeUp[1] = (int)(walkCycle * sliderRatio);
            driveTimeUp[2] = (int)(walkCycle * walkRatio* pedalRatio);
            driveTimeUp[3] = (int)(walkCycle * sliderRatio);
            driveTimeDown[0] = (int)(walkCycle * walkRatio * (1 - pedalRatio));
            driveTimeDown[1] = (int)(walkCycle * (1 - sliderRatio));
            driveTimeDown[2] = (int)(walkCycle * walkRatio * (1 - pedalRatio));
            driveTimeDown[3] = (int)(walkCycle * (1 - sliderRatio));
            delayTimeUp[0] = (int)(walkCycle * (1 - walkRatio));
            delayTimeUp[2] = (int)(walkCycle * (1 - walkRatio));
            */
        }




}



