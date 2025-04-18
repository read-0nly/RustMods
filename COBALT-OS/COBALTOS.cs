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
		public static COBALTOS instance;
		private Dictionary<int,int> connections = new Dictionary<int,int>();
		
		public ConfigData config;

		public static Dictionary<ComputerStation,Telephone> ComputerPhones = new Dictionary<ComputerStation,Telephone>();

		public class ConfigData
		{
			[JsonProperty("ComputerPhones", ObjectCreationHandling = ObjectCreationHandling.Replace)]			
			public Dictionary<ulong,ulong> ComputerPhones = new Dictionary<ulong,ulong>();
			
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

		public delegate void RunCommand(BasePlayer bp, ComputerStation cs, Telephone tn);
		
		public void EnumerateLinks(){
			
			foreach(ulong u in config.ComputerPhones.Keys){
				Puts("["+u+" : "+config.ComputerPhones[u]+"]");
			}
		}
		void OnServerSave(){
			Puts("Saving");
			EnumerateLinks();
			SaveConfig();
		}
		void OnNewSave(string filename)
		{
			LoadConfig();
			config.ComputerPhones = new Dictionary<ulong,ulong>();
			SaveConfig();
			Puts("OnNewSave works!");
		}
		public static Telephone AddPhone(ComputerStation cs){
			Vector3 spawnspot = cs.transform.TransformPoint(new Vector3(-0.6f,0.73f,0.1f));
			Telephone sm = null;
			if(instance.config.ComputerPhones.ContainsKey(cs.net.ID.Value)){
				sm = BaseNetworkable.serverEntities.Find(new NetworkableId(instance.config.ComputerPhones[cs.net.ID.Value])) as Telephone;
					Console.WriteLine("Found Phone by cache");
			}
			if(sm==null){
				List<Telephone> powerPlantCandidates = new List<Telephone>();
				BaseEntity.Query.Server.GetInSphere(spawnspot, 3, powerPlantCandidates);
				
				if(powerPlantCandidates.Count()>1){
					sm=powerPlantCandidates[0];
					sm.SetParent(cs,true);
					Console.WriteLine("Found Phone");
				}else{
					ConVar.Entity.EntitySpawnRequest spawnEntityFromName = ConVar.Entity.GetSpawnEntityFromName("telephone");
					BaseEntity baseEntity = global::GameManager.server.CreateEntity(spawnEntityFromName.PrefabName, spawnspot,Quaternion.LookRotation(cs.transform.forward, Vector3.up), true);
					sm = baseEntity as Telephone;
					sm.Spawn();
					Console.WriteLine("Spawned Phone");
				}
				if(instance.config.ComputerPhones.ContainsKey(cs.net.ID.Value)){
					instance.config.ComputerPhones[cs.net.ID.Value]=sm.net.ID.Value;
				}
				else{
					instance.config.ComputerPhones.Add(cs.net.ID.Value,sm.net.ID.Value);
				}
			}
			sm.SetParent(cs,true);
			sm.pickup.enabled = false;
			if(ComputerPhones.ContainsKey(cs)){
				ComputerPhones[cs]=sm;
			}
			else{
				ComputerPhones.Add(cs,sm);
			}
			return sm;
			
		}
		Dictionary<RunCommand,float> CommandTapeItemspawnrates = new Dictionary<RunCommand,float>();
		Dictionary<string,float> PanelTapesItemspawnrates = new Dictionary<string,float>();
		
		Dictionary<string,RunCommand> CommandTapes = new Dictionary<string,RunCommand>();
		Dictionary<string,string> PanelTapes = new Dictionary<string,string>();
		
		private void Loaded(){
			instance=this;
			LoadConfig();
			foreach(ulong key in config.ComputerPhones.Keys){
				ComputerStation cs = BaseNetworkable.serverEntities.Find(new NetworkableId(key)) as ComputerStation;
				Telephone sm = BaseNetworkable.serverEntities.Find(new NetworkableId(config.ComputerPhones[key])) as Telephone;
				if(cs==null||sm==null){
					continue;
				}
				ComputerPhones.Add(cs,sm);
			}
		}
		private void Unload(){
			Puts("Saving");
			EnumerateLinks();
			SaveConfig();
		}
		void OnEntitySpawned(Telephone sm)
		{
			GroundWatch gw = sm.GetComponent<GroundWatch>();
			if(gw!=null){
				Puts("Deleting GroundWatch");
				UnityEngine.Object.Destroy(gw);
			}
			
		}
		void OnEntitySpawned(ComputerStation entity)
		{
			if(entity.gameObject.GetComponentInChildren<Telephone>()==null){
				timer.Once(1f,()=>{
					Telephone sm = AddPhone(entity);
					EnumerateLinks();
					});
				Puts("Station ID:"+entity.net.ID);
			}
		}
		
		public bool AddPanelTape(string name, string panel, float spawnrate){
			if(PanelTapes.ContainsKey(name)){
				PanelTapes.Remove(name);
			}
			PanelTapes.Add(name,panel);
			PanelTapesItemspawnrates.Add(panel,spawnrate);
			
			return false;
		}
		public bool AddCommandTape(string name, RunCommand panel, float spawnrate){
			if(CommandTapes.ContainsKey(name)){
				CommandTapes.Remove(name);
			}
			CommandTapes.Add(name,panel);
			CommandTapeItemspawnrates.Add(panel,spawnrate);
			return false;
		}
		
		object OnLootSpawn(LootContainer container)
		{
			foreach(string s in PanelTapes.Keys){
				if(PanelTapesItemspawnrates.ContainsKey(PanelTapes[s])){
					if(UnityEngine.Random.Range(0f,1f)<PanelTapesItemspawnrates[PanelTapes[s]]){
						Item cassetteItem = ItemManager.CreateByName("cassette", 1, 0UL);
						cassetteItem.name=s;
						cassetteItem.text = s;
						container.inventory.GiveItem(cassetteItem);
					}
				}
			}
			foreach(string s in CommandTapes.Keys){
				if(CommandTapeItemspawnrates.ContainsKey(CommandTapes[s])){
					if(UnityEngine.Random.Range(0f,1f)<CommandTapeItemspawnrates[CommandTapes[s]]){
						Item cassetteItem = ItemManager.CreateByName("cassette", 1, 0UL);
						cassetteItem.text = s;
						cassetteItem.name=s;
						container.inventory.GiveItem(cassetteItem);
					}
				}
			}
			return null;
		}
        [Command("GiveTape")]
        private void GiveTape(IPlayer player, string cmd, string[] args)
        {			
            BasePlayer basePlayer = player.Object as BasePlayer;
			if(basePlayer==null){
				return;
			}
			if(args.Length==0){
				return;
			}
			if(!PanelTapes.ContainsKey(args[0])&&!CommandTapes.ContainsKey(args[0])){
				return;
			}
			Item cassetteItem = ItemManager.CreateByName("cassette", 1, 0UL);
			cassetteItem.text = args[0];
			cassetteItem.name+=args[0];
			basePlayer.GiveItem(cassetteItem);
		}
        
		void OnEntityMounted(ComputerStation cs ,BasePlayer player){
			Telephone phone = cs.gameObject.GetComponentInChildren<Telephone>();
			Telephone remotePhone= GetRemotePhone(phone);
			Item i = null;
			Telephone target = FindTape(phone,remotePhone, cs ,player, out i);
			RunTape(i, player, cs, target);//
		}
		void OnPhoneDialTimeout(PhoneController pc, PhoneController activeCallTo, BasePlayer currentPlayer){
			if(connections.ContainsKey(pc.PhoneNumber)){
				connections[pc.PhoneNumber]=pc.lastDialedNumber;//
			}else{
				connections.Add(pc.PhoneNumber,pc.lastDialedNumber);
			}
			Puts("Connecting to "+activeCallTo.lastDialedNumber+" from "+pc.lastDialedNumber);
		}
		Telephone GetRemotePhone(Telephone phone){
			if(connections.ContainsKey(phone.Controller.PhoneNumber)){	
				Puts("found connection");			
				PhoneController target = TelephoneManager.GetTelephone(connections[phone.Controller.PhoneNumber]);
				if(target==null){return null;}
				Puts(target.PhoneNumber.ToString());
				return target.ParentEntity as Telephone;
			}
			Puts("no connection");			
			return null;
		}
			
		Telephone FindTape(Telephone phone, Telephone remotePhone, ComputerStation cs ,BasePlayer player, out Item i){
			Telephone result = phone;
			if(phone==null){
				i=null;
				return result;
			}
			i = phone.inventory.GetSlot(0);
			if(i==null){
				if(remotePhone!=null){
					i = remotePhone.inventory.GetSlot(0);
					Puts("Remote tape found");
					result = remotePhone;
				}
			}
			if(i==null){
				Puts("No tape found");
				return null;
			}
			Puts("Tape found");
			return result;
		}
		
		
		public void RedirectConnection(ComputerStation cs, BasePlayer player, int phonenumber){
			PhoneController target = TelephoneManager.GetTelephone(phonenumber);
			//Puts("find "+phonenumber);
			//Puts("Found target");
			Telephone remotePhone = ComputerPhones[cs];
			if(target!=null){
				remotePhone = target.ParentEntity as Telephone;
			}
			connections[ComputerPhones[cs].Controller.PhoneNumber] = phonenumber;
			Item i = null;
			FindTape(ComputerPhones[cs],remotePhone, cs ,player, out i);
			//Puts("Run tape "+i.name);
			RunTape(i, player, cs, remotePhone);//
		}
		
		[Command("ListTapes")]
        private void ListTapes(IPlayer player, string cmd, string[] args)
        {	
			Puts("Command Tapes");
			foreach(string s in CommandTapes.Keys){
				Puts("  ["+s+"]");
			}
			Puts("Panel Tapes");
			foreach(string s in PanelTapes.Keys){
				Puts("  ["+s+"]");
			}
		}
		[Command("RedirectConnection")]
        private void RedirectConnection(IPlayer player, string cmd, string[] args)
        {	
			Puts("Redirecting?");
            BasePlayer basePlayer = player.Object as BasePlayer;
			if(basePlayer==null){
				return;
			}
			//Puts("Has Baseplayer");
			ComputerStation cs = basePlayer.GetMounted() as ComputerStation;
			if(cs==null){return;}
			//Puts("Has ComputerStation");
			
			int phonenumber=0;
			if(args.Length==0 || !int.TryParse(args[0], out phonenumber)){return;}

			if(args.Length>1 ){
				CommunityEntity.ServerInstance.ClientRPC<string>(RpcTarget.Player("DestroyUI", basePlayer.Connection),args[1]);
			}
			
			//Puts("Redirection");
			RedirectConnection(cs,basePlayer,phonenumber);
			
		}
		bool RunTape(Item i, BasePlayer player, ComputerStation cs, Telephone sm){
			if(i==null){return false;}
			string[] keyArr = i.name.Split(":");
			string key = "";
			Puts(i.name);
			if(keyArr.Length>0){
				for(int j = 0; j<keyArr.Length&&key=="";j++){
					key=keyArr[j];
				}
			}else{
				key=i.name;
				}
			
			//Puts(key);
			if(CommandTapes.ContainsKey(key)){
				//Puts("Running command");
				CommandTapes[key](player,cs,sm);
				cs.SetFlag(global::BaseEntity.Flags.On, false, false, true);
				return true;
			}
			if(PanelTapes.ContainsKey(key)){
				//Puts("Running panel");
				CommunityEntity.ServerInstance.ClientRPC<string>(RpcTarget.Player("AddUI", player.Connection),(PanelTapes[key]));
				cs.SetFlag(global::BaseEntity.Flags.On, false, false, true);
				return true;
			}
			return false;
		}
		//Interface.CallHook("OnBookmarkAdd", this, player, text) != null)
		//System.Object OnBookmarkAdd(ComputerStation cs, BasePlayer player,string text=""){
		//}
	}
	
}