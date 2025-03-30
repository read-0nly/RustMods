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
#endregion
namespace Oxide.Plugins{
	[Info("NPCShops", "obsol", "0.2.1")]
	[Description("Sticks a shop on every human npc")]
	public class NPCShops : RustPlugin{
		public int shopCount = 0;
		public int scrap = -932201673;
		public static Configuration config;
		public class Configuration{
			[JsonProperty("Economy", ObjectCreationHandling = ObjectCreationHandling.Replace)]	
			public List<EconomyEntry> Economy = new List<EconomyEntry>();
			
			
			public string ToJson() => JsonConvert.SerializeObject(this);				
			public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
		}
		protected override void LoadDefaultConfig() => config = new Configuration();
		protected override void LoadConfig(){
			base.LoadConfig();
			try{
				config = Config.ReadObject<Configuration>();
				if (config == null) throw new JsonException();
				if (!config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys)){
					Puts("Configuration appears to be outdated; updating and saving");
					SaveConfig();
					}
				if(config.Economy.Count()==0){launchEconomy(); SaveConfig();}
			}
			catch{
				Puts($"Configuration file {Name}.json is invalid; using defaults");
				LoadDefaultConfig();
				launchEconomy();
				
			}
		}
		protected override void SaveConfig(){
			Puts($"Configuration changes saved to {Name}.json");
			Config.WriteObject(config, true);
		}
		
