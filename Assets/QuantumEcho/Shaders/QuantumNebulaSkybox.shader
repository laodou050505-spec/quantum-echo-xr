Shader "QuantumEcho/Nebula Starfield Skybox"
{
    Properties
    {
        _Tint ("Nebula Tint", Color) = (0.13, 0.72, 0.94, 1)
        _Magenta ("Violet Nebula Tint", Color) = (0.55, 0.15, 0.82, 1)
        _Exposure ("Star Exposure", Range(0, 3)) = 1.2
        _StarDensity ("Star Density", Range(0.02, 0.2)) = 0.11
        _StarSize ("Star Size", Range(0.35, 1.1)) = 0.58
        _TwinkleStrength ("Twinkle Strength", Range(0, 1)) = 0.82
        _TwinkleSpeed ("Twinkle Speed", Range(0.2, 3)) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Tint;
            float4 _Magenta;
            float _Exposure;
            float _StarDensity;
            float _StarSize;
            float _TwinkleStrength;
            float _TwinkleSpeed;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 direction : TEXCOORD0; };

            float hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float noise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(lerp(hash(i + float3(0,0,0)), hash(i + float3(1,0,0)), f.x),
                                     lerp(hash(i + float3(0,1,0)), hash(i + float3(1,1,0)), f.x), f.y),
                                lerp(lerp(hash(i + float3(0,0,1)), hash(i + float3(1,0,1)), f.x),
                                     lerp(hash(i + float3(0,1,1)), hash(i + float3(1,1,1)), f.x), f.y), f.z);
            }

            float fbm(float3 p)
            {
                float result = 0.0;
                float amplitude = 0.52;
                [unroll] for (int i = 0; i < 4; i++)
                {
                    result += amplitude * noise(p);
                    p = p * 2.05 + 13.17;
                    amplitude *= 0.5;
                }
                return result;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.direction = v.vertex.xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.direction);
                float n = fbm(d * 3.1 + float3(7.2, 1.5, -3.8));
                float ribbon = saturate((n - 0.48) * 3.9);
                float mist = saturate((fbm(d * 7.2 - 4.0) - 0.62) * 4.0) * 0.35;
                float violetMix = saturate(d.y * 0.58 + n * 0.85);
                float3 nebula = lerp(_Tint.rgb, _Magenta.rgb, violetMix) * (ribbon * 0.23 + mist * 0.12);

                // Project the direction onto one of six cube faces.  A 3D volume
                // grid misses most cells when sampled only on the sky sphere; this
                // face grid keeps the stars sparse but visible in every direction.
                float3 absDirection = abs(d);
                float2 faceUV;
                float faceId;
                if (absDirection.x >= absDirection.y && absDirection.x >= absDirection.z)
                {
                    faceUV = (d.x >= 0.0 ? float2(-d.z, d.y) : float2(d.z, d.y)) / absDirection.x;
                    faceId = d.x >= 0.0 ? 0.0 : 1.0;
                }
                else if (absDirection.y >= absDirection.z)
                {
                    faceUV = (d.y >= 0.0 ? float2(d.x, -d.z) : float2(d.x, d.z)) / absDirection.y;
                    faceId = d.y >= 0.0 ? 2.0 : 3.0;
                }
                else
                {
                    faceUV = (d.z >= 0.0 ? float2(d.x, d.y) : float2(-d.x, d.y)) / absDirection.z;
                    faceId = d.z >= 0.0 ? 4.0 : 5.0;
                }
                float2 starGrid = (faceUV * 0.5 + 0.5) * 38.0;
                float2 cell2D = floor(starGrid);
                float3 cell = float3(cell2D, faceId * 31.0);
                float2 cellLocal = frac(starGrid) - 0.5;
                float2 offset = float2(hash(cell + 11.3), hash(cell + 37.1)) - 0.5;
                float distanceToStar = length(cellLocal - offset);
                float sizeSeed = hash(cell + 73.2);
                float sizeVariation = lerp(0.48, 1.28, sizeSeed * sizeSeed);
                float starSpot = pow(saturate(1.0 - distanceToStar * (2.9 / (_StarSize * sizeVariation))), 6.0);
                float seed = hash(cell);
                float star = step(1.0 - _StarDensity, seed) * starSpot;
                float brightSpot = pow(saturate(1.0 - distanceToStar * (2.65 / (_StarSize * 1.12))), 5.0);
                float brightStar = step(0.994, hash(cell + 41.3)) * brightSpot;

                // Each star has a separate phase, frequency, base brightness, and
                // pulse amplitude, so the sky never flashes in a synchronized wave.
                float frequency = lerp(0.65, 2.6, hash(cell + 5.4)) * _TwinkleSpeed;
                float phase = hash(cell + 18.9) * 6.2831853;
                float wave = 0.5 + 0.5 * sin(_Time.y * frequency + phase);
                wave *= 0.78 + 0.22 * sin(_Time.y * frequency * 1.73 + phase * 0.61);
                float pulseAmount = lerp(0.45, 1.0, hash(cell + 61.2)) * _TwinkleStrength;
                float pulse = lerp(1.0 - pulseAmount, 1.0, saturate(wave));
                float baseBrightness = lerp(1.8, 3.4, hash(cell + 8.7));
                float3 starColor = lerp(float3(0.48,0.73,1.0), float3(1.0,0.68,0.95), hash(cell + 8.7));
                float3 stars = starColor * (star * baseBrightness * pulse + brightStar * (4.5 + pulse * 3.0));
                float3 color = float3(0.0008, 0.0016, 0.005) + nebula + stars * _Exposure;
                return fixed4(color, 1);
            }
            ENDCG
        }
    }
}
