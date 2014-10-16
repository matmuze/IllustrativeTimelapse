using UnityEngine;
using System.Collections;

public class GuiScript : MonoBehaviour 
{
	MainScript mainScript;
	float progress = 0;

	// Use this for initialization
	void Start () 
	{
		mainScript = GameObject.Find("Main Script").GetComponent<MainScript>();
	}

	// Update is called once per frame
	void Update () {
	
	}

	void OnGUI () 
	{
		progress = (float)MainScript.currentFrame / (float)MainScript.NUM_FRAMES;
		
		GUI.contentColor = Color.black;
		//		GUILayout.Label("Current frame: " + currentFrame);
		//		GUILayout.Label("Current time: " + (double)currentFrame * TIME_STEP);
		
		float newProgress = GUI.HorizontalSlider(new Rect(25, Screen.height - 25, Screen.width - 50, 30), progress, 0.0F, 1.0F);
		
		if(progress != newProgress)
		{
			MainScript.currentFrame = (int)(((float)MainScript.NUM_FRAMES - 1.0f) * newProgress);
			MainScript.resetCurrentPositions = true;
		}
		
//		if(mainScript.splitScreen)
//		{
//			GUI.DrawTexture(new Rect(Screen.width * 0.5f - 5.0f, 0.0f, 10.0f, Screen.height), mainScript.blackStripe);
//		}
	}
}
