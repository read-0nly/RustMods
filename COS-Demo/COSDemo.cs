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
    [Info("COS Demo", "obsol", "0.0.2")]
    [Description("Demo app for CobaltOS")]
    internal class COSDemo : CovalencePlugin
    {
		////
		string mainPanel(){
			return @"
				[	
					{
						""name"": ""Demo"",
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
						""name"": ""CloseButton"",
						""parent"": ""Demo"",
						""components"":
						[
							{
								""type"":""UnityEngine.UI.Button"",
								""close"":""Demo"",
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
				]";
		}
		void Loaded(){
			COBALTOS.instance.AddPanelTape("Demo",mainPanel(),1f);
		}
		[Command("CloseDemo")]
        private void CloseChat(IPlayer player, string cmd, string[] args)
        {
            BasePlayer basePlayer = player.Object as BasePlayer;
			if(basePlayer==null){
				return;
			}
			CommunityEntity.ServerInstance.ClientRPC<string>(RpcTarget.Player("DestroyUI", basePlayer.Connection),"Demo");
		}
		
		
	}
	
}