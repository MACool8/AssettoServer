using AssettoServer.Commands;
using AssettoServer.Network.Tcp;
using AssettoServer.Server;
using AssettoServer.Server.Ai;
using AssettoServer.Shared.Model;
using AssettoServer.Shared.Network.Packets.Outgoing;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Qmmands;
using Serilog;
using System.IO;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using YamlDotNet.Core.Tokens;
using GhostState = AssettoServer.Shared.Network.Packets.Incoming.PositionUpdateIn;

namespace GhostManagerPlugin
{
    public class GhostManagerCommandModule : ACModuleBase
    {
        /*
        [Command("gh_tpme")]
        public void gh_tpme(int GhostSessionID, int Position)
        {
            EntryCar target = FindGhost(GhostSessionID);
            if (target == null)
                return;

            if(Position < target.GhostStart || Position > target.GhostEnd)
            {
                Reply($"Given Position ({Position}) out of Range ({target.GhostStart}:{target.GhostEnd})");
            }

            GhostState UpdateTo = target.GhostLine[Position];




            PositionUpdateOut positionUpdateOut = new PositionUpdateOut(Context.Client.SessionId,
        Context.Client.EntryCar.Status.PakSequenceId,
        (uint)(Context.Client.EntryCar.Status.Timestamp - Context.Client.EntryCar.TimeOffset),
        Context.Client.EntryCar.Ping,
        UpdateTo.Position,
        UpdateTo.Rotation,
        UpdateTo.Velocity,
        UpdateTo.TyreAngularSpeedFL,
        UpdateTo.TyreAngularSpeedFR,
        UpdateTo.TyreAngularSpeedRL,
        UpdateTo.TyreAngularSpeedRR,
        UpdateTo.SteerAngle,
        UpdateTo.WheelAngle,
        UpdateTo.EngineRpm,
        UpdateTo.Gear,
        UpdateTo.StatusFlag,
        UpdateTo.PerformanceDelta,
        UpdateTo.Gas);

            Context.Client.SendPacket(positionUpdateOut);
            Reply($"You just got TPed ... or probably not ...\n");
        }*/

        [Command("gh_identify")]
        public void IdentifyGhosts()
        {
            int GhostsAvailable = 0;
            
            foreach (EntryCar x in GhostManagerPlugin._entryCarManager.EntryCars)
            {
                if (x.AiControlled)
                {
                    Reply($"[{x.SessionId}]Name:'{x.AiName}',Model:'{x.Model}',Skin:'{x.Skin}',Ghost Name:'{x.GhostName}',Loaded Ghost file:'{Path.GetFileName(x.ReadInGhostFile)}.ghost'\n");
                    GhostsAvailable++;
                }
            }
            Reply($"{GhostsAvailable} Ghosts are available:\n");        }

        [Command("gh_rec")]
        public void StartRecord(string PartialPlayerName)
        {
            if (!Context.Client.IsAdministrator)
            {
                Reply("Command requires admin privileges.");
                Log.Information($"[GhostManagerPlugin]{Context.Client.Name} tried using the Command '{Context.Command.Name}' without enough privileges.");
                return;
            }

            EntryCar target = FindPlayer(PartialPlayerName);
            if (target == null)
                return;

            if (target.RecordingStarted)
            {
                Reply("You can not start a recording twice. Especialy not a GhostRecording!");
                Log.Debug($"[GhostManagerPlugin] {Context.Client.Name} tried to start a recording for {target.Client.Name} which was already running.");
                return;
            }

            target.StartRecording();

            Reply($"Started recording {target.Client.Name}");

            Log.Debug($"[GhostManagerPlugin] {Context.Client.Name} started recording {target.Client.Name}.");



        }

