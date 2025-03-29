
using Oxide.Plugins;
using Oxide.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine; 
using System.Collections;
using System.Collections.Generic;
using System;


namespace Oxide.Plugins
{
	[Info("DeployAnywhere", "obsol", "0.0.1")]
	[Description("Allows any deployable to be deployed using middle-click regardless of triggers/zones")]
	public class DeployAnywhere : CovalencePlugin
	{
		private static Game.Rust.Libraries.Player _rustPlayer = Interface.Oxide.GetLibrary<Game.Rust.Libraries.Player>("Player");
		private static void SendChatMsg(BasePlayer pl, string msg) =>
			_rustPlayer.Message(pl, msg,  "<color=#00ff00>[FactionSystem]</color>", 0, Array.Empty<object>());
		private void OnServerInitialized()
        {
			TriggerSafeZone[] szArr = TriggerSafeZone.allSafeZones.ToArray();
			foreach(TriggerSafeZone sz in szArr){
				sz.gameObject.SetActive(false);
			}
		}
		
		
		void OnEntitySpawned(BaseCombatEntity entity){
				GroundWatch gw = entity.GetComponent<GroundWatch>();
				DestroyOnGroundMissing dg = entity.GetComponent<DestroyOnGroundMissing>();
				if(gw!=null){
					GameObject.Destroy(gw);
				}
				if(dg!=null){
					GameObject.Destroy(dg);
				}
		}
		bool? OnGround(GroundWatch gw){
			global::BaseEntity component = gw.GetComponent<global::BaseEntity>();
			if (component)
			{
				global::Construction construction = global::PrefabAttribute.server.Find<global::Construction>(component.prefabID);
				if (construction)
				{
					global::Socket_Base[] allSockets = construction.allSockets;
					for (int i = 0; i < allSockets.Length; i++)
					{
						int socketIndex = -1;
						global::SocketMod[] socketMods = allSockets[i].socketMods;
						for (int j = 0; j < socketMods.Length; j++)
						{
							global::SocketMod_AreaCheck socketMod_AreaCheck = socketMods[j] as global::SocketMod_AreaCheck;
							if (socketMod_AreaCheck)
							{
								socketIndex = j;	
							}
							
						}
						if(socketIndex>-1){
							List<SocketMod> bucket = new List<SocketMod>(construction.allSockets[i].socketMods);
							bucket.RemoveAt(socketIndex);
							construction.allSockets[i].socketMods=bucket.ToArray();
						}
					}
				}
			}
			return null;
		}
		bool? CanUseWires(BasePlayer bp){
			Puts("Using Wires");
			return null;
		}
		bool? CanWiretoolEntity(BasePlayer player, BaseEntity ent){
			Puts("CanWiretool");
			return null;
			
	
			
		}
		/*
		if (ioentity.inputs[num].connectedTo.Get(true) != null)
		{
			return;
		}
		*/
		class WireBucket : BaseMonoBehaviour{
			public IOEntity currentEntity;
			public IOEntity.IOSlot currentSlot;
			public int slotIndex;
			public bool isInput;
		}
		object OnActiveItemChange(BasePlayer player, Item oldItem, uint newItemId)
		{
			WireTool wiretool = player.GetHeldEntity() as WireTool;
			if(wiretool){
				if(!wiretool.gameObject.GetComponent<WireBucket>())wiretool.gameObject.AddComponent<WireBucket>();
				WireBucket wb = wiretool.gameObject.GetComponent<WireBucket>();
				wb.currentEntity=null;
				wb.currentSlot=null;
			}
			return null;
		}
		
