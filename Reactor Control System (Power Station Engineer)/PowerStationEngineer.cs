//Requires: COBALTOS
//Requires: PowerTransmission
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
    [Info("Power Station Engineer", "obsol", "0.0.2")]
    [Description("Implements into CobaltOS to add a way to turn on and manage the power station to power the island")]
    internal class PowerStationEngineer : CovalencePlugin
    {
		//assets/content/props/powerline_poles/powerline_pole_electric_box.prefab
		//assets/prefabs/deployable/playerioents/fluidsplitter/fluidsplitter.prefab
		//-0.08 0.8 0.013 : 0 295.5 90
		//assets/prefabs/deployable/playerioents/splitter/splitter.prefab
		//0 2 0.09 : 0 0 0 
		
		
		//assets/bundled/prefabs/autospawn/monument/large/powerplant_1.prefab
		
		//assets/prefabs/deployable/computerstation/computerstation.deployed.prefab
		//-37.35 22.25 10.1 : 0 350 0
		
		//assets/prefabs/deployable/playerioents/generators/generator.small.prefab//
		
		ElectricGenerator stationGenerator;
		ComputerStation stationComputer;
		Splitter rootPowerSplitter;
		HashSet<Splitter> powerSplitters = new HashSet<Splitter>();
		HashSet<BasePlayer> players = new HashSet<BasePlayer>();
		MonumentInfo mi = null;
		
		Dictionary<Vector2Int, Vector3> poleCandidates = new Dictionary<Vector2Int, Vector3>();
		Timer nucleationTimer;
		
		int pump = 10;
		int rods = 0;
		
		int pump_max=100;
		int rods_max=1000;
		
		float temp = 100f;
		float flow = 600f;
		float powerout=303f;
		
		float temp_max = 1000;
		float flow_max = 1000;
		float power_max = 1000;
		
		
		#region mainPanel
		string mainPanel=@"
					
[	
	{
		""name"": ""ReactorControlSystem2"",
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
		""parent"": ""ReactorControlSystem2"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""0.2 0.5 0.7 1"",
				""text"":""REACTOR CONTROL SYSTEM"",
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
		""parent"": ""ReactorControlSystem2"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""0.2 0.5 0.7 1"",
				""text"":""Version:0.3.9.1
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
		""name"": ""PowerOut"",
		""parent"": ""ReactorControlSystem2"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Image"",
				""imagetype"": ""Tiled"",
				""color"": ""0 0 0 1.0"",
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.3 0.41"",
				""anchormax"": ""0.7 0.46""
			},
			{
				""type"":""NeedsCursor""
			}
		]
	},
	{
		""name"": ""PowerOutInner"",
		""parent"": ""PowerOut"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Image"",
				""imagetype"": ""Tiled"",
				""color"": ""0.0255 0.8 0.03 0.5"",
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.01 0.1"",
				""anchormax"": ""0.75 0.9""
			},
			{
				""type"":""NeedsCursor""
			}
		]
	},
	{
		""name"": ""Temperature"",
		""parent"": ""ReactorControlSystem2"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Image"",
				""imagetype"": ""Tiled"",
				""color"": ""0 0 0 1.0"",
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.3 0.48"",
				""anchormax"": ""0.7 0.53""
			},
			{
				""type"":""NeedsCursor""
			}
		]
	},
	{
		""name"": ""TemperatureInner"",
		""parent"": ""Temperature"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Image"",
				""imagetype"": ""Tiled"",
				""color"": ""0.8 0.255 0.03 0.5"",
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.01 0.1"",
				""anchormax"": ""0.75 0.9""
			},
			{
				""type"":""NeedsCursor""
			}
		]
	},
	{
		""name"": ""Water"",
		""parent"": ""ReactorControlSystem2"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Image"",
				""imagetype"": ""Tiled"",
				""color"": ""0 0 0 1.0"",
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.3 0.55"",
				""anchormax"": ""0.7 0.60""
			},
			{
				""type"":""NeedsCursor""
			}
		]
	},
	{
		""name"": ""WaterInner"",
		""parent"": ""Water"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Image"",
				""imagetype"": ""Tiled"",
				""color"": ""0.0255 0.03 0.8 0.5"",
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.01 0.1"",
				""anchormax"": ""0.75 0.9""
			},
			{
				""type"":""NeedsCursor""
			}
		]
	},
	
	
	{
		""name"": ""LowerRodsStep"",
		""parent"": ""ReactorControlSystem2"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Button"",
				""command"":""PSERods -10"",
				""color"": ""0.05 0.08 0.13 1"",
				""imagetype"": ""Tiled""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.65 0.33"",
				""anchormax"": ""0.7 0.38""
			}
		]
	},
	{
		""parent"": ""LowerRodsStep"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""0.2 0.5 0.7 1"",
				""font"":""DroidSansMono.ttf"",
				""text"":""--"",
				""align"": ""MiddleCenter""
			}
		]
	},
	{
		""name"": ""RaiseRodsStep"",
		""parent"": ""ReactorControlSystem2"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Button"",
				""command"":""PSERods 10"",
				""color"": ""0.05 0.08 0.13 1"",
				""imagetype"": ""Tiled""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.3 0.33"",
				""anchormax"": ""0.35 0.38""
			}
		]
	},
	{
		""parent"": ""RaiseRodsStep"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""0.2 0.5 0.7 1"",
				""font"":""DroidSansMono.ttf"",
				""text"":""++"",
				""align"": ""MiddleCenter""
			}
		]
	},
	{
		""name"": ""LowerRods"",
		""parent"": ""ReactorControlSystem2"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Button"",
				""command"":""PSERods -1"",
				""color"": ""0.05 0.08 0.13 1"",
				""imagetype"": ""Tiled""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.55 0.33"",
				""anchormax"": ""0.64 0.38""
			}
		]
	},
	{
		""parent"": ""LowerRods"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""0.2 0.5 0.7 1"",
				""font"":""DroidSansMono.ttf"",
				""text"":""CTRL -"",
				""align"": ""MiddleCenter""
			}
		]
	},
	{
		""name"": ""RodOutput"",
		""parent"": ""ReactorControlSystem2"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Image"",
				""imagetype"": ""Tiled"",
				""color"": ""0 0 0 1.0"",
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.46 0.33"",
				""anchormax"": ""0.54 0.38""
			},
			{
				""type"":""NeedsCursor""
			}
		]
	},
	{
		""name"": ""RodOutputInner"",
		""parent"": ""RodOutput"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Image"",
				""imagetype"": ""Tiled"",
				""color"": ""0.2 0.5 0.7 1"",
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.05 0.1"",
				""anchormax"": ""0.75 0.9""
			},
			{
				""type"":""NeedsCursor""
			}
		]
	},
	{
		""name"": ""RaiseRods"",
		""parent"": ""ReactorControlSystem2"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Button"",
				""command"":""PSERods 1"",
				""color"": ""0.05 0.08 0.13 1"",
				""imagetype"": ""Tiled""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.36 0.33"",
				""anchormax"": ""0.45 0.38""
			}
		]
	},
	{
		""parent"": ""RaiseRods"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""0.2 0.5 0.7 1"",
				""font"":""DroidSansMono.ttf"",
				""text"":""+ CTRL"",
				""align"": ""MiddleCenter""
			}
		]
	},
	
	
	{
		""name"": ""LowerFlow"",
		""parent"": ""ReactorControlSystem2"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Button"",
				""command"":""PSEFlow -1"",
				""color"": ""0.05 0.08 0.13 1"",
				""imagetype"": ""Tiled""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.55 0.25"",
				""anchormax"": ""0.7 0.30""
			}
		]
	},
	{
		""parent"": ""LowerFlow"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""0.2 0.5 0.7 1"",
				""font"":""DroidSansMono.ttf"",
				""text"":""Pump -"",
				""align"": ""MiddleCenter""
			}
		]
	},
	{
		""name"": ""FlowOutput"",
		""parent"": ""ReactorControlSystem2"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Image"",
				""imagetype"": ""Tiled"",
				""color"": ""0 0 0 1.0"",
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.46 0.25"",
				""anchormax"": ""0.54 0.30""
			},
			{
				""type"":""NeedsCursor""
			}
		]
	},
	{
		""name"": ""FlowOutputInner"",
		""parent"": ""FlowOutput"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Image"",
				""imagetype"": ""Tiled"",
				""color"": ""0.2 0.5 0.7 1"",
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.05 0.1"",
				""anchormax"": ""0.75 0.9""
			},
			{
				""type"":""NeedsCursor""
			}
		]
	},
	{
		""name"": ""RaiseFlow"",
		""parent"": ""ReactorControlSystem2"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Button"",
				""command"":""PSEFlow 1"",
				""color"": ""0.05 0.08 0.13 1"",
				""imagetype"": ""Tiled""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.3 0.25"",
				""anchormax"": ""0.45 0.30""
			}
		]
	},
	{
		""parent"": ""RaiseFlow"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""0.2 0.5 0.7 1"",
				""font"":""DroidSansMono.ttf"",
				""text"":""+Pump"",
				""align"": ""MiddleCenter""
			}
		]
	},
	
	
	{
		""name"": ""IgniteButton"",
		""parent"": ""ReactorControlSystem2"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Button"",
				""command"":""PSEIgnite"",
				""color"": ""0.05 0.08 0.13 1"",
				""imagetype"": ""Tiled""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.3 0.11"",
				""anchormax"": ""0.7 0.16""
			}
		]
	},
	{
		""parent"": ""IgniteButton"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""0.2 0.5 0.7 1"",
				""font"":""DroidSansMono.ttf"",
				""fontsize"":20,
				""text"":""IGNITE"",
				""align"": ""MiddleCenter""
			}
		]
	},
	
	
	{
		""name"": ""ScramButton"",
		""parent"": ""ReactorControlSystem2"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Button"",
				""command"":""PSEScram"",
				""color"": ""0.05 0.08 0.13 1"",
				""imagetype"": ""Tiled""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.3 0.17"",
				""anchormax"": ""0.7 0.22""
			}
		]
	},
	{
		""parent"": ""ScramButton"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""0.2 0.5 0.7 1"",
				""font"":""DroidSansMono.ttf"",
				""fontsize"":20,
				""text"":""SCRAM"",
				""align"": ""MiddleCenter""
			}
		]
	},
	
	
	{
		""name"": ""CloseButton"",
		""parent"": ""ReactorControlSystem2"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Button"",
				""command"":""PSEDisconnect"",
				""color"": ""0.05 0.08 0.13 1"",
				""imagetype"": ""Tiled""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.3 0.05"",
				""anchormax"": ""0.7 0.1""
			}
		]
	},
	{
		""parent"": ""CloseButton"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""0.2 0.5 0.7 1"",
				""font"":""DroidSansMono.ttf"",
				""fontsize"":20,
				""text"":""Close"",
				""align"": ""MiddleCenter""
			}
		]
	}
]
		";
		#endregion
		#region update panel
		void updateControl(){
			float pump_ratio = 0.98f*pump/pump_max;//
			float rods_ratio = 0.98f*rods/rods_max;
			string ctrlString = @"
			[
			{
				""name"": ""FlowOutputInner"",
				""parent"": ""FlowOutput"",
				""update"": true,
				""components"":
				[
					{
						""type"":""RectTransform"",
						""anchormin"": ""0.05 0.1"",
						""anchormax"": """+pump_ratio+@" 0.9""
					}
				]
			},
			{
				""name"": ""RodOutputInner"",
				""parent"": ""RodOutput"",
				""update"": true,
				""components"":
				[
					{
						""type"":""RectTransform"",
						""anchormin"": ""0.05 0.1"",
						""anchormax"": """+rods_ratio+@" 0.9""
					}
				]
			}
			]";
			foreach(BasePlayer bp in players){
				CommunityEntity.ServerInstance.ClientRPC<string>(RpcTarget.Player("AddUI", bp.Connection),ctrlString);
			}
		}
		void updatePanel(){
			float temp_ratio = 0.98f*((temp>temp_max?temp_max:temp)/temp_max);
			float flow_ratio = 0.98f*((flow>flow_max?flow_max:flow)/flow_max);
			float powerout_ratio = 0.98f*((powerout>power_max?power_max:powerout)/power_max);
			string newPanel= @"
		
[	
	{
		""name"": ""PowerOutInner"",
		""parent"": ""PowerOut"",
		""update"": true,
		""components"":
		[
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.01 0.1"",
				""anchormax"": """+powerout_ratio+@" 0.9""
			}
		]
	},
	{
		""name"": ""TemperatureInner"",
		""parent"": ""Temperature"",
		""update"": true,
		""components"":
		[
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.01 0.1"",
				""anchormax"": """+temp_ratio+@" 0.9""
			}
		]
	},
	{
		""name"": ""WaterInner"",
		""parent"": ""Water"",
		""update"": true,
		""components"":
		[
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.01 0.1"",
				""anchormax"": """+flow_ratio+@" 0.9""
			}
		]
	}
]

		";
			foreach(BasePlayer bp in players){
				CommunityEntity.ServerInstance.ClientRPC<string>(RpcTarget.Player("AddUI", bp.Connection),newPanel);
			}
				
		}
		private void ShowToPlayer(BasePlayer bp, ComputerStation cs=null){
			if(!players.Contains(bp)){
				players.Add(bp);
			}				
			CommunityEntity.ServerInstance.ClientRPC<string>(RpcTarget.Player("AddUI", bp.Connection),mainPanel);
			updatePanel();
			updateControl();
		}
		#endregion
       
	    #region Nuclear sim
		private float sigmoid(float x, float offset, float curve = 1, bool inverted = true)
        {
            float y = 0;
            y = (((inverted ? -1 : 1) * 1.0f) / (1 + Mathf.Pow(2.7182817f, 0 - (curve * (x - offset))))) + (inverted ? 1 : 0);
            if (y == 0) { y = 0; }
            return y;
        }
		
		void stepNucleation(){			
            temp += (powerout / 10);
            if (float.IsNaN(temp)) { temp = 0; }
            flow += (temp > 0 ? Mathf.Sqrt(temp) * pump : pump);
            if (float.IsNaN(flow)) { flow = 0; }
            temp -= Mathf.Sqrt(temp);
            if (float.IsNaN(temp)) { temp = 0; }
            powerout = Mathf.Pow((flow * 2), 1 / 1.3f) * Mathf.Sqrt(temp / (temp_max / 10));
            if (float.IsNaN(powerout)) { powerout = 0; }
            flow -= powerout;
            if (float.IsNaN(flow)) { flow = 0; }
            temp = (temp + (temp * sigmoid(rods, 500, 0.013f))) / 2;
            if (float.IsNaN(temp)) { temp = 0; }

            if (flow < 0)
            {
                temp += 0 - flow;
                flow = 0;
            }

            if (temp < 0)
            {
                powerout += temp;
                temp = 0;
            }
            if (powerout < 0)
            {
                powerout = 0;
            }
			if(flow>flow_max||temp>temp_max){
				Scram();
			}
			stationGenerator.electricAmount=powerout;//
			stationGenerator.MarkDirty();
		}
		
		private void Scram(){
			pump=0;
			rods=(rods==rods_max?0:rods_max);
			updateControl();
			Puts("Scram");
		}
		#endregion
		
		#region commands
		[Command("PSEScram")]
        private void PSEScram(IPlayer player, string cmd, string[] args)
        {
			Scram();
		}
		[Command("PSEDisconnect")]
        private void PSEDisconnect(IPlayer player, string cmd, string[] args)
        {
            BasePlayer basePlayer = player.Object as BasePlayer;
			if(basePlayer==null||!players.Contains(basePlayer)){
				return;
			}
			CommunityEntity.ServerInstance.ClientRPC<string>(RpcTarget.Player("DestroyUI", 
				basePlayer.Connection), "ReactorControlSystem2");
			players.Remove(basePlayer);
		}
		[Command("PSEIgnite")]
        private void PSEIgnite(IPlayer player, string cmd, string[] args)
        {
			temp=(temp<50?50:temp);
			updatePanel();
			Puts("PSEIgnite");
		}
		[Command("PSEFlow")]
        private void PSEFlow(IPlayer player, string cmd, string[] args)
        {
			if(args.Length>0){
				pump+=Int32.Parse(args[0]);
				pump=Mathf.Clamp(pump,0,pump_max);
				Puts("PSEFlow changed "+pump);
				updateControl();
			}
		}
		[Command("PSERods")]
        private void PSERods(IPlayer player, string cmd, string[] args)
        {
			if(args.Length>0){
				rods+=Int32.Parse(args[0]);
				rods=Mathf.Clamp(rods,0,rods_max);
				Puts("Rods changed "+rods);
				updateControl();
			}
		}
		
		[Command("PowerApp")]
        private void PowerApp(IPlayer player, string cmd, string[] args)
        {
            BasePlayer basePlayer = player.Object as BasePlayer;
			if(basePlayer==null){
				return;
			}
			ShowToPlayer(basePlayer);
		}
		
		#endregion
		
		
		
		void Loaded(){
			COBALTOS.instance.AddCommandTape("Reactor Control System",ShowToPlayer,1f);
		}
		void Unload(){//
			Puts("Unloading");
			nucleationTimer.Destroy();
			foreach(BasePlayer bp in players.ToArray()){
				CommunityEntity.ServerInstance.ClientRPC<string>(RpcTarget.Player("DestroyUI", 
					bp.Connection), "ReactorControlSystem2");
				players.Remove(bp);
			}
		}
		void OnServerInitialized(){//
			/*foreach(GameObject go in Resources.FindObjectsOfTypeAll<GameObject>()){
				if(!go.activeSelf){ continue;}
				if(go.transform.name.ToLower().Contains("powerline_pole_electric_box")){
					Vector2Int gridPos = new Vector2Int((int)go.transform.position.x/10,(int)go.transform.position.z/10);
					if(!poleCandidates.ContainsKey(gridPos)){
						poleCandidates.Add(gridPos, go.transform.position);
					}
				}
			}
			foreach(Vector2Int vi in poleCandidates.Keys){
				Puts("Candidate pole: "+vi.ToString()+poleCandidates[vi].ToString());
			}*/
			foreach(MonumentInfo mi2 in TerrainMeta.Path.Monuments){
				if(mi2.transform.name.ToLower()=="assets/bundled/prefabs/autospawn/monument/large/powerplant_1.prefab"){
					mi=mi2;
					Puts(mi.displayPhrase.english.ToLower()+"\t\t:"+mi.transform.name);
					Puts("Found Power");
					break;
				}
			}
			spawnParts();
			
			
			nucleationTimer=timer.Every(5f,()=>{
				stepNucleation();
				updatePanel();
				updateControl();
			});
		}
		void spawnParts(){
			if(mi==null){return;}
			Vector3 spawnspot = mi.transform.TransformPoint(new Vector3(-37.35f, 12.25f, 10.1f));
			List<ComputerStation> powerPlantCandidates = new List<ComputerStation>();
			BaseEntity.Query.Server.GetInSphere(spawnspot, 3, powerPlantCandidates);
			if(powerPlantCandidates.Count()>1){
				stationComputer=powerPlantCandidates[0];
				Console.WriteLine("linke PowerCntrol?");///
			}else{
				spawnPC(spawnspot);
			}
			spawnspot = mi.transform.TransformPoint(new Vector3(-34.87f, 12.25f, 7.62f));
			List<ElectricGenerator> powerGenCandidates = new List<ElectricGenerator>();
			BaseEntity.Query.Server.GetInSphere(spawnspot, 3, powerGenCandidates);
			if(powerGenCandidates.Count()>1){
				stationGenerator=powerGenCandidates[0];
				Console.WriteLine("linke gen?");////
			}else{
				spawnPower(spawnspot);
			}
			Telephone tele = stationComputer.gameObject.GetComponentInChildren<Telephone>();
			if(tele==null){Puts("No phone");return;}
			stationGenerator.ConnectTo(tele,1,0);
			stationGenerator.UpdateOutputs();
			stationGenerator.MarkDirty();
			RFBroadcaster tx = spawnBroadcaster(stationGenerator);
			stationGenerator.ConnectTo(tx,2,0);
			RFManager.ChangeFrequency(1, 333, tx, false);
		}
		void spawnPower(Vector3 spawnspot){			
			ConVar.Entity.EntitySpawnRequest spawnEntityFromName = ConVar.Entity.GetSpawnEntityFromName("generator.small");//
			global::ElectricGenerator baseEntity = (global::GameManager.server.CreateEntity(spawnEntityFromName.PrefabName, spawnspot,Quaternion.AngleAxis(5.5f, Vector3.up), true) as ElectricGenerator);
			if(baseEntity==null){Puts("Nope");return;}			
			stationGenerator = baseEntity;
			baseEntity.pickup.enabled = false;
			baseEntity.Spawn();
			Console.WriteLine("Spawned gen?");//////
		}
		RFBroadcaster spawnBroadcaster(ElectricGenerator gen){	
			ConVar.Entity.EntitySpawnRequest spawnEntityFromName = ConVar.Entity.GetSpawnEntityFromName("rfbroadcaster");//
			RFBroadcaster baseEntity = (GameManager.server.CreateEntity(spawnEntityFromName.PrefabName, gen.transform.position+new Vector3(0,0.75f,0.5f),
				Quaternion.LookRotation(gen.transform.right, Vector3.up), true) as RFBroadcaster);
			if(baseEntity==null){Puts("Nope");return null;}	
			
			baseEntity.pickup.enabled = false;
			baseEntity.Spawn();//
			return baseEntity ;
			
		}
		void spawnPC(Vector3 spawnspot){			
			ConVar.Entity.EntitySpawnRequest spawnEntityFromName = ConVar.Entity.GetSpawnEntityFromName("computerstation.deployed");//
			global::ComputerStation baseEntity = (global::GameManager.server.CreateEntity(spawnEntityFromName.PrefabName, spawnspot,Quaternion.AngleAxis(260, Vector3.up), true) as ComputerStation);
			if(baseEntity==null){Puts("Nope");return;}			
			stationComputer = baseEntity;
			baseEntity.pickup.enabled = false;
			baseEntity.Spawn();
			Console.WriteLine("Spawned PowerCntrol?");///
		}
	}
	
}