        [Command("gh_stoprec")]
        public void StopRecord(string PartialPlayerName)
        {
            if (!Context.Client.IsAdministrator)
            {
                Reply("Command requires admin privileges.");
                Log.Information($"[GhostManagerPlugin]{Context.Client.Name} tried using the Command '{Context.Command.Name}' without enough privileges.");
                return;
            }


            EntryCar target = FindPlayer(PartialPlayerName);
            if (target == null)
                return;


            if (!target.RecordingStarted)
            {
                Reply("You can not stop something, you haven't started. Especialy not a Ghost recording!");
                Log.Debug($"[GhostManagerPlugin] {Context.Client.Name} tried to stop a recording which wasn't even started.");
                return;
            }

            string SaveUserName = MakeValidFileName(target.Client.Name);

            // Find out what is the latest recording counter for the filename
            List<string> files = Directory.EnumerateFiles(GhostManagerPlugin.GhostBasePath).ToList<string>();
            files = files.FindAll(x => x.Contains(SaveUserName) && x.EndsWith(".ghost"));
            int Maximum = -1;
            foreach (string file in files)
            {
                string[] parts = file.Split('-');
                if (parts.Length < 3)
                    continue;
                int number;

                if (int.TryParse(parts[1], out number))
                {
                    if (number > Maximum)
                        Maximum = number;
                }
            }
            Maximum++;

            // the fulltrack name will most of the times (but not always) include csp/xxxx/../trackname before a trackname with the linux pathseperators even on windows ...
            // thats why we choose ourself what os seperator we are looking for in the next step
            string FullTrackPath = GhostManagerPlugin._configuration.FullTrackName;
            int lastSeperatorPosition = Math.Max(FullTrackPath.LastIndexOf('/'), FullTrackPath.LastIndexOf('\\'));

            string TrackName = MakeValidFileName(FullTrackPath.Substring(lastSeperatorPosition + 1));

            // Create File
            string FileName = SaveUserName + "-" + Maximum.ToString("000") + "-" + TrackName + ".ghost";

            target.StopRecordingAndSave(Path.Join(GhostManagerPlugin.GhostBasePath, FileName), target.Client.Name);
            
            long RecordCount = new System.IO.FileInfo(Path.Join(GhostManagerPlugin.GhostBasePath, FileName)).Length / 61;

            Reply($"Stopped recording and saved file to {Path.Join(GhostManagerPlugin.GhostBasePath, FileName)}");
            Log.Information($"[GhostManagerPlugin] '{Context.Client.Name}' stopped a recording for '{target.Client.Name}' with the length of {RecordCount} Records. Saved File: {Path.Join(GhostManagerPlugin.GhostBasePath, FileName)}");
        }

        [Command("gh_savemeta")]
        public void GhostSaveMeta(int GhostSessionID)
        {
            if (!Context.Client.IsAdministrator)
            {
                Reply("Command requires admin privileges.");
                Log.Information($"[GhostManagerPlugin]{Context.Client.Name} tried using the Command '{Context.Command.Name}' without enough privileges.");
                return;
            }

            EntryCar target = FindGhost(GhostSessionID);
            if (target == null)
                return;

            string MetaDataJson = target.SourceMetaData(target.GhostName);
            Log.Information($"[GhostManagerPlugin]{Context.Client.Name} saves the meta data for '{GhostSessionID}' to '{Path.Join(GhostManagerPlugin.GhostBasePath, target.ReadInGhostFile + ".meta")}'.");
            target.SaveCurrentRecordingMetaData(Path.Join(GhostManagerPlugin.GhostBasePath, target.ReadInGhostFile + ".meta"), MetaDataJson);
            Reply($"Saved Metadata for Ghost {GhostSessionID} to '{Path.Join(GhostManagerPlugin.GhostBasePath, target.ReadInGhostFile + ".meta")}'");
        }

