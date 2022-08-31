using UnityEngine;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Video;
using System.Collections.Generic;
//6モータ用
//一歩目遅延時間もパラメータ通信する
//目標設定2分割->4分割に変更
//UDPReceiver（グラフ描画，csv保存）なし
//動画付き調整法用, 


public class SeatSample4:MonoBehaviour
{

	public string host = "192.168.11.35";
	public int port = 61000;
	private UdpClient client;
 
    //public WalkTimer walkTimer;
    public string sendText;
    //public int walkCycle = 1400; //歩行周期[ms]
    private static StreamWriter sw3;

    private string FileName ="abc" ;
    private string dataName = "1m" ;
    private string saveData3;
    //ゲームパッド調整用
    /*
    public float sliderAjustment = 3;
    public float rotationAjustment =0.5f;
    public float lsh;
    public float lsv;
    public float rsh;
    public float rsv;
    */

    /// リフト・ロール・ピッチ・ヨーの計算
    [SerializeField, HeaderAttribute("Parameter")]
    public double lift = 1.26;
    public double pitch = 0.13;
    public double roll = 0.15;
    public double yaw = 0;
    //public double liftSpeed = 6.74;

    public bool setParameter;// パラメータ　→ 振幅値（mm）
    public bool rightFoot;// 右足から始める場合は最初から値をスワップする
    double rad_P;
    double rad_R;
    double rad_Y;
    private int l0 = 510; //座高
    private double z_c;

    double F_1;
    double F_2;
    double F_3;


    private int numMotor = 3; //使用するモータの数

    private float Motor1Up1_f = 0f;//No.1モータの目標パルス格納用（小数で）
    private float Motor1Down1_f = 0f;//No.1モータの目標パルス格納用（小数で）
    private float Motor1Up2_f = 0f;//No.1モータの目標パルス格納用（小数で）
    private float Motor1Down2_f = 0f;//No.1モータの目標パルス格納用（小数で）
    private float Motor2Up1_f = 0f;//No.2モータの目標パルス格納用（小数で）
    private float Motor2Down1_f = 0f;//No.2モータの目標パルス格納用（小数で）
    private float Motor2Up2_f = 0f;//No.2モータの目標パルス格納用（小数で）
    private float Motor2Down2_f = 0f;//No.2モータの目標パルス格納用（小数で）
    private float Motor3Up1_f = 0f;//No.3モータの目標パルス格納用（小数で）
    private float Motor3Down1_f = 0f;//No.3モータの目標パルス格納用（小数で）
    private float Motor3Up2_f = 0f;//No.3モータの目標パルス格納用（小数で）
    private float Motor3Down2_f = 0f;//No.3モータの目標パルス格納用（小数で）

    private const float motorResolution = 0.01f;//[mm/pulse]

    /// <summary>
    /// アクチュエーターの長さ設定(mm)
    /// </summary>
    [SerializeField, HeaderAttribute("Length"), Range(-20, 20)]//スライダの範囲を設定
    float Motor1Up1 = 3;//モータ1上昇目標値1[mm]
    [SerializeField, Range(-20, 20)]//スライダの範囲を設定
    float Motor1Down1= 0;//モータ1下降目標値1[mm]
    [SerializeField, Range(-20, 20)]//スライダの範囲を設定
    float Motor1Up2 = 3;//モータ1上昇目標値2[mm]
    [SerializeField, Range(-20, 20)]//スライダの範囲を設定
    float Motor1Down2 = 0;//モータ1下降目標値2[mm]

    [SerializeField, Range(-20, 20)]//スライダの範囲を設定
    float Motor2Up1 = 3;//モータ1上昇目標値1[mm]
    [SerializeField, Range(-20, 20)]//スライダの範囲を設定
    float Motor2Down1 = 0;//モータ1下降目標値1[mm]
    [SerializeField, Range(-20, 20)]//スライダの範囲を設定
    float Motor2Up2 = 3;//モータ1上昇目標値2[mm]
    [SerializeField, Range(-20, 20)]//スライダの範囲を設定
    float Motor2Down2 = 0;//モータ1下降目標値2[mm]

