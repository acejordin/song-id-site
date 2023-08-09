# song-id-site

Goal here is to develop a small website with one page, running on a raspi that is also hooked up via USB to a turntable. The one page when hit will listen in to the turntable audio and make a call to the Shazam API, then return the track, artist and album art in the page.

This raspi is also running darkice/icecast to stream the audio, which Sonos hooks into to play over Sonos. But Sonos cannot show audio metadata.

The purpose here is to provide a way to show current audio info via webpage, since Sonos does not support metadata for custom streams.

### Shazam support is owed to:

https://github.com/marin-m/SongRec
which showed that it is actually possible to use the Shazam API directly without ShazamKit

https://github.com/AlekseyMartynov/shazam-for-real/
which I used to actually get it working in .NET, with some tweaks 

### Ongoing research links:

https://stackoverflow.com/questions/13793514/monodevelop-naudio-ubuntu-linux-tells-me-winmm-dll-not-found

### Open issues

* how to get audio on linux, naudio only supports windows
    * look into how songrec does this
    * see if can do this by extending naudio 
        * https://stackoverflow.com/questions/13793514/monodevelop-naudio-ubuntu-linux-tells-me-winmm-dll-not-found
* convert console app into library for website to consume
* learn angular enough to get basic page working
* how to run the site on raspi
    * run via docker? 
        * https://hub.docker.com/_/microsoft-dotnet
        * https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/building-net-docker-images?view=aspnetcore-7.0
    
