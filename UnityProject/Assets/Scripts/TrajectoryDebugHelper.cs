

//public GameObject trajectoryHelper;
//public Material trajectoryMaterial;
//int[] trajectoryIndices;
//Mesh trajectoryMesh;
//Vector3[] trajectoryVertices;
//int trajectoryObjectId = 1;
//
///*****/
//
//void Start()
//{
//	trajectoryMesh = new Mesh();
//	trajectoryIndices = new int[1000];
//	trajectoryVertices = new Vector3[1000];
//
//	for(int i = 0; i < 1000; i++)
//	{
//		trajectoryIndices[i] = i;
//		
//		ParticleData[] reactionFrameData = SimulationDataReader.LoadFrameData(i);
//		int index = reactionFrameData.ToList().FindIndex( e => e.id == trajectoryObjectId);
//		trajectoryVertices[i] = reactionFrameData[index].position;
//	}
//	
//	trajectoryMesh.vertices = trajectoryVertices;
//	trajectoryMesh.SetIndices(trajectoryIndices, MeshTopology.LineStrip, 0);
//}
//
//void Update () 
//{			
//	if(showTrajectory)
//	{
//		if(!trajectoryHelper.activeSelf)
//		{
//			trajectoryHelper.SetActive(true);
//		}
//		
//		Graphics.DrawMesh(trajectoryMesh, Vector3.zero, Quaternion.identity, trajectoryMaterial, 0);
//		trajectoryHelper.transform.position = framePositions[trajectoryObjectId];
//	}
//	else
//	{
//		if(trajectoryHelper.activeSelf)
//		{
//			trajectoryHelper.SetActive(false);
//		}
//	}
//}
//	
