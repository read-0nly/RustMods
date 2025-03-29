////
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
using System.IO;
using UnityEngine; 
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using Oxide.Plugins;
using Oxide.Core.Plugins;
using Oxide.Core;

namespace Oxide.Plugins
{
	[Info("Psilocybin", "obsol", "0.0.1")]
	[Description("Allows rewiring of State-Event maps using JSON, and applying that structure to instance/prefab/global")]
	public class Psilocybin : CovalencePlugin
	{
		private Game.Rust.Libraries.Player _rustPlayer = Interface.Oxide.GetLibrary<Game.Rust.Libraries.Player>("Player");
		private void SendChatMsg(BasePlayer pl, string msg) => _rustPlayer.Message(pl, msg,  "<color=#00ff00>[Psilocybin]</color>", 0, Array.Empty<object>());
		public Configuration config;
		
		public class Configuration
		{
			public class AIDesign{
				[JsonProperty("description", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public string description="Custom AI Design";
				[JsonProperty("availableStates", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public List<string> availableStates = new List<string>();
				[JsonProperty("defaultStateContainer", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public int defaultStateContainer=0;
				[JsonProperty("intialViewStateID", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public int intialViewStateID=0;
				[JsonProperty("scope", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public int scope=0;
				[JsonProperty("AIStateContainer", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public List<AIStateContainer> stateContainers=new List<AIStateContainer>();				
				public AIDesign(){}
				public AIDesign(ProtoBuf.AIDesign aid){
					foreach(int i in aid.availableStates){
						availableStates.Add(((AIState)i).ToString());
					}
					description=aid.description;
					intialViewStateID=aid.intialViewStateID;
					scope=aid.scope;
					foreach(ProtoBuf.AIStateContainer asc in aid.stateContainers){
						stateContainers.Add(new AIStateContainer(asc));
					}
				}
				public ProtoBuf.AIDesign ToProto(){
					ProtoBuf.AIDesign aid = new ProtoBuf.AIDesign();
					aid.availableStates=new List<int>();
					foreach(string s in availableStates){
						aid.availableStates.Add(((int)Enum.Parse(typeof(AIState),s)));
					}
					aid.description=description;
					aid.intialViewStateID=intialViewStateID;
					aid.scope=scope;
					aid.stateContainers = new List<ProtoBuf.AIStateContainer>();
					foreach(AIStateContainer asc in stateContainers){
						aid.stateContainers.Add(asc.ToProto());
					}
					return aid;
				}
			}
			public class AIStateContainer{
				[JsonProperty("id", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public int id=-1;
				[JsonProperty("inputMemorySlot", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public int inputMemorySlot=-1;
				[JsonProperty("state", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public string state="";
				[JsonProperty("events", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public List<AIEventData> events = new List<AIEventData>();
				public AIStateContainer(){}				
				public AIStateContainer(ProtoBuf.AIStateContainer asc){
					id=asc.id;
					inputMemorySlot=asc.inputMemorySlot;
					state=((AIState)asc.state).ToString();
					foreach(ProtoBuf.AIEventData aed in asc.events){
						events.Add(new AIEventData(aed));
					}
				}
				public ProtoBuf.AIStateContainer ToProto(){
					ProtoBuf.AIStateContainer asc = new ProtoBuf.AIStateContainer();
					asc.id=id;
					asc.inputMemorySlot=inputMemorySlot;
					asc.state=(int)Enum.Parse(typeof(AIState),state);
					asc.events = new List<ProtoBuf.AIEventData>();
					foreach(AIEventData aed in events){
						asc.events.Add(aed.ToProto());
					}
					return asc;
				}
			}
			public class AIEventData{	
				[JsonProperty("eventType", ObjectCreationHandling = ObjectCreationHandling.Replace)]			
				public string eventType="";//
				[JsonProperty("id", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public int id=-1;//
				[JsonProperty("inverted", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public bool inverted=false;//
				[JsonProperty("inputMemorySlot", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public int inputMemorySlot=-1;//
				[JsonProperty("outputMemorySlot", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public int outputMemorySlot=-1;//
				[JsonProperty("triggerStateContainer", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public int triggerStateContainer=-1;//	
				[JsonProperty("AggressionTimer", ObjectCreationHandling = ObjectCreationHandling.Replace)]			
				public float AggressionTimer = -1f;
				[JsonProperty("AmmoBelow", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public float AmmoBelow = -1f;
				[JsonProperty("Chance", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public float Chance = -1f;
				[JsonProperty("HealthBelow", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public float HealthBelow = -1f;
				[JsonProperty("HungerAbove", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public float HungerAbove = -1f;
				[JsonProperty("InRange", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public float InRange = -1f;
				[JsonProperty("InRangeHome", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public float InRangeHome = -1f;
				[JsonProperty("PlayerDetected", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public float PlayerDetected = -1f;
				[JsonProperty("TargetDetected", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public float TargetDetected = -1f;
				[JsonProperty("ThreatDetected", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public float ThreatDetected = -1f;
				[JsonProperty("TimerDuration", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public float TimerDuration = -1f;
				[JsonProperty("TimerMax", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public float TimerMax = -1f;
				[JsonProperty("ThreatCooldown", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public float ThreatCooldown = -1f;
				[JsonProperty("Tired", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public float Tired = -1f;
				public AIEventData(){}
				public AIEventData(ProtoBuf.AIEventData aed){					
					eventType = ((AIEventType)aed.eventType).ToString();////
					id = aed.id;//
					inputMemorySlot = aed.inputMemorySlot;//
					inverted = aed.inverted;//
					outputMemorySlot = aed.outputMemorySlot;//
					triggerStateContainer = aed.triggerStateContainer;//
					if(aed.aggressionTimerData!=null){
						AggressionTimer = aed.aggressionTimerData.value;						
					}
					if(aed.ammoBelowData!=null){
						AmmoBelow = aed.ammoBelowData.value;						
					}
					if(aed.chanceData!=null){						
						Chance = aed.chanceData.value;
					}
					if(aed.healthBelowData!=null){
						HealthBelow = aed.healthBelowData.healthFraction;						
					}
					if(aed.hungerAboveData!=null){
						HungerAbove = aed.hungerAboveData.value;						
					}
					if(aed.inRangeData!=null){
						InRange = aed.inRangeData.range;						
					}
					if(aed.inRangeOfHomeData!=null){
						InRangeHome = aed.inRangeOfHomeData.range;						
					}
					if(aed.playerDetectedData!=null){
						PlayerDetected = aed.playerDetectedData.range;						
					}
					if(aed.targetDetectedData!=null){
						TargetDetected = aed.targetDetectedData.range;						
					}
					if(aed.threatDetectedData!=null){
						ThreatDetected = aed.threatDetectedData.range;						
					}
					if(aed.timerData!=null){
						TimerDuration = aed.timerData.duration;
						TimerMax = aed.timerData.durationMax;						
					}
					if(aed.timeSinceThreatData!=null){
						ThreatCooldown = aed.timeSinceThreatData.value;						
					}
					if(aed.tirednessAboveData!=null){
						Tired = aed.tirednessAboveData.value;						
					}
				}
				public ProtoBuf.AIEventData ToProto(){		
					ProtoBuf.AIEventData aed = new ProtoBuf.AIEventData();
					aed.eventType = (int)Enum.Parse(typeof(AIEventType),eventType);//
					aed.id = id;//
					aed.inputMemorySlot = inputMemorySlot;//
					aed.inverted = inverted;//
					aed.outputMemorySlot = outputMemorySlot;//
					aed.triggerStateContainer = triggerStateContainer;//
					if(AggressionTimer>-0.01f){
						aed.aggressionTimerData = new ProtoBuf.AggressionTimerAIEventData();
						aed.aggressionTimerData.value=AggressionTimer;						
					}
					if(AmmoBelow>-0.01f){
						aed.ammoBelowData = new ProtoBuf.AmmoBelowAIEventData();						
						aed.ammoBelowData.value=AmmoBelow;
					}
					if(Chance>-0.01f){					
						aed.chanceData = new ProtoBuf.ChanceAIEventData();
						aed.chanceData.value=Chance;
					}
					if(HealthBelow>-0.01f){
						aed.healthBelowData = new ProtoBuf.HealthBelowAIEventData();						
						aed.healthBelowData.healthFraction=HealthBelow;
					}
					if(HungerAbove>-0.01f){
						aed.hungerAboveData = new ProtoBuf.HungerAboveAIEventData();					
						aed.hungerAboveData.value=HungerAbove;
					}
					if(InRange>-0.01f){
						aed.inRangeData = new ProtoBuf.InRangeAIEventData();					
						aed.inRangeData.range=InRange;
					}
					if(InRangeHome>-0.01f){
						aed.inRangeOfHomeData = new ProtoBuf.InRangeOfHomeAIEventData();					
						aed.inRangeOfHomeData.range=InRangeHome;						
					}
					if(PlayerDetected>-0.01f){
						aed.playerDetectedData = new ProtoBuf.PlayerDetectedAIEventData();					
						aed.playerDetectedData.range=PlayerDetected ;						
					}
					if(TargetDetected>-0.01f){
						aed.targetDetectedData = new ProtoBuf.TargetDetectedAIEventData();					
						aed.targetDetectedData.range=TargetDetected;						
					}
					if(ThreatDetected>-0.01f){
						aed.threatDetectedData = new ProtoBuf.ThreatDetectedAIEventData();					
						aed.threatDetectedData.range=ThreatDetected ;						
					}
					if(TimerDuration>-0.01f){
						aed.timerData = new ProtoBuf.TimerAIEventData();
						aed.timerData.duration=TimerDuration;
						aed.timerData.durationMax=TimerMax;						
					}
					if(ThreatCooldown>-0.01f){
						aed.timeSinceThreatData = new ProtoBuf.TimeSinceThreatAIEventData();				
						aed.timeSinceThreatData.value=ThreatCooldown;						
					}
					if(Tired>-0.01f){
						aed.tirednessAboveData = new ProtoBuf.TirednessAboveAIEventData();				
						aed.tirednessAboveData.value=Tired;						
					}
					return aed;
				}
			}
			
			[JsonProperty("Designs", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<string,AIDesign> AIDesigns = new Dictionary<string,AIDesign>();
			[JsonProperty("Assignments", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Dictionary<string,AIDesign> Assignments = new Dictionary<string,AIDesign>();
			[JsonProperty("GlobalDesign", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public AIDesign GlobalDesign = new AIDesign();
			
			public string ToJson() => JsonConvert.SerializeObject(this);				
			public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
		}
		protected override void LoadDefaultConfig() => config = new Configuration();//
		protected override void LoadConfig(){
			base.LoadConfig();
			try{
				config = Config.ReadObject<Configuration>();
				if (config == null) throw new JsonException();
				if (!config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys)){LogWarning("Configuration appears to be outdated; updating and saving");SaveConfig();}
			}catch(Exception e){LogWarning($"Configuration file {Name}.json is invalid; using defaults");LogWarning(e.ToString());LoadDefaultConfig();}
		}
		protected override void SaveConfig(){
			LogWarning($"Configuration changes saved to {Name}.json");
			Config.WriteObject(config, true);
		}
		void OnServerInitialized()
        {
			LoadConfig();
		}
		void OnEntitySpawned(BaseNetworkable entity)
		{
			BaseEntity be = (entity as BaseEntity);
			if(be==null)return;
			if(config.Assignments.ContainsKey(be.ShortPrefabName)){
				ApplyDesign(be,config.Assignments[be.ShortPrefabName].ToProto());
			}else{
				if(config.GlobalDesign.availableStates.Count() ==0){
					
				}else{
					ApplyDesign(be,config.GlobalDesign.ToProto());
				}
				
			}
		}
		void ApplyDesign(BaseEntity entity, ProtoBuf.AIDesign aid){
			HumanNPC hn = (entity as HumanNPC);
			BaseNpc bn = (entity as BaseNpc);
			if(hn==null){
				if(bn==null) return;
				BaseAIBrain brain = (bn.GetComponent<BaseAIBrain>());
				if (brain==null){return;}//
				brain.InstanceSpecificDesign = aid;
				if(bn.HasBrain) brain.LoadAIDesignAtIndex(0);
				brain.AllowedToSleep=false;
				Puts("Processed "+entity.transform.name);
					
			}else{
				BaseAIBrain brain = (hn.GetComponent<BaseAIBrain>());
				(hn as HumanNPC).VirtualInfoZone =null;
				(hn as HumanNPC).cachedInfoZone = AIInformationZone.GetForPoint(hn.transform.position, true);
				if (brain==null){ brain = (hn.GetComponent<ScientistBrain>() as BaseAIBrain);}
				if (brain==null){return;}//
				brain.InstanceSpecificDesign = aid;
				if(hn.HasBrain) brain.LoadAIDesignAtIndex(0);
				hn.Brain.AllowedToSleep=false;
				
				Puts("Processed "+entity.transform.name);
			}
		}
		
		
		[Command("aid-assign")] void chat_assign(IPlayer player, string command, string[] args){	
			BasePlayer bp = (BasePlayer)player.Object;
			try{//
				if(args.Count()==0) return;	
				if(args[0].Contains(".")||args[0].Contains("/")||args[0].Contains("\\")) return;	
				if(args.Count()<2){
					
					BaseEntity entity = getLookingAt(bp);
					SendChatMsg(bp, "<color=#00FF00>Assigning to "+entity.name+"</color>");
					BaseAIBrain brain = (entity.GetComponent<BaseAIBrain>());
					
					if (brain==null){ brain = (entity.GetComponent<ScientistBrain>() as BaseAIBrain);}
					if (brain==null){SendChatMsg(bp,"<color=#FF0000>No brain</color>");return;}//
					if(!config.AIDesigns.ContainsKey(args[0])) SendChatMsg(bp,"<color=#FF0000>Key not found: "+args[0]);
					SendChatMsg(bp,"<color=#00FF00>IsNull?: "+(config.AIDesigns[args[0]].ToProto()==null));
					brain.InstanceSpecificDesign = config.AIDesigns[args[0]].ToProto();
					SendChatMsg(bp,"<color=#00FF00>hasDesigns"+(brain.Designs.Count()>0));					
					SendChatMsg(bp,"<color=#00FF00>fname"+brain.Designs[0].Filename);
					SendChatMsg(bp,"<color=#00FF00>InstanceSet");
					brain.LoadAIDesignAtIndex(0);
					SendChatMsg(bp,"<color=#00FF00>DesignSet");
					SendChatMsg(bp, "<color=#00FF00>Assigned to "+entity.ShortPrefabName+" instance</color>");
					brain.OwningPlayer = bp;
				}else if(args[1]=="global"){
					SendChatMsg(bp, "<color=#00FF00>Assigning to Global</color>");
					config.GlobalDesign = config.AIDesigns[args[0]];
				}else if(args[1]=="prefab"){
					BaseEntity entity = getLookingAt(bp);
					SendChatMsg(bp, "<color=#00FF00>Assigning to "+entity.ShortPrefabName+"</color>");
					BaseAIBrain brain = (entity.GetComponent<BaseAIBrain>());
					if (brain==null){ brain = (entity.GetComponent<ScientistBrain>() as BaseAIBrain);}
					if (brain==null){SendChatMsg(bp,"<color=#FF0000>No brain</color>");return;}//
					config.Assignments[entity.ShortPrefabName]=config.AIDesigns[args[0]];
				}
				SaveConfig();
			}
			catch(Exception e){SendChatMsg(bp, "<color=#FF0000>"+e.Message+"</color>");}
		}
		[Command("aid-scan")] void chat_scan(IPlayer player, string command, string[] args){
			BasePlayer bp = (BasePlayer)player.Object;	
			try{
				BaseCombatEntity fc = (BaseCombatEntity)getLookingAt(bp);
				if(Vector3.Distance(fc.gameObject.transform.position,bp.transform.position) < 10){
					if((fc as BaseNpc)==null){
						BaseAIBrain brain = fc.GetComponent<ScientistBrain>();
						if(brain==null){brain=fc.GetComponent<BaseAIBrain>();}
						if(brain==null) {SendChatMsg(bp, "<color=#FF0000>No brain found</color>");return;}
						SendChatMsg(bp, "<color=#00FFFF>["+brain.CurrentState.ToString()+"] ");
						string pathname = (brain.Navigator.Path==null?"<color=#FF0000>No Path</color>":"<color=#00FF00>"+brain.Navigator.Path.transform.parent.name+"</color>");
						SendChatMsg(bp,pathname);
						SendChatMsg(bp, "<color=#00FF00>AgentTypeID: "+brain.Navigator.Agent.agentTypeID);
						SendChatMsg(bp, "<color=#00FF00>Layermask: "+brain.Navigator.DefaultArea);
					}else{
						BaseAIBrain brain = fc.GetComponent<BaseAIBrain>();
						if(brain==null) {SendChatMsg(bp, "<color=#FF0000>No brain found</color>");return;}
						SendChatMsg(bp, "<color=#00FFFF>["+brain.CurrentState.ToString()+"] ");
						string pathname = (brain.Navigator.Path==null?"<color=#FF0000>No Path</color>":"<color=#00FF00>"+brain.Navigator.Path.transform.parent.name+"</color>");
						SendChatMsg(bp,pathname);
						SendChatMsg(bp, "<color=#00FF00>AgentTypeID: "+brain.Navigator.Agent.agentTypeID);
						SendChatMsg(bp, "<color=#00FF00>Layermask: "+brain.Navigator.DefaultArea);
						
					}
				}
			}catch(Exception e){SendChatMsg(bp, "<color=#FF0000>"+e.Message+"</color>");}
			
		}
		[Command("aid-export")] void chat_export(IPlayer player, string command, string[] args){	
			BasePlayer bp = (BasePlayer)player.Object;
			BaseCombatEntity fc = (BaseCombatEntity)getLookingAt(bp);
			if(fc==null){return;}
			
			if(fc is HumanNPC){
				try{
					if(Vector3.Distance(fc.gameObject.transform.position,bp.transform.position) < 10){
						BaseAIBrain brain = fc.GetComponent<ScientistBrain>();
						if(brain==null){brain=fc.GetComponent<BaseAIBrain>();}
						if(brain==null) {SendChatMsg(bp, "<color=#FF0000>No brain found</color>");return;}
						SendChatMsg(bp, "<color=#00FFFF>Exporting AI mapping for ["+fc.faction.ToString()+":"+fc.ShortPrefabName+"]</color>");
						try{
							Configuration.AIDesign aid = new Configuration.AIDesign(brain.AIDesign.ToProto(brain.currentStateContainerID));
							string key = fc.ShortPrefabName;
							if(args.Count()>0)key=args[0];
							if(config.AIDesigns.ContainsKey(key)){config.AIDesigns[key]=aid;}
							else{config.AIDesigns.Add(key,aid);}
							SaveConfig();
						}catch(Exception e){SendChatMsg(bp, "<color=#FF0000>"+e.Message+" ");}		
					}
				}catch(Exception e){SendChatMsg(bp, "<color=#FF0000>"+e.Message+"</color>");}
			}else if (fc is BaseAnimalNPC){
				try{
					if(Vector3.Distance(fc.gameObject.transform.position,bp.transform.position) < 10){
						BaseAIBrain brain = fc.GetComponent<BaseAIBrain>();
						if(brain==null){brain=fc.GetComponent<BaseAIBrain>();}
						if(brain==null) {SendChatMsg(bp, "<color=#FF0000>No brain found</color>");return;}
						SendChatMsg(bp, "<color=#00FFFF>Exporting AI mapping for ["+fc.faction.ToString()+":"+fc.name+"]</color>");
						try{
							Configuration.AIDesign aid = new Configuration.AIDesign(brain.AIDesign.ToProto(brain.currentStateContainerID));
							string key = fc.ShortPrefabName;
							if(args.Count()>0)key=args[0];
							if(config.AIDesigns.ContainsKey(key)){config.AIDesigns[key]=aid;}
							else{config.AIDesigns.Add(key,aid);}
							SaveConfig();
						}catch(Exception e){SendChatMsg(bp, "<color=#FF0000>"+e.Message+" ");}		
					}
				}catch(Exception e){SendChatMsg(bp, "<color=#FF0000>"+e.Message+"</color>");}
				
			}else if(fc is CH47HelicopterAIController){
				try{
					if(Vector3.Distance(fc.gameObject.transform.position,bp.transform.position) < 10){
						BaseAIBrain brain = fc.GetComponent<CH47AIBrain>();
						if(brain==null){brain=fc.GetComponent<CH47AIBrain>();}
						if(brain==null) {SendChatMsg(bp, "<color=#FF0000>No brain found</color>");return;}
						SendChatMsg(bp, "<color=#00FFFF>Exporting AI mapping for ["+fc.faction.ToString()+":"+fc.name+"]</color>");
						try{
							if (brain.UseAIDesign)
							{
								Configuration.AIDesign aid = new Configuration.AIDesign(brain.AIDesign.ToProto(brain.currentStateContainerID));
								string key = fc.ShortPrefabName;
								if(args.Count()>0)key=args[0];
								if(config.AIDesigns.ContainsKey(key)){config.AIDesigns[key]=aid;}
								else{config.AIDesigns.Add(key,aid);}
								SaveConfig();
							}
						}catch(Exception e){SendChatMsg(bp, "<color=#FF0000>"+e.Message+" ");}		
					}
				}catch(Exception e){SendChatMsg(bp, "<color=#FF0000>"+e.Message+"</color>");}
				
				
			}
		}
		
		public Transform getLookingAtRaw(BasePlayer player){			
			RaycastHit hit;
			if (Physics.Raycast(player.eyes.HeadRay(), out hit)){
				var entity = hit.transform;
				return entity;
			}
			return null;
		}
		public BaseEntity getLookingAt(BasePlayer player){			
			RaycastHit hit;
			if (Physics.Raycast(player.eyes.HeadRay(), out hit)){
				var entity = hit.GetEntity();
				if (entity != null){if(entity.GetComponent<BaseEntity>()!=null) return entity.GetComponent<BaseEntity>();}
			}
			return null;
		}
		
		
	}
}

// Important Enums  

/* public enum AIEventType
{
	Timer,
	PlayerDetected,
	StateError,
	Attacked,
	StateFinished,
	InAttackRange,
	HealthBelow,
	InRange,
	PerformedAttack,
	TirednessAbove,
	HungerAbove,
	ThreatDetected,
	TargetDetected,
	AmmoBelow,
	BestTargetDetected,
	IsVisible,
	AttackTick,
	IsMounted,
	And,
	Chance,
	TargetLost,
	TimeSinceThreat,
	OnPositionMemorySet,
	AggressionTimer,
	Reloading,
	InRangeOfHome
}*/

/* public enum AIState
{
	None,
	Idle,
	Roam,
	Chase,
	Cover,
	Combat,
	Mounted,
	Exfil,
	Patrol,
	Orbit,
	Egress,
	Land,
	DropCrate,
	MoveTowards,
	Flee,
	Attack,
	Sleep,
	Reload,
	TakeCover,
	Dismounted,
	FollowPath,
	NavigateHome,
	CombatStationary,
	Cooldown,
	MoveToPoint
}*/