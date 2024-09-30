// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Horus/BurnTex"
{
    Properties
    {
        _MainTex("MainTex", 2D) = "white" {}
        _BumpMap("BumpMap", 2D) = "bump" {}
        _DisolveGuide("Disolve Guide", 2D) = "white" {}
        _BurnRamp("Burn Ramp", 2D) = "white" {}
        _BurnRamp1("Burn Ramp1", 2D) = "white" {}
        _Glossiness("Smoothness", Float) = 1
        _ChangeValue("ChangeValue", Float) = 3
        _EffectSpeed("EffectSpeed", Float) = 3
        _AddValue("AddValue", Float) = 0.5
        _OnOff("OnOff", Float) = 0
        _Metallic("Metallic", Float) = 0
        _Color("Color", Color) = (1,1,1,0)
        [HideInInspector] _texcoord( "", 2D ) = "white" {}
        [HideInInspector] __dirty( "", Int ) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "Queue" = "Geometry+0" "IsEmissive" = "true"
        }
        LOD 500
        Cull Back
        CGPROGRAM
        #include "UnityShaderVariables.cginc"
        #pragma target 3.0
        #pragma surface surf Standard keepalpha addshadow fullforwardshadows
        struct Input
        {
            float2 uv_texcoord;
        };

        uniform sampler2D _BumpMap;
        uniform float4 _Color;
        uniform sampler2D _MainTex;
        uniform float _OnOff;
        uniform float _EffectSpeed;
        uniform sampler2D _DisolveGuide;
        uniform float4 _DisolveGuide_ST;
        uniform sampler2D _BurnRamp;
        uniform float _ChangeValue;
        uniform float _AddValue;
        uniform sampler2D _BurnRamp1;
        uniform float _Metallic;
        uniform float _Glossiness;

        void surf(Input i, inout SurfaceOutputStandard o)
        {
            o.Normal = UnpackNormal(tex2D(_BumpMap, i.uv_texcoord));
            o.Albedo = (_Color * tex2D(_MainTex, i.uv_texcoord)).rgb;
            float2 uv_DisolveGuide = i.uv_texcoord * _DisolveGuide_ST.xy + _DisolveGuide_ST.zw;
            float4 tex2DNode45 = tex2D(_DisolveGuide, uv_DisolveGuide);
            float clampResult50 = clamp(
                (-4.0 + (((-0.5 + (((_Time.x * _EffectSpeed) % 1.0) - 0.0) * (0.55 - -0.5) / (1.0 - 0.0)) + tex2DNode45.
                    r) - 0.0) * (4.0 - -4.0) / (1.0 - 0.0)), 0.0, 1.0);
            float temp_output_51_0 = (1.0 - clampResult50);
            float2 appendResult52 = (float2(temp_output_51_0, 0.0));
            float clampResult134 = clamp(
                (-4.0 + (((-0.5 + ((((_Time.x * _EffectSpeed) + _AddValue) % 1.0) - 0.0) * (0.55 - -0.5) / (1.0 - 0.0))
                    + tex2DNode45.r) - 0.0) * (4.0 - -4.0) / (1.0 - 0.0)), 0.0, 1.0);
            float temp_output_135_0 = (1.0 - clampResult134);
            float2 appendResult136 = (float2(temp_output_135_0, 0.0));
            o.Emission = (_OnOff * ((temp_output_51_0 * (1.0 + (temp_output_51_0 - 0.0) * (0.0 - 1.0) / (1.0 - 0.0)) *
                tex2D(_BurnRamp, appendResult52) * _ChangeValue) + (temp_output_135_0 * (1.0 + (temp_output_135_0 - 0.0)
                * (0.0 - 1.0) / (1.0 - 0.0)) * tex2D(_BurnRamp1, appendResult136) * _ChangeValue))).rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1;
        }
        ENDCG
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry+0"
        }
        LOD 100
        Pass
        {

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            v2f vert(appdata i)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(i.vertex);
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
    CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18900
595;0;1262;751;1757.335;-651.915;1;True;False
Node;AmplifyShaderEditor.TimeNode;119;-3439.861,1043.797;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;122;-3130.892,1145.056;Inherit;False;Property;_EffectSpeed;EffectSpeed;7;0;Create;True;0;0;0;False;0;False;5;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;144;-2985.387,2171.323;Inherit;False;Property;_AddValue;AddValue;8;0;Create;True;0;0;0;False;0;False;0;0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;125;-2743.155,1788.862;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;3;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;121;-2431.361,599.7286;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;3;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;142;-2671.394,2004.892;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;42;-2152.523,621.9835;Inherit;False;908.2314;498.3652;Dissolve - Opacity Mask;2;47;150;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;126;-2322.553,2094.644;Inherit;False;908.2314;498.3652;Dissolve - Opacity Mask;2;131;129;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleRemainderNode;120;-2267.99,642.9928;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleRemainderNode;127;-2473.053,2208.385;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;129;-1881.612,2168.488;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;-0.5;False;4;FLOAT;0.55;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;150;-1795.438,669.5329;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;-0.5;False;4;FLOAT;0.55;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;45;-2111.255,1250.608;Inherit;True;Property;_DisolveGuide;Disolve Guide;2;0;Create;True;0;0;0;False;0;False;-1;None;e28dc97a9541e3642a48c0e3886688c5;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;48;-2078.083,160.9987;Inherit;False;814.5701;432.0292;Burn Effect - Emission;5;52;51;50;49;67;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;132;-2248.113,1633.659;Inherit;False;814.5701;432.0292;Burn Effect - Emission;5;140;136;135;134;133;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleAddOpNode;47;-1504.835,678.3297;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;131;-1674.865,2150.99;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;49;-2063.302,392.7963;Inherit;True;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;-4;False;4;FLOAT;4;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;133;-2233.333,1865.456;Inherit;True;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;-4;False;4;FLOAT;4;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;50;-2043.083,161.4157;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;134;-2213.113,1634.076;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;135;-1959.079,1669.063;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;51;-1789.049,196.4032;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;52;-1753.389,445.7026;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;136;-1923.419,1918.363;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;138;-1387.61,1872.423;Inherit;True;Property;_BurnRamp1;Burn Ramp1;4;0;Create;True;0;0;0;False;0;False;-1;None;64e7766099ad46747a07014e44d0aea1;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;0,0;False;1;FLOAT2;1,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TFHCRemapNode;140;-1718.063,1778.979;Inherit;True;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;1;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;70;-1231.884,839.0106;Inherit;False;Property;_ChangeValue;ChangeValue;6;0;Create;True;0;0;0;False;0;False;5.25;1.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;54;-1096.58,272.7629;Inherit;True;Property;_BurnRamp;Burn Ramp;3;0;Create;True;0;0;0;False;0;False;-1;None;64e7766099ad46747a07014e44d0aea1;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;0,0;False;1;FLOAT2;1,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TFHCRemapNode;67;-1548.033,306.3188;Inherit;True;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;1;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;139;-775.0754,1239.023;Inherit;True;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;3;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;53;-2014.534,-147.9802;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;68;-733.221,234.7432;Inherit;True;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;3;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;141;-459.9171,610.4385;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;154;-471.9252,156.7622;Inherit;False;Property;_OnOff;OnOff;9;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;157;-694.1316,-712.998;Inherit;False;Property;_Color;Color;11;0;Create;True;0;0;0;False;0;False;1,1,1,0;1,1,1,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;60;-964.4126,-537.6143;Inherit;True;Property;_MainTex;MainTex;0;0;Create;True;0;0;0;False;0;False;-1;None;69782b7dbad92504ebcb9b22161919f2;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;156;-349.2657,65.46271;Inherit;False;Property;_Metallic;Metallic;10;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;159;-357.1316,-589.998;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;155;-295.5731,177.2089;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;61;-962.8568,-149.5424;Inherit;True;Property;_BumpMap;BumpMap;1;0;Create;True;0;0;0;False;0;False;-1;None;11f03d9db1a617e40b7ece71f0a84f6f;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;62;-282.458,356.9795;Float;False;Property;_Glossiness;Smoothness;5;0;Create;False;0;0;0;False;0;False;1;0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;0,0;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;Horus/BurnTex;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;125;0;119;1
WireConnection;125;1;122;0
WireConnection;121;0;119;1
WireConnection;121;1;122;0
WireConnection;142;0;125;0
WireConnection;142;1;144;0
WireConnection;120;0;121;0
WireConnection;127;0;142;0
WireConnection;129;0;127;0
WireConnection;150;0;120;0
WireConnection;47;0;150;0
WireConnection;47;1;45;1
WireConnection;131;0;129;0
WireConnection;131;1;45;1
WireConnection;49;0;47;0
WireConnection;133;0;131;0
WireConnection;50;0;49;0
WireConnection;134;0;133;0
WireConnection;135;0;134;0
WireConnection;51;0;50;0
WireConnection;52;0;51;0
WireConnection;136;0;135;0
WireConnection;138;1;136;0
WireConnection;140;0;135;0
WireConnection;54;1;52;0
WireConnection;67;0;51;0
WireConnection;139;0;135;0
WireConnection;139;1;140;0
WireConnection;139;2;138;0
WireConnection;139;3;70;0
WireConnection;68;0;51;0
WireConnection;68;1;67;0
WireConnection;68;2;54;0
WireConnection;68;3;70;0
WireConnection;141;0;68;0
WireConnection;141;1;139;0
WireConnection;60;1;53;0
WireConnection;159;0;157;0
WireConnection;159;1;60;0
WireConnection;155;0;154;0
WireConnection;155;1;141;0
WireConnection;61;1;53;0
WireConnection;0;0;159;0
WireConnection;0;1;61;0
WireConnection;0;2;155;0
WireConnection;0;3;156;0
WireConnection;0;4;62;0
ASEEND*/
//CHKSM=823CF6AFD36F7A52EA6D548D7270D6AEF1A506E9