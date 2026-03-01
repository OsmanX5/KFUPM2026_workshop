using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Sirenix.OdinInspector;
using UnityEngine;

public class JSBSimBridgeTest1 : MonoBehaviour
{
    #region Inspector Fields

    [BoxGroup("UDP Settings")]
    [SerializeField] private int udpReceivePort = 5138;
    [BoxGroup("UDP Settings")]
    [SerializeField] private int udpSendPort = 5139;
    [BoxGroup("UDP Settings")]
    [SerializeField] private string jsbSimHost = "127.0.0.1";

    [BoxGroup("Visualization")]
    [SerializeField] private Transform targetObject;

    [BoxGroup("Control")]
    [SerializeField] private float sideForce = 100f;
    [BoxGroup("Control")]
    [SerializeField] private float pushForce = 100f;
    [BoxGroup("Control")]
    [ReadOnly, SerializeField] private float currentSideInput;
    [BoxGroup("Control")]
    [ReadOnly, SerializeField] private float currentPushInput;

    [BoxGroup("Conversion")]
    [SerializeField] private float feetToMeters = 0.3048f;
    [BoxGroup("Conversion")]
    [SerializeField] private float metersPerDegree = 111320f;
    [BoxGroup("Conversion")]
    [SerializeField] private double refLatitude = 0.0;
    [BoxGroup("Conversion")]
    [SerializeField] private double refLongitude = -90.0;

    [BoxGroup("Status")]
    [ReadOnly, SerializeField] private float simTime;

    [BoxGroup("Status")]
    [ReadOnly, SerializeField] private float altitude;

    [BoxGroup("Status")]
    [ReadOnly, SerializeField] private double latitude;

    [BoxGroup("Status")]
    [ReadOnly, SerializeField] private double longitude;

    [BoxGroup("Status")]
    [ReadOnly, SerializeField] private int packetsReceived;

    [BoxGroup("Debug")]
    [SerializeField] private bool logEveryUpdate = false;
    [BoxGroup("Debug")]
    [SerializeField] private bool logControlCommands = true;
    [BoxGroup("Debug")]
    [ReadOnly, SerializeField] private int commandsSent = 0;

    #endregion

    #region Private Fields

    private UdpClient udpClient;
    private UdpClient udpSender;
    private IPEndPoint jsbSimEndPoint;
    private Thread receiveThread;
    private bool isRunning = false;

    private readonly object dataLock = new object();
    private bool hasNewData = false;
    private string latestMessage = "";

    #endregion

    #region Unity Lifecycle

    void Start()
    {
        if (targetObject == null)
            targetObject = transform;

        StartUdpReceiver();
    }

    void Update()
    {
        // Handle keyboard control input
        HandleControlInput();

        string messageToProcess = null;

        lock (dataLock)
        {
            if (hasNewData)
            {
                messageToProcess = latestMessage;
                hasNewData = false;
            }
        }

        if (messageToProcess != null)
        {
            ParseData(messageToProcess);
            packetsReceived++;
            UpdateTransform();

            if (logEveryUpdate)
            {
                Debug.Log($"[JSBSim] Time: {simTime:F2}s | Alt: {altitude:F1}");
            }
        }
    }

    void OnDestroy()
    {
        StopUdpReceiver();
    }

    void OnApplicationQuit()
    {
        StopUdpReceiver();
    }

    #endregion

    #region UDP Receiver

    void StartUdpReceiver()
    {
        try
        {
            udpClient = new UdpClient(udpReceivePort);
            isRunning = true;

            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            // Initialize sender for control commands
            udpSender = new UdpClient();
            jsbSimEndPoint = new IPEndPoint(IPAddress.Parse(jsbSimHost), udpSendPort);

            Debug.Log($"[JSBSim] UDP Receiver started on port {udpReceivePort}, Sender to port {udpSendPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[JSBSim] Failed to start UDP: {e.Message}");
        }
    }

    void ReceiveData()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

        while (isRunning)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEP);
                string message = Encoding.UTF8.GetString(data);

                lock (dataLock)
                {
                    latestMessage = message;
                    hasNewData = true;
                }
            }
            catch (SocketException)
            {
                break;
            }
            catch (Exception e)
            {
                if (isRunning)
                {
                    Debug.LogError($"[JSBSim] Receive error: {e.Message}");
                }
            }
        }
    }

    void StopUdpReceiver()
    {
        isRunning = false;

        if (udpClient != null)
        {
            udpClient.Close();
            udpClient = null;
        }

        if (udpSender != null)
        {
            udpSender.Close();
            udpSender = null;
        }

        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Join(100);
            receiveThread = null;
        }

        Debug.Log("[JSBSim] UDP stopped");
    }

    #endregion

    #region Control Input

    void HandleControlInput()
    {
        // Side force: A/D or Left/Right arrows
        currentSideInput = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            currentSideInput = -1f;
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            currentSideInput = 1f;

        // Push force: W/S or Up/Down arrows
        currentPushInput = 0f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            currentPushInput = 1f;
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            currentPushInput = -1f;

        // Send control commands to JSBSim
        if (udpSender != null && jsbSimEndPoint != null)
        {
            SendControlCommand();
        }
    }

    void SendControlCommand()
    {
        // Only send when there's actual input
        if (currentSideInput == 0f && currentPushInput == 0f)
            return;

        try
        {
            // Format: property=value\n for each property
            // JSBSim expects properties to be set via socket input
            string command = $"set fcs/yaw-cmd-norm {currentSideInput}\nset fcs/throttle-cmd-norm {currentPushInput}\n";

            byte[] data = Encoding.UTF8.GetBytes(command);
            udpSender.Send(data, data.Length, jsbSimEndPoint);
            commandsSent++;

            if (logControlCommands)
            {
                Debug.Log($"[JSBSim] Sent: side={currentSideInput}, push={currentPushInput}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[JSBSim] Send error: {e.Message}");
        }
    }

    #endregion

    #region Data Processing

    void ParseData(string message)
    {
        try
        {
            string[] values = message.Trim().Split(',');

            // Handle both 3 values (alt, lat, lon) and 4 values (time, alt, lat, lon)
            if (values.Length >= 3)
            {
                if (values.Length >= 4)
                {
                    simTime = float.Parse(values[0].Trim());
                    altitude = float.Parse(values[1].Trim());
                    latitude = double.Parse(values[2].Trim());
                    longitude = double.Parse(values[3].Trim());
                }
                else
                {
                    altitude = float.Parse(values[0].Trim());
                    latitude = double.Parse(values[1].Trim());
                    longitude = double.Parse(values[2].Trim());
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[JSBSim] Parse error: {e.Message} | Raw: {message}");
        }
    }

    void UpdateTransform()
    {
        // Convert lat/lon to Unity X/Z (relative to reference point)
        float deltaLat = (float)(latitude - refLatitude);
        float deltaLon = (float)(longitude - refLongitude);

        // North -> Z, East -> X
        float x = deltaLon * metersPerDegree * Mathf.Cos((float)(latitude * Mathf.Deg2Rad));
        float y = altitude * feetToMeters;
        float z = deltaLat * metersPerDegree;

        targetObject.position = new Vector3(x, y, z);
    }

    #endregion
}
