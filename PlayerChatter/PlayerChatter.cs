
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
	using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CircularBuffer;
using CompanionServer;
using Facepunch;
using Facepunch.Math;
using Network;
using Oxide.Core;
using UnityEngine;
#endregion
namespace Oxide.Plugins{
	[Info("PlayerChatter", "obsol", "0.2.1")]
	[Description("Limits chat distance and adds a radio chatter soundeffect when player sends a chat message")]
	public class PlayerChatter : RustPlugin{
		object OnPlayerChat(BasePlayer player, string message, ConVar.Chat.ChatChannel channel)	{
			Puts("OnPlayerChat works!");
			ulong userId = player.userID;
			Effect.server.Run("assets/prefabs/npc/scientist/sound/chatter.prefab", player.transform.position);
			string username = player.displayName;
			ConVar.Chat.ChatChannel targetChannel = channel;
			
			if (ConVar.Chat.serverlog)
			{
				global::ServerConsole.PrintColoured(new object[]
				{
					ConsoleColor.DarkYellow,
					string.Concat(new object[]
					{
						"[",
						targetChannel,
						"] ",
						username,
						": "
					}),
					ConsoleColor.DarkGreen,
					message
				});
				string str = ((player != null) ? player.ToString() : null) ?? string.Format("{0}[{1}]", username, userId);
				if (targetChannel == ConVar.Chat.ChatChannel.Team)
				{
					DebugEx.Log("[TEAM CHAT] " + str + " : " + message, StackTraceLogType.None);
				}
				else if (targetChannel == ConVar.Chat.ChatChannel.Cards)
				{
					DebugEx.Log("[CARDS CHAT] " + str + " : " + message, StackTraceLogType.None);
				}
				else
				{
					DebugEx.Log("[CHAT] " + str + " : " + message, StackTraceLogType.None);
				}
			}
			string text2 = "#5af";
			string text3 = username.EscapeRichText();
			ConVar.Chat.Record(new ConVar.Chat.ChatEntry
			{
				Channel = targetChannel,
				Message = message,
				UserId = ((player != null) ? player.UserIDString : userId.ToString()),
				Username = username,
				Color = text2,
				Time = Epoch.Current
			});
			
			
			List<Network.Connection> connections = new List<Network.Connection>();
			List<Network.Connection> farConnections = new List<Network.Connection>();
			foreach(BasePlayer bp in BasePlayer.activePlayerList){
				if(Vector3.Distance(bp.transform.position, player.transform.position)<300){
					connections.Add(bp.net.connection);
				}else if (Vector3.Distance(bp.transform.position, player.transform.position)<500){
					farConnections.Add(bp.net.connection);					
				}
			}
			char[] farMessageChars = message.ToCharArray();
			char[] farUserChars = text3.ToCharArray();
			int rotate = UnityEngine.Random.Range(0,4);
			for(int i = rotate;  i-rotate < farMessageChars.Length;i++){
				if(i%3==0||i%5==0||i%7==0){
					farMessageChars[i-rotate]='*';
				}
			}
			rotate = UnityEngine.Random.Range(0,4);
			for(int i = rotate;  i-rotate < farUserChars.Length;i++){
				if(i%3==0||i%5==0||i%7==0){
					farUserChars[i-rotate]='*';
				}
			}
			string farMessageString = new string(farMessageChars);
			string farUser = new string(farUserChars);
			switch (targetChannel)
			{
				case ConVar.Chat.ChatChannel.Global:
					global::ConsoleNetwork.SendClientCommand(connections, "chat.add2", new object[]
					{
						1,
						userId,
						message,
						text3,
						text2,
						1f
					});
					global::ConsoleNetwork.SendClientCommand(farConnections, "chat.add2", new object[]
					{
						0,
						userId,
						farMessageString,
						farUser,
						text2,
						1f
					});
					return true;
				case ConVar.Chat.ChatChannel.Team:
				{
					text2 = "#5fa";
					string farText2 = "#fa5";
					global::RelationshipManager.PlayerTeam playerTeam = global::RelationshipManager.ServerInstance.FindPlayersTeam(userId);
					if (playerTeam == null)
					{
						return false;
					}
					
					List<Network.Connection> onlineMemberConnections = playerTeam.GetOnlineMemberConnections();
					if (onlineMemberConnections != null)
					{
					
						foreach(Network.Connection conn in connections.ToArray()){
							if(!onlineMemberConnections.Contains(conn)){
								connections.Remove(conn);
								farConnections.Add(conn);
							}
						}
						global::ConsoleNetwork.SendClientCommand(connections, "chat.add2", new object[]
						{
							1,
							userId,
							message,
							text3,
							text2,
							1f
						});
						global::ConsoleNetwork.SendClientCommand(farConnections, "chat.add2", new object[]
						{
							0,
							userId,
							farMessageString,
							farUser,
							farText2,
							1f
						});
					}
					return true;
				}
			}
			return null;
			
		}
	}
}