    [SerializeField, Range(-20, 20)]//スライダの範囲を設定
    float Motor3Up1 = 3;//モータ1上昇目標値1[mm]
    [SerializeField, Range(-20, 20)]//スライダの範囲を設定
    float Motor3Down1 = 0;//モータ1下降目標値1[mm]
    [SerializeField, Range(-20, 20)]//スライダの範囲を設定
    float Motor3Up2 = 3;//モータ1上昇目標値2[mm]
    [SerializeField, Range(-20, 20)]//スライダの範囲を設定
    float Motor3Down2 = 0;//モータ1下降目標値2[mm]

    //出力パルス（送信）
    public int[] targetPulseUp1 = new int[3] { 0, 0, 0};//上昇時の目標パルス1（No.1,No.2,No.3）[pulse]
    public int[] targetPulseDown1 = new int[3] { 0, 0, 0};//下降時の目標パルス1（No.1,No.2,No.3）[pulse]
    public int[] targetPulseUp2 = new int[3] { 0, 0, 0 };//上昇時の目標パルス1（No.1,No.2,No.3）[pulse]
    public int[] targetPulseDown2 = new int[3] { 0, 0, 0 };//下降時の目標パルス1（No.1,No.2,No.3）[pulse]
    //駆動時間(送信）
    public int[] driveTimeUp1 = new int[3] { 350, 350, 350};//上昇の駆動時間1（No.1,No.2,No.3）[ms]
    public int[] driveTimeDown1 = new int[3] { 350, 350, 350};//下降の駆動時間1（No.1,No.2,No.3）[ms]
    public int[] driveTimeUp2 = new int[3] { 350, 350, 350 };//上昇の駆動時間1（No.1,No.2,No.3）[ms]
    public int[] driveTimeDown2 = new int[3] { 350, 350, 350 };//下降の駆動時間1（No.1,No.2,No.3）[ms]
    //待機時間（送信）
    public int[] delayTimeUp1 = new int[3] { 0, 0, 0};//上昇始めモータ停止時間（No.1,No.2,No.3）[ms]
    public int[] delayTimeDown1 = new int[3] { 0, 0, 0};//下降始めモータ停止時間（No.1,No.2,No.3）[ms]
    public int[] delayTimeUp2 = new int[3] { 0, 0, 0 };//上昇始めモータ停止時間（No.1,No.2,No.3）[ms]
    public int[] delayTimeDown2 = new int[3] { 0, 0, 0 };//下降始めモータ停止時間（No.1,No.2,No.3）[ms]
    public int[] delayTimeFirst = new int[3] { 0, 0, 0 };//一歩目モータ停止時間（No.1,No.2,No.3）[ms]

    public bool walk;
    private float startTime;
    public float walkTime;
    public bool start;
    public bool stop;
    public bool update;
    private bool stick1;
    private bool stick2;
    private bool stick3;
    private bool stick4;
    private bool stick5;
    private bool stick6;
    private bool write;
    public int[] free = new int[3] { 0, 0, 0 };
    public bool clear;
    private int cleear_st;
    public bool home;
    private int home_st;

    //動画制御用
    [SerializeField]
    private List<VideoPlayer> playList; 
   
    
    // Use this for initialization
    void Start ()
	{
		client = new UdpClient ();
		client.Connect (host, port);//UDP接続
   
        sw3 = new StreamWriter(@FileName + ".csv", true, Encoding.GetEncoding("Shift_JIS"));
        write = true;
        sw3.WriteLine(dataName);
        string[] s1 = { "No.1Up1", "No.1Up2", "No.2Up1", "No.2Up2", "No.3Up1", "No.3Up2", };//saveData1のラベル

        string s2 = string.Join(",", s1);//間にカンマ追加
        sw3.WriteLine(s2);//ラベル書き込み
        //Play();
        //Pause();

    }
    
    

