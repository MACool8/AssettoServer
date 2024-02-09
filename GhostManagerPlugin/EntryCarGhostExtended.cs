using System.Numerics;
using System.ServiceModel.Channels;
using AssettoServer.Network.Tcp;
using AssettoServer.Server;
using AssettoServer.Shared.Network.Packets.Incoming;
using AssettoServer.Shared.Network.Packets.Outgoing;
using AssettoServer.Shared.Network.Packets.Shared;

namespace GhostManagerPlugin;
public class EntryCarGhostExtended
{
    private readonly SessionManager _sessionManager;
    private readonly EntryCarManager _entryCarManager;
    private readonly EntryCar _entryCar;

    private long StartedHonking = -1;
    private long LastLightFlashTime = -1;
    private int LightFlashCount = 0;
    private int OffsetToLastRecording = 0;

    public EntryCarGhostExtended(EntryCar entryCar, EntryCarManager entryCarManager, SessionManager sessionManager)
    {
        _entryCar = entryCar;
        _entryCarManager = entryCarManager;
        _sessionManager = sessionManager;
        _entryCar.PositionUpdateReceived += OnPositionUpdateReceived;
        _entryCar.ResetInvoked += OnResetInvoked;
    }

    private void OnResetInvoked(EntryCar sender, EventArgs args)
    {
        //Stoppin Recording
        if (sender.RecordingStarted)
            GhostManagerCommandModule._StopRecord(sender, "[QUICKRECORD]" + sender.Client.Name);
    }

