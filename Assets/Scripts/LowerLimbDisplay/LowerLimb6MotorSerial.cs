using UnityEngine;
using System;
using System.IO.Ports;
//6モータ用
//一歩目遅延時間もパラメータ通信する
//目標設定2分割->4分割に変更
//UDPReceiver（グラフ描画，csv保存）なし
//動画付き調整法用, 


public class LowerLimb6MotorSerial : LowerLimb6MotorBase
{
    [SerializeField]
    WalkDemoMainController mainController;
    [SerializeField]
    UDPReceiver udpReceiver;


    public string portName = "COM7";
    public int baudRate = 9600;
    private SerialPort client;

    //public WalkTimer walkTimer;
    public string sendText;

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
    public float seatRotation = 0f;



    [SerializeField, Range(-55, 105)]
    public float leftPedalUp1 = 25;//左ペダル下端上昇目標値[mm]
    [SerializeField, Range(-55, 105)]
    private float leftPedalDown1 = 0;//左ペダル下端下降目標値[mm]
    [SerializeField, Range(-90, 90)]
    float leftSliderForward1 = 36;////左スライダ前進目標値[mm]
    [SerializeField, Range(-90, 90)]
    float leftSliderBackward1 = -24;//左スライダ後退目標値[mm]


    [SerializeField, Range(-55, 105)]
    public float rightPedalUp1 = 25;//右ペダル下端上昇目標値[mm]
    [SerializeField, Range(-55, 105)]
    private float rightPedalDown1 = 0;//右ペダル下端下降目標値[mm]

    [SerializeField, Range(-90, 90)]
    float rightSliderForward1 = 36;//右スライダ後退目標値[mm]
    [SerializeField, Range(-90, 90)]
    float rightSliderBackward1 = -24;//右スライダ後退目標値[mm]

    //Yaw回転
    [SerializeField, Range(-18, 18)]
    float leftRotationAngle1 = -1.5f;//左足前進時Yaw回転角度[degree]
    [SerializeField, Range(-18, 18)]
    float leftRotationAngle2 = 1.5f;//左足後退時Yaw回転角度[degree]

    [SerializeField, Range(-18, 18)]
    float rightRotationAngle1 = -1.5f;//右足前進時Yaw回転角度[degree]
    [SerializeField, Range(-18, 18)]
    float rightRotationAngle2 = 1.5f;//右足後退時Yaw回転角度[degree]
    [SerializeField, Range(-18, 18)]

    //出力パルス（送信）
    public int[] targetPulseUp1 = new int[6] { 0, 0, 0, 0, 0, 0 };//上昇／前進時の目標パルス（左ペダル、左スライダ、右ペダル、右スライダ）[pulse]
    public int[] targetPulseDown1 = new int[6] { 0, 0, 0, 0, 0, 0 };//下降／後退時の目標パルス（左ペダル、左スライダ、右ペダル、右スライダ）[pulse]
                                                                    //駆動時間（送信）
    public int[] driveTimeUp1 = new int[6] { 560, 560, 560, 560, 560, 560 };//上昇／前進時の駆動時間（左ペダル、左スライダ、右ペダル、右スライダ）[ms]
    public int[] driveTimeDown1 = new int[6] { 280, 840, 280, 840, 840, 840 };//下降／後退時の駆動時間（左ペダル、左スライダ、右ペダル、右スライダ）[ms]
                                                                              //待機時間（送信）
    public int[] delayTimeUp1 = new int[6] { 560, 0, 560, 0, 0, 0 };//上昇／前進始めモータ停止時間（左ペダル、左スライダ、右ペダル、右スライダ）[ms]
    public int[] delayTimeDown1 = new int[6] { 0, 0, 0, 0, 0, 0 };//下降／後退始めモータ停止時間（左ペダル、左スライダ、右ペダル、右スライダ）[ms]
    public int[] delayTimeFirst = new int[6] { 0, 280, 700, 980, 280, 980 };//一歩目モータ停止時間（左ペダル、左スライダ、右ペダル、右スライダ）[ms]
    public int seatRotationPulse;
    public bool walk;




