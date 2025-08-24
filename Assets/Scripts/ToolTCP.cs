using System;
using System.Collections;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToolTCP : MonoBehaviour
{
    public string suiteIP = "172.16.136.164";  // Replace with the Raspberry Pi's IP address
    public int port = 25568;             // Replace with the port number used on the Raspberry Pi

    public TcpClient client;
    public NetworkStream stream;
    private Thread receiveThread;
    public bool isRunning = false;
    public bool startHeartbeat;

    public Button button;
    public TMP_InputField inputIP;

    public ToolController toolController;

    // Start is called before the first frame update
    void Start()
    {
        inputIP.text = suiteIP;
        client = new TcpClient();
    }

    // Update is called once per frame
    void Update()
    {
        if (client != null && !client.Connected)
        {
            //Debug.Log("SignalSender is not connected");
            button.image.color = Color.red;
        }
        if (startHeartbeat)
        {
            startHeartbeat = false;
            StartCoroutine(Heartbeat());
        }
    }

    public void ConnectToSuite()
    {
        if (client != null && !client.Connected)
        {
            try
            {
                // Create a TCP/IP socket
                client = new TcpClient();

                client.ConnectAsync(inputIP.text, port).Wait(1000);

                // Get a client stream for reading and writing
                stream = client.GetStream();

                isRunning = true;
                receiveThread = new Thread(new ThreadStart(ReadMessageFromSuite)) { IsBackground = true };
                receiveThread.Start();

                Debug.Log("Connected to suite");
                button.image.color = Color.green;
                button.interactable = false;
            }
            catch (SocketException e)
            {
                SocketError(e);
            }
        }
    }

    public void SendMessageToSuite(string message)
    {
        int packetSize = 4096;

        try
        {
            // Translate the signal string into bytes
            byte[] data = Encoding.ASCII.GetBytes(message + "$");

            // Send message count
            int packetCount = (int)Math.Ceiling((float)data.Length / packetSize);
            byte[] countData = Encoding.ASCII.GetBytes(packetCount.ToString());

            // Send messages
            for (int i = 0; i < packetCount; i++)
            {
                stream.Write(data[(i * packetSize)..Math.Min(((i + 1) * packetSize), data.Length)], 0, data.Length);
                Debug.Log($"Wrote message to stream: {message}");
            }
        }
        catch (Exception e)
        {
            SocketError(e);
        }
    }

    public void ReadMessageFromSuite()
    {
        byte[] buffer = new byte[4096];
        while (isRunning)
        {
            if (stream.CanRead)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead)[..^1];
                    Debug.Log("Received: " + receivedMessage);

                    toolController.HandleMessageFromSuite(receivedMessage);
                }
            }
        }
    }

    private void SocketError(Exception e)
    {
        Debug.Log("Socket Exception: " + e);
        button.image.color = Color.red;
        button.interactable = true;
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        receiveThread?.Abort();
        stream?.Close();
        client?.Close();
    }

    public IEnumerator Heartbeat()
    {
        yield return new WaitForSeconds(1f);
        if (client.Connected == false)
        {
            SocketError(new Exception("Heartbeat found no connection"));
        }
        else
        {
            StartCoroutine(Heartbeat());
        }
    }
}