        [Command("gh_debug")]
        public void Ghostdebugging(int GhostSessionID, string Type)
        {
            if (!Context.Client.IsAdministrator)
            {
                Reply("Command requires admin privileges.");
                Log.Information($"[GhostManagerPlugin]{Context.Client.Name} tried using the Command '{Context.Command.Name}' without enough privileges.");
                return;
            }

            EntryCar target = FindGhost(GhostSessionID);
            if (target == null)
                return;

            Reply($"Start:{target.GhostStart}, Loop:{target.GhostLoop}, End:{target.GhostEnd}, Loopmode:{target.GhostLoopMode}");

            int SequenceNumber;

            if (Type.ToLower() == "start")
            {
                target.GhostPlaying = false;
                foreach (AssettoServer.Server.Ai.AiState y in target._aiStates)
                    y.TeleportGhostTo(target.GhostStart);
            }
            else if (Type.ToLower() == "loop")
            {
                target.GhostPlaying = false;
                foreach (AssettoServer.Server.Ai.AiState y in target._aiStates)
                    y.TeleportGhostTo(target.GhostLoop);
            }
            else if (Type.ToLower() == "end")
            {
                target.GhostPlaying = false;
                foreach (AssettoServer.Server.Ai.AiState y in target._aiStates)
                    y.TeleportGhostTo(target.GhostEnd);
            }
            else if (Type.ToLower() == "transition")
            {
                target.GhostPlaying = true;
                int TicksBeforeTransition = 92;
                if (target.GhostEnd - 92 < target.GhostLoop)
                    TicksBeforeTransition = target.GhostEnd - target.GhostLoop;
                foreach (AssettoServer.Server.Ai.AiState y in target._aiStates)
                    y.TeleportGhostTo(target.GhostEnd - 96);
            }
            else if (int.TryParse(Type, out SequenceNumber))
            {
                if (SequenceNumber < -1 || SequenceNumber >= target.GhostLine.Count)
                {
                    Reply($"Give time number needs to be inbetweeen 0 and {target.GhostLine.Count}");
                    return;
                }
                foreach (AssettoServer.Server.Ai.AiState y in target._aiStates)
                    y.TeleportGhostTo(SequenceNumber);

            }
            else
            {
                Reply("Can't identify Command please use /gh_debug [start/loop/end/transition/suspe/<Time number>]");
            }

        }

        [Command("gh_load")]
        public void GhostLoading(int GhostSessionID, string PartialFileName)
        {
            if (!Context.Client.IsAdministrator)
            {
                Reply("Command requires admin privileges.");
                Log.Information($"[GhostManagerPlugin]{Context.Client.Name} tried using the Command '{Context.Command.Name}' without enough privileges.");
                return;
            }

            EntryCar target = FindGhost(GhostSessionID);
            if (target == null)
                return;

            List<string> FilesFittingToDiscreption = new List<string>();

            foreach (string File in Directory.EnumerateFiles(GhostManagerPlugin.GhostBasePath))
            {
                if (!File.EndsWith(".ghost"))
                    continue;

                if (!File.Contains(PartialFileName))
                    continue;

                FilesFittingToDiscreption.Add(File);

            }

            if (FilesFittingToDiscreption.Count == 0)
            {
                Reply("No Files found fitting to this description.");
                Reply("Usage /gh_load <ghost id> <Part of the Filename>. E.g. /gh_load 17 playerABC_01");
                return;
            }
            else if (FilesFittingToDiscreption.Count > 1)
            {
                Reply("Too many Files found fitting to this description. Be more specific to only match one file.");
                Reply("Usage /gh_load <ghost id> <Part of the Filename>. E.g. /gh_load 17 playerABC-001");
                return;
            }
            else
            {
                // Set GhostRecords Counter to 0
                target.GhostPlaying = false;
                foreach (AssettoServer.Server.Ai.AiState y in target._aiStates)
                    if(y.LastSequenceID != 0)
                        y.TeleportGhostTo(0);

                string AbsolutePathToFile = FilesFittingToDiscreption[0];

                target.DecodeAndLoadGhostFile(AbsolutePathToFile);
                target.DecodeAndLoadGhostMetaFile(AbsolutePathToFile.Replace(".ghost", ".meta"));
                AbsolutePathToFile = Path.GetFileName(AbsolutePathToFile);
                if (AbsolutePathToFile.EndsWith(".ghost"))
                    AbsolutePathToFile = AbsolutePathToFile.Replace(".ghost", "");
                target.ReadInGhostFile = AbsolutePathToFile;
                target.GhostPlaying = true;

            }

        }

        [Command("gh_play")]
        public void GhostPlay(int GhostSessionID)
        {
            if (!Context.Client.IsAdministrator)
            {
                Reply("Command requires admin privileges.");
                Log.Information($"[GhostManagerPlugin]{Context.Client.Name} tried using the Command '{Context.Command.Name}' without enough privileges.");
                return;
            }

            EntryCar target = FindGhost(GhostSessionID);
            if (target == null)
                return;

            target.GhostPlaying = true;
        }

        [Command("gh_pause")]
        public void GhostPause(int GhostSessionID)
        {
            if (!Context.Client.IsAdministrator)
            {
                Reply("Command requires admin privileges.");
                Log.Information($"[GhostManagerPlugin]{Context.Client.Name} tried using the Command '{Context.Command.Name}' without enough privileges.");
                return;
            }

            EntryCar target = FindGhost(GhostSessionID);
            if (target == null)
                return;

            target.GhostPlaying = false;
        }

