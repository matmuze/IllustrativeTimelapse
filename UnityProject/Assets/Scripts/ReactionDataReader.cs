using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

using UnityEngine;

public static class ReactionDataReader
{
	public static void ReadReactionData(bool reloadReactionData)
	{
		// Declare and init reactionPerFrame array
		MainScript.reactionFrameStart = new int[MainScript.NUM_FRAMES];
		MainScript.reactionFrameEnd = new int[MainScript.NUM_FRAMES];

		for(int i = 0; i < MainScript.NUM_FRAMES; i++) 
		{
			MainScript.reactionFrameStart[i] = -1;
			MainScript.reactionFrameEnd[i] = -1;
		}

		if(!reloadReactionData && File.Exists(Path.Combine(Application.persistentDataPath, "reactions.xml")))
		{
			var serializer = new XmlSerializer(typeof(ReactionData[]));
			var stream = new FileStream(Path.Combine(Application.persistentDataPath, "reactions.xml"), FileMode.Open);
			MainScript.reactions = serializer.Deserialize(stream) as ReactionData[];
			stream.Close();
			
			BuildReactionMaps();
		}
		else
		{
			ReadReactionsFromFile();
		}
	}

	static void BuildReactionMaps()
	{
		for(int i = 0; i < MainScript.reactions.Length; i++)
		{
			if(MainScript.reactionFrameStart[MainScript.reactions[i].frame] == -1)
			{
				MainScript.reactionFrameStart[MainScript.reactions[i].frame] = i;
				MainScript.reactionFrameEnd[MainScript.reactions[i].frame] = i;
			}
			else
			{
				MainScript.reactionFrameEnd[MainScript.reactions[i].frame] ++;
			}
		}
	}

	static void ReadReactionsFromFile()
	{
		// Read reaction data
		List<ReactionData> reactionList = new List<ReactionData>();
		
		StreamReader reader = File.OpenText(MainScript.rxn_data_path);
		string line;
		
		while ((line = reader.ReadLine()) != null)
		{
			string[] fields = line.Split(' ');
			
			ReactionData reactionData = new ReactionData();
			reactionData.frame = int.Parse(fields[0]);
			
			if(reactionData.frame >= MainScript.NUM_FRAMES) break;
			
			reactionData.time = float.Parse(fields[1]);
			reactionData.position = Quaternion.Euler(-90,0,0) * new Vector3(-float.Parse(fields[2]), float.Parse(fields[3]), -float.Parse(fields[4])) * MainScript.SCALING_FACTOR;
			reactionData.position.y *= -1;
			reactionData.type = fields[5];
			reactionData.reactants = new int[2];
			reactionData.products = new int[1];
			
			reactionList.Add(reactionData);
		}		
		MainScript.reactions = reactionList.ToArray();
		
		BuildReactionMaps();

		ProcessReactionData();
		
		var serializer = new XmlSerializer(typeof(ReactionData[]));
		var stream = new FileStream(Path.Combine(Application.persistentDataPath, "reactions.xml"), FileMode.Create);
		serializer.Serialize(stream, MainScript.reactions);
		stream.Close();
	}

