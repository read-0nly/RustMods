
/*
    [ServerVar(Help = "Show user info for players on server.")]
    public static void users(ConsoleSystem.Arg arg)
    {
*/

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
using UnityEngine; 
using UnityEngine.AI; 
using Oxide.Core.Libraries.Covalence;
using Oxide.Plugins;
using Oxide.Core.Plugins;
using System.Threading;
using Oxide.Core;

namespace Oxide.Plugins
{
	[Info("JumpTest", "obsol", "0.0.1")]
	[Description("Re-bakes everything for Agent 0 and spawns everything Agent 0")]
	public class JumpTest : CovalencePlugin
	{
		int mask = 25;
		List<OffMeshLink> links = new List<OffMeshLink>();
		Dictionary<Vector3Int, OffMeshLink> dynamic_jumps = new Dictionary<Vector3Int, OffMeshLink>();
		private Game.Rust.Libraries.Player _rustPlayer = Interface.Oxide.GetLibrary<Game.Rust.Libraries.Player>("Player");
		private void SendChatMsg(BasePlayer pl, string msg) => _rustPlayer.Message(pl, msg,  "<color=#00ff00>[JumpTest]</color>", 0, Array.Empty<object>());
	
        void OnTerrainInitialized()
        {
			
			foreach(TriggerLadder ladder in UnityEngine.Object.FindObjectsOfType<TriggerLadder>()){
				
                UnityEngine.AI.NavMeshHit nmh = new UnityEngine.AI.NavMeshHit();
                NavMesh.SamplePosition(ladder.transform.position+new Vector3(0,ladder.transform.localScale.y/2,0), out nmh, 20, mask);
                Vector3 pt1 = nmh.position;
				NavMesh.SamplePosition(ladder.transform.position-new Vector3(0,ladder.transform.localScale.y/2,0), out nmh, 20, mask);
                Vector3 pt2 = nmh.position;
				GameObject offmesh = new GameObject();
				offmesh.transform.position = pt1;
				GameObject endpoint = new GameObject();
				endpoint.transform.position = pt2;
				OffMeshLink omLink = offmesh.AddComponent<OffMeshLink>();
				omLink.startTransform = offmesh.transform;
				omLink.endTransform = endpoint.transform;
				omLink.activated = true;
				omLink.biDirectional = true;
				omLink.area = 0;
				omLink.enabled = true;
				links.Add(omLink);
				
			}

        }
		public static Dictionary<BasePlayer,Vector3> JUMPING_PLAYERS = new Dictionary<BasePlayer,Vector3>();
		/*
		inputState.IsDown(BUTTON.JUMP)
		*/
		void OnPlayerInput(BasePlayer player, InputState input)
		{
			if(!input.IsDown(BUTTON.JUMP)&&player.modelState.onground&&JUMPING_PLAYERS.ContainsKey(player)){
				Puts("AddingJumpPoint");
				addJumpPoint(player);
			}
			if(input.WasJustPressed(BUTTON.JUMP)&&player.modelState.onground){
				Puts("OnPlayerInput works!");
				if(!JUMPING_PLAYERS.ContainsKey(player)){
					JUMPING_PLAYERS.Add(player,player.transform.position);
				}
			}
		}
		void addJumpPoint(BasePlayer entity){
			if(!JUMPING_PLAYERS.ContainsKey(entity)){
				return;
			}
			Vector3 start_pos = JUMPING_PLAYERS[entity];
			JUMPING_PLAYERS.Remove(entity);
			
			Vector3 end_pos = entity.transform.position;
			Vector3Int jump_key = new Vector3Int(
				(int)(start_pos.x/5), (int)(start_pos.y/2), (int)(start_pos.z/5)
				);
			if(dynamic_jumps.ContainsKey(jump_key)){
				return;
			}
			
			UnityEngine.AI.NavMeshHit nmh = new UnityEngine.AI.NavMeshHit();
			NavMesh.SamplePosition(start_pos, out nmh, 3, mask);
			if(!nmh.hit) return;
			Vector3 pt1 = nmh.position;
			NavMesh.SamplePosition(end_pos, out nmh, 3, mask);
			if(!nmh.hit) return;
			Vector3 pt2 = nmh.position;
			float distance = Mathf.Abs(pt1.y-pt2.y);
			if(distance<0.5f || distance > 10f){
				return;
			}
			
			GameObject offmesh = new GameObject();
			offmesh.transform.position = pt1;
			GameObject endpoint = new GameObject();
			endpoint.transform.position = pt2;
			OffMeshLink omLink = offmesh.AddComponent<OffMeshLink>();
			omLink.startTransform = offmesh.transform;
			omLink.endTransform = endpoint.transform;
			omLink.activated = true;
			omLink.biDirectional = false;
			omLink.area = 0;
			omLink.enabled = true;
			dynamic_jumps.Add(jump_key,omLink);
			SendChatMsg(entity,"omLink created!");
			
		}
		object OnEntityTakeDamage(BasePlayer entity, HitInfo info)
		{
			if(!info.damageTypes.Has(Rust.DamageType.Fall)){
				return null;
			}
			Puts("Fall Damage!");
			SendChatMsg(entity,"Fall Damage!");
			return null;
		}
		[Command("jumpdebug")] void navigation_scan_cmd(IPlayer player, string command, string[] args){	
			BasePlayer baseplayer = (BasePlayer)player.Object;
			if(baseplayer==null){
				return;
			}			
			
			foreach(OffMeshLink link in UnityEngine.Object.FindObjectsOfType<OffMeshLink>()){
				ConsoleNetwork.SendClientCommand(baseplayer.Connection,"ddraw.text", new object[]
				{
					10,
					link.occupied?UnityEngine.Color.red:UnityEngine.Color.yellow,
					link.startTransform.position,
					"∙"
				});		
				ConsoleNetwork.SendClientCommand(baseplayer.Connection,"ddraw.text", new object[]
				{
					10,
					link.occupied?UnityEngine.Color.red:UnityEngine.Color.cyan,
					link.endTransform.position,
					"∙"
				});
			}
		}
		
