This is a whole fucking bespoke Internet Radio server built out of raw sockets and with a hand-crafter MPEG frame carver stuffed into an oxide mod babeyyyyyy

You need an available second port from which to serve the MP3 audio stream. Your server might not have that, and you also need to know what it is to configure this thing, if you can't find it reach out to your server's support they should be able to help.

This could be bandwidth heavy, it's media streaming afterall. If you don't have unlimited bandwidth KEEP AN EYE ON IT. What should help is the following
- Ingame stereos aren't stereo anyways - mono that shit
- they also sound a little shitty - you don't need to be encoding at 320kbps, turn it down
- you can re-encode your MP3s with Audacity. Stripping the metadata will also make load eaier
  
Shove the mod in your oxide/plugins folder, launch the server to make the config file, kill the server, enter your server IP and second available port in the config, create oxide/media, create folders to represent the custom radios, stuff MP3s in the folders, relaunch the server, use a radio and the custom stations should be auto-added in there.

This is rough right now, wanted to get it out the gate so it could be linux-tested - it's all mono so it should work but still. On windows host for the server there's a socket concurrency limit of 10 (10 max listeners) with no way around it. On linux, I expect it to be higher. It works great at 10 listeners but it could get rough at 100, I have no way to tell


PLEASE post an issue if you run into trouble with server logs and screenshot of any player-side errors in their f1 console. TY <3
