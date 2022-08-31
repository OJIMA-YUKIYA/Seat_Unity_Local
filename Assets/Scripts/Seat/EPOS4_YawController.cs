/*****************************************
<Summary>
EPOS4をUnityから制御する．
加速度制御と振動制御が可能．
振動制御にはESP32が別途必要．
******************************************/


////////////////////////メモ　432行目確認　　　　　　　12/7

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Threading;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using EposCmd.Net;
using EposCmd.Net.DeviceCmdSet.Operation;
using System.Threading.Tasks;

//モータ用パラメータ保存クラス
public class Motor_Params2
{
    public ushort nodeId;
    public Device epos;
    public StateMachine sm;
    public string mode;
    public ProfilePositionMode ppm;
    public bool isRiverse;
    public int position;
    public int velocity;
    public short current;
    public bool isReached = true;
}

public class EPOS4_YawController : MonoBehaviour
{

    [SerializeField]
    WalkDemoMainController mainController;

    private Thread _thread;
    private bool _isThreadEnd = false;

    private DeviceManager _connector;

    private Motor_Params _motorParams = new Motor_Params();
    //  private Motor_Params _motorParamsR = new Motor_Params();

    private string _usb = "USB0";
    private uint _baudrate = 1000000;
    private uint _timeout = 500;
    private string _eposName = "EPOS4";
    private string _protcoll = "MAXON SERIAL V2";
    private string _interface = "USB";

    //モーター仕様
    [SerializeField, HeaderAttribute("Motor")]
    private static double ONE_ROT_ENCODER_PULSE = 2000;                //モータ1回転のエンコーダパルス[inc]　500の場合,1回転に必要なqcは，(500*4)=2000
    private static double REDUCTION_RATIO = -16 * 5;                   //減速比 4.3*5=21.5 4.3から16に変更　±が逆になったので、-16

    private static double ONE_QC = 360 / ONE_ROT_ENCODER_PULSE;// 1 qc = 360[deg] / (500*4) = 0.18 deg; エンコーダカウントパルス:
                                                               //  [SerializeField] private double TRANSLATE_DEG_TO_QC = REDUCTION_RATIO / ONE_QC;// pos_qc = pos_deg * 21.5/0.18; //ただし,減速比が4.3*5=21.5
    [SerializeField] private static double TRANSLATE_DEG_TO_QC = -80 / ONE_QC;// pos_qc = pos_deg * -80/0.18; //ただし,減速比が16*5=80
    private static double TRANSLATE_RPM = 60.0 / 360 * REDUCTION_RATIO;         //rpm変換用係数 0.03


    // private static double TRANSLATE_RPM = 60.0 / ONE_ROT_ENCODER_PULSE;         //rpm変換用係数 0.03
    //private static double GEARS_RATIO = (double)WHEEL_GEARS / MOTOR_GEARS;      //ギア比 10.08
    //private static double ONE_ROT_WHEEL_PULSE = ONE_ROT_ENCODER_PULSE * REDUCTION_RATIO * GEARS_RATIO;  //車輪1回転のエンコーダパルス
    //private static double PULSE_PER_METER = ONE_ROT_WHEEL_PULSE / WHEEL_CIR;    //1メートル当たりのパルス 290671.27
    //private static double TRANSLATE_RPM = 60.0 / ONE_ROT_ENCODER_PULSE;         //rpm変換用係数 0.03

    //[SerializeField]
    private Vector2Int _nodeId = new Vector2Int(1, 3); //EPOSのNode
    [SerializeField] private string _currentMode;   //現在のモード 表示用
    [SerializeField] private bool _isCSVMode = false;   //CSVモードを使う

    public bool IsCSVMode
    {
        get { return _isCSVMode; }
        private set { _isCSVMode = value; }
    }

    ///森田さんのプログラムで使用///////////////////////////////////////////////////////////
    public byte Frequency = 1;         //振動の周波数 1, 3, 5, 10, 20, 40
    public float SineWaveGain = 1;     //振幅のゲイン    
    private double _gainAccel = 1.0;   //加速度ゲイン
    private double _videoAccel = 1.0;  //動画の加速度
    private double _targetAccel;       //提示する加速度 表示用 映像の加速度のゲイン倍
    private double _targetFreq = 1.0;  //周期
    private int _times = 3;            //回数 2回以上でないとバグる
    ///////////////////////////////////////////////////////////////////////////////////////　

    private double m_Accel = 0;   //加速度
    private double m_Dccel = 0;   //減速度

    [SerializeField, HeaderAttribute("Parameter")] //v_max = 120000[rpm] = [m/s]                    //車いす仕様：v_max = 6700[rpm] = 0.7683[m/s]
    public bool _isTurnSetStep = true;
    [SerializeField] private int Turnstep = 6;

