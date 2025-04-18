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
    [Info("COS Linker", "obsol", "0.0.2")]
    [Description("Admin app for CobaltOS")]
    internal class COSLinker : CovalencePlugin
    {
		string LinkerString(Telephone tn){
			string result = @"
			[
				{
					""name"": ""LinkerPanel"",
					""parent"": ""Overlay"",
					""components"": [
						{
							""type"": ""UnityEngine.UI.Image"",
							""color"": ""0.0255 0.026 0.03 1.0"",
							""imagetype"": ""Tiled""
						},
						{
							""type"": ""RectTransform"",
							""anchormin"": ""0.1 0.08"",
							""anchormax"": ""0.9 0.9""
						},
						{
							""type"": ""NeedsCursor""
						}
					]
				},
				{
					""parent"": ""LinkerPanel"",
					""components"": [
						{
							""type"": ""UnityEngine.UI.Text"",
							""color"": ""0.2 0.5 0.7 1"",
							""text"": ""Linker"",
							""align"": ""MiddleCenter"",
							""fontSize"": 40,
							""font"": ""DroidSansMono.ttf""
						},
						{
							""type"": ""RectTransform"",
							""anchormin"": ""0.1 0.85"",
							""anchormax"": ""0.9 0.95""
						}
					]
				},
				{
					""name"": ""CloseButton"",
					""parent"": ""LinkerPanel"",
					""components"": [
						{
							""type"": ""UnityEngine.UI.Button"",
							""close"": ""LinkerPanel"",
							""color"": ""0.05 0.08 0.13 1"",
							""imagetype"": ""Tiled""
						},
						{
							""type"": ""RectTransform"",
							""anchormin"": ""0.3 0.05"",
							""anchormax"": ""0.7 0.15""
						}
					]
				},
				{
					""parent"": ""CloseButton"",
					""components"": [
						{
							""type"": ""UnityEngine.UI.Text"",
							""color"": ""0.2 0.5 0.7 1"",
							""font"": ""DroidSansMono.ttf"",
							""fontsize"": 20,
							""text"": ""CLOSE"",
							""align"": ""MiddleCenter""
						}
					]
				},";
			int i = 0;
			Puts("Loading linker!");
			foreach(ProtoBuf.PhoneDirectory.DirectoryEntry entry in tn.Controller?.savedNumbers.entries){
				result+=ButtonString(entry.phoneName, entry.phoneNumber, i);//
				Puts("  "+entry.phoneName+" : "+entry.phoneNumber);
				i++;
				if(i>10){break;}
			}
			result+=@"
			]";	
			Puts("Loaded linker!");
			return result;
		}
		
		void Loaded(){
			COBALTOS.instance.AddCommandTape("Linker",RunCommand,1f);
		}		
		void RunCommand(BasePlayer bp, ComputerStation cs, Telephone tn){
			CommunityEntity.ServerInstance.ClientRPC<string>(RpcTarget.Player("AddUI", bp.Connection),LinkerString(tn));
			
		}
		string ButtonString(string name, int number, int row){
			return @"{
					""name"": ""Bookmark"+row+@""",
					""parent"": ""LinkerPanel"",
					""components"": [
						{
							""type"": ""UnityEngine.UI.Button"",
							""command"": ""RedirectConnection "+number+@" LinkerPanel"",
							""color"": ""0.05 0.08 0.13 1"",
							""imagetype"": ""Tiled""
						},
						{
							""type"": ""RectTransform"",
							""anchormin"": ""0.1 "+(0.75f-(0.06f*row))+@""",
							""anchormax"": ""0.9 "+(0.8f-(0.06f*row))+@"""
						}
					]
				},
				{
					""parent"": ""Bookmark"+row+@""",
					""components"": [
						{
							""type"": ""UnityEngine.UI.Text"",
							""color"": ""0.2 0.5 0.7 1"",
							""font"": ""DroidSansMono.ttf"",
							""fontsize"": 20,
							""text"": """+name+@""",
							""align"": ""MiddleCenter""
						}
					]
				},";
		}
	}
	
}