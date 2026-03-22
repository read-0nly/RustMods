
using Convert = System.Convert;
using Network;
using Oxide.Core.Plugins;
using Oxide.Core;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using Rust.Ai;
using System;
using UnityEngine; 
using UnityEngine.AI; 
using  UnityEditor; 
using Oxide.Core.Libraries.Covalence;
using Oxide.Plugins;
	using Oxide.Core.Plugins;
using System.Threading;
	using Oxide.Core;
	using Newtonsoft.Json;
using Oxide.Ext.RustEdit;
using System.Reflection;

namespace Oxide.Plugins
{
	[Info("CustomAIZones", "obsol", "0.6.1")]
	[Description("Allows the creation of custom AI zones, move and cover points for custom monuments - allows using native AI in custom monuments. If you're a map maker, visit <url> for details on how to make compatible monuments. Facepunch if you read this why haven't you done this yet. Why is AI designing also locked down? Why is the default statemap still likely to completely break if takecover can't find cover? StateError isn't handled")]
	public class CustomAIZones : CovalencePlugin
	{
		
		#region config
		public static ConfigData config;
		public bool zonesCreated = false;
		public class ConfigData
		{
			[JsonProperty("version", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Oxide.Core.VersionNumber Version = default(VersionNumber);
			[JsonProperty("debug mode", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public bool debug = false;
		}
		protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<ConfigData>();
                if (config == null)
                {
                    LoadDefaultConfig();
                }
                else
                {
                    UpdateConfigValues();
                }
            }
            catch (Exception ex)
            {
                PrintError($"The configuration file is corrupted or missing. \n{ex}");
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
			Puts("Version mismatch for config");
            config = new ConfigData();
            config.Version = Version;
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        private void UpdateConfigValues()
        {
            if (config.Version < Version)
            {
                if (config.Version <= default(VersionNumber))
                {
					Puts("Version mismatch for config");
                }
                config.Version = Version;
            }
        }

        private bool GetConfigValue<T>(out T value, params string[] path)
        {
            var configValue = Config.Get(path);
            if (configValue == null)
            {
                value = default(T);
                return false;
            }
            value = Config.ConvertValue<T>(configValue);
            return true;
        }

		[Command("CAZ-loadcfg")] //
		private void CAZloadcfg(IPlayer iplayer, string command, string[] args){
			LoadConfig();
		}
		[Command("CAZ-savecfg")] //
		private void CAZsavecfg(IPlayer iplayer, string command, string[] args){	
			SaveConfig();
		}
		[Command("CAZ-debug")] //
		private void CAZdebug(IPlayer iplayer, string command, string[] args){	
			config.debug=!config.debug;
		}
		#endregion
		
		List<AIPoint> customPoints = new List<AIPoint>();
		List<AIMovePoint> customMovePoints = new List<AIMovePoint>();
		List<AICoverPoint> customCoverPoints = new List<AICoverPoint>();
		List<MonumentInfo> customMonuments = new List<MonumentInfo>();
		List<AIMovePointPath> allPaths = new List<AIMovePointPath>();
		struct AgentProperties{
			public static float acceleration;
			public static int agentTypeID;
			public static float angularSpeed;
			public static int areaMask;
			public static bool autoBraking;
			public static bool autoRepath;
			public static bool autoTraverseOffMeshLink;
			public static int avoidancePriority;
			public static float baseOffset;
			public static float height;
			public static ObstacleAvoidanceType obstacleAvoidanceType;
			public static float radius;
			public static float speed;
			public static float stoppingDistance;
			public static bool updatePosition;
			public static bool updateRotation;
			public static bool updateUpAxis;
			public static int walkableMask;
			
			public static string DefaultArea = "Walkable";
			
			public static void Store(NavMeshAgent nma){
			 acceleration = nma.acceleration;
			 agentTypeID = nma.agentTypeID;
			 angularSpeed = nma.angularSpeed;
			 areaMask = nma.areaMask;
			 autoBraking = nma.autoBraking;
			 autoRepath = nma.autoRepath;
			 autoTraverseOffMeshLink = nma.autoTraverseOffMeshLink;
			 avoidancePriority = nma.avoidancePriority;
			 baseOffset = nma.baseOffset;
			 height = nma.height;
			 obstacleAvoidanceType = nma.obstacleAvoidanceType;
			 radius = nma.radius;
			 speed = nma.speed;
			 stoppingDistance = nma.stoppingDistance;
			 updatePosition = nma.updatePosition;
			 updateRotation = nma.updateRotation;
			 updateUpAxis = nma.updateUpAxis;
			 walkableMask = nma.walkableMask;
			 
			 BaseNavigator bn = nma.GetComponent<BaseNavigator>();
			 if(bn!=null){DefaultArea = bn.DefaultArea;}
			}
			public static void Set(NavMeshAgent nma){
			 nma.acceleration = acceleration;
			 nma.agentTypeID = agentTypeID;
			 nma.angularSpeed = angularSpeed;
			 nma.areaMask = areaMask;
			 nma.autoBraking = autoBraking;
			 nma.autoRepath = autoRepath;
			 nma.autoTraverseOffMeshLink = autoTraverseOffMeshLink;
			 nma.avoidancePriority = avoidancePriority;
			 nma.baseOffset = baseOffset;
			 nma.height = height;
			 nma.obstacleAvoidanceType = obstacleAvoidanceType;
			 nma.radius = radius;
			 nma.speed = speed;
			 nma.stoppingDistance = stoppingDistance;
			 nma.updatePosition = updatePosition;
			 nma.updateRotation = updateRotation;
			 nma.updateUpAxis = updateUpAxis;
			 nma.walkableMask = walkableMask;
				
			 BaseNavigator bn = nma.GetComponent<BaseNavigator>();
			 if(bn!=null){bn.DefaultArea =DefaultArea;}
			}
		}
		ProtoBuf.AIDesign roamDesign = new ProtoBuf.AIDesign();
		ProtoBuf.AIDesign followpathdesign = new ProtoBuf.AIDesign();
		
		void RustEdit_OnMapDataProcessed(){
			Puts("Getting RE spawners");
			RESpawners = GameObject.FindObjectsOfType<Oxide.Ext.RustEdit.NPC.NPCSpawner>();
			Puts("Found " +RESpawners.Count()+ " spawners");
			
		}
		
		private void RustEdit_NPCSpawned(BasePlayer spawned){
			Vector3 pos = spawned.transform.position;
			NextFrame(()=>{NextFrame(()=>{
				if(RESpawners == null || RESpawners.Count()==0){//
					return;
				}
				List<HumanNPC> list = new List<HumanNPC>();//
				Vis.Entities<global::HumanNPC>(pos, 0.1f, list, -1, QueryTriggerInteraction.UseGlobal);
				if(list == null || list.Count==0){
					return;
				}
				
				Oxide.Ext.RustEdit.NPC.NPCSpawner spawnpick = null;
				float bestDist=999;
				foreach(Oxide.Ext.RustEdit.NPC.NPCSpawner spawner in RESpawners){
					float dist = Vector3.Distance(spawner.transform.position,pos);
					if(dist<bestDist){
						bestDist=dist;
						spawnpick=spawner;
					}
				}
				if(spawnpick==null){return;}
				FieldInfo[] myFieldInfo;
				Type myType = spawnpick.GetType();
				// Get the type and fields of FieldInfoClass.
				myFieldInfo = myType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance
					| BindingFlags.Public);
					
				for(int i = 0; i < myFieldInfo.Length; i++)
				{
					if(myFieldInfo[i].FieldType.ToString()=="BaseCombatEntity"){						
						myFieldInfo[i].SetValue(spawnpick, list[0]);
						Puts("Assigned NPC to RustEdit Spawn");
					}
				}
				
			});});
		}
		private void OnTerrainInitialized(){
			Puts("Getting Points");
			GetCustomAIPoints();
			
			Puts("Getting markers");
			zonesCreated=true;
			GetMonumentMarkers();//assets/bundled/prefabs/modding/volumes_and_triggers/monument_marker.prefab
			loaddefaultnpcstuff();
		}
		private void loaddefaultnpcstuff(){
			UnityEngine.Debug.LogError("TriggeredAgentProp");
			ConVar.Entity.EntitySpawnRequest spawnEntityFromName = ConVar.Entity.GetSpawnEntityFromName("scientistnpc_junkpile_pistol");
			if (!spawnEntityFromName.Valid)
			{
				return;
			}
			BaseEntity baseEntity = GameManager.server.CreateEntity(spawnEntityFromName.PrefabName, Vector3.zero,Quaternion.LookRotation(Vector3.forward, Vector3.up), false);
			
			AgentProperties.Store(baseEntity.GetComponent<NavMeshAgent>());
			UnityEngine.Debug.LogError("FinishedAgentProp");
			GameObject.Destroy(baseEntity.gameObject);//
			
			spawnEntityFromName = ConVar.Entity.GetSpawnEntityFromName("scientistnpc_roam");
			if (!spawnEntityFromName.Valid)
			{
				return;
			}
			BaseEntity baseEntity2 = GameManager.server.CreateEntity(spawnEntityFromName.PrefabName, Vector3.zero,Quaternion.LookRotation(Vector3.forward, Vector3.up), false);
			
			BaseAIBrain brain = baseEntity2.gameObject.GetComponent<BaseAIBrain>();
			if(brain!=null){
				if(brain.InstanceSpecificDesign!=null){
					Puts("Instance Loaded");////
					brain.InstanceSpecificDesign.CopyTo(roamDesign);
				}
				else{
					if(brain.Designs.Count>0){
						Puts("Design Loaded");
						AIDesigns.GetByNameOrInstance(brain.Designs[0].Filename, null).CopyTo(roamDesign);
					}else{
						Puts("DesignSO Loaded");
						AIDesigns.GetByNameOrInstance(brain.DefaultDesignSO.Filename, null).CopyTo(roamDesign);
					}
				}
				if(roamDesign!=null){
					roamDesign.CopyTo(followpathdesign);
					for(int i = 0; i < followpathdesign.availableStates.Count(); i++){
						if(followpathdesign.availableStates[i]==2){
							followpathdesign.availableStates[i]=0x14;
						}
					}
					for(int i = 0; i < followpathdesign.stateContainers.Count(); i++){
						if(followpathdesign.stateContainers[i].state==2){
							followpathdesign.stateContainers[i].state=0x14;
						}
					}
				}
			}
			GameObject.Destroy(baseEntity2.gameObject);//
			UnityEngine.Debug.LogError("FinishedAgentStateGrab");
			
		}
		private void OnServerInitialized()
        {
			if(!zonesCreated){
				Puts("Getting Points");
				GetCustomAIPoints();
				
				Puts("Getting markers");
				GetMonumentMarkers();//assets/bundled/prefabs/modding/volumes_and_triggers/monument_marker.prefab
				zonesCreated=true;
				Puts("Getting safe Navmesh Agent params");//
				loaddefaultnpcstuff();
			}
			
			
		}
		Oxide.Ext.RustEdit.NPC.NPCSpawner[] RESpawners;
		public class NavmeshAgentSwapFlag : BaseMonoBehaviour{
			public bool swapped = true;
			
		}
		private void OnEntitySpawned(HumanNPC hn){
			if(zonesCreated==false){hn.Kill();return;}
			if(hn.GetComponent<NavmeshAgentSwapFlag>()){return;}
			NavMeshHit navMeshHit;
			bool inZone = false;
			
			AIInformationZone forPoint=null;
			foreach(MonumentInfo custom in customMonuments){
				if(custom.GetComponent<AIInformationZone>()!=null&&custom.GetComponent<AIInformationZone>().bounds.Contains(hn.transform.position)){
					inZone=true;
					forPoint=custom.GetComponent<AIInformationZone>();
					break;
				}			
			}	
			if(!inZone){
				hn.gameObject.AddComponent<NavmeshAgentSwapFlag>();
				
				NextFrame(()=>{
					SpawnPointInstance spi = hn.gameObject.GetComponent<SpawnPointInstance>();
					NPCPlayerNavigator nma2 = hn.GetComponent<NPCPlayerNavigator>();
					if(spi!=null && nma2!=null && spi.parentSpawnPointUser is NPCSpawner){
						nma2.Path = (spi.parentSpawnPointUser as NPCSpawner).Path;
						Puts("NOTINZONE: Has Path?"+((spi.parentSpawnPointUser as NPCSpawner).Path!=null));
						Puts("NOTINZONE: NPCSPAWNER "+(spi.parentSpawnPointUser as NPCSpawner).transform.parent.name);
						AIMovePointPath t = (spi.parentSpawnPointUser as NPCSpawner).transform.parent.gameObject.GetComponentInChildren<AIMovePointPath>();
						Puts("NOTINZONE: FoundPint"+(t!=null));
						if(nma2.Path==null){
							nma2.Path=t;
						}
						Puts("NOTINZONE: HasDesign?"+(hn.gameObject.GetComponent<BaseAIBrain>().AIDesign!=null).ToString());
						Puts("NOTINZONE: Idle?"+hn.gameObject.GetComponent<BaseAIBrain>().SwitchToState(AIState.Idle,0));
						foreach(int aisc_key in hn.gameObject.GetComponent<BaseAIBrain>().AIDesign.stateContainers.Keys){
							if(hn.gameObject.GetComponent<BaseAIBrain>().AIDesign.stateContainers[aisc_key].State ==AIState.FollowPath){
								Puts("NOTINZONE: FollowPath?"+hn.gameObject.GetComponent<BaseAIBrain>().SwitchToState(AIState.FollowPath,aisc_key));
								
							}
						}
						Puts("");
					}
				});
				if(config.debug){
					ConsoleNetwork.BroadcastToAllClients("ddraw.text", new object[]
					{
						600,
						global::UnityEngine.Color.red,
						hn.transform.position,
						"[ ]"
					});
				}
				return;
			}
			Puts("In Zone");
			HumanNPC baseEntity = GameManager.server.CreateEntity(hn.gameObject.name, hn.transform.position, hn.transform.rotation, false) as HumanNPC;	
			Vector3 spawnpoint = hn.transform.position;
			NavMeshAgent nma = baseEntity.GetComponent<NavMeshAgent>();
			BaseAIBrain brain = baseEntity.gameObject.GetComponent<BaseAIBrain>();
			NPCPlayerNavigator nav = baseEntity.gameObject.GetComponent<NPCPlayerNavigator>();
			AgentProperties.Set(nma);	
			if(brain!=null && roamDesign!=null && followpathdesign!=null){	
				if (forPoint == null)
				{			
					brain.InstanceSpecificDesign=roamDesign;
					Puts("Applied Roam State");
				}else{
					brain.InstanceSpecificDesign=followpathdesign;
					Puts("Applied Followpath State");

				}
				brain.UseAIDesign=true;
			}
			baseEntity.gameObject.AddComponent<NavmeshAgentSwapFlag>();
			//baseEntity.IsDormant=false;
			//baseEntity.syncPosition=true;
			baseEntity.gameObject.AwakeFromInstantiate();
			baseEntity.EnableSaving(false);//
			//nma.enabled=true;
			baseEntity.Spawn();
			//nav.Warp(baseEntity.transform.position);
			NextFrame(()=>{
				SpawnPointInstance spi = hn.gameObject.GetComponent<SpawnPointInstance>();
				if(spi!=null){//assets/bundled/prefabs/autospawn/monument/medium/compound.prefab
						Puts("Transferring spawn instance");
						SpawnPointInstance spawnPointInstance = baseEntity.gameObject.AddComponent<SpawnPointInstance>();
						(spi.parentSpawnPointUser as SpawnGroup).PostSpawnProcess(baseEntity, spi.parentSpawnPoint);
						spawnPointInstance.parentSpawnPointUser = spi.parentSpawnPointUser;
						spawnPointInstance.parentSpawnPoint = spi.parentSpawnPoint;
						spawnPointInstance.Entity = baseEntity;
						spawnPointInstance.Notify();
						Puts("Transferred spawn instance");
				}
				if(config.debug){
					ConsoleNetwork.BroadcastToAllClients("ddraw.text", new object[]
					{
						600,
						global::UnityEngine.Color.green,
						hn.transform.position,
						"[ ]"
					});
				}
				Puts("Can Idle?");
				brain.SwitchToState(AIState.Idle,0);
				Puts("Setting path/roam");
				if (forPoint == null)
				{			
					foreach(int aisc_key in brain.AIDesign.stateContainers.Keys){
						if(brain.AIDesign.stateContainers[aisc_key].State ==AIState.Roam){
							Puts("Key found "+aisc_key);
							Puts("Custom Roam?"+brain.SwitchToState(AIState.Roam,aisc_key));
							break;
						}
					}
				}else{
					foreach(int aisc_key in brain.AIDesign.stateContainers.Keys){
						if(brain.AIDesign.stateContainers[aisc_key].State ==AIState.FollowPath){
							Puts("Key found "+aisc_key);
							Puts("Custom FollowPath?"+brain.SwitchToState(AIState.FollowPath,aisc_key));
							
							break;
						}
					}
					
				}
				nav.Warp(baseEntity.transform.position+new Vector3(0,-0.5f,0));
				nav.PlaceOnNavMesh(1f);
				nav.Agent.enabled=true;
				Puts("isSleep?"+brain.sleeping.ToString());
				Puts("");
				hn.Kill();//
			});
		}
		public void GetCustomAIPoints(){//
			GameObject[] all = UnityEngine.Object.FindObjectsOfType(typeof(GameObject)) as GameObject[];
			foreach(GameObject go in all){
				if(go.transform.name.Contains("assets/prefabs/npc/scientist/patrolpoint.prefab")){
					//Puts("Found point! " + go.transform.name);
					if(go.transform.localScale.y>0){

						AIMovePoint movepoint = go.GetComponent<AIMovePoint>();
						if(movepoint ==null){
							movepoint=go.AddComponent<AIMovePoint>();
							
							//Puts("Movepoint added!");
						}
						movepoint.WaitTime = (go.transform.localScale.y*5);
						Puts("Found MOVE point! " + movepoint.WaitTime + " " + go.transform.localScale.ToString());
						if(!customMovePoints.Contains(movepoint)){
							customMovePoints.Add(movepoint);
							//Puts("Added Movepoint to processing :");
						}
						customPoints.Add(movepoint);
					}
					else{
						
						AICoverPoint coverpoint = go.GetComponent<AICoverPoint>();
						Puts("Found COVER point! " + go.transform.localScale.ToString());
						if(coverpoint ==null){
							coverpoint=go.AddComponent<AICoverPoint>();
							//Puts("coverpoint added!");
						}
						if(!customCoverPoints.Contains(coverpoint)){
							customCoverPoints.Add(coverpoint);
							//Puts("Added coverpoint to processing");
						}
						customPoints.Add(coverpoint);
					}

				}
			}
			
		}
		
		class CustomAIZ : AIInformationZone{
			
		}
		
		//
		public void GetMonumentMarkers(){
			
			MonumentInfo[] Markers =  UnityEngine.Object.FindObjectsOfType(typeof(MonumentInfo)) as MonumentInfo[];
			AIInformationZone[] Zones = UnityEngine.Object.FindObjectsOfType(typeof(AIInformationZone)) as AIInformationZone[];
			Puts("Found "+Markers.Count()+" markers and "+Zones.Count()+" zones");
			foreach(MonumentInfo marker in Markers){	
			
				//Check if marker is already in a zone
				bool isInZone = false;
				CustomAIZ markerzone = marker.GetComponent<CustomAIZ>();
				foreach(AIInformationZone zone in Zones){
					if(markerzone!=null || (zone != markerzone && zone.bounds.Contains(marker.transform.position))){
						isInZone=true;
						Puts("Marker in zone");
						break;
					}
				}
				if(isInZone){continue;}
				Puts("Marker not in zone or self");
				
				//Ignore markers that aren't for AI otherwise add to custom monuments
				if(marker.transform.localScale == new Vector3(1,1,1)){continue;}
				if(!customMonuments.Contains(marker)){
					customMonuments.Add(marker);
					Puts("Marker added to custom monuments");//
				}
				
				//Vars we'll need
				Bounds monumentBounds = new Bounds(marker.transform.position,marker.transform.localScale);
				List<AIMovePoint> myMovePoints = new List<AIMovePoint>();
				
				//Parent points in bounds
				foreach(AIPoint point in customPoints){
					if(monumentBounds.Contains(point.transform.position)){
						Vector3 oldScale = point.transform.localScale;
						point.transform.SetParent(marker.transform);
						point.transform.localScale=oldScale;
						Puts("Point in bounds, parenting!:" + point.transform.localScale);
					}
					if(point is AIMovePoint){
						myMovePoints.Add(point as AIMovePoint);
					}
				}
				
				//Get or create AI Info zone, remove to readd on new start if existing 
				if(markerzone==null){	
					Puts("Adding new zone");
					markerzone = marker.gameObject.AddComponent<CustomAIZ>();
				}
				else{
					if(AIInformationZone.zones.Contains(markerzone)){
						AIInformationZone.zones.Remove(markerzone);
					}
				}
				//Make sure it has a grid
				AIInformationGrid newGrid = marker.gameObject.GetComponent<AIInformationGrid>();
				if(newGrid==null){
					newGrid = marker.gameObject.AddComponent<AIInformationGrid>();
				}
				markerzone.paths=getPaths(myMovePoints,marker);
				//Init zone
				markerzone.bounds=monumentBounds;
				markerzone.ShouldSleepAI=false;
				markerzone.Virtual=false;
				markerzone.Start();				
				
			}
		}
        [Command("ShowPoints")]
        void DemoMonumentSwap_cmd(IPlayer player, string command, string[] args)
        {
			foreach(AIPoint p in customPoints){
				global::ConsoleNetwork.BroadcastToAllClients("ddraw.sphere", new object[]
				{
					60,
					global::UnityEngine.Color.cyan,
					p.transform.position,
					1f
				});
			}
			foreach(AIMovePointPath path in allPaths){
				int i = 0;
				while(i<path.Points.Count()){
					int i2 = (i<path.Points.Count()-1?i+1:0);
					global::ConsoleNetwork.BroadcastToAllClients("ddraw.line", new object[]
					{
						60,
						global::UnityEngine.Color.green,
						path.Points[i].transform.position,
						path.Points[i2].transform.position
					});
					i++;
				}
			}
			
			
		}
		
		public List<AIMovePointPath> getPaths (List<AIMovePoint> allPoints, MonumentInfo marker){
			List<AIMovePoint> pointsToProcess = new List<AIMovePoint>(allPoints.ToArray());
			List<AIMovePoint> processedPoints = new List<AIMovePoint>();
			List<AIMovePointPath> result = new List<AIMovePointPath>();
			Dictionary<int,List<AIMovePoint>> Paths= new Dictionary<int,List<AIMovePoint>>();
			
			int maxSeek=allPoints.Count();
			
			foreach(AIMovePoint p in allPoints.ToArray()){
				int pPath = (int)Mathf.Round(p.transform.localScale.x*1000);
				if(!Paths.ContainsKey(pPath)){
					Paths.Add(pPath,new List<AIMovePoint>());
				}
				Paths[pPath].Add(p);
			}
			foreach(List<AIMovePoint> l in Paths.Values){
				if(l.Count()>0){
					Puts("Path"+l[0].transform.localScale.x+" has "+l.Count()+" points!");
					l.Sort(delegate(AIMovePoint x, AIMovePoint y)
						{
							return (x.transform.localScale.z>y.transform.localScale.z?
								1:-1);
						});
					
					AIMovePointPath path = l[0].gameObject.GetComponent<AIMovePointPath>();
					if(path==null){
						path = l[0].gameObject.AddComponent<AIMovePointPath>();
					}
					path.Points.Clear();
					path.Points.AddRange(l);
					result.Add(path);
				}
				
			}
			
			return result;
		}
		
	}
}