    [SerializeField, Range(0, 10)]//スライダの範囲を設定
    private float Stimulus_rate_yaw = 1.0f;//刺激量調整用[mm]

    //_name：計算式内仕様パラメータ(共通)　name：各パラメータ　

    [SerializeField, HeaderAttribute("inner Foot Parameter")]
    private double targetPosition_deg_inner = 3.19;   //内側回転角度[deg]
    [SerializeField] private double movingTime_ms_inner = 264;   //内側回転時間[ms]
    [SerializeField] private double washBackRatio_inner = 0.45;   //内側戻り率
    [SerializeField] private uint profVel_inner;   //内側回転速度[rpm]   
    [SerializeField] private uint profAcc_inner;   //内側回転加速度[rpm/s]
    [SerializeField] private uint profDec_inner;   //内側回転減速度[rpm/s]
    [SerializeField] private uint profVel2_inner;   //内側回転速度[rpm]   
    [SerializeField] private uint profAcc2_inner;   //内側回転加速度[rpm/s]
    [SerializeField] private uint profDec2_inner;   //内側回転減速度[rpm/s]


    [SerializeField, HeaderAttribute("outer Foot Parameter")]
    private double targetPosition_deg_outer = 2.57;   //外側回転角度[deg]
    [SerializeField] private double movingTime_ms_outer = 314;   //外側回転時間[ms]
    [SerializeField] private double washBackRatio_outer = 0.46;   //外側戻り率
    [SerializeField] private uint profVel_outer;   //外側回転速度[rpm]
    [SerializeField] private uint profAcc_outer;   //外側回転加速度[rpm/s]
    [SerializeField] private uint profDec_outer;   //外側回転減速度[rpm/s]
    [SerializeField] private uint profVel2_outer;   //外側回転速度[rpm]
    [SerializeField] private uint profAcc2_outer;   //外側回転加速度[rpm/s]
    [SerializeField] private uint profDec2_outer;   //外側回転減速度[rpm/s]

    //旋回歩行のパラメータ格納用
    [SerializeField] private int targetPosition_inner;
    [SerializeField] private int targetPosition2_inner;
    [SerializeField] private int targetPosition_outer;
    [SerializeField] private int targetPosition2_outer;

    //Timer
    private Stopwatch stopWatch;    //高精度タイマ

    [SerializeField, HeaderAttribute("Status")]
    public float _currentTime;    //現在の時間 表示用
    [SerializeField] public float _movingTime;   //動作している時間 表示用
    [SerializeField] public float _waitNextStepTime = 0;

    public float MovingTime
    {
        get { return _movingTime; }
        private set { _movingTime = value; }
    }

    [SerializeField] private float _correctionTime = 0f;//動画の終わりズレの補正のため

    private bool _isMove = false;           //動く命令を入れたか
    private bool _isAbsolute = false;       //モータを原点からの絶対値，現時点からの相対値で制御するか
    private bool _isImmediately = false;    //入力したら即座に実行するか

    [SerializeField] public int _targetPosition;   //[inc]
    [SerializeField] public int _targetPosition2;   //[inc]
    [SerializeField] public Vector2Int _position = new Vector2Int(0, 0);     //表示用
    [SerializeField] public uint _profVel;         //[rpm]
    [SerializeField] public uint _profVel2;         //[rpm]
    [SerializeField] public Vector2Int _velocity = new Vector2Int(0, 0);     //表示用
    [SerializeField] public uint _profAcc;         //[rpm/s]
    [SerializeField] public uint _profDec;         //[rpm/s]
    [SerializeField] public uint _profAcc2;         //[rpm/s]
    [SerializeField] public uint _profDec2;         //[rpm/s]
    [SerializeField] public Vector2Int _current = new Vector2Int(0, 0);      //表示用

    [SerializeField] private bool _isMoving = false;    //動いてるかをセンサから判断 表示用

    //[SerializeField] private bool _isVibrate = false;   //振動しているかをスクリプトから判断 表示用
    //public bool IsVibrate {
    //    get { return _isVibrate; }
    //    private set { _isVibrate = value; }
    //}

    [SerializeField] private bool _isBackHoming = false; //原点に戻っているかをスクリプトから判断 表示用
    [SerializeField] public float _backHomeTime = 4200;   //原点に戻るまでの時間 表示用
    public double backHomePosition_deg;
    private double backWash_deg;

    private bool once = true; //待機時間1.4秒　最初だけ

    public bool IsBackHoming
    {
        get { return _isBackHoming; }
        private set { _isBackHoming = value; }
    }

