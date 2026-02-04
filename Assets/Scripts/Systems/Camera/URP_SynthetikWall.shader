Shader "Custom/URP_SynthetikWall"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor]   _BaseColor("Base Color", Color) = (1,1,1,1)
        
        _BumpScale("Normal Scale", Float) = 1.0
        _BumpMap("Normal Map", 2D) = "bump" {}
        
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0)
        _EmissionMap("Emission Map", 2D) = "black" {}
        
        [Space(20)]
        [Header(Synthetik Hide Settings)]
        _CutoutRadius("Cutout Radius", Float) = 1.5
        _CutoutSoftness("Cutout Softness", Float) = 0.5
        // _Fade is no longer used, logic is always ON
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend One Zero 
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
            #pragma multi_compile_fragment _ _FORWARD_PLUS // Critical for URP 14+
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
                float _BumpScale;
                float _Smoothness;
                float _Metallic;
                float4 _EmissionColor;
                float _CutoutRadius;
                float _CutoutSoftness;
            CBUFFER_END

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
                // === AUTOMATIC CUTOUT LOGIC ===
                // Always check distance to "Camera->Player" line.
                // If this pixel blocks the view, cut it.

                float3 pixelPos = input.positionWS;
                float3 camPos = GetCameraPositionWS();
                float3 playerPos = _PlayerPos + float3(0, 1.0, 0); // Aim for Body

                // Line Segment: Cam -> Player
                float3 lineStart = camPos;
                float3 lineEnd = playerPos;
                float3 lineVec = lineEnd - lineStart;
                float lineLen = length(lineVec);
                float3 lineDir = lineVec / lineLen;

                float3 toPixel = pixelPos - lineStart;
                float t = dot(toPixel, lineDir);
                
                // Check if the wall pixel is physically BETWEEN the camera and the player.
                // t > NearPlane (avoid cutting too close to cam) 
                // t < lineLen (avoid cutting behind the player)
                
                // Relaxed range: Start cutting very close to camera, stop exactly at player
                if (t > 0.2 && t < lineLen - 0.2)
                {
                    float3 closestPoint = lineStart + lineDir * t;
                    float dist = length(pixelPos - closestPoint);
                    
                    float radius = _CutoutRadius; 
                    
                    if (dist < radius)
                    {
                        // Calc Softness
                        float softness = clamp(_CutoutSoftness, 0.01, 0.99);
                        float edgeStart = radius * (1.0 - softness);
                        float alphaNoise = smoothstep(edgeStart, radius, dist);
                        
                        // Screen Dither Pattern
                        float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS).xy * _ScreenParams.xy;
                        uint x = (uint)screenUV.x % 4;
                        uint y = (uint)screenUV.y % 4;
                        const float4x4 Dither = float4x4(
                            0.0625, 0.5625, 0.1875, 0.6875,
                            0.8125, 0.3125, 0.9375, 0.4375,
                            0.2500, 0.7500, 0.1250, 0.6250,
                            1.0000, 0.5000, 0.8750, 0.3750
                        );
                        
                        // Clip
                        clip(alphaNoise - Dither[y][x]);
                    }
                }
                // === END CUTOUT ===


                // === PBR LIGHTING ===
                // 1. Sample Textures first
                half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb * _EmissionColor.rgb;

                // 2. Calculate World Normal (with Normal Map)
                float3 normalWS = normalize(input.normalWS);
                float3 tangentWS = normalize(input.tangentWS.xyz);
                float3 bitangentWS = cross(normalWS, tangentWS) * input.tangentWS.w;
                float3x3 tbn = float3x3(tangentWS, bitangentWS, normalWS);
                
                // Perturb normal
                normalWS = TransformTangentToWorld(normalTS, tbn);
                normalWS = normalize(normalWS);

                // 3. Fill InputData
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalWS; // Use perturbed normal
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                
                inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
                inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedoAlpha.rgb;
                surfaceData.alpha = 1.0; 
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = normalTS;
                surfaceData.emission = emission;
                surfaceData.occlusion = 1.0; 

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                return half4(color.rgb, 1.0);
            }
            ENDHLSL
        }


        // --- Pass 2: DepthOnly (Fixes object penetration/ghosting) ---
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex VertDist
            #pragma fragment FragDepth
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD1; };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _CutoutRadius;
                float _CutoutSoftness;
            CBUFFER_END
            float3 _PlayerPos;

            Varyings VertDist(Attributes input) {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }

            half4 FragDepth(Varyings input) : SV_Target {
                float3 camPos = GetCameraPositionWS();
                float3 playerPos = _PlayerPos + float3(0, 1.0, 0);
                float3 lineVec = playerPos - camPos;
                float lineLen = length(lineVec);
                float3 lineDir = lineVec / lineLen;
                float3 toPixel = input.positionWS - camPos;
                float t = dot(toPixel, lineDir);

                if (t > 0.2 && t < lineLen - 0.2) {
                    float3 closestPoint = camPos + lineDir * t;
                    float dist = length(input.positionWS - closestPoint);
                    if (dist < _CutoutRadius) {
                        float softness = clamp(_CutoutSoftness, 0.01, 0.99);
                        // Using same clip logic as Forward but simplified (no dithering for depth is usually safer or harder)
                        // User requested simple clip.
                        clip(dist - (_CutoutRadius * (1.0 - softness)));
                    }
                }
                return 0;
            }
            ENDHLSL
        }

        // --- Pass 3: DepthNormals (Fixes SSAO/Outlines) ---
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex VertNorm
            #pragma fragment FragDepthNormals
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD1; float3 normalWS : NORMAL; };

            CBUFFER_START(UnityPerMaterial)
                float _CutoutRadius;
                float _CutoutSoftness;
            CBUFFER_END
            float3 _PlayerPos;

            Varyings VertNorm(Attributes input) {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 FragDepthNormals(Varyings input) : SV_Target {
                float3 camPos = GetCameraPositionWS();
                float3 playerPos = _PlayerPos + float3(0, 1.0, 0);
                float3 lineVec = playerPos - camPos;
                float lineLen = length(lineVec);
                float3 lineDir = lineVec / lineLen;
                float t = dot(input.positionWS - camPos, lineDir);

                if (t > 0.2 && t < lineLen - 0.2) {
                    float dist = length(input.positionWS - (camPos + lineDir * t));
                    if (dist < _CutoutRadius) {
                         // Same clip
                         clip(-1);
                    }
                }
                return float4(PackNormalOctRectEncode(normalize(input.normalWS)), 0.0, 0.0);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD1; float2 uv : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; float4 _BaseColor; float _BumpScale; float _Smoothness; float _Metallic; float4 _EmissionColor;
                float _CutoutRadius; float _CutoutSoftness;
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
                float3 lineStart = GetCameraPositionWS();
                float3 lineEnd = _PlayerPos + float3(0, 1.0, 0);
                float3 lineVec = lineEnd - lineStart; float lineLen = length(lineVec); float3 lineDir = lineVec / lineLen;
                float3 toPixel = input.positionWS - lineStart; float t = dot(toPixel, lineDir);
                
                if (t > 0.2 && t < lineLen - 0.2) {
                    float3 closestPoint = lineStart + lineDir * t;
                    float dist = length(input.positionWS - closestPoint);
                    float radius = _CutoutRadius; // Always active
                    
                    if (dist < radius) {
                        float softness = clamp(_CutoutSoftness, 0.01, 0.99);
                        float edgeStart = radius * (1.0 - softness);
                        float alphaNoise = smoothstep(edgeStart, radius, dist);
                        
                        float2 screenUV = input.positionCS.xy;
                        uint x = (uint)screenUV.x % 4; uint y = (uint)screenUV.y % 4;
                        const float4x4 Dither = float4x4(0.0625, 0.5625, 0.1875, 0.6875, 0.8125, 0.3125, 0.9375, 0.4375, 0.2500, 0.7500, 0.1250, 0.6250, 1.0000, 0.5000, 0.8750, 0.3750);
                        clip(alphaNoise - Dither[y][x]);
                    }
                }
                return 0;
            }
            ENDHLSL
        }
    }
}
