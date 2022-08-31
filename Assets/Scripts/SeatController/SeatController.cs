using System;
using System.Net.Sockets;
using UnityEngine;

//6モータ用
//一歩目遅延時間もパラメータ通信する
//目標設定2分割->4分割に変更
//UDPReceiver（グラフ描画，csv保存）なし
//動画付き調整法用, 

public class SeatController : SeatBaseController
{
    [SerializeField] WalkDemoMainController mainController;
    public string host = "192.168.11.35";
    public int port = 61000;
    private UdpClient client;

    //public WalkTimer walkTimer;
    public string sendText;

    public float walkCycle; // = 1400; //歩行周期[ms]
    //float WalkTimer;


    /// リフト・ロール・ピッチ・ヨーの計算
    [SerializeField, Header("Parameter")]
    public double lift = 1.26;

    public double pitch = 0.13;
    public double roll = 0.15;

    public double yaw = 0;
    //public double liftSpeed = 6.74;

    public bool setParameter = true; // パラメータ　→ 振幅値（mm）
    public bool rightFoot; // 右足から始める場合は最初から値をスワップする
    double rad_P;
    double rad_R;
    double rad_Y;
    private int l0 = 510; //座高
    private double z_c;

    //private
    public int step; //ウォッシュバックの歩数(周期)
    public bool Left;
    public bool Right;
    public bool Back;

    //Yaw回転計算用
    double outer_deg = 3.19;
    double inner_deg = 2.57;
    double outer_ratio = 0.45;

    double inner_ratio = 0.46;

    //左足用
    double yaw_1;
    double F_1;
    double F_2;

    double F_3;

    //右足用
    double yaw_2;
    double F_4;
    double F_5;
    double F_6;


    private int numMotor = 3; //使用するモータの数

    private float Motor1Up1_f = 0f; //No.1モータの目標パルス格納用（小数で）
    private float Motor1Down1_f = 0f; //No.1モータの目標パルス格納用（小数で）
    private float Motor1Up2_f = 0f; //No.1モータの目標パルス格納用（小数で）
    private float Motor1Down2_f = 0f; //No.1モータの目標パルス格納用（小数で）
    private float Motor2Up1_f = 0f; //No.2モータの目標パルス格納用（小数で）
    private float Motor2Down1_f = 0f; //No.2モータの目標パルス格納用（小数で）
    private float Motor2Up2_f = 0f; //No.2モータの目標パルス格納用（小数で）
    private float Motor2Down2_f = 0f; //No.2モータの目標パルス格納用（小数で）
    private float Motor3Up1_f = 0f; //No.3モータの目標パルス格納用（小数で）
    private float Motor3Down1_f = 0f; //No.3モータの目標パルス格納用（小数で）
    private float Motor3Up2_f = 0f; //No.3モータの目標パルス格納用（小数で）
    private float Motor3Down2_f = 0f; //No.3モータの目標パルス格納用（小数で）

    private const float motorResolution = 0.01f; //[mm/pulse]

    /// <summary>
    /// アクチュエーターの長さ設定(mm)
    /// </summary>
    [SerializeField, Header("Length"), Range(-50, 50)] //スライダの範囲を設定
    float Motor1Up1 = 3; //モータ1上昇目標値1[mm]

    [SerializeField, Range(-50, 50)] //スライダの範囲を設定
    float Motor1Down1 = 0; //モータ1下降目標値1[mm]

    [SerializeField, Range(-50, 50)] //スライダの範囲を設定
    float Motor1Up2 = 3; //モータ1上昇目標値2[mm]

    [SerializeField, Range(-50, 50)] //スライダの範囲を設定
    float Motor1Down2 = 0; //モータ1下降目標値2[mm]

    [SerializeField, Range(-50, 50)] //スライダの範囲を設定
    float Motor2Up1 = 3; //モータ1上昇目標値1[mm]

    [SerializeField, Range(-50, 50)] //スライダの範囲を設定
    float Motor2Down1 = 0; //モータ1下降目標値1[mm]

    [SerializeField, Range(-50, 50)] //スライダの範囲を設定
    float Motor2Up2 = 3; //モータ1上昇目標値2[mm]

