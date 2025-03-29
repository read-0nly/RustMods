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
    [Info("Cobalt-OS", "obsol", "0.0.2")]
    [Description("OS for computer stations. When tapedecks are deployed on computer stations, allows computers to execute programs from custom tapes")]
	/*
	Creates a diegetic way to represent extradiegetic things - put simply, instead of typing a command in F1 or chat, you insert 
	the program tape that holds the commands you wanna execute then sit at the computer to interact with the program. 
	
	For instance a lot of PVE servers have custom economies and server shops with /shop - tapes with a shop app could be in the loot table instead. Call it eBay or something.
	Then when users interact with the computer station while that tape is loaded in the bound tapedeck, the shop launches over the usual interface and they can 
	still exit to use cameras
	
	Two methods of calling apps - a CUI string or a command button. Command buttons get added to the base workstation UI, CUI strings call the whole panel.
	*/
    internal class COBALTOS : CovalencePlugin
    {
			
		#region AdminOS
		string AdminOSString = @"				
[	
	{
		""name"": ""TestPanel776"",
		""parent"": ""Overlay"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Image"",
				""imagetype"": ""Tiled"",
				""color"": ""0.0255 0.026 0.03 1.0"",
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.1 0.08"",
				""anchormax"": ""0.9 0.9""
			},
			{
				""type"":""NeedsCursor""
			}
		]
	},
	{
		""parent"": ""TestPanel776"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""0.2 0.5 0.7 1"",
				""text"":""COBALT-OS"",
				""align"": ""MiddleCenter"",
				""fontSize"":40,
				""font"":""DroidSansMono.ttf""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.1 0.85"",
				""anchormax"": ""0.9 0.95""
			}
		]
	},
	{
		""parent"": ""TestPanel776"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""0.2 0.5 0.7 1"",
				""text"":""Version:0.5.7.14(ADMIN UNLOCKED)
COBALT TECHNOLOGIES

TERMINAL CONNECTED"",
				""align"": ""MiddleCenter"",
				""fontSize"":20,
				""font"":""DroidSansMono.ttf""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.1 0.65"",
				""anchormax"": ""0.9 0.85""
			}
		]
	},
	{
		""name"": ""Button8119"",
		""parent"": ""TestPanel776"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Button"",
				""command"":""quit"",
				""close"":""TestPanel776"",
				""color"": ""0.05 0.08 0.13 1"",
				""imagetype"": ""Tiled""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.3 0.44"",
				""anchormax"": ""0.7 0.49""
			}
		]
	},
	{
		""parent"": ""Button8119"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""0.2 0.5 0.7 1"",
				""font"":""DroidSansMono.ttf"",
				""text"":""END SIMULATION"",
				""align"": ""MiddleCenter""
			}
		]
	},
	{
		""name"": ""Button8114"",
		""parent"": ""TestPanel776"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Button"",
				""command"":""env.time 0"",
				""close"":""TestPanel776"",
				""color"": ""0.05 0.08 0.13 1"",
				""imagetype"": ""Tiled""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.3 0.32"",
				""anchormax"": ""0.7 0.37""
			}
		]
	},
	{
		""parent"": ""Button8114"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""0.2 0.5 0.7 1"",
				""font"":""DroidSansMono.ttf"",
				""text"":""Night"",
				""align"": ""MiddleCenter""
			}
		]
	},
	{
		""name"": ""Button8112"",
		""parent"": ""TestPanel776"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Button"",
				""command"":""env.time 12"",
				""close"":""TestPanel776"",
				""color"": ""0.05 0.08 0.13 1"",
				""imagetype"": ""Tiled""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.3 0.26"",
				""anchormax"": ""0.7 0.31""
			}
		]
	},
	{
		""parent"": ""Button8112"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""0.2 0.5 0.7 1"",
				""font"":""DroidSansMono.ttf"",
				""text"":""Day"",
				""align"": ""MiddleCenter""
			}
		]
	},
	{
		""name"": ""Button88"",
		""parent"": ""TestPanel776"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Button"",
				""close"":""TestPanel776"",
				""color"": ""0.05 0.08 0.13 1"",
				""imagetype"": ""Tiled""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.3 0.15"",
				""anchormax"": ""0.7 0.2""
			}
		]
	},
	{
		""parent"": ""Button88"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""0.2 0.5 0.7 1"",
				""font"":""DroidSansMono.ttf"",
				""fontsize"":20,
				""text"":""CLOSE"",
				""align"": ""MiddleCenter""
			}
		]
	}
]
";
		#endregion
		[PluginReference]
		private Plugin GUIShop;
			
		public delegate void RunCommand(BasePlayer bp, ComputerStation cs);
				
		public static void AddPhone(ComputerStation cs){
			Vector3 spawnspot = cs.transform.TransformPoint(new Vector3(-0.6f,0.73f,0.1f));
			ConVar.Entity.EntitySpawnRequest spawnEntityFromName = ConVar.Entity.GetSpawnEntityFromName("telephone");
			global::BaseEntity baseEntity = global::GameManager.server.CreateEntity(spawnEntityFromName.PrefabName, spawnspot,Quaternion.LookRotation(cs.transform.forward, Vector3.up), true);
			Telephone sm = baseEntity as Telephone;
			sm.Spawn();
			sm.SetParent(cs,true);
			sm.pickup.enabled = false;
			Console.WriteLine("Spawned SM?");
		}
		Dictionary<RunCommand,float> CommandTapeSpawnrates = new Dictionary<RunCommand,float>();
		Dictionary<string,float> PanelTapeSpawnrates = new Dictionary<string,float>();
		
		Dictionary<Item,RunCommand> CommandTapes = new Dictionary<Item,RunCommand>();
		Dictionary<Item,string> PanelTapes = new Dictionary<Item,string>();
		
		private bool AddPanelTape(Item cassette, string panel){
			
		}
		private bool AddCommandTape(Item cassette, RunCommand command){
			
		}
		
		public void ShowGUIShop(BasePlayer bp, ComputerStation cs){
			if(GUIShop!=null){
				GUIShop.Call("ShowGUIShops", bp, "Component");
			}
		}
		object OnLootSpawn(LootContainer container)
		{
			foreach(string s in PanelTapeSpawnrates.Keys){
				if(UnityEngine.Random.Range(0f,1f)<PanelTapeSpawnrates[s]){
					Item cassetteItem = ItemManager.CreateByName("cassette", 1, 0UL);
					cassetteItem.text = "DATATAPE";
					basePlayer.GiveItem(cassetteItem);
					PanelTapes.Add(cassetteItem,AdminOSString);
				}
			}
			Puts("OnLootSpawn works!");
			return null;
		}
        [Command("GiveAdminTape")]
        private void GiveSpawnTape(IPlayer player, string cmd, string[] args)
        {			
            BasePlayer basePlayer = player.Object as BasePlayer;
			if(basePlayer==null){
				return;
			}
			Item cassetteItem = ItemManager.CreateByName("cassette", 1, 0UL);
			cassetteItem.text = "-- TOP SECRET --";
			basePlayer.GiveItem(cassetteItem);
			if(!PanelTapeSpawnrates.ContainsKey(AdminOSString)){PanelTapeSpawnrates.Add(AdminOSString,0.5f);}
			PanelTapes.Add(cassetteItem,AdminOSString);
		}
        [Command("GiveEbayTape")]
        private void GiveEbayTape(IPlayer player, string cmd, string[] args)
        {			
            BasePlayer basePlayer = player.Object as BasePlayer;
			if(basePlayer==null){
				return;
			}
			Item cassetteItem = ItemManager.CreateByName("cassette", 1, 0UL);
			cassetteItem.text = "eBay";
			basePlayer.GiveItem(cassetteItem);
			
			CommandTapes.Add(cassetteItem,ShowGUIShop);
		}
		
		System.Object OnEntityMounted(ComputerStation cs ,BasePlayer player){
			DeployedRecorder recorder = cs.gameObject.GetComponentInChildren<DeployedRecorder>();
			Item i = null;
			if(recorder==null){
				Puts("No tapedeck");
				Telephone phone = cs.gameObject.GetComponentInChildren<Telephone>();
				if(phone==null){
					return null;
				}
				i = phone.inventory.GetSlot(0);
			}else{
				i = recorder.inventory.GetSlot(0);
			}
			if(i==null){
				Puts("No tape");
				return null;
			}
			if(CommandTapes.ContainsKey(i)){
				CommandTapes[i](player,cs);
				return null;
			}
			if(PanelTapes.ContainsKey(i)){
				CommunityEntity.ServerInstance.ClientRPC<string>(RpcTarget.Player("AddUI", player.Connection),(PanelTapes[i]));
				return null;
			}
			Cassette cassette = (i.info.itemMods[0] as ItemModAssociatedEntity<Cassette>).entityPrefab.GetEntity() as Cassette;
			Puts((cassette.GetType()).ToString());
			
			
			return null;
			
		}
		//Interface.CallHook("OnBookmarkAdd", this, player, text) != null)
		//System.Object OnBookmarkAdd(ComputerStation cs, BasePlayer player,string text=""){
		//}
	}
	
}