    private void OnPositionUpdateReceived(EntryCar sender, in PositionUpdateIn positionUpdate)
    {
        if(!GhostManagerPlugin.QuickCommandsEnabled || 
            sender == null || 
            sender.Client == null || 
            sender.AiControlled ||
            (!sender.Client.IsAdministrator && GhostManagerPlugin.AdminRequired)) 
            return;


        long currentTick = _sessionManager.ServerTimeMilliseconds;
        // Upon turning on hazard lights
        if (((_entryCar.Status.StatusFlag & CarStatusFlags.HazardsOn) == 0 
            && (positionUpdate.StatusFlag & CarStatusFlags.HazardsOn) != 0))
        {
            // Starting recording
            if (!sender.RecordingStarted)
            {
                GhostManagerCommandModule._StartRecord(sender, "[QUICKRECORD]" + sender.Client.Name);
                string message = "Started the quickrecording";
                sender.Client.SendPacket(new ChatMessage { SessionId = 255, Message = message });
            }
        }
        // Upon turning off hazard lights
        else if(((_entryCar.Status.StatusFlag & CarStatusFlags.HazardsOn) != 0 
            && (positionUpdate.StatusFlag & CarStatusFlags.HazardsOn) == 0))
        {
            //Stoppin Recording
            if (sender.RecordingStarted)
            {
                string FileName = GhostManagerCommandModule._StopRecord(sender, "[QUICKRECORD]" + sender.Client.Name);
                string message = $"Stopped recording and saved file to {FileName}";
                sender.Client.SendPacket(new ChatMessage { SessionId = 255, Message = message });
            }
        }

        if ((_entryCar.Status.StatusFlag & CarStatusFlags.LightsOn) == 0
            && (positionUpdate.StatusFlag & CarStatusFlags.LightsOn) != 0)
        {
            
        }
        else if ((_entryCar.Status.StatusFlag & CarStatusFlags.LightsOn) != 0
            && (positionUpdate.StatusFlag & CarStatusFlags.LightsOn) == 0)
        {
            LightFlashCount++;
            LastLightFlashTime = currentTick;
        }

        if (currentTick - LastLightFlashTime > 3000 && LightFlashCount > 0)
        {
            LightFlashCount = 0;
        }


        if(LightFlashCount == 3)
        {
            // Breaks if no ghosts loaded
            EntryCar FirstGhost = _entryCarManager.EntryCars.Where(car => car.AiControlled == true).ToList<EntryCar>()[0];
            if (FirstGhost.GhostHidden)
            {
                FirstGhost.GhostHidden = false;
                FirstGhost.GhostPlaying = true;
                string message = $"Ghost {FirstGhost.SessionId} is now visible.";
                sender.Client.SendPacket(new ChatMessage { SessionId = 255, Message = message });
            }
            else
            {
                FirstGhost.GhostHidden = true;
                string message = $"Ghost {FirstGhost.SessionId} is now hiden.";
                sender.Client.SendPacket(new ChatMessage { SessionId = 255, Message = message });
            }
            LightFlashCount = 0;
        }

        List<string> FilesFittingToDescription = null;


        if ((_entryCar.Status.StatusFlag & CarStatusFlags.IndicateRight) != 0
            && (positionUpdate.StatusFlag & CarStatusFlags.IndicateRight) == 0)
        {
            OffsetToLastRecording--;
        }

        if ((_entryCar.Status.StatusFlag & CarStatusFlags.IndicateLeft) != 0
            && (positionUpdate.StatusFlag & CarStatusFlags.IndicateLeft) == 0)
        {
            OffsetToLastRecording++;
        }

        //if either left or right indicator got triggered tell the user which Ghost is now selected
        if (((_entryCar.Status.StatusFlag & CarStatusFlags.IndicateLeft) != 0
            && (positionUpdate.StatusFlag & CarStatusFlags.IndicateLeft) == 0) ||
            ((_entryCar.Status.StatusFlag & CarStatusFlags.IndicateRight) != 0
            && (positionUpdate.StatusFlag & CarStatusFlags.IndicateRight) == 0))
        {
            if (FilesFittingToDescription == null)
            {
                FilesFittingToDescription = new List<string>();
                foreach (string File in Directory.EnumerateFiles(GhostManagerPlugin.GhostBasePath))
                {
                    if (!File.EndsWith(".ghost"))
                        continue;

                    if (!File.Contains(sender.Client.Name))
                        continue;

                    FilesFittingToDescription.Add(File);
                }
                FilesFittingToDescription.Sort();
            }

            int GhostCount = FilesFittingToDescription.Count;

            if (GhostCount > 0)
            {
                int SelectedGhostIndex = (GhostCount - 1 + OffsetToLastRecording) % GhostCount;
                while (SelectedGhostIndex < 0)
                    SelectedGhostIndex = (SelectedGhostIndex + GhostCount) % GhostCount;

                string SelectedGhost = FilesFittingToDescription[SelectedGhostIndex];

                string message = $"Currently selected the following Ghost: {Path.GetFileName(SelectedGhost)}";
                sender.Client.SendPacket(new ChatMessage { SessionId = 255, Message = message });
            }
        }

        if ((_entryCar.Status.StatusFlag & CarStatusFlags.Horn) == 0
            && (positionUpdate.StatusFlag & CarStatusFlags.Horn) != 0)
        {
            StartedHonking = currentTick;
        }
        else if ((_entryCar.Status.StatusFlag & CarStatusFlags.Horn) != 0
            && (positionUpdate.StatusFlag & CarStatusFlags.Horn) == 0)
        {
            StartedHonking = -1;
        }

        if(StartedHonking > 0 && currentTick - StartedHonking >= 3000) 
        {
            // Load in last (or offset to last)
            if (FilesFittingToDescription == null)
            {
                FilesFittingToDescription = new List<string>();
                foreach (string File in Directory.EnumerateFiles(GhostManagerPlugin.GhostBasePath))
                {
                    if (!File.EndsWith(".ghost"))
                        continue;

                    if (!File.Contains(sender.Client.Name))
                        continue;

                    FilesFittingToDescription.Add(File);
                }
                FilesFittingToDescription.Sort();
            }

            int GhostCount = FilesFittingToDescription.Count;

            if (GhostCount > 0)
            {
                int SelectedGhostIndex = (GhostCount - 1 + OffsetToLastRecording)% GhostCount;
                while (SelectedGhostIndex < 0)
                    SelectedGhostIndex = (SelectedGhostIndex + GhostCount) % GhostCount;

                string SelectedGhost = FilesFittingToDescription[SelectedGhostIndex];

                // Breaks if no ghosts loaded
                EntryCar FirstGhost = _entryCarManager.EntryCars.Where(car => car.AiControlled == true).ToList<EntryCar>()[0];

                GhostManagerCommandModule._LoadGhost(FirstGhost, SelectedGhost);
                string message = $"Loaded in {Path.GetFileName(SelectedGhost)}";
                sender.Client.SendPacket(new ChatMessage { SessionId = 255, Message = message });
            }
            StartedHonking = -1;
        }


    }

}

