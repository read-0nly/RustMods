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
    [Info("Monudeploy", "obsol", "0.0.2")]
    [Description("Way to deploy deployables at monuments")]
    internal class Monudeploy : CovalencePlugin
    {
		
		public ConfigData config;


		public class ConfigData
		{
			[JsonProperty("DeployBoxSkinID", ObjectCreationHandling = ObjectCreationHandling.Replace)]		
			public uint DeployBoxSkinID = 2814554242;
			
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
		}

		protected override void SaveConfig()
		{
			Config.WriteObject(config);
		}

		
		private void Loaded(){
			LoadConfig();
		}
		private void Unload(){
			SaveConfig();
		}
		object OnStructureRepair(BaseCombatEntity entity, BasePlayer player)
		{
			if(entity.GetComponent<UnsettledDeployable>()!=null){
				lockObject(entity);
				entity.pickup.enabled=false;
			}
			return null;
		}
		bool lockObject(BaseEntity entity){
			
				UnityEngine.Object.Destroy(entity.gameObject.GetComponent<UnsettledDeployable>());
				Rigidbody rigidbody = entity.GetComponent<Rigidbody>();
				rigidbody.useGravity = false;
				//rigidbody.detectCollisions = false;
				rigidbody.isKinematic = true;
				//entity.syncPosition=false;
				return true;
		}
		void OnItemDropped(Item i, DroppedItem droppedEnt){
			BasePlayer thrower = i.GetEntityOwner() as BasePlayer;
			if(thrower!=null){
				Puts("Thrown");
			}else{
				return;
			}
			BaseEntity be = i.info.GetComponent<ItemModDeployable>().entityPrefab.GetEntity();
			ConVar.Entity.EntitySpawnRequest spawnEntityFromName = ConVar.Entity.GetSpawnEntityFromName(be.ToString());//
			BaseCombatEntity baseEntity = (global::GameManager.server.CreateEntity(spawnEntityFromName.PrefabName, droppedEnt.transform.position+(thrower.eyes.HeadForward()*2),Quaternion.LookRotation(new Vector3(thrower.eyes.HeadForward().x,0,thrower.eyes.HeadForward().z),thrower.transform.up), true) as BaseCombatEntity);
			baseEntity.skinID = be.skinID;
			droppedEnt.Kill();
			
				
			if((baseEntity as BaseVehicle)==null || baseEntity.GetComponent<Rigidbody>()==null){
				SleepingBag bag = baseEntity as SleepingBag;
				baseEntity.syncPosition=true;	
				Rigidbody rigidbody = baseEntity.GetComponent<Rigidbody>();
				GroundWatch gw = baseEntity.GetComponent<GroundWatch>();
				if(bag!=null){
					timer.Once(5f,()=>{
						lockObject(baseEntity);
					});
					bag.destroyAfterUse=true;
					bag.deployerUserID=thrower.Connection.userid;
				}
				if(gw!=null){
					Puts("Deleting GroundWatch");
					UnityEngine.Object.Destroy(gw);//
				}
				if (rigidbody == null)
				{
					rigidbody = baseEntity.gameObject.AddComponent<Rigidbody>();
					rigidbody.mass = 10f;
					rigidbody.drag = 0.5f;
					rigidbody.angularDrag = 0.5f;
				}
				if((baseEntity).repair.enabled || (baseEntity).pickup.enabled==false && (baseEntity as BaseVehicle)==null){
					
					timer.Once(5f,()=>{
						lockObject(baseEntity);
						rigidbody.constraints =  RigidbodyConstraints.FreezeRotationX |  RigidbodyConstraints.FreezeRotationZ;
					});
				}
				rigidbody.useGravity = true;
				rigidbody.detectCollisions = true;
				rigidbody.includeLayers = (int)0xfffffff;
				Puts(rigidbody.includeLayers.value.ToString());
				Puts(rigidbody.excludeLayers.value.ToString());
				rigidbody.excludeLayers = rigidbody.excludeLayers;
				rigidbody.isKinematic = false;
				baseEntity.gameObject.AddComponent<UnsettledDeployable>();
				//baseEntity.transform.position+=new Vector3(0,1,0);
				rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
			}
			baseEntity.Spawn();	
			baseEntity.ApplyInheritedVelocity(thrower.eyes.HeadForward());
			
			
		}
		public class UnsettledDeployable : MonoBehaviour{
			
		}
        
	}
	
}