    //歩行動作用フラグ
    [SerializeField] private bool turnLeft;     //左旋回歩行の関数動作中
    [SerializeField] private bool turnRight;    //右旋回歩行の関数動作中
    [SerializeField] private bool washBack;    //右旋回歩行の関数動作中
    public bool command;

    //旋回歩行時の歩数(ウォッシュバックの計算に使用)
    [SerializeField] private int stepTurnRight = 0; //右旋回歩行数カウント
    [SerializeField] private int stepTurnLeft = 0;　//左旋回歩行数カウント
    [SerializeField] private int foot = 1;   //内側外側入れ替え　inner=1 outer=-1
    [SerializeField] private int step = 0;  //旋回歩行の歩数合わせ用
    private int waitFirstStep = 0; //右旋回歩行時に一歩分足をずらす用    

    void Start()
    {
        //Device Settings
        try
        {
            //つなげる前にウィンドウ表示する場合
            //_connector = new DeviceManager();

            //// Get baudrate info
            //uint b = _connector.Baudrate;

            //// Set connection properties
            //_connector.Baudrate = b;
            //_connector.Timeout = 500;

            _connector = new DeviceManager(_eposName, _protcoll, _interface, _usb)
            {
                Baudrate = _baudrate,
                Timeout = _timeout
            };
        }
        catch (DeviceException e)
        {
            StopRefresh();
            Debug.LogError(e.ErrorMessage + ":" + e.ErrorCode);
        }
        catch (Exception e)
        {
            StopRefresh();
            Debug.LogError(e.Message);
        }

        InitializeEpos();

        _thread = new Thread(ThreadUpdate);
        _thread.Start();

        stopWatch = new Stopwatch();
        stopWatch.Start();

        ///Stimulus_rate_yaw = mainController.Stimulus_rate_yaw_seat;
        Debug.Log("Rate Yaw : " + Stimulus_rate_yaw + "[倍]");

        targetPosition_deg_inner= targetPosition_deg_inner * Stimulus_rate_yaw ;
        // movingTime_ms_inner = movingTime_ms_inner * Stimulus_rate_yaw;
        //washBackRatio_inner = washBackRatio_inner * Stimulus_rate_yaw;

        //旋回時のパラメータ計算・格納
        //内側
        ParameterSet(targetPosition_deg_inner , movingTime_ms_inner , 700, washBackRatio_inner);
        targetPosition_inner = _targetPosition;
        profVel_inner = _profVel;
        profAcc_inner = _profAcc;
        profDec_inner = _profDec;
        targetPosition2_inner = _targetPosition2;
        profVel2_inner = _profVel2;
        profAcc2_inner = _profAcc2;
        profDec2_inner = _profDec2;

        targetPosition_deg_outer = targetPosition_deg_outer * Stimulus_rate_yaw;
        //movingTime_ms_outer = movingTime_ms_inner * Stimulus_rate_yaw;
       // washBackRatio_outer = washBackRatio_outer * Stimulus_rate_yaw;


        //外側
        ParameterSet(targetPosition_deg_outer , movingTime_ms_outer , 700, washBackRatio_outer);
        targetPosition_outer = _targetPosition;
        profVel_outer = _profVel;
        profAcc_outer = _profAcc;
        profDec_outer = _profDec;
        targetPosition2_outer = _targetPosition2;
        profVel2_outer = _profVel2;
        profAcc2_outer = _profAcc2;
        profDec2_outer = _profDec2;
        Debug.Log("Motor : " + ONE_QC + "[rpm]");

        backWash_deg = targetPosition_deg_inner * (1 - washBackRatio_inner) + targetPosition_deg_outer *  (1 - washBackRatio_outer);

    }

    void OnDestroy()
    {

        if (_thread != null)
        {
            _isThreadEnd = true;
            _thread.Abort();
            _thread = null;
        }
        stopWatch.Stop();

        StopRefresh();

        if (_connector != null)
        {
            /*
             * Important notice:
             * It's recommended to call the Dispose function before application close
             */
            _connector.Dispose();
        }

    }

