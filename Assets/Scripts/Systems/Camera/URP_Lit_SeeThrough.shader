Shader "Custom/URP_Lit_SeeThrough"
{
    Properties
    {
        // Standard Lit Properties
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor]   _BaseColor("Base Color", Color) = (1,1,1,1)
        
        _BumpScale("Scale", Float) = 1.0
        _BumpMap("Normal Map", 2D) = "bump" {}
        
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        
        [HDR] _EmissionColor("Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}
        
        // Cutout Properties
        [Space(20)]
        [Header(See Through Settings)]
        _CutoutRadius("Cutout Radius", Float) = 1.5
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

            ZWrite On
            ZTest LEqual
            Cull Back // Important: Hide backfaces to avoid seeing internal geometry

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            
            // -------------------------------------
            // Universal Render Pipeline Keywords
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
                float _BumpScale;
                float _Smoothness;
                float _Metallic;
                float4 _EmissionColor;
                float _CutoutRadius;
                float _CutoutSoftness;
                float _Fade; 
            CBUFFER_END

            float3 _PlayerPos; // Global Variable

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
                // We calculate distance from pixel to the line segment (Camera -> PlayerHead)
                // We can simplify this to just: distance from pixel to Player Position on the screen plane?
                // Or true 3D cylinder. Let's do 3D cylinder/capsule test.
                
                float3 camPos = GetCameraPositionWS();
                float3 playerPos = _PlayerPos;
                // Add offset to player pos to aim for body/head
                playerPos.y += 1.0; 
                
                float3 lineStart = camPos;
                float3 lineEnd = playerPos;
                
                float3 lineVec = lineEnd - lineStart;
                float lineLen = length(lineVec);
                float3 lineDir = lineVec / lineLen;
                
                float3 toPixel = input.positionWS - lineStart;
                float t = dot(toPixel, lineDir);
                float3 closestPoint = lineStart + lineDir * saturate(t); // Clamp to segment
                
                float dist = length(input.positionWS - closestPoint);
                
                // Only cut if the wall is actually between camera and player
                // t > 0 means in front of camera
                // t < lineLen means in front of player
                // But we generally want to see player through wall, so wall must be closer than player.
                // We use _Fade to control whether this logic is active at all for this object.
                
                float effectiveRadius = _CutoutRadius * _Fade;
                
                if (effectiveRadius > 0.05 && t < lineLen * 0.95 && t > 0.5)
                {
                   // Dithering for soft edge
                   // 4x4 Dither Matrix
                   float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS).xy * _ScreenParams.xy;
                   uint dx = (uint)screenUV.x & 3;
                   uint dy = (uint)screenUV.y & 3;
                   const float4x4 Dither = float4x4(
                        0.0625, 0.5625, 0.1875, 0.6875,
                        0.8125, 0.3125, 0.9375, 0.4375,
                        0.2500, 0.7500, 0.1250, 0.6250,
                        1.0000, 0.5000, 0.8750, 0.3750
                    );
                   float noise = Dither[dy][dx];
                   
                   // Normalized distance (0 at center, 1 at radius)
                   float normDist = dist / effectiveRadius;
                   
                   // We want 0 alpha at center, 1 at edge.
                   // smoothstep(0, 1, normDist) gives that.
                   // Add softness.
                   
                   // If dist < radius, we potentially discard.
                   // Alpha should be 1.0 everywhere normally.
                   // Inside hole, alpha drops.
                   
                   float edge = smoothstep(effectiveRadius * (1.0 - _CutoutSoftness), effectiveRadius, dist);
                   
                   // dither: if alpha < noise, discard
                   // If edge is 0 (center), we definitely discard (0 < noise always false unless noise is 0? wait. clip(x) discards if x < 0)
                   // clip(val): discards if val < 0.
                   // we want to Keep if alpha > noise. 
                   // So clip(alpha - noise).
                   
                   clip(edge - noise);
                }

                // --- STANDARD LIT SHADING ---
                
                half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb * _EmissionColor.rgb;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                
                half3 viewDirWS = GetWorldSpaceViewDir(input.positionWS);
                inputData.viewDirectionWS = viewDirWS;
                
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalWS, input.tangentWS);
                inputData.normalWS = normalize(TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w, input.normalWS)));
                
                inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
                inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedoAlpha.rgb;
                surfaceData.alpha = albedoAlpha.a;
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

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual

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
                float4 _BaseMap_ST; float4 _BaseColor; float _BumpScale; float _Smoothness; float _Metallic; float4 _EmissionColor;
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
                // Shadow Caster Logic - also needs Cutout to match holes
                float3 camPos = GetCameraPositionWS();
                float3 playerPos = _PlayerPos; playerPos.y += 1.0; 
                float3 lineStart = camPos; float3 lineEnd = playerPos;
                float3 lineVec = lineEnd - lineStart; float lineLen = length(lineVec); float3 lineDir = lineVec / lineLen;
                float3 toPixel = input.positionWS - lineStart; float t = dot(toPixel, lineDir);
                float3 closestPoint = lineStart + lineDir * saturate(t);
                float dist = length(input.positionWS - closestPoint);
                float effectiveRadius = _CutoutRadius * _Fade;
                
                if (effectiveRadius > 0.05 && t < lineLen * 0.95 && t > 0.5) {
                   float2 screenUV = input.positionCS.xy;
                   uint dx = (uint)screenUV.x & 3; uint dy = (uint)screenUV.y & 3;
                   const float4x4 Dither = float4x4(0.0625, 0.5625, 0.1875, 0.6875, 0.8125, 0.3125, 0.9375, 0.4375, 0.2500, 0.7500, 0.1250, 0.6250, 1.0000, 0.5000, 0.8750, 0.3750);
                   float noise = Dither[dy][dx];
                   float edge = smoothstep(effectiveRadius * (1.0 - _CutoutSoftness), effectiveRadius, dist);
                   clip(edge - noise);
                }
                return 0;
            }
            ENDHLSL
        }
    }
}
