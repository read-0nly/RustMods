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
    [Info("Power Transmission", "obsol", "0.0.2")]
    [Description("Allows power transmission over RF channels")]
    internal class PowerTransmission : RustPlugin
    {
		void inflateBroadcaster(RFBroadcaster tx){
			//If broadcaster has less than 32 outputs, set it to 32 outputs
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
			//If tx is null, disconnect. If tx isn't null and it's the current connection, disconnect. If invert is true, if tx isn't the current connection disconnect
			bool flag = tx==null||(rx.inputs[0].connectedTo.ioEnt==tx ^ invert);
			
			//If no connection, it's already disconnected
			if(!rx.inputs[0].IsConnected()){return true;}
			
			//Disconnect based on flag
			if(flag){
				Puts("Disconnecting");
				IOEntity ioent = rx.inputs[0].connectedTo.ioEnt;
				rx.Disconnect(0,true);
				rx.SetFlag(global::BaseEntity.Flags.On,false);
				rx.ensureOutputsUpdated =true;
				rx.UpdateOutputs();
				rx.MarkDirty();
				rx.SendNetworkUpdateImmediate(false);
				ioent.ensureOutputsUpdated =true;
				ioent.UpdateOutputs();//
				ioent.MarkDirty();
				return true;
			}
			//Not disconnected.
			return false;
		}
		bool connectReceiver(RFReceiver rx, RFBroadcaster tx ,int j){
			disconnectReceiver(rx);
			
			//connect to listener
			tx.ConnectTo(rx,j,0);
			rx.SetFlag(global::BaseEntity.Flags.On,true);
			//update IO
			tx.UpdateOutputs();
			tx.MarkDirty();
			//Force on
			rx.SetFlag(global::BaseEntity.Flags.On,true);
			//Force refresh
			rx.MarkDirty();
			//Not disconnected.
			return false;
		}
		void OnRfBroadcasterAdded(global::IRFObject obj,int frequency){
			if(!(obj is RFBroadcaster)){return;}
			
			//Make sure broadcaster has 32 outputs
			RFBroadcaster tx = obj as RFBroadcaster;
			tx.ensureOutputsUpdated=true;
			inflateBroadcaster(tx);
			//Skip 0th output - not really necessary
			int j = 1;
			
			foreach(RFReceiver rx in RFManager.GetListenerSet(frequency).ToArray()){
				//if we reached the limit stop
				if(j>=tx.outputs.Length){break;}
				//disconnect all but us
				connectReceiver(rx,tx,j);
				j++;
			}
		}
		void OnRfBroadcasterRemoved(global::IRFObject obj,int frequency){
			if(!(obj is RFBroadcaster)){return;}
			//Make sure broadcaster has 32 outputs
			RFBroadcaster tx = obj as RFBroadcaster;
			inflateBroadcaster(tx);
			//Remove self from all receivers on frequency
			for(int j =0;j< tx.outputs.Length;j++){
				tx.Disconnect(j,false);
			}
		}
		void OnRfListenerAdded(global::IRFObject obj,int frequency){
			if(!(obj is RFReceiver)){return;}
			RFReceiver rx = obj as RFReceiver;
			//Find valid broadcastr
			foreach(RFBroadcaster tx in RFManager.GetBroadcasterSet(frequency).ToArray()){
				//Make sure broadcaster has 32 outputs
				inflateBroadcaster(tx);
				int j = 0;
				//find empty slot
				while(j < tx.outputs.Length &&tx.outputs[j].IsConnected() ){
					j++;
				}
				if(j < tx.outputs.Length){
					connectReceiver(rx,tx,j);
					//broadcaster valid, stop search
					break;
				}
			}
		}
		void OnRfListenerRemoved(global::IRFObject obj,int frequency){
			if(!(obj is RFReceiver)){return;}
			RFReceiver rx = obj as RFReceiver;
			//Disconnect listener
			disconnectReceiver(rx);
		}
	}
}