using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public struct ParticleData 
{
	public bool surface;	
	public int id;
	public int type;	
	public Vector3 position;
	public Vector3 orientation;
}

public struct ReactionData 
{
	public int frame;
	public float time;	
	public int[] reactants;
	public int[] products;	
	public string type;
	public Vector3 position;
}

public class MainScript : MonoBehaviour 
{
	public const int SCALING_FACTOR = 25;
	public const int MAX_NUM_PARTICLES = 4000;
	public const int FRAME_PARTICLE_SIZE = 32;
	public const int FRAME_SIZE = MAX_NUM_PARTICLES * FRAME_PARTICLE_SIZE;	
	
	public static int NUM_FRAMES = 0;
	
	public const string viz_data_path = @"C:\Users\matmuze\MCell\data.bin";
	public const string rxn_data_path = @"MCell\rxn_data\reactions.txt";

	/*****/	
	
	GameObject camera;
	GameObject target;
	MolScript molScript;
	
	/*****/
	
	bool init = false;
	bool pause = true;
	int previousFrame = -1;	
	int previousTemporalResolution = -1;	
	int fixedUpdateCount = 0;	
	
	public static bool resetCurrentPositions = false;	
	public static int currentFrame = 0;

	/*****/

	int[] currentStates = new int[MAX_NUM_PARTICLES]; 
	float[] currentAngles = new float[MAX_NUM_PARTICLES];	
	GameObject[] molObjects = new GameObject[MAX_NUM_PARTICLES];

	// Alternative to game object
	//	Vector3[] currentPositions = new Vector3[MAX_NUM_PARTICLES];
	//	Quaternion[] currentRotations = new Quaternion[MAX_NUM_PARTICLES];

	/*****/

	int[] ownershipMapSnapshot;
	Dictionary<int, int> ownershipMap = new Dictionary<int, int>();
	Dictionary<int, int> molIdToIndexMap = new Dictionary<int, int>();
	ParticleData[] frameParticleData;

	/*****/

	Vector4[] tempPos = new Vector4[MAX_NUM_PARTICLES];	
	Vector4[] tempTraj = new Vector4[MAX_NUM_PARTICLES];
	Vector4[] tempRot = new Vector4[MAX_NUM_PARTICLES];		
	int[] tempType = new int[MAX_NUM_PARTICLES];
	int[] tempState = new int[MAX_NUM_PARTICLES];
	
	/*****/
	
	public static ReactionData[] reactions;
	public static int[] reactionFrameStart;
	public static int[] reactionFrameEnd;
	
	List<int> ongoingReactions = new List<int>();
	List<int> terminatedReactions = new List<int>();
	
	/*****/
	
	public ComputeShader updateShader;
	public Texture blackStripe;
		
	public bool enableAO = true;
	public bool enableDOF = false;
	public bool enableMultitemporal = false;
	public bool splitScreen = false;
//	public bool showTrajectory = false;
//	public bool usePhysics = true;

	
	public float abstractionMin = 0.0f;
	public float innerSphere = 10.0f;
	public float outerSphere = 25.0f;
	
	[RangeAttribute(0, 1)]
	public float abstractionLevel = 1.0f;
	
	[RangeAttribute(0, 100)]
	public float angularDrag = 1.0f;
	
	[RangeAttribute(0, 100)]
	public float colliderRadius = 1.0f;
		
	[RangeAttribute(0.1f, 10.0f)]
	public float mass = 1;

	[RangeAttribute(0, 30)]
	public float drag = 10.0f;
	
	[RangeAttribute(0, 0.1f)]
	public float jitterForce = 0;
	
	[RangeAttribute(0, 1)]
	public float molScale = 0.015f;
	
	[RangeAttribute(0, 100)]
	public float scaleTorque = 10.0f;
	
	[RangeAttribute(1, 1000)]
	public int temporalResolution = 1;

	[RangeAttribute(1, 1000)]
	public int reactionTemporalResolution = 1;
	
	[RangeAttribute(1, 100)]
	public int reactionAnticipation = 1;
	
	[RangeAttribute(1, 10)]
	public int nextFrameCount = 1;
	
	[RangeAttribute(0, 1000)]
	public float maximumScreenSpeed = 10.0f;

	[RangeAttribute(0, 1)]
	public float extraWurst = 0.1f;

	[RangeAttribute(0, 50)]
	public float minAngle = 10.0f;
	
	[RangeAttribute(0, 50)]
	public float maxAngle = 30.0f;

	bool forceNextFrame = false;

	void Start ()
	{
		if (!File.Exists (viz_data_path)) throw new Exception("No file found at: " + viz_data_path);
		if (!File.Exists (rxn_data_path)) throw new Exception("No file found at: " + rxn_data_path);

		for(int i = 0; i < MAX_NUM_PARTICLES; i++)
		{
			molObjects[i] = new GameObject();
			molObjects[i].hideFlags = HideFlags.HideAndDontSave;
			
			//			var rigidBody = molObjects[i].AddComponent<Rigidbody>();
			//			rigidBody.useGravity = false;
			//			rigidBody.isKinematic = true;
		}
		
		camera = GameObject.Find("Main Camera");
		target = GameObject.Find("Target");
		molScript = camera.GetComponent<MolScript>();
		
		// Get number of frames
		var dataFileLength = new System.IO.FileInfo(viz_data_path).Length;
		NUM_FRAMES = (int)(dataFileLength / (long)FRAME_SIZE);
		
		ReactionDataReader.ReadReactionData(false);
		SimulationDataReader.BuildIndexToIdMap();
	}
	
