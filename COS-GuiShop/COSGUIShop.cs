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
    [Info("COS GUI Shop", "obsol", "0.0.2")]
    [Description("GUI shop implementation for CobaltOS")]
    internal class COSGUIShop : CovalencePlugin
    {
		///
		[PluginReference]
		private Plugin GUIShop;
		
		public void ShowGUIShop(BasePlayer bp, ComputerStation cs, Telephone tn){
			if(GUIShop!=null){
				GUIShop.Call("ShowGUIShops", bp, "Component");
			}
		}
		
		void Loaded(){
			COBALTOS.instance.AddCommandTape("Shop",ShowGUIShop,1f);
		}
	}
	
}