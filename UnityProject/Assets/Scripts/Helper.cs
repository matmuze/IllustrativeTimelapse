using System;
using UnityEngine;

public static class Helper
{
	// Calculates new MVP matrix from main camera
	public static Matrix4x4 GetMVPMatrix()
	{
		bool d3d = SystemInfo.graphicsDeviceVersion.IndexOf("Direct3D") > -1;
		
		//Matrix4x4 M = GameObject.Find("Cube1").transform.localToWorldMatrix;
		Matrix4x4 V = Camera.main.worldToCameraMatrix;
		Matrix4x4 P = Camera.main.projectionMatrix;
		
		if (d3d)
		{
			// Invert Y for rendering to a render texture
			for ( int i = 0; i < 4; i++) { P[1,i] = -P[1,i]; }
			// Scale and bias from OpenGL -> D3D depth range
			for ( int i = 0; i < 4; i++) { P[2,i] = P[2,i]*0.5f + P[3,i]*0.5f;}
		}
		
		return P*V;
	}
}

