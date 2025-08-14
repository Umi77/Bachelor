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
            float _SpecNormalStrength;
            float _SpecShininess;
            float _Diffuse;

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
                
                uint count, stride;
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

                // Assemble of Colors
                float3 pixelColor = blinnColor + diffuseColor + _Color.rgb;
                return float4(pixelColor, 1.0);
            }
            ENDHLSL
        }
    }
}