    // Update is called once per frame
    void Update()
    {
        //Eposの主な動作はループがあるためThreadで操作

        IndicateMotionInfo(_motorParams);//, _motorParamsR);


        if (mainController.walkStraight)//直進歩行開始
        {
            walkStraight();
            command = true;
        }
        if (mainController.walkLeft)//左旋回歩行開始
        {
            if (!turnLeft)
            {
                turnLeft = true;
                turnRight = false;
                step = 0;
                washBack = false;
                once = true;
                command = true;
            }



        }
        if (mainController.walkRight)//左旋回歩行開始
        {
            if (!turnRight)
            {

                turnLeft = false;
                turnRight = true;
                step = 0;
                washBack = false;
                once = true;
                command = true;
            }



        }

        if (mainController.walkStop)//歩行停止
        {

            StopRefresh();// walkStop();

        }
        if (mainController.walkBack)
        {
            //if (!washBack)
            //{
            //turnLeft = false;
            //   turnRight = false;
            washBack = true;

            //}
            
            command = true;
            Invoke(nameof(BackHomePosition), 1.4f);//1.4f); 2歩分待って戻る。1.4fだと下肢駆動装置とずれ、2.8fなら合う
            Debug.Log("Epos BackHome Finish");
            

            // BackHomePosition();


            //command = true;
        }



        /* if (!IsCSVMode) {
              //for ppm
              if (!_isMoving) {
                  //Move
                  if (Input.GetKeyDown(KeyCode.Space)) {
                      _isMove = true;
                      stopWatch.Restart();
                  }

                  //back home
                  if (Input.GetKey(KeyCode.O)) {
                      _isMove = true;
                      _currentTime = Single.PositiveInfinity;
                      IsBackHoming = true;
                  }
                  else {
                      IsBackHoming = false;
                  }
              }
          } else {
              //for csv
              //Enable
              if (Input.GetKeyDown(KeyCode.Space) && !IsVibrate) {
                  IsVibrate = true;
                  stopWatch.Restart();
                  EnableEpos(_motorParams);
                  //EnableEpos(_motorParamsR);
              }

              //Disable
              if (Input.GetKeyDown(KeyCode.O)) {
                  _currentTime = Single.PositiveInfinity;
              }
          }

          //reset
          if (Input.GetKeyDown(KeyCode.R)) {
              InitializeEpos();
          }*/
    }

    private async void ThreadUpdate()
    {

        while (true)
        {
            //get info
            GetMotionInfo(_motorParams);            // GetMotionInfo(_motorParamsR);

            _currentTime = NowSec();

            if (turnLeft)
            {
                if (once)
                {
                    Thread.Sleep(1750); //
                    //Thread.Sleep(350); //斜めとタイミングを合わせるなら1750
                    once = false;
                }
                await walkTurn();
                //Debug.Log("LeftFinish");
            }
            if (turnRight)
            {
                if (once)
                {
                    //Thread.Sleep(350); 

                     Thread.Sleep(1750);
                    once = false;
                }
                await walkTurn();
                //Debug.Log("RIghtFinish");
            }
            //if (washBack)
            //{
            //    下肢駆動装置の仕様上、1.4s待つ
            //    Invoke(nameof(BackHomePosition), 1.4f);
            //}

            //if (_isMove)
            //{
            //    if (IsBackHoming)
            //    {
            //        BackHomePosition();
            //    }
            //    //else if (_currentTime < (_times - 1) * (float)_targetFreq) {
            //    //    //Move
            //    //    MoveToPosition(_motorParams);//20210609 out
            //    //   // MoveToPosition(_motorParamsR);
            //    //}

            //    //if (_currentTime > MovingTime) {
            //    //    _isMove = false;
            //    //}
            //}

            ////if (IsVibrate) {
            ////    if (_currentTime > MovingTime) {
            ////        DisableEpos(_motorParams);
            ////        DisableEpos(_motorParamsR);
            ////        IsVibrate = false;
            ////    }
            ////}


            //whileを抜けないとバグる
            if (_isThreadEnd)
            {
                break;
            }
        }
    }

    //現在の時間（秒）
    private float NowSec()
    {
        return (float)stopWatch.Elapsed.TotalSeconds;
    }


