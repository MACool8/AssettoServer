# Ghost Management Guide

Welcome to the GhostManagerPlugin tutorial! This guide will walk you through the step-by-step process of recording a ghost using the commands provided and on the side also explains all the commands and how to understand the logic behind ghosts.

Use the `/gh_help` command ingame to get a short explanation of all commands in one go.

## Prerequisites

1. **Server Implementation**: Ensure that you are using a Ghost Assetto Corsa server and that you are also using the GhostManagerPlugin

2. **Admin Privileges**: You need admin privileges on your server to execute most of the commands. Make sure you have admin access on your server.

## Step 0: Understanding Ghost and Meta Files

- **Ghost Files**: These files contain the car's state at specific time points, each associated with a consecutive `sequence number` for easy reference. The `sequence number` will be the main reference point to adjust your ghosts. Between each time point/sequence number should be roughly 30ms. 

- **Meta Files**: While not mandatory, meta files save start, loop, and end sequence numbers, along with other meta information, for use when the ghosts get loaded in.

## Step 1: Starting a Recording

Use the following command to initiate a recording for a specific player:

```
/gh_rec <PartialPlayername>
```

Replace `<PartialPlayername>` with a unique part of the player's name (e.g. `Dora` for `DoraTheExplorer`).

## Step 2: Recording Tips for Effective Ghosts

To ensure effective ghost recordings, consider the following:

Before starting to record, plan how you want your ghost to behave.

1. **Indicate Ghost Presence:** Ghosts appear identical to regular players. To distinguish them during playback, activate hazard lights while recording.

2. **Looping Mode Preparation:** For a seamless loop, choose a circuit spot where you can consistently maintain position, speed, direction, and angle (for drifting) over most laps. This way you can later on choose loop and end sequence numbers which create a more seamless jump during the transition/loop point.

3. **Proximity Mode Preparation:** If planning to run a ghost in proximity mode, choose a starting position near the player pits. However, ensure that the starting position is outside of pits to avoid unintended collisions if the "no-contact pits" option is not enabled on the track.

4. **Ping/Packetloss to the Server:** The recordings are just recording your incoming messages. If the server doesn't get your message, it can not record it. Try to use servers which are close to you or even better let the server run on the pc you are playing. The implmentation has it's way of dealing with lost packages, but it can only do so much.

## Step 3: Stopping the Recording

To stop the recording for the player and save a ghost and meta file, use the command:

```
/gh_stoprec <PartialPlayername>
```

The server will provide you with the assigned filename for the recorded ghost.

(pro tip: if you first enter /gh_stoprec player and then enter /gh_rec player you are only two arrow button up-presses away from easily stopping the recording once you are finished)

## Step 4: Loading the Ghost File

Load the recorded ghost file into the server using the following command:

```
/gh_load <GhostID> <part of the filename>
```

Replace `<GhostID>` with the SessionID of the ghost (you can find it in the ghost's name or use `/gh_identify`) and `<part of the filename>` with a unique part of the filename.

You can use a partial filename as long as it remains unique. For example, if you have a file named 'Ghost_Dora-002-2022_06_06-09_29.ghost' you can load it with '/gh_load 7 Dora-002,' as long as no other file in the ghost folder shares the same 'Dora-002' in its filename.

## Step 5: Playback and Debugging

Now that you've loaded the ghost, you can control its playback and debug their start/loop/end sequence numbers using various commands:

- Start Playback:
  ```
  /gh_play <GhostID>
  ```

- Pause Playback:
  ```
  /gh_pause <GhostID>
  ```

- Move Ghost Forward or Backward:
  ```
  /gh_step <GhostID> <steps>
  ```

- Teleport Ghost to Specific State:
  ```
  /gh_debug <GhostID> [start/loop/end/transition/<sequence number>]
  ```

- Show the current sequence number of the ghost:
  ```
  /gh_time <GhostID>
  ```

## Step 6: Customizing Ghost Settings

Adjust the ghost's behavior using the following commands:

- Set the values of the variables start, loop, end and offset for the current session:
  ```
  /gh_set <GhostID> [start/loop/end/offset] <sequence number>
  ```
  To make these values persistent for the corresponding ghost file, use the `/gh_savemeta <GhostID>` command.

- Switch between Loop Mode (true) and Proximity Mode (false) for the current session:
  ```
  /gh_loopmode <GhostID> <true/false>
  ```

During debugging/setting of values, it's best to have the loopmode on, as proximity mode will probably respawn the ghost once you type something.
(Pro Tipp: Proximity Mode doesn't take AFK players into account and if you are the only one on the server and you go AFK proximity mode ghosts don't respawn until you go out of being AFK, for example by sending a message or starting to drive)

- Save the Meta Data (start/loop/end/mode):
  ```
  /gh_savemeta <GhostID>
  ```
This excludes the offset and clustername (explained later)

- Toggle Ghost Visibility:
  ```
  /gh_hide <GhostID>
  ```

## Optional Step 7: Clustering Ghosts

Let's say you want to have multiple Ghosts that play together as a unit (for example you recorded a whole race or you want multiple clones of yourselves driving behind each other), then it would be rather bad if only the front ones respawn back if you felt behind a bit and you use the proximity mode. 
That would mean only half the other ghosts (who still are in your proximity) keep on playing, while the others are back at the start. 

To avoid this you can give a group of ghosts the same ClusterName. If ghosts are in the same cluster they will only jump back to the start if no ghosts of the cluster are in proximity of any player. And once they jump back they all are back in sync.

- Set Cluster for Ghost:
  ```
  /gh_setcluster <GhostID> <ClusterName>
  ```

- Also here the offset value gets useful:
  ```
  /gh_set <GhostID> offset <amount of sequence number to offset>
  ```

If you use the same ghost file for multiple ghosts, the only way to make them persistently non-overlapping is by using offsets, as the metafile will always be the same for each ghost while the offset gets set/edited outside of those files.
Both the cluster and the offset are not saved in the metafile (=> /gh_savemeta doesn't save these parameters).
They can be set dynamically while the server is running with these commands (best for testing out) or persistently in the entry_list.ini-file (recommended for long-running servers).

To persistently set the cluster and/or the offset the following lines have to be added to each ghost car inside the entry_list.ini, which need them:
  ```
  GHOST_OFFSET=<how far should the ghosts start off>
  GHOST_Cluster=<Clustername>
  ```

If needed, you can respawn all ghosts at their start positions to reset/sync their positions:

  ```
  /gh_sync
  ```

Congratulations! You've successfully recorded and managed a ghost using the GhostManagerPlugin in Assetto Corsa. Experiment with different settings and share your experiences with the community!

Also a tipp on where to find/backup the ghost files:
Ghost files are saved for each track seperatly. You can find them in `content -> tracks -> csp -> <track name> -> ai -> ghosts`.


## To-do: In-depth tutorial about the usage of start, loop, end and offset sequence numbers.

To-do