        [Command("gh_step")]
        public void GhostStepp(int GhostSessionID, int Steps)
        {
            if (!Context.Client.IsAdministrator)
            {
                Reply("Command requires admin privileges.");
                Log.Information($"[GhostManagerPlugin]{Context.Client.Name} tried using the Command '{Context.Command.Name}' without enough privileges.");
                return;
            }

            EntryCar target = FindGhost(GhostSessionID);
            if (target == null)
                return;



            foreach (AssettoServer.Server.Ai.AiState y in target._aiStates)
                y.TeleportGhostTo((y.CurrentGhostRecord + Steps + target.GhostLine.Count) % target.GhostLine.Count);

        }

        [Command("gh_set")]
        public void GhostSet(int GhostSessionID, string Type, int Value)
        {
            if (!Context.Client.IsAdministrator)
            {
                Reply("Command requires admin privileges.");
                Log.Information($"[GhostManagerPlugin]{Context.Client.Name} tried using the Command '{Context.Command.Name}' without enough privileges.");
                return;
            }

            EntryCar target = FindGhost(GhostSessionID);
            if (target == null)
                return;

            if (Type.ToLower() == "start")
            {
                if (Value < 0 || Value >= target.GhostEnd)
                {
                    Reply($"Start value needs to be inbetween 0 and the end value {target.GhostEnd}.");
                    return;
                }
                if(Value > target.GhostLoop)
                {
                    Reply($"Start value was bigger than loop value and therefore the loop value (before:{target.GhostLoop}) was also set to the start value {Value}");
                    target.GhostLoop = Value;
                }
                target.GhostStart = Value;
            }
            else if (Type.ToLower() == "loop")
            {
                if (Value < 0 || Value >= target.GhostEnd)
                {
                    Reply($"Loop value needs to be inbetween 0 and end value {target.GhostEnd}.");
                    return;
                }
                if(Value < target.GhostStart)
                {
                    Reply($"Loop value was smaller than start value and therefore the start value (before: {target.GhostStart}) was also set to the loop value {Value}");
                    target.GhostStart = Value;
                }
            
                target.GhostLoop = Value;
            }
            else if (Type.ToLower() == "end")
            {
                if (Value < 1 || Value > target.GhostLine.Count)
                {
                    Reply($"End value needs to be inbetween 1 and max value '{target.GhostLine.Count}'.");
                    return;
                }
                if (Value < target.GhostLoop)
                {
                    Reply($"Loop value was bigger than end value and therefore the loop value (before: {target.GhostLoop}) was also set to the end value {Value}");
                    target.GhostLoop = Value;
                }
                target.GhostEnd = Value;
            }
            else if (Type.ToLower() == "offset")
            {
                target.GhostOffset = Value;
            }
            else
            {
                Reply("Can't identify Command please use /gh_set [start/loop/end/offset] <number>");
            }
            Log.Debug($"[GhostManagerPlugin]Ghost {target.SessionId} has now the meta values [start:{target.GhostStart}|loop:{target.GhostLoop}|end:{target.GhostEnd}|offset:{target.GhostOffset}]");
        }

        // Show current Recordnumber
        [Command("gh_time")]
        public void GhostShowTime(int GhostSessionID)
        {
            EntryCar target = FindGhost(GhostSessionID);
            if (target == null)
                return;

            foreach (AssettoServer.Server.Ai.AiState y in target._aiStates)
            {
                Reply($"Current sequence number for {target.AiName}: {y.CurrentGhostRecord}");
            }


        }

        // Show all recorded files in chat
        [Command("gh_show_files")]
        public void GhostShowRecordFiles()
        {
            if (!Context.Client.IsAdministrator)
            {
                Reply("Command requires admin privileges.");
                Log.Information($"[GhostManagerPlugin]{Context.Client.Name} tried using the Command '{Context.Command.Name}' without enough privileges.");
                return;
            }
            DirectoryInfo directory = new DirectoryInfo(GhostManagerPlugin.GhostBasePath);
            foreach (FileInfo file in directory.GetFiles()) 
            {
                if (!file.Name.Contains(".ghost"))
                    continue;

                string MetaFileExists = "";
                if (Path.Exists(GhostManagerPlugin.GhostBasePath + Path.PathSeparator + file.Name.Replace(".ghost", ".meta")))
                    MetaFileExists = " + Metafile ";

                // Creates a double of the filesize in megabytes
                string NiceFileSize = (file.Length / (double)Math.Pow(1024, (Int64)2)).ToString("0.00");

                Reply($"{file.Name}{MetaFileExists} [{NiceFileSize}MB]\n");
            }
        }

