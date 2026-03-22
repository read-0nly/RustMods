using UnityEngine;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Oxide.Core.Libraries.Covalence;
using Oxide.Plugins;
using Oxide.Core.Plugins;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("TrueMusic", "obsol", "0.0.1")]
    [Description("Puts custom oggs stored on the server as audio on tapes in-game")]
    class TrueMusic : CovalencePlugin
    {//
		Dictionary<string,List<Cassette>> cassettes = new Dictionary<string,List<Cassette>>();
        void OnServerInitialized(){
			List<string> dirs = new List<string>(Directory.EnumerateDirectories(".\\oxide\\media"));
			foreach(string d in dirs){
				Puts("Folder : "+d);
				List<string> files = new List<string>(System.IO.Directory.EnumerateFiles(d, "*.ogg", SearchOption.TopDirectoryOnly));
				cassettes.Add(d,new List<Cassette>());
				foreach(string f in files){
					Puts("  File : "+f);
					Cassette cst = GameManager.server.CreateEntity("assets/prefabs/voiceaudio/cassette/cassette.entity.prefab", Vector3.zero, Quaternion.identity /* + Quaternion.Euler(0, 90, 0)*/, false) as Cassette;
					CustomCassette ccst = cst.gameObject.AddComponent<CustomCassette>();
					ccst.CopyProps(cst);
					ccst.Spawn();
					
					Rigidbody rb = ccst.GetComponent<Rigidbody>();
					if(rb!=null){
						rb.isKinematic=true;
					}
					
					byte[] audioBytes = File.ReadAllBytes(f);
					ccst.MakeNewFile(audioBytes);
					cassettes[d].Add(ccst);
					
				}
			}
		}
		[Command("TMGiveTapes")] //
		private void CAZloadcfg(IPlayer iplayer, string command, string[] args){
			BasePlayer bp = iplayer.Object as BasePlayer;
			foreach(string s in cassettes.Keys){
				foreach(Cassette c in cassettes[s]){
					Item cassetteItem = ItemManager.CreateByName("cassette", 1, 0UL);
					
					cassetteItem.text = "Music?";
					cassetteItem.Drop(bp.transform.position,bp.transform.forward);
					Cassette c2 = null;
					ItemModAssociatedEntity<Cassette>.GetAssociatedEntity(cassetteItem, out c2,true);
					Puts("Cassette spawned"+(c2!=null));
					c2.SetAudioId(c.AudioId,0);
					
				}
			}
		}
		public class CustomCassette : Cassette{
			public void MakeNewFile(byte[] sound)
			{
				global::FileStorage.server.RemoveAllByEntity(this.net.ID);
				uint num2 = global::FileStorage.server.Store(sound, global::FileStorage.Type.ogg, this.net.ID, 0U);
				this.SetAudioId(num2, 0);
			}
		
			#region Upcast by reflection
			public void CopyProps(Cassette source)
			{
				Type type = source.GetType();
				while (type != null && type.ToString()!="BaseMonoBehaviour")
				{
					UnityEngine.Debug.LogWarning(type.ToString());
					CopyPropsType(type, source);
					type = type.BaseType;
				}
			}

			private void CopyPropsType(Type type, Cassette source)
			{
				FieldInfo[] myObjectFields = type.GetFields(
					BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
				
				foreach (FieldInfo fi in myObjectFields)
				{
					fi.SetValue(this, fi.GetValue(source));
				}
			}
			#endregion
		}
		
    }
}
