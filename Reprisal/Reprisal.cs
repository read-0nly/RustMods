
using Network;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using Rust;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System;
using Time = UnityEngine.Time;
using UnityEngine.UI;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Reprisal", "obsol", "0.0.2")]
    [Description("CH47+scientist event - if notorious enough, CH47 will investigate one of your auth'd TCs and drop a scouting party. If aggro'd on auth'd members it'll raze your building to the ground. After a while, CH47 leaves and scientists start patrolling the roads")]
    internal class Reprisal : CovalencePlugin
    {
		
		#region declaration
		public static Dictionary<BuildingPrivlidge, float> targets = new Dictionary<BuildingPrivlidge, float>();
		
		public ConfigData config;
		public static Reprisal _instance;

		public class LoadoutEntry{
			public string itemname = "";
			public ulong skinid = 0;
			public int amount=0;			
		}
		public class Loadout{
			public List<LoadoutEntry> entries = new List<LoadoutEntry>();
			public float chance = 0;
		}
		public class ConfigData
		{
			[JsonProperty("BeltLoadouts", ObjectCreationHandling = ObjectCreationHandling.Replace)]			
			public List<Loadout> Belts = new List<Loadout>();
			[JsonProperty("OutfitLoadouts", ObjectCreationHandling = ObjectCreationHandling.Replace)]			
			public List<Loadout> Outfits = new List<Loadout>();
			
			
			[JsonProperty("version", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public Oxide.Core.VersionNumber Version = default(VersionNumber);
		}
		#endregion
		
		#region load/unloads
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
			LoadoutEntry le = new LoadoutEntry();
			le.itemname="bow.hunting";
			le.amount=1;
			Loadout lo = new Loadout();
			lo.entries.Add(le);
			config.Belts.Add(lo);
			
			lo = new Loadout();
			le = new LoadoutEntry();
			le.itemname="hazmatsuittwitch";
			le.amount=1;
			lo.entries.Add(le);
			le = new LoadoutEntry();
			le.itemname="parachute";
			le.amount=1;
			lo.entries.Add(le);
			config.Outfits.Add(lo);
			
		}

		protected override void SaveConfig()
		{
			Config.WriteObject(config);
		}
		
		void OnServerSave(){
			Puts("Saving");
			SaveConfig();
		}
		void OnNewSave(string filename)
		{
			LoadDefaultConfig();
			SaveConfig();
			Puts("OnNewSave works!");
		}
		
		void Loaded(){
			_instance=this;
		}
		void OnServerInitialized(){
			_instance=this;
			
		}
		void Unload(){
			
		}
		#endregion
		
		#region event target logic
		void OnEntityDeath(BuildingPrivlidge entity, HitInfo hit)
		{
			if(targets.ContainsKey(entity)){
				targets.Remove(entity);
			}
		}
		void OnEntitySpawned(BuildingPrivlidge entity)
		{
			//BuildingPrivlidge
			
			if(!targets.ContainsKey(entity)){
				targets.Add(entity,1);
				CH47LandingZone lz = entity.gameObject.AddComponent<CH47LandingZone>();
				CH47DropZone dz = entity.gameObject.AddComponent<CH47DropZone>();
			}
		}
		
		#endregion
		

		private object OnEntityTakeDamage( BaseCombatEntity basePlayer, HitInfo hitInfo )
		{
			if(hitInfo.Initiator==null){
				return basePlayer;
			}
			return null;
		}
		public class RaidingNPC : global::ScientistNPC, global::IAISenses, global::IAIAttack, global::IThinker
		{
			
		}
		#region heli stuff
		[Command("SpawnCH47")]//
        private void SpawnCH47(IPlayer player, string cmd, string[] args)
        {
			Vector3 spawnpoint = new Vector3(0,1000,0);
			foreach(BuildingPrivlidge priv in targets.Keys){
				Puts(priv.transform.name);
			}
			ConVar.Entity.EntitySpawnRequest spawnEntityFromName = ConVar.Entity.GetSpawnEntityFromName("ch47scientists.entity");//
			global::CH47Helicopter baseEntity = (global::GameManager.server.CreateEntity(spawnEntityFromName.PrefabName, spawnpoint,Quaternion.AngleAxis(5.5f, Vector3.up), true) as CH47Helicopter);
			if(baseEntity==null){Puts("Nope");return;}		
			
			baseEntity.Spawn();
			CH47AIBrain chbrain = baseEntity.GetComponentInChildren<CH47AIBrain>();
			CH47HelicopterAIController chcontrol = baseEntity.GetComponentInChildren<CH47HelicopterAIController>();
		
			PathList road=null;
			int roadIdx=-1;
			foreach(BuildingPrivlidge priv in targets.Keys){
				chbrain.mainInterestPoint=priv.transform.position;
				ScientistScoutingStates.targetTC=priv;
				List<PathList> roads = TerrainMeta.Path.Roads;
				PathList best = null;
				float bestDistance = 0;
				int bestIdx = 0;
				foreach(PathList path in roads){
					int i = 0;
					foreach(Vector3 v in path.Path.Points){
						if(Vector3.Distance(baseEntity.transform.position, v)<bestDistance||best==null){
							best=path;
							bestDistance=Vector3.Distance(baseEntity.transform.position, v);
							bestIdx = i;
						}
						i++;
					}
				}
				road=best;
				roadIdx=bestIdx;
				chcontrol.SetMoveTarget(chbrain.mainInterestPoint);//
				break;
				//Set up the loop for future target selection logic but for now just grab the first one
			};
			chcontrol.TriggeredEventSpawn();
			chbrain.UseAIDesign=false;
			if(chbrain==null){Puts("No brain");return;}
			NextFrame(()=>{
				chbrain.states[AIState.Idle]=new CH47CustomStates.IdleState();
				chbrain.states[AIState.Land]=new CH47CustomStates.LandState();
				chbrain.states[AIState.Orbit]=new CH47CustomStates.OrbitState();
				chbrain.states[AIState.Patrol]=new CH47CustomStates.PatrolState();
				chbrain.states[AIState.Egress]=new CH47CustomStates.EgressState();
				foreach(AIState statetype in chbrain.states.Keys){
					BaseAIBrain.BasicAIState state=chbrain.states[statetype];
					if(state.brain==null){
						state.brain=chbrain;
					}
					
				}
				chbrain.CurrentState =null;
			});
			timer.Once(60f,()=>{				
				chcontrol.SetLandingTarget(road.Path.Points[roadIdx]);
			});
			timer.Once(2f,()=>{
				
				chbrain.SwitchToState(AIState.Patrol,0);
				
				
				foreach (global::BaseVehicle.MountPointInfo mountPointInfo in chcontrol.mountPoints)
				{
					if (mountPointInfo.mountable && mountPointInfo.mountable.AnyMounted())
					{
						HumanNPC bp = mountPointInfo.mountable._mounted as HumanNPC;
						//mountPointInfo.mountable.DismountAllPlayers();
						bp.Brain.SetEnabled(true);
						break;
					}
				}
				NextFrame(()=>{
					ScientistNPC leader = spawnMember(chcontrol.transform.position);
					
					leader.inventory.containerBelt.Clear();
					Item cassetteItem = ItemManager.CreateByName("rocket.launcher", 1, 0);
					leader.inventory.containerBelt.GiveItem(cassetteItem);
					leader.UpdateActiveItem(cassetteItem.uid);
					AttackEntity atEnt = cassetteItem.heldEntity.Get(true) as AttackEntity;
					if(atEnt!=null){
						atEnt.effectiveRange = atEnt.effectiveRange<10?10f:atEnt.effectiveRange;
					}
					chcontrol.AttemptMount(leader, false);
					for(int i = 0; i < 4; i++){
						ScientistNPC member = spawnMember(chcontrol.transform.position, leader);
						chcontrol.AttemptMount(member, false);
					}
				});
			});
			timer.Every(5f,()=>{
				Puts("Heli state:"+chbrain.CurrentState.ToString() + " ["+chbrain.transform.position+"]");
			});
		}
		#endregion
		
		#region scientist stuff
		[Command("SpawnRoamer")]
        private void SpawnRoamer(IPlayer player, string cmd, string[] args)
        {
            BasePlayer basePlayer = player.Object as BasePlayer;
			if(basePlayer==null){
				return;
			}
			ScientistNPC leader = spawnMember(basePlayer);
			for(int i = 0; i < UnityEngine.Random.Range(0,4);i++){
				spawnMember(basePlayer,leader);
			}
				
		}
		public ScientistNPC spawnMember(BasePlayer basePlayer, ScientistNPC owner = null){
			Vector3 ius = (UnityEngine.Random.insideUnitSphere);
			ius.y=0;
			ius.Normalize();
			return spawnMember(getLookingAtPoint(basePlayer)+(ius*5),owner);
		}
		
		public void setupRaidBrain(HumanNPC npc){
			
				npc.Brain.Navigator.MaxRoamDistanceFromHome =-1;
				npc.Brain.states[AIState.FollowPath]=new ScientistRaidingStates.BaseRoamState();
				npc.Brain.states[AIState.TakeCover]=new ScientistRaidingStates.TakeCoverState();
				npc.Brain.states[AIState.Chase]=new ScientistRaidingStates.BaseChaseState();
				npc.Brain.states[AIState.Combat]=new ScientistRaidingStates.CombatState();
				npc.Brain.SwitchToState(AIState.FollowPath,0);
				
				npc.Brain.AIDesign.GetFirstStateContainerOfType(AIState.Combat).Events[4].TriggerStateContainerID=4;
				npc.Brain.AIDesign.GetFirstStateContainerOfType(AIState.Combat).Events[6].Inverted=false;
				npc.Brain.AIDesign.GetFirstStateContainerOfType(AIState.Combat).Events[6].TriggerStateContainerID=2;
				foreach(BaseAIEvent evt in npc.Brain.AIDesign.GetFirstStateContainerOfType(AIState.Chase).Events){
					Puts(""+evt.EventType + " : " + evt.ID);
				}
				npc.Brain.Senses.targetLostRange=100f;//
				if(npc.creatorEntity!=null && (npc.creatorEntity is ScientistNPC)){
					npc.Brain.Senses.Memory.Players=(npc.creatorEntity as ScientistNPC).Brain.Senses.Memory.Players;
				}
				npc.inventory.containerBelt.Clear();
				Item cassetteItem = ItemManager.CreateByName("rocket.launcher", 1, 0);
				npc.inventory.containerBelt.GiveItem(cassetteItem);
				npc.UpdateActiveItem(cassetteItem.uid);
				AttackEntity atEnt = cassetteItem.heldEntity.Get(true) as AttackEntity;
				if(atEnt!=null){
					atEnt.effectiveRange = atEnt.effectiveRange<10?10f:atEnt.effectiveRange;
				}
				npc.inventory.containerWear.Clear();
				foreach(LoadoutEntry le in config.Outfits[0].entries){
					Item cassetteItem2 = ItemManager.CreateByName(le.itemname, le.amount, le.skinid);
					npc.inventory.containerWear.GiveItem(cassetteItem2);
				}
				Puts("Creator: "+npc.creatorEntity);
		}
		
		public void setupScoutBrain(HumanNPC npc){
			
				npc.Brain.Navigator.MaxRoamDistanceFromHome =-1;
				npc.Brain.states[AIState.FollowPath]=new ScientistScoutingStates.BaseRoamState();
				npc.Brain.states[AIState.TakeCover]=new ScientistScoutingStates.TakeCoverState();
				npc.Brain.states[AIState.Chase]=new ScientistScoutingStates.BaseChaseState();
				npc.Brain.states[AIState.Combat]=new ScientistScoutingStates.CombatState();
				npc.Brain.SwitchToState(AIState.FollowPath,0);
				
				npc.Brain.AIDesign.GetFirstStateContainerOfType(AIState.Combat).Events[4].TriggerStateContainerID=4;
				npc.Brain.AIDesign.GetFirstStateContainerOfType(AIState.Combat).Events[6].Inverted=false;
				npc.Brain.AIDesign.GetFirstStateContainerOfType(AIState.Combat).Events[6].TriggerStateContainerID=2;
				foreach(BaseAIEvent evt in npc.Brain.AIDesign.GetFirstStateContainerOfType(AIState.Chase).Events){
					Puts(""+evt.EventType + " : " + evt.ID);
				}
				npc.Brain.Senses.targetLostRange=100f;//
				if(npc.creatorEntity!=null && (npc.creatorEntity is ScientistNPC)){
					npc.Brain.Senses.Memory.Players=(npc.creatorEntity as ScientistNPC).Brain.Senses.Memory.Players;
				}
				npc.inventory.containerBelt.Clear();
				foreach(LoadoutEntry le in config.Belts[0].entries){
					Item cassetteItem = ItemManager.CreateByName(le.itemname, le.amount, le.skinid);
					npc.inventory.containerBelt.GiveItem(cassetteItem);
					npc.UpdateActiveItem(cassetteItem.uid);
					AttackEntity atEnt = cassetteItem.heldEntity.Get(true) as AttackEntity;
					if(atEnt!=null){
						atEnt.effectiveRange = atEnt.effectiveRange<10?10f:atEnt.effectiveRange;
					}
				}
				npc.inventory.containerWear.Clear();
				foreach(LoadoutEntry le in config.Outfits[0].entries){
					Item cassetteItem = ItemManager.CreateByName(le.itemname, le.amount, le.skinid);
					npc.inventory.containerWear.GiveItem(cassetteItem);
				}
				npc.Brain.Senses.Memory = new Rust.Ai.SimpleAIMemory();
				Puts("Creator: "+npc.creatorEntity);
		}
		
		public ScientistNPC spawnSquad(){
			return null;
		}//
		public ScientistNPC spawnMember(Vector3 position, ScientistNPC owner = null){
			
			ConVar.Entity.EntitySpawnRequest spawnEntityFromName = ConVar.Entity.GetSpawnEntityFromName("scientistnpc_junkpile");//
			
			global::ScientistNPC baseEntity = (global::GameManager.server.CreateEntity(spawnEntityFromName.PrefabName, position,Quaternion.AngleAxis(5.5f, Vector3.up), true) as ScientistNPC);
			if(baseEntity==null){Puts("Nope");return null;}		
			
			baseEntity.Spawn();
			//baseEntity.Load(loadInfo);
			
			baseEntity.creatorEntity=owner;
			if(owner!=null){
				timer.Once(0.2f,()=>{
					setupScoutBrain(baseEntity);
					timer.Once(1810,()=>{
						ScientistPatrolStates.setupRoamBrain(baseEntity);
					});
				});
			}else{
				timer.Once(0.1f,()=>{
					setupRaidBrain(baseEntity);
					baseEntity.Brain.Senses.Memory = new Rust.Ai.SimpleAIMemory();
					timer.Once(1800,()=>{
						baseEntity.Brain.Senses.Memory = new Rust.Ai.SimpleAIMemory();
						ScientistPatrolStates.setupRoamBrain(baseEntity);
					});
				});
			}
			return baseEntity;
		}
		
		
		void OnEntitySpawned(JunkPile entity)
		{
			if(entity.gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>()!=null){return;}
			UnityEngine.AI.NavMeshObstacle nmo = entity.gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>();
			nmo.enabled = false;
			nmo.carving = true;
			nmo.shape= UnityEngine.AI.NavMeshObstacleShape.Capsule;
			nmo.height=20;
			nmo.radius=10;
			nmo.size=new Vector3(7,7,7);
			nmo.enabled = true;
			//Puts("OnEntitySpawned works!");
		}
		#endregion
		
		#region navmesh manip
		void OnEntitySpawned(ResourceEntity entity)
		{
			try{
				if(entity.gameObject.GetComponent<UnityEngine.AI.NavMeshObstacle>()!=null){return;}
				UnityEngine.AI.NavMeshObstacle nmo = entity.gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>();
				nmo.enabled = false;
				nmo.carving = true;
				nmo.shape= UnityEngine.AI.NavMeshObstacleShape.Capsule;
				nmo.height=20;
				nmo.radius=3;
				nmo.size=new Vector3(3,7,3);
				nmo.enabled = true;
				//Puts("OnEntitySpawned works!");
			}		
			catch(Exception e){}
		}
		
		#endregion
		
		#region utlity
		public Vector3 getLookingAtPoint(BasePlayer player){			
			RaycastHit hit;
			if (Physics.Raycast(player.eyes.HeadRay(), out hit)){
				Vector3 entity = hit.point;
				return entity;
			}
			return new Vector3(0,0,0);
		}
		
		#endregion
		
		#region CH47 States
		class CH47CustomStates{
					
		// Token: 0x0200082B RID: 2091
		public class EgressState : global::BaseAIBrain.BasicAIState
		{
			// Token: 0x0600427E RID: 17022 RVA: 0x0017E0FC File Offset: 0x0017C2FC
			public EgressState()
				: base(global::AIState.Egress)
			{
			}

			// Token: 0x0600427F RID: 17023 RVA: 0x0017E108 File Offset: 0x0017C308
			public override bool CanInterrupt()
			{
				return false;
			}

			// Token: 0x06004280 RID: 17024 RVA: 0x0017E10C File Offset: 0x0017C30C
			public override float GetWeight()
			{
				global::CH47HelicopterAIController ch47HelicopterAIController = this.brain.GetBrainBaseEntity() as global::CH47HelicopterAIController;
				global::CH47AIBrain component = this.brain.GetComponent<global::CH47AIBrain>();
				Console.ForegroundColor = ConsoleColor.Magenta;
				if (!(component != null))
				{
					//Console.WriteLine("Nullweight");
					Console.ResetColor();
					return 0f;
				}
				if (component.Age <= 1800f)
				{
					//Console.WriteLine("Under max age");
					Console.ResetColor();
					return 0f;
				}
				//Console.WriteLine("Egress "+component.Age);
				Console.ResetColor();
				return 10000f;
			}

			// Token: 0x06004281 RID: 17025 RVA: 0x0017E174 File Offset: 0x0017C374
			public override void StateEnter(global::BaseAIBrain brain, global::BaseEntity entity)
			{
				global::CH47HelicopterAIController ch47HelicopterAIController = entity as global::CH47HelicopterAIController;
				ch47HelicopterAIController.EnableFacingOverride(false);
				Transform transform = ch47HelicopterAIController.transform;
				Rigidbody rigidBody = ch47HelicopterAIController.rigidBody;
				Vector3 vector = ((rigidBody.velocity.magnitude < 0.1f) ? transform.forward : rigidBody.velocity.normalized);
				Vector3 vector2 = Vector3.Cross(Vector3.Cross(transform.up, vector), Vector3.up);
				brain.mainInterestPoint = transform.position + vector2 * 8000f;
				brain.mainInterestPoint.y = 100f;
				ch47HelicopterAIController.SetMoveTarget(brain.mainInterestPoint);
				base.StateEnter(brain, entity);
			}

			// Token: 0x06004282 RID: 17026 RVA: 0x0017E220 File Offset: 0x0017C420
			public override global::StateStatus StateThink(float delta, global::BaseAIBrain brain, global::BaseEntity entity)
			{
				base.StateThink(delta, brain, entity);
				if (this.killing)
				{
					return global::StateStatus.Running;
				}
				global::CH47HelicopterAIController ch47HelicopterAIController = entity as global::CH47HelicopterAIController;
				Vector3 position = ch47HelicopterAIController.transform.position;
				if (position.y < 85f && !this.egressAltitueAchieved)
				{
					global::CH47LandingZone closest = global::CH47LandingZone.GetClosest(position);
					if (closest != null && Vector3Ex.Distance2D(closest.transform.position, position) < 20f)
					{
						float num = global::WaterLevel.GetWaterOrTerrainSurface(position, false, false, null);
						num += 100f;
						Vector3 vector = position;
						vector.y = num;
						ch47HelicopterAIController.SetMoveTarget(vector);
						return global::StateStatus.Running;
					}
				}
				this.egressAltitueAchieved = true;
				ch47HelicopterAIController.SetMoveTarget(brain.mainInterestPoint);
				if (base.TimeInState > 300f)
				{
					SingletonComponent<InvokeHandler>.Instance.Invoke(new Action(ch47HelicopterAIController.DelayedKill), 2f);
					this.killing = true;
				}
				return global::StateStatus.Running;
			}

			// Token: 0x04003529 RID: 13609
			private bool killing;

			// Token: 0x0400352A RID: 13610
			private bool egressAltitueAchieved;
		}
			// Token: 0x0200082C RID: 2092
			public class IdleState : global::BaseAIBrain.BaseIdleState
			{
				// Token: 0x06004283 RID: 17027 RVA: 0x0017E2FC File Offset: 0x0017C4FC
				public override float GetWeight()
				{
					return 0.1f;
				}

				// Token: 0x06004284 RID: 17028 RVA: 0x0017E304 File Offset: 0x0017C504
				public override void StateEnter(global::BaseAIBrain brain, global::BaseEntity entity)
				{
					global::CH47HelicopterAIController ch47HelicopterAIController = entity as global::CH47HelicopterAIController;
					ch47HelicopterAIController.SetMoveTarget(ch47HelicopterAIController.GetPosition() + ch47HelicopterAIController.rigidBody.velocity.normalized * 10f);
					base.StateEnter(brain, entity);
				}
			}

			// Token: 0x0200082D RID: 2093
			public class LandState : global::BaseAIBrain.BasicAIState
			{
				Vector3 interestPoint=new Vector3(0,0,0);
				List<global::BaseVehicle.MountPointInfo> mountPoints = new List<global::BaseVehicle.MountPointInfo>();
				// Token: 0x06004286 RID: 17030 RVA: 0x0017E358 File Offset: 0x0017C558
				public LandState()
					: base(global::AIState.Land)
				{
				}
				Vector3 lastHeight = new Vector3();
				Vector3 lastLastHeight = new Vector3();
				// Token: 0x06004287 RID: 17031 RVA: 0x0017E370 File Offset: 0x0017C570
				public override float GetWeight()
				{
					if (!(this.brain.GetBrainBaseEntity() as global::CH47HelicopterAIController).ShouldLand())
					{
						return 0f;
					}
					float num = Time.time - this.lastLandtime;
					if (base.IsInState() && this.landedForSeconds < 12f)
					{
						return 1000f;
					}
					if (!base.IsInState() && num > 10f)
					{
						return 9000f;
					}
					return 0f;
				}
				
				// Token: 0x06004288 RID: 17032 RVA: 0x0017E3E0 File Offset: 0x0017C5E0
				public override global::StateStatus StateThink(float delta, global::BaseAIBrain brain, global::BaseEntity entity)
				{
					base.StateThink(delta, brain, entity);
					brain.mainInterestPoint =interestPoint+new Vector3(0,20f,0);
					global::CH47HelicopterAIController ch47HelicopterAIController = entity as global::CH47HelicopterAIController;
					Vector3 position = ch47HelicopterAIController.transform.position;
					Vector3 forward = ch47HelicopterAIController.transform.forward;
					/*global::CH47LandingZone closest = global::CH47LandingZone.GetClosest(ch47HelicopterAIController.landingTarget);
					if (!closest)
					{
						return global::StateStatus.Error;
					}*/
					float magnitude = ch47HelicopterAIController.rigidBody.velocity.magnitude;
					float num = Vector3Ex.Distance2D(brain.mainInterestPoint, position);
					bool flag = num < 40f;
					bool flag2 = num > 15f && position.y < brain.mainInterestPoint.y + 10f;
					ch47HelicopterAIController.EnableFacingOverride(flag);
					ch47HelicopterAIController.SetAltitudeProtection(flag2);
					bool flag3 = (Mathf.Abs(brain.mainInterestPoint.y - position.y) < 30f && num <= 50f && magnitude < 1f);
					if (flag3)
					{
						this.landedForSeconds += delta;
						if (this.lastLandtime == 0f)
						{
							this.lastLandtime = Time.time;
						}
					}
					float num2 = 1f - Mathf.InverseLerp(0f, 7f, num);
					this.landingHeight -= 4f * num2 * Time.deltaTime;
					if (this.landingHeight < -5f)
					{
						this.landingHeight = -5f;
					}
					ch47HelicopterAIController.SetAimDirection(entity.transform.forward);
					Vector3 vector = brain.mainInterestPoint + new Vector3(0f, this.landingHeight, 0f);
					
					if (num < 100f && num > 15f)
					{
						Vector3 vector2 = Vector3Ex.Direction2D(brain.mainInterestPoint, position);
						RaycastHit raycastHit;
						if (Physics.SphereCast(position, 15f, vector2, out raycastHit, num, 1218511105))
						{
							Vector3 vector3 = Vector3.Cross(vector2, Vector3.up);
							vector = raycastHit.point + vector3 ;
						}
						lastLastHeight=lastHeight;
						lastHeight=vector;
						
					}
					ch47HelicopterAIController.SetMoveTarget(vector);
					if (flag3)
					{
						if (this.landedForSeconds > 1f && Time.time > this.nextDismountTime)
						{
							if(mountPoints.Count()>0){
								Console.ForegroundColor=ConsoleColor.Yellow;
								Console.WriteLine("Boot Mounted");
								Console.ResetColor();
								global::BaseVehicle.MountPointInfo mountPointInfo = mountPoints[0];
								mountPoints.RemoveAt(0);
								if (mountPointInfo.mountable && mountPointInfo.mountable.AnyMounted())
								{
									BasePlayer bp = mountPointInfo.mountable._mounted;
									foreach(Item slot in bp.inventory.containerWear.itemList){
										
										Console.ForegroundColor=ConsoleColor.Yellow;
										Console.WriteLine("  Checking "+slot.name);
										Console.ResetColor();
										global::ItemModParachute itemModParachute=null;
										if(slot!=null&&slot.info!=null){
											slot.info.TryGetComponent<global::ItemModParachute>(out itemModParachute);
										}
										if (itemModParachute!=null)
										{
											Console.ForegroundColor=ConsoleColor.Magenta;
											Console.WriteLine("  Parachute ");
											Console.ResetColor();
											mountPointInfo.mountable.DismountAllPlayers();
											Vector3 direction = -entity.transform.forward;
											direction.y=0;
											direction.Normalize();
											bp.transform.position+=new Vector3(0,-5,0)+(direction*10);
											global::Parachute parachute = global::GameManager.server.CreateEntity(itemModParachute.ParachuteVehiclePrefab.resourcePath, bp.transform.position, Quaternion.LookRotation(direction), true) as global::Parachute;
											if (parachute != null)
											{
												
												Console.ForegroundColor=ConsoleColor.Green;
												Console.WriteLine("  Booted?");
												Console.ResetColor();
												parachute.skinID = slot.skin;
												parachute.Spawn();
												parachute.SetHealth(parachute.MaxHealth() * slot.conditionNormalized);
												//parachute.collisionDeath=true;
												parachute.AttemptMount(bp, false);
												
												slot.Remove(0f);
												global::ItemManager.DoRemoves();
												bp.SendNetworkUpdate(global::BasePlayer.NetworkQueue.Update);
												if (!bp.isMounted)
												{
													Console.ForegroundColor=ConsoleColor.Red;
													Console.WriteLine("  Not Booted");
													Console.ResetColor();
													parachute.Kill(global::BaseNetworkable.DestroyMode.None);
													bp.Kill();
													break;
												}
												InputState state = new InputState();
												state.current.buttons=(int)(BUTTON.FORWARD | BUTTON.RIGHT);
												parachute.PlayerServerInput(state,bp);
												this.nextDismountTime = Time.time + 2f;
												break;
											}
										}
									}
								}
							}
						}
						if (this.landedForSeconds > 8f)
						{
							return global::StateStatus.Finished;
							brain.GetComponent<global::CH47AIBrain>().ForceSetAge(float.PositiveInfinity);
						}
					}
					Console.ForegroundColor = ConsoleColor.Cyan;
					//Console.WriteLine("TryLand");
					Console.ResetColor();
					return global::StateStatus.Running;
				}

				// Token: 0x06004289 RID: 17033 RVA: 0x0017E6A0 File Offset: 0x0017C8A0
				public override void StateEnter(global::BaseAIBrain brain, global::BaseEntity entity)
				{
					this.landingHeight = 5f;
					global::CH47HelicopterAIController ch47HelicopterAIController = entity as global::CH47HelicopterAIController;
					if(mountPoints.Count()==0){
						mountPoints = new List<global::BaseVehicle.MountPointInfo>(ch47HelicopterAIController.mountPoints.ToArray());
					}
					base.StateEnter(brain, entity);
					//	brain.mainInterestPoint = (entity as global::CH47HelicopterAIController).landingTarget;
					if(interestPoint==Vector3.zero){
						float best=-1;
						Vector3 bestPoint = new Vector3(0,0,0);
						foreach(PathList path in TerrainMeta.Path.Roads){
							foreach(Vector3 point in path.Path.Points){
								//Set up the loop for future target selection logic but for now just grab the first one
								if(Vector3.Distance(entity.transform.position,point)<best||best==-1){
									best=Vector3.Distance(entity.transform.position,point);
									bestPoint=point;
								}
							}
						}
						interestPoint=bestPoint;
						brain.mainInterestPoint =interestPoint;
					}
				}

				// Token: 0x0600428A RID: 17034 RVA: 0x0017E6C8 File Offset: 0x0017C8C8
				public override void StateLeave(global::BaseAIBrain brain, global::BaseEntity entity)
				{
					global::CH47HelicopterAIController ch47HelicopterAIController = entity as global::CH47HelicopterAIController;
					ch47HelicopterAIController.EnableFacingOverride(false);
					ch47HelicopterAIController.SetAltitudeProtection(true);
					ch47HelicopterAIController.SetMinHoverHeight(30f);
					this.landedForSeconds = 0f;
					this.lastLandtime = Time.time+99999999999999999f;
					base.StateLeave(brain, entity);
				}

				// Token: 0x0600428B RID: 17035 RVA: 0x0017E6FC File Offset: 0x0017C8FC
				public override bool CanInterrupt()
				{
					return true;
				}

				// Token: 0x0400352B RID: 13611
				private float landedForSeconds;

				// Token: 0x0400352C RID: 13612
				private float lastLandtime;

				// Token: 0x0400352D RID: 13613
				private float landingHeight = 20f;

				// Token: 0x0400352E RID: 13614
				private float nextDismountTime;
			}

			// Should be ready to test
			public class OrbitState : global::BaseAIBrain.BasicAIState
			{
				// Token: 0x0600428C RID: 17036 RVA: 0x0017E700 File Offset: 0x0017C900
				public OrbitState()
					: base(global::AIState.Orbit)
				{
				}

				// Token: 0x0600428D RID: 17037 RVA: 0x0017E70C File Offset: 0x0017C90C
				public Vector3 GetOrbitCenter()
				{
					return this.brain.mainInterestPoint;
				}

				// Token: 0x0600428E RID: 17038 RVA: 0x0017E71C File Offset: 0x0017C91C
				public override float GetWeight()
				{
					Console.ForegroundColor = ConsoleColor.Green;
					if (base.IsInState())
					{
						float num = 1f - Mathf.InverseLerp(120f, 180f, base.TimeInState);
						//Console.WriteLine("IsInState" + num);
						Console.ResetColor();
						return 5f * num;
					}
					if (this.brain.CurrentState != null && this.brain.CurrentState.StateType == global::AIState.Patrol)
					{
						CH47CustomStates.PatrolState patrolState = this.brain.CurrentState as CH47CustomStates.PatrolState;
						if (patrolState != null && patrolState.AtPatrolDestination())
						{
							//Console.WriteLine("Patrol done");
							Console.ResetColor();
							return 20f;
						}
					}
					CH47HelicopterAIController ch47HelicopterAIController = this.brain.GetBrainBaseEntity() as global::CH47HelicopterAIController;
					if(!ch47HelicopterAIController.ShouldLand())
					{
						//Console.WriteLine("Done landing");
						Console.ResetColor();
						return 20f;
					}
					return 0f;
				}

				// Token: 0x0600428F RID: 17039 RVA: 0x0017E79C File Offset: 0x0017C99C
				public override void StateEnter(global::BaseAIBrain brain, global::BaseEntity entity)
				{
					global::CH47HelicopterAIController ch47HelicopterAIController = entity as global::CH47HelicopterAIController;
					ch47HelicopterAIController.EnableFacingOverride(true);
					ch47HelicopterAIController.InitiateAnger();
					base.StateEnter(brain, entity);
				}

				// Token: 0x06004290 RID: 17040 RVA: 0x0017E7B8 File Offset: 0x0017C9B8
				public override global::StateStatus StateThink(float delta, global::BaseAIBrain brain, global::BaseEntity entity)
				{
					//Vector3 orbitCenter = this.GetOrbitCenter();
					Vector3 orbitCenter = new Vector3(0,0,0);
					if(targets.Count()>0){
						
					}
					foreach(BuildingPrivlidge priv in targets.Keys){
						orbitCenter=priv.transform.position;
						break;
						//Set up the loop for future target selection logic but for now just grab the first one
					}
					
					
					global::CH47HelicopterAIController ch47HelicopterAIController = entity as global::CH47HelicopterAIController;
					Vector3 position = ch47HelicopterAIController.GetPosition();
					Vector3 vector = Vector3Ex.Direction2D(orbitCenter, position);
					Vector3 vector2 = Vector3.Cross(Vector3.up, vector);
					float num = ((Vector3.Dot(Vector3.Cross(ch47HelicopterAIController.transform.right, Vector3.up), vector2) < 0f) ? (-1f) : 1f);
					float num2 = 75f;
					Vector3 normalized = (-vector + vector2 * num * 0.6f).normalized;
					Vector3 vector3 = orbitCenter + normalized * num2;
					ch47HelicopterAIController.SetMoveTarget(vector3);
					ch47HelicopterAIController.SetAimDirection(Vector3Ex.Direction2D(vector3, position));
					base.StateThink(delta, brain, entity);
					Console.ForegroundColor = ConsoleColor.Cyan;
					//Console.WriteLine("TryOrbit");
					Console.ResetColor();
					return global::StateStatus.Running;
				}

				// Token: 0x06004291 RID: 17041 RVA: 0x0017E880 File Offset: 0x0017CA80
				public override void StateLeave(global::BaseAIBrain brain, global::BaseEntity entity)
				{
					global::CH47HelicopterAIController ch47HelicopterAIController = entity as global::CH47HelicopterAIController;
					ch47HelicopterAIController.EnableFacingOverride(false);
					ch47HelicopterAIController.CancelAnger();
					base.StateLeave(brain, entity);
				}
			}

			// Should be ready to test
			public class PatrolState : global::BaseAIBrain.BasePatrolState
			{
				// Token: 0x06004292 RID: 17042 RVA: 0x0017E89C File Offset: 0x0017CA9C
				public override void StateEnter(global::BaseAIBrain brain, global::BaseEntity entity)
				{
					if(this.brain==null){
						this.brain=brain;
						
					}
					base.StateEnter(brain, entity);
					foreach(BuildingPrivlidge priv in targets.Keys){
						brain.mainInterestPoint =priv.transform.position;
						break;
						//Set up the loop for future target selection logic but for now just grab the first one
					}
				}

				// Token: 0x06004293 RID: 17043 RVA: 0x0017E8B8 File Offset: 0x0017CAB8
				public override global::StateStatus StateThink(float delta, global::BaseAIBrain brain, global::BaseEntity entity)
				{
					base.StateThink(delta, brain, entity);
					(entity as global::CH47HelicopterAIController).SetMoveTarget(brain.mainInterestPoint);
					Console.ForegroundColor = ConsoleColor.Cyan;
					//Console.WriteLine("TryPatrol");
					Console.ResetColor();
					return global::StateStatus.Running;
				}
				bool hasReached = false;
				// Token: 0x06004294 RID: 17044 RVA: 0x0017E8D8 File Offset: 0x0017CAD8
				public bool AtPatrolDestination()
				{
					hasReached = hasReached | Vector3Ex.Distance2D(this.GetDestination(), this.brain.transform.position) < this.patrolApproachDist;
					return hasReached;
				}

				// Token: 0x06004295 RID: 17045 RVA: 0x0017E900 File Offset: 0x0017CB00
				public Vector3 GetDestination()
				{
					return this.brain.mainInterestPoint;
				}

				// Token: 0x06004296 RID: 17046 RVA: 0x0017E910 File Offset: 0x0017CB10
				public override bool CanInterrupt()
				{
					return base.CanInterrupt() && this.AtPatrolDestination();
				}

				// Token: 0x06004297 RID: 17047 RVA: 0x0017E924 File Offset: 0x0017CB24
				public override float GetWeight()
				{
					if (!base.IsInState())
					{
						float num = Mathf.InverseLerp(70f, 120f, base.TimeSinceState()) * 5f;
						return 1f + num;
					}
					if (this.AtPatrolDestination() && base.TimeInState > 2f)
					{
						return 0f;
					}
					return 30f;
				}

				// Token: 0x0400352F RID: 13615
				protected float patrolApproachDist = 75f;
			}
		}
		#endregion
		
		void OnEntitySpawned(BaseLauncher baseProjectile)
		{
			if(baseProjectile && baseProjectile.MuzzlePoint){
				baseProjectile.MuzzlePoint.position+=baseProjectile.MuzzlePoint.InverseTransformPoint((Vector3.down*1.5f)-(Vector3.left*0.5f)-(Vector3.forward*2));
				baseProjectile.MuzzlePoint.forward= baseProjectile.MuzzlePoint.InverseTransformDirection(/*forward*/(Vector3.down*2)/*left*/+(Vector3.left*1.72f)/*up*/-(Vector3.forward*0.15f));// Fixes muzzlepoint for rocket launcher as it was never set up by facepunch 
				
			}
		}
		#region Base Raiding
		
		class ScientistRaidingStates{
			//if explosive weapon, swap. If target lost, dig towards TCpublic static bool DoFall(float delta, global::BaseAIBrain brain, global::BaseEntity entity,bool mounted){
			
			public static BaseEntity FindBuildBlocks(BaseEntity entity, BaseAIBrain brain,BaseEntity target ){
					List<BuildingBlock> piles = new List<BuildingBlock>();
					BaseEntity.Query.Server.GetInSphere(entity.transform.position, 30f, piles);
					if(piles!=null&&piles.Count()>0){
						foreach(BuildingBlock ore in piles){
							if(ore!=null && entity.IsVisible(ore.CenterPoint(), entity.CenterPoint(), float.PositiveInfinity)){
								target=ore;
								brain.Senses.Memory.SetLOS(ore,true);
								brain.Senses.Players.Add(ore);
							}
						}
					}
					return target;
			}
			// Token: 0x0200006A RID: 106
			public class BaseChaseState : global::BaseAIBrain.BasicAIState
			{
				// Token: 0x06000D3C RID: 3388 RVA: 0x00046CB0 File Offset: 0x00044EB0
				public BaseChaseState()
					: base(global::AIState.Chase)
				{
					base.AgrresiveState = true;
				}

				bool mounted = false;
				// Token: 0x06000D3D RID: 3389 RVA: 0x00046CC0 File Offset: 0x00044EC0
				public override void StateEnter(global::BaseAIBrain brain, global::BaseEntity baseentity)
				{
					base.StateEnter(brain, baseentity);
					if(this.brain==null){
						this.brain=brain;
					}
					if((baseentity as ScientistNPC).GetMounted()!=null){
						mounted=true;
						return;
					}
					if(!(baseentity is ScientistNPC)){return;}
					ScientistNPC entity = (baseentity as ScientistNPC);
					BaseEntity coverFromEntityAsEntity = this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
					BasePlayer coverFromEntity = coverFromEntityAsEntity as BasePlayer;
					Vector3 vector = (coverFromEntityAsEntity ? coverFromEntityAsEntity.transform.position : (entity.transform.position + entity.LastAttackedDir * 10f));
					
					/*
					global::AIInformationZone informationZone = entity.GetInformationZone(entity.transform.position);
					if (informationZone == null)
					{
						return false;
					}*/
					float num = ((entity.SecondsSinceAttacked < 2f) ? 2f : 0f);
					float bestCoverPointMaxDistance = this.brain.Navigator.BestCoverPointMaxDistance;
					List<ResourceEntity> piles = new List<ResourceEntity>();
					BaseEntity.Query.Server.GetInSphere(vector, 50, piles);
					
					
					Vector3 insideUnitSphere = global::UnityEngine.Random.insideUnitSphere;
					insideUnitSphere-=entity.eyes.HeadForward();
					insideUnitSphere.y = 0f;
					insideUnitSphere.Normalize();
					
					Vector3 target = vector+(insideUnitSphere*30);
					float bestScore = 10;
					Vector3 bestTarget = new Vector3(0,0,0);
					Vector3 coverVector =  (coverFromEntity!=null?new Vector3(0,0,0)-coverFromEntity.eyes.HeadForward():entity.eyes.HeadForward());
					coverVector.y=0;
					coverVector.Normalize();
					if(piles.Count()>0){
						foreach(ResourceEntity pile in piles){
							float dot = Vector3.Distance(vector, pile.transform.position);
							if(dot<bestScore){
								bestScore=dot;
								bestTarget = pile.transform.position;
								coverVector = (vector-pile.transform.position-vector).normalized;
							}
						}
					}
					if(bestScore!=10){
						target=bestTarget+((coverVector+insideUnitSphere*10)*1);
					}else{
						
					}
					//Console.WriteLine("Found target:" + Vector3.Distance(vector,target));
					
					
					if (!this.brain.Navigator.SetDestination(target, global::BaseNavigator.NavigationSpeed.Fast, 0f, 50f))
					{
						Console.WriteLine("Can't chase " + target);
						this.Stop();
					}
				}

				// Token: 0x06000D3E RID: 3390 RVA: 0x00046D24 File Offset: 0x00044F24
				public override void StateLeave(global::BaseAIBrain brain, global::BaseEntity entity)
				{
					base.StateLeave(brain, entity);
					//this.Stop();
				}

				// Token: 0x06000D3F RID: 3391 RVA: 0x00046D34 File Offset: 0x00044F34
				private void Stop()
				{
					this.brain.Navigator.Stop();
				}

				// Token: 0x06000D40 RID: 3392 RVA: 0x00046D48 File Offset: 0x00044F48
				public override global::StateStatus StateThink(float delta, global::BaseAIBrain brain, global::BaseEntity entity)
				{
					base.StateThink(delta, brain, entity);
					
					mounted = ScientistPatrolStates.DoFall(delta, brain, entity,mounted);
					if(mounted){
						return global::StateStatus.Finished;
					}
					global::BaseEntity baseEntity = brain.Events.Memory.Entity.Get(brain.Events.CurrentInputMemorySlot);
					if (baseEntity == null)
					{
						this.Stop();
						return global::StateStatus.Finished;
					}
					if (!brain.Navigator.Moving)
					{
						return global::StateStatus.Finished;
					}
					return global::StateStatus.Running;
				}
			}
			public class CombatState : global::BaseAIBrain.BasicAIState
			{
				// Token: 0x060031B2 RID: 12722 RVA: 0x00132300 File Offset: 0x00130500
				public CombatState()
					: base(global::AIState.Combat)
				{
					base.AgrresiveState = true;
				}

				// Token: 0x060031B3 RID: 12723 RVA: 0x00132310 File Offset: 0x00130510
				public override void StateEnter(global::BaseAIBrain brain, global::BaseEntity entity)
				{
					base.StateEnter(brain, entity);
					this.brain=brain;
					this.combatStartPosition = entity.transform.position;
					BaseEntity target=this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
					target=FindBuildBlocks(entity, brain, target);
					if(target!=null){
						this.FaceTarget(target);
					}
				}

				// Token: 0x060031B4 RID: 12724 RVA: 0x00132334 File Offset: 0x00130534
				public override void StateLeave(global::BaseAIBrain brain, global::BaseEntity entity)
				{
					base.StateLeave(brain, entity);
					(entity as global::HumanNPC).SetDucked(false);
					brain.Navigator.ClearFacingDirectionOverride();
				}

				// Token: 0x060031B5 RID: 12725 RVA: 0x00132358 File Offset: 0x00130558
				public override global::StateStatus StateThink(float delta, global::BaseAIBrain brain, global::BaseEntity entity)
				{
					base.StateThink(delta, brain, entity);
						
					
					global::HumanNPC humanNPC = entity as global::HumanNPC;
					BaseEntity target = this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
					if(target!=null){
						this.FaceTarget(target);
					}
					if (Time.time > this.nextActionTime)
					{
						if (global::UnityEngine.Random.Range(0, 3) == 1)
						{
							this.nextActionTime = Time.time + global::UnityEngine.Random.Range(1f, 2f);
							humanNPC.SetDucked(true);
							brain.Navigator.Stop();
						}
						else
						{
							this.nextActionTime = Time.time + global::UnityEngine.Random.Range(2f, 3f);
							humanNPC.SetDucked(false);
							brain.Navigator.SetDestination(brain.PathFinder.GetRandomPositionAround(this.combatStartPosition, 1f, 2f), global::BaseNavigator.NavigationSpeed.Fast, 0f, 0f);
						}
					}
					return global::StateStatus.Running;
				}

				// Token: 0x060031B6 RID: 12726 RVA: 0x0013241C File Offset: 0x0013061C
				private void FaceTarget(BaseEntity baseEntity)
				{
					if (baseEntity == null)
					{
						this.brain.Navigator.ClearFacingDirectionOverride();
						return;
					}
					this.brain.Navigator.SetFacingDirectionEntity(baseEntity);
				}

				// Token: 0x040021C2 RID: 8642
				private float nextActionTime;

				// Token: 0x040021C3 RID: 8643
				private Vector3 combatStartPosition;
			}

					
			public class TakeCoverState : global::BaseAIBrain.BasicAIState{
				Vector3 lastSpot = new Vector3(0,0,0);
				public TakeCoverState()
					: base(global::AIState.TakeCover)
				{
				}
				public override void StateEnter(global::BaseAIBrain brain, global::BaseEntity entity)
				{
					if(this.brain==null){
						this.brain=brain;//
					}
					base.StateEnter(brain, entity);
					this.status = global::StateStatus.Running;
					if((entity as ScientistNPC).GetMounted()!=null){
						mounted=true;
						return;
					}
					if (!this.StartMovingToCover(entity as global::HumanNPC))
					{
						this.status = global::StateStatus.Error;
					}
				}

				public override void StateLeave(global::BaseAIBrain brain, global::BaseEntity entity)
				{
					base.StateLeave(brain, entity);
					brain.Navigator.ClearFacingDirectionOverride();
					this.ClearCoverPointUsage(entity);
				}

				private void ClearCoverPointUsage(global::BaseEntity entity)
				{
					global::AIPoint aipoint = this.brain.Events.Memory.AIPoint.Get(4);
					if (aipoint != null)
					{
						aipoint.ClearIfUsedBy(entity);
					}
				}
				Vector3 bestTarget = new Vector3(0,0,0);

				private bool StartMovingToCover(global::HumanNPC entity)
				{
					this.coverFromEntity = this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
					BasePlayer bp = this.coverFromEntity as BasePlayer;
					Vector3 vector = (this.coverFromEntity ? this.coverFromEntity.transform.position : (entity.transform.position + entity.LastAttackedDir * 10f));
					
					/*
					global::AIInformationZone informationZone = entity.GetInformationZone(entity.transform.position);
					if (informationZone == null)
					{
						return false;
					}*/
					float num = ((entity.SecondsSinceAttacked < 2f) ? 2f : 0f);
					float bestCoverPointMaxDistance = this.brain.Navigator.BestCoverPointMaxDistance;
					List<ResourceEntity> piles = new List<ResourceEntity>();
					BaseEntity.Query.Server.GetInSphere(entity.transform.position, 30f, piles);
					
					Vector3 insideUnitSphere = global::UnityEngine.Random.insideUnitSphere;
					insideUnitSphere-=entity.eyes.HeadForward();
					insideUnitSphere.y = 0f;
					insideUnitSphere.Normalize();
					
					float bestScore = 0;
					bestTarget = new Vector3(0,0,0);
					//Try to cover away from player vision
					Vector3 coverVector =  new Vector3(0,0,0)+(Vector3.Distance(
						entity.transform.position,coverFromEntity.transform.position+coverFromEntity.transform.right)>
						Vector3.Distance(entity.transform.position,coverFromEntity.transform.position-coverFromEntity.transform.right)?
						entity.eyes.HeadRight():-entity.eyes.HeadRight());
					Vector3 target = entity.transform.position+(((coverVector*2)+insideUnitSphere)*10);
					if(piles.Count()>0){
						foreach(ResourceEntity pile in piles){
							float dot = Vector3.Dot((bp!=null?bp.eyes.HeadForward(): new Vector3(0,0,0)-entity.LastAttackedDir), (pile.transform.position-vector).normalized);
							float score = Mathf.Sqrt(1-dot)*Mathf.Pow(1/Vector3.Distance(entity.transform.position,pile.transform.position),2);//
							if(score>bestScore && dot>0f&& pile.transform.position!=bestTarget){
								bestScore=score;
								bestTarget = pile.transform.position;
								coverVector = (pile.transform.position-vector).normalized;
							}
						}
					}
					if(bestScore!=0){
						target=bestTarget+((coverVector*2+insideUnitSphere)*1f);
						Console.WriteLine("Found cover" +target);
					}
					
					
					if (!this.brain.Navigator.SetDestination(target, global::BaseNavigator.NavigationSpeed.Fast, 0f, 5f))
					{
						return false;
					}
					
					this.FaceCoverFromEntity();		
					return true;
					/*
					global::AICoverPoint bestCoverPoint = informationZone.GetBestCoverPoint(entity.transform.position, vector, num, bestCoverPointMaxDistance, entity, true);
					if (bestCoverPoint == null)
					{
						return false;
					}
					Vector3 position = bestCoverPoint.transform.position;
					if (!this.brain.Navigator.SetDestination(position, global::BaseNavigator.NavigationSpeed.Normal, 0f, 0f))
					{
						return false;
					}
					this.FaceCoverFromEntity();
					this.brain.Events.Memory.AIPoint.Set(bestCoverPoint, 4);
					bestCoverPoint.SetUsedBy(entity);
					return true;
					*/
				}

				// Token: 0x060031D7 RID: 12759 RVA: 0x00132C4C File Offset: 0x00130E4C
				public override void DrawGizmos()
				{
					base.DrawGizmos();
				}
				float timeStuck = 0;
				bool mounted = false;
				// Token: 0x060031D8 RID: 12760 RVA: 0x00132C54 File Offset: 0x00130E54
				public override global::StateStatus StateThink(float delta, global::BaseAIBrain brain, global::BaseEntity entity)
				{
					base.StateThink(delta, brain, entity);
					this.FaceCoverFromEntity();
					
					bool newmounted = ScientistPatrolStates.DoFall(delta, brain, entity,mounted);
					if(mounted && !newmounted){
						StartMovingToCover(entity as HumanNPC);
					}
					mounted =newmounted;
					if(mounted){
						return global::StateStatus.Finished;
					}
					
					if (this.status == global::StateStatus.Error)
					{
						return this.status;
					}
					if(lastSpot == entity.transform.position){
						timeStuck+=delta;
					}else{
						timeStuck=0;
					}
					lastSpot = entity.transform.position;
					if (brain.Navigator.Moving && timeStuck<3)
					{
						return global::StateStatus.Running;
					}
					return global::StateStatus.Finished;
				}

				// Token: 0x060031D9 RID: 12761 RVA: 0x00132C88 File Offset: 0x00130E88
				private void FaceCoverFromEntity()
				{
					this.coverFromEntity = this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
					if (this.coverFromEntity == null)
					{
						return;
					}
					this.brain.Navigator.SetFacingDirectionEntity(this.coverFromEntity);
				}

				// Token: 0x040021C7 RID: 8647
				private global::StateStatus status = global::StateStatus.Error;

				// Token: 0x040021C8 RID: 8648
				private global::BaseEntity coverFromEntity;
			}

			
			
			public class BaseRoamState : global::BaseAIBrain.BasicAIState
			{
				public BaseRoamState()
					: base(global::AIState.FollowPath)
				{
				}
				Vector3 target = new Vector3(0,0,0);
				PathList road = null;
				bool mounted = false;
				int roadIdx = 0;
				public override float GetWeight()
				{
					return 0f;
				}

				public override void StateEnter(global::BaseAIBrain brain, global::BaseEntity entity)
				{
					base.StateEnter(brain, entity);
					if(ScientistScoutingStates.targetTC==null||ScientistScoutingStates.targetTC.transform==null){
						
						ScientistPatrolStates.setupRoamBrain(entity as ScientistNPC);
					}
					target=ScientistScoutingStates.targetTC.transform.position;
					if((entity as ScientistNPC).GetMounted()!=null){
						mounted=true;
						return;
					}
					
					this.nextRoamPositionTime = -1f;
					this.lastDestinationTime = Time.time;
					if(entity.creatorEntity!=null && Vector3.Distance(entity.transform.position,entity.creatorEntity.transform.position)>10f){
						Vector3 insideUnitSphere = global::UnityEngine.Random.insideUnitSphere;
						insideUnitSphere.y = 0f;
						insideUnitSphere.Normalize();
						Vector3 vector = (entity.creatorEntity.transform.position+ (insideUnitSphere * 5));
						brain.Navigator.SetDestination(vector, global::BaseNavigator.NavigationSpeed.Fast, 0f, 20f);
						return;
					}
					SetDestination( brain, entity as ScientistNPC);
				}
				public virtual Vector3 GetDestination()
				{
					return target;
				}

				public virtual Vector3 GetForwardDirection()
				{
					return Vector3.forward;
				}

				public virtual void SetDestination(BaseAIBrain brain,ScientistNPC entity)
				{
					FindBuildBlocks(entity, brain, null);
					Vector3 insideUnitSphere = global::UnityEngine.Random.insideUnitSphere;
					insideUnitSphere+=entity.eyes.HeadForward();
					insideUnitSphere.y = 0f;
					insideUnitSphere.Normalize();
					Console.ForegroundColor = ConsoleColor.Magenta;
					Console.WriteLine(target.ToString() + " : " + ScientistScoutingStates.targetTC.transform.position);
					Console.ResetColor();
					brain.Navigator.SetDestination(target+(insideUnitSphere*50), global::BaseNavigator.NavigationSpeed.Normal, 0f, 100f);
				}

				public override void DrawGizmos()
				{
					base.DrawGizmos();
					this.brain.PathFinder.DebugDraw();
				}

				public virtual Vector3 GetRoamAnchorPosition()
				{
					if (this.brain.Navigator.MaxRoamDistanceFromHome > -1f)
					{
						return this.brain.Events.Memory.Position.Get(4);
					}
					return this.brain.GetBaseEntity().transform.position;
				}

				public override global::StateStatus StateThink(float delta, global::BaseAIBrain brain, global::BaseEntity entity)
				{
					base.StateThink(delta, brain, entity);
					bool newmounted = ScientistPatrolStates.DoFall(delta, brain, entity,mounted);
					mounted=newmounted;
					if(mounted){
						return global::StateStatus.Finished;
					}
					if(ScientistScoutingStates.targetTC==null||ScientistScoutingStates.targetTC.transform==null){
						
						ScientistPatrolStates.setupRoamBrain(entity as ScientistNPC);
					}
					
					bool flag = Time.time - this.lastDestinationTime > 5f;
					if (((Vector3.Distance(this.GetDestination(), entity.transform.position) < 1f || flag) ) && this.nextRoamPositionTime == -1f)
					{
						this.nextRoamPositionTime = Time.time + global::UnityEngine.Random.Range(1f, 3f);
					}
					if (this.nextRoamPositionTime != -1f && Time.time > this.nextRoamPositionTime)
					{
						this.nextRoamPositionTime = -1f;
						this.lastDestinationTime = Time.time;
						if(entity.creatorEntity!=null && entity.creatorEntity.transform!=null){
							if(Vector3.Distance(entity.transform.position,entity.creatorEntity.transform.position)>10f){
								Vector3 insideUnitSphere = global::UnityEngine.Random.insideUnitSphere;
								insideUnitSphere.y = 0f;
								insideUnitSphere.Normalize();
								Vector3 vector = (entity.creatorEntity.transform.position + (((entity.creatorEntity.transform.position-entity.transform.position).normalized*2+insideUnitSphere) * 3));
								brain.Navigator.SetDestination(vector, global::BaseNavigator.NavigationSpeed.Fast, 0f, 20f);
							}
						}else{
							this.SetDestination(brain,entity as ScientistNPC);
							
						}
					}
					return global::StateStatus.Running;
				}

				// Token: 0x0400036D RID: 877
				private float nextRoamPositionTime = -1f;

				// Token: 0x0400036E RID: 878
				private float lastDestinationTime;
			}
			
		
		}
		#endregion
		
		#region Base Scouting
		
		class ScientistScoutingStates{
			public static BuildingPrivlidge targetTC = null;
			public static bool DoFall(float delta, global::BaseAIBrain brain, global::BaseEntity entity,bool mounted){
				BaseMountable mountable = (entity as ScientistNPC).GetMounted();
				if(mountable==null){
					if(mounted){
						if(!brain.Navigator.Warp(entity.transform.position)){
							UnityEngine.AI.NavMeshHit hit;
							bool hitNMSH = UnityEngine.AI.NavMesh.Raycast(entity.transform.position, entity.transform.position+(new Vector3(0,-10,0)), 
							out hit, brain.Navigator.Agent.areaMask);				
							brain.Navigator.Warp(hitNMSH?hit.position:entity.transform.position+(new Vector3(0,-10*delta,0)));
							mounted=!hitNMSH;
						}else{
							mounted=false;
						}
					}
					//NavMesh.Raycast(Vector3 sourcePosition, Vector3 targetPosition, out NavMeshHit hit, int areaMask)
				}else{
					mounted=true;
				}
				return mounted;
				
			}
			public static BaseEntity FindBuildBlocks(BaseEntity entity, BaseAIBrain brain,BaseEntity target ){
					List<BuildingBlock> piles = new List<BuildingBlock>();
					BaseEntity.Query.Server.GetInSphere(entity.transform.position, 30f, piles);
					if(piles!=null&&piles.Count()>0){
						foreach(BuildingBlock ore in piles){
							if(ore!=null && entity.IsVisible(ore.CenterPoint(), entity.CenterPoint(), float.PositiveInfinity)){
								target=ore;
								brain.Senses.Memory.SetLOS(ore,true);
								brain.Senses.Players.Add(ore);
							}
						}
					}
					return target;
			}
			// Token: 0x0200006A RID: 106
			public class BaseChaseState : global::BaseAIBrain.BasicAIState
			{
				// Token: 0x06000D3C RID: 3388 RVA: 0x00046CB0 File Offset: 0x00044EB0
				public BaseChaseState()
					: base(global::AIState.Chase)
				{
					base.AgrresiveState = true;
				}

				bool mounted = false;
				// Token: 0x06000D3D RID: 3389 RVA: 0x00046CC0 File Offset: 0x00044EC0
				public override void StateEnter(global::BaseAIBrain brain, global::BaseEntity baseentity)
				{
					base.StateEnter(brain, baseentity);
					if(this.brain==null){
						this.brain=brain;
					}
					if((baseentity as ScientistNPC).GetMounted()!=null){
						mounted=true;
						return;
					}
					if(!(baseentity is ScientistNPC)){return;}
					ScientistNPC entity = (baseentity as ScientistNPC);
					BaseEntity coverFromEntityAsEntity = this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
					BasePlayer coverFromEntity = coverFromEntityAsEntity as BasePlayer;
					Vector3 vector = (coverFromEntityAsEntity ? coverFromEntityAsEntity.transform.position : (entity.transform.position + entity.LastAttackedDir * 10f));
					
					/*
					global::AIInformationZone informationZone = entity.GetInformationZone(entity.transform.position);
					if (informationZone == null)
					{
						return false;
					}*/
					float num = ((entity.SecondsSinceAttacked < 2f) ? 2f : 0f);
					float bestCoverPointMaxDistance = this.brain.Navigator.BestCoverPointMaxDistance;
					List<ResourceEntity> piles = new List<ResourceEntity>();
					BaseEntity.Query.Server.GetInSphere(vector, 50, piles);
					
					
					Vector3 insideUnitSphere = global::UnityEngine.Random.insideUnitSphere;
					insideUnitSphere-=entity.eyes.HeadForward();
					insideUnitSphere.y = 0f;
					insideUnitSphere.Normalize();
					
					Vector3 target = vector+(insideUnitSphere*30);
					float bestScore = 10;
					Vector3 bestTarget = new Vector3(0,0,0);
					Vector3 coverVector =  (coverFromEntity!=null?new Vector3(0,0,0)-coverFromEntity.eyes.HeadForward():entity.eyes.HeadForward());
					coverVector.y=0;
					coverVector.Normalize();
					if(piles.Count()>0){
						foreach(ResourceEntity pile in piles){
							float dot = Vector3.Distance(vector, pile.transform.position);
							if(dot<bestScore){
								bestScore=dot;
								bestTarget = pile.transform.position;
								coverVector = (vector-pile.transform.position-vector).normalized;
							}
						}
					}
					if(bestScore!=10){
						target=bestTarget+((coverVector+insideUnitSphere*10)*1);
					}else{
						
					}
					//Console.WriteLine("Found target:" + Vector3.Distance(vector,target));
					
					
					if (!this.brain.Navigator.SetDestination(target, global::BaseNavigator.NavigationSpeed.Fast, 0f, 50f))
					{
						Console.WriteLine("Can't chase " + target);
						this.Stop();
					}
				}

				// Token: 0x06000D3E RID: 3390 RVA: 0x00046D24 File Offset: 0x00044F24
				public override void StateLeave(global::BaseAIBrain brain, global::BaseEntity entity)
				{
					base.StateLeave(brain, entity);
					//this.Stop();
				}

				// Token: 0x06000D3F RID: 3391 RVA: 0x00046D34 File Offset: 0x00044F34
				private void Stop()
				{
					this.brain.Navigator.Stop();
				}

				// Token: 0x06000D40 RID: 3392 RVA: 0x00046D48 File Offset: 0x00044F48
				public override global::StateStatus StateThink(float delta, global::BaseAIBrain brain, global::BaseEntity entity)
				{
					base.StateThink(delta, brain, entity);
					
					mounted = ScientistPatrolStates.DoFall(delta, brain, entity,mounted);
					if(mounted){
						return global::StateStatus.Finished;
					}
					global::BaseEntity baseEntity = brain.Events.Memory.Entity.Get(brain.Events.CurrentInputMemorySlot);
					if (baseEntity == null)
					{
						this.Stop();
						return global::StateStatus.Finished;
					}
					if (!brain.Navigator.Moving)
					{
						return global::StateStatus.Finished;
					}
					return global::StateStatus.Running;
				}
			}
			public class CombatState : global::BaseAIBrain.BasicAIState
			{
				// Token: 0x060031B2 RID: 12722 RVA: 0x00132300 File Offset: 0x00130500
				public CombatState()
					: base(global::AIState.Combat)
				{
					base.AgrresiveState = true;
				}

				// Token: 0x060031B3 RID: 12723 RVA: 0x00132310 File Offset: 0x00130510
				public override void StateEnter(global::BaseAIBrain brain, global::BaseEntity entity)
				{
					base.StateEnter(brain, entity);
					this.brain=brain;
					this.combatStartPosition = entity.transform.position;
					BaseEntity target=this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
					target=FindBuildBlocks(entity, brain, target);
					if(target!=null){
						this.FaceTarget(target);
					}
				}

				// Token: 0x060031B4 RID: 12724 RVA: 0x00132334 File Offset: 0x00130534
				public override void StateLeave(global::BaseAIBrain brain, global::BaseEntity entity)
				{
					base.StateLeave(brain, entity);
					(entity as global::HumanNPC).SetDucked(false);
					brain.Navigator.ClearFacingDirectionOverride();
				}

				// Token: 0x060031B5 RID: 12725 RVA: 0x00132358 File Offset: 0x00130558
				public override global::StateStatus StateThink(float delta, global::BaseAIBrain brain, global::BaseEntity entity)
				{
					base.StateThink(delta, brain, entity);
						
					
					global::HumanNPC humanNPC = entity as global::HumanNPC;
					BaseEntity target = this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
					if(target!=null){
						this.FaceTarget(target);
					}
					if (Time.time > this.nextActionTime)
					{
						if (global::UnityEngine.Random.Range(0, 3) == 1)
						{
							this.nextActionTime = Time.time + global::UnityEngine.Random.Range(1f, 2f);
							humanNPC.SetDucked(true);
							brain.Navigator.Stop();
						}
						else
						{
							this.nextActionTime = Time.time + global::UnityEngine.Random.Range(2f, 3f);
							humanNPC.SetDucked(false);
							brain.Navigator.SetDestination(brain.PathFinder.GetRandomPositionAround(this.combatStartPosition, 1f, 2f), global::BaseNavigator.NavigationSpeed.Normal, 0f, 0f);
						}
					}
					return global::StateStatus.Running;
				}

				// Token: 0x060031B6 RID: 12726 RVA: 0x0013241C File Offset: 0x0013061C
				private void FaceTarget(BaseEntity baseEntity)
				{
					if (baseEntity == null)
					{
						this.brain.Navigator.ClearFacingDirectionOverride();
						return;
					}
					this.brain.Navigator.SetFacingDirectionEntity(baseEntity);
				}

				// Token: 0x040021C2 RID: 8642
				private float nextActionTime;

				// Token: 0x040021C3 RID: 8643
				private Vector3 combatStartPosition;
			}

					
			public class TakeCoverState : global::BaseAIBrain.BasicAIState{
				Vector3 lastSpot = new Vector3(0,0,0);
				public TakeCoverState()
					: base(global::AIState.TakeCover)
				{
				}
				public override void StateEnter(global::BaseAIBrain brain, global::BaseEntity entity)
				{
					if(this.brain==null){
						this.brain=brain;//
					}
					base.StateEnter(brain, entity);
					this.status = global::StateStatus.Running;
					if((entity as ScientistNPC).GetMounted()!=null){
						mounted=true;
						return;
					}
					if (!this.StartMovingToCover(entity as global::HumanNPC))
					{
						this.status = global::StateStatus.Error;
					}
				}

				public override void StateLeave(global::BaseAIBrain brain, global::BaseEntity entity)
				{
					base.StateLeave(brain, entity);
					brain.Navigator.ClearFacingDirectionOverride();
					this.ClearCoverPointUsage(entity);
				}

				private void ClearCoverPointUsage(global::BaseEntity entity)
				{
					global::AIPoint aipoint = this.brain.Events.Memory.AIPoint.Get(4);
					if (aipoint != null)
					{
						aipoint.ClearIfUsedBy(entity);
					}
				}
				Vector3 bestTarget = new Vector3(0,0,0);

				private bool StartMovingToCover(global::HumanNPC entity)
				{
					this.coverFromEntity = this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
					BasePlayer bp = this.coverFromEntity as BasePlayer;
					Vector3 vector = (this.coverFromEntity ? this.coverFromEntity.transform.position : (entity.transform.position + entity.LastAttackedDir * 10f));
					
					/*
					global::AIInformationZone informationZone = entity.GetInformationZone(entity.transform.position);
					if (informationZone == null)
					{
						return false;
					}*/
					float num = ((entity.SecondsSinceAttacked < 2f) ? 2f : 0f);
					float bestCoverPointMaxDistance = this.brain.Navigator.BestCoverPointMaxDistance;
					List<ResourceEntity> piles = new List<ResourceEntity>();
					BaseEntity.Query.Server.GetInSphere(entity.transform.position, 30f, piles);
					
					Vector3 insideUnitSphere = global::UnityEngine.Random.insideUnitSphere;
					insideUnitSphere-=entity.eyes.HeadForward();
					insideUnitSphere.y = 0f;
					insideUnitSphere.Normalize();
					
					float bestScore = 0;
					bestTarget = new Vector3(0,0,0);
					//Try to cover away from player vision
					Vector3 coverVector =  new Vector3(0,0,0)+(Vector3.Distance(
						entity.transform.position,coverFromEntity.transform.position+coverFromEntity.transform.right)>
						Vector3.Distance(entity.transform.position,coverFromEntity.transform.position-coverFromEntity.transform.right)?
						entity.eyes.HeadRight():-entity.eyes.HeadRight());
					Vector3 target = entity.transform.position+(((coverVector*2)+insideUnitSphere)*10);
					if(piles.Count()>0){
						foreach(ResourceEntity pile in piles){
							float dot = Vector3.Dot((bp!=null?bp.eyes.HeadForward(): new Vector3(0,0,0)-entity.LastAttackedDir), (pile.transform.position-vector).normalized);
							float score = Mathf.Sqrt(1-dot)*Mathf.Pow(1/Vector3.Distance(entity.transform.position,pile.transform.position),2);//
							if(score>bestScore && dot>0f&& pile.transform.position!=bestTarget){
								bestScore=score;
								bestTarget = pile.transform.position;
								coverVector = (pile.transform.position-vector).normalized;
							}
						}
					}
					if(bestScore!=0){
						target=bestTarget+((coverVector*2+insideUnitSphere)*1f);
						Console.WriteLine("Found cover" +target);
					}
					
					
					if (!this.brain.Navigator.SetDestination(target, global::BaseNavigator.NavigationSpeed.Fast, 0f, 5f))
					{
						return false;
					}
					
					this.FaceCoverFromEntity();		
					return true;
					/*
					global::AICoverPoint bestCoverPoint = informationZone.GetBestCoverPoint(entity.transform.position, vector, num, bestCoverPointMaxDistance, entity, true);
					if (bestCoverPoint == null)
					{
						return false;
					}
					Vector3 position = bestCoverPoint.transform.position;
					if (!this.brain.Navigator.SetDestination(position, global::BaseNavigator.NavigationSpeed.Normal, 0f, 0f))
					{
						return false;
					}
					this.FaceCoverFromEntity();
					this.brain.Events.Memory.AIPoint.Set(bestCoverPoint, 4);
					bestCoverPoint.SetUsedBy(entity);
					return true;
					*/
				}

				// Token: 0x060031D7 RID: 12759 RVA: 0x00132C4C File Offset: 0x00130E4C
				public override void DrawGizmos()
				{
					base.DrawGizmos();
				}
				float timeStuck = 0;
				bool mounted = false;
				// Token: 0x060031D8 RID: 12760 RVA: 0x00132C54 File Offset: 0x00130E54
				public override global::StateStatus StateThink(float delta, global::BaseAIBrain brain, global::BaseEntity entity)
				{
					base.StateThink(delta, brain, entity);
					this.FaceCoverFromEntity();
					
					bool newmounted = ScientistPatrolStates.DoFall(delta, brain, entity,mounted);
					if(mounted && !newmounted){
						StartMovingToCover(entity as HumanNPC);
					}
					mounted =newmounted;
					if(mounted){
						return global::StateStatus.Finished;
					}
					
					if (this.status == global::StateStatus.Error)
					{
						return this.status;
					}
					if(lastSpot == entity.transform.position){
						timeStuck+=delta;
					}else{
						timeStuck=0;
					}
					lastSpot = entity.transform.position;
					if (brain.Navigator.Moving && timeStuck<3)
					{
						return global::StateStatus.Running;
					}
					return global::StateStatus.Finished;
				}

				// Token: 0x060031D9 RID: 12761 RVA: 0x00132C88 File Offset: 0x00130E88
				private void FaceCoverFromEntity()
				{
					this.coverFromEntity = this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
					if (this.coverFromEntity == null)
					{
						return;
					}
					this.brain.Navigator.SetFacingDirectionEntity(this.coverFromEntity);
				}

				// Token: 0x040021C7 RID: 8647
				private global::StateStatus status = global::StateStatus.Error;

				// Token: 0x040021C8 RID: 8648
				private global::BaseEntity coverFromEntity;
			}

			
			
			public class BaseRoamState : global::BaseAIBrain.BasicAIState
			{
				public BaseRoamState()
					: base(global::AIState.FollowPath)
				{
				}
				Vector3 target = new Vector3(0,0,0);
				PathList road = null;
				bool mounted = false;
				int roadIdx = 0;
				public override float GetWeight()
				{
					return 0f;
				}

				public override void StateEnter(global::BaseAIBrain brain, global::BaseEntity entity)
				{
					base.StateEnter(brain, entity);
					if(ScientistScoutingStates.targetTC==null||ScientistScoutingStates.targetTC.transform==null){
						
						ScientistPatrolStates.setupRoamBrain(entity as ScientistNPC);
					}
					target=targetTC.transform.position;
					if((entity as ScientistNPC).GetMounted()!=null){
						mounted=true;
						return;
					}
					
					this.nextRoamPositionTime = -1f;
					this.lastDestinationTime = Time.time;
					if(entity.creatorEntity!=null && Vector3.Distance(entity.transform.position,entity.creatorEntity.transform.position)>10f){
						Vector3 insideUnitSphere = global::UnityEngine.Random.insideUnitSphere;
						insideUnitSphere.y = 0f;
						insideUnitSphere.Normalize();
						Vector3 vector = (entity.creatorEntity.transform.position+ (insideUnitSphere * 5));
						brain.Navigator.SetDestination(vector, global::BaseNavigator.NavigationSpeed.Fast, 0f, 20f);
						return;
					}
					SetDestination( brain, entity as ScientistNPC);
				}
				public virtual Vector3 GetDestination()
				{
					return target;
				}

				public virtual Vector3 GetForwardDirection()
				{
					return Vector3.forward;
				}

				public virtual void SetDestination(BaseAIBrain brain,ScientistNPC entity)
				{
					FindBuildBlocks(entity, brain, null);
					Vector3 insideUnitSphere = global::UnityEngine.Random.insideUnitSphere;
					insideUnitSphere+=entity.eyes.HeadForward();
					insideUnitSphere.y = 0f;
					insideUnitSphere.Normalize();
					Console.ForegroundColor = ConsoleColor.Magenta;
					Console.WriteLine(target.ToString() + " : " + targetTC.transform.position);
					Console.ResetColor();
					brain.Navigator.SetDestination(target+(insideUnitSphere*50), global::BaseNavigator.NavigationSpeed.Normal, 0f, 100f);
				}

				public override void DrawGizmos()
				{
					base.DrawGizmos();
					this.brain.PathFinder.DebugDraw();
				}

				public virtual Vector3 GetRoamAnchorPosition()
				{
					if (this.brain.Navigator.MaxRoamDistanceFromHome > -1f)
					{
						return this.brain.Events.Memory.Position.Get(4);
					}
					return this.brain.GetBaseEntity().transform.position;
				}

				public override global::StateStatus StateThink(float delta, global::BaseAIBrain brain, global::BaseEntity entity)
				{
					base.StateThink(delta, brain, entity);
					bool newmounted = ScientistPatrolStates.DoFall(delta, brain, entity,mounted);
					mounted=newmounted;
					if(mounted){
						return global::StateStatus.Finished;
					}
					
					if(ScientistScoutingStates.targetTC==null||ScientistScoutingStates.targetTC.transform==null){
						
						ScientistPatrolStates.setupRoamBrain(entity as ScientistNPC);
					}
					bool flag = Time.time - this.lastDestinationTime > 5f;
					if (((Vector3.Distance(this.GetDestination(), entity.transform.position) < 1f || flag) ) && this.nextRoamPositionTime == -1f)
					{
						this.nextRoamPositionTime = Time.time + global::UnityEngine.Random.Range(1f, 3f);
					}
					if (this.nextRoamPositionTime != -1f && Time.time > this.nextRoamPositionTime)
					{
						this.nextRoamPositionTime = -1f;
						this.lastDestinationTime = Time.time;
						if(entity.creatorEntity!=null && entity.creatorEntity.transform!=null){
							if(Vector3.Distance(entity.transform.position,entity.creatorEntity.transform.position)>10f){
								Vector3 insideUnitSphere = global::UnityEngine.Random.insideUnitSphere;
								insideUnitSphere.y = 0f;
								insideUnitSphere.Normalize();
								Vector3 vector = (entity.creatorEntity.transform.position + (((entity.creatorEntity.transform.position-entity.transform.position).normalized*2+insideUnitSphere) * 3));
								brain.Navigator.SetDestination(vector, global::BaseNavigator.NavigationSpeed.Fast, 0f, 20f);
							}
						}else{
							this.SetDestination(brain,entity as ScientistNPC);
							
						}
					}
					return global::StateStatus.Running;
				}

				// Token: 0x0400036D RID: 877
				private float nextRoamPositionTime = -1f;

				// Token: 0x0400036E RID: 878
				private float lastDestinationTime;
			}
			
		}
		
		#endregion
		
		#region Road patrol
		class ScientistPatrolStates{
			public static void setupRoamBrain(HumanNPC npc){
				npc.Brain.Navigator.MaxRoamDistanceFromHome =-1;
				npc.Brain.states[AIState.FollowPath]=new ScientistPatrolStates.BaseRoamState();
				npc.Brain.states[AIState.TakeCover]=new ScientistPatrolStates.TakeCoverState();
				npc.Brain.states[AIState.Chase]=new ScientistPatrolStates.BaseChaseState();
				npc.Brain.states[AIState.Combat]=new ScientistPatrolStates.CombatState();
				npc.Brain.SwitchToState(AIState.FollowPath,0);
				
				npc.Brain.AIDesign.GetFirstStateContainerOfType(AIState.Combat).Events[4].TriggerStateContainerID=4;
				npc.Brain.AIDesign.GetFirstStateContainerOfType(AIState.Combat).Events[6].Inverted=false;
				npc.Brain.AIDesign.GetFirstStateContainerOfType(AIState.Combat).Events[6].TriggerStateContainerID=2;
				foreach(BaseAIEvent evt in npc.Brain.AIDesign.GetFirstStateContainerOfType(AIState.Chase).Events){
					Console.WriteLine(""+evt.EventType + " : " + evt.ID);
				}
				npc.Brain.Senses.targetLostRange=100f;//
				npc.Brain.Senses.maxRange=60f;//
				if(npc.creatorEntity!=null && (npc.creatorEntity is ScientistNPC)){
					npc.Brain.Senses.Memory.Players=(npc.creatorEntity as ScientistNPC).Brain.Senses.Memory.Players;
				}
				npc.inventory.containerBelt.Clear();
				foreach(LoadoutEntry le in Reprisal._instance.config.Belts[0].entries){
					Item cassetteItem = ItemManager.CreateByName(le.itemname, le.amount, le.skinid);
					npc.inventory.containerBelt.GiveItem(cassetteItem);
					npc.UpdateActiveItem(cassetteItem.uid);
					AttackEntity atEnt = cassetteItem.heldEntity.Get(true) as AttackEntity;
					if(atEnt!=null){
						atEnt.effectiveRange = atEnt.effectiveRange<20?20f:atEnt.effectiveRange;
					}
				}
				npc.inventory.containerWear.Clear();
				foreach(LoadoutEntry le in Reprisal._instance.config.Outfits[0].entries){
					Item cassetteItem = ItemManager.CreateByName(le.itemname, le.amount, le.skinid);
					npc.inventory.containerWear.GiveItem(cassetteItem);
				}
			}
		
		
			public static bool DoFall(float delta, global::BaseAIBrain brain, global::BaseEntity entity,bool mounted){
				BaseMountable mountable = (entity as ScientistNPC).GetMounted();
				if(mountable==null){
					if(mounted){
						if(!brain.Navigator.Warp(entity.transform.position)){
							UnityEngine.AI.NavMeshHit hit;
							bool hitNMSH = UnityEngine.AI.NavMesh.Raycast(entity.transform.position, entity.transform.position+(new Vector3(0,-10,0)), 
							out hit, brain.Navigator.Agent.areaMask);				
							brain.Navigator.Warp(hitNMSH?hit.position:entity.transform.position+(new Vector3(0,-10*delta,0)));
							mounted=!hitNMSH;
						}else{
							mounted=false;
						}
					}
					//NavMesh.Raycast(Vector3 sourcePosition, Vector3 targetPosition, out NavMeshHit hit, int areaMask)
				}else{
					mounted=true;
				}
				return mounted;
				
			}
			public static BaseEntity FindBuildBlocks(BaseEntity entity, BaseAIBrain brain,BaseEntity target ){
					List<BuildingBlock> piles = new List<BuildingBlock>();
					BaseEntity.Query.Server.GetInSphere(entity.transform.position, 30f, piles);
					if(piles!=null&&piles.Count()>0){
						foreach(BuildingBlock ore in piles){
							if(ore!=null && entity.IsVisible(ore.CenterPoint(), entity.CenterPoint(), float.PositiveInfinity)){
								target=ore;
								brain.Events.Memory.Entity.Set(ore,brain.Events.CurrentInputMemorySlot);
								brain.Senses.Memory.SetLOS(ore,true);
								brain.Senses.Players.Add(ore);
								break;
							}
						}
					}
					return target;
			}
			// Token: 0x0200006A RID: 106
			public class BaseChaseState : global::BaseAIBrain.BasicAIState
			{
				// Token: 0x06000D3C RID: 3388 RVA: 0x00046CB0 File Offset: 0x00044EB0
				public BaseChaseState()
					: base(global::AIState.Chase)
				{
					base.AgrresiveState = true;
				}

				bool mounted = false;
				// Token: 0x06000D3D RID: 3389 RVA: 0x00046CC0 File Offset: 0x00044EC0
				public override void StateEnter(global::BaseAIBrain brain, global::BaseEntity baseentity)
				{
					base.StateEnter(brain, baseentity);
					if(this.brain==null){
						this.brain=brain;
					}
					if((baseentity as ScientistNPC).GetMounted()!=null){
						mounted=true;
						return;
					}
					if(!(baseentity is ScientistNPC)){return;}
					ScientistNPC entity = (baseentity as ScientistNPC);
					BaseEntity coverFromEntityAsEntity = this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
					BasePlayer coverFromEntity = coverFromEntityAsEntity as BasePlayer;
					Vector3 vector = (coverFromEntityAsEntity ? coverFromEntityAsEntity.transform.position : (entity.transform.position + entity.LastAttackedDir * 10f));
					
					/*
					global::AIInformationZone informationZone = entity.GetInformationZone(entity.transform.position);
					if (informationZone == null)
					{
						return false;
					}*/
					float num = ((entity.SecondsSinceAttacked < 2f) ? 2f : 0f);
					float bestCoverPointMaxDistance = this.brain.Navigator.BestCoverPointMaxDistance;
					List<JunkPile> piles = new List<JunkPile>();
					BaseEntity.Query.Server.GetInSphere(vector, 50, piles);
					
					
					Vector3 insideUnitSphere = global::UnityEngine.Random.insideUnitSphere;
					insideUnitSphere-=entity.eyes.HeadForward();
					insideUnitSphere.y = 0f;
					insideUnitSphere.Normalize();
					
					Vector3 target = vector+insideUnitSphere;
					float bestScore = 10;
					Vector3 bestTarget = new Vector3(0,0,0);
					Vector3 coverVector =  (coverFromEntity!=null?new Vector3(0,0,0)-coverFromEntity.eyes.HeadForward():entity.eyes.HeadForward());
					coverVector.y=0;
					coverVector.Normalize();
					if(piles.Count()>0){
						foreach(JunkPile pile in piles){
							float dot = Vector3.Distance(vector, pile.transform.position);
							if(dot<bestScore){
								bestScore=dot;
								bestTarget = pile.transform.position;
								coverVector = (vector-pile.transform.position-vector).normalized;
							}
						}
					}
					if(bestScore!=10){
						target=bestTarget+((coverVector+insideUnitSphere)*1);
					}else{
						
					}
					//Console.WriteLine("Found target:" + Vector3.Distance(vector,target));
					
					
					if (!this.brain.Navigator.SetDestination(target, global::BaseNavigator.NavigationSpeed.Fast, 0f, 10f))
					{
					}
				}

				// Token: 0x06000D3E RID: 3390 RVA: 0x00046D24 File Offset: 0x00044F24
				public override void StateLeave(global::BaseAIBrain brain, global::BaseEntity entity)
				{
					base.StateLeave(brain, entity);
					//this.Stop();
				}

				// Token: 0x06000D3F RID: 3391 RVA: 0x00046D34 File Offset: 0x00044F34
				private void Stop()
				{
					this.brain.Navigator.Stop();
				}

				// Token: 0x06000D40 RID: 3392 RVA: 0x00046D48 File Offset: 0x00044F48
				public override global::StateStatus StateThink(float delta, global::BaseAIBrain brain, global::BaseEntity entity)
				{
					base.StateThink(delta, brain, entity);
					
					mounted = ScientistPatrolStates.DoFall(delta, brain, entity,mounted);
					if(mounted){
						return global::StateStatus.Finished;
					}
					global::BaseEntity baseEntity = brain.Events.Memory.Entity.Get(brain.Events.CurrentInputMemorySlot);
					if (baseEntity == null)
					{
						this.Stop();
						return global::StateStatus.Error;
					}
					if (!brain.Navigator.Moving)
					{
						return global::StateStatus.Finished;
					}
					return global::StateStatus.Running;
				}
			}
			public class CombatState : global::BaseAIBrain.BasicAIState
			{
				// Token: 0x060031B2 RID: 12722 RVA: 0x00132300 File Offset: 0x00130500
				public CombatState()
					: base(global::AIState.Combat)
				{
					base.AgrresiveState = true;
				}

				// Token: 0x060031B3 RID: 12723 RVA: 0x00132310 File Offset: 0x00130510
				public override void StateEnter(global::BaseAIBrain brain, global::BaseEntity entity)
				{
					base.StateEnter(brain, entity);
					this.brain=brain;
					this.combatStartPosition = entity.transform.position;
					BaseEntity target=this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
					target=FindBuildBlocks(entity, brain, target);
					if(target!=null){
						this.FaceTarget(target);
					}
				}

				// Token: 0x060031B4 RID: 12724 RVA: 0x00132334 File Offset: 0x00130534
				public override void StateLeave(global::BaseAIBrain brain, global::BaseEntity entity)
				{
					base.StateLeave(brain, entity);
					(entity as global::HumanNPC).SetDucked(false);
					brain.Navigator.ClearFacingDirectionOverride();
				}

				// Token: 0x060031B5 RID: 12725 RVA: 0x00132358 File Offset: 0x00130558
				public override global::StateStatus StateThink(float delta, global::BaseAIBrain brain, global::BaseEntity entity)
				{
					base.StateThink(delta, brain, entity);
					global::HumanNPC humanNPC = entity as global::HumanNPC;
					BaseEntity target = this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
					if(target!=null){
						this.FaceTarget(target);
					}
					if (Time.time > this.nextActionTime)
					{
						if (global::UnityEngine.Random.Range(0, 3) == 1)
						{
							this.nextActionTime = Time.time + global::UnityEngine.Random.Range(1f, 2f);
							humanNPC.SetDucked(true);
							brain.Navigator.Stop();
						}
						else
						{
							this.nextActionTime = Time.time + global::UnityEngine.Random.Range(2f, 3f);
							humanNPC.SetDucked(false);
							brain.Navigator.SetDestination(brain.PathFinder.GetRandomPositionAround(this.combatStartPosition, 1f, 2f), global::BaseNavigator.NavigationSpeed.Fast, 0f, 0f);
						}
					}
					return global::StateStatus.Running;
				}

				// Token: 0x060031B6 RID: 12726 RVA: 0x0013241C File Offset: 0x0013061C
				private void FaceTarget(BaseEntity baseEntity)
				{
					if (baseEntity == null)
					{
						this.brain.Navigator.ClearFacingDirectionOverride();
						return;
					}
					this.brain.Navigator.SetFacingDirectionEntity(baseEntity);
				}

				// Token: 0x040021C2 RID: 8642
				private float nextActionTime;

				// Token: 0x040021C3 RID: 8643
				private Vector3 combatStartPosition;
			}

					
			public class TakeCoverState : global::BaseAIBrain.BasicAIState{
				Vector3 lastSpot = new Vector3(0,0,0);
				public TakeCoverState()
					: base(global::AIState.TakeCover)
				{
				}
				public override void StateEnter(global::BaseAIBrain brain, global::BaseEntity entity)
				{
					if(this.brain==null){
						this.brain=brain;//
					}
					base.StateEnter(brain, entity);
					this.status = global::StateStatus.Running;
					if((entity as ScientistNPC).GetMounted()!=null){
						mounted=true;
						return;
					}
					if (!this.StartMovingToCover(entity as global::HumanNPC))
					{
						this.status = global::StateStatus.Error;
					}
				}

				public override void StateLeave(global::BaseAIBrain brain, global::BaseEntity entity)
				{
					base.StateLeave(brain, entity);
					brain.Navigator.ClearFacingDirectionOverride();
					this.ClearCoverPointUsage(entity);
				}

				private void ClearCoverPointUsage(global::BaseEntity entity)
				{
					global::AIPoint aipoint = this.brain.Events.Memory.AIPoint.Get(4);
					if (aipoint != null)
					{
						aipoint.ClearIfUsedBy(entity);
					}
				}
				Vector3 bestTarget = new Vector3(0,0,0);

				private bool StartMovingToCover(global::HumanNPC entity)
				{
					this.coverFromEntity = this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
					BasePlayer bp = this.coverFromEntity as BasePlayer;
					Vector3 vector = (this.coverFromEntity ? this.coverFromEntity.transform.position : (entity.transform.position + entity.LastAttackedDir * 10f));
					
					/*
					global::AIInformationZone informationZone = entity.GetInformationZone(entity.transform.position);
					if (informationZone == null)
					{
						return false;
					}*/
					float num = ((entity.SecondsSinceAttacked < 2f) ? 2f : 0f);
					float bestCoverPointMaxDistance = this.brain.Navigator.BestCoverPointMaxDistance;
					List<JunkPile> piles = new List<JunkPile>();
					BaseEntity.Query.Server.GetInSphere(entity.transform.position, 30f, piles);
					
					Vector3 insideUnitSphere = global::UnityEngine.Random.insideUnitSphere;
					insideUnitSphere-=entity.eyes.HeadForward();
					insideUnitSphere.y = 0f;
					insideUnitSphere.Normalize();
					
					float bestScore = 0;
					bestTarget = new Vector3(0,0,0);
					Vector3 coverVector =  new Vector3(0,0,0)-entity.eyes.HeadForward();
					Vector3 target = entity.transform.position+(((coverVector*0.5f)+insideUnitSphere)*30);
					if(piles.Count()>0){
						foreach(JunkPile pile in piles){
							float dot = Vector3.Dot((bp!=null?bp.eyes.HeadForward(): new Vector3(0,0,0)-entity.LastAttackedDir), (pile.transform.position-vector).normalized);
							float score = Mathf.Sqrt(1-dot)*Mathf.Pow(1/Vector3.Distance(entity.transform.position,pile.transform.position),2);//
							if(score>bestScore && dot>0f&& pile.transform.position!=bestTarget){
								bestScore=score;
								bestTarget = pile.transform.position;
								coverVector = (pile.transform.position-vector).normalized;
							}
						}
					}
					if(bestScore!=0){
						target=bestTarget+((coverVector*2+insideUnitSphere)*1f);
						Console.WriteLine("Found cover" +target);
					}
					
					
					if (!this.brain.Navigator.SetDestination(target, global::BaseNavigator.NavigationSpeed.Fast, 0f, 5f))
					{
						return false;
					}
					
					this.FaceCoverFromEntity();		
					return true;
					/*
					global::AICoverPoint bestCoverPoint = informationZone.GetBestCoverPoint(entity.transform.position, vector, num, bestCoverPointMaxDistance, entity, true);
					if (bestCoverPoint == null)
					{
						return false;
					}
					Vector3 position = bestCoverPoint.transform.position;
					if (!this.brain.Navigator.SetDestination(position, global::BaseNavigator.NavigationSpeed.Normal, 0f, 0f))
					{
						return false;
					}
					this.FaceCoverFromEntity();
					this.brain.Events.Memory.AIPoint.Set(bestCoverPoint, 4);
					bestCoverPoint.SetUsedBy(entity);
					return true;
					*/
				}

				// Token: 0x060031D7 RID: 12759 RVA: 0x00132C4C File Offset: 0x00130E4C
				public override void DrawGizmos()
				{
					base.DrawGizmos();
				}
				float timeStuck = 0;
				bool mounted = false;
				// Token: 0x060031D8 RID: 12760 RVA: 0x00132C54 File Offset: 0x00130E54
				public override global::StateStatus StateThink(float delta, global::BaseAIBrain brain, global::BaseEntity entity)
				{
					base.StateThink(delta, brain, entity);
					this.FaceCoverFromEntity();
					
					bool newmounted = ScientistPatrolStates.DoFall(delta, brain, entity,mounted);
					if(mounted && !newmounted){
						StartMovingToCover(entity as HumanNPC);
					}
					mounted =newmounted;
					if(mounted){
						return global::StateStatus.Finished;
					}
					
					if (this.status == global::StateStatus.Error)
					{
						return this.status;
					}
					if(lastSpot == entity.transform.position){
						timeStuck+=delta;
					}else{
						timeStuck=0;
					}
					lastSpot = entity.transform.position;
					if (brain.Navigator.Moving && timeStuck<3)
					{
						return global::StateStatus.Running;
					}
					return global::StateStatus.Finished;
				}

				// Token: 0x060031D9 RID: 12761 RVA: 0x00132C88 File Offset: 0x00130E88
				private void FaceCoverFromEntity()
				{
					this.coverFromEntity = this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
					if (this.coverFromEntity == null)
					{
						return;
					}
					this.brain.Navigator.SetFacingDirectionEntity(this.coverFromEntity);
				}

				// Token: 0x040021C7 RID: 8647
				private global::StateStatus status = global::StateStatus.Error;

				// Token: 0x040021C8 RID: 8648
				private global::BaseEntity coverFromEntity;
			}

			
			
			public class BaseRoamState : global::BaseAIBrain.BasicAIState
			{
				public BaseRoamState()
					: base(global::AIState.FollowPath)
				{
				}
				Vector3 target = new Vector3(0,0,0);
				PathList road = null;
				bool mounted = false;
				int roadIdx = 0;
				public override float GetWeight()
				{
					return 0f;
				}

				public override void StateEnter(global::BaseAIBrain brain, global::BaseEntity entity)
				{
					base.StateEnter(brain, entity);
					if((entity as ScientistNPC).GetMounted()!=null){
						mounted=true;
						return;
					}
					List<LootContainer> piles = new List<LootContainer>();
					BaseEntity.Query.Server.GetInSphere(entity.transform.position, 30f, piles);
					BaseEntity target=this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot);
					
					target=FindBuildBlocks( entity,brain, target);
					
					this.nextRoamPositionTime = -1f;
					this.lastDestinationTime = Time.time;
					if(entity.creatorEntity!=null && Vector3.Distance(entity.transform.position,entity.creatorEntity.transform.position)>10f){
						Vector3 insideUnitSphere = global::UnityEngine.Random.insideUnitSphere;
						insideUnitSphere.y = 0f;
						insideUnitSphere.Normalize();
						Vector3 vector = (entity.creatorEntity.transform.position+ (insideUnitSphere * 5));
						brain.Navigator.SetDestination(vector, global::BaseNavigator.NavigationSpeed.Fast, 0f, 20f);
						return;
					}
					GetRoad(brain, entity);
					SetDestination( brain, entity as ScientistNPC);
				}
				public void GetRoad(global::BaseAIBrain brain, global::BaseEntity entity){
					List<PathList> roads = TerrainMeta.Path.Roads;
					PathList best = null;
					float bestDistance = 0;
					int bestIdx = 0;
					foreach(PathList path in roads){
						int i = 0;
						foreach(Vector3 v in path.Path.Points){
							if(Vector3.Distance(entity.transform.position, v)<bestDistance||best==null){
								best=path;
								bestDistance=Vector3.Distance(entity.transform.position, v);
								bestIdx = i;
							}
							i++;
						}
					}
					road=best;
					roadIdx=bestIdx;
				}
				public virtual Vector3 GetDestination()
				{
					return target;
				}

				public virtual Vector3 GetForwardDirection()
				{
					return Vector3.forward;
				}

				public virtual void SetDestination(BaseAIBrain brain,ScientistNPC entity)
				{
					Vector3 insideUnitSphere = global::UnityEngine.Random.insideUnitSphere;
					insideUnitSphere+=entity.eyes.HeadForward();
					insideUnitSphere.y = 0f;
					insideUnitSphere.Normalize();
					Vector3 vector = (entity.transform.position+ (insideUnitSphere * 10));
					if(road!=null){
						int nextIdx = ((roadIdx+1)%road.Path.Points.Length);
						int previousIdx = ((road.Path.Points.Length+roadIdx-1)%road.Path.Points.Length);
						if(Vector3.Distance(road.Path.Points[nextIdx],vector)<Vector3.Distance(road.Path.Points[previousIdx],vector)){
							int i = 0;
							for(i = 0; i < 5; i++){
								if(nextIdx+i>=(road.Path.Points.Length-1)||Vector3.Distance(road.Path.Points[roadIdx],road.Path.Points[nextIdx+i])>15){
									break;
								}
							}
							roadIdx=nextIdx+i;
						}
						else{
							int i = 0;
							for(i = 0; i < 5; i++){
								if(previousIdx-i<=0||Vector3.Distance(road.Path.Points[roadIdx],road.Path.Points[previousIdx-i])>15){
									break;
								}
							}
							roadIdx=previousIdx-i;
						}
						vector = (road.Path.Points[roadIdx]+ (insideUnitSphere * 5));
					}
					target =vector;
					brain.Navigator.SetDestination(target, global::BaseNavigator.NavigationSpeed.Slow, 0f, 20f);
				}

				public override void DrawGizmos()
				{
					base.DrawGizmos();
					this.brain.PathFinder.DebugDraw();
				}

				public virtual Vector3 GetRoamAnchorPosition()
				{
					if (this.brain.Navigator.MaxRoamDistanceFromHome > -1f)
					{
						return this.brain.Events.Memory.Position.Get(4);
					}
					return this.brain.GetBaseEntity().transform.position;
				}

				public override global::StateStatus StateThink(float delta, global::BaseAIBrain brain, global::BaseEntity entity)
				{
					base.StateThink(delta, brain, entity);
					bool newmounted = ScientistPatrolStates.DoFall(delta, brain, entity,mounted);
					if(mounted&&!newmounted){
						GetRoad(brain, entity);
					}
					mounted=newmounted;
					if(mounted){
						return global::StateStatus.Finished;
					}
					
					bool flag = Time.time - this.lastDestinationTime > 5f;
					if (((Vector3.Distance(this.GetDestination(), entity.transform.position) < 1f || flag) ) && this.nextRoamPositionTime == -1f)
					{
						this.nextRoamPositionTime = Time.time + global::UnityEngine.Random.Range(1f, 3f);
					}
					if (this.nextRoamPositionTime != -1f && Time.time > this.nextRoamPositionTime)
					{
						this.nextRoamPositionTime = -1f;
						this.lastDestinationTime = Time.time;
						if(entity.creatorEntity!=null){
							if(Vector3.Distance(entity.transform.position,entity.creatorEntity.transform.position)>10f){
								Vector3 insideUnitSphere = global::UnityEngine.Random.insideUnitSphere;
								insideUnitSphere.y = 0f;
								insideUnitSphere.Normalize();
								Vector3 vector = (entity.creatorEntity.transform.position + (((entity.creatorEntity.transform.position-entity.transform.position).normalized*2+insideUnitSphere) * 3));
								brain.Navigator.SetDestination(vector, global::BaseNavigator.NavigationSpeed.Normal, 0f, 20f);
							}
						}else{
							this.SetDestination(brain,entity as ScientistNPC);
							
						}
					}
					return global::StateStatus.Running;
				}

				// Token: 0x0400036D RID: 877
				private float nextRoamPositionTime = -1f;

				// Token: 0x0400036E RID: 878
				private float lastDestinationTime;
			}
			
		}
		#endregion
	}
}