Shader "Metaballs/Metaballs2D"
{
    Properties
    {
        [HideInInspector]_MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off

        Pass
        {
            CGPROGRAM

            #pragma exclude_renderers gles
            #pragma vertex vert_img
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _NoiseTex;

			int _MetaballCount;
			float _OutlineSize;
            float _CameraSize;
			
            StructuredBuffer<half4> _MetaballPSData;
            StructuredBuffer<half4> _MetaballPSColorData;

            float4 frag (v2f_img i) : SV_Target
            {
                float4 noisyCircle = tex2D(_NoiseTex, i.uv);

				float dist = 1.0f;
                float sp = _ScreenParams.y / _CameraSize;
                float weightSum = 0.0f;
                float4 colorSum = float4(0.0f, 0.0f, 0.0f, 0.0f);
                float2 uvScaled = i.uv * _ScreenParams.xy;

				for (int m = 0; m < _MetaballCount; ++m)
				{
                    half4 data = _MetaballPSData[m];

					float2 metaballPos = data.xy + noisyCircle.xy * 10;
                    float radiusSize = data.z * sp;                   
    
                    float distFromMetaballSq = dot(metaballPos - uvScaled, metaballPos - uvScaled);
                    float radiusSizeSq = radiusSize * radiusSize;

                    if (distFromMetaballSq > radiusSizeSq) continue;

					float distFromMetaball = distance(metaballPos, i.uv * _ScreenParams.xy);

                    float d = distFromMetaball / radiusSize;
                    float weight = saturate(1.0f - d);
					dist *= saturate(d);

                    colorSum += _MetaballPSColorData[m] * weight;
                    weightSum += weight;
				}

                float4 tex = tex2D(_MainTex, i.uv);
                if(weightSum < 0.01) return tex;

				float threshold = 0.5f;
				float outlineThreshold = threshold * (1.0f - _OutlineSize);

                float4 innerColor = colorSum / weightSum;
                float4 outlineColor = innerColor * innerColor.a;
             
                float4 noise = tex2D(_NoiseTex, i.uv + _Time.xy);
                float4 refractedTex = tex2D(_MainTex, i.uv + noise.xy * _ScreenParams.zw * .01);

                float4 color = (dist > threshold) ? tex : ((dist > outlineThreshold) ? outlineColor : innerColor * refractedTex);
                return color;
            }
            ENDCG
        }
    }
}
