#region Using
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
using Oxide.Core.Libraries.Covalence;
using Oxide.Plugins;
using Oxide.Core.Plugins;
using System.Threading;
using Oxide.Core;
#endregion
namespace Oxide.Plugins
{
	[Info("Ninjutsu", "obsol", "0.1.1")]
	[Description("Server debugging tool for modders")]
	public class Ninjutsu : CovalencePlugin
	{
		#region Variables
		private EnvSync _envSync;
		HashSet<BasePlayer> hiphopPlayers = new HashSet<BasePlayer>();
		HashSet<BasePlayer> silly1Players = new HashSet<BasePlayer>();
		HashSet<BasePlayer> silly2Players = new HashSet<BasePlayer>();
		
		List<BaseNetworkable.SaveInfo> bushGhosts = new List<BaseNetworkable.SaveInfo>();
		List<BaseNetworkable.SaveInfo> meanGhosts = new List<BaseNetworkable.SaveInfo>();
		List<Wisp> wisps = new List<Wisp>();
		Dictionary<BasePlayer,long> muteTimeouts = new Dictionary<BasePlayer,long>();
		Dictionary<BasePlayer,long> nightTimeouts = new Dictionary<BasePlayer,long>();
		Dictionary<BasePlayer,long> phaseTimeouts = new Dictionary<BasePlayer,long>();
		Dictionary<BasePlayer,long> bushTimeouts = new Dictionary<BasePlayer,long>();
		Dictionary<BasePlayer,long> ignoreTimeouts = new Dictionary<BasePlayer,long>();
		Dictionary<BaseCombatEntity,long> summonTimeouts = new Dictionary<BaseCombatEntity,long>();
		
		int zombieQueue=0;
		int wolfQueue=0;
		
		int zombieMax=10;
		int wolfMax=10;
		bool cleanSkins = false;
		long muteDuration = 300;
		long nightDuration = 300;
		long phaseDuration = 300;
		long bushDuration = 300;
		long ignoreDuration = 300;
		long summonDuration = 300;
		
		List<BaseCombatEntity> zombiePop=new List<BaseCombatEntity>();
		List<BaseCombatEntity> wolfPop=new List<BaseCombatEntity>();
				
		string[] bushes = {
			"assets/bundled/prefabs/autospawn/resource/v3_bushes_temp/bush_willow_a.prefab",
			"assets/bundled/prefabs/autospawn/resource/v3_bushes_temp/bush_willow_b.prefab",
			"assets/bundled/prefabs/autospawn/resource/v3_bushes_temp/bush_willow_c.prefab",
			"assets/bundled/prefabs/autospawn/resource/v3_bushes_temp/bush_willow_d.prefab",
			"assets/bundled/prefabs/autospawn/resource/v3_bushes_arid_desert/mormon_tea_a.prefab",
			"assets/bundled/prefabs/autospawn/resource/v3_bushes_arid_desert/mormon_tea_b.prefab",
			"assets/bundled/prefabs/autospawn/resource/v3_bushes_arid_desert/mormon_tea_c.prefab",
			"assets/bundled/prefabs/autospawn/resource/v3_bushes_arid_desert/mormon_tea_d.prefab",
			"assets/bundled/prefabs/autospawn/resource/v3_bushes_tundra/bush_spicebush_a.prefab",
			"assets/bundled/prefabs/autospawn/resource/v3_bushes_tundra/bush_spicebush_c.prefab"
		};
		string[] means = {
			"assets/rust.ai/agents/bear/bear.prefab",
			"assets/rust.ai/agents/wolf/wolf.prefab",
			"assets/prefabs/npc/scarecrow/scarecrow.prefab",
			"assets/prefabs/npc/scarecrow/scarecrow.prefab",
			"assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_any.prefab",
			"assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_any.prefab",
			"assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_any.prefab",
			"assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_any.prefab",
			"assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_heavy.prefab"
		};
		string[] plants = {
			"assets/bundled/prefabs/autospawn/collectable/berry-black/berry-black-collectable.prefab",
			"assets/bundled/prefabs/autospawn/collectable/berry-blue/berry-blue-collectable.prefab",
			"assets/bundled/prefabs/autospawn/collectable/berry-green/berry-green-collectable.prefab",
			"assets/bundled/prefabs/autospawn/collectable/berry-red/berry-red-collectable.prefab",
			"assets/bundled/prefabs/autospawn/collectable/berry-white/berry-white-collectable.prefab",
			"assets/bundled/prefabs/autospawn/collectable/berry-yellow/berry-yellow-collectable.prefab",
			"assets/bundled/prefabs/autospawn/collectable/hemp/hemp-collectable.prefab",
			"assets/bundled/prefabs/autospawn/collectable/hemp/hemp-collectable.prefab",
			"assets/bundled/prefabs/autospawn/collectable/hemp/hemp-collectable.prefab",
			"assets/bundled/prefabs/autospawn/collectable/hemp/hemp-collectable.prefab",
			"assets/bundled/prefabs/autospawn/collectable/mushrooms/mushroom-cluster-5.prefab",
			"assets/bundled/prefabs/autospawn/collectable/mushrooms/mushroom-cluster-6.prefab",
			"assets/bundled/prefabs/autospawn/collectable/potato/potato-collectable.prefab"
		};
		string[] loots = {
			"assets/bundled/prefabs/radtown/underwater_labs/crate_ammunition.prefab",
			"assets/bundled/prefabs/radtown/underwater_labs/crate_ammunition.prefab",
			"assets/bundled/prefabs/radtown/crate_basic.prefab",
			"assets/bundled/prefabs/radtown/crate_basic.prefab",
			"assets/bundled/prefabs/radtown/crate_basic.prefab",
			"assets/bundled/prefabs/radtown/crate_basic.prefab",
			"assets/bundled/prefabs/radtown/crate_elite.prefab",
			"assets/bundled/prefabs/radtown/underwater_labs/crate_food_1.prefab",
			"assets/bundled/prefabs/radtown/underwater_labs/crate_food_2.prefab",
			"assets/bundled/prefabs/radtown/underwater_labs/crate_medical.prefab",
			"assets/bundled/prefabs/radtown/crate_normal.prefab",
			"assets/bundled/prefabs/radtown/crate_tools.prefab",
			"assets/bundled/prefabs/radtown/loot_barrel_1.prefab",
			"assets/bundled/prefabs/radtown/loot_barrel_2.prefab",
			"assets/bundled/prefabs/radtown/oil_barrel.prefab",
			"assets/content/props/roadsigns/roadsign1.prefab",
			"assets/content/props/roadsigns/roadsign2.prefab",
			"assets/content/props/roadsigns/roadsign3.prefab",
			"assets/content/props/roadsigns/roadsign4.prefab",
			"assets/content/props/roadsigns/roadsign5.prefab",
			"assets/bundled/prefabs/radtown/underwater_labs/tech_parts_2.prefab",
			"assets/bundled/prefabs/radtown/vehicle_parts.prefab"
		};
		#endregion
		#region Loaded
		void OnServerInitialized(){
			timer.Every(121f,() =>{
				weatherVote();
			});
			timer.Every(133f,() =>{
				handleSummons();
			});
			timer.Every(45f,() =>{
				unphase();
			});
			timer.Every(12f,() =>{
				updateTime();
			});
			timer.Every(22f,() =>{
				updateIgnore();
			});
			timer.Every(100f,() =>{
				updateBushes();
			});	
			timer.Every(17f,() =>{
				cleanWisps();
			});
			timer.Every(37f, () =>
			{
				foreach (BasePlayer basePlayer2 in BasePlayer.activePlayerList)
				{
					checkPlayerDancing(basePlayer2);
				}
			});
			
			 CinematicEntity[] oldLights = GameObject.FindObjectsOfType<CinematicEntity>();
			 foreach(CinematicEntity oldLight in oldLights.ToArray()){
				 oldLight.Kill();
			 }
            _envSync = BaseNetworkable.serverEntities.OfType<EnvSync>().FirstOrDefault();
			if (!_envSync.limitNetworking)
				_envSync.limitNetworking = true;
			else
				_envSync.limitNetworking = false;
			for(int i = 0; i < bushes.Length; i++){
				BaseNetworkable.SaveInfo si2 = new BaseNetworkable.SaveInfo();
				BaseEntity be = GameManager.server.CreateEntity(bushes[i], new Vector3(0,0,0), Quaternion.LookRotation(new Vector3(1,0,0), Vector3.up), true);
				be.Spawn();
				si2.msg = Facepunch.Pool.Get<ProtoBuf.Entity>();
				be.Save(si2);
				be.Kill();
				bushGhosts.Add(si2);
			}
			for(int i = 0; i < means.Length; i++){
				BaseNetworkable.SaveInfo si2 = new BaseNetworkable.SaveInfo();
				BaseEntity be = GameManager.server.CreateEntity(means[i], new Vector3(0,0,0), Quaternion.LookRotation(new Vector3(1,0,0), Vector3.up), true);
				be.Spawn();
				si2.msg = Facepunch.Pool.Get<ProtoBuf.Entity>();
				be.Save(si2);
				be.Kill();
				meanGhosts.Add(si2);
			}
			
		}
		#endregion
		class Wisp{
			public BaseEntity[] lights = new BaseEntity[5];
			public BasePlayer owner;
			public long timeout;
			string[] lightStr = {
					"assets/bundled/prefabs/modding/cinematic/cinelights/cinelight_3p_cool.prefab",
					"assets/bundled/prefabs/modding/cinematic/cinelights/cinelight_3p_warm.prefab",
					"assets/bundled/prefabs/modding/cinematic/cinelights/cinelight_point_cool.prefab",
					"assets/bundled/prefabs/modding/cinematic/cinelights/cinelight_point_warm.prefab",
					"assets/bundled/prefabs/modding/cinematic/cinelights/cinelight_point_red.prefab",
					"assets/bundled/prefabs/modding/cinematic/cinelights/cinelight_point_green.prefab"
				};
			
