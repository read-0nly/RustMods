# Public Radio

## Become the madman raving over the airwaves you've always dreamt to be!

The idea is thanks to Delanorix, with special thanks to Facepunch for making this so easy to do.

Allows you to transmit audio over RF to be received and sent to connected speakers in the homes of your listeners. So by interacting with a microphone stand you can host radio shows, streaming audio over voip. Pirate radio at the end of the world really whips the llama's ass

Connects RF Receivers and Broadcasters electrically. This allows connected speakers downstream of the receivers to play audio from sources behind the broadcaster. 
I've tested with radio input and it holds until about 2.5 squares away but you need to get within a square for it to trigger. 
As far as I can tell this is a quirk of radios, who disconnect you from the radio stream past a certain distance and need a minimum distance to trigger.
I couldn't find testers to confirm if the same quirk happens with microphones, but phones work, so should they (in theory)

The way this is implemented is that the broadcasters are given 32 hidden output nodes. These are then connected by hidden wire to each of the receivers, splitting power between them. So your listeners can also steal some power from you.
This has funny quirks - for instance if the broadcaster cannot provide enough power to power all conneted receivers, speakers might turn off. This is untested and I will correct this if it actually doesn't matter.
basically power each broadcaster with 32

There's no broadcaster prioritization outside of the first one found on the frequency gets the listener. This should be updated to distance with falloff at some point and made configurable. If I ever touch it again.

To support more than 32 listeners (if you think you have that many), just use a splitter downstream of your microphone/radio to split the signal to as many broadcasters as you'd like.

Please submit issues if you run into any. If you intend to use this on a server you actually care about, please spin off a testing server locally or something and test it there first. 
This might have balance ramifications if someone finds a way to abuse the power transmission so it might only be appropriate for PVE servers
