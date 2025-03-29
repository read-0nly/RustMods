
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
	[Info("Cordyceps", "obsol", "0.0.1")]
	[Description("A baseline for swapping out custom AI state handlers without lobotomies.")]
	public class Cordyceps : CovalencePlugin
	{
		private Game.Rust.Libraries.Player _rustPlayer = Interface.Oxide.GetLibrary<Game.Rust.Libraries.Player>("Player");
		private void SendChatMsg(BasePlayer pl, string msg) => _rustPlayer.Message(pl, msg,  "<color=#00ff00>[Cordyceps]</color>", 0, Array.Empty<object>());	
		public static Dictionary<string,Dictionary<AIState,BaseAIBrain.BasicAIState>> StateAssignments = 
			new Dictionary<string,Dictionary<AIState,BaseAIBrain.BasicAIState>>();
		public static Dictionary<string,Dictionary<AIState,BaseAIBrain.BasicAIState>> AnimalStateAssignments = 
			new Dictionary<string,Dictionary<AIState,BaseAIBrain.BasicAIState>>();
		public Configuration config;
		public bool WalkableOnly = false;
		
		public class Configuration
		{
			[JsonProperty("Name", ObjectCreationHandling = ObjectCreationHandling.Replace)]
			public string name = "";
			public string ToJson() => JsonConvert.SerializeObject(this);				
			public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
			
		}
		protected override void LoadDefaultConfig() => config = new Configuration();//
		protected override void LoadConfig(){
			base.LoadConfig();
			try{
				config = Config.ReadObject<Configuration>();
				if (config == null) throw new JsonException();
				if (!config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys)){
					LogWarning("Configuration appears to be outdated; updating and saving");SaveConfig();}
			}
			catch(Exception e){LogWarning($"Configuration file {Name}.json is invalid; using defaults");LogWarning(e.ToString());LoadDefaultConfig();}
		}
		protected override void SaveConfig(){
			LogWarning($"Configuration changes saved to {Name}.json");
			Config.WriteObject(config, true);
		}		
		private void OnServerInitialized()
        {
			LoadConfig();
		}
		
		// Requires custom injection : BaseAIBrain.InitializeAI() injection index 0, continue, just this//
		void OnAIInitialize(BaseAIBrain brain)
        {
            ApplyDesign(brain.Navigator.BaseEntity);
		}
		
		public bool ApplyDesign(BaseEntity entity){
			HumanNPC hn = (entity as HumanNPC);
			BaseAnimalNPC bn = (entity as BaseAnimalNPC);
			if(hn==null){
				if(bn!=null){
					return SwapAnimalState(bn.brain);
				}
				return false;
			}else{		
				hn.GetComponent<BaseNavigator>().PlaceOnNavMesh(0);
				hn.GetComponent<BaseNavigator>().TriggerStuckEvent = true;
				hn.Brain.AllowedToSleep=false;
				return SwapHumanState(hn.Brain);
			}
		}
		public bool AssignHumanState(string prefabname, AIState stateType, BaseAIBrain.BasicAIState state){
			if(!StateAssignments.ContainsKey(prefabname)){
				StateAssignments.Add(prefabname,new Dictionary<AIState,BaseAIBrain.BasicAIState>());
			}
			if(!StateAssignments[prefabname].ContainsKey(stateType)){
				StateAssignments[prefabname].Add(stateType,state);
			}else{
				StateAssignments[prefabname][stateType]=state;
			}
			return true;
		}
		public bool AssignAnimalState(string prefabname, AIState stateType, BaseAIBrain.BasicAIState state){
			if(!AnimalStateAssignments.ContainsKey(prefabname)){
				AnimalStateAssignments.Add(prefabname,new Dictionary<AIState,BaseAIBrain.BasicAIState>());
			}
			if(!AnimalStateAssignments[prefabname].ContainsKey(stateType)){
				AnimalStateAssignments[prefabname].Add(stateType,state);
			}else{
				AnimalStateAssignments[prefabname][stateType]=state;
			}
			return true;
		}
		public bool SwapHumanState(BaseAIBrain brain,AIState stateType,BaseAIBrain.BasicAIState state){
			try{
				bool isInState = false;
				if (brain.CurrentState != null)
					if (brain.CurrentState.StateType == stateType)
					{
						brain.SwitchToState(AIState.Idle, 0);
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
		}
		public bool SwapAnimalState(BaseAIBrain brain,AIState stateType,BaseAIBrain.BasicAIState state){
			try{
				if (brain.states.ContainsKey(stateType)){
					if(brain.CurrentState!=null)
						if(brain.CurrentState.StateType==stateType)
							brain.states[stateType].StateLeave(brain,brain.Navigator.BaseEntity);
				}
				
				BaseAIBrain.BasicAIState state2 = (BaseAIBrain.BasicAIState)System.Activator.CreateInstance(state.GetType());
				state2.brain = brain;
				brain.states[stateType]=state2;
				return true;
			}catch(Exception e){
				Puts(e.ToString());
				return false;
			}
		}
		public bool SwapAnimalState(BaseAIBrain brain){
			bool result = false;
			if(AnimalStateAssignments.ContainsKey(brain.gameObject.transform.name)){
				foreach(AIState state in AnimalStateAssignments[brain.gameObject.transform.name].Keys){
					bool stateResult = (SwapAnimalState(brain,state,AnimalStateAssignments[brain.gameObject.transform.name][state]));
					result = result || stateResult;
				}
				return result;	
			}
			return false;
		}

		public bool SwapHumanState(BaseAIBrain brain){
			bool result = false;
            //Puts(brain.gameObject.transform.name +":" + (StateAssignments.ContainsKey(brain.gameObject.transform.name)).ToString());
            if (StateAssignments.ContainsKey(brain.gameObject.transform.name)){
				foreach(AIState state in StateAssignments[brain.gameObject.transform.name].Keys){
					bool stateResult = (SwapHumanState(brain,state,StateAssignments[brain.gameObject.transform.name][state]));
					result = result || stateResult;
				}
				return result;	
			}
			return false;
		}

	}
}

