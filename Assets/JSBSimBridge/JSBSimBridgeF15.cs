using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using CesiumForUnity;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class JSBSimBridgeF15 : MonoBehaviour
{
    #region Inspector Fields
    [SerializeField]
    JSbSimServerRunner jsbSimServerRunner;
    [BoxGroup("UDP Settings")]
    [SerializeField] private int udpReceivePort = 5138;

    [BoxGroup("UDP Settings")]
    [SerializeField] private int udpSendPort = 5139;

    [BoxGroup("UDP Settings")]
    [SerializeField] private string jsbsimHost = "127.0.0.1";

    [BoxGroup("Cesium")]
    [SerializeField] private CesiumGlobeAnchor globeAnchor;

    [BoxGroup("Flight Controls")]
    [Range(0f, 1f)]
    [SerializeField] private float throttle = 0.9f;

    [BoxGroup("Flight Controls")]
    [Range(-1f, 1f)]
    [SerializeField] private float elevator = 0f;

    [BoxGroup("Flight Controls")]
    [Range(-1f, 1f)]
    [SerializeField] private float aileron = 0f;

    [BoxGroup("Flight Controls")]
    [Range(-1f, 1f)]
    [SerializeField] private float rudder = 0f;

    [BoxGroup("Flight Controls")]
    [SerializeField] private bool sendControlsEveryFrame = true;

    [BoxGroup("Flight Data")]
    [ReadOnly, SerializeField] private float simTime;

    [BoxGroup("Flight Data")]
    [ReadOnly, SerializeField] private float altitudeFt;

    [BoxGroup("Flight Data")]
    [ReadOnly, SerializeField] private double latitudeDeg;

    [BoxGroup("Flight Data")]
    [ReadOnly, SerializeField] private double longitudeDeg;

    [BoxGroup("Flight Data")]
    [ReadOnly, SerializeField] private float rollDeg;

    [BoxGroup("Flight Data")]
    [ReadOnly, SerializeField] private float pitchDeg;

    [BoxGroup("Flight Data")]
    [ReadOnly, SerializeField] private float headingDeg;

    [BoxGroup("Flight Data")]
    [ReadOnly, SerializeField] private float airspeedKts;

    [BoxGroup("Status")]
    [ReadOnly, SerializeField] private int packetsReceived;

    [BoxGroup("Status")]
    [ReadOnly, SerializeField] private int packetsSent;

    [BoxGroup("Status")]
    [ReadOnly, SerializeField] private bool isReceiving;

    [BoxGroup("Debug")]
    [SerializeField] private bool logEveryPacket = false;
    [SerializeField]
    bool resumeSimulationOnStart = false;
    #endregion

    #region Private Fields

    private UdpClient udpReceiver;
    [ShowInInspector]
    private TcpClient tcpSender;
    private Thread receiveThread;
    private bool isRunning = false;

    private readonly object dataLock = new object();
    private bool hasNewData = false;
    private string latestMessage = "";

    private const float FeetToMeters = 0.3048f;

    // Track previous control values to only send on change
    private float lastThrottle = -1f;
    private float lastElevator = -999f;
    private float lastAileron = -999f;
    private float lastRudder = -999f;

    bool isSimulationRunning = false;

    #endregion

    #region Unity Lifecycle

    void Start()
    {
        if (globeAnchor == null)
            globeAnchor = GetComponent<CesiumGlobeAnchor>();

        if (globeAnchor == null)
        {
            Debug.LogError("[F15] CesiumGlobeAnchor component required! Add it to this GameObject.");
            return;
        }

        // Disable automatic transform change detection since we're controlling position
        globeAnchor.detectTransformChanges = false;
        globeAnchor.adjustOrientationForGlobeWhenMoving = false;
        jsbSimServerRunner.OnServerStarted += JSBSimServer_Started;

    }

    private void JSBSimServer_Started()
    {
        StartUdpReceiver();
        StartUdpSender();
        if (resumeSimulationOnStart)
        {
            Invoke(nameof(ResumeSimulation), 5f);
        }
    }

    void Update()
    {
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
            UpdateAircraftTransform();

            if (logEveryPacket)
            {
                LogFlightData();
            }
        }

        // Send control inputs to JSBSim
        if (sendControlsEveryFrame)
        {
            SendControlInputs();
        }
        else
        {
            SendControlInputsIfChanged();
        }

    }

    void OnDestroy()
    {
        StopUdpReceiver();
        StopUdpSender();
    }

    void OnApplicationQuit()
    {
        StopUdpReceiver();
        StopUdpSender();
    }

    #endregion

    #region Public Control Methods

    /// <summary>
    /// Set throttle value (0-1)
    /// </summary>
    [Button("Set Throttle")]
    public void SetThrottle(float value)
    {
        throttle = Mathf.Clamp01(value);
        SendControlInputs();
    }

    /// <summary>
    /// Set elevator value (-1 to 1, positive = pitch up)
    /// </summary>
    [Button("Set Elevator")]
    public void SetElevator(float value)
    {
        elevator = Mathf.Clamp(value, -1f, 1f);
        SendControlInputs();
    }

    /// <summary>
    /// Set aileron value (-1 to 1, positive = roll right)
    /// </summary>
    [Button("Set Aileron")]
    public void SetAileron(float value)
    {
        aileron = Mathf.Clamp(value, -1f, 1f);
        SendControlInputs();
    }


    /// <summary>
    /// Set rudder value (-1 to 1, positive = yaw right)
    /// </summary>
    [Button("Set Rudder")]
    public void SetRudder(float value)
    {
        rudder = Mathf.Clamp(value, -1f, 1f);
        SendControlInputs();
    }

    /// <summary>
    /// Set all flight controls at once
    /// </summary>
    public void SetControls(float throttleValue, float elevatorValue, float aileronValue, float rudderValue)
    {
        throttle = Mathf.Clamp01(throttleValue);
        elevator = Mathf.Clamp(elevatorValue, -1f, 1f);
        aileron = Mathf.Clamp(aileronValue, -1f, 1f);
        rudder = Mathf.Clamp(rudderValue, -1f, 1f);
        SendControlInputs();
    }

    /// <summary>
    /// Send a raw property command to JSBSim
    /// </summary>
    public void SendProperty(string propertyName, double value)
    {
        string message = $"set {propertyName} {value.ToString(CultureInfo.InvariantCulture)}\n";
        SendToJSBSim(message);
    }

    #endregion
    [SerializeField]
    bool showLog;

    #region UDP Sender

    void StartUdpSender()
    {
        try
        {
            tcpSender = new TcpClient();

            tcpSender.Connect(jsbsimHost, udpSendPort);
            byte[] testData = Encoding.ASCII.GetBytes("ping\n");
            NetworkStream stream = tcpSender.GetStream();
            stream.Write(testData, 0, testData.Length);

            byte[] buffer = new byte[1024];

            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string returnData = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            if (showLog)
            {
                Debug.Log($"[F15] Received from JSBSim: {returnData.Trim()}");
                Debug.Log($"[F15] UDP Sender ready to send to {jsbsimHost}:{udpSendPort}");
            }


        }
        catch (Exception e)
        {
            Debug.LogError($"[F15] Failed to start UDP sender: {e.Message}");
        }
    }

    void SendControlInputs()
    {
        if (tcpSender == null) return;

        // JSBSim expects property set commands in format: "set property_name value\n"
        StringBuilder sb = new StringBuilder();

        // Throttle (both engines)
        sb.AppendLine($"set fcs/throttle-cmd-norm[0] {throttle.ToString("F4", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"set fcs/throttle-cmd-norm[1] {throttle.ToString("F4", CultureInfo.InvariantCulture)}");

        // Flight controls
        sb.AppendLine($"set fcs/elevator-cmd-norm {elevator.ToString("F4", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"set fcs/aileron-cmd-norm {aileron.ToString("F4", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"set fcs/rudder-cmd-norm {rudder.ToString("F4", CultureInfo.InvariantCulture)}");

        SendToJSBSim(sb.ToString());

        // Update tracking
        lastThrottle = throttle;
        lastElevator = elevator;
        lastAileron = aileron;
        lastRudder = rudder;
    }

    void SendControlInputsIfChanged()
    {
        bool changed = Math.Abs(throttle - lastThrottle) > 0.001f ||
                       Math.Abs(elevator - lastElevator) > 0.001f ||
                       Math.Abs(aileron - lastAileron) > 0.001f ||
                       Math.Abs(rudder - lastRudder) > 0.001f;

        if (changed)
        {
            SendControlInputs();
        }
    }

    void SendToJSBSim(string message)
    {
        if (tcpSender == null) return;

        try
        {
            NetworkStream stream = tcpSender.GetStream();
            byte[] data = Encoding.ASCII.GetBytes($"{message}\n");
            stream.Write(data, 0, data.Length);   // Write to the stream
            packetsSent++;
        }
        catch (Exception e)
        {
            Debug.LogError($"[F15] Send error: {e.Message}");
        }
    }

    void StopUdpSender()
    {
        if (tcpSender != null)
        {
            tcpSender.Close();
            tcpSender = null;
        }
        Debug.Log("[F15] UDP Sender stopped");
    }

    #endregion

    #region UDP Receiver

    void StartUdpReceiver()
    {
        try
        {
            udpReceiver = new UdpClient(udpReceivePort);
            isRunning = true;
            isReceiving = true;

            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            Debug.Log($"[F15] UDP Receiver started on port {udpReceivePort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[F15] Failed to start UDP receiver: {e.Message}");
            isReceiving = false;
        }
    }

    void ReceiveData()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

        while (isRunning)
        {
            try
            {
                byte[] data = udpReceiver.Receive(ref remoteEP);
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
                    Debug.LogError($"[F15] Receive error: {e.Message}");
                }
            }
        }
    }

    void StopUdpReceiver()
    {
        isRunning = false;
        isReceiving = false;

        if (udpReceiver != null)
        {
            udpReceiver.Close();
            udpReceiver = null;
        }

        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Join(100);
            receiveThread = null;
        }

        Debug.Log("[F15] UDP Receiver stopped");
    }

    #endregion

    #region Data Processing

    void ParseData(string message)
    {
        try
        {
            string[] values = message.Trim().Split(',');

            // Expected: simTime (default), altitude, lat, lon, roll, pitch, heading, airspeed
            if (values.Length >= 8)
            {
                simTime = float.Parse(values[0].Trim());
                altitudeFt = float.Parse(values[1].Trim());
                latitudeDeg = double.Parse(values[2].Trim());
                longitudeDeg = double.Parse(values[3].Trim());
                rollDeg = float.Parse(values[4].Trim());
                pitchDeg = float.Parse(values[5].Trim());
                headingDeg = float.Parse(values[6].Trim());
                airspeedKts = float.Parse(values[7].Trim());
            }
            else
            {
                Debug.LogWarning($"[F15] Expected 8 values, got {values.Length} | Raw: {message}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[F15] Parse error: {e.Message} | Raw: {message}");
        }
    }

    void UpdateAircraftTransform()
    {
        if (globeAnchor == null) return;

        // Convert altitude from feet to meters
        double altitudeMeters = altitudeFt * FeetToMeters;

        // Set position using CesiumGlobeAnchor (longitude, latitude, height)
        globeAnchor.longitudeLatitudeHeight = new double3(longitudeDeg, latitudeDeg, altitudeMeters);

        // Set rotation using East-Up-North frame
        // In EUN: +X = East, +Y = Up, +Z = North
        // JSBSim: heading 0 = North, 90 = East (clockwise from North)
        // Roll: positive = right wing down
        // Pitch: positive = nose up

        // Convert JSBSim angles to EUN quaternion
        // Heading rotates around Up (Y), Pitch around East (X), Roll around North (Z)
        // But we need to apply in correct order for aircraft: Yaw, Pitch, Roll

        // In EUN frame, heading 0 = facing North (+Z)
        // Heading 90 = facing East (+X)
        float yawRad = headingDeg * Mathf.Deg2Rad;
        float pitchRad = pitchDeg * Mathf.Deg2Rad;
        float rollRad = rollDeg * Mathf.Deg2Rad;

        // Create rotation: first heading (around Y/Up), then pitch (around X/East), then roll (around Z/North)
        quaternion headingRot = quaternion.RotateY(yawRad);
        quaternion pitchRot = quaternion.RotateX(-pitchRad); // Negative because Unity pitch is opposite
        quaternion rollRot = quaternion.RotateZ(-rollRad);   // Negative because Unity roll is opposite

        // Combine: Heading * Pitch * Roll (applied in reverse order)
        quaternion finalRotation = math.mul(math.mul(headingRot, pitchRot), rollRot);

        globeAnchor.rotationEastUpNorth = finalRotation;
    }

    void LogFlightData()
    {
        if (showLog)
        {
            Debug.Log($"[F15] T:{simTime:F1}s | Alt:{altitudeFt:F0}ft | " +
                    $"Lat:{latitudeDeg:F4}° Lon:{longitudeDeg:F4}° | " +
                    $"Roll:{rollDeg:F1}° Pitch:{pitchDeg:F1}° Hdg:{headingDeg:F1}° | " +
                    $"Speed:{airspeedKts:F0}kts");
        }
    }

    [Button(ButtonSizes.Large)]
    public void ResumeSimulation()
    {
        isSimulationRunning = true;
        SendToJSBSim("resume\n");
    }
    [Button(ButtonSizes.Large)]
    public void HoldSimulation()
    {
        isSimulationRunning = false;
        SendToJSBSim("hold\n");
    }
    #endregion

    [SerializeField]
    bool testJoystickInput = false;
    public void ReadyEleavoterAndRudderFromJoystick(Vector3 input)
    {
        if (!testJoystickInput) return;

        elevator = input.y;
        rudder = input.x;
    }
}
