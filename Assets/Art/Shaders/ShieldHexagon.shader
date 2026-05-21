Shader "Custom/ShieldHexagon"
{
    Properties
    {
        [Header(Colors)]
        _ShieldColor ("Shield Color (Faint Tint)", Color) = (1, 0.5, 0, 0.05)
        _OutlineColor ("Outline Color (Glow)", Color) = (1, 0.6, 0.1, 1)
        _HexColor ("Hex Color (Glow)", Color) = (1, 0.4, 0, 1)
        
        [Header(Hexagon Settings)]
        _HexScale ("Hex Scale", Float) = 15.0
        _HexThickness ("Hex Thickness", Range(0, 0.15)) = 0.015
        _HexBlur ("Hex Blur", Range(0.001, 0.1)) = 0.01
        _HexGloss ("Hex Gloss Strength", Range(0, 10)) = 2.0
        _HexFadePower ("Hex Edge Fade Power", Range(0.1, 10)) = 1.5
        _HexScrollSpeed ("Hex Scroll Speed (XY)", Vector) = (0.05, 0.03, 0, 0)
        
        [Header(Outline Settings)]
        _FresnelPower ("Fresnel Power", Range(0.5, 10)) = 3.0
        _PulseSpeed ("Pulse Speed", Float) = 2.0
        _PulseMin ("Pulse Minimum Glow", Range(0.1, 1.0)) = 0.7
        
        [Header(Intersection Glow)]
        _IntersectionScale ("Intersection Sharpness", Float) = 8.0
        _IntersectionColor ("Intersection Color (Override)", Color) = (1, 0.7, 0.2, 1)
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ShieldColor;
                float4 _OutlineColor;
                float4 _HexColor;
                float _HexScale;
                float _HexThickness;
                float _HexBlur;
                float _HexGloss;
                float _HexFadePower;
                float4 _HexScrollSpeed;
                float _FresnelPower;
                float _PulseSpeed;
                float _PulseMin;
                float _IntersectionScale;
                float4 _IntersectionColor;
            CBUFFER_END

            // Helper function for procedural hexagon grid
            float HexagonGrid(float2 UV, float Scale, float Thickness, float Blur)
            {
                float2 uv = UV * Scale;
                float2 r = float2(1.0, 1.73205080757); // 1.0, sqrt(3.0)
                float2 h = r * 0.5;
                
                // Tiling math
                float2 a1 = uv - r * floor(uv / r) - h;
                float2 b1 = (uv - h) - r * floor((uv - h) / r) - h;
                
                float2 g = dot(a1, a1) < dot(b1, b1) ? a1 : b1;
                
                // Distance to nearest hexagon edges
                float d = max(abs(g.x) * 0.86602540378 + abs(g.y) * 0.5, abs(g.y));
                
                float distToEdge = 0.5 - d;
                
                // Draw grid lines
                float lineVal = smoothstep(Thickness + Blur, Thickness, distToEdge);
                return lineVal;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = IN.uv;
                OUT.screenPos = ComputeScreenPos(OUT.positionCS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 1. Fresnel Outline
                float3 normal = normalize(IN.normalWS);
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - IN.positionWS);
                
                // Flip normal for back-faces if we look at them (since Cull Off is active)
                if (dot(normal, viewDir) < 0.0)
                {
                    normal = -normal;
                }
                
                float baseFresnel = 1.0 - saturate(dot(normal, viewDir));
                float fresnel = pow(baseFresnel, _FresnelPower);
                
                // Pulsating Fresnel outline
                float pulse = lerp(_PulseMin, 1.0, 0.5 + 0.5 * sin(_Time.y * _PulseSpeed));
                float3 outlineGlow = _OutlineColor.rgb * fresnel * pulse * _OutlineColor.a;
                
                // 2. Procedural Hexagons with subtle UV distortion
                float2 uvDistort = float2(
                    sin(IN.positionWS.y * 1.5 + _Time.y * 0.5),
                    cos(IN.positionWS.z * 1.5 + _Time.y * 0.5)
                ) * 0.015;
                
                float2 hexUV = IN.uv + uvDistort + _HexScrollSpeed.xy * _Time.y;
                float hexGrid = HexagonGrid(hexUV, _HexScale, _HexThickness, _HexBlur);
                
                // Hexagons should fade towards the center of the sphere to maintain visibility
                float hexVisibility = pow(baseFresnel, _HexFadePower);
                float3 hexGlow = _HexColor.rgb * hexGrid * _HexGloss * hexVisibility * _HexColor.a;
                
                // 3. Intersection Glow (Depth Fade)
                float2 screenUV = IN.screenPos.xy / (IN.screenPos.w + 0.00001);
                float sceneDepth = SampleSceneDepth(screenUV);
                float sceneLinearDepth = LinearEyeDepth(sceneDepth, _ZBufferParams);
                float pixelLinearDepth = IN.screenPos.w;
                float depthDiff = sceneLinearDepth - pixelLinearDepth;
                
                float intersectionGlow = saturate(1.0 - (depthDiff * _IntersectionScale));
                intersectionGlow = pow(intersectionGlow, 2.0); // Sharpen the edge line
                float3 intersectColor = _IntersectionColor.rgb * intersectionGlow * _IntersectionColor.a * 3.0;
                
                // 4. Combine Colors
                float3 finalColor = _ShieldColor.rgb * _ShieldColor.a;
                finalColor += outlineGlow;
                finalColor += hexGlow;
                finalColor += intersectColor;
                
                // Calculate alpha
                float finalAlpha = _ShieldColor.a;
                finalAlpha = max(finalAlpha, fresnel * pulse);
                finalAlpha = max(finalAlpha, hexGrid * 0.3 * hexVisibility);
                finalAlpha = max(finalAlpha, intersectionGlow);
                
                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}
