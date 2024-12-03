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
https://medium.com/@niteshsinghal85/using-channels-for-asynchronous-queuing-in-c-ed96c51d4576
https://learn.microsoft.com/en-us/dotnet/core/docker/build-container?tabs=windows&pivots=dotnet-8-0
https://devblogs.microsoft.com/dotnet/improving-multiplatform-container-support/
https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-8.0&tabs=windows

### Open issues

* how to get audio on linux, naudio only supports windows
    * [**DONE**]look into how songrec does this
    * see if can do this by extending naudio 
        * https://stackoverflow.com/questions/13793514/monodevelop-naudio-ubuntu-linux-tells-me-winmm-dll-not-found
    * [**THIS**] Alternative?: https://github.com/ManagedBass/ManagedBass
        * ARM64 binaries for rpi https://www.un4seen.com/forum/?topic=13804 don't use softfp version, use hardfp, maybe aarch64
* [**DONE**] Convert console app into library for website to consume
* ~~learn angular enough to get basic page working~~ Using Blazor instead
* how to run the site on raspi
    * run via docker 
        * https://hub.docker.com/_/microsoft-dotnet
        * https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/building-net-docker-images?view=aspnetcore-7.0
        
### TODO

* More Docker support
  * Docker Compose template, with var for setting default audio device, mapping audio device to container, icecast auth creds
  * https://docs.linuxserver.io/general/docker-compose/
  * https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-8.0#command-line for changing appsettings.json values from command line,
    combine with Dockerfile ENTRYPOINT and CMD https://stackoverflow.com/questions/54654987/how-to-pass-command-line-arguments-to-a-dotnet-dll-in-a-docker-image-at-run-time
  
* Improve the `SongIdService`
  * [**DONE**] Detect dead air and update result
  * Store list of last `n` results
  * [**DONE**]Somehow signal listeners that a new song occurred
    * ~~Use System.Threading.Channels?~~ Using Blazor and SignalR
  
* Now Playing page improvements
  * Display list of past results
  * ~~Add refresh link~~
  * [**DONE**] Eventually add auto-refresh when now playing changes
  * GET request to icecast server to update metadata 
    * [Icecast docs](https://icecast.org/docs/icecast-2.0.1/admin-interface.html)
    * http://recordpi.local:8000//admin/metadata?mount=/mystream&mode=updinfo&song=ACDC+Back+In+Black
    * Basic Authentication header needed

* Figure out how to configure icecast secrets outside of development environment via secrets.json
  * https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-8.0&tabs=windows

### Docker Info

Info on how to turn this is turned into a docker image, and how to update, test etc using docker

[Gitea Contain Registry](https://docs.gitea.com/usage/packages/container)
[Building Dockerfile from cmd line](https://stackoverflow.com/questions/66146088/docker-gets-error-failed-to-compute-cache-key-not-found-runs-fine-in-visual)
[Gitea 'unexpected status from PUT request 404 not found' error during PUSH](https://github.com/go-gitea/gitea/issues/31861) add `--provenance=false` to build command

`docker login gitea.example.com`

`docker build -f "C:\Users\acejo\repos\song-id-site\song-id-site\Dockerfile" --force-rm -t songidsite:dev --target base  --build-arg "BUILD_CONFIGURATION=Debug" --label "com.microsoft.created-by=visual-studio" --label "com.microsoft.visual-studio.project-name=song-id-site" "C:\Users\acejo\repos\song-id-site"`
`docker login`
`docker tag songidsite:dev acejordin/songidsite:dev`
`docker push/pull acejordin/songidsite:dev`

So unfortunately DarkIce and song-id-site cannot share the ALSA input on the raspberry pi, only one can use it at a time. One solution is a 'mixer' application [pulseaudio](https://www.freedesktop.org/wiki/Software/PulseAudio/). 
But it's a pain to set up. What I did to get it sorta working for later cleanup

Dockerfile installs pulseaudio via apt-get, and others (necessary?)

this link goes over how to get sound working in a container [Container sound: ALSA or Pulseaudio](https://github.com/mviereck/x11docker/wiki/Container-sound:-ALSA-or-Pulseaudio).
I followed the "Pulseaudio with shared socket" section, and converted the docker run command to the docker compose file, only tricky bit was the user param, which is
defined `--user $(id -u):$(id -g)`, but in compose file is `user: "1000:1000"`. (Is priviledged mode still necessary?)

The other bit was getting DarkIce working with the pulse audio server. Currently it only starts via `~ $ darkice -c darkice.cfg` (running as local user), it normally runs as root
but that results in 

`ALSA lib pulse.c:242:(pulse_connect) PulseAudio: Unable to connect: Connection refused`

This has to do with how PulseAudio want to work by default: [The Perfect Setup](https://www.freedesktop.org/wiki/Software/PulseAudio/Documentation/User/PerfectSetup/)
Tried an experiment of adding `root` user to the `audio` group that PulseAudio uses, but that wasn't enough to get DarkIce to run like usual. But the darkice init.d file
appears to run DarkIce as `nobody` and group `nogroup`. Need to investigate more.

[Pipewire in a container](https://stackoverflow.com/a/75775875/454437)

[ALSA Exposed](https://rendaw.gitlab.io/blog/2125f09a85f2.html#alsa-exposed)

[ALSA Sharing](https://alsa.opensrc.org/AlsaSharing#The_card_does_not_support_hardware_mixing.2C_but_all_processes_accessing_it_run_applications_that_use_the_ALSA_library)

[ALSA Project](https://www.alsa-project.org/alsa-doc/alsa-lib/pcm_plugins.html)

[Asoundrc](https://www.alsa-project.org/main/index.php/Asoundrc#dsnoop)

NEW PLAN:

Get DarkIce/IceCast working in same docker container as song-id-site. Alsa should share input audio within a docker container

Notes for putting DarkIce/IceCast into a container:

- Build for arm64 platform
- `docker buildx build --tag git.paulson.network/acejordin/recordpi:dev . --platform=linux/arm64 --provenance=false`
- Push to repo
- `docker push git.paulson.network/acejordin/recordpi:dev1`
- From SSH on Pi
- `sudo docker pull git.paulson.network/acejordin/recordpi:dev`
- `sudo docker run --privileged -p 8888:8000 -it git.paulson.network/acejordin/recordpi:dev`



TODO:
- figure out darkice, run as local user? as root? pros/cons?
- figure out exactly what needs to be installed for pulseaudio support
  - installed on raspi and in docker. was it already installed on rpi? what apt-get's are exactly needed for dockerfile?
  - research the container sound approach to understand it better
  - research the user `1000:1000` in the docker compose to understand better


### Install Steps (WORK IN PROGRESS)
1. Install Docker on Raspberry Pi
	* https://docs.docker.com/engine/install/debian/ (For 64-bit Raspberry Pi OS)
2. Download the Docker Image TODO
1. Set up icecast secrets TODO
1. Modify Docker Compose template variables TODO
1. Use Docker Compose template to run the container TODO
1. Browse to site TODO