		BaseEntity OnPlayerInput(BasePlayer player, InputState input){
			if(!input.WasJustPressed(BUTTON.FIRE_THIRD)) return null;
			Deployer deployer = player.GetHeldEntity() as Deployer;
			if(deployer){Puts("Deployer");return null;}
			Planner planner = player.GetHeldEntity() as Planner;
			WireTool wiretool = player.GetHeldEntity() as WireTool;
			uint prefabID = 0;
			if(planner){
				Puts("Planner " + planner.GetOwnerItemDefinition().isUsable.ToString());
				prefabID = (uint)planner.GetOwnerItemDefinition().gameObject.GetComponentInChildren<ItemModDeployable>()?.entityPrefab.resourceID;
			}
			if(planner==null){
				if((player.GetHeldEntity().ToString().Contains("hammer.entity"))){
					Puts("Rotating?");
					BaseEntity be = getLookingAt(player);
					Puts(be.name);
					if (input.IsDown(BUTTON.SPRINT)){
						be.gameObject.transform.Rotate(new Vector3(0,180f,0));
					}
					else if (input.IsDown(BUTTON.DUCK)){
						be.gameObject.transform.Rotate(new Vector3(0,15f,0));
						
					}
					else{
						be.gameObject.transform.Rotate(new Vector3(0,90f,0));
					}
					
					be.InvalidateNetworkCache();
					if (!be.isCallingUpdateNetworkGroup)
					{
						be.Invoke(new System.Action(be.UpdateNetworkGroup), 5f);
						be.isCallingUpdateNetworkGroup = true;
					}
					be.SendNetworkUpdate_Position();
					be.OnPositionalNetworkUpdate();
					Puts(be.gameObject.transform.eulerAngles.ToString());
				}
				else if (wiretool){
					Puts("Wiretool");
					if(!wiretool.gameObject.GetComponent<WireBucket>())wiretool.gameObject.AddComponent<WireBucket>();
					WireBucket wb = wiretool.gameObject.GetComponent<WireBucket>();
					IOEntity ioe = getLookingAt(player) as IOEntity;
					if(ioe == null) ioe = getLookingAt(player).gameObject.GetComponent<IOEntity>();
					if(ioe == null) ioe = getLookingAt(player).gameObject.GetComponentInChildren<IOEntity>();
					if(ioe==null) return null;
					Puts("ioe");
					IOEntity.IOSlot nearestIOS = null;
					bool isInput = true;
					int foundIndex = 255;
					Vector3 lookatPoint = getLookingAtPoint(player);
					int i = 0;
					foreach(IOEntity.IOSlot ios in ioe.inputs){		
						if(nearestIOS == null){ nearestIOS=ios; 
							Puts(ios.niceName+ "is input");foundIndex=i;}
						else if(Vector3.Distance(ioe.transform.TransformPoint(ios.handlePosition),lookatPoint)<Vector3.Distance(ioe.transform.TransformPoint(nearestIOS.handlePosition),lookatPoint)){
							Puts(ios.niceName +"is input");
							nearestIOS = ios;
							foundIndex=i;
						}
						i++;
					}
					i=0;
					foreach(IOEntity.IOSlot ios in ioe.outputs){
						if(nearestIOS == null){ nearestIOS=ios; isInput=false;
							Puts(ios.niceName +"is output");foundIndex=i;}
						else if(Vector3.Distance(ioe.transform.TransformPoint(ios.handlePosition),lookatPoint)<Vector3.Distance(ioe.transform.TransformPoint(nearestIOS.handlePosition),lookatPoint)){
							Puts(ios.niceName+ "is output");
							nearestIOS = ios;
							isInput=false;
							foundIndex=i;
						}
						i++;
					}
					Puts(nearestIOS.niceName);
					if(nearestIOS==null) return null;
					IOEntity connectedIOE = (nearestIOS.connectedTo.Get());
					if(connectedIOE == null){
						if(wb.currentEntity==null){
							wb.currentEntity = ioe;
							wb.currentSlot = nearestIOS;
							wb.slotIndex = foundIndex;
							wb.isInput = isInput;
							SendChatMsg(player, (wb.currentSlot.connectedTo.Get()==null?"Connection started from ": "Disconnection started for ")+wb.currentEntity.GetDisplayName()+":"+wb.currentSlot.niceName);
							return null;
							
						}
						else if(ioe==wb.currentEntity){
							SendChatMsg(player, "Letting go of "+wb.currentEntity.GetDisplayName());
							wb.currentEntity = null;
							wb.currentSlot = null;
							wb.slotIndex = foundIndex;
							wb.isInput = isInput;
							return null;
						}else if(wb.isInput!=isInput){
							Puts(wb.slotIndex.ToString()+":"+foundIndex.ToString()+":"+wb.isInput.ToString()+":"+isInput.ToString());
							bool worked =false;
							if(wb.isInput){
								worked|=MakeConnection(player,wb.currentEntity,wb.slotIndex,ioe,foundIndex);
							}else{
								worked|=MakeConnection(player,ioe,foundIndex,wb.currentEntity,wb.slotIndex);
								
							}
							if(worked){
								wb.currentEntity = null;
								wb.currentSlot = null;
								wb.slotIndex = foundIndex;
								wb.isInput = isInput;
							}else{
								SendChatMsg(player, "Connection failed") ;
							}
							
							return null;
						}
					}
					else{
						if((connectedIOE==wb.currentEntity && wb.currentSlot.connectedTo.Get() == ioe)||wb.currentSlot==nearestIOS){
							SendChatMsg(player, "Disconnected "+ioe.GetDisplayName()+" from "+connectedIOE.GetDisplayName());
							
							
							BreakConnection(player,ioe,foundIndex,isInput);
							
							wb.currentEntity = null;
							wb.currentSlot = null;
							wb.slotIndex = foundIndex;
							wb.isInput = isInput;
							return null;
						}else{
							SendChatMsg(player, "Connection exists from "+ioe.GetDisplayName()+":"+nearestIOS.niceName+" to "+connectedIOE.GetDisplayName()+":"+(isInput?connectedIOE.outputs[nearestIOS.connectedToSlot]:connectedIOE.inputs[nearestIOS.connectedToSlot]).niceName);
							if(wb.currentEntity==null){								
								wb.currentEntity = ioe;
								wb.currentSlot = nearestIOS;
								wb.slotIndex = foundIndex;
								wb.isInput = isInput;
							}
							return null;
						}
					}
						
				}
			}
			else{
				Deployable deploy = planner.GetDeployable();
				if(deploy==null)
				Puts("No  Deployable");
				RaycastHit raycastHit;
				if (!Physics.Raycast( player.eyes.HeadRay(), out raycastHit, 5f))
				{
					return null;
				}
				Puts("Raycast");
				Vector3 lhs;
				string targetName = planner.GetItem().info.shortname.ToLower();
				string[]freeAngle ={ 
				"ladder.wooden.wall",
				"sign.wooden.",
				"electric.",
				"electrical.",
				".switch",
				"fluid.",
				"ceilinglight",
				"door.",
				};
				
				bool limitAngle = true;
				foreach(string s in freeAngle){
					Puts(targetName+":"+s);
					limitAngle &= !(targetName.ToLower().Contains(s.ToLower()));
				}
				Puts(limitAngle.ToString());
				float yRotation = 0;
				if (input.IsDown(BUTTON.SPRINT) && input.IsDown(BUTTON.DUCK)){
					yRotation=180f;
				}
				else if (input.IsDown(BUTTON.SPRINT)){
					yRotation=90f;
					
				}
				else if (input.IsDown(BUTTON.DUCK)){
					yRotation=-90f;
				}
				
				if(limitAngle){
					lhs= Quaternion.LookRotation(raycastHit.normal,  raycastHit.point) * Quaternion.Euler(90, 0, 0) * (Vector3.up)+ new Vector3(0,yRotation,0);
					if (!(Mathf.Acos(Vector3.Dot(lhs- new Vector3(0,yRotation,0), Vector3.up)) < 0.61086524f))
					{
						return null;
					}
				}
				else{
					lhs= Quaternion.LookRotation(raycastHit.normal,  raycastHit.point) * Quaternion.Euler(yRotation, 0, 0) * Vector3.up;
				}
				
				Construction.Target target = new Construction.Target();
				target.ray = player.eyes.HeadRay();
				target.onTerrain = true;
				target.valid = true;
				target.inBuildingPrivilege = false;
				target.position = raycastHit.point+ new Vector3(0,0f,0);
				target.normal = raycastHit.normal;
				target.rotation = lhs;
				target.player = player;
				Puts("Target");
				Construction construction = (deploy?PrefabAttribute.server.Find<Construction>(deploy.prefabID):PrefabAttribute.server.Find<Construction>(prefabID));
				if(construction==null) return null;
				Puts("Construction");
				
				if (Interface.CallHook("CanBuild", planner, construction, target) != null)
				{
					return null;
				}
				BaseEntity baseEntity =(construction.CreateConstruction(target,false));
				switch (planner.GetItem().info.shortname.ToLower()){
					case "ladder.wooden.wall":
						break;
					case "sign.wooden.small":
						break;
					default:
						baseEntity.gameObject.transform.Rotate(new Vector3(0,0f,0));
						break;
				}
			
			
				float num = 1f;
				global::Item ownerItem = player.inventory.FindItemUID(planner.ownerItemUID);
				if (ownerItem != null)
				{
					baseEntity.skinID = ownerItem.skin;
					if (ownerItem.hasCondition)
					{
						num = ownerItem.conditionNormalized;
					}
				}
				baseEntity.gameObject.AwakeFromInstantiate();
				global::BuildingBlock buildingBlock = baseEntity as global::BuildingBlock;
				if (buildingBlock)
				{
					buildingBlock.blockDefinition = global::PrefabAttribute.server.Find<global::Construction>(buildingBlock.prefabID);
					if (!buildingBlock.blockDefinition)
					{
						Debug.LogError("Placing a building block that has no block definition!");
						return null;
					}
					buildingBlock.SetGrade(buildingBlock.blockDefinition.defaultGrade.gradeBase.type);
					float num2 = buildingBlock.currentGrade.maxHealth;
				}
				global::BaseCombatEntity baseCombatEntity = baseEntity as global::BaseCombatEntity;
				if (baseCombatEntity)
				{
					float num2 = (buildingBlock != null) ? buildingBlock.currentGrade.maxHealth : baseCombatEntity.startHealth;
					baseCombatEntity.ResetLifeStateOnSpawn = false;
					baseCombatEntity.InitializeHealth(num2 * num, num2);
				}
				baseEntity.gameObject.SendMessage("SetDeployedBy", player, SendMessageOptions.DontRequireReceiver);
				baseEntity.OwnerID = player.userID;
				baseEntity.Spawn();
				if (buildingBlock)
				{
					global::Effect.server.Run("assets/bundled/prefabs/fx/build/frame_place.prefab", baseEntity, 0U, Vector3.zero, Vector3.zero, null, false);
				}
				global::StabilityEntity stabilityEntity = baseEntity as global::StabilityEntity;
				if (stabilityEntity)
				{
					stabilityEntity.UpdateSurroundingEntities();
				}
				GameObject gameObject = baseEntity.gameObject;
				if (gameObject != null)
				{
					Puts("Gameobject");
					global::Deployable deployable = planner.GetDeployable();
					if (deployable != null)
					{
						if (deployable.setSocketParent && target.entity != null && target.entity.SupportsChildDeployables() && baseEntity)
						{
							Puts("SetParent?");
							baseEntity.SetParent(target.entity, true, false);
						}
						if (deployable.wantsInstanceData && ownerItem.instanceData != null)
						{
							Puts("Instance?");
							(baseEntity as global::IInstanceDataReceiver).ReceiveInstanceData(ownerItem.instanceData);
						}
						if (deployable.copyInventoryFromItem)
						{
							global::StorageContainer component2 = baseEntity.GetComponent<global::StorageContainer>();
							if (component2)
							{
								component2.ReceiveInventoryFromItem(ownerItem);
							}
						}
						baseEntity.OnDeployed(baseEntity.GetParentEntity(), player, ownerItem);
						if (deployable.placeEffect.isValid)
						{
							Puts("placeffect");
							if (target.entity && target.socket != null)
							{
								global::Effect.server.Run(deployable.placeEffect.resourcePath, target.entity.transform.TransformPoint(target.socket.worldPosition), target.entity.transform.up, null, false);
							}
							else
							{
								global::Effect.server.Run(deployable.placeEffect.resourcePath, target.position, target.normal, null, false);
							}
						}
					}
					if (Interface.Call("OnEntityBuilt", planner, gameObject) != null)
					{
						return null;
					}
					Puts("Pay");
					planner.PayForPlacement(player, construction);
				}
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
		public Vector3 getLookingAtPoint(BasePlayer player){			
			RaycastHit hit;
			if (Physics.Raycast(player.eyes.HeadRay(), out hit)){
				var entity = hit.point;
				return entity;
			}
			return new Vector3(0,-255,0);
		}
		public BaseEntity getLookingAt(BasePlayer player){			
			RaycastHit hit;
			if (Physics.Raycast(player.eyes.HeadRay(), out hit)){
				var entity = hit.GetEntity();
				if (entity != null){if(entity.GetComponent<BaseEntity>()!=null) return entity.GetComponent<BaseEntity>();}
			}
			return null;
		}
		
		public void BreakConnection(BasePlayer player, IOEntity ioentity, int num, bool flag){
			
			if (ioentity == null)
			{
				return;
			}
			if (num >= (flag ? ioentity.inputs.Length : ioentity.outputs.Length))
			{
				return;
			}
			global::IOEntity.IOSlot ioslot = flag ? ioentity.inputs[num] : ioentity.outputs[num];
			if (ioslot.connectedTo.Get(true) == null)
			{
				return;
			}
			global::IOEntity ioentity2 = ioslot.connectedTo.Get(true);
			if (Interface.CallHook("OnWireClear", player, ioentity, num, ioentity2, flag) != null)
			{
				return;
			}
			global::IOEntity.IOSlot ioslot2 = flag ? ioentity2.outputs[ioslot.connectedToSlot] : ioentity2.inputs[ioslot.connectedToSlot];
			if (flag)
			{
				ioentity.UpdateFromInput(0, num);
			}
			else if (ioentity2)
			{
				ioentity2.UpdateFromInput(0, ioslot.connectedToSlot);
			}
			ioslot.Clear();
			ioslot2.Clear();
			ioentity.MarkDirtyForceUpdateOutputs();
			ioentity.SendNetworkUpdate(global::BasePlayer.NetworkQueue.Update);
			if (flag && ioentity2 != null)
			{
				ioentity2.SendChangedToRoot(true);
			}
			else if (!flag)
			{
				foreach (global::IOEntity.IOSlot ioslot3 in ioentity.inputs)
				{
					if (ioslot3.mainPowerSlot && ioslot3.connectedTo.Get(true))
					{
						ioslot3.connectedTo.Get(true).SendChangedToRoot(true);
					}
				}
			}
			ioentity2.SendNetworkUpdate(global::BasePlayer.NetworkQueue.Update);
		}
		public bool MakeConnection(BasePlayer player, IOEntity ioentity, int num, IOEntity ioentity2, int num2, WireTool.WireColour wireColour = WireTool.WireColour.Default){
			
			if (ioentity == null || (ioentity2 == null))
			{
				return false;
			}
			if (num >= ioentity.inputs.Length)
			{
				return false;
			}
			if (num2 >= ioentity2.outputs.Length)
			{
				return false;
			}
			if (ioentity.inputs[num].connectedTo.Get(true) != null)
			{
				return false;
			}
			if (ioentity2.outputs[num2].connectedTo.Get(true) != null)
			{
				return false;
			}
			if (ioentity.inputs[num].rootConnectionsOnly && !ioentity2.IsRootEntity())
			{
				SendChatMsg(player, "Connection requires root") ;
				return false;
			}
			if (Interface.CallHook("OnWireConnect", player, ioentity, num, ioentity2, num2) != null)
			{
				ioentity2.outputs[num2].linePoints = null;
				return false;
			}
			List<Vector3> list = new List<Vector3>();
			list.Add(ioentity.transform.TransformPoint(ioentity.inputs[num].handlePosition));
			list.Add(ioentity2.transform.TransformPoint(ioentity2.outputs[num2].handlePosition));
			if (!ValidateLine(list, ioentity, ioentity2, player))
			{
				
				SendChatMsg(player, "Line too long") ;
				return false;
			}
			SendChatMsg(player, "Connected "+ioentity.GetDisplayName() + " to " +ioentity2.GetDisplayName()) ;
			ioentity.inputs[num].connectedTo.Set(ioentity2);
			ioentity.inputs[num].connectedToSlot = num2;
			ioentity.inputs[num].wireColour = wireColour;
			ioentity.inputs[num].connectedTo.Init();
			ioentity2.outputs[num2].connectedTo.Set(ioentity);
			ioentity2.outputs[num2].connectedToSlot = num;
			ioentity2.outputs[num2].wireColour = wireColour;
			ioentity2.outputs[num2].connectedTo.Init();
			AddLine(player,list,ioentity,num,ioentity2,num2,wireColour);
			
			ioentity.MarkDirtyForceUpdateOutputs();
			ioentity2.MarkDirtyForceUpdateOutputs();
			ioentity2.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
			ioentity.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
			ioentity2.SendChangedToRoot(true);
			return true;
		}
		public void AddLine(BasePlayer player, List<Vector3> list,IOEntity ioentity, int num2, IOEntity ioentity2, int num3, WireTool.WireColour wireColour){
			
			if (ioentity == null)
			{
				return;
			}
			if (ioentity2 == null)
			{
				return;
			}
			if (num2 >= ioentity.inputs.Length)
			{
				return;
			}
			if (num3 >= ioentity2.outputs.Length)
			{
				return;
			}
			if (ioentity.inputs[num2].connectedTo.Get(true) != null)
			{
				return;
			}
			if (ioentity2.outputs[num3].connectedTo.Get(true) != null)
			{
				return;
			}
			if (!global::WireTool.CanModifyEntity(player, ioentity2))
			{
				return;
			}
			if (!global::WireTool.CanModifyEntity(player, ioentity))
			{
				return;
			}
			List<float> slacks = new List<float>();
			foreach(Vector3 v in list){
				slacks.Add(0.1f);
			}
			ioentity2.outputs[num3].linePoints = list.ToArray();
			ioentity2.outputs[num3].slackLevels = slacks.ToArray();
			ioentity2.outputs[num3].wireColour = wireColour;
		}
		bool ValidateLine(List<Vector3> lineList, IOEntity inputEntity, IOEntity outputEntity, BasePlayer byPlayer)
		{
			if (lineList.Count < 2)
			{
				return false;
			}
			if (inputEntity == null || outputEntity == null)
			{
				return false;
			}
			Vector3 a = lineList[0];
			float num = 0f;
			int count = lineList.Count;
			for (int i = 1; i < count; i++)
			{
				Vector3 vector = lineList[i];
				num += Vector3.Distance(a, vector);
				if (num > global::WireTool.maxWireLength)
				{
					return false;
				}
				a = vector;
			}
			return true;
		}
	}
}
			