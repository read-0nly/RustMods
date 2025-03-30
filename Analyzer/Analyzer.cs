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
	[Info("Analyzer", "obsol", "0.0.1")]
	[Description("Allows rewiring of State-Event maps using JSON, and applying that structure to instance/prefab/global")]
	public class Analyzer : CovalencePlugin//
	{
		private Game.Rust.Libraries.Player _rustPlayer = Interface.Oxide.GetLibrary<Game.Rust.Libraries.Player>("Player");
		private void SendChatMsg(BasePlayer pl, string msg) => _rustPlayer.Message(pl, msg,  "<color=#00ff00>[Analyzer]</color>", 0, Array.Empty<object>());
		public Configuration config;
		
		public class Configuration
		{
			public class Target{
				string target;
				List<string> programs;
			}
			[JsonProperty("targets", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public List<Target> targets=new List<Target>();
			public string ToJson() => JsonConvert.SerializeObject(this);				
			public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
		
		}
		protected override void LoadDefaultConfig() => config = new Configuration();//
		protected override void LoadConfig(){
			base.LoadConfig();
			try{
				config = Config.ReadObject<Configuration>();
				if (config == null) throw new JsonException();
				if (!config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys)){LogWarning("Configuration appears to be outdated; updating and saving");SaveConfig();}
			}catch(Exception e){LogWarning($"Configuration file {Name}.json is invalid; using defaults");LogWarning(e.ToString());LoadDefaultConfig();SaveConfig();}
		}
		protected override void SaveConfig(){
			LogWarning($"Configuration changes saved to {Name}.json");
			Config.WriteObject(config, true);
		}
		
		
		void OnPlayerSleepEnded(BasePlayer player)
		{
			SendChatMsg(player, "ANALYZER ACTIVE");
			
		}
		
		[Command("anal-nav")] void navigation_scan_cmd(IPlayer player, string command, string[] args){	
			BasePlayer baseplayer = (BasePlayer)player.Object;
			int mask = 25;
			if(baseplayer==null||!player.HasPermission("Analyzer.nav")){
				SendChatMsg(baseplayer, "missing perm: Analyzer.nav");
				return;
			}			

			if(args.Length>0){
				try{
					mask=int.Parse(args[0]);
				}
				catch(Exception e){
				}
			}
			SendChatMsg(baseplayer, baseplayer.transform.position.ToString());
			navigation_scan(baseplayer.transform.position,10,30,mask, baseplayer);
		}
		public void navigation_scan(Vector3 origin, int range, int decay, int mask, BasePlayer rx){
			for(int i = -range; i<=range;i++){
				for(int j = -range; j<=range;j++){
					int offset_x = i*10;
					int offset_z = j*10;
					Vector3 point = new Vector3(origin.x+offset_x,origin.y,origin.z+offset_z);
					UnityEngine.AI.NavMeshHit sampled_point = new UnityEngine.AI.NavMeshHit();
					
					UnityEngine.AI.NavMesh.SamplePosition(point,out sampled_point,5,mask);
					UnityEngine.Color dot_color= UnityEngine.Color.red;
					string dot_text = "∙";
					if(sampled_point.hit){
							dot_color=UnityEngine.Color.green;
							dot_text=sampled_point.mask.ToString();
							point = sampled_point.position;
					}
					ConsoleNetwork.SendClientCommand(rx.Connection,"ddraw.text", new object[]
					{
						decay,
						dot_color,
						point,
						dot_text
					});
					
				}
				
			}
		}
		void OnServerInitialized()
        {
			LoadConfig();
			permission.RegisterPermission("Analyzer.nav", this);
		}
		/*
		void ApplyDesign(BaseEntity entity, ProtoBuf.AIDesign aid){
			HumanNPC hn = (entity as HumanNPC);
			BaseNpc bn = (entity as BaseNpc);
			if(hn==null){
				if(bn==null) return;
				BaseAIBrain brain = (bn.GetComponent<BaseAIBrain>());
				if (brain==null){return;}//
				brain.InstanceSpecificDesign = aid;
				if(bn.HasBrain) brain.LoadAIDesignAtIndex(0);
				brain.AllowedToSleep=false;
				Puts("Processed "+entity.transform.name);
					
			}else{
				BaseAIBrain brain = (hn.GetComponent<BaseAIBrain>());
				(hn as HumanNPC).VirtualInfoZone =null;
				(hn as HumanNPC).cachedInfoZone = AIInformationZone.GetForPoint(hn.transform.position, true);
				if (brain==null){ brain = (hn.GetComponent<ScientistBrain>() as BaseAIBrain);}
				if (brain==null){return;}//
				brain.InstanceSpecificDesign = aid;
				if(hn.HasBrain) brain.LoadAIDesignAtIndex(0);
				hn.Brain.AllowedToSleep=false;
				
				Puts("Processed "+entity.transform.name);
			}
		}
		public bool SwapState(BaseAIBrain brain,AIState stateType,BaseAIBrain.BasicAIState state){
			try{
				bool isInState = false;
				if (brain.CurrentState != null)
					if (brain.CurrentState.StateType == stateType)
					{
						brain.states[stateType].StateLeave(brain, brain.Navigator.BaseEntity);
						isInState = true;

                    }
				BaseAIBrain.BasicAIState state2 = (BaseAIBrain.BasicAIState)System.Activator.CreateInstance(state.GetType());
				
				System.Console.ForegroundColor = ConsoleColor.Cyan;
				System.Console.WriteLine(state.GetType().ToString() + " Set for "+brain.transform.name);
				System.Console.ResetColor();
				state2.brain = brain;
				brain.states[stateType]=state2;

                if(isInState) brain.SwitchToState(stateType, 0);
                return true;
			}catch(Exception e){
				Puts(e.ToString());
				return false;
			}
		}*/
	}
}