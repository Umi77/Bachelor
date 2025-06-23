using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using static Unity.Mathematics.math;
using UnityEngine.Rendering;
using Unity.VisualScripting;

public class SetWater : MonoBehaviour
{
    public Material ocean;
    KeyValuePair<string, float>[] pairs = new KeyValuePair<string, float>[]
    {
        new KeyValuePair<string, float>("_Duration", 3f),
        new KeyValuePair<string, float>("_AmplitudeAmplifier", 0.89f),
        new KeyValuePair<string, float>("_WavelengthAmplifier", 1.17f),
        new KeyValuePair<string, float>("_SpecNormalStrength", 0.1f),
        new KeyValuePair<string, float>("_Amplitude", 16.54f),
        new KeyValuePair<string, float>("_Wavelength", 23.27f),
        new KeyValuePair<string, float>("_Shininess", 17.4f),
        new KeyValuePair<string, float>("_Ambient", 1.0f),
        new KeyValuePair<string, float>("_Diffuse", 2.27f),
        new KeyValuePair<string, float>("_Specular", 0.53f)
    };
    public float[] arrSpeed = new float[] 
        { 
            1.91f, 
            2.88f, 
            0.94f, 
            0.54f, 
            0.59f, 
            0.5f, 
            0.27f, 
            0.26f, 
            2f, 
            1.53f 
        };

    public Vector4[] arrDirection = new Vector4[]
    {
        new Vector4(2.02f, 0f, -0.08f),
        new Vector4(-0.53f, 0f, 0.84f),
        new Vector4(-0.1f, 0f, 0.14f),
        new Vector4(0.04f, 0f, -0.99f),
        new Vector4(0.99f, 0f, -0.07f),
        new Vector4(-0.85f, 0f, -1.19f),
        new Vector4(0.36f, 0f, -0.08f),
        new Vector4(-0.23f, 0f, -0.04f),
        new Vector4(-0.15f, 0f, 0.06f),
        new Vector4(2.02f, 0f, -0.08f)
    };

    public Vector4[] GetDirections()
    {
        return arrDirection;
    }

    public float[] GetSpeed()
    {
        return arrSpeed;
    }

    public KeyValuePair<string, float>[] GetPairs()
    {
        return pairs;
    }
    // Start is called before the first frame update
    void Start()
    {
        if (ocean != null)
        {
            int count = 0;
            foreach (float speed in arrSpeed)
            {
                ocean.SetFloat("_Speed" + count.ToString(), speed);
                count++;
            }
            count = 0;
            foreach (Vector3 direction in arrDirection)
            {
                ocean.SetVector("_Direction" + count.ToString(), direction);
                count++;
            }
            foreach (KeyValuePair<string, float> pair in pairs)
            {
                ocean.SetFloat(pair.Key, pair.Value);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
