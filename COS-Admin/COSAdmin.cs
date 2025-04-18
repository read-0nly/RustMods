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
    [Info("COS Admin", "obsol", "0.0.2")]
    [Description("Admin app for CobaltOS")]
    internal class COSAdmin : CovalencePlugin
    {//
		string AdminOSString(){ return @"				
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
			]";
					}
					
		void Loaded(){
			COBALTOS.instance.AddPanelTape("Admin",AdminOSString(),1f);
		}		
		
	}
	
}