    //目標ターゲットを計算　三角波
    private void ParameterSet(double targetPosition_deg, double targetTime_ms, double cycleTime_ms, double returnRate)
    {
        //  MovingTime = (float)(_times * _targetFreq) + _correctionTime;
        if (targetPosition_deg < 0)
        {
            targetPosition_deg = -(targetPosition_deg);
        }

        double m_Velocity1 = 2 * targetPosition_deg / (targetTime_ms / 1000) * TRANSLATE_RPM;// v1=2*v0 起動時から速度が一定の場合

        if (m_Velocity1 > 120000)
        {
            m_Velocity1 = 120000;
        }
        Debug.Log("Velocity1 : " + m_Velocity1 + "[rpm]");

        double m_Velocity2 = 2 * targetPosition_deg * returnRate / ((cycleTime_ms - targetTime_ms) / 1000) * TRANSLATE_RPM;

        if (m_Velocity2 > 120000)
        {
            m_Velocity2 = 120000;
        }
        Debug.Log("Velocity2 : " + m_Velocity2 + "[rpm]");
        Debug.Log("Time : " + targetTime_ms + "[ms] , Time : " + cycleTime_ms + "[ms]");

        double m_Accel = m_Velocity1 * (1000 / (targetTime_ms / 2));
        double m_Decel = m_Velocity2 * (1000 / ((cycleTime_ms - targetTime_ms) / 2));
        //_targetAccel = _gainAccel * _videoAccel;
        //double v_max = _targetAccel * _targetFreq / 2.0;
        //double x = _targetFreq * v_max / 2.0;

        _targetPosition = (int)(targetPosition_deg * TRANSLATE_DEG_TO_QC);     //TRANSLATE_DEG_TO_QC = 21.5/0.18
        _profVel = (uint)(Math.Abs(m_Velocity1));
        _profAcc = (uint)(Math.Abs(m_Accel));
        _profDec = _profAcc;
        _targetPosition2 = (int)(targetPosition_deg * returnRate * TRANSLATE_DEG_TO_QC);     //TRANSLATE_DEG_TO_QC = 21.5/0.18
        _profVel2 = (uint)(Math.Abs(m_Velocity2));
        _profAcc2 = (uint)(Math.Abs(m_Decel));
        _profDec2 = _profAcc2;

        //_profDec = (uint)(Math.Abs(m_Decel));
        //_profVel = (uint) (Math.Abs(v_max) * PULSE_PER_METER * TRANSLATE_RPM);
        //_profAcc = (uint) (Math.Abs(_targetAccel) * PULSE_PER_METER * TRANSLATE_RPM);
        //_profDec = _profAcc;

    }

    private void walkStraight()
    {
        command = true;
    }

    private void walkStop()
    {
        command = true;
    }


