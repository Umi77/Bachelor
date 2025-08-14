using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class Bouyency : MonoBehaviour
{
    [System.Serializable]
    public struct Wave
    {
        public Vector2 direction;
        public float amplitude;
        public float wavelength;
        public float speed;
    }

    public ComputeShader waterHeightShader;
    public Material waterMaterial;
    public Transform waterPlane;
    public RawImage rawImage;
    public GameManager gameManager;
    public Rigidbody rb;
    public Transform body;
    public Vector2 planeSize;
    public float test;

    private Wave[] waves;

    private ComputeBuffer waveBuffer;
    private RenderTexture heightRT;
    private bool readbackInProgress = false;

    private void OnEnable()
    {
        gameManager.waveLoaded += saveWaveValues;
    }

    private void OnDisable()
    {
        gameManager.waveLoaded -= saveWaveValues;
    }

    void Start()
    {
        heightRT = new RenderTexture(256, 256, 0, RenderTextureFormat.RFloat);
        heightRT.enableRandomWrite = true;
        heightRT.Create();
        saveWaveValues();
    }

    void Update()
    {
        DispatchComputeShader();
        RequestHeightData();
        if (!rawImage) return;
        rawImage.texture = heightRT;
    }

    // just a helper to not type all the wave values manually
    private void saveWaveValues()
    {
        getWaves();
        var stride = sizeof(float) * 5; // 2 for direction, 1 for amp, 1 for wave, 1 for speed
        waveBuffer = new ComputeBuffer(waves.Length, stride);
    }

    private void getWaves()
    {
        GameManager.Wave[] currentWaves = gameManager.waves;
        waves = new Wave[currentWaves.Length];
        for (int i = 0; i < gameManager.waves.Length; i++)
        {
            waves[i].direction = currentWaves[i].direction;
            waves[i].amplitude = currentWaves[i].amplitude;
            waves[i].wavelength = currentWaves[i].wavelength;
            waves[i].speed = currentWaves[i].speed;
        }
    }

    void DispatchComputeShader()
    {
        int kernel = waterHeightShader.FindKernel("CSMain");
        waterHeightShader.SetFloat("_Time", Time.time);
        waveBuffer.SetData(waves);
        waterMaterial.SetBuffer("_Waves", waveBuffer);
        waterHeightShader.SetBuffer(kernel, "_Waves", waveBuffer);


        waterHeightShader.SetTexture(kernel, "Result", heightRT);
        waterHeightShader.SetFloat("_TextureSize", heightRT.width);
        waterHeightShader.SetVector("_PlaneSize", planeSize);
        waterHeightShader.Dispatch(kernel, heightRT.width / 8, heightRT.height / 8, 1);
    }

    private void RequestHeightData()
    {
        if (readbackInProgress) return;

        //Vector3 samplePos = waterPlane.InverseTransformPoint(transform.position);
        Vector3 samplePos = transform.position;

        // Convert world position to UV
        var uv_x = (samplePos.x / planeSize.x) + 0.5f;
        var uv_y = (samplePos.z / planeSize.y) + 0.5f;

        if (uv_x < 0 || uv_x > 1 || uv_y < 0 || uv_y > 1)
        {
            //samplePos = waterPlane.InverseTransformPoint(transform.position);
            return;
        }// Outside the water

        readbackInProgress = true;
        var xPixel = (int)(uv_x * heightRT.width);
        var yPixel = (int)(uv_y * heightRT.height);

        AsyncGPUReadback.Request(heightRT, 0, xPixel, 1, yPixel, 1, 0, 1, OnReadbackComplete);

    }

    private void OnReadbackComplete(AsyncGPUReadbackRequest request)
    {
        // this check prevents error when exiting playmode
        if (this == null) return;
        if (request.hasError) return;

        readbackInProgress = false;

        var data = request.GetData<float>();
        var waterHeight = data[0];
        var newPos = transform.position;
        waterHeight = waterPlane.position.y + waterHeight;
        if (rb == null || body == null)
        {

            newPos.y = waterHeight;
            transform.position = newPos;
        }
        else
        {
            float oceanFluidDensity = test;
            float depth = data[0] - transform.GetLowestPoint<BoxCollider>(); // surface of the ocean - current position of the body
            float submersion = Mathf.Clamp01(depth / body.localScale.y); // How much of the body is under water
            Debug.Log(waterHeight);
            float displacedVolume = rb.mass * submersion;

            Vector3 force = Vector3.up * oceanFluidDensity * displacedVolume * Physics.gravity.magnitude;
            rb.AddForce(force, ForceMode.Force);
        }
    }

    private void OnDestroy()
    {
        if (heightRT != null) heightRT.Release();
        waveBuffer?.Release();
    }
}
