Shader "Custom/CellShader" 
{
	Properties
	{
		_InsideColor ("Inside Color", Color) = (1,1,1,1)
		_OutsideColor ("Outside Color", Color) = (1,1,1,1)
	}
	
	SubShader 
	{
		 Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
		
			Cull Front
		
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag

            #include "UnityCG.cginc"
                        
			uniform sampler2D _DepthTex;
			uniform float4 _InsideColor;
			uniform int _PlaneOrientation;
			
			struct v2f
			{
				float4 pos : SV_Position;
				float4 screenSpace : SV_Position;
			};			

			v2f vert (appdata_base v)
			{
				v2f o;				
				o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
				return o;
			}		
				
		    float4 frag(v2f i) : COLOR 
		    {		
		    	float d = tex2D(_DepthTex, i.screenSpace.xy / _ScreenParams.xy).r;
		    	
		    	if(_PlaneOrientation == 0)
		    	{		    		
		    		if(i.pos.z > d)
		    			discard;
		    	}
		    	else if(_PlaneOrientation == 1)		    		
		    	{
		    		if(i.pos.z < d)
		    			discard;
		    	}
            	
            	
//            	_IndideColor.a = 0.75;
            	return _InsideColor;	        
		    }
            ENDCG
        } 
	
		Pass
		{	
			Blend SrcAlpha OneMinusSrcAlpha
		
			Cull Back
		
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag

            #include "UnityCG.cginc"
                        
			uniform sampler2D _DepthTex;
			uniform float4 _OutsideColor;
			uniform int _PlaneOrientation;
			
			struct v2f
			{
				float4 pos : SV_Position;
				float4 screenSpace : SV_Position;
			};			

			v2f vert (appdata_base v)
			{
				v2f o;				
				o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
				return o;
			}		
				
		    float4 frag(v2f i) : COLOR 
		    {		
		    	float d = tex2D(_DepthTex, i.screenSpace.xy / _ScreenParams.xy).r;
		    	
		    	if(_PlaneOrientation == 0)
		    	{		    		
		    		if(i.pos.z > d)
		    			discard;
		    	}
		    	else if(_PlaneOrientation == 1)		    		
		    	{
		    		if(i.pos.z < d)
		    			discard;
		    	}
		    	
            	return _OutsideColor;	        
		    }
            ENDCG
        }	
	}	
}