    private async Task walkTurn()
    {
        //Startでパラメータ格納済み
        if (_isTurnSetStep)
        {

            if (turnLeft)
            {
                if (step < Turnstep)
                {
                    if (foot > 0)
                    {
                        if (_currentTime - _waitNextStepTime >= 0.7f)
                        {
                            _waitNextStepTime = _currentTime;

                            //inner往路
                            MoveToPosition(_motorParams, targetPosition_inner, profVel_inner, profAcc_inner, profDec_inner);
                            //動き終わるまで待つ
                            await Task.Run(async () =>
                            {
                                while (true)
                                {
                                    GetMotionInfo(_motorParams);
                                    if (_motorParams.isReached) return;
                                    await Task.Delay(1);
                                }

                            });

                            //inner復路
                            MoveToPosition(_motorParams, -targetPosition2_inner, profVel2_inner, profAcc2_inner, profDec2_inner);

                            foot = foot * (-1);     //内側外側入れ替え
                            step++;     //旋回歩行数カウント
                            stepTurnLeft++;         //左旋回歩行数カウント(WashBackで使用）
                                                    //  Debug.Log("TurnLeft1");
                        }
                    }
                    else if (foot < 0)
                    {
                        if (_currentTime - _waitNextStepTime >= 0.7f)
                        {
                            _waitNextStepTime = _currentTime;

                            //outer往路
                            MoveToPosition(_motorParams, targetPosition_outer, profVel_outer, profAcc_outer, profDec_outer);
                            //動き終わるまで待つ
                            await Task.Run(async () =>
                            {
                                while (true)
                                {
                                    GetMotionInfo(_motorParams);
                                    if (_motorParams.isReached) return;
                                    await Task.Delay(1);
                                }

                            });

                            //outer復路
                            MoveToPosition(_motorParams, -targetPosition2_outer, profVel2_outer, profAcc2_outer, profDec2_outer);
                            foot = foot * (-1);  //内側外側入れ替え
                            step++;     //旋回歩行数カウント
                            stepTurnLeft++;     //左旋回歩行数カウント(WashBackで使用）

                            //  Debug.Log("TurnLeft2");
                        }

                    }
                }
                else if (step >= Turnstep)
                {
                    Debug.Log("Epos TurnFinish");
                    turnLeft = false;
                    // 初期化
                    step = 0;
                    foot = 1;
                }
                // command = true;

            }//if(turnLeft)

            if (turnRight)
            {
                //左足一歩分待った後、右足から開始
                //一歩目かどうか
                //if (waitFirstStep <= 0)
                //{
                //    if (_currentTime - _waitNextStepTime >= 0.7f)
                //    {
                //        _waitNextStepTime = _currentTime;
                //        waitFirstStep++;
                //        Debug.Log("TurnRightStart");
                //    }
                //}
                ////一歩分待った後だったら
                //else if (waitFirstStep > 0)
                //{
                if (step < Turnstep)
                {
                    if (foot > 0)
                    {
                        if (_currentTime - _waitNextStepTime >= 0.7f)
                        {
                            _waitNextStepTime = _currentTime;
                            //inner往路
                            MoveToPosition(_motorParams, -targetPosition_inner, profVel_inner, profAcc_inner, profDec_inner);
                            //動き終わるまで待つ
                            await Task.Run(async () =>
                            {
                                while (true)
                                {
                                    GetMotionInfo(_motorParams);
                                    if (_motorParams.isReached) return;
                                    await Task.Delay(1);
                                }

                            });

                            //inner復路
                            MoveToPosition(_motorParams, targetPosition2_inner, profVel2_inner, profAcc2_inner, profDec2_inner);

                            foot = foot * (-1);     //内側外側入れ替え
                            step++;     //旋回歩行数カウント
                            stepTurnRight++;        //右旋回歩行数カウント(WashBackで使用）
                                                    // }
                                                    //if (_currentTime - _waitNextStepTime <= (movingTime_ms_inner / 1000))
                                                    //{

                            //    Debug.Log(movingTime_ms_inner / 1000);
                            // Debug.Log(_currentTime - _waitNextStepTime);
                            //}
                            //else if (_currentTime - _waitNextStepTime > (movingTime_ms_inner / 1000))
                            //   {
                            //Debug.Log(_currentTime - _waitNextStepTime);

                            // }                           
                            // Debug.Log("TurnRight1");
                            //   while (_waitNextStepTime >= _currentTime) ;
                        }
                    }
                    else if (foot < 0)
                    {
                        if (_currentTime - _waitNextStepTime >= 0.7f)
                        {
                            _waitNextStepTime = _currentTime;

                            //outer往路                       
                            MoveToPosition(_motorParams, -targetPosition_outer, profVel_outer, profAcc_outer, profDec_outer);
                            //動き終わるまで待つ
                            await Task.Run(async () =>
                            {
                                while (true)
                                {
                                    GetMotionInfo(_motorParams);
                                    if (_motorParams.isReached) return;
                                    await Task.Delay(1);
                                }

                            });

                            //outer復路
                            MoveToPosition(_motorParams, targetPosition2_outer, profVel2_outer, profAcc2_outer, profDec2_outer);
                            foot = foot * (-1);     //内側外側入れ替え
                            step++;     //旋回歩行数カウント
                            stepTurnRight++;        //右旋回歩行数カウント(WashBackで使用）

                            // Debug.Log("TurnRight2");
                        }

                    }

                }
                else if (step >= Turnstep)
                {
                    Debug.Log("Epos TurnFinish");
                    turnRight = false;
                    // 初期化
                    step = 0;
                    foot = 1;
                    waitFirstStep = 0;
                }
                //command

            }//if(turnRight)
        }
        if (!_isTurnSetStep)
        {
            foot = 1;

            if (foot > 0)
            {
                //inner往路
                MoveToPosition(_motorParams, targetPosition_inner, profVel_inner, profAcc_inner, profDec_inner);
                //動き終わるまで待つ
                await Task.Run(async () =>
                {
                    while (true)
                    {
                        GetMotionInfo(_motorParams);
                        if (_motorParams.isReached) return;
                        await Task.Delay(1);
                    }
                });

                //inner復路
                MoveToPosition(_motorParams, targetPosition2_inner, profVel2_inner, profAcc2_inner, profDec2_inner);
                foot = foot * (-1);     //内側外側入れ替え

            }
            else if (foot < 0)
            {
                //outer往路
                MoveToPosition(_motorParams, targetPosition_outer, profVel_outer, profAcc_outer, profDec_outer);
                //動き終わるまで待つ
                await Task.Run(async () =>
                {
                    while (true)
                    {
                        GetMotionInfo(_motorParams);
                        if (_motorParams.isReached) return;
                        await Task.Delay(1);
                    }
                });

                //outer復路
                MoveToPosition(_motorParams, targetPosition2_outer, profVel2_outer, profAcc2_outer, profDec2_outer);
                foot = foot * (-1);     //内側外側入れ替え

            }

            command = true;
            //初期化
            step = 0;
            foot = 1;

        }

    }

