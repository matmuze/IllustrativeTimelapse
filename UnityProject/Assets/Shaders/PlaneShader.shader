Shader "Custom/PlaneShader" 
{	
	SubShader
	{		
		Pass
		{
			Cull Off
				
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
			
			uniform float4x4 _P;
			
			struct v2f
			{
				float4 pos : Position;
			};			

			v2f vert (appdata_base v)
			{
				v2f o;				
				o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
				return o;
			}		
				
		    float4 frag(v2f i) : COLOR 
		    {		
		    	return float4(1,0,0,1);	        
		    }
            ENDCG
        }
	} 
	FallBack "Diffuse"
}
