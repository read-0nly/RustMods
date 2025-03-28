using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;



namespace Oxide.Plugins
{
    [Info("Public Radio", "obsol", "0.0.2")]
    [Description("Allows audio transmission over RF channels")]
    internal class PublicRadio : RustPlugin
    {
		void inflateBroadcaster(RFBroadcaster tx){
			if(tx.outputs.Length < 32){
				IOEntity.IOSlot[] slotBucket = tx.outputs;
				tx.outputs = new IOEntity.IOSlot[32];
				for(int i = 0; i < tx.outputs.Length;i++){
					if(i<slotBucket.Length){
						tx.outputs[i]=slotBucket[i];
					}else{
						tx.outputs[i]=new IOEntity.IOSlot();
						tx.outputs[i].Clear();
					}
				}
			}
			
		}
		bool disconnectReceiver(RFReceiver rx, RFBroadcaster tx = null, bool invert = false){
			bool flag = (tx==null||rx.inputs[0].connectedTo.ioEnt==tx) ^ invert;
			if(!rx.inputs[0].IsConnected()){
				return true;
			}
			if(rx.inputs[0].connectedTo.ioEnt is RFBroadcaster && flag){
				rx.Disconnect(0,true);
				return true;
			}
			return false;
		}
		/*
		Interface.CallHook("OnRfBroadcasterAdded", obj, frequency);
		Interface.CallHook("OnRfBroadcasterRemoved", obj, frequency);
		Interface.CallHook("OnRfListenerAdded", obj, frequency);
		Interface.CallHook("OnRfListenerRemoved", obj, frequency);
		*/
		void playSpeakers(IOEntity rx, int depth){
			ConnectedSpeaker speaker =  findSpeaker(rx, depth);
			//Puts("click?");
			if(speaker==null){return;}
			BaseEntity.Flags flag = speaker.flags;
			BaseEntity.Flags flagplus = flag | BaseEntity.Flags.Reserved8;
			BaseEntity.Flags flagminus = (BaseEntity.Flags) ((uint)flag & (0xffffffff ^ (uint)BaseEntity.Flags.Reserved8));
			speaker.OnFlagsChanged(flagminus,flagplus);
			//Puts("Turning on speaker");
			//Puts("click");
		}
		ConnectedSpeaker findSpeaker(IOEntity rx, int depth){
				
			if (depth <= 0)
			{
				return null;
			}
			global::IOEntity.IOSlot[] inputs = rx.outputs;
			for (int i = 0; i < inputs.Length; i++)
			{
				global::IOEntity ioentity = inputs[i].connectedTo.Get(rx.isServer);
				if (ioentity == rx)
				{
					//Puts("self");
					return null;
				}
				if (ioentity != null)
				{
					//Puts("found");
					global::ConnectedSpeaker audioConnectionSource = ioentity as global::ConnectedSpeaker;
					if (audioConnectionSource != null)
					{
						return audioConnectionSource;
					}
				}
				if (ioentity != null)
				{
					//Puts("godeeper");
					global::ConnectedSpeaker connectionSource = findSpeaker(ioentity, depth - 1);
					if (connectionSource != null)
					{
						return connectionSource;
					}
				}
			}
			return null;
		}
		void OnRfBroadcasterAdded(global::IRFObject obj,int frequency){
			if(!(obj is RFBroadcaster)){return;}
			RFBroadcaster tx = obj as RFBroadcaster;
			tx.ensureOutputsUpdated=true;
			inflateBroadcaster(tx);
			int j = 1;
			
			//Puts("OnRfBroadcasterAdded?");
			foreach(RFReceiver rx in RFManager.GetListenerSet(frequency).ToArray()){
				disconnectReceiver(rx);
				tx.ConnectTo(rx,j,0);
				tx.ensureOutputsUpdated =true;
				tx.UpdateOutputs();
				playSpeakers(rx,8);
				j++;
				if(j>=tx.outputs.Length){break;}
			}
		}
		void OnRfBroadcasterRemoved(global::IRFObject obj,int frequency){
			//Puts("OnRfBroadcasterRemoved?");
			if(!(obj is RFBroadcaster)){return;}
			RFBroadcaster tx = obj as RFBroadcaster;
			inflateBroadcaster(tx);
			foreach(RFReceiver rx in RFManager.GetBroadcasterSet(frequency).ToArray()){
				disconnectReceiver(rx,tx);
			}
		}
		void OnRfListenerAdded(global::IRFObject obj,int frequency){
			//Puts("OnRfListenerAdded?");
			if(!(obj is RFReceiver)){return;}
			RFReceiver rx = obj as RFReceiver;
			rx.ensureOutputsUpdated=true;
			foreach(RFBroadcaster tx in RFManager.GetBroadcasterSet(frequency).ToArray()){
				inflateBroadcaster(tx);
				int j = 0;
				while(j < tx.outputs.Length &&tx.outputs[j].IsConnected() ){
					j++;
				}
				Puts(j.ToString());
				if(j < tx.outputs.Length){
					if(disconnectReceiver(rx,tx, true)){
						tx.ConnectTo(rx,j,0);
						tx.ensureOutputsUpdated =true;
						tx.UpdateOutputs();
						rx.SetFlag(BaseEntity.Flags.On, true);
						NextFrame(()=>{rx.MarkDirty();});
						//Puts("Connected tx");
					}
					playSpeakers(rx,8);
					//Puts("Connected");
					break;
				}else{
					//Puts("Outofbounds");
					
				}
			}
		}
		void OnRfListenerRemoved(global::IRFObject obj,int frequency){
			//Puts("OnRfListenerRemoved?");
			if(!(obj is RFReceiver)){return;}
			RFReceiver rx = obj as RFReceiver;
			disconnectReceiver(rx);
		}
	}
}