    //原点に戻す
    private void BackHomePosition()
    {
        //_waitNextStepTime = _currentTime;



        //backHomePosition_deg = backWash_deg * (stepTurnLeft - stepTurnRight) / 2;
        backHomePosition_deg = _motorParams.position / TRANSLATE_DEG_TO_QC;

        if (backHomePosition_deg < 0)
        {
            backHomePosition_deg = -(backHomePosition_deg);
        }

        double m_backVelocity = backHomePosition_deg / (_backHomeTime / 1000) * TRANSLATE_RPM * 3 / 2;// v1=3x/2t 加減速時間が1:1:1
        if (m_backVelocity > 120000)
        {
            m_backVelocity = 120000;
        }

        // Debug.Log("backVelocity:" + m_backVelocity + "[rpm]");
        Debug.Log("Epos WashBack");

        double m_backAccel = m_backVelocity * (1000 / (_backHomeTime / 3));   // 加減速時間は1:1:1

        _targetPosition = 0;// (int)(backHomePosition_deg * TRANSLATE_DEG_TO_QC);     //TRANSLATE_DEG_TO_QC = 21.5/0.18//0;
        _profVel = (uint)(Math.Abs(m_backVelocity)); //    (unit)1000;//適当な値
        _profAcc = (uint)(Math.Abs(m_backAccel));   //(unit) 250;
        _profDec = _profAcc;
        _isAbsolute = true; //原点からの絶対位置に設定

        // backHomePosition_deg = _motorParams.position;
        //if (_currentTime - _waitNextStepTime >= 0.7f)
        //{
        //Invoke(MoveToPosition(_motorParams, _targetPosition, _profVel, _profAcc, _profDec)), 1.4f);
        MoveToPosition(_motorParams, _targetPosition, _profVel, _profAcc, _profDec);
        _isAbsolute = false;//現時点からの相対位置に設定
                            //}
        Debug.Log("Epos Home Position Reached");
        command = true;
        washBack = false;



    }

    //Epos初期化
    private void InitializeEpos()
    {
        //Initialization
        CreateEpos(_motorParams, (ushort)_nodeId.x, false);
        //CreateEpos(_motorParamsR, (ushort)_nodeId.y, true);

        if (IsCSVMode)
        {
            //CyclicSynchronousVelocityMode
            SetCSVMode(_motorParams);
            //SetCSVMode(_motorParamsR);
            DisableEpos(_motorParams);
            //DisableEpos(_motorParamsR);
        }
        else
        {
            //PositionProfileMode
            SetPpmMode(_motorParams);
            //SetPpmMode(_motorParamsR);
            EnableEpos(_motorParams);
            //EnableEpos(_motorParamsR);
        }
    }

    //Eposオブジェクトの作成
    private void CreateEpos(Motor_Params motorParams, ushort nodeId, bool isRiverse)
    {
        try
        {
            motorParams.nodeId = nodeId;
            motorParams.isRiverse = isRiverse;

            motorParams.epos = _connector.CreateDevice(motorParams.nodeId);
            motorParams.sm = motorParams.epos.Operation.StateMachine;
            Debug.Log("node: " + motorParams.nodeId + " Created");
        }
        catch (DeviceException e)
        {
            StopRefresh();
            Debug.LogError("node: " + motorParams.nodeId + ", " + e.ErrorMessage + ":" + e.ErrorCode);
        }
        catch (Exception e)
        {
            StopRefresh();
            Debug.LogError("node: " + motorParams.nodeId + ", " + e.Message);
        }
    }

    //EposをEnable状態に変更
    private void EnableEpos(Motor_Params motorParams)
    {
        try
        {
            if (motorParams.sm.GetFaultState())
            {
                motorParams.sm.ClearFault();
            }
            motorParams.sm.SetEnableState();
            Debug.Log("node: " + motorParams.nodeId + " Enabled");
        }
        catch (DeviceException e)
        {
            StopRefresh();
            Debug.LogError("node: " + motorParams.nodeId + ", " + e.ErrorMessage + ":" + e.ErrorCode);
        }
        catch (Exception e)
        {
            StopRefresh();
            Debug.LogError("node: " + motorParams.nodeId + ", " + e.Message);
        }
    }

    //EposをDisable状態に変更
    private void DisableEpos(Motor_Params motorParams)
    {
        try
        {
            motorParams.sm.SetDisableState();
            Debug.Log("node: " + motorParams.nodeId + " Disabled");
        }
        catch (DeviceException e)
        {
            StopRefresh();
            Debug.LogError("node: " + motorParams.nodeId + ", " + e.ErrorMessage + ":" + e.ErrorCode);
        }
        catch (Exception e)
        {
            StopRefresh();
            Debug.LogError("node: " + motorParams.nodeId + ", " + e.Message);
        }
    }

    //UnityのInspectorにモータ情報を表示
    private void IndicateMotionInfo(Motor_Params L)
    {//, Motor_Params R) {
        _position.x = L.position;
        // _position.y = R.position;
        _velocity.x = L.velocity;
        // _velocity.y = R.velocity;
        _current.x = L.current;
        // _current.y = R.current;
        _isMoving = !(L.isReached);// && R.isReached);
                                   //if (L.mode == R.mode) {
                                   //    _currentMode = L.mode;
                                   //} else {
                                   //    _currentMode = "Different Mode Error";
                                   //}
    }

