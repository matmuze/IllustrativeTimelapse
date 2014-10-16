using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;

public static class SimulationDataReader
{
	private static FileStream fileStream;

	private static int[] nearestReactions;
	private static Dictionary<int, int[]> reactionFrameIds;

	public static void BuildIndexToIdMap()
	{
		reactionFrameIds = new Dictionary<int, int[]>();
		nearestReactions = new int[MainScript.NUM_FRAMES];
		
		for(int f = 0; f < MainScript.NUM_FRAMES; f++)
		{
			if(MainScript.reactionFrameStart[f] == -1)
			{
				if(f == 0)
				{
					reactionFrameIds.Add(0, LoadFrameData (f).ToList().Select(e => e.id).ToArray());
					nearestReactions[0] = 0;
				}
				else 
					nearestReactions[f] = nearestReactions[f-1];
			}
			else
			{
				reactionFrameIds.Add(f, LoadFrameData (f).ToList().Select(e => e.id).ToArray());
				nearestReactions[f] = f;
			}
		}
	}

	private static int FindParticleIndexFromId (int frame, int particleId)
	{
		return Array.FindIndex(reactionFrameIds[nearestReactions[frame]], e => e == particleId);
	}

	public static ParticleData LoadParticleData (int frame, int particleId)
	{
		if(fileStream == null)
			fileStream = new FileStream(MainScript.viz_data_path, FileMode.Open);
		
		int particleIndex = FindParticleIndexFromId(frame, particleId);
		long offset = (long)frame * (long)MainScript.FRAME_SIZE + particleIndex * MainScript.FRAME_PARTICLE_SIZE;
		byte[] particleBytes = new byte[MainScript.FRAME_PARTICLE_SIZE];
		
		fileStream.Seek(offset, SeekOrigin.Begin);
		fileStream.Read(particleBytes, 0, MainScript.FRAME_PARTICLE_SIZE);		
		
		ParticleData particleFrameData = new ParticleData();
		
		particleFrameData.type = (int)BitConverter.ToSingle(particleBytes, 0 * sizeof(float));
		particleFrameData.id = (int)BitConverter.ToSingle(particleBytes, 1 * sizeof(float));
		particleFrameData.position.x = -BitConverter.ToSingle(particleBytes, 2 * sizeof(float));			
		particleFrameData.position.y = BitConverter.ToSingle(particleBytes, 3 * sizeof(float));
		particleFrameData.position.z = BitConverter.ToSingle(particleBytes, 4 * sizeof(float));
		particleFrameData.orientation.x = BitConverter.ToSingle(particleBytes, 5 * sizeof(float));
		particleFrameData.orientation.y = BitConverter.ToSingle(particleBytes, 6 * sizeof(float));
		particleFrameData.orientation.z = BitConverter.ToSingle(particleBytes, 7 * sizeof(float));
		
		particleFrameData.surface = (particleFrameData.orientation.x != 0 || particleFrameData.orientation.y != 0 || particleFrameData.orientation.z != 0);			
		particleFrameData.position = Quaternion.Euler(-90,0,0) * particleFrameData.position * MainScript.SCALING_FACTOR;
		particleFrameData.orientation = Quaternion.Euler(-90,0,0) * particleFrameData.orientation;
		particleFrameData.orientation.y *= -1;
		
		return particleFrameData;
	}
	
	public static ParticleData[] LoadFrameData (int frame)
	{	
		if(fileStream == null)
			fileStream = new FileStream(MainScript.viz_data_path, FileMode.Open);
		
		long offset = (long)frame * (long)MainScript.FRAME_SIZE;
		byte[] frameBytes = new byte[MainScript.FRAME_SIZE];
		
		fileStream.Seek(offset, SeekOrigin.Begin);
		fileStream.Read(frameBytes, 0, MainScript.FRAME_SIZE);
		
		ParticleData[] frameData = new ParticleData[MainScript.MAX_NUM_PARTICLES];
		
		for (var i = 0; i < MainScript.MAX_NUM_PARTICLES; i++)
		{
			frameData[i].type = (int)BitConverter.ToSingle(frameBytes, i * MainScript.FRAME_PARTICLE_SIZE + (0 * sizeof(float)));
			frameData[i].id = (int)BitConverter.ToSingle(frameBytes, i * MainScript.FRAME_PARTICLE_SIZE + (1 * sizeof(float)));
			frameData[i].position.x = -BitConverter.ToSingle(frameBytes, i * MainScript.FRAME_PARTICLE_SIZE + (2 * sizeof(float)));			
			frameData[i].position.y = BitConverter.ToSingle(frameBytes, i * MainScript.FRAME_PARTICLE_SIZE + (3 * sizeof(float)));
			frameData[i].position.z = BitConverter.ToSingle(frameBytes, i * MainScript.FRAME_PARTICLE_SIZE + (4 * sizeof(float)));
			frameData[i].orientation.x = BitConverter.ToSingle(frameBytes, i * MainScript.FRAME_PARTICLE_SIZE + (5 * sizeof(float)));
			frameData[i].orientation.y = BitConverter.ToSingle(frameBytes, i * MainScript.FRAME_PARTICLE_SIZE + (6 * sizeof(float)));
			frameData[i].orientation.z = BitConverter.ToSingle(frameBytes, i * MainScript.FRAME_PARTICLE_SIZE + (7 * sizeof(float)));
			
			frameData[i].surface = (frameData[i].orientation.x != 0 || frameData[i].orientation.y != 0 || frameData[i].orientation.z != 0);			
			frameData[i].position = Quaternion.Euler(-90,0,0) * frameData[i].position * MainScript.SCALING_FACTOR;
			frameData[i].orientation = Quaternion.Euler(-90,0,0) * frameData[i].orientation;
			frameData[i].orientation.y *= -1;
		}	
		
		return frameData;
	}	
}