    [SerializeField, Range(-50, 50)] //スライダの範囲を設定
    float Motor2Down2 = 0; //モータ1下降目標値2[mm]

    [SerializeField, Range(-50, 50)] //スライダの範囲を設定
    float Motor3Up1 = 3; //モータ1上昇目標値1[mm]

    [SerializeField, Range(-50, 50)] //スライダの範囲を設定
    float Motor3Down1 = 0; //モータ1下降目標値1[mm]

    [SerializeField, Range(-50, 50)] //スライダの範囲を設定
    float Motor3Up2 = 3; //モータ1上昇目標値2[mm]

    [SerializeField, Range(-50, 50)] //スライダの範囲を設定
    float Motor3Down2 = 0; //モータ1下降目標値2[mm]

    //出力パルス（送信）
    public int[] targetPulseUp1 = new int[3] {0, 0, 0}; //上昇時の目標パルス1（No.1,No.2,No.3）[pulse]
    public int[] targetPulseDown1 = new int[3] {0, 0, 0}; //下降時の目標パルス1（No.1,No.2,No.3）[pulse]
    public int[] targetPulseUp2 = new int[3] {0, 0, 0}; //上昇時の目標パルス1（No.1,No.2,No.3）[pulse]

    public int[] targetPulseDown2 = new int[3] {0, 0, 0}; //下降時の目標パルス1（No.1,No.2,No.3）[pulse]

    //駆動時間(送信）
    public int[] driveTimeUp1 = new int[3] {350, 350, 350}; //上昇の駆動時間1（No.1,No.2,No.3）[ms]
    public int[] driveTimeDown1 = new int[3] {350, 350, 350}; //下降の駆動時間1（No.1,No.2,No.3）[ms]
    public int[] driveTimeUp2 = new int[3] {350, 350, 350}; //上昇の駆動時間1（No.1,No.2,No.3）[ms]

    public int[] driveTimeDown2 = new int[3] {350, 350, 350}; //下降の駆動時間1（No.1,No.2,No.3）[ms]

    //待機時間（送信）
    public int[] delayTimeUp1 = new int[3] {0, 0, 0}; //上昇始めモータ停止時間（No.1,No.2,No.3）[ms]
    public int[] delayTimeDown1 = new int[3] {0, 0, 0}; //下降始めモータ停止時間（No.1,No.2,No.3）[ms]
    public int[] delayTimeUp2 = new int[3] {0, 0, 0}; //上昇始めモータ停止時間（No.1,No.2,No.3）[ms]
    public int[] delayTimeDown2 = new int[3] {0, 0, 0}; //下降始めモータ停止時間（No.1,No.2,No.3）[ms]
    public int[] delayTimeFirst = new int[3] {0, 0, 0}; //一歩目モータ停止時間（No.1,No.2,No.3）[ms]

    public bool walk;


    public int[] free = new int[3] {0, 0, 0};
    public bool clear;
    private int cleear_st;
    public bool home;
    private int home_st;


    // Use this for initialization
    void Start()
    {
        client = new UdpClient();
        client.Connect(host, port); //UDP接続
    }

    // Update is called once per frame
    void Update()
    {
        if (Left)
        {
            walkCycle += Time.deltaTime;
            if (walkCycle > 1.4)
            {
                WalkLeft();
                Debug.Log("WalkLeft" + walkCycle);
            }
        }

        if (Right)
        {
            walkCycle += Time.deltaTime;
            if (walkCycle > 1.4)
            {
                WalkRight();
                walkCycle = 0;
                Debug.Log("WalkRight" + walkCycle);
            }
        }

        if (Back)
        {
            if (step < 3)
            {
                walkCycle += Time.deltaTime;
                if (walkCycle > 1.4)
                {
                    WalkBack();
                    walkCycle = 0;
                    step++;
                    Debug.Log("WalkBack" + walkCycle);
                }
            }
            else
            {
                yaw = 0;
                Back = false;
                WalkStraight();
            }
        }

        if (Input.GetKeyDown(KeyCode.U)) //パラメータ変更
        {
            dataUpdate();
        }

        if (Input.GetKeyDown(KeyCode.F)) //レンジブレーキクリア
        {
            sendText = "free" + ",";
            sendText += free[0].ToString() + "," + free[1].ToString() + "," + free[2].ToString() + ",";
            sendText += "/";

            byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText); //送信する文字列をbyteに変換
            client.Send(sendByte, sendByte.Length); //送信
            Debug.Log(sendText);
        }