	#region economy
		void launchEconomy(){
			config.Economy = new List<EconomyEntry>();
			config.Economy.Add(new EconomyEntry("12 Gauge Buckshot",-1685290200,32,62));
			config.Economy.Add(new EconomyEntry("12 Gauge Incendiary Shell",-1036635990,32,115));
			config.Economy.Add(new EconomyEntry("12 Gauge Slug",-727717969,32,62));
			config.Economy.Add(new EconomyEntry("16x Zoom Scope",174866732,1,300));
			config.Economy.Add(new EconomyEntry("40mm HE Grenade",349762871,1,54));
			config.Economy.Add(new EconomyEntry("40mm Shotgun Round",1055319033,1,3));
			config.Economy.Add(new EconomyEntry("40mm Smoke Grenade",915408809,1,33));
			config.Economy.Add(new EconomyEntry("5.56 Rifle Ammo",-1211166256,64,57));
			config.Economy.Add(new EconomyEntry("8x Zoom Scope",567235583,1,100));
			config.Economy.Add(new EconomyEntry("Adv. Anti-Rad Tea",-1729415579,1,80));
			config.Economy.Add(new EconomyEntry("Advanced Healing Tea",-2123125470,1,80));
			config.Economy.Add(new EconomyEntry("Advanced Max Health Tea",603811464,1,80));
			config.Economy.Add(new EconomyEntry("Advanced Ore Tea",2063916636,1,80));
			config.Economy.Add(new EconomyEntry("Advanced Rad. Removal Tea",2021351233,1,80));
			config.Economy.Add(new EconomyEntry("Advanced Scrap Tea",524678627,1,80));
			config.Economy.Add(new EconomyEntry("Advanced Wood Tea",-541206665,1,80));
			config.Economy.Add(new EconomyEntry("AND Switch",1171735914,1,10));
			config.Economy.Add(new EconomyEntry("Anti-Rad Tea",-487356515,1,20));
			config.Economy.Add(new EconomyEntry("Armored Cockpit Vehicle Module",1874610722,1,25));
			config.Economy.Add(new EconomyEntry("Armored Door",1353298668,1,665));
			config.Economy.Add(new EconomyEntry("Armored Double Door",1221063409,1,675));
			config.Economy.Add(new EconomyEntry("Armored Passenger Vehicle Module",-1615281216,1,25));
			config.Economy.Add(new EconomyEntry("Assault Rifle",1545779598,1,373));
			config.Economy.Add(new EconomyEntry("Auto Turret",-2139580305,1,1281));
			config.Economy.Add(new EconomyEntry("Basic Healing Tea",-929092070,1,20));
			config.Economy.Add(new EconomyEntry("Basic Max Health Tea",-1184406448,1,20));
			config.Economy.Add(new EconomyEntry("Basic Ore Tea",1480022580,1,20));
			config.Economy.Add(new EconomyEntry("Beancan Grenade",1840822026,1,22));
			config.Economy.Add(new EconomyEntry("Bed",-1273339005,1,48));
			config.Economy.Add(new EconomyEntry("Black Berry",1771755747,2,10));
			config.Economy.Add(new EconomyEntry("Blocker",-690968985,1,8));
			config.Economy.Add(new EconomyEntry("Blue Berry",1112162468,2,10));
			config.Economy.Add(new EconomyEntry("Blue Keycard",-484206264,1,40));
			config.Economy.Add(new EconomyEntry("Bolt Action Rifle",1588298435,1,219));
			config.Economy.Add(new EconomyEntry("Boots",-1549739227,1,57));
			config.Economy.Add(new EconomyEntry("Button",-1778897469,1,8));
			config.Economy.Add(new EconomyEntry("Camper Vehicle Module",-1040518150,1,20));
			config.Economy.Add(new EconomyEntry("CCTV Camera",634478325,1,504));
			config.Economy.Add(new EconomyEntry("Ceiling Light",1142993169,1,30));
			config.Economy.Add(new EconomyEntry("Chainsaw",1104520648,1,350));
			config.Economy.Add(new EconomyEntry("Cloth",-858312878,100,12));
			config.Economy.Add(new EconomyEntry("Cockpit Vehicle Module",-1501451746,1,17));
			config.Economy.Add(new EconomyEntry("Cockpit With Engine Vehicle Module",170758448,1,27));
			config.Economy.Add(new EconomyEntry("Coffee Can Helmet",-803263829,1,23));
			config.Economy.Add(new EconomyEntry("Combat Knife",2040726127,1,5));
			config.Economy.Add(new EconomyEntry("Computer Station",-1588628467,1,1287));
			config.Economy.Add(new EconomyEntry("Corn",1367190888,10,7));
			config.Economy.Add(new EconomyEntry("Counter",-216999575,1,8));
			config.Economy.Add(new EconomyEntry("Custom SMG",1796682209,1,95));
			config.Economy.Add(new EconomyEntry("Diesel Fuel",1568388703,1,150));
			config.Economy.Add(new EconomyEntry("Diving Fins",296519935,1,25));
			config.Economy.Add(new EconomyEntry("Diving Mask",-113413047,1,15));
			config.Economy.Add(new EconomyEntry("Diving Tank",-2022172587,1,35));
			config.Economy.Add(new EconomyEntry("Door Closer",1409529282,1,63));
			config.Economy.Add(new EconomyEntry("Door Controller",-502177121,1,8));
			config.Economy.Add(new EconomyEntry("Double Barrel Shotgun",-765183617,1,250));
			config.Economy.Add(new EconomyEntry("Electric Fuse",-629028935,1,20));
			config.Economy.Add(new EconomyEntry("Electric Heater",-784870360,1,75));
			config.Economy.Add(new EconomyEntry("Electrical Branch",-1448252298,1,8));
			config.Economy.Add(new EconomyEntry("Elevator",1177596584,1,151));
			config.Economy.Add(new EconomyEntry("Empty Propane Tank",-1673693549,1,20));
			config.Economy.Add(new EconomyEntry("Engine Vehicle Module",1559779253,1,16));
			config.Economy.Add(new EconomyEntry("Explosive 5.56 Rifle Ammo",-1321651331,64,299));
			config.Economy.Add(new EconomyEntry("Explosives",-592016202,5,104));
			config.Economy.Add(new EconomyEntry("Explosives",-592016202,1,20));
			config.Economy.Add(new EconomyEntry("F1 Grenade",143803535,1,13));
			config.Economy.Add(new EconomyEntry("Flame Thrower",-1215753368,1,262));
			config.Economy.Add(new EconomyEntry("Flame Turret",528668503,1,250));
			config.Economy.Add(new EconomyEntry("Flasher Light",-939424778,1,12));
			config.Economy.Add(new EconomyEntry("Flatbed Vehicle Module",-1880231361,1,12));
			config.Economy.Add(new EconomyEntry("Fluid Combiner",-265292885,1,30));
			config.Economy.Add(new EconomyEntry("Fluid Splitter",-1166712463,1,30));
			config.Economy.Add(new EconomyEntry("Fluid Switch & Pump",443432036,1,30));
			config.Economy.Add(new EconomyEntry("Fridge",1413014235,1,8));
			config.Economy.Add(new EconomyEntry("Fuel Tank Vehicle Module",1186655046,1,20));
			config.Economy.Add(new EconomyEntry("Garage Door",-148794216,1,280));
			config.Economy.Add(new EconomyEntry("Gears",479143914,1,125));
			config.Economy.Add(new EconomyEntry("Green Berry",858486327,2,10));
			config.Economy.Add(new EconomyEntry("Green Keycard",37122747,1,15));
			config.Economy.Add(new EconomyEntry("Hazmat Suit",1266491000,1,196));
			config.Economy.Add(new EconomyEntry("HBHF Sensor",-1507239837,1,8));
			config.Economy.Add(new EconomyEntry("Heavy Plate Helmet",1181207482,1,38));
			config.Economy.Add(new EconomyEntry("Heavy Plate Jacket",-1102429027,1,68));
			config.Economy.Add(new EconomyEntry("Heavy Plate Pants",-1778159885,1,38));
			config.Economy.Add(new EconomyEntry("High External Stone Gate",-691113464,1,640));
			config.Economy.Add(new EconomyEntry("High External Stone Wall",-967648160,1,75));
			config.Economy.Add(new EconomyEntry("High External Wooden Gate",-335089230,1,310));
			config.Economy.Add(new EconomyEntry("High External Wooden Wall",99588025,1,30));
			config.Economy.Add(new EconomyEntry("High Quality Carburetor",656371026,1,17));
			config.Economy.Add(new EconomyEntry("High Quality Crankshaft",1158340332,1,12));
			config.Economy.Add(new EconomyEntry("High Quality Metal",317398316,10,20));
			config.Economy.Add(new EconomyEntry("High Quality Pistons",1883981800,2,24));
			config.Economy.Add(new EconomyEntry("High Quality Spark Plugs",1072924620,2,24));
			config.Economy.Add(new EconomyEntry("High Quality Valves",-1802083073,2,24));
			config.Economy.Add(new EconomyEntry("High Velocity Rocket",-1841918730,1,64));
			config.Economy.Add(new EconomyEntry("Holosight",442289265,1,274));
			config.Economy.Add(new EconomyEntry("Homemade Landmine",-1663759755,1,15));
			config.Economy.Add(new EconomyEntry("Hoodie",1751045826,1,20));
			config.Economy.Add(new EconomyEntry("HV 5.56 Rifle Ammo",1712070256,64,246));
			config.Economy.Add(new EconomyEntry("HV Pistol Ammo",-1691396643,64,164));
			config.Economy.Add(new EconomyEntry("Incendiary 5.56 Rifle Ammo",605467368,64,166));
			config.Economy.Add(new EconomyEntry("Incendiary Pistol Bullet",51984655,64,111));
			config.Economy.Add(new EconomyEntry("Incendiary Rocket",1638322904,1,290));
			config.Economy.Add(new EconomyEntry("Jackhammer",1488979457,1,150));
			config.Economy.Add(new EconomyEntry("L96 Rifle",-778367295,1,339));
			config.Economy.Add(new EconomyEntry("Ladder Hatch",1948067030,1,423));
			config.Economy.Add(new EconomyEntry("Large Flatbed Vehicle Module",-1693832478,1,18));
			config.Economy.Add(new EconomyEntry("Large Furnace",-1992717673,1,350));
			config.Economy.Add(new EconomyEntry("Large Medkit",254522515,1,21));
			config.Economy.Add(new EconomyEntry("Large Planter Box",1581210395,1,64));
			config.Economy.Add(new EconomyEntry("Large Rechargeable Battery",553270375,1,520));
			config.Economy.Add(new EconomyEntry("Large Solar Panel",2090395347,1,260));
			config.Economy.Add(new EconomyEntry("Large Water Catcher",-1100168350,1,90));
			config.Economy.Add(new EconomyEntry("Laser Detector",-798293154,1,8));
			config.Economy.Add(new EconomyEntry("Leather",1381010055,100,200));
			config.Economy.Add(new EconomyEntry("Leather",1381010055,1,2));
			config.Economy.Add(new EconomyEntry("Locker",-110921842,1,40));
			config.Economy.Add(new EconomyEntry("Longsword",-1469578201,1,40));
			config.Economy.Add(new EconomyEntry("Low Grade Fuel",-946369541,100,50));
			config.Economy.Add(new EconomyEntry("LR-300 Assault Rifle",-1812555177,1,500));
			config.Economy.Add(new EconomyEntry("M249",-2069578888,1,624));
			config.Economy.Add(new EconomyEntry("M39 Rifle",28201841,1,400));
			config.Economy.Add(new EconomyEntry("M92 Pistol",-852563019,1,250));
			config.Economy.Add(new EconomyEntry("Medical Syringe",1079279582,2,16));
			config.Economy.Add(new EconomyEntry("Medium Rechargeable Battery",2023888403,1,260));
			config.Economy.Add(new EconomyEntry("Memory Cell",-746647361,1,8));
			config.Economy.Add(new EconomyEntry("Metal Barricade",1655650836,1,50));
			config.Economy.Add(new EconomyEntry("Metal Blade",1882709339,1,15));
			config.Economy.Add(new EconomyEntry("Metal Chest plate",1110385766,1,270));
			config.Economy.Add(new EconomyEntry("Metal Facemask",-194953424,1,220));
			config.Economy.Add(new EconomyEntry("Metal Fragments",69511070,250,25));
			config.Economy.Add(new EconomyEntry("Metal horizontal embrasure",-1199897169,1,10));
			config.Economy.Add(new EconomyEntry("Metal Pipe",95950017,1,30));
			config.Economy.Add(new EconomyEntry("Metal Spring",-1021495308,1,60));
			config.Economy.Add(new EconomyEntry("Metal Vertical embrasure",-1199897172,1,10));
			config.Economy.Add(new EconomyEntry("Mining Quarry",1052926200,1,1000));
			config.Economy.Add(new EconomyEntry("Mixing Table",1259919256,1,175));
			config.Economy.Add(new EconomyEntry("MLRS Aiming Module",343045591,1,505));
			config.Economy.Add(new EconomyEntry("MLRS Rocket",-1843426638,1,94));
			config.Economy.Add(new EconomyEntry("Modular Car Lift",1696050067,1,155));
			config.Economy.Add(new EconomyEntry("MP5A4",1318558775,1,169));
			config.Economy.Add(new EconomyEntry("Multiple Grenade Launcher",-1123473824,1,86));
			config.Economy.Add(new EconomyEntry("Muzzle Boost",-1405508498,1,20));
			config.Economy.Add(new EconomyEntry("Muzzle Brake",1478091698,1,16));
			config.Economy.Add(new EconomyEntry("Night Vision Goggles",-1518883088,1,520));
			config.Economy.Add(new EconomyEntry("OR Switch",-1286302544,1,10));
			config.Economy.Add(new EconomyEntry("Paddle",1491189398,1,25));
			config.Economy.Add(new EconomyEntry("Pants",237239288,1,20));
			config.Economy.Add(new EconomyEntry("Passenger Vehicle Module",895374329,1,20));
			config.Economy.Add(new EconomyEntry("Pistol Bullet",785728077,64,43));
			config.Economy.Add(new EconomyEntry("Potato",-2086926071,10,50));
			config.Economy.Add(new EconomyEntry("Powered Water Purifier",-365097295,1,150));
			config.Economy.Add(new EconomyEntry("Pressure Pad",-2049214035,1,188));
			config.Economy.Add(new EconomyEntry("Pump Jack",-1130709577,1,750));
			config.Economy.Add(new EconomyEntry("Pump Shotgun",795371088,1,150));
			config.Economy.Add(new EconomyEntry("Pumpkin",-567909622,10,50));
			config.Economy.Add(new EconomyEntry("Pure Anti-Rad Tea",-33009419,1,320));
			config.Economy.Add(new EconomyEntry("Pure Healing Tea",-1677315902,1,320));
			config.Economy.Add(new EconomyEntry("Pure Max Health Tea",1712261904,1,320));
			config.Economy.Add(new EconomyEntry("Pure Ore Tea",1729374708,1,320));
			config.Economy.Add(new EconomyEntry("Pure Rad. Removal Tea",1905387657,1,320));
			config.Economy.Add(new EconomyEntry("Pure Scrap Tea",2024467711,1,320));
			config.Economy.Add(new EconomyEntry("Pure Wood Tea",-557539629,1,320));
			config.Economy.Add(new EconomyEntry("Python Revolver",1373971859,1,200));
			config.Economy.Add(new EconomyEntry("Rad. Removal Tea",-496584751,1,20));
			config.Economy.Add(new EconomyEntry("RAND Switch",492357192,1,8));
			config.Economy.Add(new EconomyEntry("Rear Seats Vehicle Module",1376065505,1,17));
			config.Economy.Add(new EconomyEntry("Red Berry",1272194103,2,10));
			config.Economy.Add(new EconomyEntry("Red Keycard",-1880870149,1,80));
			config.Economy.Add(new EconomyEntry("Reinforced Glass Window",671706427,1,8));
			config.Economy.Add(new EconomyEntry("RF Broadcaster",-1044468317,1,260));
			config.Economy.Add(new EconomyEntry("RF Pager",-566907190,1,258));
			config.Economy.Add(new EconomyEntry("RF Transmitter",596469572,1,260));
			config.Economy.Add(new EconomyEntry("Rifle Body",176787552,1,29));
			config.Economy.Add(new EconomyEntry("Road Sign Jacket",-2002277461,1,170));
			config.Economy.Add(new EconomyEntry("Road Sign Kilt",1850456855,1,100));
			config.Economy.Add(new EconomyEntry("Road Signs",1199391518,1,50));
			config.Economy.Add(new EconomyEntry("Roadsign Gloves",-699558439,1,120));
			config.Economy.Add(new EconomyEntry("Rocket",-742865266,1,319));
			config.Economy.Add(new EconomyEntry("Rocket Launcher",442886268,1,200));
			config.Economy.Add(new EconomyEntry("Root Combiner",-458565393,1,8));
			config.Economy.Add(new EconomyEntry("Rope",1414245522,1,4));
			config.Economy.Add(new EconomyEntry("Salvaged Axe",-262590403,1,105));
			config.Economy.Add(new EconomyEntry("Salvaged Cleaver",-1978999529,1,55));
			config.Economy.Add(new EconomyEntry("Salvaged Hammer",-1506397857,1,35));
			config.Economy.Add(new EconomyEntry("Salvaged Icepick",-1780802565,1,105));
			config.Economy.Add(new EconomyEntry("Salvaged Shelves",1950721418,1,25));
			config.Economy.Add(new EconomyEntry("Salvaged Sword",1326180354,1,17));
			config.Economy.Add(new EconomyEntry("SAM Ammo",-384243979,1,31));
			config.Economy.Add(new EconomyEntry("SAM Site",-1009359066,1,500));
			config.Economy.Add(new EconomyEntry("Satchel Charge",-1878475007,1,94));
			config.Economy.Add(new EconomyEntry("Scrap",-932201673,1,20));
			config.Economy.Add(new EconomyEntry("Search Light",2087678962,1,30));
			config.Economy.Add(new EconomyEntry("Semi Automatic Body",573926264,1,27));
			config.Economy.Add(new EconomyEntry("Semi-Automatic Pistol",818877484,1,65));
			config.Economy.Add(new EconomyEntry("Semi-Automatic Rifle",-904863145,1,140));
			config.Economy.Add(new EconomyEntry("Sewing Kit",1234880403,1,15));
			config.Economy.Add(new EconomyEntry("Sheet Metal",-1994909036,1,30));
			config.Economy.Add(new EconomyEntry("Shotgun Trap",352499047,1,293));
			config.Economy.Add(new EconomyEntry("Silencer",-1850571427,1,10));
			config.Economy.Add(new EconomyEntry("Siren Light",762289806,1,12));
			config.Economy.Add(new EconomyEntry("Small Generator",1849887541,1,260));
			config.Economy.Add(new EconomyEntry("Small Oil Refinery",-1293296287,1,179));
			config.Economy.Add(new EconomyEntry("Small Planter Box",1903654061,1,32));
			config.Economy.Add(new EconomyEntry("Small Rechargeable Battery",-692338819,1,10));
			config.Economy.Add(new EconomyEntry("Small Water Catcher",-132247350,1,37));
			config.Economy.Add(new EconomyEntry("Smart Alarm",-695978112,1,256));
			config.Economy.Add(new EconomyEntry("Smart Switch",988652725,1,256));
			config.Economy.Add(new EconomyEntry("SMG Body",1230323789,1,19));
			config.Economy.Add(new EconomyEntry("Snow Jacket",-48090175,1,23));
			config.Economy.Add(new EconomyEntry("Spas-12 Shotgun",-41440462,1,250));
			config.Economy.Add(new EconomyEntry("Speargun",-1517740219,1,9));
			config.Economy.Add(new EconomyEntry("Speargun Spear",-1800345240,1,5));
			config.Economy.Add(new EconomyEntry("Splitter",-563624462,1,10));
			config.Economy.Add(new EconomyEntry("Sprinkler",-781014061,1,15));
			config.Economy.Add(new EconomyEntry("Stones",-2099697608,1000,50));
			config.Economy.Add(new EconomyEntry("Storage Monitor",1149964039,1,256));
			config.Economy.Add(new EconomyEntry("Storage Vehicle Module",268565518,1,8));
			config.Economy.Add(new EconomyEntry("Strengthened Glass Window",-1614955425,1,5));
			config.Economy.Add(new EconomyEntry("Survey Charge",1975934948,1,22));
			config.Economy.Add(new EconomyEntry("Switch",1951603367,1,10));
			config.Economy.Add(new EconomyEntry("Tactical Gloves",-1108136649,1,95));
			config.Economy.Add(new EconomyEntry("Targeting Computer",1523195708,1,757));
			config.Economy.Add(new EconomyEntry("Tarp",2019042823,1,30));
			config.Economy.Add(new EconomyEntry("Taxi Vehicle Module",-626174997,1,30));
			config.Economy.Add(new EconomyEntry("Tech Trash",73681876,1,250));
			config.Economy.Add(new EconomyEntry("Tesla Coil",1371909803,1,256));
			config.Economy.Add(new EconomyEntry("Test Generator",-295829489,1,346));
			config.Economy.Add(new EconomyEntry("Thompson",-1758372725,1,101));
			config.Economy.Add(new EconomyEntry("Timed Explosive Charge",1248356124,1,918));
			config.Economy.Add(new EconomyEntry("Timer",665332906,1,8));
			config.Economy.Add(new EconomyEntry("Torpedo",-1671551935,1,38));
			config.Economy.Add(new EconomyEntry("Triangle Ladder Hatch",2041899972,1,423));
			config.Economy.Add(new EconomyEntry("Watch Tower",-463122489,1,4));
			config.Economy.Add(new EconomyEntry("Water Jug",-119235651,1,5));
			config.Economy.Add(new EconomyEntry("Water Pump",-1284169891,1,200));
			config.Economy.Add(new EconomyEntry("Weapon Flashlight",952603248,1,6));
			config.Economy.Add(new EconomyEntry("Weapon Lasersight",-132516482,1,256));
			config.Economy.Add(new EconomyEntry("Wetsuit",-1101924344,1,20));
			config.Economy.Add(new EconomyEntry("White Berry",854447607,2,10));
			config.Economy.Add(new EconomyEntry("Wind Turbine",-1819763926,1,500));
			config.Economy.Add(new EconomyEntry("Wood",-151838493,1000,20));
			config.Economy.Add(new EconomyEntry("Wooden Barricade",866889860,1,10));
			config.Economy.Add(new EconomyEntry("Wooden Ladder",-316250604,1,18));
			config.Economy.Add(new EconomyEntry("Workbench Level 1",1524187186,1,70));
			config.Economy.Add(new EconomyEntry("Workbench Level 2",-41896755,1,590));
			config.Economy.Add(new EconomyEntry("Workbench Level 3",-1607980696,1,1550));
			config.Economy.Add(new EconomyEntry("XOR Switch",1293102274,1,10));
			config.Economy.Add(new EconomyEntry("Yellow Berry",1660145984,2,10));

		}
	
