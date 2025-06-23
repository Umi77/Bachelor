using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using static Unity.Mathematics.math;
using UnityEngine.Rendering;
using System.IO;
using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;


public class Bouyency : MonoBehaviour
{
    public ComputeShader computeShader;
    public RenderTexture heightMapRT;
    private int kernel;
    private ComputeBuffer debugBuffer;
    private float[] debugData = new float[1];
    // Start is called before the first frame update 

    private void Start()
    {
        kernel = computeShader.FindKernel("CSMain");
        

        // 1 Element vom Typ float (4 Byte)
        debugBuffer = new ComputeBuffer(1, sizeof(float));
        SetWater setWater = gameObject.AddComponent<SetWater>();
        SetupCalculations(setWater);
        // Optional: an Compute Shader binden
        computeShader.SetBuffer(kernel, "debugBuffer", debugBuffer);
        debugBuffer.SetData(debugData);

        heightMapRT = new RenderTexture(256, 256, 0);
        heightMapRT.enableRandomWrite = true;
        heightMapRT.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat;
        heightMapRT.Create();

        computeShader.SetTexture(kernel, "Result", heightMapRT);
        computeShader.Dispatch(kernel, heightMapRT.width / 8, heightMapRT.height / 8, 1);

        // 1. Temporäre Texture2D erstellen
        Texture2D tex = new Texture2D(heightMapRT.width, heightMapRT.height, TextureFormat.RGBA32, false);

        // 2. RenderTexture aktiv setzen und Pixel lesen
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = heightMapRT;

        tex.ReadPixels(new Rect(0, 0, heightMapRT.width, heightMapRT.height), 0, 0);
        tex.Apply();

        RenderTexture.active = currentRT;

        // 3. In PNG konvertieren
        byte[] pngData = tex.EncodeToPNG();

        // 4. Speicherpfad bestimmen
        string scriptPath = Application.dataPath; // Pfad zu Assets/
        string savePath = Path.Combine(scriptPath, "RenderOutput.png");

        // 5. Datei speichern
        File.WriteAllBytes(savePath, pngData);

        Debug.Log($"PNG gespeichert unter: {savePath}");
    }

    private void SetupCalculations(SetWater setWater)
    {
        KeyValuePair<string, float>[] pairs =  setWater.GetPairs();
        foreach (KeyValuePair<string, float> pair in pairs)
        {
            computeShader.SetFloat(pair.Key, pair.Value);
        }
        computeShader.SetFloats("_Speed", setWater.GetSpeed());
        computeShader.SetVectorArray("_Direction", setWater.GetDirections());
    }
    private void Update()
    {
        computeShader.SetFloat("_Time", Time.time);
        computeShader.SetFloat("_DeltaTime", Time.deltaTime);
        if (Input.GetKeyDown(KeyCode.Space))
        {
            kernel = computeShader.FindKernel("CSMain");
            computeShader.Dispatch(kernel, 32, 32, 1);
            debugBuffer.GetData(debugData);

            Debug.Log("Debugwert: " + debugData[0]);
            debugBuffer.Release();
        }
    }

    public void SaveRenderTextureAsPNG()
    {
        // 1. Temporäre Texture2D erstellen
        Texture2D tex = new Texture2D(heightMapRT.width, heightMapRT.height, TextureFormat.RGBA32, false);

        // 2. RenderTexture aktiv setzen und Pixel lesen
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = heightMapRT;

        tex.ReadPixels(new Rect(0, 0, heightMapRT.width, heightMapRT.height), 0, 0);
        tex.Apply();

        RenderTexture.active = currentRT;

        // 3. In PNG konvertieren
        byte[] pngData = tex.EncodeToPNG();

        // 4. Speicherpfad bestimmen
        string scriptPath = Application.dataPath; // Pfad zu Assets/
        string savePath = Path.Combine(scriptPath, "RenderOutput.png");

        // 5. Datei speichern
        File.WriteAllBytes(savePath, pngData);

        Debug.Log($"PNG gespeichert unter: {savePath}");
    }
}