    // Update is called once per frame
    void Update ()
	{
        
        if (Input.GetKeyDown(KeyCode.Space) || start)//歩行開始
        {
            targetCalculate();//目標値計算

            //送信するデータを文字列でまとめる
            sendText = "start" + ",";
            for (int i = 0; i < numMotor; i++)
            {

                sendText += targetPulseUp1[i].ToString() + "," + targetPulseDown1[i].ToString() + "," + targetPulseUp2[i].ToString() + "," + targetPulseDown2[i].ToString() + ",";
                sendText += driveTimeUp1[i].ToString() + "," + driveTimeDown1[i].ToString() + "," + driveTimeUp2[i].ToString() + "," + driveTimeDown2[i].ToString() + ",";
                sendText += delayTimeUp1[i].ToString() + "," + delayTimeDown1[i].ToString() + "," + delayTimeUp2[i].ToString() + "," + delayTimeDown2[i].ToString() + ",";
                sendText += delayTimeFirst[i].ToString() + ",";
            }
            sendText += "/";//終わりの目印
            byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText);//送信する文字列をbyteに変換
            client.Send(sendByte, sendByte.Length);//送信
            Debug.Log(sendText);
            startTime = Time.time;
       
            walk= true;
            start = false;
            //Play();

        }
        
        if (Input.GetKeyDown(KeyCode.S)||stop)//歩行停止
        {
            sendText = "stop" + ","+ "/";

            byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText);//送信する文字列をbyteに変換
            client.Send(sendByte, sendByte.Length);//送信
            Debug.Log(sendText);
            walk = false;
            stop = false;
            Pause();
         
        }
        if(Input.GetKeyDown(KeyCode.U)|| update)//パラメータ変更
        {
            dataUpdate();

        }
        if (Input.GetKeyDown(KeyCode.F))//レンジブレーキクリア
        {
            sendText = "free" + ",";
            sendText += free[0].ToString() + ","+ free[1].ToString() + ","+free[2].ToString() + ",";
            sendText += "/";

            byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText);//送信する文字列をbyteに変換
            client.Send(sendByte, sendByte.Length);//送信
            Debug.Log(sendText);
        }
        if (Input.GetKeyDown(KeyCode.C))//&&clear)//アラーム解除
        {
            cleear_st = 0;

            if (clear)
            {
                cleear_st = 1;
            }
                sendText = "clear" + ",";
                sendText += cleear_st + "," + cleear_st + "," + cleear_st + ",";
                sendText += "/";

                byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText);//送信する文字列をbyteに変換
                client.Send(sendByte, sendByte.Length);//送信
                Debug.Log(sendText);
           
        }
        if (Input.GetKeyDown(KeyCode.H))//&&home)//ホーム復帰
        {
            home_st = 0;
            if (home)
            {
                home_st = 1;
            }
                sendText = "home" + ",";
                sendText +=  home_st + "," + home_st + "," + home_st + ",";
                sendText += "/";

                byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText);//送信する文字列をbyteに変換
                client.Send(sendByte, sendByte.Length);//送信
                Debug.Log(sendText);
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
        if (Input.GetKeyDown(KeyCode.D))//データ保存
        {
            if (write)
            {
                saveData3 = Motor1Up1.ToString() + "," + Motor1Up2.ToString() + "," ;
                saveData3 += Motor2Up1.ToString() + "," + Motor2Up2.ToString() + ",";
                saveData3 += Motor3Up1.ToString() + "," + Motor3Up2.ToString() + ",";
                sw3.WriteLine(saveData3);
                sw3.Close();
                write = false;
                Debug.Log("SaveData");
            }
        }
        /*
        //ゲームパッド入力
        if (!walk)
        {
            foreach (VideoPlayer player in playList)
            {
                if (!player.isPlaying)
                {
                    player.Pause();
                }
            }
            if (Input.GetKeyDown("joystick button 4"))
            {
                start = true;
            }
        }
        else
        {
            if (Input.GetKeyDown("joystick button 5"))
            {
                stop = true;
            }
        }


        if (Input.GetKeyDown("joystick button 0"))//A
        {
            rightSliderForward1 -= sliderAjustment;
            if(rightSliderForward1 - rightSliderBackward1 <0)//限界きたら
            {
                rightSliderForward1 += sliderAjustment;
                StartCoroutine("Vibration");//振動させる
            }
               
            dataUpdate();
        }
        if (Input.GetKeyDown("joystick button 1"))//B
        {
            rightRotationAngle1 -= rotationAjustment;
            if(rightRotationAngle1 -rightRotationAngle2 > 18)
            {

                rightRotationAngle1 += rotationAjustment;
                StartCoroutine("Vibration");//振動させる
            }
            dataUpdate();
        }
        if (Input.GetKeyDown("joystick button 2"))//X
        {
            rightRotationAngle1 += rotationAjustment;
            if(Mathf.Abs(rightRotationAngle1 - leftRotationAngle2) > 9)
            {
                rightRotationAngle1 -= rotationAjustment;
               
                StartCoroutine("Vibration");//振動させる

            }
            if (rightRotationAngle1 - rightRotationAngle2 > 18)
            {

                rightRotationAngle2 -= rotationAjustment;
                StartCoroutine("Vibration");//振動させる
            }
            dataUpdate();
        }
        if (Input.GetKeyDown("joystick button 3"))//Y

        {
            rightSliderForward1 += sliderAjustment;
            if (rightSliderForward1 > maxPosition)
            {
                rightSliderForward1 -= sliderAjustment;
                StartCoroutine("Vibration");//振動させる
            }
            
            dataUpdate();
        }


         lsh = Input.GetAxis("L_Stick_H");
         lsv = Input.GetAxis("L_Stick_V");
        if(Mathf.Abs(lsh) <= 0.3)
        {
            stick1 = true;
        }
        if (stick1)
        {
            if (lsh >= 0.8)
            {
                leftRotationAngle1 += rotationAjustment;
                dataUpdate();
                stick1 = false;
                if (leftRotationAngle1 - leftRotationAngle2 > 18)
                {

                    leftRotationAngle1 -= rotationAjustment;
                    StartCoroutine("Vibration");//振動させる
                }
            }
            if (lsh <= -0.8)
            {
                leftRotationAngle1 -= rotationAjustment;
                if(Mathf.Abs(rightRotationAngle1 - leftRotationAngle2) >9)

                {
                leftRotationAngle1 += rotationAjustment;
               
                StartCoroutine("Vibration");//振動させる

                }
            if (leftRotationAngle1 - leftRotationAngle2 > 18)
            {

                leftRotationAngle1 += rotationAjustment;
                StartCoroutine("Vibration");//振動させる
                }
                dataUpdate();
                stick1 = false;
            }
        }
        if (Mathf.Abs(lsv) <= 0.3)
        {
            stick2 = true;
        }
        if (stick2) { 
            if (lsv >= 0.8)
            {
                leftSliderForward1 += sliderAjustment;
                if (leftSliderForward1 > maxPosition)
                {
                    leftSliderForward1 -= sliderAjustment;
                    StartCoroutine("Vibration");//振動させる
                }
                dataUpdate();
                stick2 = false;

            }
            if (lsv <= -0.8)
            {
                leftSliderForward1 -= sliderAjustment;
                if (leftSliderForward1 - leftSliderBackward1 < 0)
                {
                    leftSliderForward1 += sliderAjustment;
                    StartCoroutine("Vibration");//振動させる
                }
                dataUpdate();
                stick2 = false;
            }
        }



            //R Stick
         rsh = Input.GetAxis("R_Stick_H");
         rsv = Input.GetAxis("R_Stick_V");
        if (Mathf.Abs(rsh) <= 0.3)
        {
            stick3 = true;
        }
        if (stick3)
        {
            if (rsh >= 0.8)
            {
                rightRotationAngle2 += rotationAjustment;
                
                if (rightRotationAngle1 - rightRotationAngle2 > 18)
                {

                    rightRotationAngle2 -= rotationAjustment;
                    StartCoroutine("Vibration");//振動させる
                }
                dataUpdate();
                stick3 = false;
            }
            if (rsh <= -0.8)
            {
                rightRotationAngle2 -= rotationAjustment;
                if (rightRotationAngle1 - rightRotationAngle2 > 18)
                {

                    rightRotationAngle2 += rotationAjustment;
                    StartCoroutine("Vibration");//振動させる
                }
                dataUpdate();
                stick3 = false;
            }
        }
        if (Mathf.Abs(rsv) <= 0.3)
        {
            stick4 = true;
        }
        if (stick4)
        {
            if (rsv >= 0.8)
            {
                rightSliderBackward1 += sliderAjustment;
                if (rightSliderForward1 - rightSliderBackward1 < 0)
                {
                    rightSliderBackward1 -= sliderAjustment;
                    StartCoroutine("Vibration");//振動させる
                }
                dataUpdate();
                stick4 = false;
            }
            if (rsv <= -0.8)
            {
                rightSliderBackward1 -= sliderAjustment;
                if (rightSliderBackward1 < minPosition)
                {
                    rightSliderBackward1 += sliderAjustment;
                    StartCoroutine("Vibration");//振動させる
                }
                dataUpdate();
                stick4 = false;
            }
        }

        //D-Pad
        float dph = Input.GetAxis("D_Pad_H");
        float dpv = Input.GetAxis("D_Pad_V");
        if (dph == 0)
        {
            stick5 = true;
        }
        if (stick5)
        {
            if (dph >= 1)
            {
                leftRotationAngle2 += rotationAjustment;
                if (leftRotationAngle1 - leftRotationAngle2 > 18)
                {

                    leftRotationAngle2 -= rotationAjustment;
                    StartCoroutine("Vibration");//振動させる
                }
                dataUpdate();
                stick5 = false;
            }
            if (dph <= -1)
            {
                leftRotationAngle2 -= rotationAjustment;
                if (leftRotationAngle1 - leftRotationAngle2 > 18)
                {

                    leftRotationAngle2 += rotationAjustment;
                    StartCoroutine("Vibration");//振動させる
                }
                if (Mathf.Abs(rightRotationAngle1 - leftRotationAngle2) > 9)
                {
                    leftRotationAngle2 += rotationAjustment;
                    StartCoroutine("Vibration");//振動させる

                }
                dataUpdate();
                stick5 = false;
            }
        }
        if (dpv == 0)
        {
            stick6 = true;
        }
        if (stick6)
        {
            if (dpv >= 1)
            {
                leftSliderBackward1 -= sliderAjustment;
                if (leftSliderBackward1 < minPosition)
                {
                    leftSliderBackward1 += sliderAjustment;
                    StartCoroutine("Vibration");//振動させる
                }

                dataUpdate();
                stick6 = false;

            }
            if (dpv <= -1)
            {
                
                leftSliderBackward1 += sliderAjustment;
                if (leftSliderForward1 - leftSliderBackward1 < 0)
                {
                    leftSliderBackward1 -= sliderAjustment;
                    StartCoroutine("Vibration");//振動させる
                }
                dataUpdate();
                stick6 = false;
            }
        }*/
      
    }
    
    void dataUpdate()
    {

        targetCalculate();//目標値計算
                          //送信するデータを文字列でまとめる
        sendText = "update" + ",";
        for (int i = 0; i < numMotor; i++)
        {
            sendText += targetPulseUp1[i].ToString() + "," + targetPulseDown1[i].ToString() + ","+ targetPulseUp2[i].ToString() + "," + targetPulseDown2[i].ToString() + ",";
            sendText += driveTimeUp1[i].ToString() + "," + driveTimeDown1[i].ToString() + ","+ driveTimeUp2[i].ToString() + "," + driveTimeDown2[i].ToString() + ",";
            sendText += delayTimeUp1[i].ToString() + "," + delayTimeDown1[i].ToString() + ","+ delayTimeUp2[i].ToString() + "," + delayTimeDown2[i].ToString() + ",";
            sendText += delayTimeFirst[i].ToString() + ",";
        }
        sendText += "/";//終わりの目印
        byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText);//送信する文字列をbyteに変換
        client.Send(sendByte, sendByte.Length);//送信
        Debug.Log(sendText);
        update = false;

    }

    void OnApplicationQuit()
    {
        if (write)
        {
            saveData3 = Motor1Up1.ToString() + "," + Motor1Up2.ToString() + ",";
            saveData3 += Motor2Up1.ToString() + "," + Motor2Up2.ToString() + ",";
            saveData3 += Motor3Up1.ToString() + "," + Motor3Up2.ToString() + ",";
            sw3.WriteLine(saveData3);
            sw3.Close();
            write = false;
            Debug.Log("SaveData");
        }
        if (walk)
        {
            sendText = "stop" + "," + "/";

            byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText);//送信する文字列をbyteに変換
            client.Send(sendByte, sendByte.Length);//送信
            Debug.Log(sendText);
        }
       

    }
    void targetCalculate()//パラメータ　→ 振幅値（mm）→ 出力パルス変換
    {
        if (setParameter)//パラメータ　→ 振幅値（mm）
        {            
            SetParameterToLength();
        }

        //ペダル目標値計算
        Motor1Up1_f = Motor1Up1 / motorResolution;
        Motor1Down1_f = Motor1Down1 / motorResolution;
        Motor1Up2_f = Motor1Up2 / motorResolution;
        Motor1Down2_f = Motor1Down2 / motorResolution;

        Motor2Up1_f = Motor2Up1 / motorResolution;
        Motor2Down1_f = Motor2Down1 / motorResolution;
        Motor2Up2_f = Motor2Up2 / motorResolution;
        Motor2Down2_f = Motor2Down2 / motorResolution;

        Motor3Up1_f = Motor3Up1 / motorResolution;
        Motor3Down1_f = Motor3Down1 / motorResolution;
        Motor3Up2_f = Motor3Up2 / motorResolution;
        Motor3Down2_f = Motor3Down2 / motorResolution;


        //目標パルスを整数型で格納
        targetPulseUp1[0] = (int)Motor1Up1_f;
        targetPulseDown1[0] = (int)Motor1Down1_f;
        targetPulseUp2[0] = (int)Motor1Up2_f;
        targetPulseDown2[0] = (int)Motor1Down2_f;
        targetPulseUp1[1] = (int)Motor2Up1_f;
        targetPulseDown1[1] = (int)Motor2Down1_f;
        targetPulseUp2[1] = (int)Motor2Up2_f;
        targetPulseDown2[1] = (int)Motor2Down2_f;
        targetPulseUp1[2] = (int)Motor3Up1_f;
        targetPulseDown1[2] = (int)Motor3Down1_f;
        targetPulseUp2[2] = (int)Motor3Up2_f;
        targetPulseDown2[2] = (int)Motor3Down2_f;


    }



    public void Play()//動画再生用
    {
        foreach(VideoPlayer player in playList)
        {
            if (!player.isPlaying)
            {
                player.Play();
            }
        }
    }

    public void Pause()
    {
        foreach (VideoPlayer player in playList)
        {
            if (player.isPlaying)
            {
                player.Pause();
            }
        }
    }

    void SetParameterToLength()
    {
        //deg->rad変換
        rad_P = (pitch * Math.PI) / 180;
        rad_R = (roll * Math.PI) / 180;
        rad_Y = (yaw * Math.PI) / 180;

        //回転行列
        // a b c
        // d e f 
        // g h i
        ///////////
        double a = Math.Cos(rad_Y) * Math.Cos(rad_P);
        double b = Math.Cos(rad_Y) * Math.Sin(rad_P) * Math.Sin(rad_R) - Math.Sin(rad_Y) * Math.Cos(rad_R);
        double c = Math.Cos(rad_Y) * Math.Sin(rad_P) * Math.Cos(rad_R) + Math.Sin(rad_Y) * Math.Sin(rad_R);
        double d = Math.Sin(rad_Y) * Math.Cos(rad_P);
        double e = Math.Sin(rad_Y) * Math.Sin(rad_P) * Math.Sin(rad_R) + Math.Cos(rad_Y) * Math.Cos(rad_R);
        double f = Math.Sin(rad_Y) * Math.Sin(rad_P) * Math.Cos(rad_R) - Math.Cos(rad_Y) * Math.Sin(rad_R);
        double g = -Math.Sin(rad_P);
        double h = Math.Cos(rad_P) * Math.Sin(rad_R);
        double i = Math.Cos(rad_P) * Math.Cos(rad_R);

        z_c = l0 + lift;

        F_1 = Math.Pow(Math.Pow((a - 1) * 160, 2.0) + Math.Pow((d * 160), 2.0) + Math.Pow(g * 160 + z_c, 2.0), 0.5) - l0;
        F_2 = Math.Pow(Math.Pow((-a + b + 1) * 160, 2.0) + Math.Pow(((-d + e - 1) * 160), 2.0) + Math.Pow((-g + h) * 160 + z_c, 2.0), 0.5) - l0;
        F_3 = Math.Pow(Math.Pow((-a - b + 1) * 160, 2.0) + Math.Pow(((-d - e + 1) * 160), 2.0) + Math.Pow((-g - h) * 160 + z_c, 2.0), 0.5) - l0;

        /*//回転行列
        
        rotationArray = { { Math.Cos(rad_Y) * Math.Cos(rad_P), Math.Cos(rad_Y) * Math.Sin(rad_P) * Math.Sin(rad_R) - Math.Sin(rad_Y) * Math.Cos(rad_R), Math.Cos(rad_Y) * Math.Sin(rad_P) * Math.Cos(rad_R) + Math.Sin(rad_Y) * Math.Sin(rad_R) }, { Math.Sin(rad_Y) * Math.Cos(rad_P), Math.Sin(rad_Y) * Math.Sin(rad_P) * Math.Sin(rad_R) + Math.Cos(rad_Y) * Math.Cos(rad_R), Math.Sin(rad_Y) * Math.Sin(rad_P) * Math.Cos(rad_R) - Math.Cos(rad_Y) * Math.Sin(rad_R) }, { -Math.Sin(rad_P), Math.Cos(rad_P) * Math.Sin(rad_R), Math.Cos(rad_P) * Math.Cos(rad_R) } };
        z_c = l0 + lift;

        F_1 = Math.Pow(Math.Pow((rotationArray[0][0] - 1) * 160, 2.0) + Math.Pow((rotationArray[1][0] * 160), 2.0) + Math.Pow(rotationArray[2][0] * 160 + z_c, 2.0), 0.5) - l0; 
        F_2 = Math.Pow(Math.Pow((-rotationArray[0][0] + rotationArray[0][1] + 1) * 160, 2.0) + Math.Pow(((-rotationArray[1][0] + rotationArray[1][1] - 1) * 160), 2.0) + Math.Pow((-rotationArray[2][0] + rotationArray[2][1]) * 160 + z_c, 2.0), 0.5) - l0; 
        F_3 = Math.Pow(Math.Pow((-rotationArray[0][0] - rotationArray[0][1] + 1) * 160, 2.0) + Math.Pow(((-rotationArray[1][0] - rotationArray[1][1] + 1) * 160), 2.0) + Math.Pow((-rotationArray[2][0] - rotationArray[2][1]) * 160 + z_c, 2.0), 0.5) - l0; 

         //各軸の速度値設定  // [R]と[L]は[F]との移動距離に比例した値?
        double sokudo_F_up = Math.Abs(liftSpeed * (F_1 / 1.26));//shinpuku_lift));//前方アクチュエータ上昇速度の計算[m/s]
        double sokudo_R_up = Math.Abs(liftSpeed * (F_2 / 1.26));//shinpuku_lift));//右後方アクチュエータ上昇速度の計算
        double sokudo_L_up = Math.Abs(liftSpeed * (F_3 / 1.26));//shinpuku_lift));//左後方アクチュエータ上昇速度の計算
*/

        // 右足から始める場合は最初から値をスワップする
        if (rightFoot)
        {
            double xchange = F_2;
            F_2 = F_3;
            F_3 = xchange;
        }

        Motor1Up1 = (float)F_1;
        Motor2Up1 = (float)F_2;
        Motor3Up1 = (float)F_3;
        Motor1Up2 = (float)F_1;
        Motor2Up2 = (float)F_3;
        Motor3Up2 = (float)F_2;

    }
}


