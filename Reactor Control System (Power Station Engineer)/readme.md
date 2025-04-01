## Video of this thing:
[![IMAGE ALT TEXT](http://img.youtube.com/vi/V2HF4nhzvkc/0.jpg)](https://www.youtube.com/watch?v=V2HF4nhzvkc "Video Title")


Quick snapshot of... well, 3 mods I'm working on. But mostly Reactor Control System

Depends on PowerTransmission and COBALTOS. PowerTransmission lets you route power over RF. CobaltOS binds UI panels and commands to cassettes - place cassette in computer station and mount to call UI, pretty easy to integrate other mods into it as well
The idea is gonna be that when you reach the control room of the power plant, you'll find a computer station with the RCS tape already loaded up. You'll then be able to loot it, use it in place, or once I get the phones-as-modem idea figured out, take note of the phone number of parented phone and dial in from your own computer station to run the loaded tape at that station.

The idea is that you control water pumped in, and the control rods. If it runs dry, temperature quickly ramps up. If it overflows or overheats, it autoscrams (it could do something else, maybe nuke the island, mulling over it still). Temperature ramps from the output of the last tick, then is used by flow to add to it (water to steam, basically), then water is consumed to scale power out using a messy equation but basically assume it multiplies them. You can get around 3k rustwatts out of the system if balanced to run at the edge of it's limits, but then you'll have to regularly get it back up there as it falls out of balance and autoscrams.

Part of an idea I've been playing with of Public Works, or ways to revive the functionality of monuments to benefit the whole server, or to sabotage it to hinder them. Something that changes world state to some extent when completed.
And it's a nice middle ground between standard power generation and the ton of entities that requires or just giving everyone a free test gen. This way you still get less power generation entities, but players still gotta work for it a little - and when it's done, they all benefit just the same as long as they keep maintaining it
