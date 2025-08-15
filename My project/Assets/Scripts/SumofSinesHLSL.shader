Shader "ProperSumOfSinesUnlit"
{
    Properties
    {
        _Skybox ("Skybox", CUBE) = "" {}
        _Color ("Color", Color) = (0.2, 0.3, 0.6, 1.0)
        _SpecReflectance ("Specular Reflectance", Color) = (0.0, 0.0, 0.0, 0.0)
        _SpecNormalStrength ("Specular Normal Strength", float) = 1
        _SpecShininess ("Shininess", float) = 1
        _Diffuse ("Diffuse Amplifier", float) = 1
        _WaterScatterColor ("Water Scatter Color", Color) = (1, 1, 1, 1)
        _AirBubblesColor ("Air Bubbles Color", Color) = (1, 1, 1, 1)
        _HeightScatter ("Height Scatter", float) = 6.33
        _NormalScatter ("Normal Scatter", float) = 0.00001
        _LCLScatter ("LCL Scatter", float) = 1
        _AmbientScatter ("Ambient Scatter", float) = 2.6
        _BubbleDensity ("Bubble Density", float) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            TEXTURECUBE(_Skybox);
            SAMPLER(sampler_Skybox);
            struct Wave
            {
                float2 direction;
                float amplitude;
                float wavelength;
                float speed;
            };

            float4 _Color;
            float4 _SpecReflectance;
            float4 _WaterScatterColor;
            float4 _AirBubblesColor;
            float _SpecNormalStrength;
            float _SpecShininess;
            float _Diffuse;
            float _HeightScatter;
            float _NormalScatter;
            float _LCLScatter;
            float _AmbientScatter;
            float _BubbleDensity;

            #define MAX_WAVES 355

            struct Attributes
            {
                float4 positionOS   : POSITION;
            };

            struct fragIn
            {
                float4 positionHCS  : SV_POSITION;
                float3 worldNormal  : TEXCOORD0;
                float3 worldPos  : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                StructuredBuffer<Wave> _Waves;
            CBUFFER_END

            fragIn vert(Attributes IN)
            {
                fragIn OUT;
                
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                
                float totalHeight = 0;
                float3 tangent = float3(1, 0, 0);
                float3 binormal = float3(0, 0, 1);
                
                uint count;
                uint stride;
                _Waves.GetDimensions(count, stride);

                count = min(count, MAX_WAVES);

                for (int i = 0; i < count; i++)
                {
                    Wave wave = _Waves[i];
                    // this logic is the same as in compute shader
                    float frequency = 6.28318 / wave.wavelength;
                    float phase = frequency * wave.speed;
                    float2 normDir = normalize(wave.direction);
                    float sin_val = sin(dot(normDir, worldPos.xz) * frequency + _Time.y * phase);
                    float cos_val = cos(dot(normDir, worldPos.xz) * frequency + _Time.y * phase);
                    
                    totalHeight += wave.amplitude * sin_val; // * (pow(2.71828, sin_val) - 1);

                    float derivative = frequency * wave.amplitude * cos_val;
                    tangent.y += derivative * normDir.x;
                    binormal.y += derivative * normDir.y;
                }
                
                worldPos.y += totalHeight;

                OUT.worldNormal = normalize(cross(binormal, tangent));
                OUT.positionHCS = TransformWorldToHClip(worldPos);
                OUT.worldPos = worldPos;
                
                return OUT;
            }

            float4 frag(fragIn IN) : SV_Target
            {
                Light mainLight = GetMainLight();
                float3 normView = normalize(_WorldSpaceCameraPos - IN.worldPos);
                //float3 normView = GetCameraPositionWS() - TransformObjectToWorld(IN.positionHCS.xyz);
                //float3 normView = normalize(viewDir);
                //float3 normView = normalize(GetViewForwardDir());
                float3 normLight = normalize(mainLight.direction);
                float3 normNormal = normalize(IN.worldNormal);
                float3 halfNormal = normalize(normLight + normView);

                //Light Scattering
                float heightReflectance = _HeightScatter * max(0, IN.worldPos.y) * pow(max(0, dot(normLight,-normNormal)), 4);
                heightReflectance = heightReflectance * pow((0.5 - 0.5 * dot(normLight, halfNormal)), 3);

                float normalReflectance = _NormalScatter * pow(max(0, dot(normView, halfNormal)), 2);

                float lclScatter = _LCLScatter * max(dot(normLight, halfNormal), 0);

                float ambientScatter = _AmbientScatter * _BubbleDensity;

                // Smith's microfacet shadowing
                //Step 1
                float cosTheta = dot(normNormal, normLight);
                
                //Step 2
                float tanTheta = pow(1 - pow(cosTheta, 2) / cosTheta, 0.5);
                
                //Step 3
                float3 pointTangent;
                
                if (IN.worldNormal.z != 0) 
                {
                    pointTangent = float3 (1,1, -(IN.worldNormal.x + IN.worldNormal.y) / IN.worldNormal.z);
                }
                else 
                {
                    pointTangent = float3 (1, -IN.worldNormal.x / IN.worldNormal.y, 1);
                }
                
                //Step 4
                float3 pointBitangent = cross(IN.worldNormal, pointTangent);
                
                //Step 5
                float3 omega = mainLight.direction;
                float omegaT = dot(omega, pointTangent);
                float omegaB = dot(omega, pointBitangent);
                float cosPhi = pow(omegaT, 2) / pow(pow(omegaT, 2) + pow(omegaB, 2), 0.5);
                float sinPhi = pow(omegaB, 2) / pow(pow(omegaT, 2) + pow(omegaB, 2), 0.5);
                
                //Step 6
                float roughness = 0.2;
                float anisotropy = 0.6;

                //Step 7
                float alphaX = 0.2;
                float alphaY = 0.6;

                float alpha = pow(pow(alphaX, 2) * pow(cosPhi, 2) + pow(alphaY, 2) * pow(sinPhi, 2), 0.5);

                //Step 8
                float a = max(1 / alpha * tanTheta, 0);

                //Step 9
                if (a < 1.6) 
                {
                    a = (1 - 1.259 * a + 0.396 * pow(a, 2)) / (3.535 * a * 2.181 * pow(a, 2));
                }
                else 
                {
                    a = 0;
                }

                // Lambertion Diffuse
                float lambert = saturate(dot(IN.worldNormal, mainLight.direction));

                // Blinn Phong Specular Lighting
                float schlickFresnel = pow(1 - dot(normNormal, normView), 5); // The closer the cam the lower the reflectance
                
                float3 specNormal = normalize(float3(IN.worldNormal.x * _SpecNormalStrength, IN.worldNormal.y, IN.worldNormal.z * _SpecNormalStrength));
                float3 specHalfVector = normalize(normLight + normView); // Doppel normalize?
                float blinnPhongSpecular = pow(saturate(dot(specNormal, specHalfVector)), _SpecShininess) * schlickFresnel; // Specular factor
                //float blinnPhongSpecular = pow(saturate(dot(specNormal, specHalfVector)), _SpecShininess) ;
                
                // Space Reflection
                float3 reflecDir = dot(normView, normNormal) * (normNormal * 2) - normView;

                // Colors
                float3 blinnColor = blinnPhongSpecular * _SpecReflectance * mainLight.color;
                float3 diffuseColor = mainLight.color * (lambert * _Diffuse);
                float3 reflectColor = _Skybox.SampleLevel(sampler_Skybox, reflecDir, 1.0);

                //Light Scatter Colors
                float3 lightScatter = (heightReflectance + normalReflectance) * _WaterScatterColor * mainLight.color * 1 / (1 + a);
                lightScatter = lclScatter * _WaterScatterColor * mainLight.color + ambientScatter * _BubbleDensity * mainLight.color;

                // Assemble of Colors
                float3 pixelColor = blinnColor + diffuseColor + _Color.rgb;
                return float4(lightScatter, 1.0);
            }
            ENDHLSL
        }
    }
}
