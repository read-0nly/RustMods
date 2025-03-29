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
    [Info("Autoclone", "obsol", "1.0.0")]
    [Description("Adds a machine that clones the plant within it. Intended for use with Autofarm, but not required. ORiginal idea comes from S0faKingAwes0me")]
    public class Autoclone : CovalencePlugin
    {
		#region top-level variables
		//Self instance
		public static Autoclone instance;
		//Loaded config
		public static Configuration config;
		
		//Tracks the cloners built by players
		public static Dictionary <ulong,List<Cloner>> PlayerCloners = new Dictionary <ulong,List<Cloner>> ();
		
		//Looping index  for cloner processing
		int clonerIdx = 0;		
		#endregion
		
		#region classes
		//Class representing a cloning machine
		public class Cloner : UnityEngine.MonoBehaviour{
			//Mapping of clone -> preview entity
			public static Dictionary<string, string> plantPreviews =  
					new Dictionary<string, string>()
					{
						{"Blue Berry Clone","assets/bundled/prefabs/autospawn/collectable/berry-blue/berry-blue-collectable.prefab"},
						{"Green Berry Clone","assets/bundled/prefabs/autospawn/collectable/berry-green/berry-green-collectable.prefab"},
						{"Red Berry Clone","assets/bundled/prefabs/autospawn/collectable/berry-red/berry-red-collectable.prefab"},
						{"Yellow Berry Clone","assets/bundled/prefabs/autospawn/collectable/berry-yellow/berry-yellow-collectable.prefab"},
						{"White Berry Clone","assets/bundled/prefabs/autospawn/collectable/berry-white/berry-white-collectable.prefab"},
						{"Black Berry Clone","assets/bundled/prefabs/autospawn/collectable/berry-black/berry-black-collectable.prefab"},
						{"Hemp Clone","assets/bundled/prefabs/autospawn/collectable/hemp/hemp-collectable.prefab"},
						{"Corn Clone","assets/bundled/prefabs/autospawn/collectable/corn/corn-collectable.prefab"},
						{"Pumpkin Plant Clone","assets/bundled/prefabs/autospawn/collectable/pumpkin/pumpkin-collectable.prefab"},
						{"Potato Clone","assets/bundled/prefabs/autospawn/collectable/potato/potato-collectable.prefab"}
					}; 
			
			//static global list of cloners for the cloning cycle (this is what the idx enumerates)
			public static List<Cloner> cloners = new List<Cloner>();
			// Reference to the box itself
			public StorageContainer self = null;
			//Reference to the preview plant
			public CollectibleEntity plantPreview = null;
			//Reference to the pump
			public FluidSwitch pump = null;
			//Reference to the light
			public SimpleLight clonelight = null;
			//Reference to the igniter
			public Igniter igniter = null;
			
			//This checks if a box should be a cloner then reattaches subcomponents and recreates them as necessary - important for persistance across restart
			public static void restoreCloner(StorageContainer sc){
				//If it's skinned per config
				if(sc.skinID == Autoclone.config.skinID){//2041296814
					//Check if it already has acloner component - if so, remove thepreview then destroy the component
					Cloner cloner = sc.GetComponent<Cloner>();
					if(cloner!=null){
						if(cloner.plantPreview!=null && !cloner.plantPreview.IsDestroyed)  {
							cloner.plantPreview.DestroyShared();
							cloner.plantPreview.Kill();
						};
						Destroy(cloner);
					}
					//Generate a new Cloner component
					cloner = sc.gameObject.AddComponent<Cloner>();
					//Set reference to box, set box slots
					cloner.self=sc;
					sc.inventory.capacity=Autoclone.config.cloneSlots;
					int idx = 0;//
					foreach(BaseEntity be in sc.children){
						//Attach the igniter and set properties
						if(be is Igniter){
							cloner.igniter=be as Igniter;
							cloner.igniter.IgniteRange=0;
							cloner.igniter.SelfDamagePerIgnite=0;//
							cloner.igniter.pickup.enabled=true;
							idx++;
						}
						//Attach the light
						else if(be is SimpleLight){
							cloner.clonelight=be as SimpleLight;
							cloner.clonelight.pickup.enabled=true;
						}
						//Attach the pump
						else if(be is FluidSwitch){
							cloner.pump=be as FluidSwitch;
							cloner.pump.pickup.enabled=false;
						}
						//Attach the plant preview
						else if(be is CollectibleEntity){
							UnityEngine.Debug.Log("Found Plant");
							cloner.plantPreview=be as CollectibleEntity;
						}
					}	
					// Set refresh plant preview
					cloner.setupPlant();
					// Add to cloner list for cloning loop
					cloners.Add(cloner);
					// Add igniter if missing
					if(cloner.igniter==null) cloner.setupIgniter();
					// Add pump if missing
					if(cloner.pump==null) cloner.setupPump();
					// Add light if missing
					if(cloner.clonelight==null) cloner.setupclonelight();
					// Connect IO up
					cloner.connectDrain();
					//Init player cloner list for creator if necessary, then add the cloner to the player's list
					if(!Autoclone.PlayerCloners.ContainsKey(sc.OwnerID)){
						Autoclone.PlayerCloners.Add(sc.OwnerID, new List<Cloner>());
					}
					Autoclone.PlayerCloners[sc.OwnerID].Add(cloner);
				}
			}
			
			//Sets up the light
			private void setupclonelight(){
				//Spawn the light and position it, parent it to the box
				clonelight = (SimpleLight) GameManager.server.CreateEntity("assets/prefabs/misc/permstore/industriallight/industrial.wall.lamp.red.deployed.prefab", self.transform.TransformPoint(new Vector3(0f,0.6f,0f)), self.transform.rotation*Quaternion.LookRotation(new Vector3(0,90,0), Vector3.up), true);
				clonelight.transform.Rotate(new Vector3(0,0,90),Space.Self);;
				clonelight.Spawn();
				clonelight.syncPosition=true;
				clonelight.SetParent(self,true,true);
				clonelight.pickup.enabled=true;
				clonelight.SendNetworkUpdateImmediate(true);
			}
			//Sets up the pump
			private void setupPump(){
				//Spawn the pump and position it, parent it to the box
				pump = (FluidSwitch) GameManager.server.CreateEntity("assets/prefabs/deployable/playerioents/fluidswitch/fluidswitch.prefab", self.transform.TransformPoint(new Vector3(0.45f,0.6f,0f)), self.transform.rotation*Quaternion.LookRotation(new Vector3(0,90,0), Vector3.up), true);
				pump.transform.Rotate(new Vector3(0,0,90),Space.Self);
				pump.Spawn();
				pump.syncPosition=true;
				pump.SetParent(self,true,true);
				pump.pickup.enabled=false;
				pump.SendNetworkUpdateImmediate(true);
						
			}
			//Sets up the igniter
			public void setupIgniter(){		
					//Spawn and position igniter
					igniter = (Igniter)GameManager.server.CreateEntity("assets/prefabs/deployable/playerioents/igniter/igniter.deployed.prefab", self.transform.TransformPoint(new Vector3(0,0.55f,0)),self.transform.rotation, true);
					igniter.SetFlag(global::BaseEntity.Flags.On, true, false, true);    
					igniter.Spawn();
					igniter.syncPosition=true;
					
					//Make it take water instead of electricity
					igniter.ioType = IOEntity.IOType.Fluidic;
					igniter.inputs[0].type = IOEntity.IOType.Fluidic;
						
					//Destroy groundwatch
					GroundWatch gw = igniter.GetComponent<GroundWatch>();
					DestroyOnGroundMissing dgw = igniter.GetComponent<DestroyOnGroundMissing>();
					if(dgw!=null){Destroy(dgw);}
					if(gw!=null){Destroy(gw);}
					
					//Parent to box, allow pickup, turn off ignition behavior and damage
					igniter.SetParent(self,true,true);
					igniter.pickup.enabled=true;
					igniter.IgniteRange=0;
					igniter.SelfDamagePerIgnite=0;
					igniter.SendNetworkUpdateImmediate(true);
			}
			//Connects IOs
			private void connectDrain(){          
				//Connect pump water out to igniter electricity in, change igniter to fluidic (it takes water now). Linepoints are relative to outputing device.
                igniter.inputs[0].connectedTo.Set(pump);
                igniter.inputs[0].connectedToSlot = 0;
                igniter.inputs[0].connectedTo.Init();
                pump.outputs[0].connectedTo.Set(igniter);
                pump.outputs[0].connectedToSlot = 0;
                pump.outputs[0].linePoints = new Vector3[]{pump.outputs[0].handlePosition,new Vector3(-0.35f,0,-0.05f),new Vector3(-0.35f,0.3f,-0.05f),pump.transform.InverseTransformPoint(igniter.transform.TransformPoint(igniter.inputs[0].handlePosition))};
                pump.outputs[0].connectedTo.Init();
                pump.outputs[0].worldSpaceLineEndRotation = igniter.transform.TransformDirection(igniter.inputs[0].handleDirection);
                pump.MarkDirtyForceUpdateOutputs();
                igniter.MarkDirtyForceUpdateOutputs();
                pump.SendNetworkUpdate(global::BasePlayer.NetworkQueue.Update);
                igniter.SendNetworkUpdate(global::BasePlayer.NetworkQueue.Update);
                pump.SendChangedToRoot(true);
                pump.RefreshIndustrialPreventBuilding();   

				//Connect light's passthrough to pump main power
                pump.inputs[2].connectedTo.Set(clonelight);
                pump.inputs[2].connectedToSlot = 0;
                pump.inputs[2].connectedTo.Init();
                clonelight.outputs[0].connectedTo.Set(pump);
                clonelight.outputs[0].connectedToSlot = 2;
                clonelight.outputs[0].linePoints = new Vector3[]{clonelight.outputs[0].handlePosition,clonelight.transform.InverseTransformPoint(pump.transform.TransformPoint(pump.inputs[2].handlePosition))};
                clonelight.outputs[0].connectedTo.Init();
                clonelight.outputs[0].worldSpaceLineEndRotation = pump.transform.TransformDirection(pump.inputs[2].handleDirection);//
                clonelight.MarkDirtyForceUpdateOutputs();
                pump.MarkDirtyForceUpdateOutputs();
                clonelight.SendNetworkUpdate(global::BasePlayer.NetworkQueue.Update);
                pump.SendNetworkUpdate(global::BasePlayer.NetworkQueue.Update);
                clonelight.SendChangedToRoot(true);
                clonelight.RefreshIndustrialPreventBuilding();                    
			}
			
			//Converts a new box to a cloner
			public static Cloner setupCloner(StorageContainer sc){
				//Ignore existing cloner
				if(sc.gameObject.GetComponent<Cloner>()==null){		
					//Add cloner component, set box ref and setup box
					Cloner cloner = sc.gameObject.AddComponent<Cloner>();
					sc.inventory.capacity=Autoclone.config.cloneSlots;
					cloner.self=sc;
					cloner.self.skinID = Autoclone.config.skinID;
					cloner.self.SendNetworkUpdateImmediate(true);
					
					//Setup and attach components
					cloner.setupIgniter();				
					cloner.setupPump();
					cloner.setupclonelight();
					//Connect component IO
					cloner.connectDrain();
					//Add to list for cloning timer
					cloners.Add(cloner);
					//Return cloner
					return cloner;
				}
				return null;
			}
			
			//First half of setting up the preview (this is the picking logic)
			public void setupPlant(){
				//Remove the plant preview
				if(plantPreview!=null && !plantPreview.IsDestroyed) {
					plantPreview.DestroyShared();
					plantPreview.Kill();
				};
				//If no clones in box, drop out
				if(self.inventory==null || self.inventory.itemList==null) return;
				if(self.inventory.itemList.Count==0) return;
				//Find a plant, and pass it to the next phase - if it successfully previews, drop out
				foreach(Item i in self.inventory.itemList){
					if(i==null) continue;
					if(setupPlant(i))
						break;
				}
					
			}
			//Second half of setting up the preview (this is the rendering logic)
			public bool setupPlant(Item i){
				//Make sure the preview is really REALLY dead
				if(plantPreview!=null && !plantPreview.IsDestroyed) {
					plantPreview.DestroyShared();
					plantPreview.Kill();
				};
				//If the clone has no genes, drop out
				if(i.instanceData==null) return false;
				//Get plant name and fetch preview entity, if fail drop out
				string s = i.info.displayName.english;
				if(!plantPreviews.ContainsKey(s)) return false;
				//Spawn preview entity with an empty itemlist to prevent pickup and parented to box
				plantPreview = (CollectibleEntity)GameManager.server.CreateEntity(plantPreviews[s], self.transform.TransformPoint(new Vector3(0f,0.7f,0f)), self.transform.rotation*Quaternion.LookRotation(new Vector3(180,0,0), Vector3.up), true);
				plantPreview.Spawn();
				plantPreview.itemList = null;
				plantPreview.SetParent(self,true,true);	
				//Complete sucessfully
				return true;
			}
			//The beating heart - this clones all the slots of the box if possible
			public void clonePlant(){
				//If water check is enabled and igniter has no water, drop out
				if(Autoclone.config.checkWater && ! igniter.HasFlag(global::BaseEntity.Flags.On)) return;
				//If power check is enabled and the light isn't on, drop out
				if(Autoclone.config.checkPower && !clonelight.HasFlag(global::BaseEntity.Flags.On)) return;
				//If the pump isn't on, drop out (this is the main machine switch)
				if(!pump.HasFlag(global::BaseEntity.Flags.On)) return;
				//For each slot, try to clone
				foreach(Item i2 in self.inventory.itemList){
					//If either the amount matches max stack or the plant has no genes, go on to the next plant
					 if(i2.instanceData==null) continue;
					 if(i2.MaxStackable()<=i2.amount) continue;
					 //Decode genes
					 GrowableGenes gg = new GrowableGenes();
					 GrowableGeneEncoding.DecodeIntToGenes(i2.instanceData.dataInt, gg);
					 //Start with double the minimum of 2- it'll make the math easier
					 int increment = 4;
					 //Countthe Ys in the genes
					 foreach(GrowableGene g in gg.Genes){
						 if(g.Type == GrowableGenetics.GeneType.Yield){
							increment++;
						 }
					 }
					 //Divide the result by 2 - equivalent to 2+(0.5 * n(Y))
					 increment= (int)(increment/2);
					 //Clamp to max stackable
					 i2.amount=Mathf.Min(i2.MaxStackable(),i2.amount+increment);
				}
				//Refresh open panels
				self.inventory.MarkDirty();
			}
		}
		
		//Configuration class definition
		public class Configuration
		{
			//When false, ignores the need for the light to be powered for it to run
			[JsonProperty("checkPower", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public bool checkPower=false;
			//When false, ignores the need for the 'igniter' to be fed water to run
			[JsonProperty("checkWater", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public bool checkWater =false;
			//How long between cloning cycles
			[JsonProperty("cloneTimer", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public float cloneTimer =600;
			//Skin used  for the wood box  making the main body. This skin ID will also be how it identifies cloners on reload - use unbuyable skin
			[JsonProperty("skinID", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public ulong skinID =2041296814;
			//How many slots the box has - it clones each slot on the tick.
			[JsonProperty("cloneSlots", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public int cloneSlots =1;
			//Maximum cloners each player can build
			[JsonProperty("max_per_player", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public int max_per_player =4;			
			
			//Helper functions
			public string ToJson() => JsonConvert.SerializeObject(this);				
			public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
		}
		
		#endregion
		
		#region plugin functions
		//This clones a Cloner, then schedules the next Cloner on the next frame
		void recursiveScan(){
			//If current idx is greater than count set to 0 and drop out. (assume finished)
			if(clonerIdx>=Cloner.cloners.Count){
				clonerIdx = 0;
				return;
			}
			//Pick the cloner for this frame's check
			Cloner currentCloner = Cloner.cloners[clonerIdx];
			
			//If stale cloner, clean out
			if(currentCloner.self != null&&currentCloner.self.IsDestroyed ){
				if(PlayerCloners.ContainsKey(currentCloner.self.OwnerID)){
					List<Cloner> clonerlist = PlayerCloners[currentCloner.self.OwnerID];
					if(clonerlist.Contains(currentCloner)){
						clonerlist.Remove(currentCloner);
					}
				}
			}
			//otherwise, attempt to clone and increment
			else{
				Puts("Cloning box");
				currentCloner.clonePlant();//				
				clonerIdx++;
			}
			//Schedule next box for next frame - this is attempting to spread out processing to not lock the process if there's too many cloners.
			NextFrame(() =>
			{
				recursiveScan();
			});
		}
		
		//This refreshes plant preview when called
		void handleItemChange(ItemContainer container, Item item){
			//If it's not a cloner drop out
			if(container==null) return;
			if(container.entityOwner==null) return;
			Cloner c = container.entityOwner.GetComponent<Cloner>();
			if(c==null) return;
			//Refresh plant preview
			c.setupPlant();
			
		}
		
		#endregion
		
		#region umod hooks
		//Setup timers and permissions, restore existing Cloners, static instance reference
		void OnServerInitialized(bool initial)
		{
			instance = this; // Set static instance for easy integration and cross-class access from cloner class
			
			// Turn boxes that were cloners back into cloners
			StorageContainer[] boxes = GameObject.FindObjectsOfType<StorageContainer>();
			foreach(StorageContainer box in boxes){
				Cloner.restoreCloner(box);
			}
			//Trigger scan on timer
			timer.Every(config.cloneTimer, () =>
			{
				//Puts("Cloning");
				recursiveScan();
			});
			//Permission to build cloners
            permission.RegisterPermission("autoclone.build", this);
			//Permission topickup cloners
            permission.RegisterPermission("autoclone.pickup", this);
		}
		
		//Remove or add, we update the preview
		void OnItemAddedToContainer(ItemContainer container, Item item)
		{
			handleItemChange(container,item);
		}
		void OnItemRemovedFromContainer(ItemContainer container, Item item)
		{
			handleItemChange(container,item);
		}
		
		//Cloner crafting flow - needs 100 scrap, ceiling light, pump, and sprinkler in otherwise empty box, then smack box with hammer
		object OnStructureRepair(BaseCombatEntity entity, BasePlayer player)
		{
			if(!entity.PrefabName.Contains("woodbox_deployed.prefab")) return null;
			//Init player cloner list if needed
			if(!PlayerCloners.ContainsKey(player.userID)){
				PlayerCloners.Add(player.userID, new List<Cloner>());
			}
			//Clean out stale cloners
			foreach(Cloner c in PlayerCloners[player.userID].ToArray()){
				if(c == null || c.self == null || c.self.IsDestroyed){
					PlayerCloners[player.userID].Remove(c);
				}
			}
			//Confirm box is a box and has the right number of items
			StorageContainer sc = entity as StorageContainer;
			if(sc==null)return null;
			Puts("Type passed");
			if(sc.inventory.itemList.Count !=4) return null;
			Puts("Count passed");
			//Items we'll need for the recipe check
			Item scrapFlag=null;
			Item lightFlag=null;
			Item sprinklerFlag=null;
			Item pumpFlag=null;
			//These two aren't implemented - planning customizable recipes
			Item gearFlag=null;
			Item tarpFlag=null;
			//Identify recipe ingredients
			foreach(Item i in sc.inventory.itemList){
				if(i.info.displayName.english=="Scrap" && i.amount>=100) scrapFlag=i;
				if(i.info.displayName.english=="Ceiling Light") lightFlag=i;
				if(i.info.displayName.english=="Sprinkler") sprinklerFlag=i;
				if(i.info.displayName.english=="Fluid Switch & Pump") pumpFlag=i;
				if(i.info.displayName.english=="Gears") gearFlag=i;
				if(i.info.displayName.english=="Tarp") tarpFlag=i;
			}
			
			//Puts("Scrap: " +(scrapFlag!=null).ToString()+ " - Light : " +(lightFlag!=null).ToString()+ " - Sprinkler : " +(sprinklerFlag!=null).ToString()+ " - Pump : " +(pumpFlag!=null).ToString());
			
			//If any ingredient missing drop out
			if(scrapFlag==null||lightFlag==null||sprinklerFlag==null||pumpFlag==null) return null;
			
			//If permission missing, throw error and drop out
			if (!permission.UserHasPermission(player.userID.ToString(), "autoclone.build")) {
				
				SendChatMsg(player,"No permission to craft cloner");
				return null;
			}
			//If limit reached, error and drop out
			if(PlayerCloners[player.userID].Count >= config.max_per_player) {
				SendChatMsg(player,"Maximum cloners reached");
				return null;
			}
			//Deal with scrap cost
			if(scrapFlag.amount>100){scrapFlag.amount+=-100;}
			else{scrapFlag.Remove();}
			
			//Deal with light cost
			if(lightFlag.amount>1){lightFlag.amount+=-1;}
			else{lightFlag.Remove();}
			
			//Deal with sprinkler cost
			if(sprinklerFlag.amount>1){sprinklerFlag.amount+=-1;}
			else{sprinklerFlag.Remove();}
			
			//Deal with pump cost
			if(pumpFlag.amount>1){pumpFlag.amount+=-1;}
			else{pumpFlag.Remove();}
			//Drop any remaining contents 
			sc.DropItems();
			//Set owner and add to player's cloner list
			sc.OwnerID = player.userID;
			PlayerCloners[player.userID].Add(Cloner.setupCloner(sc));
			
			return null;
		}
		
		//Custom pickup behavior implementation
		bool? CanPickupEntity(BasePlayer player, BaseEntity entity)
		{
			//If it's not the pickup handles for a cloner, drop out
			if(!entity is Igniter &&  !entity is SimpleLight) return (bool?)null;
			
			//Confirm parent is a cloner otherwise drop out
			BaseEntity baseEntity = entity.parentEntity.Get(entity.isServer);
			if(baseEntity==null || !(baseEntity is StorageContainer && baseEntity.GetComponent<Cloner>()!=null)) return (bool?)null;
			
			// If not authed, disallow
			if(!player.IsBuildingAuthed()) return false;
			// If permission missing, error out and disallow
			if (!permission.UserHasPermission(player.userID.ToString(), "autoclone.pickup")){
				
				SendChatMsg(player,"No permission to pickup cloner");
				return (bool?)false;
			}
			// Toss the ingredients for crafting and destroy the cloner
			StorageContainer sc = baseEntity as StorageContainer;
			ItemManager.CreateByName("ceilinglight").DropAndTossUpwards(sc.GetDropPosition());
			ItemManager.CreateByName("fluid.switch").DropAndTossUpwards(sc.GetDropPosition());
			ItemManager.CreateByName("electric.sprinkler").DropAndTossUpwards(sc.GetDropPosition());
			ItemManager.CreateByName("box.wooden").DropAndTossUpwards(sc.GetDropPosition());
			sc.DropItems();
			sc.Kill();
			// Cancel default pickup flow
			return (bool?)false;
		}
		
		//Flush cloners on unload, otherwise they stack on oxide.reload and cause sticky plant preview
		void Unload()
		{
			foreach(Cloner c in Cloner.cloners){
				if(c.plantPreview!=null){
					Puts("Clearing cloner");
					if(c.plantPreview!=null) c.plantPreview.Kill();
				}
				GameObject.Destroy(c);
			}
		}
		#endregion
		
		#region Helper functions
		
		//Helper functions for messaging player
        private static Game.Rust.Libraries.Player _rustPlayer = Interface.Oxide.GetLibrary<Game.Rust.Libraries.Player>("Player");
        private static void SendChatMsg(BasePlayer pl, string msg) =>
            _rustPlayer.Message(pl, msg, "<color=#00ff00>[Autocloner]</color>", 0, Array.Empty<object>());
		
		//Bog-standard config load and save
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
		
		#endregion	
		
	}
}