	void OnApplicationQuit()
	{
		GC.Collect();
		
		foreach(var gameObject in molObjects)DestroyImmediate (gameObject, true);
		
		var objects = GameObject.FindObjectsOfType<GameObject>();
		foreach (var o in objects) DestroyImmediate(o);
		
		Resources.UnloadUnusedAssets();
		UnityEditor.EditorUtility.UnloadUnusedAssets();
		UnityEditor.EditorUtility.UnloadUnusedAssetsIgnoreManagedReferences();		
	}

	void RebuildIdToIndexMap()
	{
		molIdToIndexMap.Clear();
		for(int i = 0; i < frameParticleData.Length; i++)
		{
			molIdToIndexMap.Add(frameParticleData[i].id, i);
		}
	}

	void RebuildOwnershipMap()
	{
		ownershipMap.Clear();
		for(int i = 0; i < frameParticleData.Length; i++)
		{
			ownershipMap.Add(frameParticleData[i].id, i);
		}
	}

	void TakeOwnershipMapSnapshot()
	{
		if(ownershipMapSnapshot == null) ownershipMapSnapshot = new int[ownershipMap.Count];
		
		for(int i = 0; i < frameParticleData.Length; i++)
		{
			ownershipMapSnapshot[i] = ownershipMap[frameParticleData[i].id];
		}
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space)) pause = !pause;
		if (Input.GetKeyDown(KeyCode.N) && pause) forceNextFrame = true;
	}

	void LoadNextFrame()
	{
		// Reset positions if temporal resolution has changed
		if(temporalResolution != previousTemporalResolution)
		{
			resetCurrentPositions = true;
			previousTemporalResolution = temporalResolution;
		}
		
		// If there is a new frame to display
		if(currentFrame != previousFrame)
		{					
			frameParticleData = SimulationDataReader.LoadFrameData(currentFrame);
			
			// Reset the current mol positions to the frame positions
			if(resetCurrentPositions || currentFrame == 0)
			{
				resetCurrentPositions = false;

				ongoingReactions.Clear();
				terminatedReactions.Clear();
				
				// Assign game objects to molecules	
				for(int i = 0; i < frameParticleData.Length; i++)
				{			
					currentStates[i] = 0;
					currentAngles[i] = UnityEngine.Random.Range(-180, 180);
					molObjects[i].transform.position = frameParticleData[i].position;
				}

				RebuildIdToIndexMap();
				RebuildOwnershipMap();								
				TakeOwnershipMapSnapshot();
			}
			
			TriggerReactions();
			
			UpdateReactions();
			
			previousFrame = currentFrame;
		}

		if(!pause)
		{
			fixedUpdateCount ++;
			
			if(fixedUpdateCount >= nextFrameCount)
			{
				currentFrame += temporalResolution;
				fixedUpdateCount = 0;
			}
		}
		else if (forceNextFrame)
		{
			currentFrame += temporalResolution;
		}
		
		if (currentFrame > NUM_FRAMES-1) currentFrame = 0; 
		if (currentFrame < 0) currentFrame = NUM_FRAMES - 1; 
	}

	// Trigger new reactions
	void TriggerReactions()
	{
		for(int f = currentFrame; f < Mathf.Min(currentFrame + (temporalResolution * reactionAnticipation), NUM_FRAMES); f++)
		{
			if(reactionFrameStart[f] == -1) continue;
			
			int numReactions = (reactionFrameEnd[f] - reactionFrameStart[f]) + 1;
			
			for(int r = 0; r < numReactions; r++)
			{
				int currentReactionIndex = reactionFrameStart[f] + r;
				
				if(ongoingReactions.Contains(currentReactionIndex)) continue;
				if(terminatedReactions.Contains(currentReactionIndex)) continue;
				
				ReactionData reaction = reactions[currentReactionIndex];

				int reactant1 = reaction.reactants[0];
				int reactant2 = reaction.reactants[1];

				if(ownershipMap.ContainsKey(reactant1) && ownershipMap.ContainsKey(reactant2))
				{
					currentStates[ownershipMap[reactant1]] = currentStates[ownershipMap[reactant2]] = 1;
					ongoingReactions.Add(currentReactionIndex);
				}				
				else
				{
					Debug.Log("Blitz reaction");
				}
			}
		}
	}
		
	// Update ongoing reactions
	void UpdateReactions() 
	{
		bool reactionOccurred = false;

		foreach (var reactionId in ongoingReactions.ToArray())
		{
			ReactionData reaction = reactions[reactionId];
			
			// If reaction is occurring in the current frame
			if (currentFrame >= reaction.frame)
			{
				int reactant = reaction.reactants[0];
				int partner = reaction.reactants[1];
				int product = reaction.products[0];

//				Debug.Log("Remove reactant: " + reactant);
//				Debug.Log("Add product: " + product);

				int value = ownershipMap[reactant];
				ownershipMap.Remove(reactant);
				ownershipMap.Add(product, value);

				currentStates[ownershipMap[product]] = 0;
				currentStates[ownershipMap[partner]] = 0;
				
				ongoingReactions.Remove(reactionId);
				terminatedReactions.Add(reactionId);

				reactionOccurred = true;
			}
		}

		// If one or several reaction occurred we must refresh the ownershipMap and molIdToIndexMap
		if(reactionOccurred) 
		{
			TakeOwnershipMapSnapshot();
			RebuildIdToIndexMap();
		}

		foreach (var reactionId in ongoingReactions.ToArray())
		{
			ReactionData reaction = reactions[reactionId];

			// If reaction is not occurring in the current frame
			if (currentFrame < reaction.frame)
			{
				int reactant = reaction.reactants[0];
				int partner = reaction.reactants[1];
				int product = reaction.products[0];

				int reactionCountDown = (reaction.frame - currentFrame) / temporalResolution + 1;
				//	if((reaction.frame - currentFrame) % temporalResolution != 0) reactionCountDown += 1;

				int currentReactionFrame = (enableMultitemporal) ? (reaction.frame) - reactionCountDown * reactionTemporalResolution : currentFrame;
				
				ParticleData reactantParticle = SimulationDataReader.LoadParticleData(currentReactionFrame, reactant);
				if(reactantParticle.id != reactant)
				{	
					Debug.Log("Error finding particle from reactant id");
					Debug.Log(reactant + "_ _" + reactantParticle.id);
				}
				
				ParticleData partnerParticle = SimulationDataReader.LoadParticleData(currentReactionFrame, partner);
				if(partnerParticle.id != partner)
				{
					Debug.Log("Error finding particle from patner");
					Debug.Log(partner + "_ _ " + partnerParticle.id);
				}
				
				frameParticleData[molIdToIndexMap[reactant]].position = reactantParticle.position;
				frameParticleData[molIdToIndexMap[partner]].position = partnerParticle.position;
			}
		}		
	}


	void FixedUpdate () 
	{		
		LoadNextFrame();

		/* Update molecules here */
		
		for(int i = 0; i < frameParticleData.Length; i++)
		{
			int objectIndex = ownershipMapSnapshot[i];
			GameObject molObject = molObjects[objectIndex];

			// If the molecule type is surface
			if( frameParticleData[i].surface )
			{					
				var q = Quaternion.FromToRotation(Vector3.forward, frameParticleData[i].orientation);
				
//				float angleRange = Mathf.Lerp(minAngle, maxAngle, (float)temporalResolution * 0.01f);
//				currentAngles[i] += UnityEngine.Random.Range(-angleRange, angleRange);
				
				molObject.transform.rotation = Quaternion.Euler(-30, 60, currentAngles[objectIndex]) * q;
			}
			else
			{
				// TODO: Add rotation to volume molecules
			}

			tempPos[i] = molObjects[objectIndex].transform.position;	
			tempPos[i].w = 1;	
			
			tempRot[i].x = molObjects[objectIndex].transform.rotation.x;
			tempRot[i].y = molObjects[objectIndex].transform.rotation.y;
			tempRot[i].z = molObjects[objectIndex].transform.rotation.z;
			tempRot[i].w = molObjects[objectIndex].transform.rotation.w;
						
			tempState[i] = currentStates[objectIndex];

			tempTraj[i]= frameParticleData[i].position; 
			tempTraj[i].w = 1;
			
			tempType[i] = frameParticleData[i].type; 
		}
		
		// Upload data to compute buffers
		molScript.UpdateComputeBuffers(tempPos, tempRot, tempType, tempState, tempTraj); 
		
		Matrix4x4[] mvp = new Matrix4x4[1];
		mvp[0] = Helper.GetMVPMatrix();
		molScript.mvpMatrixBuffer.SetData(mvp);
		
		updateShader.SetBuffer(0,"trajectoryPositions", molScript.molTrajectoryBuffer);
		updateShader.SetBuffer(0,"positions", molScript.molPositionsBuffer);
		updateShader.SetBuffer (0, "mvpMatrix", molScript.mvpMatrixBuffer);
		
		updateShader.SetFloat("screenWidth", Screen.width);
		updateShader.SetFloat("screenHeight", Screen.height);
		updateShader.SetFloat("maximumScreenSpeed", maximumScreenSpeed);	
		updateShader.SetFloat("extraWurst", extraWurst);	
		
		// Compute new positions
		updateShader.Dispatch(0, 64, 1, 1);
		
		// Fetch position data from compute buffer
		molScript.molPositionsBuffer.GetData(tempPos);
		
		// Set new positions to mol objects
		for(int i = 0; i < frameParticleData.Length; i++)
		{
			int objectIndex = ownershipMapSnapshot[i];			
			molObjects[objectIndex].transform.position = tempPos[i];
		}
	}
}
