using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Sirenix.OdinInspector;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class JSbSimServerRunner : MonoBehaviour
{
    [Header("JSBSim Configuration")]
    [Tooltip("Name of the JSBSim folder inside StreamingAssets (for JSBSim.exe)")]
    [SerializeField] private string jsbsimFolder = "JSBSim";

    [Tooltip("Name of the example folder inside StreamingAssets (e.g., F15Example, BallExample)")]
    [SerializeField] private string exampleFolder = "F15Example";

    [Tooltip("Aircraft name (folder name inside aircraft folder)")]
    [SerializeField] private string aircraftName = "f15";

    [Tooltip("Initialization file name (inside aircraft folder)")]
    [SerializeField] private string initFile = "cruise_init.xml";

    [Tooltip("Path to jsbsim executable (leave empty to use JSBSim.exe from StreamingAssets/JSBSim)")]
    [SerializeField] private string jsbsimExecutablePath = "";

    [Tooltip("Run in realtime mode")]
    [SerializeField] private bool realtime = true;

    [Tooltip("Start in suspended mode")]
    [SerializeField] private bool suspend = true;

    [Tooltip("Auto-start server on Awake")]
    [SerializeField] private bool autoStart = true;

    // Events
    public event Action OnServerStarted;
    public event Action OnServerStopped;
    public event Action<string> OnServerOutput;
    public event Action<string> OnServerError;

    // Properties
    [ShowInInspector,ReadOnly]
    public bool IsRunning => _jsbsimProcess != null && !_jsbsimProcess.HasExited;
    [ShowInInspector,ReadOnly]
    
    public string StreamingAssetsPath => _streamingAssetsPath;
    [ShowInInspector,ReadOnly]
    public string AircraftPath => _aircraftPath;

    private Process _jsbsimProcess;
    [ShowInInspector,ReadOnly]
    private string _streamingAssetsPath;
    [ShowInInspector,ReadOnly]
    private string _aircraftPath;
    [ShowInInspector,ReadOnly]
    private Thread _outputThread;
    [ShowInInspector,ReadOnly]
    private Thread _errorThread;
    [ShowInInspector,ReadOnly]
    private bool _isShuttingDown;
    [ShowInInspector,ReadOnly]
    bool isPathValid = false;
    private void Awake()
    {
        isPathValid = ValidatePaths();

        if (autoStart && isPathValid)
        {
            StartServer();
        }
    }

    private void OnDestroy()
    {
        StopServer();
    }

    private void OnApplicationQuit()
    {
        StopServer();
    }

    /// <summary>
    /// Validates that all required paths and files exist
    /// </summary>
    /// <returns>True if all paths are valid</returns>
    public bool ValidatePaths()
    {
        _streamingAssetsPath = Path.Combine(Application.streamingAssetsPath, exampleFolder);
        _aircraftPath = Path.Combine(_streamingAssetsPath, "aircraft");

        // Check if example folder exists
        if (!Directory.Exists(_streamingAssetsPath))
        {
            Debug.LogError($"[JSBSimServerRunner] Example folder not found: {_streamingAssetsPath}");
            return false;
        }

        // Check if aircraft folder exists
        if (!Directory.Exists(_aircraftPath))
        {
            Debug.LogError($"[JSBSimServerRunner] Aircraft folder not found: {_aircraftPath}");
            return false;
        }

        // Check if specific aircraft folder exists
        string specificAircraftPath = Path.Combine(_aircraftPath, aircraftName);
        if (!Directory.Exists(specificAircraftPath))
        {
            Debug.LogError($"[JSBSimServerRunner] Aircraft '{aircraftName}' folder not found: {specificAircraftPath}");
            return false;
        }

        // Check if init file exists
        string initFilePath = Path.Combine(specificAircraftPath, initFile);
        if (!File.Exists(initFilePath))
        {
            Debug.LogError($"[JSBSimServerRunner] Init file not found: {initFilePath}");
            return false;
        }

        Debug.Log($"[JSBSimServerRunner] All paths validated successfully");
        Debug.Log($"[JSBSimServerRunner] StreamingAssets: {_streamingAssetsPath}");
        Debug.Log($"[JSBSimServerRunner] Aircraft: {aircraftName}");
        Debug.Log($"[JSBSimServerRunner] Init File: {initFile}");

        return true;
    }

    /// <summary>
    /// Starts the JSBSim server process
    /// </summary>
    public void StartServer()
    {
        if (IsRunning)
        {
            Debug.LogWarning("[JSBSimServerRunner] Server is already running");
            return;
        }

        if (!ValidatePaths())
        {
            Debug.LogError("[JSBSimServerRunner] Cannot start server - path validation failed");
            return;
        }

        _isShuttingDown = false;

        try
        {
            // Use JSBSim.exe from StreamingAssets/JSBSim if no custom path specified
            string jsbsimPath = Path.Combine(Application.streamingAssetsPath, jsbsimFolder);
            string executable = string.IsNullOrEmpty(jsbsimExecutablePath)
                ? Path.Combine(jsbsimPath, "JSBSim.exe")
                : jsbsimExecutablePath;

            // Build command arguments
            string arguments = $"--aircraft={aircraftName} --initfile={initFile}";

            if (realtime)
                arguments += " --realtime";

            if (suspend)
                arguments += " --suspend";

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                WorkingDirectory = _streamingAssetsPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            Debug.Log($"[JSBSimServerRunner] Starting JSBSim server...");
            Debug.Log($"[JSBSimServerRunner] Command: {executable} {arguments}");
            Debug.Log($"[JSBSimServerRunner] Working Directory: {_streamingAssetsPath}");

            _jsbsimProcess = new Process { StartInfo = startInfo };
            _jsbsimProcess.Start();

            // Start threads to read output and error streams
            _outputThread = new Thread(ReadOutput) { IsBackground = true };
            _outputThread.Start();

            _errorThread = new Thread(ReadError) { IsBackground = true };
            _errorThread.Start();

            // Wait a brief moment to check if process started successfully
            Thread.Sleep(100);

            if (_jsbsimProcess.HasExited)
            {
                Debug.LogError($"[JSBSimServerRunner] JSBSim process exited immediately with code: {_jsbsimProcess.ExitCode}");
                return;
            }

            Debug.Log($"[JSBSimServerRunner] JSBSim server started successfully (PID: {_jsbsimProcess.Id})");

            // Fire the started event on the main thread
            UnityMainThreadDispatcher.Enqueue(() => OnServerStarted?.Invoke());
        }
        catch (Exception ex)
        {
            Debug.LogError($"[JSBSimServerRunner] Failed to start JSBSim server: {ex.Message}");
            Debug.LogError($"[JSBSimServerRunner] Make sure JSBSim.exe exists in StreamingAssets/JSBSim or set the executable path explicitly");
        }
    }

    /// <summary>
    /// Stops the JSBSim server process
    /// </summary>
    public void StopServer()
    {
        _isShuttingDown = true;

        if (_jsbsimProcess != null && !_jsbsimProcess.HasExited)
        {
            try
            {
                Debug.Log("[JSBSimServerRunner] Stopping JSBSim server...");
                _jsbsimProcess.Kill();
                _jsbsimProcess.WaitForExit(5000);
                Debug.Log("[JSBSimServerRunner] JSBSim server stopped");

                UnityMainThreadDispatcher.Enqueue(() => OnServerStopped?.Invoke());
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JSBSimServerRunner] Error stopping JSBSim server: {ex.Message}");
            }
            finally
            {
                _jsbsimProcess.Dispose();
                _jsbsimProcess = null;
            }
        }

        // Clean up threads
        _outputThread = null;
        _errorThread = null;
    }

    /// <summary>
    /// Restarts the JSBSim server
    /// </summary>
    public void RestartServer()
    {
        StopServer();
        Thread.Sleep(500);
        StartServer();
    }

    private void ReadOutput()
    {
        try
        {
            while (_jsbsimProcess != null && !_jsbsimProcess.HasExited && !_isShuttingDown)
            {
                string line = _jsbsimProcess.StandardOutput.ReadLine();
                if (line != null)
                {
                    Debug.Log($"[JSBSim] {line}");
                    UnityMainThreadDispatcher.Enqueue(() => OnServerOutput?.Invoke(line));
                }
            }
        }
        catch (Exception ex)
        {
            if (!_isShuttingDown)
            {
                Debug.LogError($"[JSBSimServerRunner] Output read error: {ex.Message}");
            }
        }
    }

    private void ReadError()
    {
        try
        {
            while (_jsbsimProcess != null && !_jsbsimProcess.HasExited && !_isShuttingDown)
            {
                string line = _jsbsimProcess.StandardError.ReadLine();
                if (line != null)
                {
                    Debug.LogWarning($"[JSBSim Error] {line}");
                    UnityMainThreadDispatcher.Enqueue(() => OnServerError?.Invoke(line));
                }
            }
        }
        catch (Exception ex)
        {
            if (!_isShuttingDown)
            {
                Debug.LogError($"[JSBSimServerRunner] Error read error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Gets available example folders in StreamingAssets
    /// </summary>
    public string[] GetAvailableExamples()
    {
        string streamingAssets = Application.streamingAssetsPath;
        if (Directory.Exists(streamingAssets))
        {
            return Directory.GetDirectories(streamingAssets);
        }
        return Array.Empty<string>();
    }

    /// <summary>
    /// Gets available aircraft in the current example folder
    /// </summary>
    public string[] GetAvailableAircraft()
    {
        string aircraftPath = Path.Combine(Application.streamingAssetsPath, exampleFolder, "aircraft");
        if (Directory.Exists(aircraftPath))
        {
            return Directory.GetDirectories(aircraftPath);
        }
        return Array.Empty<string>();
    }

    /// <summary>
    /// Gets available init files for the current aircraft
    /// </summary>
    public string[] GetAvailableInitFiles()
    {
        string aircraftPath = Path.Combine(Application.streamingAssetsPath, exampleFolder, "aircraft", aircraftName);
        if (Directory.Exists(aircraftPath))
        {
            return Directory.GetFiles(aircraftPath, "*.xml");
        }
        return Array.Empty<string>();
    }
}

/// <summary>
/// Helper class to dispatch actions to the Unity main thread
/// </summary>
public static class UnityMainThreadDispatcher
{
    private static readonly System.Collections.Generic.Queue<Action> _executionQueue = new();
    private static readonly object _lock = new();
    private static bool _initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        var go = new GameObject("UnityMainThreadDispatcher");
        go.AddComponent<MainThreadDispatcherBehaviour>();
        UnityEngine.Object.DontDestroyOnLoad(go);
    }

    public static void Enqueue(Action action)
    {
        lock (_lock)
        {
            _executionQueue.Enqueue(action);
        }
    }

    private class MainThreadDispatcherBehaviour : MonoBehaviour
    {
        private void Update()
        {
            lock (_lock)
            {
                while (_executionQueue.Count > 0)
                {
                    try
                    {
                        _executionQueue.Dequeue()?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[MainThreadDispatcher] Error executing action: {ex.Message}");
                    }
                }
            }
        }
    }
}
