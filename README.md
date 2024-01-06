# Ghost AssettoServer Proof-of-Concept

## About
Ghost AssettoServer is a POC (Proof-of-Concept) implementation for a custom game server for Assetto Corsa developed with training/filling empty servers in mind. 
It implements the feature to play recordings of players back on the server. Those recordings are called ghosts (based on the ghosts known from offline AC and Gran Turismo Ghosts) 
Besides the playback, it should allow for more customization of the ghost playbacks (e.g. looping, proximity aware respawn, multiple ghosts playing synchronized and more)

The ghost server implementation is accompanied by the GhostManagerPlugin. This Plugin allows you to record, debug and permanently save settings. This Plugin is not mandatory if you have already recorded and set up all your ghosts.
The documentation for this plugin can be found (here)[GhostManagerPlugin/README.md]

This is a fork of https://github.com/compujuckel/AssettoServer which is a fork of https://github.com/Niewiarowski/AssettoServer.

As I found out that the main assumption behind the implementation of ghosts is flawed (explained later on), this code has become a POC with the intention to allow anyone to test this POC, start a discussion for features the community would love to see and use the new information for creating a full-fledged implementation with the community wished and feasible features in mind.
Therefore if you have any ideas you want to be implemented or want to influence in which direction the project should go, visit [this vote](https://www.tricider.com/brainstorming/3EZ8Cjq5FtJ) and input your ideas or upvote the ones you like.

## Documentation
The documentation of the ghost features implemented in this Proof-of-Concept can be mainly found here in this readme or sub-linked files.
But as this implementation relies mostly on compujuckel work, I would suggest you to also get yourself comfortable with all this documentation: [assettoserver.org](https://assettoserver.org/docs/intro)

## Getting help
Do not expect compujuckel or anyone from the AssettoServer Development discord to help you out with problems for the ghost servers. The ghost server implementation was created without the involvement of anyone mentioned.
The setting-up guide will require knowledge of setting up an AI Server. I highly suggest getting yourself familiar with setting up an AI server with plugins with the original fork from compujuckel. 
If you fail at setting up an AI server you will 100% fail at setting up a ghost server.
For now, this implementation will be provided as-is and extensive support can not be expected. 

## General
Ghosts are essentially recordings of players, capturing the state of the recorded person at each time point. This enables playback, which can be configured in two primary modes: Loop Mode (standard) and Proximity Mode.

### Loop Mode
In Loop Mode, ghosts follow a continuous playback loop. They seamlessly jump back to the loop sequence number once they reach the end sequence number, creating an endlessly repeatable sequence. This mode is ideal for scenarios where players can chase ghosts indefinitely. 

### Proximity Mode
Proximity Mode introduces an additional layer of interaction. Like Loop Mode, ghosts jump back to the loop sequence number upon reaching the end, but they also respond to the player's proximity. If the player moves out of the defined proximity, the ghost will reset to the start sequence number.

## Setting-up-guide
Quickquide: 
- Replace the Original AI Server executable with the downloaded one.
- Copy the GhostManagerPlugin into the plugin folder
- Adapt the following settings:
**server_cfg.ini:** 
Use only practice Sessions, 
SET: `CLIENT_SEND_INTERVAL_HZ=32`
**extra_cfg.yaml:**
SET: `EnableAi: true`
ADD: `EnableGhosts: true`
SET: `EnablePlugins: [GhostManagerPlugin, ...]`
SET: `PlayerRadiusMeters: <amount of meters after which the ghosts goes out of proximity, the standard 200 works for most people>`
**cfg/data_track_params.ini**
If the track you want to use is not already in there, copy an entry and add your track to this file. If not done a `No track params found for ...`-error will stop your server from starting up. 
**entry_list.ini:** (add for each car you want to be a ghost, have at least one ghost in your setup)
ADD: `AI=fixed` (AI=auto is not supported yet and probably crashes if a player joins in as the ghost, admins also can't join as ghosts/bots)
<optional>If you already have a Ghost file you want to load in (during the server startup) then add this, else just start the server without these settings below and record or dynamically load in ghosts:
ADD: `GHOST=<ghost file name>`
If you want to add multiple Ghosts with the same ghost file:
ADD: `GHOST_OFFSET=<how far should the ghosts start off>`
If you want to cluster:
ADD: `GHOST_Cluster=<Clustername>`

In-depth Guide: To-do

## Ghost Management Guide (including recording/configuring/dynamic commands)

(Ghost Management Guide)[GhostManagment.md]

## Demo-Servers

2x Proximity Mode Ghosts for each direction
(01 Drift Fruitsline with DeathWishGarage cars)[https://acstuff.ru/s/q:race/online/join?ip=195.90.201.187&httpPort=8091]
(02 Drift Fruitsline with DeathWishGarage cars)[https://acstuff.ru/s/q:race/online/join?ip=195.90.201.187&httpPort=8101]

Proximity Mode Ghost
(01 Drift Euphoria with WDTS cars)[https://acstuff.ru/s/q:race/online/join?ip=195.90.201.187&httpPort=8093]
(02 Drift Euphoria with WDTS cars)[https://acstuff.ru/s/q:race/online/join?ip=195.90.201.187&httpPort=8103]

2x Looping Mode Ghosts
(01 Drift Sunrise with WDTS cars)[https://acstuff.ru/s/q:race/online/join?ip=195.90.201.187&httpPort=8094]
(02 Drift Sunrise with WDTS cars)[https://acstuff.ru/s/q:race/online/join?ip=195.90.201.187&httpPort=8104]

10x Proximity Mode Ghosts clustered to one unit
(Cruise Shuto PTB with Paris Cars)[https://acstuff.ru/s/q:race/online/join?ip=195.90.201.187&httpPort=8092]

## Performance
During development I kept performance in mind so that I could run this server on low-end hardware later on.
I didn't perform scientific measurements I tested running the 7 demo servers on a Dell Wyse 3040 with an Atom x5-Z8350 CPU which is basically a x64 CPU but with the performance of a Raspberry PI 4.
And it ran fine (given I used only 1-2 people at a time on the servers) the whole system stayed under 20% as for the whole CPU utilization. 
And on a "more proper" server (where the current demos are running, a VPS with 4 shared cores on an EPYC 7502 CPU) I never really broke the 6% mark yet.
Nonetheless, I will monitor the server over the next few days as the Demos go public.

## The Flaw that makes this a POC (Proof-of-Concept)
During the start of the development I assumed that the server update rate (which is also being sent out to the client upon connecting to the server) will be roughly abode by the server and the client.
Because of this I simply recorded incoming messages at the speed the client used and played them back with the servers update rate.
I found out late into development (two days before this first release) this assumption is not true and the client will use roughly a 4% higher update rate to send out its position data.
As the Server played back the update with its 4% slower update rate (compared to the client), this led to the ghosts being 4% slower than they should be.
For now, I used a hacky way to lock the send-out update rate to the client to 32 HZ and lock the actual used update rate of the server to 33.333... HZ, which syncs both the server and the client to the same update rate.
This seems to work for now, but compromises in so many other things that I think should not be used going further.
The current implementation is highly interlaced with the AI implementation of compujuckel, which is bound to the given update rate of the server. 
This means that a proper implementation would require a new logic to follow, a lot of code would need to be reworked and also the format of the ghost files would need to be changed.
Therefore I decided to make the current implementation a POC, as the proper implementation would have a vastly different way of working, which would make bugfixes in this POC-Version useless in the long run.

## State of the Project and Future
Initially, I thought that I could release this as an early alpha build and fix it step by step to get it into a stable, well-written piece of code. But with the flaw I have to rewrite the whole thing anyway which means I will release it as POC with the premise as-is. 
As long bugs don't fully hinder the community of testing, I would rather focus on the full rewrite of the implementation. But as this flawed implementation was already a full year in the making, the full rewrite could also take a while.
I can't guarantee that the bots are 100% as fast as the person who recorded them. 
Most of the testing happened on a Linux server (mostly with docker). I successfully ran it on Windows and added some bug fixes there, but I can't be sure I didn't miss anything there.

## License
AssettoServer is licensed under the GNU Affero General Public License v3.0, see [LICENSE](https://github.com/compujuckel/AssettoServer/blob/master/LICENSE) for more info.  
Additionally, you must preserve the legal notices and author attributions present in the server.

```
Copyright (C)  2024 Niewiarowski, compujuckel, MACool8 aka CMD

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.


Additional permission under GNU AGPL version 3 section 7

If you modify this Program, or any covered work, by linking or combining it 
with the Steamworks SDK by Valve Corporation, containing parts covered by the
terms of the Steamworks SDK License, the licensors of this Program grant you
additional permission to convey the resulting work.

Additional permission under GNU AGPL version 3 section 7

If you modify this Program, or any covered work, by linking or combining it 
with plugins published on https://www.patreon.com/assettoserver, the licensors
of this Program grant you additional permission to convey the resulting work.
```