    // Use this for initialization
    void Start()
    {
        client = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
        client.Open();
    }



    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))//パラメータ変更
        {
            dataUpdate();

        }

    }
    void dataUpdate()
    {

        targetCalculate();//目標値計算
                          //送信するデータを文字列でまとめる
        sendText = "update" + ",";
        for (int i = 0; i < 6; i++)
        {
            sendText += targetPulseUp1[i].ToString() + "," + targetPulseDown1[i].ToString() + ",";
            sendText += driveTimeUp1[i].ToString() + "," + driveTimeDown1[i].ToString() + ",";
            sendText += delayTimeUp1[i].ToString() + "," + delayTimeDown1[i].ToString() + ",";
            sendText += delayTimeFirst[i].ToString() + ",";
        }
        sendText += "/";//終わりの目印
        byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText);//送信する文字列をbyteに変換
        client.Write(sendByte, 0, sendByte.Length);//送信
        Debug.Log(sendText);

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
        targetPulseUp1[4] = (int)(-leftRotationAngle1 * 10000 * 11 / 120);//回転角度*（駆動モータ1回転のパルス量/モータ1回転でのレール上回転角度）
        targetPulseDown1[4] = (int)(-leftRotationAngle2 * 10000 * 11 / 120);//回転角度*（駆動モータ1回転のパルス量/モータ1回転でのレール上回転角度）
        targetPulseUp1[5] = (int)(-rightRotationAngle1 * 10000 * 11 / 120);//回転角度*（駆動モータ1回転のパルス量/モータ1回転でのレール上回転角度）
        targetPulseDown1[5] = (int)(-rightRotationAngle2 * 10000 * 11 / 120);//回転角度*（駆動モータ1回転のパルス量/モータ1回転でのレール上回転角度）
        seatRotationPulse = (int)(-seatRotation * 10000 * 11 / 120);

    }

    public override void WalkBack()
    {
        //直進パラメータ設定
        leftPedalUp1 = 24;
        leftPedalDown1 = 0;
        leftSliderForward1 = 38;
        leftSliderBackward1 = -22;
        leftRotationAngle1 = 0;
        leftRotationAngle2 = 0;
        rightPedalUp1 = 24;
        rightPedalDown1 = 0;
        rightSliderForward1 = 38;
        rightSliderBackward1 = -22;
        rightRotationAngle1 = 0;
        rightRotationAngle2 = 0;
        seatRotation = 0f;

        targetCalculate();//目標値計算

        //送信するデータを文字列でまとめる

        sendText = "back" + ",";
        for (int i = 0; i < 6; i++)
        {

            sendText += targetPulseUp1[i].ToString() + "," + targetPulseDown1[i].ToString() + ",";
            sendText += driveTimeUp1[i].ToString() + "," + driveTimeDown1[i].ToString() + ",";
            sendText += delayTimeUp1[i].ToString() + "," + delayTimeDown1[i].ToString() + ",";
            sendText += delayTimeFirst[i].ToString() + ",";
        }
        sendText += seatRotationPulse.ToString() + ",";
        sendText += "/";//終わりの目印
        byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText);//送信する文字列をbyteに変換
        client.Write(sendByte, 0, sendByte.Length);//送信
        Debug.Log(sendText);

        walk = true;
        command = true;

    }
    public override void WalkLeft()
    {
        //左旋回パラメータ設定
        leftPedalUp1 = 24;
        leftPedalDown1 = 0;
        leftSliderForward1 = 45.3f;
        leftSliderBackward1 = -48.4f;
        leftRotationAngle1 = 4.08f;
        leftRotationAngle2 = -1.41f;//5.49＝そう振幅
        rightPedalUp1 = 24;
        rightPedalDown1 = 0;
        rightSliderForward1 = 47.8f;
        rightSliderBackward1 = -43.7f;
        rightRotationAngle1 = 4.83f;
        rightRotationAngle2 = 0.61f;
        seatRotation = 3.15f;
        targetCalculate();//目標値計算
        //送信するデータを文字列でまとめる
        if (!walk)
        {
            sendText = "start" + ",";
        }
        else
        {
            sendText = "update" + ",";
        }

        for (int i = 0; i < 6; i++)
        {

            sendText += targetPulseUp1[i].ToString() + "," + targetPulseDown1[i].ToString() + ",";
            sendText += driveTimeUp1[i].ToString() + "," + driveTimeDown1[i].ToString() + ",";
            sendText += delayTimeUp1[i].ToString() + "," + delayTimeDown1[i].ToString() + ",";
            sendText += delayTimeFirst[i].ToString() + ",";
        }
        sendText += seatRotationPulse.ToString() + ",";
        sendText += "/";//終わりの目印
        byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText);//送信する文字列をbyteに変換
        client.Write(sendByte, 0, sendByte.Length);//送信
        Debug.Log(sendText);

        walk = true;
        command = true;//mainController返信用

    }

    public override void WalkRight()
    {
        //右旋回パラメータ設定
        leftPedalUp1 = 24;
        leftPedalDown1 = 0;
        leftSliderForward1 = 47.8f;
        leftSliderBackward1 = -43.7f;
        leftRotationAngle1 = -4.83f;
        leftRotationAngle2 = -0.61f;
        rightPedalUp1 = 24;
        rightPedalDown1 = 0;
        rightSliderForward1 = 45.3f;
        rightSliderBackward1 = -48.3f;
        rightRotationAngle1 = -4.08f;
        rightRotationAngle2 = 1.41f;
        seatRotation = -3.15f;
        targetCalculate();//目標値計算

        //送信するデータを文字列でまとめる
        if (!walk)
        {
            sendText = "start" + ",";
        }
        else
        {
            sendText = "update" + ",";
        }

        for (int i = 0; i < 6; i++)
        {

            sendText += targetPulseUp1[i].ToString() + "," + targetPulseDown1[i].ToString() + ",";
            sendText += driveTimeUp1[i].ToString() + "," + driveTimeDown1[i].ToString() + ",";
            sendText += delayTimeUp1[i].ToString() + "," + delayTimeDown1[i].ToString() + ",";
            sendText += delayTimeFirst[i].ToString() + ",";
        }
        sendText += seatRotationPulse.ToString() + ",";
        sendText += "/";//終わりの目印
        byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText);//送信する文字列をbyteに変換
        client.Write(sendByte, 0, sendByte.Length);//送信
        Debug.Log(sendText);

        walk = true;
        command = true;//mainController返信用
    }
    public override void WalkStop()
    {
        sendText = "stop" + "," + "/";

        byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText);//送信する文字列をbyteに変換
        if (client != null)
        {
            client.Write(sendByte, 0, sendByte.Length);//送信
        }
        Debug.Log(sendText);
        walk = false;
        command = true;//mainController返信用

    }

    public override void WalkStraight()
    {
        //直進パラメータ設定
        leftPedalUp1 = 24;
        leftPedalDown1 = 0;
        leftSliderForward1 = 38;
        leftSliderBackward1 = -22;
        leftRotationAngle1 = 0;
        leftRotationAngle2 = 0;
        rightPedalUp1 = 24;
        rightPedalDown1 = 0;
        rightSliderForward1 = 38;
        rightSliderBackward1 = -22;
        rightRotationAngle1 = 0;
        rightRotationAngle2 = 0;
        seatRotation = 0f;

        targetCalculate();//目標値計算

        //送信するデータを文字列でまとめる
        if (!walk)
        {
            sendText = "start" + ",";
        }
        else
        {
            sendText = "update" + ",";
        }

        for (int i = 0; i < 6; i++)
        {

            sendText += targetPulseUp1[i].ToString() + "," + targetPulseDown1[i].ToString() + ",";
            sendText += driveTimeUp1[i].ToString() + "," + driveTimeDown1[i].ToString() + ",";
            sendText += delayTimeUp1[i].ToString() + "," + delayTimeDown1[i].ToString() + ",";
            sendText += delayTimeFirst[i].ToString() + ",";
        }
        sendText += seatRotationPulse.ToString() + ",";
        sendText += "/";//終わりの目印
        byte[] sendByte = System.Text.Encoding.ASCII.GetBytes(sendText);//送信する文字列をbyteに変換
        if (client != null)
        {
            client.Write(sendByte, 0, sendByte.Length);//送信
        }
        Debug.Log(sendText);

        walk = true;
        command = true;

    }

    void OnDestroy()//Scene閉じたとき
    {

        if (walk)
        {
            WalkStop();
        }


    }
}


