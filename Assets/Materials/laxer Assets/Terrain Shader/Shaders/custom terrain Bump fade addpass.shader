Shader "Hidden/Custom/Terrain/Bumped fade addpass" {
Properties {
	[HideInInspector] _Control ("Control (RGBA)", 2D) = "black" {}
	[HideInInspector] _Splat3 ("Layer 3 (A)", 2D) = "Black" {}
	[HideInInspector] _Splat2 ("Layer 2 (B)", 2D) = "Black" {}
	[HideInInspector] _Splat1 ("Layer 1 (G)", 2D) = "Black" {}
	[HideInInspector] _Splat0 ("Layer 0 (R)", 2D) = "Black" {}
	[HideInInspector] _Normal3 ("Normal 3 (A)", 2D) = "bump" {}
	[HideInInspector] _Normal2 ("Normal 2 (B)", 2D) = "bump" {}
	[HideInInspector] _Normal1 ("Normal 1 (G)", 2D) = "bump" {}
	[HideInInspector] _Normal0 ("Normal 0 (R)", 2D) = "bump" {}
	// used in fallback on old cards & base map
	[HideInInspector] _MainTex ("BaseMap (RGB)", 2D) = "white" {}
	[HideInInspector] _Color ("Main Color", Color) = (1,1,1,1)
	_Fade ("Fade length", Range( 0.001,0.5)) = 0.01
}
	
SubShader {
	Tags {
		"SplatCount" = "4"
		"Queue" = "Geometry-99"
		"IgnoreProjector"="True"
		"RenderType" = "Opaque"
	}
CGPROGRAM
#pragma surface surf Lambert vertex:vert decal:blend exclude_path:prepass exclude_path:deferred
#pragma target 3.0

void vert (inout appdata_full v)
{
	v.tangent.xyz = cross(v.normal, float3(0,0,1));
	v.tangent.w = -1;
}
struct Input {
	float2 uv_Control : TEXCOORD0;
	float2 uv_Splat0 : TEXCOORD1;
	float2 uv_Splat1 : TEXCOORD2;
	float2 uv_Splat2 : TEXCOORD3;
	float2 uv_Splat3 : TEXCOORD4;
};

sampler2D _Control;
sampler2D _Splat0,_Splat1,_Splat2,_Splat3;
sampler2D _Normal0,_Normal1,_Normal2,_Normal3;
float _Fade;

void surf (Input IN, inout SurfaceOutput o) {
	fixed4 splat_control = tex2D (_Control, IN.uv_Control);
	fixed4 tex0 = tex2D (_Splat0, IN.uv_Splat0);
	fixed4 tex1 = tex2D (_Splat1, IN.uv_Splat1);
	fixed4 tex2 = tex2D (_Splat2, IN.uv_Splat2);
	fixed4 tex3 = tex2D (_Splat3, IN.uv_Splat3);
	fixed4 norm0 = tex2D (_Normal0, IN.uv_Splat0);
	fixed4 norm1 = tex2D (_Normal1, IN.uv_Splat1);
	fixed4 norm2 = tex2D (_Normal2, IN.uv_Splat2);
	fixed4 norm3 = tex2D (_Normal3, IN.uv_Splat3);
	//splat_control *= 0.5.xxxx;
	fixed nothing = 1-splat_control.r - splat_control.g - splat_control.b - splat_control.a;
	//nothing = nothing *2;
	splat_control += fixed4(tex0.a * splat_control.r, tex1.a * splat_control.g, tex2.a * splat_control.b, tex3.a * splat_control.a);
	
	fixed4 sc2;
		sc2.r =	clamp((splat_control.r - nothing)+_Fade,0.001,1) * clamp((splat_control.r - splat_control.g)+_Fade,0.001,1) * clamp((splat_control.r - splat_control.b)+_Fade,0.001,1) * clamp((splat_control.r - splat_control.a)+_Fade,0.001,1);
		sc2.g = clamp((splat_control.g - nothing)+_Fade,0.001,1) * clamp((splat_control.g - splat_control.r)+_Fade,0.001,1) * clamp((splat_control.g - splat_control.b)+_Fade,0.001,1) * clamp((splat_control.g - splat_control.a)+_Fade,0.001,1);
		sc2.b = clamp((splat_control.b - nothing)+_Fade,0.001,1) * clamp((splat_control.b - splat_control.r)+_Fade,0.001,1) * clamp((splat_control.b - splat_control.g)+_Fade,0.001,1) * clamp((splat_control.b - splat_control.a)+_Fade,0.001,1);
		sc2.a = clamp((splat_control.a - nothing)+_Fade,0.001,1) * clamp((splat_control.a - splat_control.r)+_Fade,0.001,1) * clamp((splat_control.a - splat_control.g)+_Fade,0.001,1) * clamp((splat_control.a - splat_control.b)+_Fade,0.001,1);
		fixed alpha = clamp((nothing - splat_control.r)+_Fade,0.001,1) * clamp((nothing - splat_control.g)+_Fade,0.001,1) * clamp((nothing - splat_control.b)+_Fade,0.001,1) * clamp((nothing - splat_control.a)+_Fade,0.001,1);
	half sum = sc2.r + sc2.g + sc2.b + sc2.a;
	sc2 = sc2 / sum;
	alpha = alpha / (sum+alpha);
	o.Albedo =  tex0 * sc2.r + tex1 * sc2.g + tex2 * sc2.b + tex3 * sc2.a;
	o.Normal = UnpackNormal(norm0) * sc2.r + UnpackNormal(norm1) * sc2.g + UnpackNormal(norm2) * sc2.b + UnpackNormal(norm3) * sc2.a;
	o.Alpha = clamp(1-alpha-0.01,0.001,1);
}
ENDCG  
}


// Fallback to Diffuse
Fallback "Hidden/TerrainEngine/Splatmap/Lightmap-AddPass"
}
