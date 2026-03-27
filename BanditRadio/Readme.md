This is a whole fucking bespoke Internet Radio server built out of raw sockets and with a hand-crafted MPEG frame carver stuffed into an oxide mod babeyyyyyy

It's in a rough state - I need some server admins to test it and give me feedback. Please don't hesitate to file an issue (include your server logs if it's an actual issue)

You need an available second port from which to serve the MP3 audio stream. Your server might not have that, and you also need to know what it is to configure this thing, if you can't find it reach out to your server's support they should be able to help.

This could be bandwidth heavy, it's media streaming after all. If you don't have unlimited bandwidth KEEP AN EYE ON IT. What should help is the following.
- Ingame stereos aren't stereo anyways - re-encode mono
- The ingame radio also kind of sounds *bad* - re-encode with a lower kbps, 128 is probably still excessive, 96kbps sounded kinda the same. The lower you can get away with the better
- This doesn't parse MP3 metadata anyways so feel free to strip that too to make the file smaller.


You can do all this in Audacity by importing audio then picking the MP3 then exporting the audio back to mp3, it'll let you pick the settings.
  
1. Shove the mod in your oxide/plugins folder
2. Launch the server to make the config file
3. Kill the server
4. Enter your server IP and second available port in the config
5. Create oxide/media folder
6. Create folders to represent the custom radio stations
7. Stuff MP3s in the folders
8. Relaunch the server
9. Set the radio station on an in-game radio, you should see your custom stations now.

The folder structure should be like this:

- ./oxide/media
  - Album
    - MP3 File
    - MP3 File
    - MP3 File
  - Album
    - MP3 File
    - MP3 File
    - MP3 File
    - MP3 File
    - MP3 File


This is rough right now, wanted to get it out the gate so it could be linux-tested, I've only tested it on windows, and only on a LAN - if you have a public test server with 2+ ports please test it and let me know that it works

To figure out the Internet Radio streaming stuff, I first built it as a standalone windows app - if you have a use for that, it's here: [MiniRadio](https://github.com/read-0nly/MiniRadio)