	static void ProcessReactionData ()
	{
		for(int i = 1; i < MainScript.reactionFrameStart.Length; i++)
		{
			if(MainScript.reactionFrameStart[i] == -1) continue;
			
			int NUM_REACTIONS = (MainScript.reactionFrameEnd[i] - MainScript.reactionFrameStart[i]) + 1;			
			Debug.Log ("Frame: " + i + " Reaction count: " + NUM_REACTIONS);
			
			var previousFrameData = SimulationDataReader.LoadFrameData(i-1).ToList();
			var reactionFrameData = SimulationDataReader.LoadFrameData(i).ToList();	
			
			int[] previousSortedIDs = previousFrameData.Select(e => e.id).OrderBy(e => e).ToArray();
			int[] reactionSortedIDs = reactionFrameData.Select(e => e.id).OrderBy(e => e).ToArray();
			
			List<int> reactants = new List<int>();
			for(int j = 0; j < NUM_REACTIONS; j++) 
			{
				reactants.Add(-1);
			}
			
			List<int> products = new List<int>();
			for(int j = 0; j < NUM_REACTIONS; j++) 
			{
				products.Add(-1);
			}
			
			List<int> partners = new List<int>();
			for(int j = 0; j < NUM_REACTIONS; j++) 
			{
				partners.Add(-1);
			}
			
			// Find reactants
			
			int a = 0;
			int b = 0;
			int c = 0;
			
			while(a < previousSortedIDs.Length)
			{
				while(b < reactionSortedIDs.Length)
				{
					if(previousSortedIDs[a] == reactionSortedIDs[b]) 
					{
						b++;
						break;
					}
					
					if(previousSortedIDs[a] < reactionSortedIDs[b])
					{
						int ind = -1;
						
						int reactant = previousSortedIDs[a];
						int reactantIndex = previousFrameData.FindIndex( e => e.id == reactant);
						
						float reactionDistance = float.MaxValue;
						int reaction = -1;
						
						for(int j = 0; j < NUM_REACTIONS; j++)
						{
							if(reactants[j] != -1) continue;
							
							ind = MainScript.reactionFrameStart[i]+j;
							
							float d = Vector3.Distance(previousFrameData[reactantIndex].position, MainScript.reactions[ind].position);
							if(d < reactionDistance)
							{
								reactionDistance = d;
								reaction = j;
							}
						}
						
						reactants[reaction] = reactant;
						//						Debug.Log("Reactant reaction distance: " + reactionDistance);
						
						c++;
						break;
					}
					b++;
				}
				a++;
			}
			
			if(c != NUM_REACTIONS)
			{
				Debug.LogError("Some fucked up thing just happened");
			}
			
			if(reactants.Contains(-1))
			{
				Debug.LogError("Found empty entries in reactants");
			}
			
			if(reactants.Distinct().Count() != reactants.Count())
			{
				Debug.LogError("Found duplicates in reactants");
			}
			
			// Find products
			
			a = reactionSortedIDs.Length-1;
			b = previousSortedIDs.Length-1;
			c = 0;
			
			while(a > 0)
			{
				while(b > 0)
				{
					if(reactionSortedIDs[a] == previousSortedIDs[b]) 
					{
						b--;
						break;
					}
					
					if(reactionSortedIDs[a] > previousSortedIDs[b])
					{
						int ind = -1;
						
						int product = reactionSortedIDs[a];
						int productIndex = reactionFrameData.FindIndex( e => e.id == product);
						
						float reactionDistance = float.MaxValue;
						int reaction = -1;
						
						for(int j = 0; j < NUM_REACTIONS; j++)
						{
							if(products[j] != -1) continue;
							
							ind = MainScript.reactionFrameStart[i]+j;
							
							float d = Vector3.Distance(reactionFrameData[productIndex].position, MainScript.reactions[ind].position);
							if(d < reactionDistance)
							{
								reactionDistance = d;
								reaction = j;
							}
						}
						
						products[reaction] = product;
						//						Debug.Log("Product reaction distance: " + reactionDistance);
						
						c++;
						break;
					}					
					b--;
				}				
				a--;
			}	
			
			if(c != NUM_REACTIONS)
			{
				Debug.LogError("Some fucked up thing just happened");
			}
			
			if(products.Contains(-1))
			{
				Debug.LogError("Found empty entries in products");
			}
			
			if(products.Distinct().Count() != products.Count())
			{
				Debug.LogError("Found duplicates in products");
			}
			
			// Find partners
			
			for(int j = 0; j < NUM_REACTIONS; j++)
			{
				int partner = -1;
				float partnerDistance = float.MaxValue;
				
				int ind = MainScript.reactionFrameStart[i]+j;
				
				for(int kk = 0; kk < previousFrameData.Count; ++kk)
				{
					if(previousFrameData[kk].type != 2) continue;
					if(partners.Contains(previousFrameData[kk].id)) continue;
					
					float d = Vector3.Distance(previousFrameData[kk].position, MainScript.reactions[ind].position);
					
					if(d < partnerDistance)
					{
						partner = previousFrameData[kk].id;
						partnerDistance = d;
					}
				}	
				partners[j] = partner;
				//				Debug.Log("Partner reaction distance: " + partnerDistance);
			}
			
			if(partners.Contains(-1))
			{
				Debug.LogError("Found empty entries in partners");
			}
			
			if(partners.Distinct().Count() != partners.Count())
			{
				Debug.LogError("Found duplicates in partners");
			}
			
			for(int j = 0; j < NUM_REACTIONS; j++)
			{
				int ind = MainScript.reactionFrameStart[i]+j;
				
				MainScript.reactions[ind].reactants[0] = reactants[j];
				MainScript.reactions[ind].reactants[1] = partners[j];
				MainScript.reactions[ind].products[0] = products[j];
				
				Debug.Log("Reaction: " + ind + " reactant: " + reactants[j] + " partner: " + partners[j] + " product: " + products[j]);
			}
		}
	}
}