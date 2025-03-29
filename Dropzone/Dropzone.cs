/*README:

Chat Commands:
/clearorbit		 - Removes the orbit points (MonumentInfo) that are actively loaded (native and custom)
/clearLZ		 - Removes active landing zones (native and custom)
/clearDZ		 - Removes active drop zones (native and custom)
/clearall		 - Remoes all drop, landing, and orbit points on the map
/Switchcustomonly- Flips whether or not it'll delete native points on boot

/deleteLZ		 - deletes a custom landing point within 15u of you
/deleteDZ		 - deletes a custom drop point within 15u of you
/deleteOrbit	 - deletes a custom orbit point within 15u of you

/setorbit		 - sets an orbit points
/setlz			 - sets a landing zones
/setdz			 - sets a drop zone
/setpoint		 - makes a unified point (all three points as one)

/clearcustom	 - flushes all custom points
/callhere		 - removes all active points in the scene, creates all three point types at player position, and calls ch47 to position.

Config:
CustomOnly 		 - removes all the built-in orbit and drop points, only loading the points you set
Land 			 - unimplemented - creates landing zones defined in config
Drop 			 - unimplemented - creates drop points defined in config
Orbit 			 - unimplemented - creates orbit points in config
The other three	 - vector lists, best to leave them alone and use chat commands to set and delete points

The permission (there's just one)
oxide.grant group admin dropzone.admin

Workflow to remove all orbit points and create a single point
/clearall
/setpoint


*/


#region using
	using Convert = System.Convert;
	using Network;
	using Newtonsoft.Json;
	using System;
	using System.Collections.Generic;
	using System.Collections;
	using System.Linq;
	using System.Text;
	using Oxide.Core.Libraries.Covalence;
	using Oxide.Plugins;
	using Oxide.Core.Plugins;
	using Oxide.Core;
	using UnityEngine; 
	using UnityEngine.SceneManagement;
	using UnityEngine.AI;
	using Rust.Ai;
	using Oxide.Ext.RustEdit;
	using Oxide.Ext.RustEdit.NPC;
#endregion
namespace Oxide.Plugins{
	[Info("Dropzone", "obsol", "0.0.1")]
	[Description("Creates/Deletes defined CH47 drop, orbit, and landing zones")]
	public class Dropzone : RustPlugin{
	