		void OnEntitySpawned(NPCPlayer bn){
			BaseNavigator bnav = bn.GetComponent<BaseNavigator>();
			if(bnav==null)return;
			bnav.Agent.autoTraverseOffMeshLink = false;
			bn.gameObject.AddComponent<AgentLinkMover>();
			
		}
		public enum OffMeshLinkMoveMethod
		{
			Teleport,
			NormalSpeed,
			Parabola
		}

		[RequireComponent(typeof(NavMeshAgent))]
		public class AgentLinkMover : MonoBehaviour
		{
			public OffMeshLinkMoveMethod method = OffMeshLinkMoveMethod.Parabola;
			IEnumerator Start()
			{
				NavMeshAgent agent = GetComponent<NavMeshAgent>();
				BasePlayer player = GetComponent<BasePlayer>();
				agent.autoTraverseOffMeshLink = false;
				float lift = agent.currentOffMeshLinkData.startPos.y-agent.currentOffMeshLinkData.endPos.y;
				float height = Math.Abs(lift);
				while (true)
				{
					if (agent.isOnOffMeshLink)
					{
						player.modelState.flying = true;
						player.SendModelState(true);
						if (lift<0)
							yield return StartCoroutine(NormalSpeed(agent));
						else if (method == OffMeshLinkMoveMethod.Parabola)
							yield return StartCoroutine(Parabola(agent, 1f, Mathf.Sqrt(height*9.8f/2)+1f));
						
						agent.CompleteOffMeshLink();
						player.modelState.flying = false;
						player.SendModelState(true);
					}
					yield return null;
				}
			}

			IEnumerator NormalSpeed(NavMeshAgent agent)
			{
				OffMeshLinkData data = agent.currentOffMeshLinkData;
				Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;
				while (agent.transform.position != endPos)
				{
					agent.transform.position = Vector3.MoveTowards(agent.transform.position, endPos, agent.speed * Time.deltaTime);
					yield return null;
				}
			}


			IEnumerator Parabola(NavMeshAgent agent, float height, float duration)
			{
				OffMeshLinkData data = agent.currentOffMeshLinkData;
				Vector3 startPos = agent.transform.position;
				Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;
				float normalizedTime = 0.0f;
				while (normalizedTime < 1.0f)
				{
					float yOffset = height * 4.0f * (normalizedTime - normalizedTime * normalizedTime);
					agent.transform.position = Vector3.Lerp(startPos, endPos, normalizedTime) + yOffset * Vector3.up;
					normalizedTime += Time.deltaTime / duration;
					yield return null;
				}
			}
		}
	}
	
}