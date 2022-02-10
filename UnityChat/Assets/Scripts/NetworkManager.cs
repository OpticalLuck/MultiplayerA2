using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using TMPro;

public class NetworkManager : MonoBehaviour
{
    public string IpAddress = "127.0.0.1";
    public int PortNumber = 7890;
    public bool IsConnected = false;

    public TMP_Text StatusText;
    
    private IPAddress _ipAddress;
    private TcpClient _tcpClient;

    // Start is called before the first frame update
    void Start()
    {
        _ipAddress = IPAddress.Parse("127.0.0.1");
        _tcpClient = new TcpClient();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ConnectToServer()
    {
        StartCoroutine(DoConnectServer());
    }

    private void OnDestroy()
    {
        _tcpClient.Close();
    }

    IEnumerator DoConnectServer()
    {
        yield return _tcpClient.ConnectAsync(_ipAddress, PortNumber);

        IsConnected = _tcpClient.Connected;
        string toReplace = StatusText.text.Substring(StatusText.text.IndexOf(':') + 1, StatusText.text.Length - StatusText.text.IndexOf(':') - 1);
        StatusText.text = StatusText.text.Replace(toReplace, " Connected");
    }
}
