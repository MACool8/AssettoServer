# GhostManagerPlugin

This plugin allows you to record and manage ghost and meta files in combination with a server implementing ghosts

## General

Ghost files contain the car's state (position, speed, tire angle, etc.) at every point of time recorded. Their index is called sequence number. They will get played back with a server that loads in the ghost files.

Meta files are not mandatory, but fulfill the purpose of saving the start, loop and end sequence numbers and other information, to be used once the server restarts. If no meta file is found with a ghost file, the server will use default values.

The Parameters <PartialPlayername> are meant to be filled with an unique part of a PlayerName. E.g. Dora is enough if the full name DoraTheExplorer is searched, as long no one else on this Server has Dora in their Name.

The Parameters <GhostID> are the SessionID of the Ghost. Ghosts should have the SessionID in their name it's either inside the first square brackets or the only number in the name. They can also be listed with the /gh_identify command.

This Plugin only works with a Server implementation supporting Ghosts.

All Commands require Admin privileges, except Commands with [noAdmin] in the description.

## Commands
The following commands can be called.

`/gh_help`

[noAdmin]Will print this.

`/gh_rec <PartialPlayername>`

Allows to start a recording for the selected player. (Currently up to 30 mins)

`/gh_stoprec <PartialPlayername>`

Stops the recording for this player, saves it to a ghost and a metafile and tells you what name it has been given.

`/gh_load <GhostID> <part of the filename>`

Loads in a ghostfile out of the ghostfile folder. Only part of the filename can be given as long it's unique enough.

Filename 'Ghost_PlayerABC-002-2022_06_06-09_29.ghost' can be loaded with '/gh_load PlayerABC-002' as long no other file in the ghost folder has PlayerABC-002 in their filename.

`/gh_show_files`

Shows all available Ghost files in the ghost folder which can be loaded.

`/gh_time <GhostID>`

[noAdmin]Shows the current sequence number at which the Ghost is at.

`/gh_debug <GhostID> [start/loop/end/transition/<sequence number>]`

Teleports the Ghost to the selected value.

start, loop, end and <sequence number> (a sequence number you can set e.g. 133) are fixed states which you can view.

transition will tp the ghost car 3 seconds before the transition between end and loop happens so you can view the transition and tweak it.

Example usages for GhostID 8:

`/gh_debug 8 loop`

`/gh_debug 8 1300`

`/gh_play <GhostID>`

If the Ghost is paused, it will start moving again.

`/gh_pause <GhostID>`

If the Ghost is playing, it gets paused.

`/gh_step <GhostID> <steps>`

Will make the Ghost forward (positive <steps>) or reverse (negative <steps>) the given amount of steps.

`/gh_set <GhostID> [start/loop/end/offset]`

Sets the values of the three variables to be used for this session. (To make the start, loop and end sequence numbers persistent to their correlating Ghost file, call /gh_savemeta <GhostID>)

`/gh_loopmode <GhostID>`

Enables Loopmode, in which the Ghost doesn't try to jump back to a nearby player if the player gets out of proximity range.

`/gh_savemeta <GhostID>`

Takes the current metadata (start/loop/end/loopmode) and saves it to the meta file.

This means changes to the start, loop and end values will not be persistent until you call this command.

Also, cluster and offset values are NOT saved with this command and can only be adjusted permanently in the entry_list.ini

If you use multiple Ghosts with the same Ghost file, be aware that you save the meta date of one ghost to all and upon the next reboot all ghosts will use the new metadata.

`/gh_hide <GhostID>`

Toggles whether the Ghost hides under the map.

`/gh_setcluster <GhostID> <ClusterName>`

Sets the cluster of a ghost. Ghosts in the same cluster will only tp back to the start (+offset if exists) in the proximity mode if all cars in the cluster are out of range for any player.

`/gh_sync`

Respawns all ghosts at their start sequence number (+ offset if used) and by this resets all positions. Useful for ghosts that are clustered or have offsets
