using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoraDataChannel : MonoBehaviour
{
    Sora sora;
    [SerializeField] WalkDemoMainController controller;
    [SerializeField] string ChannelID;
    [SerializeField] string SignalingUrl;
    [SerializeField] string dataChannelLabel;
    float CoolTime = 0f;
    List<Sora.DataChannel> dataChannels;
    // Start is called before the first frame update
    void Start()
    {
        dataChannels = new List<Sora.DataChannel>();
        dataChannels.Add(new Sora.DataChannel() { Compress = false,Direction = Sora.Direction.Recvonly,Ordered = true,Label = dataChannelLabel });
        sora = new Sora();
        Sora.Config config = new Sora.Config()
        {
            ChannelId = ChannelID,
            SignalingUrl = SignalingUrl,
            Multistream = true,
            Role = Sora.Role.Recvonly,
            Video = false,
            Audio = true,
            UnityAudioOutput = false,
            UnityAudioInput = true,
            DataChannelSignaling = true,
            EnableDataChannelSignaling = true,
            UnityCamera = Camera.main,
            CapturerType = Sora.CapturerType.UnityCamera,
            UnityCameraRenderTargetDepthBuffer = 16,
            DataChannels = dataChannels
        };
     //   sora.OnNotify = (message) => { Debug.Log(message); };
        
        sora.OnMessage = (label,data) => 
        {
            if(label == this.dataChannelLabel)
            {
                if((data[0] == 0xcc) && (data[1] == 0x00) && isStart && CoolTime<0.01f)
                {
                    Debug.Log("Stop");
                    controller.walkStop = true;
                    isStart = false;
                    CoolTime = 2f;
                }
                else if ((data[0] == 0xcc) && (data[1] == 0x01) && !isStart && CoolTime < 0.01f)
                {
                    Debug.Log("Start");
                    isStart = true;
                    controller.walkStraight = true;
                    CoolTime = 2f;
                }
            }
        };
        sora.Connect(config);
      //  StartCoroutine(GetStats());
    }
    bool isStart = false;
    // Update is called once per frame
    void Update()
    {
        sora.DispatchEvents();
        sora.OnRender();
        CoolTime -= Time.deltaTime;
    }
    IEnumerator GetStats()
    {
        while (true)
        {
            yield return new WaitForSeconds(10);
 

            sora.GetStats((stats) =>
            {
                Debug.LogFormat("GetStats: {0}", stats);
            });
        }
    }
}
