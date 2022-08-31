using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//UDP受信用
//リアルタイムグラフ描画，csv保存追加
//2020/12/22 山岡作成
public class UDPReceiver : MonoBehaviour
{

    public int LOCAL_PORT = 62000;
    static UdpClient udp;
    Thread thread;
    public bool receiveUDP =false;
   
    public static int LeftPedalPulse = 0;
    public static int LeftSliderPulse = 0;
    public static int RightPedalPulse = 0;
    public static int RightSliderPulse = 0;
    public static int LeftRotationPulse = 0;
    public static int RightRotationPulse = 0;


    public static float leftPedal = 0f;
    public static float leftSlider = 0f;
    public static float rightPedal = 0f;
    public static float rightSlider = 0f;
    public static float leftRotationAngle = 0;
    public static float rightRotationAngle = 0;
    private static float resolutionPedal = 0.0144f; //[degrees/pulse]
    private static float footLength = 155f;//回転部から踵端までの長さ
    private static float resolutionSlider = 0.012f; //[mm/pulse]
    public  static float dataTime;
    public static float dataMicroTime;
    private static string saveData1;
    private static string saveData2;
    private static int Rale = 510;
    private static float FirstAngle = 20;

    public static float X_Left = 0f;
    public static float Z_Left = 0f;
    public static float X_Right = 0f;
    public static float Z_Right = 0f;
    private static StreamWriter sw1;
    private static StreamWriter sw2;

    public void UDPStart()
    {
        udp = new UdpClient(LOCAL_PORT);
        thread = new Thread(new ThreadStart(ThreadMethod));
        sw1 = new StreamWriter(@"SaveData1.csv", false, Encoding.GetEncoding("Shift_JIS"));
        sw2 = new StreamWriter(@"SaveData2.csv", false, Encoding.GetEncoding("Shift_JIS"));
        string[] s1 = { "Time", "LeftPedal", "LeftSlider", "LeftRotationAngle", "RightPedal","RightSlider","RightRotationAngle" };//saveData1のラベル
        string[] s3 = { "Time", "X_Left", "Z_Left", "X_Right", "Z_Right" };//saveData2のラベル

        string s2 = string.Join(",", s1);//間にカンマ追加
        string s4 = string.Join(",", s3);//間にカンマ追加
        sw1.WriteLine(s2);//ラベル書き込み
        sw2.WriteLine(s4);//ラベル書き込み
        thread.Start();
        Debug.Log("UDP Receive START");

    }

    public static void ThreadMethod()
    {
        while (true)
        {
            IPEndPoint remoteEp = null;
            byte[] data = udp.Receive(ref remoteEp); //データ受信
            string text = Encoding.ASCII.GetString(data);//文字列に変換
            string[] pulse = text.Split(',');//カンマで区切る
            //↓文字を数値に変換
            dataMicroTime = int.Parse(pulse[0]);
            LeftPedalPulse = int.Parse(pulse[1]);
            LeftSliderPulse = int.Parse(pulse[2]);
            RightPedalPulse = int.Parse(pulse[3]);
            RightSliderPulse = int.Parse(pulse[4]);
            LeftRotationPulse = int.Parse(pulse[5]);
            RightRotationPulse = int.Parse(pulse[6]);

            CalculatePosition();
            sw1.WriteLine(saveData1);//csv保存
            sw2.WriteLine(saveData2);//csv保存
        }
    }

    void OnApplicationQuit()
    {
        sw1.Close();
        sw2.Close();
        thread.Abort();
    }
  
  void Update()
    {
       
        GraphPlot();
         
    }
    public static void GraphPlot()//グラフプロット用
    {
       
    }
    public static void CalculatePosition()//下肢駆動装置のパルス量から座標に変換
    {
        dataTime = dataMicroTime * 0.001f;
        leftPedal = -footLength * Mathf.Sin(LeftPedalPulse * resolutionPedal / Mathf.Rad2Deg);
        leftSlider = LeftSliderPulse * resolutionSlider;
        rightPedal = footLength * Mathf.Sin(RightPedalPulse * resolutionPedal / Mathf.Rad2Deg) ;
        rightSlider = RightSliderPulse * resolutionSlider;
        leftRotationAngle = -LeftRotationPulse* 120/11*0.0001f;
        rightRotationAngle = -(RightRotationPulse * 120) /  11*0.0001f;
        /*
        //各足初期位置を原点とした場合
        X_Left = Rale * Mathf.Cos((90+FirstAngle + leftRotationAngle) / Mathf.Rad2Deg) + leftSlider * Mathf.Sin(leftRotationAngle / Mathf.Rad2Deg)+Rale*Mathf.Sin(FirstAngle/Mathf.Rad2Deg);
        Z_Left = Rale * Mathf.Sin((90+FirstAngle + leftRotationAngle) / Mathf.Rad2Deg) + leftSlider * Mathf.Cos(leftRotationAngle / Mathf.Rad2Deg)-Rale*Mathf.Cos(FirstAngle / Mathf.Rad2Deg);
        X_Right = Rale * Mathf.Cos((90-FirstAngle + rightRotationAngle) / Mathf.Rad2Deg) + rightSlider * Mathf.Sin(rightRotationAngle / Mathf.Rad2Deg)-Rale* Mathf.Sin(FirstAngle / Mathf.Rad2Deg);
        Z_Right = Rale * Mathf.Sin((90-FirstAngle + rightRotationAngle) / Mathf.Rad2Deg) + rightSlider * Mathf.Cos(rightRotationAngle / Mathf.Rad2Deg)-Rale * Mathf.Cos(FirstAngle / Mathf.Rad2Deg);
        */

        //座席中心を原点とした場合
        X_Left = Rale * Mathf.Cos((90 + FirstAngle + leftRotationAngle) / Mathf.Rad2Deg) + leftSlider * Mathf.Sin(leftRotationAngle / Mathf.Rad2Deg);
        Z_Left = Rale * Mathf.Sin((90 + FirstAngle + leftRotationAngle) / Mathf.Rad2Deg) + leftSlider * Mathf.Cos(leftRotationAngle / Mathf.Rad2Deg);
        X_Right = Rale * Mathf.Cos((90-FirstAngle + rightRotationAngle) / Mathf.Rad2Deg) + rightSlider * Mathf.Sin(rightRotationAngle / Mathf.Rad2Deg);
        Z_Right = Rale * Mathf.Sin((90-FirstAngle + rightRotationAngle) / Mathf.Rad2Deg) + rightSlider * Mathf.Cos(rightRotationAngle / Mathf.Rad2Deg);
        

        saveData1 = dataTime.ToString() + "," + leftPedal.ToString() + "," + leftSlider.ToString() + "," + leftRotationAngle.ToString() + "," + rightPedal.ToString() + "," + rightSlider.ToString() + "," + rightRotationAngle.ToString();
        saveData2 = dataTime.ToString() + "," + X_Left.ToString() + ","+ leftPedal.ToString() + "," +Z_Left.ToString() + "," + X_Right.ToString() + "," + rightPedal.ToString() + "," + Z_Right.ToString();
    }
    



}