        // Show the help message
        [Command("gh_help")]
        public void GhostHelp()
        {
            string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            string ReadmeText = "";

            foreach (string x in Directory.EnumerateFiles(assemblyFolder))
            {
                if (!x.Contains("README.md"))
                    continue;

                ReadmeText = File.ReadAllText(x);                
            }
            if(ReadmeText.Length == 0)
            {
                Reply("README.md not loaded in or empty.\n");
                return;
            }

            var Splittedtxt = ReadmeText.Split("\n");
            foreach(string x in Splittedtxt)
                Reply(x);
        }

        // Toggle Looponly mode
        [Command("gh_loopmode")]
        public void GhostLoopOnlyMode(int GhostSessionID, string Mode)
        {
            if (!Context.Client.IsAdministrator)
            {
                Reply("Command requires admin privileges.");
                Log.Information($"[GhostManagerPlugin]{Context.Client.Name} tried using the Command '{Context.Command.Name}' without enough privileges.");
                return;
            }

            EntryCar target = FindGhost(GhostSessionID);
            if (target == null)
                return;

            string mode = Mode.ToLower();
            if (mode == "on" || mode == "true" || mode == "yes" || mode == "1")
            {
                target.GhostLoopMode = true;
                AiBehavior.GhostsGroupedByCluster.Clear();
                Reply($"Turned loopmode on for Ghost [{target.SessionId}]");
            }
            else
            {
                target.GhostLoopMode = false;
                AiBehavior.GhostsGroupedByCluster.Clear();
                Reply($"Turned loopmode off for Ghost [{target.SessionId}]");
            }
        }

        // Hide Ghost under the map
        [Command("gh_hide")]
        public void GhostHide(int GhostSessionID)
        {
            if (!Context.Client.IsAdministrator)
            {
                Reply("Command requires admin privileges.");
                Log.Information($"[GhostManagerPlugin]{Context.Client.Name} tried using the Command '{Context.Command.Name}' without enough privileges.");
                return;
            }

            EntryCar target = FindGhost(GhostSessionID);
            if (target == null)
                return;

            if (target.GhostHidden)
            {
                target.GhostHidden = false;
                target.GhostPlaying = true;
                Reply($"Ghost {GhostSessionID} is now visible again.");
            }
            else
            {
                target.GhostHidden = true;
                Reply($"Ghost {GhostSessionID} is now hidden.");
            }
        }

        [Command("gh_setcluster")]
        public void GhostSetCluster(int GhostSessionID, string ClusterName)
        {
            if (!Context.Client.IsAdministrator)
            {
                Reply("Command requires admin privileges.");
                Log.Information($"[GhostManagerPlugin]{Context.Client.Name} tried using the Command '{Context.Command.Name}' without enough privileges.");
                return;
            }

            EntryCar target = FindGhost(GhostSessionID);
            if (target == null)
                return;

            target.GhostCluster = ClusterName;
            AiBehavior.GhostsGroupedByCluster.Clear();
            Reply("Cluster succesfully set.");
        }


