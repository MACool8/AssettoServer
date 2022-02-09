# AssettoServer - CMDs Plugin Collection Fork

This is a Fork of https://github.com/compujuckel/AssettoServer for the sole purpose of adding Plugins.

## NightTimePlugin 
The Plugin is pretty simple. It will check if it is 22000 seconds after 12 AM (~ 6:06 AM) and set the time to 12 AM.

Tested and worked with AsettoServer 0.0.45 to 0.0.47-pre7

## LightCommunicationPlugin
This Plugin will allow you to communicate 3 things to your nearest Player via flashing the lights near him:
Flash the light 2x: Say hi to the nearest player
Flash the light 3x: Say overtake me or let me overtake
Flash the light 4x: Something went wrong

A lot of the code was taken over from the raceplugin

Tested and worked with AsettoServer 0.0.47-pre5 to 0.0.47-pre7

## General
To enable a plugin, copy the downloaded folder of the plugins you want into the plugins folder in your server and enable it in the extra_cfg.yml like in this example:
EnablePlugins: [] -> EnablePlugins: [NightTimePlugin]  or   EnablePlugins: [NightTimePlugin, LightCommunicationPlugin]

## Misc
Those are my first plugins for the ACServer. Pretty much everything is hardcoded and can t be configured.


### Plugin Interface

There is an experimental plugin interface for adding functionality to the server. Take a look at one of the
included plugins to get started with developing your own plugin.

The API is still under development and might change in the future.

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
