using Mediapipe;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MediaPipeTest : MonoBehaviour
{
    [SerializeField] bool useGPU = true;

    [SerializeField]
    private DemoGraph graph;
    [SerializeField]
    private WebCamScreenController webcamScreenController;

    const int MAX_WAIT_FRAME = 1000;
    private GpuResources gpuResources;
    private GlCalculatorHelper gpuHelper;
    static IntPtr currentContext = IntPtr.Zero;

    private Coroutine graphRunner;
    private Coroutine cameraSetupCoroutine;
    private WebCamDevice? webCamDevice;
    private object graphLock = new object ();

    void Start()
    {
#if UNITY_EDITOR_OSX || UNITY_EDITOR_WIN
#if UNITY_STANDALONE
        if (useGPU)
        {
            Debug.LogWarning("PC Standalone on macOS or Windows does not support GPU. Uncheck `Use GPU` from the Inspector window (SceneDirector).");
        }
#endif
#endif

#if UNITY_ANDROID && !UNITY_EDITOR_OSX && !UNITY_EDITOR_WIN
    if (IsGpuEnabled()) {
      PluginCallback callback = GetCurrentContext;

      var fp = Marshal.GetFunctionPointerForDelegate(callback);
      GL.IssuePluginEvent(fp, 1);
    }
#endif
        var resourceManager = GameObject.Find("ResourceManager");
#if UNITY_EDITOR
        resourceManager.AddComponent<LocalAssetLoader>();
#else
    resourceManager.AddComponent<AssetBundleLoader>();
#endif
        var webcamDevice = GetDefaultWebcam();
        if (webcamDevice.HasValue)
        {
            ChangeWebCamDevice(webcamDevice);
        }
    }

    private WebCamDevice? GetDefaultWebcam()
    {
        WebCamDevice[] devices = WebCamTexture.devices;

        // for debugging purposes, prints available devices to the console
        for (int i = 0; i < devices.Length; i++)
        {
            print("Webcam available: " + devices[i].name);
        }
        if (devices.Length > 0)
        {
            return devices[0];
        }
        else
        {
            return null;
        }
    }

    public void ChangeWebCamDevice(WebCamDevice? webCamDevice)
    {
        lock (graphLock)
        {
            ResetCamera(webCamDevice);

            if (graph != null)
            {
                StopGraph();
                StartGraph();
            }
        }
    }

    void ResetCamera(WebCamDevice? webCamDevice)
    {
        StopCamera();
        cameraSetupCoroutine = StartCoroutine(webcamScreenController.ResetScreen(webCamDevice));
        this.webCamDevice = webCamDevice;
    }

    void StopCamera()
    {
        if (cameraSetupCoroutine != null)
        {
            StopCoroutine(cameraSetupCoroutine);
            cameraSetupCoroutine = null;
        }
    }

    void StartGraph()
    {
        if (graphRunner != null)
        {
            Debug.Log("The graph is already running");
            return;
        }

        if (IsGpuEnabled())
        {
            SetupGpuResources();
        }
        graphRunner = StartCoroutine(RunGraph());
    }

    void StopGraph()
    {
        if (graphRunner != null)
        {
            StopCoroutine(graphRunner);
            graphRunner = null;
        }
    }

    void SetupGpuResources()
    {
        if (gpuResources != null)
        {
            Debug.Log("Gpu resources are already initialized");
            return;
        }

        // TODO: have to wait for currentContext to be initialized.
        if (currentContext == IntPtr.Zero)
        {
            Debug.LogWarning("No EGL Context Found");
        }
        else
        {
            Debug.Log($"EGL Context Found ({currentContext})");
        }

        gpuResources = GpuResources.Create(currentContext).Value();
        gpuHelper = new GlCalculatorHelper();
        gpuHelper.InitializeForTest(gpuResources);
    }

    IEnumerator RunGraph()
    {
        yield return WaitForCamera(webcamScreenController);

        if (!webcamScreenController.isPlaying)
        {
            Debug.LogWarning("WebCamDevice is not working. Stopping...");
            yield break;
        }

        if (IsGpuEnabled())
        {
            graph.Initialize(gpuResources, gpuHelper);
        }
        else
        {
            graph.Initialize();
        }

        graph.StartRun(webcamScreenController.GetScreen()).AssertOk();

        while (true)
        {
            yield return new WaitForEndOfFrame();

            var nextFrameRequest = webcamScreenController.RequestNextFrame();
            yield return nextFrameRequest;

            var nextFrame = nextFrameRequest.textureFrame;

            graph.PushInput(nextFrame).AssertOk();
            graph.RenderOutput(webcamScreenController, nextFrame);
        }
    }

    IEnumerator WaitForCamera(WebCamScreenController webCamScreenController)
    {
        var waitFrame = MAX_WAIT_FRAME;

        yield return new WaitUntil(() =>
        {
            waitFrame--;
            var isWebCamPlaying = webCamScreenController.isPlaying;

            if (!isWebCamPlaying && waitFrame % 50 == 0)
            {
                Debug.Log($"Waiting for a WebCamDevice");
            }

            return isWebCamPlaying || waitFrame < 0;
        });
    }

    bool IsGpuEnabled()
    {
#if UNITY_EDITOR_OSX || UNITY_EDITOR_WIN
        return false;
#else
    return useGPU;
#endif
    }
}