        // Will sync every ghost up to the given Ghost. This will make all other ghost recalculate their
        // current time so that their time +/- offset matches that of the given ghost
        // Pretty much only useful if you have multiple ghosts with the same ghostfile running but set an offset for each 
        /*
        [Command("gh_sync_by")]
        public void GhostSyncBy(int GhostSessionID)
        {
            if (!Context.Client.IsAdministrator)
            {
                Reply("Command requires admin privileges.");
                Log.Information($"[GhostManagerPlugin]{Context.Client.Name} tried using the Command '{Context.Command.Name}' without enough privileges.");
                return;
            }

            EntryCar target = FindGhost(GhostSessionID);
            if (target == null)
                return;

            Dictionary<string, List<EntryCar>> GroupedByCluster = new Dictionary<string, List<EntryCar>>();
            foreach (EntryCar car in GhostManagerPlugin._entryCarManager.EntryCars)
            {
                List<EntryCar> FoundCluster;
                if (!GroupedByCluster.TryGetValue(car.GhostCluster, out FoundCluster))
                {
                    FoundCluster = new List<EntryCar>();
                    GroupedByCluster.Add(car.GhostCluster, FoundCluster);
                }
                FoundCluster.Add(car);
            }

            foreach (string Cluster in GroupedByCluster.Keys)
            {

                // needs rework once overbooking for ghosts is implemented
                int CurrentZeroPoint = target._aiStates[0].CurrentGhostRecord - target.GhostStart;

                foreach (EntryCar car in GhostManagerPlugin._entryCarManager.EntryCars)
                {
                    if (car.SessionId == target.SessionId)
                        continue;
                    if (car.AiControlled && car.SessionId != null && car.ReadInGhostFile == target.ReadInGhostFile)
                    {
                        int RelativeOffset = car.GhostOffset - target.GhostOffset;
                        int Range = car.GhostEnd - car.GhostStart;
                        foreach (AiState state in car._aiStates)
                        {
                            state.TeleportGhostTo((car.GhostStart + CurrentZeroPoint + RelativeOffset) % Range);
                        }
                    }
                }
            }
        }*/

        [Command("gh_sync")]
        public void GhostSyncAll()
        {
            if (!Context.Client.IsAdministrator)
            {
                Reply("Command requires admin privileges.");
                Log.Information($"[GhostManagerPlugin]{Context.Client.Name} tried using the Command '{Context.Command.Name}' without enough privileges.");
                return;
            }

            foreach (EntryCar car in GhostManagerPlugin._entryCarManager.EntryCars)
            {
                if (car.AiControlled && car.SessionId != null)
                {
                    int Range = car.GhostEnd - car.GhostStart;
                    foreach (AiState state in car._aiStates)
                    {
                        state.TeleportGhostTo((car.GhostStart + car.GhostOffset) % Range);
                    }
                }
            }
        }


        /// <summary>Replaces characters in <c>text</c> that are not allowed in 
        /// file names with the specified replacement character.</summary>
        /// <param name="text">Text to make into a valid filename. The same string is returned if it is valid already.</param>
        /// <param name="replacement">Replacement character, or null to simply remove bad characters.</param>
        /// <param name="fancy">Whether to replace quotes and slashes with the non-ASCII characters ” and ⁄.</param>
        /// <returns>A string that can be used as a filename. If the output string would otherwise be empty, returns "_".</returns>
        public static string MakeValidFileName(string text, char? replacement = '_')
        {
            char[] invalids = Path.GetInvalidFileNameChars();
            StringBuilder sb = new StringBuilder(text.Length);
            bool changed = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (invalids.Contains(c))
                {
                    changed = true;
                    var repl = replacement ?? '\0';
                    if (repl != '\0')
                        sb.Append(repl);
                }
                else
                    sb.Append(c);
            }
            if (sb.Length == 0)
                return "_";
            return changed ? sb.ToString() : text;
        }

        private EntryCar FindPlayer(string PartialPlayerName)
        {
            List<EntryCar> found = new List<EntryCar>();
            foreach (EntryCar x in GhostManagerPlugin._entryCarManager.EntryCars)
            {
                if(x == null) continue;

                if (x.AiMode == AiMode.None && x.Client != null && x.Client.Name != null && x.Client.Name.Contains(PartialPlayerName))
                    found.Add(x);
            }
            if (found.Count <= 0)
            {
                Reply($"No Players found with the string '{PartialPlayerName}' part of their name.");
                return null;
            }
            else if (found.Count > 1)
            {
                string matching_players = "";
                foreach (EntryCar x in found)
                {
                    matching_players += $"[{x.Client.Name}], ";
                }
                Reply($"The Players '{matching_players}' have all the string '{PartialPlayerName}' as a part of their name and therefore the provided identifier is not unique enough.");
                return null;
            }
            return found[0];

        }

        private EntryCar FindGhost(int GhostSessionID)
        {
            foreach (EntryCar x in GhostManagerPlugin._entryCarManager.EntryCars)
            {
                if (x.AiControlled && x.SessionId != null && x.SessionId == GhostSessionID)
                    return x;
            }

            Reply($"No Ghosts found with the ID '{GhostSessionID}'. Use /gh_identify to show all available ghosts.");
            return null;

        }

    }
}