		public class Configuration{
			[JsonProperty("CustomOnly", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public bool CustomOnly=false;
			[JsonProperty("Land", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public bool Land=false; //Requires navmesh hacks to play nice
			[JsonProperty("Drop", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public bool Drop=true;
			[JsonProperty("Orbit", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public bool Orbit=true;
			[JsonProperty("DropPoints", ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public List<Vector3> DropPoints = new List<Vector3>();
			[JsonProperty("LandingZones", ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public List<Vector3> LandingZone = new List<Vector3>();
			[JsonProperty("OrbitPoints", ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public List<Vector3> OrbitPoints = new List<Vector3>();
			
			
			public string ToJson() => JsonConvert.SerializeObject(this);				
			public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
		}
		Configuration config = new Configuration();
		protected override void LoadDefaultConfig() => config = new Configuration();
		protected override void LoadConfig(){
			base.LoadConfig();
			try{
				config = Config.ReadObject<Configuration>();
				if (config == null) throw new JsonException();
				if (!config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys)){
					Puts("Configuration appears to be outdated; updating and saving");
					SaveConfig();
					}
			}
			catch{
				Puts($"Configuration file {Name}.json is invalid; using defaults");
				LoadDefaultConfig();
				
			}
		}
		protected override void SaveConfig(){
			Puts($"Configuration changes saved to {Name}.json");
			Config.WriteObject(config, true);
		}
		List<CH47DropZone> customDZ = new List<CH47DropZone>();
		List<CH47LandingZone> customLZ = new List<CH47LandingZone>();
		List<MonumentInfo> customOrbit = new List<MonumentInfo>();

		void Init(){
				permission.RegisterPermission("dropzone.admin", this);
			}
		void OnServerInitialized(){
			LoadConfig();
			if(config.CustomOnly){
				clearLZ();
				clearDZ();
				clearOrbit();
			}
			initPoints();
		}
		
		void initPoints(bool drop = true,bool land=true,bool orbit=true){
			
			if(config.Drop && drop){
				foreach(Vector3 v in config.DropPoints){
					setDZ(v);
				}
			}
			if(config.Land && land){
				foreach(Vector3 v in config.LandingZone){
					setLZ(v);
				}
			} 
			if(config.Orbit && orbit){
				foreach(Vector3 v in config.OrbitPoints){
					setOrbit(v);
				}
			}
		}
		
		[ChatCommand("ClearLZ")] void ClearLZ(BasePlayer player, string command, string[] args){
			if(permission.UserHasPermission(player.UserIDString, "dropzone.admin")){
				clearLZ();
				delLZ();
				SendReply(player,"All landing zones cleared");
				}
		}
		[ChatCommand("ClearDZ")] void ClearDZ(BasePlayer player, string command, string[] args){
			
			if(permission.UserHasPermission(player.UserIDString, "dropzone.admin")){
				clearDZ();
				delDZ();
				SendReply(player,"All drop zones cleared");
				}
		}
		[ChatCommand("ClearOrbit")] void ClearOrbit(BasePlayer player, string command, string[] args){
			
			if(permission.UserHasPermission(player.UserIDString, "dropzone.admin")){
				clearOrbit();
				delOrbit();
				SendReply(player,"All orbits cleared");
				}
		}
		[ChatCommand("ClearAll")] void ClearAll(BasePlayer player, string command, string[] args){
			
			if(permission.UserHasPermission(player.UserIDString, "dropzone.admin")){
				config.CustomOnly = true;
				clearLZ();
				clearDZ();
				clearOrbit();
				delLZ();
				delDZ();
				delOrbit();
				SendReply(player,"Flushed everything, CustomOnly set to true");
			}
		}
		[ChatCommand("SwitchCustomOnly")] void SwitchCustomOnly(BasePlayer player, string command, string[] args){
			if(permission.UserHasPermission(player.UserIDString, "dropzone.admin")){
			
				config.CustomOnly = !config.CustomOnly;
				SaveConfig();
				SendReply(player,"CustomOnly switched to "+config.CustomOnly.ToString());
			}
		}
		
		
		[ChatCommand("DeleteLZ")] void DeleteLZ(BasePlayer player, string command, string[] args){
			if(permission.UserHasPermission(player.UserIDString, "dropzone.admin"))
				delLZ(player.transform.position);
		}
		[ChatCommand("DeleteDZ")] void DeleteDZ(BasePlayer player, string command, string[] args){
			
			if(permission.UserHasPermission(player.UserIDString, "dropzone.admin"))
				delDZ(player.transform.position);
		}
		[ChatCommand("DeleteOrbit")] void DeleteOrbit(BasePlayer player, string command, string[] args){
			
			if(permission.UserHasPermission(player.UserIDString, "dropzone.admin"))
				delOrbit(player.transform.position);
		}
		
		
		[ChatCommand("SetLZ")] void SetLZ(BasePlayer player, string command, string[] args){
			
			if(permission.UserHasPermission(player.UserIDString, "dropzone.admin")){
				setLZ(player.transform.position);
				config.LandingZone.Add(player.transform.position);
				SaveConfig();
				SendReply(player,"Landing Zone created at "+(player.transform.position.ToString()));	
			}
			
	
		}
		[ChatCommand("SetDZ")] void SetDZ(BasePlayer player, string command, string[] args){
			if(permission.UserHasPermission(player.UserIDString, "dropzone.admin")){
					
				setDZ(player.transform.position);
				config.DropPoints.Add(player.transform.position);
				SaveConfig();
				SendReply(player,"Dropzone created at "+(player.transform.position.ToString()));
			}


		}
		[ChatCommand("SetOrbit")] void SetOrbit(BasePlayer player, string command, string[] args){
			if(permission.UserHasPermission(player.UserIDString, "dropzone.admin")){
					
				setOrbit(player.transform.position);
				config.OrbitPoints.Add(player.transform.position);
				SaveConfig();
				SendReply(player,"Orbit created at "+(player.transform.position.ToString()));
			}
		}
		[ChatCommand("SetPoint")] void SetPoint(BasePlayer player, string command, string[] args){
			if(permission.UserHasPermission(player.UserIDString, "dropzone.admin")){
					
				setLZ(player.transform.position);
				config.LandingZone.Add(player.transform.position);
				setDZ(player.transform.position);
				config.DropPoints.Add(player.transform.position);
				setOrbit(player.transform.position);
				config.OrbitPoints.Add(player.transform.position);
				SaveConfig();
				SendReply(player,"Unified point created at "+(player.transform.position.ToString()));
					
			}
			
		}
		
		
		[ChatCommand("ClearCustom")] void ClearCustom(BasePlayer player, string command, string[] args){
			if(permission.UserHasPermission(player.UserIDString, "dropzone.admin")){
				delLZ();
				delDZ();
				delOrbit();
				SaveConfig();
				SendReply(player,"All custom points deleted");
			}
		}
		[ChatCommand("CallHere")] void CallHere(BasePlayer player, string command, string[] args){
			if(permission.UserHasPermission(player.UserIDString, "dropzone.admin")){
					
				clearLZ();
				clearDZ();
				clearOrbit();
				setLZ(player.transform.position);
				setDZ(player.transform.position);
				setOrbit(player.transform.position);
				
				ConVar.Entity.svspawn("ch47scientists.entity", player.transform.position+new Vector3(0,50,0), new Vector3(0,0,0));
				SendReply(player,"Wiped all points and created point at "+(player.transform.position.ToString()) + " - CH47 on the way");
			}
		}
		
		
		
		public void delLZ(Vector3 v){
			foreach(Vector3 v2 in config.LandingZone.ToArray()){
				if(Vector3.Distance(v,v2)<15){
					config.LandingZone.Remove(v2);
				}
			}
			foreach(CH47LandingZone clz in customLZ.ToArray()){
				if(Vector3.Distance(v,clz.transform.position)<15){
					GameObject.Destroy(clz);
				}
			}
			SaveConfig();
		}
		public void delDZ(Vector3 v){
			foreach(Vector3 v2 in config.DropPoints.ToArray()){
				if(Vector3.Distance(v,v2)<15){
					config.DropPoints.Remove(v2);
				}
			}
			foreach(CH47DropZone cdz in customDZ.ToArray()){
				if(Vector3.Distance(v,cdz.transform.position)<15){
					GameObject.Destroy(cdz);
				}
			}
			SaveConfig();
		}
		public void delOrbit(Vector3 v){
			foreach(Vector3 v2 in config.OrbitPoints.ToArray()){
				if(Vector3.Distance(v,v2)<15){
					config.OrbitPoints.Remove(v2);
				}
			}
			foreach(MonumentInfo mi in customOrbit.ToArray()){
				if(Vector3.Distance(v,mi.transform.position)<15){
					GameObject.Destroy(mi);
				}
			}
			SaveConfig();
		}
		
		
		
		public void delLZ(){
			config.LandingZone= new List<Vector3>();
			foreach(CH47LandingZone clz in customLZ.ToArray()){
				GameObject.Destroy(clz);
			}
			SaveConfig();
		}
		public void delDZ(){
			config.DropPoints= new List<Vector3>();
			foreach(CH47DropZone cdz in customDZ.ToArray()){
				GameObject.Destroy(cdz);
			}
			SaveConfig();
		}
		public void delOrbit(){
			config.OrbitPoints= new List<Vector3>();
			foreach(MonumentInfo mi in customOrbit.ToArray()){
				GameObject.Destroy(mi);
			}
			SaveConfig();
		}
		
		
		
		void setLZ(Vector3 position){
			GameObject go = new GameObject("CustomLandzone");
			go.transform.position=position;
			CH47LandingZone cdz = go.AddComponent(typeof (CH47LandingZone)) as CH47LandingZone;
			cdz.Awake();		
			customLZ.Add(cdz);
			
		}
		void setDZ(Vector3 position){
			GameObject go = new GameObject("CustomDropzone");
			go.transform.position=position;
			CH47DropZone cdz = go.AddComponent(typeof (CH47DropZone)) as CH47DropZone;
			cdz.Awake();	
			customDZ.Add(cdz);		
		}
		void setOrbit(Vector3 position){
			GameObject go = new GameObject("CustomOrbit");
			go.transform.position=position;
			MonumentInfo mi = go.AddComponent(typeof(MonumentInfo)) as MonumentInfo;
			mi.displayPhrase = new Translate.Phrase("Custom Orbit","Custom Orbit");
			mi.shouldDisplayOnMap = true;
			mi.Bounds = new Bounds(position,new Vector3(50,20,50));
			mi.HasNavmesh=false;
			mi.IsSafeZone=false;
			mi.Tier = (MonumentTier)7;
			customOrbit.Add(mi);
			
		}
	
		void setPoint(Vector3 position){
			setLZ(position);
			setDZ(position);
			setOrbit(position);
		}
	
	
		void clearLZ(){		
			foreach(CH47LandingZone s in GameObject.FindObjectsOfType<CH47LandingZone>()){
				GameObject.Destroy(s);				
			}
		}
		void clearDZ(){
			foreach(CH47DropZone s in GameObject.FindObjectsOfType<CH47DropZone>()){
				GameObject.Destroy(s);
			}	
		}
		void clearOrbit(){
			TerrainMeta.Path.Monuments.Clear();	
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