
#region using
	using Oxide.Core.Libraries.Covalence;
	using Oxide.Core;
	using UnityEngine; 
#endregion
namespace Oxide.Plugins{
	[Info("PinBradley", "obsol", "0.0.1")]
	[Description("Pins the bradley the player is looking at")]
	public class PinBradley: CovalencePlugin{//
		
		static BradleySpawner[] spawners;
		
        [Command("Pin.Bradley")]
        void PinBradley_cmd(IPlayer player, string command, string[] args){
			
			//Get calling player and confirm admin
			BasePlayer basePlayer = player.Object as BasePlayer;
			if(!basePlayer.IsAdmin){
				Puts(basePlayer.displayName + " attempted to pause bradley without admin ");
				basePlayer.ChatMessage("You must be an admin to use this");
				basePlayer.ChatMessage("If you're server owner, the command is \"ownerid <steamid>\"");
				basePlayer.ChatMessage("For example \"ownerid 6543211682531321\"");
				
				return;
			}
			
			//Get entity player is targeting
			BaseEntity lookingAt = getLookingAtEntity(basePlayer);
			if(lookingAt==null) return;
			//Get the Bradley as a Bradley if it's a Bradley
			BradleyAPC targetTank = lookingAt as BradleyAPC;
			if(targetTank==null) return;
			
			//Find spawner if it exists
			BradleySpawner myspawn = null;
			foreach(BradleySpawner bs in spawners){
				if(bs.spawned==targetTank){
					myspawn=bs;
				}
			}
			//If bradley has a spawner
			if(myspawn!=null){
				
				// Check if tank currently has a path
				bool flag = targetTank.patrolPath==null;
				
				//Advise of action
				basePlayer.ChatMessage(flag?"Resuming Bradley Pathing":"Pausing Bradley Pathing");
				Puts(flag?
					"Pausing Bradley Pathing "+basePlayer.displayName+ " at "+targetTank.transform.position:
					"Resume Bradley Pathing "+basePlayer.displayName+ " at "+targetTank.transform.position);
					
				//Either reset the path to the spawner's path, or clear the path, depending on the flag
				targetTank.patrolPath = flag ? myspawn.path : null;
			}
			//If it doesn't have a spawner just clear path
			else{
				targetTank.patrolPath=null;
			}
		}
		
		void OnServerInitialized(){
			// Take note of all spawners on server initialized to make the command's lookup faster
			spawners =UnityEngine.Object.FindObjectsOfType(typeof(BradleySpawner)) as BradleySpawner[];
		}
        public BaseEntity getLookingAtEntity(BasePlayer player)
        {
			// This will hold the collision result
            RaycastHit hit;
			// Shoot a ray from player eyes and set the info of the hit in hit
            if (Physics.Raycast(player.eyes.HeadRay(), out hit))
            {
				// Use Facepunch's extension RaycastHitEx's static method GetEntity to return the BaseEntity of the gameobject found.
                return RaycastHitEx.GetEntity(hit);
            }
            return null;
        }
	}
}