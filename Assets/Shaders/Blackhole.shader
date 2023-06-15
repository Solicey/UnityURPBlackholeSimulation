Shader "Custom/Blackhole"
{
    Properties
    {
        [NoScaleOffset] _SkyboxTex ("Skybox Texture", Cube) = "_Skybox" {}
        _TimeStep ("Time Step", Range(0.01, 0.2)) = 0.01
        _IterCount ("Iter Count", Range(100, 500)) = 300
        
        [Header(Accretion Disc)]
        [Space(5)]
        [Toggle(_True)] _RenderDisc ("Render Disc", float) = 1
        [NoScaleOffset] _DiscTex ("Disc Texture", 2D) = "white" {}
        _DiscInnerRad ("Inner Radius", Range(2.7, 3.2)) = 3
        _DiscMidRad ("Middle Radius", Range(3.5, 4.5)) = 3.5
        _DiscOuterRad ("Outer Radius", Range(5.5, 7.5)) = 6
        _DiscThickness ("Thickness", Range(0.15, 0.3)) = 0.2
        _DiscInnerRot ("Inner Rotation Speed", Range(0.15, 0.2)) = 0.15
        _DiscOuterRot ("Outer Rotation Speed", Range(0.01, 0.05)) = 0.02
    }
    SubShader
    {
        Cull Off 
        ZWrite Off 
        ZTest Always
        
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 rayDirWS : TEXCOORD0;
            };

            samplerCUBE _SkyboxTex;
            half4 _SkyboxTex_HDR;
            float _TotalTime;
            float _TimeStep;
            int _IterCount;
            sampler2D _DiscTex;
            
            float _RenderDisc;
            float _DiscInnerRad;
            float _DiscMidRad;
            float _DiscOuterRad;
            float _DiscThickness;
            float _DiscInnerRot;
            float _DiscOuterRot;

            float event_horizon(float3 pos)
            {
                return length(pos) - 1;
            }

            // return acceleration
            float3 gravitational_lensing(float h2, float3 pos)
            {
                float r2 = dot(pos, pos);
                float r5 = pow(r2, 2.5);
                return -1.5 * h2 * pos / r5;
            }

            void runge_kutta_4(float h2, out float3 v, out float3 a, float3 pos, float3 vel)
            {
                v = vel;
                a = gravitational_lensing(h2, pos);
            }

            void cal_disc_color(inout half3 color, inout float alphaLeft, float3 pos, float3 prevPos)
            {
                bool useMid = abs(pos.y - prevPos.y) > 0.01;
                float2 mid = (pos.y * prevPos.xz - prevPos.y * pos.xz) / (pos.y - prevPos.y);
                float2 dir = useMid ? mid : pos.xz;
                float r = length(dir);
                bool cross = (pos.y * prevPos.y < 0.0) || (abs(pos.y) < 0.001);
                if (r >= _DiscInnerRad && r <= _DiscOuterRad && cross)
                {
                    float ratioMid = r / _DiscMidRad;
                    float alpha = saturate(pow(abs(r - _DiscOuterRad), 3.0) / 8.0);

                    float offset = atan2(dir.y, dir.x) / 2.0 / UNITY_PI + 0.5;
                    float uInner = frac(_TotalTime * _DiscInnerRot + offset);
                    float uOuter = frac(_TotalTime * _DiscOuterRot + offset);
                    float v = 1.0 - (r - _DiscInnerRad) / (_DiscOuterRad - _DiscInnerRad);

                    half3 colorInner = tex2D(_DiscTex, float2(uInner, v)).rgb;
                    half3 colorOuter = tex2D(_DiscTex, float2(uOuter, v)).rgb;
                    half3 c = lerp(colorInner, colorOuter, 1.0 - pow(v, 2.5));

                    color += alpha * alphaLeft * c;
                    alphaLeft *= (1.0 - alpha); 
                }
            }

            Varyings vert (Attributes i)
            {
                Varyings o;
                o.positionCS = UnityObjectToClipPos(i.positionOS);
                float3 dir = mul(unity_CameraInvProjection, float4(i.uv * 2.0f - 1.0f, 0.0f, -1.0f));
                o.rayDirWS = normalize(mul(unity_CameraToWorld, float4(dir, 0.0)));
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half3 color = half3(0, 0, 0);
                float alpha = 1.0;
                float3 pos = _WorldSpaceCameraPos;

                float3 vel = i.rayDirWS;
                float3 h = cross(pos, vel);  
                float3 h2 = dot(h, h);

                UNITY_LOOP
                for (int j = 0; j < _IterCount; j++)
                {
                    if (event_horizon(pos) < 0)
                    {
                        return half4(color, 1);
                    }

                    float dt = _TimeStep * max(10.0, length(pos));
                    float3 v1, v2, v3, v4;
                    float3 a1, a2, a3, a4;

                    //float3 offset = gravitational_lensing(h2, pos);
                    //vel += offset;
                    //pos += vel;
                    
                    runge_kutta_4(h2, v1, a1, pos, vel);
                    runge_kutta_4(h2, v2, a2, pos + 0.5 * dt * v1, vel + 0.5 * dt * a1);
                    runge_kutta_4(h2, v3, a3, pos + 0.5 * dt * v2, vel + 0.5 * dt * a2);
                    runge_kutta_4(h2, v4, a4, pos + 1.0 * dt * v3, vel + 1.0 * dt * a3);

                    float3 dp = dt * (v1 + 2.0 * v2 + 2.0 * v3 + v4) / 6.0;
                    float3 dv = dt * (a1 + 2.0 * a2 + 2.0 * a3 + a4) / 6.0;

                    float3 prevPos = pos;
                    pos += dp;
                    vel += dv;

                    if (_RenderDisc)
                        cal_disc_color(color, alpha, pos, prevPos);
                }

                float4 skybox = texCUBE(_SkyboxTex, vel);
                color += DecodeHDR(skybox, _SkyboxTex_HDR).rgb;

                return half4(color, 1);
            }
            
            ENDHLSL
        }
    }
}