	#endregion
	
		//
		void Unload(){
			SaveConfig();
			foreach(InvisibleVendingMachine s in GameObject.FindObjectsOfType<InvisibleVendingMachine>()){
				if(s.transform.parent!=null){
					HumanNPC hn = s.transform.parent.gameObject.GetComponent<HumanNPC>();
					if(hn!=null || s.shopName.Contains("[NPCShop]")){
						s.Kill();
					}
				}
			}
		}
		
		void Loaded(){
			LoadConfig();
			
		}
		
		public class EconomyEntry{
			public string name;
			public int id;
			public int amount;
			public int price;
			public EconomyEntry(string s,int i,int a,int p){
				name=s;id=i;amount=a;price=p;
			}
			
		}		
		
		object OnAIInitialize(ScientistBrain player){AddVending(player.GetComponent<HumanNPC>(),new Vector3(0,0,0)); return null;}
		
		void OnServerInitialized(){
			foreach(InvisibleVendingMachine s in GameObject.FindObjectsOfType<InvisibleVendingMachine>()){
				if(s.transform.parent!=null ){
					HumanNPC hn = s.transform.parent.gameObject.GetComponent<HumanNPC>();
					if(hn!=null){
						hn.gameObject.GetComponentInChildren<InvisibleVendingMachine>().Kill();
						AddVending(hn, new Vector3(0,0,0));
					}
				}else if(s.transform.name=="NPC Shop"|| (s.vendingOrders == null)){
					s.Kill();
				}
			}
		}
		void makeShop(InvisibleVendingMachine seat,int offset){
			
			float factor = UnityEngine.Random.Range(0.0f,1.0f)*0.5f+0.9f;
			List<EconomyEntry> lEcon = new List<EconomyEntry>();
			Shuffle(config.Economy,offset+(int)Math.Abs(seat.transform.position.x*100)+(int)Math.Abs(seat.transform.position.z*100));
			lEcon.AddRange(config.Economy.ToArray());
			if(lEcon.Count==0){
				
				return;
			}
			Shuffle(lEcon,UnityEngine.Random.Range(0,20000));
			EconomyEntry econ = lEcon[(0+shopCount)%lEcon.Count()];
			NPCVendingOrder.Entry[] entries = seat.vendingOrders.orders.ToArray();
			seat.vendingOrders = ScriptableObject.CreateInstance("NPCVendingOrder") as NPCVendingOrder;
			seat.vendingOrders.orders = entries;
			seat.shopName = seat.shopName.Replace("[NPCShop]","")+"[NPCShop]";
			for(int i = 0; i < seat.vendingOrders.orders.Count();i++){
				NPCVendingOrder.Entry newOrder = new NPCVendingOrder.Entry();				
				
				newOrder.sellItem=seat.vendingOrders.orders[i].sellItem;
				newOrder.sellItemAmount=seat.vendingOrders.orders[i].sellItemAmount;
				newOrder.sellItemAsBP=seat.vendingOrders.orders[i].sellItemAsBP;
				newOrder.currencyItem=seat.vendingOrders.orders[i].currencyItem;
				newOrder.currencyAmount=seat.vendingOrders.orders[i].currencyAmount;
				newOrder.currencyAsBP=seat.vendingOrders.orders[i].currencyAsBP;
				newOrder.weight=seat.vendingOrders.orders[i].weight;
				newOrder.refillAmount=seat.vendingOrders.orders[i].refillAmount;
				newOrder.refillDelay=seat.vendingOrders.orders[i].refillDelay;
				
				seat.vendingOrders.orders[i] = newOrder;
			}
			seat.vendingOrders.orders[0].currencyItem = ItemManager.FindItemDefinition(scrap);
			seat.vendingOrders.orders[0].currencyAmount =(int)(econ.price*factor);
			seat.vendingOrders.orders[0].currencyAsBP=false;
			seat.vendingOrders.orders[0].sellItemAmount=econ.amount;
			seat.vendingOrders.orders[0].sellItem=ItemManager.FindItemDefinition(econ.id);
			seat.vendingOrders.orders[0].sellItemAsBP=false;
			
			factor = UnityEngine.Random.Range(0.0f,1.0f)*0.5f+1f;
			econ = lEcon[(1+shopCount)%lEcon.Count()];
			seat.vendingOrders.orders[1].currencyItem = ItemManager.FindItemDefinition(scrap);
			seat.vendingOrders.orders[1].currencyAmount =(int)(econ.price*factor);
			seat.vendingOrders.orders[1].currencyAsBP=false;
			seat.vendingOrders.orders[1].sellItemAmount=econ.amount;
			seat.vendingOrders.orders[1].sellItem=ItemManager.FindItemDefinition(econ.id);
			seat.vendingOrders.orders[1].sellItemAsBP=false;
			
			factor = UnityEngine.Random.Range(0.0f,1.0f)*0.5f+0.9f;
			econ = lEcon[(2+shopCount)%lEcon.Count()];
			seat.vendingOrders.orders[2].currencyItem = ItemManager.FindItemDefinition(scrap);
			seat.vendingOrders.orders[2].currencyAmount =(int)(econ.price*factor);
			seat.vendingOrders.orders[2].currencyAsBP=false;
			seat.vendingOrders.orders[2].sellItemAmount=econ.amount;
			seat.vendingOrders.orders[2].sellItem=ItemManager.FindItemDefinition(econ.id);
			seat.vendingOrders.orders[2].sellItemAsBP=false;
			
			factor = UnityEngine.Random.Range(0.0f,1.0f)*0.5f+0.9f;
			econ = lEcon[(3+shopCount)%lEcon.Count()];
			seat.vendingOrders.orders[3].currencyItem = ItemManager.FindItemDefinition(scrap);
			seat.vendingOrders.orders[3].currencyAmount =(int)(econ.price*factor);
			seat.vendingOrders.orders[3].currencyAsBP=false;
			seat.vendingOrders.orders[3].sellItemAmount=econ.amount;
			seat.vendingOrders.orders[3].sellItem=ItemManager.FindItemDefinition(econ.id);
			seat.vendingOrders.orders[3].sellItemAsBP=false;
			
			factor = UnityEngine.Random.Range(0.0f,1.0f)*0.5f+0.8f;
			econ = lEcon[(4+shopCount)%lEcon.Count()];
			seat.vendingOrders.orders[4].currencyItem = ItemManager.FindItemDefinition(scrap);
			seat.vendingOrders.orders[4].currencyAmount =(int)(econ.price*factor);
			seat.vendingOrders.orders[4].currencyAsBP=false;
			seat.vendingOrders.orders[4].sellItemAmount=econ.amount;
			seat.vendingOrders.orders[4].sellItem=ItemManager.FindItemDefinition(econ.id);
			seat.vendingOrders.orders[4].sellItemAsBP=false;
			
			factor = UnityEngine.Random.Range(0.0f,1.0f)*0.5f+0.8f;
			econ = lEcon[(5+shopCount)%lEcon.Count()];
			seat.vendingOrders.orders[5].currencyItem = ItemManager.FindItemDefinition(econ.id);
			seat.vendingOrders.orders[5].currencyAmount = econ.amount;
			seat.vendingOrders.orders[5].currencyAsBP=false;
			seat.vendingOrders.orders[5].sellItemAmount=(int)(econ.price*factor);
			seat.vendingOrders.orders[5].sellItem=ItemManager.FindItemDefinition(scrap);
			seat.vendingOrders.orders[5].sellItemAsBP=false;
			
			factor = UnityEngine.Random.Range(0.0f,1.0f)*0.5f+0.7f;
			econ = lEcon[(6+shopCount)%lEcon.Count()];
			seat.vendingOrders.orders[6].currencyItem = ItemManager.FindItemDefinition(econ.id);
			seat.vendingOrders.orders[6].currencyAmount = econ.amount;
			seat.vendingOrders.orders[6].currencyAsBP=false;
			seat.vendingOrders.orders[6].sellItemAmount=(int)(econ.price*factor);
			seat.vendingOrders.orders[6].sellItem=ItemManager.FindItemDefinition(scrap);
			seat.vendingOrders.orders[6].sellItemAsBP=false;
			
			
			
			shopCount++;
			seat.SendNetworkUpdateImmediate(true);
		}
		void AddVending(HumanNPC ent, Vector3 locPos) {
			//BaseEntity seat = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/npcvendingmachine.prefab", ent.transform.position, new Quaternion()) as BaseEntity;
			if(ent == null)return;
			InvisibleVendingMachine seat = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/npcvendingmachines/shopkeeper_vm_invis.prefab", ent.transform.position, new Quaternion()) as InvisibleVendingMachine;
			if (seat == null) return;
			seat.Spawn();
			seat.SetParent(ent);		
			seat.transform.Rotate(new Vector3(90,0,0));
			seat.transform.SetParent(ent.transform);	
			seat.transform.localPosition = new Vector3(0,2f,-1f);
			seat.SetFlag(VendingMachine.VendingMachineFlags.Broadcasting,false);
			seat.syncPosition=true;
			seat.transform.name="NPC Shop";
			GameObject.Destroy(seat.GetComponent<GroundWatch>());
			if(seat.gameObject.GetComponent<Rigidbody>()!=null)
				seat.gameObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ| RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ ;
			
			makeShop(seat,(int)ent.userID);
			seat.SendNetworkUpdateImmediate(true);
			
		}
	
			
		System.Random rng = new System.Random(); 
		private void Shuffle(List<EconomyEntry> list,int seed)  
		{  
			int n = list.Count;  
			while (n > 1) {  
				n--;  
				int k = rng.Next(n + 1);  
				EconomyEntry v = list[k];  
				list[k] = list[n];  
				list[n] = v;  
			}  
		}
		
		private void Shuffle(UnityEngine.Object[] list,int seed)  
		{  
			int n = list.Length;  
			while (n > 1) {  
				n--;  
				int k = rng.Next(n + 1);  
				UnityEngine.Object v = list[k];  
				list[k] = list[n];  
				list[n] = v;  
			}  
		}
		private System.Object[] Shuffle(System.Object[] list,int seed)  
		{  
			int n = list.Length;  
			while (n > 1) {  
				n--;  
				int k = rng.Next(n + 1);  
				System.Object v = list[k];  
				list[k] = list[n];  
				list[n] = v;  
			}  
			return list;
		}
	

	
	}
}
