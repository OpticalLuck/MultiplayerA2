using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class NetworkManager : Singleton<NetworkManager>
{
    public delegate void OnServerConnectDelegate();
    public event OnServerConnectDelegate OnServerConnectListeners;
    public delegate void OnPacketReceiveDelegate(byte[] data);
    public event OnPacketReceiveDelegate OnPacketReceivedListeners;
    public delegate void OnDisconnectDelegate();
    public event OnDisconnectDelegate OnDisconnectListeners;

    public static int dataBufferSize = 4096;
    public string IpAddress = "127.0.0.1";
    public int PortNumber = 7890;
    public int ClientID = 0;
    public string ClientName;
    public int LobbyID = 0;
    public TCP tcp { get; private set; }
    public bool IsConnected { get; private set; }

    void Start()
    {
        tcp = new TCP();
    }

    void OnEnable()
    {
        OnServerConnectListeners += OnServerConnect;
        OnPacketReceivedListeners += OnPacketReceived;
    }

    void OnDisable()
    {
        OnServerConnectListeners -= OnServerConnect;
        OnPacketReceivedListeners -= OnPacketReceived;
    }

    void OnDestroy()
    {
        Disconnect();
    }

    public void ConnectToServer()
    {
        tcp.Connect();
    }

    public void Disconnect()
    {
        if (IsConnected)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(DoOnServerDisconnect());
            IsConnected = false;
            tcp.socket.Close();

            Debug.Log("Disconnected from server.");
            
        }
    }

    public void SendData(string data)
    {
        tcp.SendData(data);
    }

    public void SendWhisper(int target, string text)
    {
        MessageData msgData;
        msgData.Sender = ClientName;
        msgData.Message = text;
        msgData.Mode = MessageData.SINGLE;
        msgData.Target = target;

        string serializedData = JsonConvert.SerializeObject(msgData);

        JObject packet = new JObject();
        packet.Add(new JProperty(MessageData.Prefix, JObject.Parse(serializedData)));

        SendData(packet.ToString());
        Debug.Log("Sending: " + packet.ToString());
    }

    public void SendToAll(string text)
    {
        MessageData msgData;
        msgData.Sender = ClientName;
        msgData.Message = text;
        msgData.Mode = MessageData.ALL;
        msgData.Target = -1;

        string serializedData = JsonConvert.SerializeObject(msgData);

        JObject packet = new JObject();
        packet.Add(new JProperty(MessageData.Prefix, JObject.Parse(serializedData)));

        SendData(packet.ToString());
        Debug.Log("Sending: " + packet.ToString());
    }

    public void SendToLobby(string text)
    {
        MessageData msgData;
        msgData.Sender = ClientName;
        msgData.Message = text;
        msgData.Mode = MessageData.GROUP;
        msgData.Target = LobbyID;

        string serializedData = JsonConvert.SerializeObject(msgData);

        JObject packet = new JObject();
        packet.Add(new JProperty(MessageData.Prefix, JObject.Parse(serializedData)));

        SendData(packet.ToString());
        Debug.Log("Sending: " + packet.ToString());
    }

    public class TCP
    {
        public TcpClient socket;
        private NetworkStream stream;
        private byte[] receiveBuffer;

        public void ClearBuffer()
        {
            Array.Clear(receiveBuffer, 0, receiveBuffer.Length);
        }

        public void Connect()
        {
            socket = new TcpClient
            {
                ReceiveBufferSize = dataBufferSize,
                SendBufferSize = dataBufferSize
            };

            receiveBuffer = new byte[dataBufferSize];

            socket.BeginConnect(Instance.IpAddress, Instance.PortNumber, ConnectCallback, socket);
        }

        private void Disconnect()
        {
            Instance.Disconnect();

            stream = null;
            receiveBuffer = null;
            socket = null;
        }

        public void SendData(string outgoingData)
        {
            if (stream.CanWrite)
            {
                byte[] data = Encoding.UTF8.GetBytes(outgoingData);
                stream.Write(data, 0, data.Length);
            }
        }

        private void ConnectCallback(IAsyncResult _result)
        {
            socket.EndConnect(_result);

            if (!socket.Connected)
                return;

            Instance.IsConnected = true;

            stream = socket.GetStream();
            UnityMainThreadDispatcher.Instance().Enqueue(Instance.DoOnServerConnect());

            stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
        }

        private void ReceiveCallback(IAsyncResult _result)
        {
            try
            {
                int _byteLength = stream.EndRead(_result);

                if (_byteLength <= 0)
                {
                    Instance.Disconnect();
                    return;
                }

                byte[] _data = new byte[_byteLength];

                Array.Copy(receiveBuffer, _data, _byteLength);
                ClearBuffer();

                UnityMainThreadDispatcher.Instance().Enqueue(Instance.DoOnPacketReceived(_data));

                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }
            catch
            {
                Disconnect();
            }
        }
    }

    #region Callbacks
    IEnumerator DoOnServerConnect()
    {
        var OnServerConnect = OnServerConnectListeners;
        OnServerConnect.Invoke();
        yield break;
    }

    IEnumerator DoOnPacketReceived(byte[] data)
    {
        yield return OnPacketReceivedListeners;

        if (OnPacketReceivedListeners != null)
            OnPacketReceivedListeners.Invoke(data);

        //tcp.ClearBuffer();
        yield break;
    }

    IEnumerator DoOnServerDisconnect()
    {
        if (OnDisconnectListeners != null)
            OnDisconnectListeners.Invoke();

        //tcp.ClearBuffer();
        yield break;
    }

    protected void OnServerConnect()
    {
        
    }

    protected void OnPacketReceived(byte[] data)
    {
        Debug.Log("Received: " + Encoding.UTF8.GetString(data));

        JObject jobject = JObject.Parse(Encoding.UTF8.GetString(data));
        if (jobject.ContainsKey(LoginData.Prefix))
        {
            ClientID = jobject[LoginData.Prefix]["ID"].ToObject<int>();
            Debug.Log(ClientID);
        }
    }
    #endregion
}