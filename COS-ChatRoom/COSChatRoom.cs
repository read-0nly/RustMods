//Requires: COBALTOS
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
    [Info("COS ChatRoom", "obsol", "0.0.2")]
    [Description("Chatroom app for CobaltOS")]
    internal class COSChatRoom : CovalencePlugin
    {
		///
		string mainPanel(){
			Puts("send panel");
			return @"
[	
	{
		""name"": ""BBSChat"",
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
		""parent"": ""BBSChat"",
		""name"":""username-panel"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Image"",
				""imagetype"": ""Tiled"",
				""color"": ""0.255 0.26 0.3 0.3"",
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.01 0.11"",
				""anchormax"": ""0.19 0.16""
			},
			{
				""type"":""NeedsCursor""
			}
		]
	},
	{
		""parent"": ""username-panel"",
		""name"": ""username-input"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.InputField"",
				""color"": ""0.9 0.97 1 1"",
				""align"": ""MiddleLeft"",
				""text"":"""",
				""fontSize"":""18"",
				""font"":""DroidSansMono.ttf"",
				""characterLimit"": ""12"",
				""needsKeyboard"": true,
				""autofocus"": true,
				""command"":""BBSChatUsername""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.01 0.1"",
				""anchormax"": ""0.99 0.9""
			},
			{
				""type"":""NeedsCursor""
			}
		]
	},
	{
		""parent"": ""BBSChat"",
		""name"":""chat-panel"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Image"",
				""imagetype"": ""Tiled"",
				""color"": ""0.255 0.26 0.3 0.3"",
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.2 0.11"",
				""anchormax"": ""0.96 0.16""
			},
			{
				""type"":""NeedsCursor""
			}
		]
	},
	{
		""parent"": ""chat-panel"",
		""name"": ""chat-input"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.InputField"",
				""color"": ""0.9 0.97 1 1"",
				""align"": ""MiddleLeft"",
				""text"":"""",
				""fontSize"":""18"",
				""font"":""DroidSansMono.ttf"",
				""characterLimit"": ""65"",
				""needsKeyboard"": true,
				""autofocus"": true,
				""command"":""BBSChatPrep""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.01 0.1"",
				""anchormax"": ""0.99 0.9""
			},
			{
				""type"":""NeedsCursor""
			}
		]
	},
	{
		""name"": ""SendButton"",
		""parent"": ""chat-panel"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Button"",
				""command"":""BBSChatSend"",
				""color"": ""0.05 0.08 0.13 1"",
				""imagetype"": ""Tiled""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.9 0.1"",
				""anchormax"": ""1 0.9""
			}
		]
	},
	{
		""parent"": ""SendButton"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""0.2 0.5 0.7 1"",
				""font"":""DroidSansMono.ttf"",
				""fontsize"":20,
				""text"":""Send"",
				""align"": ""MiddleCenter""
			}
		]
	},
	{
		""name"": ""CloseButton"",
		""parent"": ""BBSChat"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Button"",
				""command"":""CloseChat"",
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
	},
	
	
	{
		""name"": ""chat-output-1"",
		""parent"": ""BBSChat"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""1 1 1 1"",
				""font"":""DroidSansMono.ttf"",
				""fontsize"":20,
				""text"":"""",
				""align"": ""MiddleLeft""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.2 0.91"",
				""anchormax"": ""0.96 0.96""
			}
		]
	},
	{
		""name"": ""chat-output-2"",
		""parent"": ""BBSChat"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""1 1 1 1"",
				""font"":""DroidSansMono.ttf"",
				""fontsize"":20,
				""text"":"""",
				""align"": ""MiddleLeft""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.2 0.855"",
				""anchormax"": ""0.96 0.905""
			}
		]
	},
	{
		""name"": ""chat-output-3"",
		""parent"": ""BBSChat"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""1 1 1 1"",
				""font"":""DroidSansMono.ttf"",
				""fontsize"":20,
				""text"":"""",
				""align"": ""MiddleLeft""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.2 0.80"",
				""anchormax"": ""0.96 0.85""
			}
		]
	},
	{
		""name"": ""chat-output-4"",
		""parent"": ""BBSChat"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""1 1 1 1"",
				""font"":""DroidSansMono.ttf"",
				""fontsize"":20,
				""text"":"""",
				""align"": ""MiddleLeft""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.2 0.745"",
				""anchormax"": ""0.96 0.795""
			}
		]
	},
	{
		""name"": ""chat-output-5"",
		""parent"": ""BBSChat"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""1 1 1 1"",
				""font"":""DroidSansMono.ttf"",
				""fontsize"":20,
				""text"":"""",
				""align"": ""MiddleLeft""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.2 0.69"",
				""anchormax"": ""0.96 0.74""
			}
		]
	},
	{
		""name"": ""chat-output-6"",
		""parent"": ""BBSChat"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""1 1 1 1"",
				""font"":""DroidSansMono.ttf"",
				""fontsize"":20,
				""text"":"""",
				""align"": ""MiddleLeft""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.2 0.635"",
				""anchormax"": ""0.96 0.685""
			}
		]
	},
	{
		""name"": ""chat-output-7"",
		""parent"": ""BBSChat"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""1 1 1 1"",
				""font"":""DroidSansMono.ttf"",
				""fontsize"":20,
				""text"":"""",
				""align"": ""MiddleLeft""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.2 0.58"",
				""anchormax"": ""0.96 0.63""
			}
		]
	},
	{
		""name"": ""chat-output-8"",
		""parent"": ""BBSChat"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""1 1 1 1"",
				""font"":""DroidSansMono.ttf"",
				""fontsize"":20,
				""text"":"""",
				""align"": ""MiddleLeft""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.2 0.525"",
				""anchormax"": ""0.96 0.575""
			}
		]
	},
	{
		""name"": ""chat-output-9"",
		""parent"": ""BBSChat"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""1 1 1 1"",
				""font"":""DroidSansMono.ttf"",
				""fontsize"":20,
				""text"":"""",
				""align"": ""MiddleLeft""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.2 0.47"",
				""anchormax"": ""0.96 0.52""
			}
		]
	},
	{
		""name"": ""chat-output-10"",
		""parent"": ""BBSChat"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""1 1 1 1"",
				""font"":""DroidSansMono.ttf"",
				""fontsize"":20,
				""text"":"""",
				""align"": ""MiddleLeft""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.2 0.415"",
				""anchormax"": ""0.96 0.465""
			}
		]
	},
	{
		""name"": ""chat-output-11"",
		""parent"": ""BBSChat"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""1 1 1 1"",
				""font"":""DroidSansMono.ttf"",
				""fontsize"":20,
				""text"":"""",
				""align"": ""MiddleLeft""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.2 0.36"",
				""anchormax"": ""0.96 0.41""
			}
		]
	},
	{
		""name"": ""chat-output-12"",
		""parent"": ""BBSChat"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""1 1 1 1"",
				""font"":""DroidSansMono.ttf"",
				""fontsize"":20,
				""text"":"""",
				""align"": ""MiddleLeft""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.2 0.305"",
				""anchormax"": ""0.96 0.355""
			}
		]
	},
	{
		""name"": ""chat-output-13"",
		""parent"": ""BBSChat"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""1 1 1 1"",
				""font"":""DroidSansMono.ttf"",
				""fontsize"":20,
				""text"":"""",
				""align"": ""MiddleLeft""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.2 0.25"",
				""anchormax"": ""0.96 0.3""
			}
		]
	},
	{
		""name"": ""chat-output-14"",
		""parent"": ""BBSChat"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""1 1 1 1"",
				""font"":""DroidSansMono.ttf"",
				""fontsize"":20,
				""text"":"""",
				""align"": ""MiddleLeft""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.2 0.195"",
				""anchormax"": ""0.96 0.245""
			}
		]
	},
	
	
	{
		""name"": ""user-output-1"",
		""parent"": ""BBSChat"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""0.7 0.9 1 1"",
				""font"":""DroidSansMono.ttf"",
				""fontsize"":20,
				""text"":"""",
				""align"": ""MiddleRight""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.01 0.91"",
				""anchormax"": ""0.19 0.96""
			}
		]
	},
	{
		""name"": ""user-output-2"",
		""parent"": ""BBSChat"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""0.7 0.9 1 1"",
				""font"":""DroidSansMono.ttf"",
				""fontsize"":20,
				""text"":"""",
				""align"": ""MiddleRight""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.01 0.855"",
				""anchormax"": ""0.19 0.905""
			}
		]
	},
	{
		""name"": ""user-output-3"",
		""parent"": ""BBSChat"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""0.7 0.9 1 1"",
				""font"":""DroidSansMono.ttf"",
				""fontsize"":20,
				""text"":"""",
				""align"": ""MiddleRight""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.01 0.80"",
				""anchormax"": ""0.19 0.85""
			}
		]
	},
	{
		""name"": ""user-output-4"",
		""parent"": ""BBSChat"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""0.7 0.9 1 1"",
				""font"":""DroidSansMono.ttf"",
				""fontsize"":20,
				""text"":"""",
				""align"": ""MiddleRight""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.01 0.745"",
				""anchormax"": ""0.19 0.795""
			}
		]
	},
	{
		""name"": ""user-output-5"",
		""parent"": ""BBSChat"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""0.7 0.9 1 1"",
				""font"":""DroidSansMono.ttf"",
				""fontsize"":20,
				""text"":"""",
				""align"": ""MiddleRight""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.01 0.69"",
				""anchormax"": ""0.19 0.74""
			}
		]
	},
	{
		""name"": ""user-output-6"",
		""parent"": ""BBSChat"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""0.7 0.9 1 1"",
				""font"":""DroidSansMono.ttf"",
				""fontsize"":20,
				""text"":"""",
				""align"": ""MiddleRight""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.01 0.635"",
				""anchormax"": ""0.19 0.685""
			}
		]
	},
	{
		""name"": ""user-output-7"",
		""parent"": ""BBSChat"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""0.7 0.9 1 1"",
				""font"":""DroidSansMono.ttf"",
				""fontsize"":20,
				""text"":"""",
				""align"": ""MiddleRight""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.01 0.58"",
				""anchormax"": ""0.19 0.63""
			}
		]
	},
	{
		""name"": ""user-output-8"",
		""parent"": ""BBSChat"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""0.7 0.9 1 1"",
				""font"":""DroidSansMono.ttf"",
				""fontsize"":20,
				""text"":"""",
				""align"": ""MiddleRight""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.01 0.525"",
				""anchormax"": ""0.19 0.575""
			}
		]
	},
	{
		""name"": ""user-output-9"",
		""parent"": ""BBSChat"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""0.7 0.9 1 1"",
				""font"":""DroidSansMono.ttf"",
				""fontsize"":20,
				""text"":"""",
				""align"": ""MiddleRight""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.01 0.47"",
				""anchormax"": ""0.19 0.52""
			}
		]
	},
	{
		""name"": ""user-output-10"",
		""parent"": ""BBSChat"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""0.7 0.9 1 1"",
				""font"":""DroidSansMono.ttf"",
				""fontsize"":20,
				""text"":"""",
				""align"": ""MiddleRight""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.01 0.415"",
				""anchormax"": ""0.19 0.465""
			}
		]
	},
	{
		""name"": ""user-output-11"",
		""parent"": ""BBSChat"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""0.7 0.9 1 1"",
				""font"":""DroidSansMono.ttf"",
				""fontsize"":20,
				""text"":"""",
				""align"": ""MiddleRight""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.01 0.36"",
				""anchormax"": ""0.19 0.41""
			}
		]
	},
	{
		""name"": ""user-output-12"",
		""parent"": ""BBSChat"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""0.7 0.9 1 1"",
				""font"":""DroidSansMono.ttf"",
				""fontsize"":20,
				""text"":"""",
				""align"": ""MiddleRight""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.01 0.305"",
				""anchormax"": ""0.19 0.355""
			}
		]
	},
	{
		""name"": ""user-output-13"",
		""parent"": ""BBSChat"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""0.7 0.9 1 1"",
				""font"":""DroidSansMono.ttf"",
				""fontsize"":20,
				""text"":"""",
				""align"": ""MiddleRight""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.01 0.25"",
				""anchormax"": ""0.19 0.3""
			}
		]
	},
	{
		""name"": ""user-output-14"",
		""parent"": ""BBSChat"",
		""components"":
		[
			{
				""type"":""UnityEngine.UI.Text"",
				""color"": ""0.7 0.9 1 1"",
				""font"":""DroidSansMono.ttf"",
				""fontsize"":20,
				""text"":"""",
				""align"": ""MiddleRight""
			},
			{
				""type"":""RectTransform"",
				""anchormin"": ""0.01 0.195"",
				""anchormax"": ""0.19 0.245""
			}
		]
	},
]";
		}
		
		public struct chatMessage{
			public ulong source = 0;
			public string message = "";
			public chatMessage(ulong id, string msg){
				source = id;
				message=msg;
			}
		}
		
		void Loaded(){
			nicknames.Add(0,"SERVER");
			COBALTOS.instance.AddCommandTape("BBS Server",showChat,1f);
		
		}
		
		void SendMessage(BasePlayer basePlayer,string message){
			ulong id = basePlayer.Connection.userid;
			if(!nicknames.ContainsKey(id)){
				nicknames.Add(id, basePlayer.Connection.username);
			}
			if(nicknames.ContainsKey(id) && nicknamecache.ContainsKey(id)){
				messages.Add(new chatMessage(0, nicknames[id]+" changed username to: "+nicknamecache[id]));
				nicknames[id]=nicknamecache[id];
				nicknamecache.Remove(id);
			}
			
			messages.Add(new chatMessage(id, message));
			if(messages.Count()>14){
				messages.RemoveAt(0);
			}
			//Color.FromArgb(numericValue)
			updateMessages();
		}
		
		void updateMessages(){
			string updatedMessages = CUIMessage();
			foreach(BasePlayer basePlayer in chatMembers){
				CommunityEntity.ServerInstance.ClientRPC<string>(RpcTarget.Player("AddUI", basePlayer.Connection),updateMessageCUIString(updatedMessages,basePlayer));//
			}
		}
		
		string updateMessageCUIString(string s, BasePlayer basePlayer){
			ulong id = basePlayer.Connection.userid;
			return @"
			
			[
			"+s+@"{
					""name"": ""username-input"",
					""parent"": ""username-panel"",
					""update"": true,
					""components"":
					[
						{
							""type"":""UnityEngine.UI.InputField"",
							""color"": ""1 1 1 1"",
							""text"":"""+nicknames[id]+@""",
						},
						
					]
				},{
					""name"": ""chat-input"",
					""parent"": ""chat-panel"",
					""update"": true,
					""components"":
					[
						{
							""type"":""UnityEngine.UI.InputField"",
							""color"": ""1 1 1 1"",
							""text"":"""",
						},
						
					]
				},
			]
			
			";
		}////
		string CUIMessage( string ongoingString="",int idx = 14,int listIdx = 14){
			if(messages.Count()==0){
				return "";
			}
			if(listIdx>=messages.Count()){
				listIdx=messages.Count()-1;
			}
			ongoingString+=
			@"
			{
					""name"": ""chat-output-"+idx+@""",
					""parent"": ""BBSChat"",
					""update"": true,
					""components"":
					[
						{
							""type"":""UnityEngine.UI.Text"",
							""color"": ""1 1 1 1"",
							""text"":"""+messages[listIdx].message+@""",
						},
						
					]
				},
				
			{
					""name"": ""user-output-"+idx+@""",
					""parent"": ""BBSChat"",
					""update"": true,
					""components"":
					[
						{
							""type"":""UnityEngine.UI.Text"",
							""color"": ""0.95 0.98 1 1"",
							""text"":"""+nicknames[messages[listIdx].source]+@":"",
						},
						
					]
				},
				
			";
			if(listIdx ==0){
				return ongoingString;
			}else{
				ongoingString+=
				@"
				{
						""name"": ""chat-output-"+idx+@""",
						""parent"": ""BBSChat"",
						""update"": true,
						""components"":
						[
							{
								""type"":""UnityEngine.UI.Text"",
								""color"": ""1 1 1 1"",
								""text"":"""+messages[listIdx].message+@""",
							},
							
						]
					},
					";
				return CUIMessage(ongoingString,idx-1,listIdx-1);
			}
			
		}
		public Dictionary<ulong,string> nicknames = new Dictionary<ulong,string>();
		public Dictionary<ulong,string> nicknamecache = new Dictionary<ulong,string>();
		public Dictionary<ulong,string> messagecache = new Dictionary<ulong,string>();
		public List<chatMessage> messages = new List<chatMessage>();
		public HashSet<BasePlayer> chatMembers = new HashSet<BasePlayer>();
		
		
		public void showChat(BasePlayer basePlayer, ComputerStation cs=null){
			
			ulong id = basePlayer.Connection.userid;
			if(!nicknames.ContainsKey(id)){
				nicknames.Add(id, basePlayer.Connection.username);
			}
			CommunityEntity.ServerInstance.ClientRPC<string>(RpcTarget.Player("AddUI", basePlayer.Connection),mainPanel());
			chatMembers.Add(basePlayer);
			updateMessages();
		}
		
		[Command("BBSChatUsername")]
        private void BBSChatUsername(IPlayer player, string cmd, string[] args)
        {
            BasePlayer basePlayer = player.Object as BasePlayer;
			if(basePlayer==null){
				return;
			}
			if(args.Length==0){return;}
			foreach(string s in args){
				Puts(s);
			}
			Puts("username loaded : "+basePlayer.Connection.userid.ToString()+" : "+args[0]);
			ulong id = basePlayer.Connection.userid;
			if(nicknamecache.ContainsKey(id)){
				nicknamecache[id]=args[0];
			}else{
				nicknamecache.Add(id,args[0]);
			}
		}
		[Command("BBSChatPrep")]
        private void BBSChatPrep(IPlayer player, string cmd, string[] args)
        {
            BasePlayer basePlayer = player.Object as BasePlayer;
			if(basePlayer==null){
				return;
			}
			if(args.Length==0){return;}
			string wholeMessage = "";
			foreach(string s in args){
				Puts(s);
				wholeMessage+=s+" ";
			}
			Puts("chat loaded messagecache : "+basePlayer.Connection.userid.ToString()+" : "+wholeMessage);
			ulong id = basePlayer.Connection.userid;
			if(messagecache.ContainsKey(id)){
				messagecache[id]=wholeMessage;
			}else{
				messagecache.Add(id,wholeMessage);
			}
		}
		[Command("BBSChatSend")]
        private void BBSChatSend(IPlayer player, string cmd, string[] args)
        {
            BasePlayer basePlayer = player.Object as BasePlayer;
			if(basePlayer==null){
				return;
			}
			ulong id = basePlayer.Connection.userid;
			if(!messagecache.ContainsKey(id)||messagecache[id].Replace(" ","")==""){return;}
			SendMessage(basePlayer,messagecache[id]);
			Puts("Sending message");
		}
		
		[Command("ShowChat")]
        private void ShowChat(IPlayer player, string cmd, string[] args)
        {
			
            BasePlayer basePlayer = player.Object as BasePlayer;
			if(basePlayer==null){
				return;
			}
			showChat(basePlayer);
		}
		[Command("CloseChat")]
        private void CloseChat(IPlayer player, string cmd, string[] args)
        {
			
            BasePlayer basePlayer = player.Object as BasePlayer;
			if(basePlayer==null){
				return;
			}
			CommunityEntity.ServerInstance.ClientRPC<string>(RpcTarget.Player("DestroyUI", basePlayer.Connection),"BBSChat");
			chatMembers.Remove(basePlayer);
		}
		
		
	}
	
}