        if (Input.GetKeyDown(KeyCode.C)) //アラーム解除
        {
            cleear_st = 0;

            if (clear)
            {
                cleear_st = 1;
            }

            sendText = "clear" + ",";
            sendText += cleear_st + "," + cleear_st + "," + cleear_st + ",";
            sendText += "/";

            byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText); //送信する文字列をbyteに変換
            client.Send(sendByte, sendByte.Length); //送信
            Debug.Log(sendText);
        }

        if (Input.GetKeyDown(KeyCode.H)) //ホーム復帰
        {
            home_st = 0;
            if (home)
            {
                home_st = 1;
            }

            sendText = "home" + ",";
            sendText += home_st + "," + home_st + "," + home_st + ",";
            sendText += "/";

            byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText); //送信する文字列をbyteに変換
            client.Send(sendByte, sendByte.Length); //送信
            Debug.Log(sendText);
        }

        if (Input.GetKeyDown(KeyCode.B)) //正面に戻す
        {
            targetCalculate(); //目標値計算
            //送信するデータを文字列でまとめる
            sendText = "reset" + ",";
            sendText += "/"; //終わりの目印
            byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText); //送信する文字列をbyteに変換
            client.Send(sendByte, sendByte.Length); //送信
            Debug.Log(sendText);
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            sendText = "stop" + "," + "/";

            byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText); //送信する文字列をbyteに変換
            client.Send(sendByte, sendByte.Length); //送信
            Debug.Log(sendText);
        }
    }

    void dataUpdate()
    {
        targetCalculate(); //目標値計算
        //送信するデータを文字列でまとめる
        sendText = "update" + ",";
        for (int i = 0; i < numMotor; i++)
        {
            sendText += targetPulseUp1[i].ToString() + "," + targetPulseDown1[i].ToString() + "," +
                        targetPulseUp2[i].ToString() + "," + targetPulseDown2[i].ToString() + ",";
            sendText += driveTimeUp1[i].ToString() + "," + driveTimeDown1[i].ToString() + "," +
                        driveTimeUp2[i].ToString() + "," + driveTimeDown2[i].ToString() + ",";
            sendText += delayTimeUp1[i].ToString() + "," + delayTimeDown1[i].ToString() + "," +
                        delayTimeUp2[i].ToString() + "," + delayTimeDown2[i].ToString() + ",";
            sendText += delayTimeFirst[i].ToString() + ",";
        }

        sendText += "/"; //終わりの目印
        byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText); //送信する文字列をbyteに変換
        client.Send(sendByte, sendByte.Length); //送信
        Debug.Log(sendText);
    }

    public override void WalkStop()
    {
        Left = false;
        Right = false;

        sendText = "stop" + "," + "/";

        byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText); //送信する文字列をbyteに変換
        client.Send(sendByte, sendByte.Length); //送信
        Debug.Log(sendText);
        walk = false;
        command = true; //mainController返信用
    }

    void OnApplicationQuit()
    {
        if (walk)
        {
            sendText = "stop" + "," + "/";

            byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText); //送信する文字列をbyteに変換
            client.Send(sendByte, sendByte.Length); //送信
            Debug.Log(sendText);
        }
    }

    void targetCalculate() //パラメータ　→ 振幅値（mm）→ 出力パルス変換
    {
        if (setParameter) //パラメータ　→ 振幅値（mm）
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
        targetPulseUp1[0] = (int) Motor1Up1_f;
        targetPulseDown1[0] = (int) Motor1Down1_f;
        targetPulseUp2[0] = (int) Motor1Up2_f;
        targetPulseDown2[0] = (int) Motor1Down2_f;
        targetPulseUp1[1] = (int) Motor2Up1_f;
        targetPulseDown1[1] = (int) Motor2Down1_f;
        targetPulseUp2[1] = (int) Motor2Up2_f;
        targetPulseDown2[1] = (int) Motor2Down2_f;
        targetPulseUp1[2] = (int) Motor3Up1_f;
        targetPulseDown1[2] = (int) Motor3Down1_f;
        targetPulseUp2[2] = (int) Motor3Up2_f;
        targetPulseDown2[2] = (int) Motor3Down2_f;
    }


    void SetParameterToLength()
    {
        yaw = yaw + yaw_1;

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

        F_1 = Math.Pow(Math.Pow((a - 1) * 160, 2.0) + Math.Pow((d * 160), 2.0) + Math.Pow(g * 160 + z_c, 2.0), 0.5) -
              l0;
        F_2 = Math.Pow(
            Math.Pow((-a + b + 1) * 160, 2.0) + Math.Pow(((-d + e - 1) * 160), 2.0) +
            Math.Pow((-g + h) * 160 + z_c, 2.0), 0.5) - l0;
        F_3 = Math.Pow(
            Math.Pow((-a - b + 1) * 160, 2.0) + Math.Pow(((-d - e + 1) * 160), 2.0) +
            Math.Pow((-g - h) * 160 + z_c, 2.0), 0.5) - l0;


        //回転行列
        // a b c
        // d e f 
        // g h i
        ///////////
        yaw = yaw + yaw_2;
        rad_Y = (yaw * Math.PI) / 180;

        a = Math.Cos(rad_Y) * Math.Cos(rad_P);
        b = Math.Cos(rad_Y) * Math.Sin(rad_P) * Math.Sin(rad_R) - Math.Sin(rad_Y) * Math.Cos(rad_R);
        c = Math.Cos(rad_Y) * Math.Sin(rad_P) * Math.Cos(rad_R) + Math.Sin(rad_Y) * Math.Sin(rad_R);
        d = Math.Sin(rad_Y) * Math.Cos(rad_P);
        e = Math.Sin(rad_Y) * Math.Sin(rad_P) * Math.Sin(rad_R) + Math.Cos(rad_Y) * Math.Cos(rad_R);
        f = Math.Sin(rad_Y) * Math.Sin(rad_P) * Math.Cos(rad_R) - Math.Cos(rad_Y) * Math.Sin(rad_R);
        //double g = -Math.Sin(rad_P);
        //double h = Math.Cos(rad_P) * Math.Sin(rad_R);
        //double i = Math.Cos(rad_P) * Math.Cos(rad_R);


        F_4 = Math.Pow(Math.Pow((a - 1) * 160, 2.0) + Math.Pow((d * 160), 2.0) + Math.Pow(g * 160 + z_c, 2.0), 0.5) -
              l0;
        F_5 = Math.Pow(
            Math.Pow((-a + b + 1) * 160, 2.0) + Math.Pow(((-d + e - 1) * 160), 2.0) +
            Math.Pow((-g + h) * 160 + z_c, 2.0), 0.5) - l0;
        F_6 = Math.Pow(
            Math.Pow((-a - b + 1) * 160, 2.0) + Math.Pow(((-d - e + 1) * 160), 2.0) +
            Math.Pow((-g - h) * 160 + z_c, 2.0), 0.5) - l0;


        /* //各軸の速度値設定  // [R]と[L]は[F]との移動距離に比例した値?
        double sokudo_F_up = Math.Abs(liftSpeed * (F_1 / 1.26));//shinpuku_lift));//前方アクチュエータ上昇速度の計算[m/s]
        double sokudo_R_up = Math.Abs(liftSpeed * (F_2 / 1.26));//shinpuku_lift));//右後方アクチュエータ上昇速度の計算
        double sokudo_L_up = Math.Abs(liftSpeed * (F_3 / 1.26));//shinpuku_lift));//左後方アクチュエータ上昇速度の計算*/

        // 右足から始める場合は最初から値をスワップする
        if (rightFoot)
        {
            double xchange = F_2;
            F_2 = F_3;
            F_3 = xchange;
            double xchange2 = F_5;
            F_5 = F_6;
            F_6 = xchange2;
        }

        Motor1Up1 = (float) F_1;
        Motor2Up1 = (float) F_2;
        Motor3Up1 = (float) F_3;
        Motor1Up2 = (float) F_4;
        Motor2Up2 = (float) F_6;
        Motor3Up2 = (float) F_5;
    }

    public override void WalkStraight()
    {
        Left = false;
        Right = false;
        yaw_1 = 0;
        yaw_2 = 0;

        targetCalculate(); //目標値計算

        //送信するデータを文字列でまとめる
        if (!walk)
        {
            sendText = "start" + ",";
        }
        else
        {
            sendText = "update" + ",";
        }

        for (int i = 0; i < numMotor; i++)
        {
            sendText += targetPulseUp1[i].ToString() + "," + targetPulseDown1[i].ToString() + "," +
                        targetPulseUp2[i].ToString() + "," + targetPulseDown2[i].ToString() + ",";
            sendText += driveTimeUp1[i].ToString() + "," + driveTimeDown1[i].ToString() + "," +
                        driveTimeUp2[i].ToString() + "," + driveTimeDown2[i].ToString() + ",";
            sendText += delayTimeUp1[i].ToString() + "," + delayTimeDown1[i].ToString() + "," +
                        delayTimeUp2[i].ToString() + "," + delayTimeDown2[i].ToString() + ",";
            sendText += delayTimeFirst[i].ToString() + ",";
        }

        sendText += "/"; //終わりの目印
        byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText); //送信する文字列をbyteに変換
        client.Send(sendByte, sendByte.Length); //送信
        Debug.Log(sendText);
        walk = true;
        /* startTime = Time.time;
         walk = true;
         start = false;
         Play();*/
    }

    public override void WalkLeft()
    {
        Left = true;
        Right = false;
        yaw_1 = inner_deg * inner_ratio;
        yaw_2 = outer_deg * outer_ratio;

        targetCalculate(); //目標値計算

        //送信するデータを文字列でまとめる
        if (!walk)
        {
            sendText = "start" + ",";
        }
        else
        {
            sendText = "update" + ",";
        }

        for (int i = 0; i < numMotor; i++)
        {
            sendText += targetPulseUp1[i].ToString() + "," + targetPulseDown1[i].ToString() + "," +
                        targetPulseUp2[i].ToString() + "," + targetPulseDown2[i].ToString() + ",";
            sendText += driveTimeUp1[i].ToString() + "," + driveTimeDown1[i].ToString() + "," +
                        driveTimeUp2[i].ToString() + "," + driveTimeDown2[i].ToString() + ",";
            sendText += delayTimeUp1[i].ToString() + "," + delayTimeDown1[i].ToString() + "," +
                        delayTimeUp2[i].ToString() + "," + delayTimeDown2[i].ToString() + ",";
            sendText += delayTimeFirst[i].ToString() + ",";
        }

        sendText += "/"; //終わりの目印
        byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText); //送信する文字列をbyteに変換
        client.Send(sendByte, sendByte.Length); //送信
        Debug.Log(sendText);
        walk = true;
        walkCycle = 0;
        command = true; //mainController返信用
    }

    public override void WalkRight()
    {
        yaw_1 = -outer_deg * outer_ratio;
        yaw_2 = -inner_deg * inner_ratio;

        targetCalculate(); //目標値計算

        //送信するデータを文字列でまとめる
        if (!walk)
        {
            sendText = "start" + ",";
        }
        else
        {
            sendText = "update" + ",";
        }

        for (int i = 0; i < numMotor; i++)
        {
            sendText += targetPulseUp1[i].ToString() + "," + targetPulseDown1[i].ToString() + "," +
                        targetPulseUp2[i].ToString() + "," + targetPulseDown2[i].ToString() + ",";
            sendText += driveTimeUp1[i].ToString() + "," + driveTimeDown1[i].ToString() + "," +
                        driveTimeUp2[i].ToString() + "," + driveTimeDown2[i].ToString() + ",";
            sendText += delayTimeUp1[i].ToString() + "," + delayTimeDown1[i].ToString() + "," +
                        delayTimeUp2[i].ToString() + "," + delayTimeDown2[i].ToString() + ",";
            sendText += delayTimeFirst[i].ToString() + ",";
        }

        sendText += "/"; //終わりの目印
        byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText); //送信する文字列をbyteに変換
        client.Send(sendByte, sendByte.Length); //送信
        Debug.Log(sendText);
        walk = true;
        walkCycle = 0;
        command = true; //mainController返信用
    }

    void WalkStop1()
    {
        yaw_1 = 0;
        yaw_2 = 0;

        targetCalculate(); //目標値計算

        //送信するデータを文字列でまとめる
        sendText = "right" + ",";
        for (int i = 0; i < numMotor; i++)
        {
            sendText += targetPulseUp1[i].ToString() + "," + targetPulseDown1[i].ToString() + "," +
                        targetPulseUp2[i].ToString() + "," + targetPulseDown2[i].ToString() + ",";
            sendText += driveTimeUp1[i].ToString() + "," + driveTimeDown1[i].ToString() + "," +
                        driveTimeUp2[i].ToString() + "," + driveTimeDown2[i].ToString() + ",";
            sendText += delayTimeUp1[i].ToString() + "," + delayTimeDown1[i].ToString() + "," +
                        delayTimeUp2[i].ToString() + "," + delayTimeDown2[i].ToString() + ",";
            sendText += delayTimeFirst[i].ToString() + ",";
        }

        sendText += "/"; //終わりの目印
        byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText); //送信する文字列をbyteに変換
        client.Send(sendByte, sendByte.Length); //送信
        Debug.Log(sendText);
        walk = false;
    }

    public override void WalkBack()
    {
        Left = false;
        Right = false;
        Back = true;

        yaw_1 = -yaw / 6;
        yaw_2 = yaw_1;
        targetCalculate(); //目標値計算

        //送信するデータを文字列でまとめる
        if (!walk)
        {
            sendText = "start" + ",";
        }
        else
        {
            sendText = "update" + ",";
        }

        for (int i = 0; i < numMotor; i++)
        {
            sendText += targetPulseUp1[i].ToString() + "," + targetPulseDown1[i].ToString() + "," +
                        targetPulseUp2[i].ToString() + "," + targetPulseDown2[i].ToString() + ",";
            sendText += driveTimeUp1[i].ToString() + "," + driveTimeDown1[i].ToString() + "," +
                        driveTimeUp2[i].ToString() + "," + driveTimeDown2[i].ToString() + ",";
            sendText += delayTimeUp1[i].ToString() + "," + delayTimeDown1[i].ToString() + "," +
                        delayTimeUp2[i].ToString() + "," + delayTimeDown2[i].ToString() + ",";
            sendText += delayTimeFirst[i].ToString() + ",";
        }

        sendText += "/"; //終わりの目印
        byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText); //送信する文字列をbyteに変換
        client.Send(sendByte, sendByte.Length); //送信
        Debug.Log(sendText);
        walk = true;
        step = 1;
        command = true; //mainController返信用
    }

    //IEnumerator WalkToLeft()
    //{
    //    Debug.Log("StartLeft");

    //    if (Left)
    //    {
    //    //ここに処理を書く
    //  //  WalkLeft();
    //    //1フレーム停止
    //    yield return new WaitForSeconds(1.4f);

    //        //ここに再開後の処理を書く
    //        WalkLeft();

    //        Debug.Log("Left");
    //    }
    //    else{

    //      Debug.Log("StopLeft");

    //        yield break;

    //    }

    //}

    //IEnumerator WalkToRight()
    //{
    //    Debug.Log("StartRight");
    //    if (Right)
    //    {
    //        //ここに処理を書く
    //        //  WalkLeft();
    //        //1フレーム停止
    //        yield return new WaitForSeconds(1.4f);

    //        //ここに再開後の処理を書く
    //        WalkRight();
    //        Debug.Log("Right");
    //    }
    //    else
    //    {
    //        Debug.Log("StopRight");

    //        yield break;

    //    }

    //}
}