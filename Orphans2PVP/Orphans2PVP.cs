using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Rust;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

/*
TODO:
Some visual way to know a base is PVP
*/

namespace Oxide.Plugins
{
    [Info("Orphans2PVP", "obsol", "0.2.2")]
    [Description("Ties into TruePVE to turn abandoned buildings into raidable, pvp zones after a delay. Supports Clans as well")]
    class Orphans2PVP : RustPlugin
    {
		long minuteMultiplier = 60*1000;
		long day = 86400;
		string[] sanityPhases = {"existingBuildings","cleanClans","stateSwitching"};
		int phaseIdx = -1;
		
		bool doPhase(){
			phaseIdx = (phaseIdx+1)%sanityPhases.Length;
			return doPhase(sanityPhases[phaseIdx]);
		}
		bool doPhase(string s){
			switch(s){
				case "existingBuildings":
					Puts("Cleaning out destroyed buildings");
				
					return existingBuildings();
					break;
				case "cleanClans":
					Puts("Updating clan states");
				
					return cleanClans();
					break;
				case "stateSwitching":
					Puts("Advising of newly protected and PVP buildings");
				
					return stateSwitching();
					break;
				default:
					return false;
					break;
			}
		}
		
		bool existingBuildings(){
			foreach (string s in config.protectionTimeouts.Keys.ToArray()){
				uint id = 0;
				UInt32.TryParse ( s.Split(':')?[1], out id);
				if(id!=0&&BuildingManager.server.GetBuilding(id)==null){
					config.protectionTimeouts.Remove(s);
				}
			}
			foreach (string s in config.protectionCooldowns.Keys.ToArray()){
				uint id = 0;
				UInt32.TryParse ( s.Split(':')?[1], out id);
				if(id!=0&&BuildingManager.server.GetBuilding(id)==null){
					config.protectionCooldowns.Remove(s);
				}
				
			}
			foreach (string s in config.protectionState.Keys.ToArray()){
				uint id = 0;
				UInt32.TryParse ( s.Split(':')?[1], out id);
				if(id!=0&&BuildingManager.server.GetBuilding(id)==null){
					config.protectionState.Remove(s);
				}
				
			}
			foreach (string s in config.buildingRefreshers.Keys.ToArray()){
				uint id = 0;
				UInt32.TryParse ( s.Split(':')?[1], out id);
				if(id!=0&&BuildingManager.server.GetBuilding(id)==null){
					config.buildingRefreshers.Remove(s);
				}
				
			}
			foreach (string s in config.refresherBuildings.Keys){					
				foreach (string t in config.refresherBuildings[s].ToArray()){
					uint id = 0;
					UInt32.TryParse ( t.Split(':')?[1], out id);
					if(id!=0&&BuildingManager.server.GetBuilding(id)==null){
						config.refresherBuildings[s].Remove(t);//
					}
					
				}
			}
			return true;
		}
		bool cleanClans(){
			
			foreach (string s in config.buildingRefreshers.Keys.ToArray()){
				uint id = 0;
				UInt32.TryParse ( s.Split(':')?[1], out id);
				BuildingManager.Building bldg = BuildingManager.server.GetBuilding(id);
				if(id!=0&&bldg!=null){
					BuildingPrivlidge bp = bldg.GetDominatingBuildingPrivilege();
					if(bp!=null){
						ulong owner = bp.OwnerID;						
						foreach (string t in config.buildingRefreshers[s].ToArray()){
							ulong id2 = 0;
							UInt64.TryParse( t, out id2);
							if(id2==0){
								config.buildingRefreshers[s].Remove(t);
								if(config.refresherBuildings.ContainsKey(t)){
									if(config.refresherBuildings[t].Contains(s)){
										config.refresherBuildings[t].Remove(s);
									}
								}
							}
						}
						string u = (string)Interface.CallHook("GetClanOf", new object[] {owner});
						if(u!=null&&u!=""){
							config.buildingRefreshers[s].Add(u);
							if(!config.refresherBuildings.ContainsKey(u)){
								config.refresherBuildings.Add(u, new List<string>());
							}
							if(!config.buildingRefreshers[s].Contains(u))
								config.buildingRefreshers[s].Add(u);
							if(!config.refresherBuildings[u].Contains(s))
								config.refresherBuildings[u].Add(s);
							
						}
					}
				}
			}
			return true;
		}
		bool stateSwitching(){
			foreach(string s in config.protectionTimeouts.Keys.ToArray()){
				if(config.protectionTimeouts[s]> System.DateTime.Now.Ticks){
					if(config.protectionCooldowns.ContainsKey(s)&&
					config.protectionCooldowns[s]<System.DateTime.Now.Ticks){
						if(!config.protectionState.ContainsKey(s)){
							config.protectionState.Add(s,true);
						}
						if(config.protectionState[s]){
							
							uint id = 0;
							UInt32.TryParse ( s.Split(':')?[1], out id);
							if(id!=0&&BuildingManager.server.GetBuilding(id)!=null){		
								BuildingPrivlidge bp = BuildingManager.server.GetBuilding(id).GetDominatingBuildingPrivilege();
								if(bp!=null){
									ConVar.Chat.Broadcast("Building with ID "+(s.Split(':')[1])+" at position "+getGrid(bp.transform.position)+" turned PVP", "Orphans2PVP", "#ca7", 0UL);
								}
							}
						}
						config.protectionState[s]=false;
					}else{						
						if(!config.protectionState.ContainsKey(s)){
							config.protectionState.Add(s,false);
						}
						if(!config.protectionState[s]){
							
							uint id = 0;
							UInt32.TryParse ( s.Split(':')?[1], out id);
							if(id!=0&&BuildingManager.server.GetBuilding(id)!=null){		
								BuildingPrivlidge bp = BuildingManager.server.GetBuilding(id).GetDominatingBuildingPrivilege();
								if(bp!=null){
									ConVar.Chat.Broadcast("Building with ID "+(s.Split(':')[1])+" at position "+getGrid(bp.transform.position)+" protected", "Orphans2PVP", "#ca7", 0UL);
								}
							}
						}
						config.protectionState[s]=true;
					}
				}else{
					if(!config.protectionState.ContainsKey(s)){
						config.protectionState.Add(s,true);
					}
					if(config.protectionState[s]){
						
						uint id = 0;
						UInt32.TryParse ( s.Split(':')?[1], out id);
						if(id!=0&&BuildingManager.server.GetBuilding(id)!=null){		
							BuildingPrivlidge bp = BuildingManager.server.GetBuilding(id).GetDominatingBuildingPrivilege();
							if(bp!=null){
								ConVar.Chat.Broadcast("Building with ID "+(s.Split(':')[1])+" at position "+getGrid(bp.transform.position)+" protected", "Orphans2PVP", "#ca7", 0UL);
							}
						}
					}
					config.protectionState[s]=false;
					
				}
			}
			return true;
		}
	
		
		
