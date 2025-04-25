
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
    [Info("Roaming Scientists", "obsol", "0.0.2")]
    [Description("Spawns a squad of modified scientists - they roam the road network, duck behind junkpiles for cover (ideally, close and out of your LoS), switch between junkpiles, and chase pretty aggressively. None of this orbiting business")]
    internal class RoamingScientists : CovalencePlugin
    {
		
		
		public ConfigData config;

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
			
			le = new LoadoutEntry();
			le.itemname="hazmatsuittwitch";
			le.amount=1;
			lo = new Loadout();
			lo.entries.Add(le);
			config.Outfits.Add(lo);
			
		}

		
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
			
			ConVar.Entity.EntitySpawnRequest spawnEntityFromName = ConVar.Entity.GetSpawnEntityFromName("scientistnpc_junkpile");//
			Vector3 ius = (UnityEngine.Random.insideUnitSphere);
			ius.y=0;
			ius.Normalize();
			
			global::ScientistNPC baseEntity = (global::GameManager.server.CreateEntity(spawnEntityFromName.PrefabName, getLookingAtPoint(basePlayer)+(ius*5),Quaternion.AngleAxis(5.5f, Vector3.up), true) as ScientistNPC);
			if(baseEntity==null){Puts("Nope");return null;}		
			
			baseEntity.Spawn();
			baseEntity.creatorEntity=owner;
			timer.Once(owner == null?0.1f:0.2f,()=>{
				
				baseEntity.Brain.Navigator.MaxRoamDistanceFromHome =-1;
				baseEntity.Brain.states[AIState.FollowPath]=new BaseRoamState();
				baseEntity.Brain.states[AIState.TakeCover]=new TakeCoverState();
				baseEntity.Brain.states[AIState.Chase]=new BaseChaseState();
				baseEntity.Brain.SwitchToState(AIState.FollowPath,0);
				
				baseEntity.Brain.AIDesign.GetFirstStateContainerOfType(AIState.Combat).Events[4].TriggerStateContainerID=-1;
				baseEntity.Brain.AIDesign.GetFirstStateContainerOfType(AIState.Combat).Events[6].TriggerStateContainerID=-1;
				baseEntity.Brain.AIDesign.GetFirstStateContainerOfType(AIState.Chase).Events[3] = BaseAIEvent.CreateEvent(AIEventType.InAttackRange);
				baseEntity.Brain.AIDesign.GetFirstStateContainerOfType(AIState.Chase).Events[3].ID=3;
				baseEntity.Brain.AIDesign.GetFirstStateContainerOfType(AIState.Chase).Events[3].TriggerStateContainerID=1;
				foreach(BaseAIEvent evt in baseEntity.Brain.AIDesign.GetFirstStateContainerOfType(AIState.Chase).Events){
					Puts(""+evt.EventType + " : " + evt.ID);
				}
				//
				/*
				BaseAIEvent attackevent = BaseAIEvent.CreateEvent(AIEventType.AttackTick);
				attackevent.TriggerStateContainerID=-1;
				attackevent.ID=4;
				baseEntity.Brain.AIDesign.GetFirstStateContainerOfType(AIState.Chase).Events.Add(attackevent);
				
				
				BaseAIEvent attackedevent = BaseAIEvent.CreateEvent(AIEventType.Attacked);
				attackedevent.TriggerStateContainerID=2;
				attackedevent.ID=5;
				baseEntity.Brain.AIDesign.GetFirstStateContainerOfType(AIState.Chase).Events.Add(attackedevent);
				*/
				
				baseEntity.Brain.Senses.targetLostRange=100f;//
				if(owner!=null){
					baseEntity.Brain.Senses.Memory.Players=owner.Brain.Senses.Memory.Players;
				}
				baseEntity.inventory.containerBelt.Clear();
				foreach(LoadoutEntry le in config.Belts[0].entries){
					Item cassetteItem = ItemManager.CreateByName(le.itemname, le.amount, le.skinid);
					baseEntity.inventory.containerBelt.GiveItem(cassetteItem);
					baseEntity.UpdateActiveItem(cassetteItem.uid);
					AttackEntity atEnt = cassetteItem.heldEntity.Get(true) as AttackEntity;
					if(atEnt!=null){
						atEnt.effectiveRange = atEnt.effectiveRange<10?10f:atEnt.effectiveRange;
					}
				}
				baseEntity.inventory.containerWear.Clear();
				foreach(LoadoutEntry le in config.Outfits[0].entries){
					Item cassetteItem = ItemManager.CreateByName(le.itemname, le.amount, le.skinid);
					baseEntity.inventory.containerWear.GiveItem(cassetteItem);
				}
				Puts("Creator: "+baseEntity.creatorEntity);
			});
			return baseEntity;
		}
		void OnEntitySpawned(JunkPile entity)
		{
			UnityEngine.AI.NavMeshObstacle nmo = entity.gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>();
			nmo.enabled = false;
			nmo.carving = true;
			nmo.shape= UnityEngine.AI.NavMeshObstacleShape.Capsule;
			nmo.height=20;
			nmo.radius=10;
			nmo.size=new Vector3(7,7,7);
			nmo.enabled = true;
			Puts("OnEntitySpawned works!");
		}
		void OnEntitySpawned(ResourceEntity entity)
		{
			UnityEngine.AI.NavMeshObstacle nmo = entity.gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>();
			nmo.enabled = false;
			nmo.carving = true;
			nmo.shape= UnityEngine.AI.NavMeshObstacleShape.Capsule;
			nmo.height=20;
			nmo.radius=3;
			nmo.size=new Vector3(3,7,3);
			nmo.enabled = true;
			Puts("OnEntitySpawned works!");
		}
		
		public Vector3 getLookingAtPoint(BasePlayer player){			
			RaycastHit hit;
			if (Physics.Raycast(player.eyes.HeadRay(), out hit)){
				Vector3 entity = hit.point;
				return entity;
			}
			return new Vector3(0,0,0);
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
			
		}
		void OnServerInitialized(){
			
		}
		void Unload(){
			
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

			// Token: 0x06000D3D RID: 3389 RVA: 0x00046CC0 File Offset: 0x00044EC0
			public override void StateEnter(global::BaseAIBrain brain, global::BaseEntity baseentity)
			{
				base.StateEnter(brain, baseentity);
				if(this.brain==null){
					this.brain=brain;
				}
				if(!(baseentity is ScientistNPC)){return;}
				ScientistNPC entity = (baseentity as ScientistNPC);
				BasePlayer coverFromEntity = this.brain.Events.Memory.Entity.Get(this.brain.Events.CurrentInputMemorySlot) as BasePlayer;
				Vector3 vector = (coverFromEntity ? coverFromEntity.transform.position : (entity.transform.position + entity.LastAttackedDir * 10f));
				
				/*
				global::AIInformationZone informationZone = entity.GetInformationZone(entity.transform.position);
				if (informationZone == null)
				{
					return false;
				}*/
				float num = ((entity.SecondsSinceAttacked < 2f) ? 2f : 0f);
				float bestCoverPointMaxDistance = this.brain.Navigator.BestCoverPointMaxDistance;
				List<JunkPile> piles = new List<JunkPile>();
				BaseEntity.Query.Server.GetInSphere(vector, 10, piles);
				
				
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
				this.Stop();
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
			// Token: 0x060031D8 RID: 12760 RVA: 0x00132C54 File Offset: 0x00130E54
			public override global::StateStatus StateThink(float delta, global::BaseAIBrain brain, global::BaseEntity entity)
			{
				base.StateThink(delta, brain, entity);
				this.FaceCoverFromEntity();
				
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
			int roadIdx = 0;
			public override float GetWeight()
			{
				return 0f;
			}

			public override void StateEnter(global::BaseAIBrain brain, global::BaseEntity entity)
			{
				base.StateEnter(brain, entity);
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
				brain.Navigator.SetDestination(vector, global::BaseNavigator.NavigationSpeed.Slow, 0f, 20f);
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
}