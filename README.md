# AssettoServer - NightTimePlugin Fork

This is a Fork of https://github.com/compujuckel/AssettoServer for the sole purpose of adding a NightTimePlugin. 
The Plugin is pretty simple. It will check if it is 22000 seconds after 12 AM (~ 6:06 AM) and set the time to 12 AM.

To enable it copy the NightTimePlugin folder into the plugins folder and enable it in the extra_cfg.yml like this:
EnablePlugins: [] -> EnablePlugins: [NightTimePlugin]

## About
AssettoServer is a custom game server for Assetto Corsa developed with freeroam in mind. It greatly improves upon the default game server by fixing various security issues and providing new features like AI traffic and dynamic weather.

Race/Quali sessions and lap times are not supported yet. Only use this if you want to run a practice-only freeroam server.

This is a fork of https://github.com/Niewiarowski/AssettoServer.

## Installation

### Windows
* Install the ASP.NET 6 Runtime (select "Hosting Bundle"): https://dotnet.microsoft.com/en-us/download/dotnet/6.0/runtime
* Download `assetto-server-win-x64.zip` from the [latest stable release](https://github.com/compujuckel/AssettoServer/releases/latest) and extract it whereever you want.  
  **DO NOT EXTRACT TO YOUR ASSETTO CORSA FOLDER.** AC and AssettoServer use different versions of the Steam SDK that will conflict with each other.
  If you still feel the need to extract to your AC folder delete the steam_api64.dll from the server first, but this will cause Steam Auth to be broken.

### Linux
* Follow the ASP.NET 6 Runtime installation instructions for your distro: https://docs.microsoft.com/en-us/dotnet/core/install/linux
* Download `assetto-server-linux-x64.tar.gz` from the [latest stable release](https://github.com/compujuckel/AssettoServer/releases/latest) and extract it whereever you want.

## Usage

### Through Content Manager (AssettoServer 0.0.44 or later)

* Open your AC server folder in Explorer, e.g. `C:\Program Files (x86)\Steam\steamapps\common\assettocorsa\server`
* Rename `acServer.exe` to something else
* Extract `assetto-server-win-x64.zip` into this directory
* Rename `AssettoServer.exe` to `acServer.exe`

Now you can run servers through CM just like you would with the original server. Keep in mind that not all features of the original server are supported yet, so some of the server settings in CM will either have no effect or the features will just not work.

### Dedicated server

The easiest way to get started is creating your server configuration with Content Manager.  
After that just click "Pack" to create an archive with all required configs and data files. Extract this archive into the server root folder.

## Features

Most features can be controlled via `extra_cfg.yml`. If this file does not exist it will be created at first server startup.

### Steam Ticket Validation

The default server implementation of Assetto Corsa does not use the Steam API to determine whether a connected players
account is actually the account they claim to be. This opens the door for SteamID spoofing, which means someone can
impersonate another player.

In this server the Steam Auth API is utilized, as documented
here: https://partner.steamgames.com/doc/features/auth

Since the player needs to get a Steam session ticket on client side that he has to transfer to the server upon joining,
a minimum CSP (Custom Shaders Patch) version of 0.1.75 or higher is required along with Content Manager v0.8.2297.38573 or higher for players to be able to join the server.

This feature must be enabled in `extra_cfg.yml`.

CSP can be found and downloaded here [https://acstuff.ru/patch/](https://acstuff.ru/patch/)  
CSP Discord: [https://discord.gg/KAbXE5Y](https://discord.gg/KAbXE5Y)

### Logging in as administrator via SteamID

It is possible to specify the SteamIDs of players that should be administrator on the server.

**Do not use this feature with Steam Auth disabled! Someone might be able to gain admin rights with SteamID spoofing.**

### AI Traffic

It is possible to load one or more AI splines to provide AI traffic. Place `fast_lane.ai` in the maps `ai/` folder and set `EnableAi` to `true` in `extra_cfg.yml`.  
The default AI settings have been tuned for Shutoko Revival Project, other maps will require different settings.

To allow AI to take a car slot you have to add a new parameter to the `entry_list.ini`, for example:
```ini
[CAR_0]
MODEL=ktyu_c8_lav_s1
SKIN=04_gunmetal_grey/ADAn
BALLAST=0
RESTRICTOR=0
AI=auto
```

Possible values for the `AI` parameter are
* `auto` - AI will take the slot when it is empty
* `fixed` - AI will always take the car slot. It won't be possible for players to join in this slot
* `none` - AI will never take the slot (default)

When using `AI=auto` slots it is highly recommended to specify a `MaxPlayerCount` in `extra_cfg.yml` to make sure there is always a minimum amount of AI cars available.


### Dynamic Weather

The server supports CSPs WeatherFX v1 which allows dynamic weather, smooth weather transitions and RainFX. CSP 0.1.76+ is required for this feature.

Two plugins are included that utilize dynamic weather:
* `LiveWeatherPlugin` for getting realtime weather from openweathermap.org
* `VotingWeatherPlugin` for letting players vote for weather changes

### Anti AFK system

Will kick players if they are not honking, braking, toggling headlights, moving steering wheel, using gas or sending
messages in chat. Can be adjusted by an admin by using the `/setafktime` command.

### Plugin Interface

There is an experimental plugin interface for adding functionality to the server. Take a look at one of the
included plugins to get started with developing your own plugin.

The API is still under development and might change in the future.

## Getting help
If you have trouble setting up a server feel free to visit the #server-troubleshooting channel on our [Discord](https://discord.gg/uXEXRcSkyz) (read #welcome if you can't see that channel).
Alternatively you can ask questions here: https://github.com/compujuckel/AssettoServer/discussions/categories/help

**Please don't use the Issue tracker for installation help or configuration questions. Also make sure to read this README first before asking questions that are already answered here!**

## Wiki
For more information on configuration, admin commands, etc. also check out the [Wiki](https://github.com/compujuckel/AssettoServer/wiki).

## License
AssettoServer is licensed under the GNU Affero General Public License v3.0, see [LICENSE](https://github.com/compujuckel/AssettoServer/blob/master/LICENSE) for more info.  
Additionally, you must preserve the legal notices and author attributions present in the server.

```
Copyright (C)  2022 Niewiarowski, compujuckel

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
```
