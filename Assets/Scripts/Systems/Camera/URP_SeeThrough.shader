Shader "Custom/URP_SeeThrough"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _BumpMap("Normal Map", 2D) = "bump" {}
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _EmissionMap("Emission Map", 2D) = "black" {}
        _EmissionColor("Emission Color", Color) = (0,0,0)
        
        [Space]
        _CutoutRadius("Cutout Radius", Float) = 1.0
        _CutoutSoftness("Cutout Softness", Float) = 0.5
        _Fade("Cutout Strength (0-1)", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            // Opaque rendering
            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            
            // URP Keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _CLUSTERED_RENDERING
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
                float2 staticLightmapUV : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD1;
                float3 normalWS     : NORMAL;
                float4 tangentWS    : TANGENT;
                float2 uv           : TEXCOORD0;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 2);
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _Smoothness;
                float _Metallic;
                float4 _EmissionColor;
                float _CutoutRadius;
                float _CutoutSoftness;
                float _Fade; // Controlled by FadingObject script
            CBUFFER_END

            // Global Variable set by ObjectFader
            float3 _PlayerPos;

            TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);        SAMPLER(sampler_BumpMap);
            TEXTURE2D(_EmissionMap);    SAMPLER(sampler_EmissionMap);

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.normalWS = normalInput.normalWS;
                output.tangentWS = float4(normalInput.tangentWS, input.tangentOS.w);
                
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                
                OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.lightmapUV);
                OUTPUT_SH(output.normalWS, output.vertexSH);
                
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // --- CUTOUT LOGIC ---
                // Calculate distance from this pixel to the line segment (Camera -> Player)
                float3 camPos = GetCameraPositionWS();
                float3 pixelPos = input.positionWS;
                float3 lineStart = camPos;
                float3 lineEnd = _PlayerPos; // Set globally

                float3 lineDir = lineEnd - lineStart;
                float lineLength = length(lineDir);
                lineDir /= lineLength; // Normalize

                float3 toPixel = pixelPos - lineStart;
                float projection = dot(toPixel, lineDir);
                // Clamp projection to stay within the segment (or just let it be infinite cylinder towards/past player)
                // For walls "between", projection will be > 0 and < lineLength.
                
                float3 closestPoint = lineStart + lineDir * projection;
                float dist = length(pixelPos - closestPoint);

                // If _Fade is 0, we don't cut. If _Fade is 1, we cut full radius.
                // We use noise or dither here? No, clean cutout.
                // If dist < _CutoutRadius * _Fade, discard.
                
                // Soft edge:
                // smoothstep(edge0, edge1, x)
                // Transpareny is not supported in Opaque pass properly unless we use AlphaToMask or Dither.
                // Using 'clip' creates hard edges. Dither creates "soft" looking edges.
                // Let's use Dither for the edge of the circle only!
                
                float dynamicRadius = _CutoutRadius * _Fade;
                
                if (dynamicRadius > 0.01)
                {
                   // Hard clip for hole
                   // clip(dist - dynamicRadius); 
                   
                   // Dithered Edge Logic:
                   // If dist is slightly larger than radius, dither it to blend.
                   // If dist < radius, fully discard.
                   
                   // Dither matrix
                   float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS).xy * _ScreenParams.xy;
                   uint dx = (uint)screenUV.x & 3;
                   uint dy = (uint)screenUV.y & 3;
                   // 4x4 Dither
                   const float4x4 Dither = float4x4(
                        0.0625, 0.5625, 0.1875, 0.6875,
                        0.8125, 0.3125, 0.9375, 0.4375,
                        0.2500, 0.7500, 0.1250, 0.6250,
                        1.0000, 0.5000, 0.8750, 0.3750
                    );
                   float noise = Dither[dy][dx];
                   
                   // Smooth falloff
                   // 1 at radius, 0 at radius + softness
                   // We want alpha 0 inside, 1 outside.
                   // "alpha" based on distance
                   float alpha = smoothstep(dynamicRadius, dynamicRadius + _CutoutSoftness, dist);
                   
                   // Clip based on dither threshold
                   clip(alpha - noise);
                }
                
                // --- END CUTOUT LOGIC ---

                // Standard PBR
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 finalColor = texColor * _BaseColor;
                
                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv));
                half3 normalWS = normalize(TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w, input.normalWS)));

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = finalColor.rgb;
                surfaceData.alpha = 1.0; 
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = normalTS;
                surfaceData.occlusion = 1.0;
                surfaceData.emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb * _EmissionColor.rgb;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS; 
                inputData.viewDirectionWS = GetWorldSpaceViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
                inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);
                
                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                return half4(color.rgb, 1.0);
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD1; float2 uv : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; float4 _BaseColor; float _Smoothness; float _Metallic; float4 _EmissionColor;
                float _CutoutRadius; float _CutoutSoftness; float _Fade;
            CBUFFER_END
            float3 _PlayerPos;
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            Varyings Vert(Attributes input) {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target {
                // Cutout logic in shadow
                float3 camPos = GetCameraPositionWS();
                float3 pixelPos = input.positionWS;
                float3 lineStart = camPos;
                float3 lineEnd = _PlayerPos;
                float3 lineDir = normalize(lineEnd - lineStart);
                float3 toPixel = pixelPos - lineStart;
                float projection = dot(toPixel, lineDir);
                float3 closestPoint = lineStart + lineDir * projection;
                float dist = length(pixelPos - closestPoint);
                float dynamicRadius = _CutoutRadius * _Fade;
                
                if (dynamicRadius > 0.01) {
                   float2 screenUV = input.positionCS.xy; // Screen position in shadow pass?
                   uint dx = (uint)screenUV.x & 3; uint dy = (uint)screenUV.y & 3;
                   const float4x4 Dither = float4x4(0.0625, 0.5625, 0.1875, 0.6875, 0.8125, 0.3125, 0.9375, 0.4375, 0.2500, 0.7500, 0.1250, 0.6250, 1.0000, 0.5000, 0.8750, 0.3750);
                   float noise = Dither[dy][dx];
                   float alpha = smoothstep(dynamicRadius, dynamicRadius + _CutoutSoftness, dist);
                   clip(alpha - noise);
                }
                return 0;
            }
            ENDHLSL
        }
    }
}
