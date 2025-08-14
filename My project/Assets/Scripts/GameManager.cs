using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
//using static Bouyency;

public class GameManager : MonoBehaviour
{
    [System.Serializable]
    public struct Wave
    {
        public Vector2 direction;
        public float amplitude;
        public float wavelength;
        public float speed;
    }

    public event System.Action waveLoaded;

    [Header("Wave Generation")]
    public float initialAmplitude = 0.8f;
    public float initialWavelength = 40f;
    [Range(0f, 5f)] public float amplitudeMultiplier = 0.89f;
    [Range(0f, 5f)] public float wavelengthMultiplier = 0.9f;
    public Vector2 waveSpeed = new Vector2(0.2f, 4f);
    public Wave[] waves = new Wave[355];

    [Header("Bouyency")]
    [Range(0f, 1f)] public float waterDensity = 1f;
    [Range(0f, 10f)] public float bodyDensity = 5f;
    public float gravity = 9.81f;

    private void Start()
    {
        GenerateWaves();
    }

    [ContextMenu("Generate Waves")]
    public void GenerateWaves()
    {
        var currentAmplitude = initialAmplitude;
        var currentWavelength = initialWavelength;

        for (var i = 0; i < waves.Length; i++)
        {
            waves[i].direction = Random.insideUnitCircle.normalized;
            waves[i].amplitude = currentAmplitude;
            waves[i].wavelength = currentWavelength;
            waves[i].speed = Random.Range(waveSpeed.x, waveSpeed.y);

            currentAmplitude *= amplitudeMultiplier;
            currentWavelength *= wavelengthMultiplier;
        }
        if (waveLoaded != null) waveLoaded.Invoke();


    }
}
