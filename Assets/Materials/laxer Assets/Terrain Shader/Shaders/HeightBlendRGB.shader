Shader "Custom/HeightBlended/DiffuseRGB" {
	Properties {
	_Splat2 ("Layer 3 (B)", 2D) = "Black" {}
	_Splat1 ("Layer 2 (G)", 2D) = "Black" {}
	_Splat0 ("Layer 1 (R)", 2D) = "Black" {}
	// used in fallback on old cards & base map
	_MainTex ("Fallback texture", 2D) = "Black" {}
	//controlls edge blurryness
	_Fade ("Fade length", Range( 0.001,0.5)) = 0.1
}
	
SubShader {
	Tags {
	}
CGPROGRAM
#pragma surface surf Lambert exclude_path:prepass exclude_path:deferred
#pragma target 2.0
struct Input {
	float2 uv_Splat0 : TEXCOORD0;
	float2 uv_Splat1 : TEXCOORD1;
	float2 uv_Splat2 : TEXCOORD2;
	float4 color : COLOR;
};

sampler2D _Splat0,_Splat1,_Splat2;
float _Fade;

void surf (Input IN, inout SurfaceOutput o) {
	float4 splat_control = IN.color;
	fixed4 tex0 = tex2D (_Splat0, IN.uv_Splat0);
	fixed4 tex1 = tex2D (_Splat1, IN.uv_Splat1);
	fixed4 tex2 = tex2D (_Splat2, IN.uv_Splat2);
	//splat_control *= 0.5.xxxx;
	splat_control += fixed4(tex0.a * splat_control.r, tex1.a * splat_control.g, tex2.a * splat_control.b, 0);
	
	fixed4 sc2;
		sc2.r = clamp((splat_control.r - splat_control.g)+_Fade,0,1) * clamp((splat_control.r - splat_control.b)+_Fade,0,1);
		sc2.g = clamp((splat_control.g - splat_control.r)+_Fade,0,1) * clamp((splat_control.g - splat_control.b)+_Fade,0,1);
		sc2.b = clamp((splat_control.b - splat_control.r)+_Fade,0,1) * clamp((splat_control.b - splat_control.g)+_Fade,0,1);
	half sum = sc2.r + sc2.g + sc2.b;
	sc2 = sc2 / sum;
	o.Albedo = tex0 * sc2.r + tex1 * sc2.g + tex2 * sc2.b;
	o.Alpha = 1;
}
ENDCG  
}


Fallback "Mobile/Diffuse"
}
