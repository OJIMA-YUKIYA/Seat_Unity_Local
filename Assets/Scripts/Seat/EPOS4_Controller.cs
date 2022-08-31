/*****************************************
<Summary>
EPOS4をUnityから制御する．
加速度制御と振動制御が可能．
振動制御にはESP32が別途必要．
******************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Threading;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using EposCmd.Net;
using EposCmd.Net.DeviceCmdSet.Operation;

//モータ用パラメータ保存クラス
public class Motor_Params {
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

public class EPOS4_Controller : MonoBehaviour {

    private Thread _thread;
    private bool _isThreadEnd = false;

    private DeviceManager _connector;

    private Motor_Params _motorParamsL = new Motor_Params();
    private Motor_Params _motorParamsR = new Motor_Params();

    private string _usb = "USB0";
    private uint _baudrate = 1000000;
    private uint _timeout = 500;

    [SerializeField] private Vector2Int _nodeId = new Vector2Int(1, 3); //EPOSのNode
    [SerializeField] private string _currentMode;   //現在のモード 表示用

    [SerializeField] private bool _isCSVMode = false;   //CSVモードを使う
    public bool IsCSVMode {
        get { return _isCSVMode; }
        private set { _isCSVMode = value; }
    }
    public byte Frequency = 1;                          //振動の周波数 1, 3, 5, 10, 20, 40
    public float SineWaveGain = 1;                      //振幅のゲイン

    //v_max = 6700[rpm] = 0.7683[m/s]
    [SerializeField] private double _gainAccel = 1.0;   //加速度ゲイン
    [SerializeField] private double _videoAccel = 1.0;  //動画の加速度
    [SerializeField] private double _targetAccel;       //提示する加速度 表示用 映像の加速度のゲイン倍
    [SerializeField] private double _targetFreq = 1.0;  //周期
    [SerializeField] private int _times = 3;            //回数 2回以上でないとバグる

    //Timer
    private Stopwatch stopWatch;    //高精度タイマ

    [SerializeField] private float _currentTime;    //現在の時間 表示用
    [SerializeField] private float _movingTime;   //動作している時間 表示用
    public float MovingTime {
        get { return _movingTime; }
        private set { _movingTime = value; }
    }
    [SerializeField] private float _correctionTime = 0f;//動画の終わりズレの補正のため

    private bool _isMove = false;           //動く命令を入れたか
    private bool _isAbsolute = false;       //モータを絶対値，相対値で制御するか
    private bool _isImmediately = false;    //入力したら即座に実行するか
    [SerializeField] private int _targetPosition;   //[inc]
    [SerializeField] private Vector2Int _position = new Vector2Int(0, 0);     //表示用
    [SerializeField] private uint _profVel;         //[rpm]
    [SerializeField] private Vector2Int _velocity = new Vector2Int(0, 0);     //表示用
    [SerializeField] private uint _profAcc;         //[rpm/s]
    [SerializeField] private uint _profDec;         //[rpm/s]
    [SerializeField] private Vector2Int _current = new Vector2Int(0, 0);      //表示用

    [SerializeField] private bool _isMoving = false;    //動いてるかをセンサから判断 表示用
    [SerializeField] private bool _isVibrate = false;   //振動しているかをスクリプトから判断 表示用
    public bool IsVibrate {
        get { return _isVibrate; }
        private set { _isVibrate = value; }
    }
    [SerializeField] private bool _isBackHoming = false; //原点に戻っているかをスクリプトから判断 表示用
    public bool IsBackHoming {
        get { return _isBackHoming; }
        private set { _isBackHoming = value; }
    }

    private static int ONE_ROT_ENCODER_PULSE = 2000;                //モータ1回転のエンコーダパルス[inc]
    private static double WHEEL_RAD = 0.287;                        //車輪の半径[m]
    private static double WHEEL_CIR = 2.0 * Math.PI * WHEEL_RAD;    //車輪の円周[m] 1.803
    private static int WHEEL_GEARS = 252;                           //車輪のギア歯数
    private static int MOTOR_GEARS = 25;                            //モータのギア歯数
    private static int REDUCTION_RATIO = 26;                        //ギアヘッドの減速比

    private static double GEARS_RATIO = (double)WHEEL_GEARS / MOTOR_GEARS;      //ギア比 10.08
    private static double ONE_ROT_WHEEL_PULSE = ONE_ROT_ENCODER_PULSE * REDUCTION_RATIO * GEARS_RATIO;  //車輪1回転のエンコーダパルス
    private static double PULSE_PER_METER = ONE_ROT_WHEEL_PULSE / WHEEL_CIR;    //1メートル当たりのパルス 290671.27
    private static double TRANSLATE_RPM = 60.0 / ONE_ROT_ENCODER_PULSE;         //rpm変換用係数 0.03

    void Start () {
        //Device Settings
        try {
            _connector = new DeviceManager("EPOS2", "MAXON SERIAL V2", "USB", _usb) {
                Baudrate = _baudrate,
                Timeout = _timeout
            };
        }
        catch (DeviceException e) {
            StopRefresh();
            Debug.LogError(e.ErrorMessage + ":" + e.ErrorCode);
        }
        catch (Exception e) {
            StopRefresh();
            Debug.LogError(e.Message);
        }

        InitializeEpos();

        _thread = new Thread(ThreadUpdate);
        _thread.Start();

        stopWatch = new Stopwatch();
        stopWatch.Start();
    }

    void OnDestroy() {
        if (_thread != null) {
            _isThreadEnd = true;
            _thread.Abort();
            _thread = null;
        }
        stopWatch.Stop();

        StopRefresh();

        if (_connector != null) {
            /*
             * Important notice:
             * It's recommended to call the Dispose function before application close
             */
            _connector.Dispose();
        }
    }
	
	// Update is called once per frame
	void Update () {

	    IndicateMotionInfo(_motorParamsL, _motorParamsR);

        if (!IsCSVMode) {
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
                EnableEpos(_motorParamsL);
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
        }
    }

    private void ThreadUpdate() {
        while (true) {
            //get info
            GetMotionInfo(_motorParamsL);
            GetMotionInfo(_motorParamsR);

            _currentTime = NowSec();

            CalcTarget();

            if (_isMove) {
                if (IsBackHoming) {
                    BackHomePosition();
                }
                else if (_currentTime < (_times - 1) * (float)_targetFreq) {
                    //Move
                    MoveToPosition(_motorParamsL);
                    MoveToPosition(_motorParamsR);
                }

                if (_currentTime > MovingTime) {
                    _isMove = false;
                }
            }

            if (IsVibrate) {
                if (_currentTime > MovingTime) {
                    DisableEpos(_motorParamsL);
                    DisableEpos(_motorParamsR);
                    IsVibrate = false;
                }
            }

             //whileを抜けないとバグる
            if (_isThreadEnd) {
                break;
            }
        }
    }

    //現在の時間（秒）
    private float NowSec() {
        return (float)stopWatch.Elapsed.TotalSeconds;
    }

    //目標ターゲットを計算
    private void CalcTarget() {
        MovingTime = (float)(_times * _targetFreq) + _correctionTime;
        _targetAccel = _gainAccel * _videoAccel;
        double v_max = _targetAccel * _targetFreq / 2.0;
        double x = _targetFreq * v_max / 2.0;

        _targetPosition = (int) (x * PULSE_PER_METER);
        _profVel = (uint) (Math.Abs(v_max) * PULSE_PER_METER * TRANSLATE_RPM);
        _profAcc = (uint) (Math.Abs(_targetAccel) * PULSE_PER_METER * TRANSLATE_RPM);
        _profDec = _profAcc;
    }

    //原点に戻す
    private void BackHomePosition() {
        _targetPosition = 0;
        _profVel = (uint)1000;//適当な値
        _profAcc = (uint)250;
        _profDec = _profAcc;
        _isAbsolute = true;
        MoveToPosition(_motorParamsL);
        //MoveToPosition(_motorParamsR);
        _isAbsolute = false;
    }

    //Epos初期化
    private void InitializeEpos() {
        //Initialization
        CreateEpos(_motorParamsL, (ushort)_nodeId.x, false);
        //CreateEpos(_motorParamsR, (ushort)_nodeId.y, true);

        if (IsCSVMode) {
            //CyclicSynchronousVelocityMode
            SetCSVMode(_motorParamsL);
            //SetCSVMode(_motorParamsR);
            DisableEpos(_motorParamsL);
            DisableEpos(_motorParamsR);
        }
        else {
            //PositionProfileMode
            SetPpmMode(_motorParamsL);
            //SetPpmMode(_motorParamsR);
            EnableEpos(_motorParamsL);
            //EnableEpos(_motorParamsR);
        }
    }

    //Eposオブジェクトの作成
    private void CreateEpos(Motor_Params motorParams, ushort nodeId, bool isRiverse) {
        try {
            motorParams.nodeId = nodeId;
            motorParams.isRiverse = isRiverse;

            motorParams.epos = _connector.CreateDevice(motorParams.nodeId);
            motorParams.sm = motorParams.epos.Operation.StateMachine;
            Debug.Log("node: "+ motorParams.nodeId + " Created");
        }
        catch (DeviceException e) {
            StopRefresh();
            Debug.LogError("node:" + motorParams.nodeId + ", " + e.ErrorMessage + ":" + e.ErrorCode);
        }
        catch (Exception e) {
            StopRefresh();
            Debug.LogError("node:" + motorParams.nodeId + ", " + e.Message);
        }
    }

    //EposをEnable状態に変更
    private void EnableEpos(Motor_Params motorParams) {
        try {
            if (motorParams.sm.GetFaultState()) {
                motorParams.sm.ClearFault();
            }
            motorParams.sm.SetEnableState();
            Debug.Log("node: " + motorParams.nodeId + " Enabled");
        }
        catch (DeviceException e) {
            StopRefresh();
            Debug.LogError("node:" + motorParams.nodeId + ", " + e.ErrorMessage + ":" + e.ErrorCode);
        }
        catch (Exception e) {
            StopRefresh();
            Debug.LogError("node:" + motorParams.nodeId + ", " + e.Message);
        }
    }

    //EposをDisable状態に変更
    private void DisableEpos(Motor_Params motorParams) {
        try {
            motorParams.sm.SetDisableState();
            Debug.Log("node: " + motorParams.nodeId + " Disabled");
        }
        catch (DeviceException e) {
            StopRefresh();
            Debug.LogError("node:" + motorParams.nodeId + ", " + e.ErrorMessage + ":" + e.ErrorCode);
        }
        catch (Exception e) {
            StopRefresh();
            Debug.LogError("node:" + motorParams.nodeId + ", " + e.Message);
        }
    }

    //UnityのInspectorにモータ情報を表示
    private void IndicateMotionInfo(Motor_Params L, Motor_Params R) {
        _position.x = L.position;
        _position.y = R.position;
        _velocity.x = L.velocity;
        _velocity.y = R.velocity;
        _current.x = L.current;
        _current.y = R.current;
        _isMoving = !(L.isReached && R.isReached);
        if (L.mode == R.mode) {
            _currentMode = L.mode;
        } else {
            _currentMode = "Different Mode Error";
        }
    }

    //Eposからモータ情報を取得
    private void GetMotionInfo(Motor_Params motorParams) {
        if (motorParams.epos != null) {
            try {
                motorParams.position = motorParams.epos.Operation.MotionInfo.GetPositionIs();
                motorParams.velocity = motorParams.epos.Operation.MotionInfo.GetVelocityIs();
                motorParams.current = motorParams.epos.Operation.MotionInfo.GetCurrentIs();
                motorParams.epos.Operation.MotionInfo.GetMovementState(ref motorParams.isReached);
            }
            catch (DeviceException e) {
                StopRefresh();
                Debug.LogError("node:" + motorParams.nodeId + ", " + e.ErrorMessage + ":" + e.ErrorCode);
            }
            catch (OverflowException e) {
                Debug.LogError("node:" + motorParams.nodeId + ", " + e.Message);
            }
            catch (FormatException e) {
                Debug.LogError("node:" + motorParams.nodeId + ", " + e.Message);
            }
            catch (Exception e) {
                StopRefresh();
                Debug.LogError("node:" + motorParams.nodeId + ", " + e.Message);
            }
        }
    }

    //Profile Position Modeに変更
    private void SetPpmMode(Motor_Params motorParams) {
        try {
            motorParams.ppm = motorParams.epos.Operation.ProfilePositionMode;
            motorParams.ppm.ActivateProfilePositionMode();
            motorParams.mode = motorParams.epos.Operation.OperationMode.GetOperationModeAsString();
            Debug.Log(motorParams.mode);
        }
        catch (DeviceException e) {
            StopRefresh();
            Debug.LogError("node:" + motorParams.nodeId + ", " + e.ErrorMessage + ":" + e.ErrorCode);
        }
        catch (OverflowException e) {
            Debug.LogError("node:" + motorParams.nodeId + ", " + e.Message);
        }
        catch (FormatException e) {
            Debug.LogError("node:" + motorParams.nodeId + ", " + e.Message);
        }
        catch (Exception e) {
            StopRefresh();
            Debug.LogError("node:" + motorParams.nodeId + ", " + e.Message);
        }
    }

    //モータを目標値に回転
    private void MoveToPosition(Motor_Params motorParams) {
        try {
            motorParams.ppm.SetPositionProfile(_profVel, _profAcc, _profDec);
            if (motorParams.isRiverse) {
                motorParams.ppm.MoveToPosition(-_targetPosition, _isAbsolute, _isImmediately);
            }
            else {
                motorParams.ppm.MoveToPosition(_targetPosition, _isAbsolute, _isImmediately);
            }
        }
        catch (DeviceException e) {
            StopRefresh();
            Debug.LogError("node:" + motorParams.nodeId + ", " + e.ErrorMessage + ":" + e.ErrorCode);
        }
        catch (OverflowException e) {
            Debug.LogError("node:" + motorParams.nodeId + ", " + e.Message);
        }
        catch (FormatException e) {
            Debug.LogError("node:" + motorParams.nodeId + ", " + e.Message);
        }
        catch (Exception e) {
            StopRefresh();
            Debug.LogError("node:" + motorParams.nodeId + ", " + e.Message);
        }
    }

    //Cyclic Synchronous Velocityモードに変更
    private void SetCSVMode(Motor_Params motorParams) {
        try {
            motorParams.epos.Operation.OperationMode.SetOperationMode(EOperationMode.OmdCyclicSynchronousVelocityMode);
            motorParams.mode = motorParams.epos.Operation.OperationMode.GetOperationModeAsString();
            Debug.Log(motorParams.mode);
        }
        catch (DeviceException e) {
            StopRefresh();
            Debug.LogError("node:" + motorParams.nodeId + ", " + e.ErrorMessage + ":" + e.ErrorCode);
        }
        catch (OverflowException e) {
            Debug.LogError("node:" + motorParams.nodeId + ", " + e.Message);
        }
        catch (FormatException e) {
            Debug.LogError("node:" + motorParams.nodeId + ", " + e.Message);
        }
        catch (Exception e) {
            StopRefresh();
            Debug.LogError("node:" + motorParams.nodeId + ", " + e.Message);
        }
    }

    //Eposを停止
    private void StopRefresh() {
        try {
            _motorParamsL = new Motor_Params();
            _motorParamsR = new Motor_Params();
        }
        catch (Exception e) {
            Debug.LogError(e.Message);
        }
    }
}