		Dictionary<BasePlayer, long> pvpTimeouts = new Dictionary<BasePlayer, long>();
		private Configuration config;
			class Configuration{
				[JsonProperty("timeoutTime", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public long timeoutTime = 30;
				[JsonProperty("cooldownTime", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public long cooldownTime = 5;
				
				[JsonProperty("protectionTimeouts", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public Dictionary<string,long> protectionTimeouts = new Dictionary<string,long>();
				[JsonProperty("protectionCooldowns", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public Dictionary<string,long> protectionCooldowns = new Dictionary<string,long>();
				[JsonProperty("protectionState", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public Dictionary<string,bool> protectionState = new Dictionary<string,bool>();
				[JsonProperty("buildingRefreshers", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public Dictionary<string, List<string>> buildingRefreshers = new Dictionary<string, List<string>>();
				[JsonProperty("refresherBuildings", ObjectCreationHandling = ObjectCreationHandling.Replace)]
				public Dictionary<string, List<string>> refresherBuildings = new Dictionary<string, List<string>>();					
				
				
				public Dictionary<BaseCombatEntity.Faction, Dictionary<string,Vector3>> newFactionSpawns = new Dictionary<BaseCombatEntity.Faction, Dictionary<string,Vector3>>();
				public string ToJson() => JsonConvert.SerializeObject(this);				
				public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
			}
			protected override void LoadDefaultConfig() => config = new Configuration();
			protected override void LoadConfig(){
				base.LoadConfig();
				try{
					config = Config.ReadObject<Configuration>();
					if (config == null) throw new JsonException();
					if (! config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys)){
						SaveConfig();}
				}
				catch{
					LoadDefaultConfig();}
			}
			protected override void SaveConfig(){
				Config.WriteObject(config, true);
			}
		
		string getGrid(Vector3 pos) {
			char letter = 'A';
			var x = Mathf.Floor((pos.x+(ConVar.Server.worldsize/2)) / 146.3f);
			var z = (Mathf.Floor(ConVar.Server.worldsize/146.3f))-Mathf.Floor((pos.z+(ConVar.Server.worldsize/2)) / 146.3f) - 1;
			letter = (char)(((int)letter)+(x%26));
			if(x>25){
				return $"{letter}{letter}{z}";
			}else{
				return $"{letter}{z}";
			}
		}
		object CanEntityTakeDamage(BaseCombatEntity entity, HitInfo hitinfo){
			if(hitinfo.Initiator == null || hitinfo.HitEntity == null) return null;
			BuildingPrivlidge bp = hitinfo.Initiator.GetBuildingPrivilege(hitinfo.Initiator.WorldSpaceBounds());
			bool? result = null;
			if(bp!=null){
				string s = "Building:"+bp.GetBuilding().ID.ToString();
				if(config.protectionTimeouts.ContainsKey(s) &&
					(config.protectionTimeouts[s]< System.DateTime.Now.Ticks || (
					config.protectionCooldowns.ContainsKey(s) && config.protectionCooldowns[s] > System.DateTime.Now.Ticks
					)
				)){
					if(hitinfo.Initiator is BasePlayer){
						if(pvpTimeouts.ContainsKey(hitinfo.Initiator as BasePlayer)){
							pvpTimeouts[hitinfo.Initiator as BasePlayer]= System.DateTime.Now.Ticks+ (System.TimeSpan.TicksPerMillisecond * minuteMultiplier *  config.cooldownTime);
						}else{							
							pvpTimeouts.Add(hitinfo.Initiator as BasePlayer, System.DateTime.Now.Ticks+ (System.TimeSpan.TicksPerMillisecond * minuteMultiplier *  config.cooldownTime));
							ConsoleNetwork.SendClientCommand((hitinfo.Initiator as BasePlayer).net.connection, "chat.add2", new object[] { 0, 0,"You are in a PVP zone - PVP timeout enabled for you","Orphans2PVP", "#ca7", 1f });
						}
					}
					if(hitinfo.HitEntity is BasePlayer){
						if(pvpTimeouts.ContainsKey(hitinfo.HitEntity as BasePlayer)){
							pvpTimeouts[hitinfo.HitEntity as BasePlayer]= System.DateTime.Now.Ticks+ (System.TimeSpan.TicksPerMillisecond * minuteMultiplier *  config.cooldownTime);
						}else{							
							pvpTimeouts.Add(hitinfo.HitEntity as BasePlayer, System.DateTime.Now.Ticks+ (System.TimeSpan.TicksPerMillisecond * minuteMultiplier *  config.cooldownTime));
							ConsoleNetwork.SendClientCommand((hitinfo.HitEntity as BasePlayer).net.connection, "chat.add2", new object[] { 0, 0,"You are in a PVP zone - PVP timeout enabled for you","Orphans2PVP", "#ca7", 1f });
						}
					}
					result=true;
				}
				if(config.protectionCooldowns.ContainsKey(s) && config.protectionCooldowns[s] < System.DateTime.Now.Ticks && config.protectionCooldowns[s]!=0){			
		
					ConVar.Chat.Broadcast("Building with ID "+(s.Split(':')[1])+" at position "+getGrid(bp.transform.position)+" protected", "Orphans2PVP", "#ca7", 0UL);
					config.protectionCooldowns[s]=0;
				}
			}
			if(hitinfo.HitEntity is BasePlayer && pvpTimeouts.ContainsKey(hitinfo.HitEntity as BasePlayer)){
				if(pvpTimeouts[hitinfo.HitEntity as BasePlayer]>System.DateTime.Now.Ticks){							
					if(hitinfo.Initiator is BasePlayer){
						if(pvpTimeouts.ContainsKey(hitinfo.Initiator as BasePlayer)){
							pvpTimeouts[hitinfo.Initiator as BasePlayer]= System.DateTime.Now.Ticks+ (System.TimeSpan.TicksPerMillisecond * minuteMultiplier *  config.cooldownTime);
						}else{							
							pvpTimeouts.Add(hitinfo.Initiator as BasePlayer, System.DateTime.Now.Ticks+ (System.TimeSpan.TicksPerMillisecond * minuteMultiplier *  config.cooldownTime));
							ConsoleNetwork.SendClientCommand((hitinfo.Initiator as BasePlayer).net.connection, "chat.add2", new object[] { 0, 0,"That player was PVP, and now so are you","Orphans2PVP", "#ca7", 1f });

						}
					}
					result=true;
				}else{
					pvpTimeouts.Remove(hitinfo.HitEntity as BasePlayer);
					ConsoleNetwork.SendClientCommand((hitinfo.HitEntity as BasePlayer).net.connection, "chat.add2", new object[] { 0, 0,"You are no longer PVP","Orphans2PVP", "#ca7", 1f });
					ConsoleNetwork.SendClientCommand((hitinfo.Initiator as BasePlayer).net.connection, "chat.add2", new object[] { 0, 0,"That player is no longer PVP - hit player","Orphans2PVP", "#ca7", 1f });
				}
			
			}					
			if(hitinfo.Initiator is BasePlayer){
				if(pvpTimeouts.ContainsKey(hitinfo.Initiator as BasePlayer) && pvpTimeouts[hitinfo.Initiator as BasePlayer]<System.DateTime.Now.Ticks){
					pvpTimeouts.Remove(hitinfo.Initiator as BasePlayer);
					ConsoleNetwork.SendClientCommand((hitinfo.Initiator as BasePlayer).net.connection, "chat.add2", new object[] { 0, 0,"You are no longer PVP","Orphans2PVP", "#ca7", 1f });
				}
			}
			return result;
		}
		
		void OnServerInitialized()
		{
			///clans = (Clans)Manager.GetPlugin("Clans");	
			//*
			timer.Every(100f, () =>
			{
				doPhase();//
			});		//*/
            Subscribe(nameof(CanEntityTakeDamage));
			doPhase();
			doPhase();
			doPhase();
		}
		
		void addRefresher(BuildingPrivlidge entity, BasePlayer player)
		{
			string bID = "Building:"+entity.GetBuilding().ID.ToString();
			if(config.protectionTimeouts.ContainsKey(bID)){
				config.protectionTimeouts[bID]= System.DateTime.Now.Ticks+ (System.TimeSpan.TicksPerMillisecond * minuteMultiplier *  config.timeoutTime);
				
			}else{
				config.protectionTimeouts.Add(bID, System.DateTime.Now.Ticks+ (System.TimeSpan.TicksPerMillisecond * minuteMultiplier *  config.timeoutTime));
			}
			string clan = //*(
			(string)Interface.CallHook("GetClanOf", new object[] {player});
			/*/
			"";
			//*/
			if(config.buildingRefreshers.ContainsKey(bID)){
				if(! config.buildingRefreshers[bID].Contains(player.userID.ToString())){
					config.buildingRefreshers[bID].Add(player.userID.ToString());
				}
				if(clan!=null&&clan!=""){
					if(!config.buildingRefreshers[bID].Contains(clan)){
						config.buildingRefreshers[bID].Add(clan);
					}					
				}
				
			}else{
				config.buildingRefreshers.Add(bID, new List<string>());
				config.buildingRefreshers[bID].Add(player.userID.ToString());
				if(clan!=null&&clan!=""){
					config.buildingRefreshers[bID].Add(clan);
				}
			}
			if(config.refresherBuildings.ContainsKey(player.userID.ToString())){
				if(! config.refresherBuildings[player.userID.ToString()].Contains(bID)){
					config.refresherBuildings[player.userID.ToString()].Add(bID);
				}
				
			}else{
				config.refresherBuildings.Add(player.userID.ToString(), new List<string>());
				config.refresherBuildings[player.userID.ToString()].Add(bID);
			}
			
			if(clan!=null&&clan!=""){
				if(config.refresherBuildings.ContainsKey(clan)){
					if(! config.refresherBuildings[clan].Contains(bID)){
						config.refresherBuildings[clan].Add(bID);
					}
					
				}else{
					config.refresherBuildings.Add(clan, new List<string>());
					config.refresherBuildings[clan].Add(bID);
				}
			}
		}
		object OnConstructionPlace(BuildingPrivlidge entity, Construction component, Construction.Target constructionTarget, BasePlayer player)
		{
			if(player!=null){
				addRefresher(entity,player);
			}
			return null;
		}
		
		
		void Loaded(){	
			LoadConfig();
		}
		void Unload(){
			SaveConfig();
		}
				
		void OnPlayerDisconnected(BasePlayer player, string reason)
		{
			List<string> buildings = new List<string>();
			string clan = //*
			(string)Interface.CallHook("GetClanOf", new object[] {player});
			/*/
			"";
			//*/
			if(clan!=null&&clan!=""){
					
				if(config.refresherBuildings.ContainsKey(clan)){
					buildings.AddRange(config.refresherBuildings[clan]);
				}
			}
			if(config.refresherBuildings.ContainsKey(player.userID.ToString())){
				buildings.AddRange(config.refresherBuildings[player.userID.ToString()]);
			}	
			foreach(string s in buildings){
				if(config.protectionTimeouts.ContainsKey(s)){
					config.protectionTimeouts[s]= System.DateTime.Now.Ticks;
					
				}else{
					config.protectionTimeouts.Add(s, System.DateTime.Now.Ticks + (System.TimeSpan.TicksPerMillisecond * minuteMultiplier *  config.timeoutTime));
				}
			}
		}
		void OnPlayerConnected(BasePlayer player)
		{
			List<string> buildings = new List<string>();
			string clan = //*
			(string)Interface.CallHook("GetClanOf", new object[] {player});
			/*/
			"";
			//*/
			if(clan!=null&&clan!=""){
					
				if(config.refresherBuildings.ContainsKey(clan)){
					buildings.AddRange(config.refresherBuildings[clan]);
				}
			}
			if(config.refresherBuildings.ContainsKey(player.userID.ToString())){
				buildings.AddRange(config.refresherBuildings[player.userID.ToString()]);
			}			
			
			foreach(string s in buildings){
				if(config.protectionTimeouts.ContainsKey(s)){
					if(config.protectionTimeouts[s]< System.DateTime.Now.Ticks){
						if(config.protectionCooldowns.ContainsKey(s)){
							config.protectionCooldowns[s]=System.DateTime.Now.Ticks + (System.TimeSpan.TicksPerMillisecond * minuteMultiplier *  config.cooldownTime);
						}else{
							config.protectionCooldowns.Add(s,System.DateTime.Now.Ticks + (System.TimeSpan.TicksPerMillisecond * minuteMultiplier *  config.cooldownTime));							
						}
					}
					config.protectionTimeouts[s]= System.DateTime.Now.Ticks + (System.TimeSpan.TicksPerMillisecond * minuteMultiplier *  config.timeoutTime);
					
				}else{
					config.protectionTimeouts.Add(s, System.DateTime.Now.Ticks + (System.TimeSpan.TicksPerMillisecond * minuteMultiplier *  config.timeoutTime));
				}
			}
			
		}
		
		
		void OnEntityKill(BuildingPrivlidge entity)
		{
			string bID = "Building:"+entity.GetBuilding().ID.ToString();
			if(config.buildingRefreshers.ContainsKey(bID)){
				foreach(string s in config.buildingRefreshers[bID].ToArray()){
					if(config.refresherBuildings.ContainsKey(s)){
						if(config.refresherBuildings[s].Contains(bID)){
							config.refresherBuildings[s].Remove(bID);
						}
					}
					config.buildingRefreshers[bID].Remove(s);
				}		
			}
			if(config.protectionTimeouts.ContainsKey(bID)){	
				config.protectionTimeouts[bID]=0;
			}
			if(config.protectionCooldowns.ContainsKey(bID)){	
				config.protectionCooldowns[bID]=System.DateTime.Now.Ticks + (System.TimeSpan.TicksPerMillisecond * minuteMultiplier *  config.cooldownTime);
			}
		}
		
		object OnCupboardClearList(BuildingPrivlidge privilege, BasePlayer player)
		{
			string bID = "Building:"+privilege.GetBuilding().ID.ToString();
			if(config.buildingRefreshers.ContainsKey(bID)){
				foreach(string s in config.buildingRefreshers[bID].ToArray()){
					if(config.refresherBuildings.ContainsKey(s)){
						if(config.refresherBuildings[s].Contains(bID)){
							config.refresherBuildings[s].Remove(bID);
						}
					}
					config.buildingRefreshers[bID].Remove(s);
				}		
			}	
			if(config.protectionCooldowns.ContainsKey(bID)){	
				config.protectionCooldowns[bID]=System.DateTime.Now.Ticks + (System.TimeSpan.TicksPerMillisecond * minuteMultiplier *  config.cooldownTime);
			}else{
				config.protectionCooldowns.Add(bID,System.DateTime.Now.Ticks + (System.TimeSpan.TicksPerMillisecond * minuteMultiplier *  config.cooldownTime));
			}
			if(!config.protectionState.ContainsKey(bID)){
				config.protectionState.Add(bID,false);
			}else{
				config.protectionState[bID]=false;
			}
			ConsoleNetwork.SendClientCommand(player.net.connection, "chat.add2", new object[] { 0, 0,"You have PVP'd this house","Orphans2PVP", "#ca7", 1f });
			ConVar.Chat.Broadcast("Building with ID "+(bID.Split(':')[1])+" at position "+getGrid(privilege.transform.position)+" was made PVP", "Orphans2PVP", "#ca7", 0UL);
			return null;
		}
		
		object OnCupboardAuthorize(BuildingPrivlidge privilege, BasePlayer player)
		{//
			
			string bID = "Building:"+privilege.GetBuilding().ID.ToString();
			addRefresher(privilege,player);
			if(config.protectionTimeouts.ContainsKey(bID)){	
				config.protectionTimeouts[bID]=System.DateTime.Now.Ticks + (System.TimeSpan.TicksPerMillisecond * minuteMultiplier *  config.timeoutTime);
			}else{	
				config.protectionTimeouts.Add(bID,System.DateTime.Now.Ticks + (System.TimeSpan.TicksPerMillisecond * minuteMultiplier *  config.timeoutTime));
			}
			ConsoleNetwork.SendClientCommand(player.net.connection, "chat.add2", new object[] { 0, 0,"You have refreshed this house - PVP will be disabled in "+config.cooldownTime.ToString("0")+" minutes","Orphans2PVP", "#ca7", 1f });
			return null;
		}
		/*
		void UpdateZoneTypes(){
			foreach(string s in config.protectionTimeouts.Keys){
				string bID = s;
				if(!config.protectionState.ContainsKey(s)){		
					config.protectionState.Add(s,false);
				}
				if(config.protectionTimeouts[s]< System.DateTime.Now.Ticks && config.protectionState[s]){	
						config.protectionState[s]=false;
						ConVar.Chat.Broadcast("Building with ID "+(bID.Split(':')[1])+" no longer protected", "Orphans2PVP", "#ca7", 0UL);
				}else{
					if((!config.protectionCooldowns.ContainsKey(s) || config.protectionCooldowns[s]< System.DateTime.Now.Ticks)){
						config.protectionState[s]=true;
						ConVar.Chat.Broadcast("Building with ID "+(bID.Split(':')[1])+" protected", "Orphans2PVP", "#ca7", 0UL);
						
					}
				}
			}
		}*/
	}
}
