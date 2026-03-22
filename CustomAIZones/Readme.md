## For server owners
Download the file CustomAIZones.cs and pop it in the oxide plugins folder, ignore the rest

## For map makers
### Before starting
You need to use a specific prefab to add the patrol and cover points to your monuments. The prefab is assets/prefabs/npc/scientist/patrolpoint.prefab (for now, until facepunch deletes it and breaks the plugin lol)

For that, you'll need to set this to true
```json
"Prefabs": {
  "Show all prefabs (WARNING: Some prefabs may not work or break the editor/your map)": true
},
```

The patrol point is an invisible prefab. I strongly suggest adding a custom placeholder for it, like how the volumes have that blue cube. To do so, add this to your PlaceHolders
```json
"Custom Placeholders": {
  "assets/prefabs/npc/scientist/patrolpoint.prefab": "Cube"
}
```
That'll be the same blue cube as normal volumes though. To make it easier to tell that the patrol points are not just another volume, I also suggest adding a placeholder color in "Placeholder Colors". The example makes it red
```json
"Placeholder Colors": {
  "assets/prefabs/npc/scientist/patrolpoint.prefab": "FF0000"
}
```

I added an example config file to the repo. I recommend looking at it as well to properly understand how to set it up. If you wanted, you could just copy the whole placeholders block and overwrite it in your file. Here it is:
```json

  "PlaceHolders": {
    "Custom Placeholders": {
      "assets/prefabs/npc/scientist/patrolpoint.prefab": "Cube"
    },
    "Placeholder Colors": {
      "assets/bundled/prefabs/autospawn/clutter/v2_misc_fireflies/fireflies.prefab": "00AFFF",
      "assets/bundled/prefabs/autospawn/decor/riversound/river-sound-emitter.prefab": "00AFFF",
      "assets/bundled/prefabs/modding/spawn_point.prefab": "007F00",
      "assets/bundled/prefabs/static/chair.invisible.static.prefab": "00AFFF",
      "assets/content/nature/ambientlife/fireflies/fireflies.prefab": "00AFFF",
      "assets/content/nature/dunes/pfx sand.prefab": "00AFFF",
      "assets/prefabs/io/electric/other/alarmsound.prefab": "AA0114",
      "assets/prefabs/npc/scientist/patrolpoint.prefab": "FF0000"
    }
  },
```

### Actually making functional paths and cover points
The patrol points have to do double-duty. There used to be cover points but FP deleted them :(
The logic of paths and cover and all that are encoded in the scale of the patrol point. I sueggest just looking at the demo map to understand what's going on.

#### Defining the zone
All points contained within a monument marker belong to that monument. Patrol points outside of the monument marker will be ignored. Scale the monument marker to encompass all NPC spawners and patrol points
<img width="1063" height="781" alt="image" src="https://github.com/user-attachments/assets/b337c425-0553-4d2b-899e-f259b61c3cb7" />

#### Cover Points
Cover points are easiest - if the Y scale is negative, instead of a patrol point, it's a cover point. Doesn't need to be -1, just less that 0
<img width="323" height="546" alt="image" src="https://github.com/user-attachments/assets/ba3b4596-4737-4fac-bd31-55918838ed8f" />

Then set the Space to Local and point the blue arrow towards the thing providing cover
<img width="696" height="600" alt="image" src="https://github.com/user-attachments/assets/97180520-2193-43bc-ba13-a2e3bb9b9fc6" />

#### Patrol points
Points are linked into paths. The X scale of the point is the Path ID (all points sharing the same X scale within the monument are members of the same path). The Y scale is how long NPCs idle at a point before moving on. The Z scale is the point's index within the path. Specifically, all points of the same X scale are added to a list then ordered in ascending order of Z scale. 

In this example, the selected point is the second point of the 2-point path X=1.02, with the other point to the right having a Z scale of 1. Since 1.01 is higher than 1, this point is after the other one.
<img width="1000" height="628" alt="image" src="https://github.com/user-attachments/assets/08b573e7-7ec7-4486-8771-0fdd3c33d073" />


## Bugs/considerations
- Because of navmesh reasons the spawned NPC needs to be replaced with a modified NPC that I tinker with before spawning in. If you have other mods spawning in NPCs this can lead to NPC duplication. File an issue and give me the mod name and I'll look into a way of making them play nice together.
- It might affect NPCs it shouldn't. In testing, everything seems to behave to me, but if you find cases where it's breaking NPCs let me know
- Your NPC spawners need to be close to a navmesh. Not everything has a navmesh so it can take some guessing. The invisible collider does have navmesh, in a pinch.
- The NPCs are wider and taller than they look, even if it looks like they'd fit you might need to give them more clearance when making the map
