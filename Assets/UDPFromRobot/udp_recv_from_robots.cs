using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System;
using System.Threading;

public class udp_recv_from_robots : MonoBehaviour
{
    [SerializeField] WalkDemoMainController controller;
    double CoolTime = 0f;
    bool isStart = false;
    UdpClient udpClient;
    Thread receiveThread;
    IPEndPoint receiveEP = new IPEndPoint(IPAddress.Any, 4009);

    Thread udp_observer_thread;
    void Awake()
    {
        udpClient = new UdpClient(receiveEP);

        receiveThread = new Thread(new ThreadStart(ThreadReceive));
        receiveThread.Start();
        print("受信セットアップ完了");

        StartCoroutine("udp_observer");
        Debug.Log("Awake");
    }

    void ThreadReceive()
    {
        while (true)
        {
            Debug.Log("hello");
            IPEndPoint senderEP = null;
            byte[] receivedBytes = udpClient.Receive(ref senderEP);
            Parse(senderEP, receivedBytes);
        }
    }

    int counta = 0;

    void Parse(IPEndPoint senderEP, byte[] message)
    {
        //受信時の処理
        counta++;
        if (message[0] == 0xc0 && !isStart && CoolTime < 0.01f)
        {
            Debug.Log("Start");
            isStart = true;
            controller.walkStraight = true;
            CoolTime = 2f;
        }
    }

    IEnumerator udp_observer()
    {
        int old_counta = 0;
        int count_stop = 0;
        while (true)
        {
            if (old_counta == counta)
            {
                count_stop++;
            }
            else
            {
                count_stop = 0;
            }
            old_counta = counta;

            if (count_stop > 5 && isStart && CoolTime < 0.01f)
            {
                Debug.Log("Stop");
                controller.walkStop = true;
                isStart = false;
                CoolTime = 2f;
            }
            yield return new WaitForSeconds((float)0.1);
        }
        yield return new WaitForSeconds((float)0.5);
    }

    void Update()
    {
        CoolTime -= Time.deltaTime;
    }
}