    //Eposからモータ情報を取得
    private void GetMotionInfo(Motor_Params motorParams)
    {
        if (motorParams.epos != null)
        {
            try
            {
                motorParams.position = motorParams.epos.Operation.MotionInfo.GetPositionIs();
                motorParams.velocity = motorParams.epos.Operation.MotionInfo.GetVelocityIs();
                motorParams.current = motorParams.epos.Operation.MotionInfo.GetCurrentIs();
                motorParams.epos.Operation.MotionInfo.GetMovementState(ref motorParams.isReached);
            }
            catch (DeviceException e)
            {
                StopRefresh();
                //Debug.LogError("node:" + motorParams.nodeId + ", " + e.ErrorMessage + ":" + e.ErrorCode);
            }
            catch (OverflowException e)
            {
                // Debug.LogError("node:" + motorParams.nodeId + ", " + e.Message);
            }
            catch (FormatException e)
            {
                // Debug.LogError("node:" + motorParams.nodeId + ", " + e.Message);
            }
            catch (Exception e)
            {
                StopRefresh();
                // Debug.LogError("node:" + motorParams.nodeId + ", " + e.Message);
            }
        }
    }

    //Profile Position Modeに変更
    private void SetPpmMode(Motor_Params motorParams)
    {
        try
        {
            motorParams.ppm = motorParams.epos.Operation.ProfilePositionMode;
            motorParams.ppm.ActivateProfilePositionMode();
            motorParams.mode = motorParams.epos.Operation.OperationMode.GetOperationModeAsString();
            Debug.Log(motorParams.mode);
        }
        catch (DeviceException e)
        {
            StopRefresh();
            Debug.LogError("node: " + motorParams.nodeId + ", " + e.ErrorMessage + ":" + e.ErrorCode);
        }
        catch (OverflowException e)
        {
            Debug.LogError("node: " + motorParams.nodeId + ", " + e.Message);
        }
        catch (FormatException e)
        {
            Debug.LogError("node: " + motorParams.nodeId + ", " + e.Message);
        }
        catch (Exception e)
        {
            StopRefresh();
            Debug.LogError("node: " + motorParams.nodeId + ", " + e.Message);
        }
    }

    //モータを目標値に回転
    private void MoveToPosition(Motor_Params motorParams, int _targetPosition, uint _profVel, uint _profAcc, uint _profDec)
    {
        try
        {
            motorParams.ppm.SetPositionProfile(_profVel, _profAcc, _profDec);
            if (motorParams.isRiverse)
            {
                motorParams.ppm.MoveToPosition(-_targetPosition, _isAbsolute, _isImmediately);
            }
            else
            {
                motorParams.ppm.MoveToPosition(_targetPosition, _isAbsolute, _isImmediately);
            }
        }
        catch (DeviceException e)
        {
            StopRefresh();
            Debug.LogError("node: " + motorParams.nodeId + ", " + e.ErrorMessage + ":" + e.ErrorCode);
        }
        catch (OverflowException e)
        {
            Debug.LogError("node: " + motorParams.nodeId + ", " + e.Message);
        }
        catch (FormatException e)
        {
            Debug.LogError("node: " + motorParams.nodeId + ", " + e.Message);
        }
        catch (Exception e)
        {
            StopRefresh();
            Debug.LogError("node: " + motorParams.nodeId + ", " + e.Message);
        }
    }

    //Cyclic Synchronous Velocityモードに変更
    private void SetCSVMode(Motor_Params motorParams)
    {
        try
        {
            motorParams.epos.Operation.OperationMode.SetOperationMode(EOperationMode.OmdCyclicSynchronousVelocityMode);
            motorParams.mode = motorParams.epos.Operation.OperationMode.GetOperationModeAsString();
            Debug.Log(motorParams.mode);
        }
        catch (DeviceException e)
        {
            StopRefresh();
            Debug.LogError("node:" + motorParams.nodeId + ", " + e.ErrorMessage + ":" + e.ErrorCode);
        }
        catch (OverflowException e)
        {
            Debug.LogError("node: " + motorParams.nodeId + ", " + e.Message);
        }
        catch (FormatException e)
        {
            Debug.LogError("node: " + motorParams.nodeId + ", " + e.Message);
        }
        catch (Exception e)
        {
            StopRefresh();
            Debug.LogError("node: " + motorParams.nodeId + ", " + e.Message);
        }
    }

    //Eposを停止
    private void StopRefresh()
    {
        try
        {
            _motorParams = new Motor_Params();
            //  _motorParamsR = new Motor_Params();
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }
}