			public Wisp(BasePlayer bp){				
				lights[0]=spawnLight(lightStr[5],bp,new Vector3(0,0,2));
				lights[1]=spawnLight(lightStr[5],bp,new Vector3(0,0,2));
				lights[2]=spawnLight(lightStr[3],bp,new Vector3(-1,0,1));
				lights[3]=spawnLight(lightStr[3],bp,new Vector3(1,0,1));
				lights[4]=spawnLight(lightStr[2],bp,new Vector3(0,0,-2));
			}
			BaseEntity spawnLight(string s, BasePlayer bp, Vector3 offset){
					BaseEntity be = GameManager.server.CreateEntity(s, bp.transform.position+new Vector3(0,2f,0), Quaternion.LookRotation(new Vector3(1,0,0), Vector3.up), true);//
					be.transform.parent=bp.transform;
					be.transform.localPosition+=offset;
					be.syncPosition=true;
					be.Spawn();
					be.syncPosition=true;
					be.SetFlag(BaseEntity.Flags.Reserved1,true);
					be.SetParent(bp,0, true, true);		
					return be;
			}
			public void Kill(){
				lights[0].Kill();
				lights[1].Kill();
				lights[2].Kill();
				lights[3].Kill();
				lights[4].Kill();
			}
		}
		void cleanWisps(){
			foreach(Wisp w in wisps.ToArray()){
				if(w.timeout<System.DateTime.Now.Ticks){
					w.Kill();
					wisps.Remove(w);
				}
			}
		}
		void ForcespawnEnt(BasePlayer player, string entity, bool limitAngle){
			RaycastHit raycastHit;
			if (!Physics.Raycast( player.eyes.HeadRay(), out raycastHit, 5f))
			{
				return ;
			}
			Vector3 lhs;
			float yRotation = 0;
			
			if(limitAngle){
				lhs= Quaternion.LookRotation(raycastHit.normal,  raycastHit.point) * Quaternion.Euler(90, 0, 0) * (Vector3.up)+ new Vector3(0,yRotation,0);
				if (!(Mathf.Acos(Vector3.Dot(lhs- new Vector3(0,yRotation,0), Vector3.up)) < 0.61086524f))
				{
					return ;
				}
			}
			else{
				lhs= Quaternion.LookRotation(raycastHit.normal,  raycastHit.point) * Quaternion.Euler(yRotation, 0, 0) * Vector3.up;
			}
			
			BaseEntity be = GameManager.server.CreateEntity(entity, raycastHit.point, Quaternion.LookRotation(lhs,Vector3.up), true);
			be.Spawn();
			
		}
		#region GetLookingAts
		public Vector3 getNearbyPos(BaseEntity player, float range){								
			Vector3 position = player.transform.position;
			position = new Vector3(position.x+UnityEngine.Random.Range(0,range),0,position.z+UnityEngine.Random.Range(0,range));
			position=new Vector3(position.x,TerrainMeta.HeightMap.GetHeight(position),position.z);	
			return position;
			
		}
		public Vector3 getNearbyPos(Vector3 position, float range){		
			position = new Vector3(position.x+UnityEngine.Random.Range(0,range),0,position.z+UnityEngine.Random.Range(0,range));
			position=new Vector3(position.x,TerrainMeta.HeightMap.GetHeight(position),position.z);	
			return position;
			
		}
		public Vector3 getLookingAtVect(BasePlayer player){			
			RaycastHit hit;
			if (Physics.Raycast(player.eyes.HeadRay(), out hit,1000)){
				return hit.point;
			}
			return new Vector3(0,0,0);
		}		
		public BasePlayer getLookingAtPlayer(BasePlayer player){			
			RaycastHit hit;
			if (Physics.Raycast(player.eyes.HeadRay(), out hit)){
				var entity = hit.GetEntity();
				if (entity != null){if(entity.GetComponent<BasePlayer>()!=null) return entity.GetComponent<BasePlayer>();}
			}
			return null;
		}
		public BaseAnimalNPC getLookingAtAnimal(BasePlayer player){			
			RaycastHit hit;
			if (Physics.Raycast(player.eyes.HeadRay(), out hit)){
				var entity = hit.GetEntity();
				if (entity != null){if(entity.GetComponent<BaseAnimalNPC>()!=null) return entity.GetComponent<BaseAnimalNPC>();}
			}
			return null;
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
		#endregion
		#region Timers
		void checkPlayerDancing(BasePlayer player){
			Item item = player.GetActiveItem();
			if(player.InGesture==false||item==null||!item.name.Contains("Focus")){
				if(hiphopPlayers.Contains(player))
					hiphopPlayers.Remove(player);
				if(silly1Players.Contains(player))
					silly1Players.Remove(player);
				if(silly2Players.Contains(player))
					silly2Players.Remove(player);
			}else{
				float magicCost = 0;
				float magicDmg = 0;	
				switch(player.currentGesture.gestureCommand){
					case "hiphopdance":
						if(!hiphopPlayers.Contains(player))
							hiphopPlayers.Add(player);
						if(silly1Players.Contains(player))
							silly1Players.Remove(player);
						if(silly2Players.Contains(player))
							silly2Players.Remove(player);	
						magicCost +=(5);
						break;
					case "sillydance":
						if(hiphopPlayers.Contains(player))
							hiphopPlayers.Remove(player);
						if(!silly1Players.Contains(player))
							silly1Players.Add(player);
						if(silly2Players.Contains(player))
							silly2Players.Remove(player);	
						magicCost +=(5);									
						break;
					case "sillydance2":
						if(hiphopPlayers.Contains(player))
							hiphopPlayers.Remove(player);
						if(silly1Players.Contains(player))
							silly1Players.Remove(player);
						if(!silly2Players.Contains(player))
							silly2Players.Add(player);		
						magicCost +=(5);					
						break;
					default:
						if(hiphopPlayers.Contains(player))
							hiphopPlayers.Remove(player);
						if(silly1Players.Contains(player))
							silly1Players.Remove(player);
						if(silly2Players.Contains(player))
							silly2Players.Remove(player);
						break;
				}				
				if(item.condition < magicCost){
					player.StopSpectating();
					player.StartSleeping();
					player.EndSleeping();
					player.Hurt((magicCost-item.condition)*magicDmg);
				}
				item.LoseCondition(magicCost);
				
			}
		}
		void handleSummons(){ // done
			
			foreach (BasePlayer basePlayer2 in silly2Players)
			{
				Item item = basePlayer2.GetActiveItem();
				if(item==null||!item.name.Contains("Focus")) return;
				switch(item.name)
				{
					case "Magic Focus": //Loot DONE 
					#region Loot
						//Roll dice, find location, spawn loot
						Vector3 position = getNearbyPos(basePlayer2,30);
						int idx = UnityEngine.Random.Range(0,loots.Length-1);
						BaseEntity be = GameManager.server.CreateEntity(loots[idx], position, Quaternion.LookRotation(new Vector3(1,0,0), Vector3.up), true);
						be.Spawn();
						break;
					#endregion
					case "Natural Focus": //Plants DONE
					#region plants
						//find nearest growable, tick
						BaseEntity[] nearbyPlants = new BaseEntity[10];
						BaseEntity.Query.Server.GetInSphere(basePlayer2.transform.position, 15,nearbyPlants,isCrop);
						BaseEntity nearestPlant = null;
						foreach(BaseEntity bp in nearbyPlants){
							if(bp==null||bp.transform==null)continue;
							if(bp!=basePlayer2&&(nearestPlant==null||Vector3.Distance(bp.transform.position,basePlayer2.transform.position)<Vector3.Distance(nearestPlant.transform.position,basePlayer2.transform.position))){
								nearestPlant=bp;
							}
						}
						if(nearestPlant!=null){
							(nearestPlant as GrowableEntity).RunUpdate();
							(nearestPlant as GrowableEntity).RunUpdate();
							(nearestPlant as GrowableEntity).RunUpdate();
						}
						break;
					#endregion
					case "Corrupted Focus": //Wolves DONE
					#region wolves
						wolfQueue++;
						break;
					#endregion
					case "Techno Focus": //Vehicles DONE
					#region vehicls
						//find nearest vehicle, repair
						BaseEntity[] nearbyCars = new BaseEntity[10];
						BaseEntity.Query.Server.GetInSphere(basePlayer2.transform.position, 15,nearbyCars,isVehicle);
						BaseEntity nearestCar = null;
						foreach(BaseEntity bp in nearbyCars){
							if(bp==null||bp.transform==null)continue;
							if(bp!=basePlayer2&&(nearestCar==null||Vector3.Distance(bp.transform.position,basePlayer2.transform.position)<Vector3.Distance(nearestCar.transform.position,basePlayer2.transform.position))){
								nearestCar=bp;
							}
						}
						if(nearestCar!=null){
							(nearestCar as BaseCombatEntity).Heal(50);
						}
						break;
					#endregion
					case "Spirit Focus": //Zombies DONE
					#region zombies
						//Find a player, find forward, *-5, spawn and warp  
						zombieQueue++;
						break;
					#endregion zombies
				}	
			}
			//Find a player, find forward, *-5, spawn and warp
			string prefab = (UnityEngine.Random.Range(0,1)==0?"assets/prefabs/npc/scarecrow/scarecrow.prefab":"assets/rust.ai/agents/wolf/wolf.prefab");
			BasePlayer[] bps = BasePlayer.allPlayerList.ToArray();
			BasePlayer target = bps[UnityEngine.Random.Range(0,bps.Length-1)];
			Vector3 targetpos = getNearbyPos(target.transform.position-(target.transform.forward*10),5);
			
			BaseEntity beSpawned = null;
			if(wolfQueue>0 && prefab.Contains("wolf") && wolfPop.Count()<wolfMax){
				beSpawned = GameManager.server.CreateEntity(prefab, targetpos, Quaternion.LookRotation(new Vector3(1,0,0), Vector3.up), true);
				wolfQueue--;
				beSpawned.Spawn();
				wolfPop.Add(beSpawned as BaseCombatEntity);
			}
			if(zombieQueue>0 && prefab.Contains("scarecrow") && zombiePop.Count()<zombieMax){
				beSpawned = GameManager.server.CreateEntity(prefab, targetpos, target.transform.rotation, true);
				zombieQueue--;
				beSpawned.Spawn();
				zombiePop.Add(beSpawned as BaseCombatEntity);
			}
			if(beSpawned!=null){
				beSpawned.GetComponent<BaseNavigator>()?.Warp(targetpos);
			}
		}
		void weatherVote(){ //done
			int rain = 0;
			int fog = 0;
			int clear = 0;
			int day = 0;
			int night = 0;
			foreach (BasePlayer basePlayer2 in silly1Players)
			{
				Item item = basePlayer2.GetActiveItem();
				if(item==null||!item.name.Contains("Focus")) return;
				switch(item.name)
				{
					case "Magic Focus": // Day
						day++;
						break;
					case "Natural Focus": // Clear
						clear++;
						break;
					case "Corrupted Focus": // Night
						night++;
						break;
					case "Techno Focus": // Storm
						rain++;
						break;
					case "Spirit Focus": //Fog
						fog++;
						break;
				}
			}
			
			if(rain>0||fog>0||clear>0){
				if(rain>fog){
					if(rain>clear){
						//RAIN
						server.Command("weather.load","Storm");			
					}else{
						//CLEAR
						server.Command("weather.load","Clear");	
					}
				}else{
					if(fog>clear){
						//FOG
						server.Command("weather.load","Fog");
					}else{
						//CLEAR
						server.Command("weather.load","Clear");	
					}
				}
			}
			
			if(day>0||night>0){
				if(day>night){
					server.Command("env.time","9");
				}else{
					server.Command("env.time","21");
				}
			}
		}
		void unphase(){ ///done
			foreach (BasePlayer basePlayer2 in BasePlayer.activePlayerList)
			{
				if(basePlayer2.IsSpectating() && (!phaseTimeouts.ContainsKey(basePlayer2) || phaseTimeouts[basePlayer2] < System.DateTime.Now.Ticks)){					
					basePlayer2.StopSpectating();
					basePlayer2.StartSleeping();
					basePlayer2.EndSleeping();
					if(phaseTimeouts.ContainsKey(basePlayer2)) phaseTimeouts.Remove(basePlayer2);
				}
			}
			
		}
		void updateTime(){		///done
			foreach (BasePlayer basePlayer2 in BasePlayer.activePlayerList)
			{
				if(!nightTimeouts.ContainsKey(basePlayer2) || nightTimeouts[basePlayer2] < System.DateTime.Now.Ticks){
					if(basePlayer2.Connection==null) continue;
					if (Net.sv.write.Start())
					{
						Network.Connection connection = basePlayer2.Connection;
						connection.validate.entityUpdates = connection.validate.entityUpdates + 1;
						BaseNetworkable.SaveInfo saveInfo = new global::BaseNetworkable.SaveInfo
						{
							forConnection = connection,
							forDisk = false
						};
						Net.sv.write.PacketID(Message.Type.Entities);
						Net.sv.write.UInt32(connection.validate.entityUpdates);
						using (saveInfo.msg = Facepunch.Pool.Get<ProtoBuf.Entity>())
						{
							_envSync.Save(saveInfo);
							if (saveInfo.msg.baseEntity == null)
							{
								return;
							}
							if (saveInfo.msg.baseNetworkable == null)
							{
								return;
							}
							saveInfo.msg.ToProto(Net.sv.write);
							_envSync.PostSave(saveInfo);
							Net.sv.write.Send(new SendInfo(connection));
						}
					}
					
					if(nightTimeouts.ContainsKey(basePlayer2)) nightTimeouts.Remove(basePlayer2);
				}
			}
		}
		void updateIgnore(){ //done			
			foreach (BasePlayer basePlayer2 in BasePlayer.activePlayerList)
			{
				if((!ignoreTimeouts.ContainsKey(basePlayer2) || ignoreTimeouts[basePlayer2] < System.DateTime.Now.Ticks)){					
					if(ignoreTimeouts.ContainsKey(basePlayer2)) ignoreTimeouts.Remove(basePlayer2);
						Rust.AI.SimpleAIMemory.RemoveIgnorePlayer(basePlayer2);
				}
			}
		}
		void updateBushes(){ // done
			
			foreach (BasePlayer basePlayer2 in BasePlayer.activePlayerList)
			{
				if((bushTimeouts.ContainsKey(basePlayer2) && bushTimeouts[basePlayer2] < System.DateTime.Now.Ticks)){
					if(bushTimeouts.ContainsKey(basePlayer2)) bushTimeouts.Remove(basePlayer2);
					//move the bushes back to 0 0 0
					foreach(BaseNetworkable.SaveInfo si in bushGhosts){
						if(basePlayer2.Connection==null) continue;
						if (Net.sv.write.Start())
						{
							Network.Connection connection = basePlayer2.Connection;
							connection.validate.entityUpdates = connection.validate.entityUpdates + 1;
							si.msg.baseEntity.pos = new Vector3(0,0,0);
							Net.sv.write.PacketID(Message.Type.Entities);
							Net.sv.write.UInt32(connection.validate.entityUpdates);
							si.msg.ToProto(Net.sv.write);
							Net.sv.write.Send(new SendInfo(connection));
						}
					}
				}
			}
		}
		#endregion
		#region Filters
		bool isPlant(BaseEntity be){
			return (be!=null&& be is CollectibleEntity && (be as CollectibleEntity).IsFood());
		}
		bool isVehicle(BaseEntity be){
			return (be!=null&& (
			be is BaseBoat ||
			be is BaseHelicopterVehicle ||
			be is BaseSubmarine ||
			be is BaseVehicleModule ||
			be is TrainCar 
			));
		}
		bool isCrop(BaseEntity be){
			return (be!=null&& be is GrowableEntity);
		}
		bool isOre(BaseEntity be){
			return (be!=null&& be is OreResourceEntity);
		}
		bool isHuman(BasePlayer bp){
			return bp!=null && (!((bp is HumanNPC && ((bp as HumanNPC).Brain==null||(bp as HumanNPC).Brain.Navigator==null))||bp is NPCShopKeeper)||bp.IsConnected);
		}
		bool isPlayer(BasePlayer bp){
			return bp!=null && bp.IsConnected;
		}
		bool isCorpse(BaseEntity bp){
			return bp!=null && bp is BaseCorpse;
		}
		bool isGenerator(BaseEntity bp){
			return bp!=null &&
			bp is FuelElectricGenerator &&
			bp is ElectricWindmill &&
			bp is ElectricGenerator &&
			bp is ElectricBattery &&
			bp is SolarPanel
			;
		}
		bool isBattery(BaseEntity bp){
			return bp!=null &&
			bp is ElectricBattery
			;
		}
		bool isAnimal(BaseEntity bp){
			return bp!=null && bp is BaseNpc;
		}
		bool isDoor(BaseEntity bp){
			return bp!=null && bp is Door;
		}
		#endregion
		#region Native Hooks
		void OnEntityDeath(BaseCombatEntity entity, HitInfo info){
			if(zombiePop.Contains(entity)){
				zombiePop.Remove(entity);
			}
			if(wolfPop.Contains(entity)){
				wolfPop.Remove(entity);
			}
		}
		void OnItemDropped(Item oldItem, BaseEntity entity){
			if(oldItem==null) return;
			BasePlayer bp = oldItem.GetOwnerPlayer();
			if(bp==null) return;
			BaseOven bo = getLookingAt(bp) as BaseOven;
			if(bo==null || !(bo.transform.name.Contains("campfire"))) return;
			if(!(bo.IsOn() && bo.FindBurnable()!=null)) return;
			Item item = ItemManager.CreateByName("rock", 1, 0UL);
			item.name = "Blank Focus";
			BaseEntity heldEntity = item.GetHeldEntity();
			ulong skinid =0;
			if((oldItem).info.displayName.translated=="Human Skull" || oldItem.name=="Corrupted Focus"){
				(item).name="Corrupted Focus";//https://steamcommunity.com/sharedfiles/filedetails/?id=2879592409
				skinid = (cleanSkins?2879592409:1547263616);
				
			}
			else if((oldItem).info.displayName.translated=="Wolf Skull" || oldItem.name=="Natural Focus"){
				(item).name="Natural Focus";//https://steamcommunity.com/sharedfiles/filedetails/?id=2879594478
				skinid = (cleanSkins?2879594478:2775197508);
				
			}
			else if(oldItem.info.displayName.translated=="Targeting Computer" || oldItem.name=="Techno Focus"){
				(item).name="Techno Focus";//https://steamcommunity.com/sharedfiles/filedetails/?id=2879593582
				skinid = (cleanSkins?2879593582:2018319624);
											
			}
			else if(oldItem.info.displayName.translated=="Pookie Bear" || oldItem.name=="Spirit Focus"){
				(item).name="Spirit Focus";//https://steamcommunity.com/sharedfiles/filedetails/?id=2879596340
				skinid = (cleanSkins?2879596340:1341378161);
											
			}
			else if(oldItem.info.displayName.translated=="Rock" || oldItem.name=="Magic Focus"){
				(item).name="Magic Focus";//https://steamcommunity.com/sharedfiles/filedetails/?id=2879590224
				skinid = (cleanSkins?2879590224:2366530202);
											
			}
			if(item.name!="Blank Focus"){
				item.skin=skinid;//1547263616
				heldEntity.skinID = skinid;
				item.MarkDirty();
				heldEntity.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
				item.CreateWorldObject(bo.transform.position+new Vector3(0,0.5f,0), default(Quaternion), null, 0U);
			}
			oldItem.RemoveFromWorld();
			return;				
				
		}
		bool? CanUseGesture(BasePlayer player, GestureConfig gesture){
			Puts(gesture.gestureCommand);
			float magicCost = 0;
			float magicDmg = 0;	
			bool unphase = false;
			if(player.GetHeldEntity().HasFlag(BaseEntity.Flags.Broken)){
				return null;
			}
			Item item = player.GetActiveItem();
			if(item==null||!item.name.Contains("Focus")) return null;
			switch(item.name)
			{
				case "Magic Focus": //2 left
					switch(gesture.gestureCommand)
					{
						case "clap"://smoke grenade
							#region magic clap
							magicCost +=(10);	
							server.Command("spawn","40mm_grenade_smoke",(getLookingAtVect(player)+new Vector3(7,50,0)).ToString());	
							server.Command("spawn","40mm_grenade_smoke",(getLookingAtVect(player)+new Vector3(-7,50,0)).ToString());	
							server.Command("spawn","40mm_grenade_smoke",(getLookingAtVect(player)+new Vector3(0,50,7)).ToString());	
							server.Command("spawn","40mm_grenade_smoke",(getLookingAtVect(player)+new Vector3(0,50,-7)).ToString());	
							break;	
							#endregion			
						case "ok"://health tea
							#region magic ok 
							magicCost +=(20);	
							player.metabolism.pending_health.value+=50;
							break;
							#endregion
						case "thumbsdown"://wood chest
							#region magic thumbsdown
							ForcespawnEnt(player,"assets/prefabs/deployable/woodenbox/woodbox_deployed.prefab",true);
							
							break;
							#endregion
						case "thumbsup"://ladder
							#region magic thumbsup
							ForcespawnEnt(player,"assets/prefabs/building/ladder.wall.wood/ladder.wooden.wall.prefab",false);
							break;
							#endregion
						case "victory"://molotov
							#region magic victory
							magicCost +=(15);
							server.Command("spawn","grenade.molotov.deployed",(getLookingAtVect(player)+new Vector3(0,200,0)).ToString());	
							 
							break;	
							#endregion		
						case "friendly"://NPCs ignore you if they're not already aware of you
							#region magic friendly
							if(ignoreTimeouts.ContainsKey(player)){
								ignoreTimeouts[player] = System.DateTime.Now.Ticks+(ignoreDuration*1000*System.TimeSpan.TicksPerMillisecond);
							}else{
								ignoreTimeouts.Add(player,System.DateTime.Now.Ticks+(ignoreDuration*1000*System.TimeSpan.TicksPerMillisecond));								
							}
							Rust.AI.SimpleAIMemory.AddIgnorePlayer(player);
							break;
							#endregion 
						case "wave"://warp to bag
							#region magic wave
							magicCost +=(30);	
							SleepingBag nearestBag = null;
							foreach(SleepingBag bag in SleepingBag.FindForPlayer(player.userID, true)){
								if(nearestBag==null||Vector3.Distance(bag.transform.position,player.transform.position)<Vector3.Distance(nearestBag.transform.position,player.transform.position))
									nearestBag=bag;
							}
							player.Teleport(nearestBag.transform.position);							
							break;
							#endregion
						case "sillydance"://Push to day
							#region magic sillydance
							silly1Players.Add(player);
							break;
							#endregion
						case "sillydance2"://respawn loot
							#region magic sillydance2
							silly2Players.Add(player);
							break;
							#endregion
						default: break;
					} break;
				case "Natural Focus": // DONE!
					switch(gesture.gestureCommand)
					{
						case "thumbsup"://Find nearest plant
							#region natural thumbsup
							magicCost +=(10);	
							BaseEntity[] nearbyPlants = new BaseEntity[10];
							BaseEntity.Query.Server.GetInSphere(player.transform.position, 150,nearbyPlants,isPlant);
							BaseEntity nearestPlant = null;
							foreach(BaseEntity bp in nearbyPlants){
								if(bp==null||bp.transform==null)continue;
								if(bp!=player&&(nearestPlant==null||Vector3.Distance(bp.transform.position,player.transform.position)<Vector3.Distance(nearestPlant.transform.position,player.transform.position))){
									nearestPlant=bp;
								}
							}
							if(nearestPlant!=null){
								player.SendConsoleCommand("ddraw.text", 60, Color.white, nearestPlant.transform.position, (nearestPlant.ToString()));
							}
							break;
							#endregion
						case "thumbsdown"://Find nearest ore
							#region natural thumbsdown
							magicCost +=(20);	
							BaseEntity[] nearbyOres = new BaseEntity[10];
							BaseEntity.Query.Server.GetInSphere(player.transform.position, 150,nearbyOres,isOre);
							BaseEntity nearestOre = null;
							foreach(BaseEntity bp in nearbyOres){
								if(bp==null||bp.transform==null)continue;
								if(bp!=player&&(nearestOre==null||Vector3.Distance(bp.transform.position,player.transform.position)<Vector3.Distance(nearestOre.transform.position,player.transform.position))){
									nearestOre=bp;
								}
							}
							if(nearestOre!=null){
								player.SendConsoleCommand("ddraw.text", 60, Color.white, nearestOre.transform.position, (nearestOre.ToString()));
							}
							break;		
							#endregion	
						case "friendly"://Find nearest player
							#region natural friendly
							magicCost +=(30);	
							BasePlayer[] nearbyPlayers = new BasePlayer[100];
							BaseEntity.Query.Server.GetPlayersInSphere(player.transform.position, 300,nearbyPlayers,isHuman);
							BasePlayer nearest = null;
							foreach(BasePlayer bp in nearbyPlayers){
								if(bp==null||bp.transform==null)continue;
								if(bp!=player&&(nearest==null||Vector3.Distance(bp.transform.position,player.transform.position)<Vector3.Distance(nearest.transform.position,player.transform.position))){
									nearest=bp;
								}
							}
							if(nearest!=null){
								player.SendConsoleCommand("ddraw.text", 60, Color.white, nearest.transform.position, (nearest.displayName==null?"[X]":nearest.displayName));
							}
							break;
							#endregion
						case "wave"://Swap with nearby animal
							#region natural wave
							magicCost +=(20);	
							BaseEntity[] nearbyAnimals = new BaseEntity[10];
							BaseEntity.Query.Server.GetInSphere(player.transform.position, 150,nearbyAnimals,isAnimal);
							BaseEntity nearestAnimal = null;
							foreach(BaseEntity bp in nearbyAnimals){
								if(bp==null||bp.transform==null)continue;
								if(bp!=player&&(nearestAnimal==null||Vector3.Distance(bp.transform.position,player.transform.position)<Vector3.Distance(nearestAnimal.transform.position,player.transform.position))){
									nearestAnimal=bp;
								}
							}
							if(nearestAnimal!=null){
								BaseNpc animal = nearestAnimal as BaseNpc;
								BaseNavigator bn = animal.gameObject.GetComponent<BaseNavigator>();
								if(animal!=null&&animal.transform!=null){
									Vector3 oldPos = player.transform.position;
									player.Teleport(nearestAnimal.transform.position);
									if(bn!=null){
										bn.Warp(oldPos);
									}
								}
							}
							break;	
							#endregion
						case "ok":// Tree tea
							#region natural ok
							/*create fake modifer then add to list and mark dirty*/
							magicCost +=(20);			
							ModifierDefintion pm = new ModifierDefintion();
							pm.type=Modifier.ModifierType.Wood_Yield;
							pm.source=Modifier.ModifierSource.Tea;
							pm.value=1.5f;
							pm.duration=30;
							List<ModifierDefintion> listmd = new List<ModifierDefintion>();
							listmd.Add(pm);
							player.modifiers.Add(listmd);
							break;
							#endregion
						case "victory"://Spawn Plants
							#region natural victory
							/*1-3 plants anywhere in 15 m*/
							
							for(int i = 0; i< UnityEngine.Random.Range(1,5);i++){		
								Vector3 position = getNearbyPos(player, 15);
								BaseEntity be = GameManager.server.CreateEntity(plants[UnityEngine.Random.Range(0,plants.Length-1)], position, Quaternion.LookRotation(new Vector3(1,0,0), Vector3.up), true);
								be.Spawn();
							}
							break;
							#endregion
						case "clap"://Stop Bleed
							#region natural clap
							magicCost +=(30);			
							player.metabolism.bleeding.Reset();
							break;
							#endregion
						case "sillydance"://Clear Weather
							#region natural sillydance
							silly1Players.Add(player);
							break;
							#endregion
						case "sillydance2"://Fast Growth
							#region natural sillydance2
							silly2Players.Add(player);
							break;
							#endregion
						default: break;
					} break;
				case "Corrupted Focus": //Done!
					switch(gesture.gestureCommand)
					{
						case "thumbsup": //dehydrate
							#region corrupt thumbsup
							magicCost +=(20);	
							BasePlayer hydratarget = getLookingAtPlayer(player);
							if(hydratarget!=null && hydratarget.metabolism!=null){
								hydratarget.metabolism.hydration.Subtract(40);
							}
							hydratarget.metabolism.SendChangesToClient();
							break;
							#endregion
						case "thumbsdown": //starve
							#region corrupt thumbsdown
							magicCost +=(20);	
							BasePlayer caltgt = getLookingAtPlayer(player);
							if(caltgt!=null && caltgt.metabolism!=null){
								caltgt.metabolism.calories.Subtract(40);
							}
							caltgt.metabolism.SendChangesToClient();
							break;	
							#endregion		
						case "friendly": //radiate
							#region corrupt friendly
							magicCost +=(20);	
							BasePlayer radtgt = getLookingAtPlayer(player);
							if(radtgt!=null && radtgt.metabolism!=null){
								radtgt.metabolism.radiation_poison.Add(10);
							}
							radtgt.metabolism.SendChangesToClient();
							break;
							#endregion
						case "wave": //swap with person
							#region corrupt wave
							magicCost +=(50);	
							BasePlayer[] nearbyPlayers = new BasePlayer[100];
							BaseEntity.Query.Server.GetPlayersInSphere(player.transform.position, 150, nearbyPlayers,isHuman);
							BasePlayer nearest = null;
							foreach(BasePlayer bp in nearbyPlayers){
								if(bp==null||bp.transform==null)continue;
								if(bp!=player&&
									(nearest==null||Vector3.Distance(bp.transform.position,player.transform.position)<Vector3.Distance(nearest.transform.position,player.transform.position))
									){
										nearest=bp;
								}
							}
							if(nearest!=null&&nearest.transform!=null){
								Vector3 oldPos = player.transform.position;
								player.Teleport(nearest.transform.position);
								nearest.Teleport(oldPos);
								if(nearest is HumanNPC && (nearest as HumanNPC).Brain!=null && (nearest as HumanNPC).Brain.Navigator!=null){
									(nearest as HumanNPC).Brain.Navigator.Warp(oldPos);
								}
							}
							break;
							#endregion
						case "ok": //max health tea
							#region corrupt ok
							magicCost +=(20);			
							ModifierDefintion pm = new ModifierDefintion();
							pm.type=Modifier.ModifierType.Max_Health;
							pm.source=Modifier.ModifierSource.Tea;
							pm.value=120f;
							pm.duration=60;
							List<ModifierDefintion> listmd = new List<ModifierDefintion>();
							listmd.Add(pm);
							player.modifiers.Add(listmd);
							break;
							#endregion
						case "victory": //bleed
							#region corrupt victory
							magicCost +=(20);	
							BasePlayer bleedtgt = getLookingAtPlayer(player);
							if(bleedtgt!=null && bleedtgt.metabolism!=null){
								bleedtgt.metabolism.bleeding.Add(10);
							}
							bleedtgt.metabolism.SendChangesToClient();
							break;
							#endregion
						case "clap": //clear self radiation
							#region corrupt clap
							/*heal rads, 1/10 hp plus condition*/
							magicCost +=(20);	
							if(player!=null && player.metabolism!=null){
								player.metabolism.radiation_poison.Subtract(20);
							}
							player.metabolism.SendChangesToClient();
							break;
							#endregion
						case "sillydance": //push to night
							#region corrupt sillydance
							silly1Players.Add(player);
							break;
							#endregion
						case "sillydance2": //spawn wolves near players
							#region corrupt sillydance2
							silly2Players.Add(player);
							break;
							#endregion
						default: break;
					} break;
				case "Techno Focus":// DONE!
					switch(gesture.gestureCommand)
					{
						case "thumbsup": //Refill nearby battery
							#region tech thumbsup 
							//10hp 100 ele
							magicCost +=(50);	
							BaseEntity[] nearbyBatts = new BaseEntity[10];
							BaseEntity.Query.Server.GetInSphere(player.transform.position, 30,nearbyBatts,isBattery);
							BaseEntity nearestBatt = null;
							foreach(BaseEntity bp in nearbyBatts){
								if(bp==null||bp.transform==null)continue;
								if(bp!=player&&(nearestBatt==null||Vector3.Distance(bp.transform.position,player.transform.position)<Vector3.Distance(nearestBatt.transform.position,player.transform.position))){
									nearestBatt=bp;
								}
							}
							if(nearestBatt!=null){
								ElectricBattery eb= (nearestBatt as ElectricBattery);
								eb.rustWattSeconds += (eb.maxCapactiySeconds/10);
							}
							
							break;
							#endregion
						case "thumbsdown": //Breaks a powersource connection
							#region tech thumbsdown
							magicCost +=(30);	
							BaseEntity[] nearbyGens = new BaseEntity[10];
							BaseEntity.Query.Server.GetInSphere(player.transform.position, 30,nearbyGens,isGenerator);
							BaseEntity nearestGen = null;
							foreach(BaseEntity bp in nearbyGens){
								if(bp==null||bp.transform==null)continue;
								if(bp!=player&&(nearestGen==null||Vector3.Distance(bp.transform.position,player.transform.position)<Vector3.Distance(nearestGen.transform.position,player.transform.position))){
									nearestGen=bp;
								}
							}
							if(nearestGen!=null){
								IOEntity io = nearestGen as IOEntity;
								List<IOEntity.IOSlot> ios = new List<IOEntity.IOSlot> ();
								foreach (global::IOEntity.IOSlot ioslot3 in io.outputs)
								{
									if (ioslot3.connectedTo.Get(true) != null)
									{
										ios.Add(ioslot3);	
									}									
								}
								int max = ios.Count()-1;
								int idx = UnityEngine.Random.Range(0,max);
								ios[idx].Clear();
							}
							
							break;	
							#endregion		
						case "friendly": //Find Vehicle
							#region tech friendly
							magicCost +=(20);	
							BaseEntity[] nearestVcls = new BaseEntity[10];
							BaseEntity.Query.Server.GetInSphere(player.transform.position, 150,nearestVcls,isVehicle);
							BaseEntity nearestVcl = null;
							foreach(BaseEntity bp in nearestVcls){
								if(bp==null||bp.transform==null)continue;
								if(bp!=player&&(nearestVcl==null||Vector3.Distance(bp.transform.position,player.transform.position)<Vector3.Distance(nearestVcl.transform.position,player.transform.position))){
									nearestVcl=bp;
								}
							}
							if(nearestVcl!=null){
								player.SendConsoleCommand("ddraw.text", 60, Color.white, nearestVcl.transform.position, (nearestVcl.ToString()));
								
							}
							
							
							break;
							#endregion
						case "wave": //Hop to telephone
							#region tech wave
							//check for phone list
							PhoneController[] phones = TelephoneManager.allTelephones.Values.ToArray();
							int idxPhone = UnityEngine.Random.Range(0,phones.Length-1);
							if(phones == null || phones[idxPhone] == null || phones[idxPhone].ParentEntity == null ||phones[idxPhone].ParentEntity.transform) return null;
							player.Teleport(phones[idxPhone].ParentEntity.transform.position);
							break;
							#endregion
						case "ok": //Scrap tea
							#region tech ok
							magicCost +=(20);			
							ModifierDefintion pm = new ModifierDefintion();
							pm.type=Modifier.ModifierType.Scrap_Yield;
							pm.source=Modifier.ModifierSource.Tea;
							pm.value=120f;
							pm.duration=60;
							List<ModifierDefintion> listmd = new List<ModifierDefintion>();
							listmd.Add(pm);
							player.modifiers.Add(listmd);
							break;
							#endregion
						case "victory": //tesla dmg
							#region tech victory 
							//rip out dmg code
							BasePlayer[] nearbyShocked = new BasePlayer[10];
							BaseEntity.Query.Server.GetPlayersInSphere(player.transform.position, 10,nearbyShocked,isPlayer);
							for (int i = 0; i < nearbyShocked.Length; i++)
							{
								global::BaseCombatEntity component = nearbyShocked[i].GetComponent<global::BaseCombatEntity>();
								if (component)
								{
									component.OnAttacked(new global::HitInfo(player, component, Rust.DamageType.ElectricShock, 15));
								}
							}
							player.OnAttacked(new global::HitInfo(player, player, Rust.DamageType.ElectricShock, 10));
							break;
							#endregion
						case "clap": //open door
							#region tech clap
							//takes dmg, takes ful focus, failure does nothing but hurt
							BaseEntity[] nearbyDoors = new BaseEntity[10];
							BaseEntity.Query.Server.GetInSphere(player.transform.position, 20,nearbyDoors,isDoor);
							int idxDoor = UnityEngine.Random.Range(0,nearbyDoors.Length-1);
							(nearbyDoors[idxDoor] as Door).SetOpen(true);
							player.OnAttacked(new global::HitInfo(nearbyDoors[idxDoor], player, Rust.DamageType.ElectricShock, 20));
							break;
							#endregion
						case "sillydance": //storm
							#region tech dance 1
							silly1Players.Add(player);
							break;
							#endregion
						case "sillydance2": //repair vehicle
							#region tech dance 2
							silly2Players.Add(player);
							break;
							#endregion
						default: break;
					} break;
				case "Spirit Focus":// Done!
					switch(gesture.gestureCommand)
					{
						case "shrug": //Self day / AOE night
							magicCost+=10;
							Wisp w = new Wisp(player);
							w.timeout = System.DateTime.Now.Ticks + (System.TimeSpan.TicksPerMillisecond * 1000 * 300);
							wisps.Add(w );
							break;
						case "point": //Self day / AOE night
							magicCost+=10;
							BasePlayer wispTgt = getLookingAtPlayer(player);
							if (wispTgt==null) return null;
							Wisp w2 = new Wisp(wispTgt);
							w2.timeout = System.DateTime.Now.Ticks + (System.TimeSpan.TicksPerMillisecond * 1000 * 300);
							wisps.Add(w2);
							break;
						case "thumbsup": //Self day / AOE night
							#region spirit thumbsup
							//adds to timeouts, during timeout self-time
							magicCost +=(20);			
							DateTime _sunnyDayDate = new DateTime(2024, 1, 25);
							
							if(nightTimeouts.ContainsKey(player)){
								nightTimeouts[player] = 0;
							}
							
							BasePlayer[] nearbyPlayers = new BasePlayer[12];
							BaseEntity.Query.Server.GetPlayersInSphere(getLookingAtVect(player), 30 ,nearbyPlayers,isHuman);
							foreach(BasePlayer bp in nearbyPlayers){
								
								if(bp==null||bp.transform==null)continue;
								if(bp!=player){
									if(bp.Connection==null) continue;
									if (Net.sv.write.Start())
									{
										Network.Connection connection = bp.Connection;
										connection.validate.entityUpdates = connection.validate.entityUpdates + 1;
										BaseNetworkable.SaveInfo saveInfo = new global::BaseNetworkable.SaveInfo
										{
											forConnection = connection,
											forDisk = false
										};
										Net.sv.write.PacketID(Message.Type.Entities);
										Net.sv.write.UInt32(connection.validate.entityUpdates);
										using (saveInfo.msg = Facepunch.Pool.Get<ProtoBuf.Entity>())
										{
											_envSync.Save(saveInfo);
											saveInfo.msg.environment.dateTime = _sunnyDayDate.AddHours(19.5f).ToBinary();
											saveInfo.msg.environment.fog = 1;
											saveInfo.msg.environment.rain = 1;
											saveInfo.msg.environment.clouds = 1;
											if (saveInfo.msg.baseEntity == null)
											{
												return null;
											}
											if (saveInfo.msg.baseNetworkable == null)
											{
												return null;
											}
											saveInfo.msg.ToProto(Net.sv.write);
											_envSync.PostSave(saveInfo);
											Net.sv.write.Send(new SendInfo(connection));
										}
									}
								}else{
									if (Net.sv.write.Start())
									{
										Network.Connection connection = bp.Connection;
										connection.validate.entityUpdates = connection.validate.entityUpdates + 1;
										BaseNetworkable.SaveInfo saveInfo = new global::BaseNetworkable.SaveInfo
										{
											forConnection = connection,
											forDisk = false
										};
										Net.sv.write.PacketID(Message.Type.Entities);
										Net.sv.write.UInt32(connection.validate.entityUpdates);
										using (saveInfo.msg = Facepunch.Pool.Get<ProtoBuf.Entity>())
										{
											_envSync.Save(saveInfo);
											saveInfo.msg.environment.dateTime = _sunnyDayDate.AddHours(6.8f	).ToBinary();
											saveInfo.msg.environment.fog = 0;
											saveInfo.msg.environment.rain = 0;
											saveInfo.msg.environment.clouds = 0;
											if (saveInfo.msg.baseEntity == null)
											{
												return null;
											}
											if (saveInfo.msg.baseNetworkable == null)
											{
												return null;
											}
											saveInfo.msg.ToProto(Net.sv.write);
											_envSync.PostSave(saveInfo);
											Net.sv.write.Send(new SendInfo(connection));
										}
									}									
								}
							}
							break;
							#endregion
						case "thumbsdown": //bush
							#region spirit thumbsdown
							//adds to timeouts, during timeout bushes
							
							if(bushTimeouts.ContainsKey(player)){
								bushTimeouts[player]=0;		
							}
							
							BasePlayer[] nearbyPlayersbush = new BasePlayer[12];
							BaseEntity.Query.Server.GetPlayersInSphere(getLookingAtVect(player), 15 ,nearbyPlayersbush,isHuman);
							foreach(BasePlayer bp in nearbyPlayersbush){
								if(bushTimeouts.ContainsKey(bp)){
									bushTimeouts[bp]=bushDuration + System.DateTime.Now.Ticks;		
								}else{
									bushTimeouts.Add(bp,bushDuration + System.DateTime.Now.Ticks);
								}
								foreach(BaseNetworkable.SaveInfo si in bushGhosts){
									if(bp.Connection==null) continue;
									if (Net.sv.write.Start())
									{
										Network.Connection connection = bp.Connection;
										connection.validate.entityUpdates = connection.validate.entityUpdates + 1;
										Vector3 bushposition = getNearbyPos(bp,10);
										si.msg.baseEntity.pos =bushposition;
										Net.sv.write.PacketID(Message.Type.Entities);
										Net.sv.write.UInt32(connection.validate.entityUpdates);
										si.msg.ToProto(Net.sv.write);
										Net.sv.write.Send(new SendInfo(connection));
									}
								}
								
							}
							
							break;		
							//Spawn those bushes
							#endregion
						case "friendly": //mute			
							#region spirit friendly
							if(muteTimeouts.ContainsKey(player)){
								muteTimeouts[player]=0;		
							}
							BasePlayer[] nearbyPlayersmute = new BasePlayer[12];
							BaseEntity.Query.Server.GetPlayersInSphere(getLookingAtVect(player), 15 ,nearbyPlayersmute,isHuman);
							foreach(BasePlayer bp in nearbyPlayersmute){
								if(muteTimeouts.ContainsKey(bp)){
									muteTimeouts[bp]=muteDuration+System.DateTime.Now.Ticks;		
								}else{
									muteTimeouts.Add(bp,muteDuration+System.DateTime.Now.Ticks);		
								}
							}
							
							//add to timeouts - until timeout block speech and chat
							break;
							#endregion
						case "wave": //warp to nearest corpse
							#region spirit wave
							magicCost +=(10);	
							BaseEntity[] nearbyCorpses = new BaseEntity[10];
							BaseEntity.Query.Server.GetInSphere(player.transform.position, 150,nearbyCorpses,isCorpse);
							BaseEntity nearestCorpse = null;
							foreach(BaseEntity bp in nearbyCorpses){
								if(bp==null||bp.transform==null)continue;
								if(bp!=player&&(nearestCorpse==null||Vector3.Distance(bp.transform.position,player.transform.position)<Vector3.Distance(nearestCorpse.transform.position,player.transform.position))){
									nearestCorpse=bp;
								}
							}
							if(nearestCorpse!=null){
								player.Teleport(nearestCorpse.transform.position);
							}
							break;
							#endregion
						case "ok": //Ore tea
							#region spirit ok 
							magicCost +=(20);			
							ModifierDefintion pm = new ModifierDefintion();
							pm.type=Modifier.ModifierType.Ore_Yield;
							pm.source=Modifier.ModifierSource.Tea;
							pm.value=120f;
							pm.duration=60;
							List<ModifierDefintion> listmd = new List<ModifierDefintion>();
							listmd.Add(pm);
							player.modifiers.Add(listmd);
							break;
							break;
							#endregion
						case "victory": //Phase shift
							#region spirit victory
							if(phaseTimeouts.ContainsKey(player)){
								phaseTimeouts[player]=System.DateTime.Now.Ticks+(System.TimeSpan.TicksPerMillisecond * 1000 * phaseDuration);		
							}else{								
								phaseTimeouts.Add(player,System.DateTime.Now.Ticks+(System.TimeSpan.TicksPerMillisecond * 1000 * phaseDuration));		
							}
							if(player.IsSpectating()){
								magicCost +=(30);	
								unphase=true;
							}
							else{
								magicCost +=(10);	
								player.StartSpectating();
							}
							break;
							#endregion
						case "clap": // jump
							#region spirit clap
							if(player.IsSpectating()){
								if(item.condition <= 5){
									magicDmg = 2;
									player.StopSpectating();
									player.StartSleeping();
									player.EndSleeping();
								}
								magicCost +=(5);
								item.MarkDirty();
								
							}								
							else{
								magicCost +=(20);	
							}
							player.inventory.ServerUpdate(0.1f);
							Vector3 position = ((getLookingAtVect(player)+new Vector3(0,0,0)));
							player.Teleport(new Vector3(position.x,TerrainMeta.HeightMap.GetHeight(position)+10,position.z));
							break;
							#endregion test
						case "sillydance": //Fog
							#region spirit dance 1
							silly1Players.Add(player);
							break;
							#endregion
						case "sillydance2"://Spawn zombies near players
							#region spirit dance 2
							silly2Players.Add(player);
							break;
							#endregion
						default: break;
					} break;
				default:break;
			}
			if(item.condition < magicCost){
				unphase=true;
				player.Hurt((magicCost-item.condition)*magicDmg);
			}
			item.LoseCondition(magicCost);
			if(unphase){				
				player.StopSpectating();
				player.StartSleeping();
				player.EndSleeping();
			}
			return null;
		}
		object OnPlayerTick(BasePlayer player, PlayerTick msg, bool wasPlayerStalled){
			if(!player.IsSpectating()) return null;
			Vector3 tickViewAngles=msg.inputState.aimAngles;
			bool flag2 = tickViewAngles != player.viewAngles;


			if (flag2)
			{
				player.viewAngles = tickViewAngles;
				player.transform.rotation = Quaternion.identity;
				player.transform.hasChanged = true;
				player.eyes.NetworkUpdate(Quaternion.Euler(player.viewAngles));
				player.NetworkPositionTick();
			}
			return null;
		}
		object OnPlayerVoice(BasePlayer player, Byte[] data){
			if(muteTimeouts.ContainsKey(player) &&muteTimeouts[player] > System.DateTime.Now.Ticks)
				return false;
			return null;
		}
		#endregion
	}
}

/*				

server.Command("weather.load","Storm");		
magicCost +=(10);

	server.Command("weather.load","Fog");	
	magicCost +=(10);			

server.Command("weather.load","Clear");		
magicCost +=(10);		

	magicCost +=(30);	
	server.Command("env.time","22");
	if (_envSync.limitNetworking)
		_envSync.limitNetworking = false;

magicCost +=(30);	
server.Command("env.time","8.5");	
if (_envSync.limitNetworking)
	_envSync.limitNetworking = false;

	magicCost +=(40);	
	ProtoBuf.BaseEntity be2 = meanGhosts[3].msg.baseEntity;
	if(be2!=null){be2.pos = player.transform.position;}
	if (Network.Net.sv.write.Start()){
	Network.Connection connection = player.Connection;
	connection.validate.entityUpdates = connection.validate.entityUpdates + 1U;
	Network.Net.sv.write.PacketID(Message.Type.Entities);
	Network.Net.sv.write.UInt32(connection.validate.entityUpdates);
	meanGhosts[3].msg.ToProto(Network.Net.sv.write);
	Network.Net.sv.write.Send(new SendInfo(connection));
	}
*/