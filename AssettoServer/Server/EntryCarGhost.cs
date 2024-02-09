using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using AssettoServer.Network.Tcp;
using AssettoServer.Server.Ai;
using AssettoServer.Server.Ai.Splines;
using AssettoServer.Server.Configuration;
using AssettoServer.Shared.Model;
using AssettoServer.Shared.Network.Packets.Incoming;
using AssettoServer.Shared.Network.Packets.Outgoing;
using Serilog;
using System.Text.Json;
using GhostState = AssettoServer.Shared.Network.Packets.Incoming.PositionUpdateIn;
using AssettoServer.Shared.Network.Packets;
using Newtonsoft.Json;

namespace AssettoServer.Server;

public partial class EntryCar
{
    public List<GhostState>? GhostLine { get; set; }
    public string GhostName = "Ghost";
    public int GhostStart = 0;
    public int GhostLoop = 0;
    public int GhostEnd;
    public bool GhostLoopMode = true;
    public bool GhostHidden = false;
    public string ReadInGhostFile = "";
    public int GhostOffset = 0;
    public string GhostCluster = "";

    public bool GhostPlaying = true;

    public bool RecordingStarted = false;
    private Memory<byte> RecorderBuffer { get; set; }
    private int _writePosition = 0;

    private const int SizeOfMessage = 61; // As long the implementation of "PositionUpdateIn" doesn't change this stays the same

    /// <summary>
    /// Constructor for initialising 
    /// </summary>
    /// <param name="entryCar"></param>
    public void GhostInit()
    {
        if (!AiControlled)
        {
            if (_configuration.Extra.EnableGhosts)
                PositionUpdateReceived += OnPositionUpdateReceived;
        }
        else if(_configuration.Extra.EnableGhosts)
        {
            Log.Debug($"[Ghost] Started initializing for the ghost [{SessionId}] file: '{ReadInGhostFile}'");
            if (ReadInGhostFile.Length == 0)
            {
                Log.Error($"[Ghost] Ghosts enabled but no Ghostfile was provided in the entry_list.ini for the Ghost [{SessionId}]. Disabling Ghost [{SessionId}] until Ghost file is loaded in.");
                GhostPlaying = false;
                return;
            }
            Log.Debug("[Ghost] Passed init checks");
            // Remove ".ghost" suffix
            if (ReadInGhostFile.EndsWith(".ghost"))
            {
                ReadInGhostFile = ReadInGhostFile.Remove(ReadInGhostFile.Length - 6);
            }

            string contentPath = "content";
            const string contentPathCMWorkaround = "content~tmp";
            // CM renames the content folder to content~tmp when enabling the "Disable integrity verification" checkbox. We still need to load an Ghost files from there, even when checksums are disabled
            if (!Directory.Exists(contentPath) && Directory.Exists(contentPathCMWorkaround))
            {
                contentPath = contentPathCMWorkaround;
            }

            Log.Debug($"[Ghost] Loading in Ghostfile for Ghost [{SessionId}]");
            string mapGhostBasePath = Path.Join(contentPath, $"tracks/{_configuration.Server.Track}/ai/ghosts/"); ;
            if (File.Exists(Path.Join(mapGhostBasePath, ReadInGhostFile + ".ghost")))
            {
                Log.Debug($"[Ghost] Reading in Ghostfile '{Path.Join(mapGhostBasePath, ReadInGhostFile + ".ghost")}'");
                DecodeAndLoadGhostFile(Path.Join(mapGhostBasePath, ReadInGhostFile + ".ghost"));

                if (File.Exists(Path.Join(mapGhostBasePath, ReadInGhostFile + ".meta")))
                {
                    Log.Debug($"[Ghost] Reading in Metafile '{Path.Join(mapGhostBasePath, ReadInGhostFile + ".meta")}'");
                    DecodeAndLoadGhostMetaFile(Path.Join(mapGhostBasePath, ReadInGhostFile + ".meta"));
                }
            }
            else
            {
                Log.Error($"[Ghost] Ghosts enabled but no Ghostfile '{ReadInGhostFile}.ghost' found for Ghost [{SessionId}]. Disabling Ghost [{SessionId}] until Ghost file is loaded in.");
                GhostPlaying = false;
                return;
            }
        }
        else
        {
            Log.Debug($"[Ghost] Vehicle [{SessionId}] is unknown");
        }
    }

    public void DecodeAndLoadGhostFile(string FileName)
    {
        List<GhostState> Updates = new List<GhostState>();

        byte[] filebinary = File.ReadAllBytes(FileName);
        long length = filebinary.Length;
        Memory<byte> data = new Memory<byte>(filebinary);

        Log.Debug($"[Ghost] Starting to read in. Filesize: {length} bytes");

        PacketReader Reader = new PacketReader(null, data);

        while (Reader.ReadPosition < length)
        {
            Updates.Add(Reader.Read<GhostState>());
        }

        SortGhostUpdates(Updates);

        GhostEnd = Updates.Count - 1;
        GhostStart = 0;
        GhostLoop = 0;
        GhostLine = Updates;
    }

    // Pretty much insertion sort 
    // with an efficiant way of finding how many pos to the left a packet needs to go
    // Practicly it should be O(n)
    private void SortGhostUpdates(List<GhostState> updates)
    {
        int ctr = 1;
        while (ctr < updates.Count)
        {
            int A = updates[ctr - 1].PakSequenceId;
            int B = updates[ctr].PakSequenceId;

            // Eg A = 2, B = 255
            bool BDoesntFitIntoThisSegment = (B - A) < 256 && (B - A) >= 128;
            // Eg A = 251; B = 250
            bool BIsSmallerThanA = (B - A) >= -128 && (B - A) < 0;

            // The amount of positiions the packet needs to move to the left
            int PosToLeft = 0;

            while ((BDoesntFitIntoThisSegment || BIsSmallerThanA) && ctr - 2 - PosToLeft >= 0)
            {
                PosToLeft++;

                A = updates[ctr - 1 - PosToLeft].PakSequenceId;
                B = updates[ctr - PosToLeft].PakSequenceId;

                BDoesntFitIntoThisSegment = (B - A) < 256 && (B - A) >= 128;
                BIsSmallerThanA = (B - A) >= -128 && (B - A) < 0;

            }

            if (PosToLeft > 0)
            {
                GhostState Temp = updates[ctr];
                updates.RemoveAt(ctr);
                updates.Insert(ctr - PosToLeft, Temp);
            }

            ctr++;
        }

    }

    public void DecodeAndLoadGhostMetaFile(string FileName)
    {
        Dictionary<string, string> Meta = new Dictionary<string, string>();

        try
        {
            string Json = File.ReadAllText(FileName);
            Meta = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(Json);
        }
        catch (Exception ex)
        {
            Log.Error($"[Ghost] Reading in Metadata from {FileName} failed. Continue with default values. Error: {ex.Message}");
        }

        FillMetaData(Meta);
    }

    public void FillMetaData(Dictionary<string, string> Meta)
    {
        string? InputToConvert;
        if (!Meta.TryGetValue("name", out GhostName))
            GhostName = Model;

        // If this gets updated while players are in the server, their displayed ainame will only change after a reconnect
        AiName = $"[{SessionId}][Ghost]{GhostName}";

        if (!Meta.TryGetValue("start", out InputToConvert))
            GhostStart = 0;
        else
            GhostStart = int.Parse(InputToConvert);

        if (!Meta.TryGetValue("loop", out InputToConvert))
            GhostLoop = 0;
        else
            GhostLoop = int.Parse(InputToConvert);

        if (!Meta.TryGetValue("end", out InputToConvert))
            GhostEnd = GhostLine.Count - 1;
        else
            GhostEnd = int.Parse(InputToConvert);

        if (!Meta.TryGetValue("loopmode", out InputToConvert))
            GhostLoopMode = false;
        else
            GhostLoopMode = bool.Parse(InputToConvert);

    }

    /// <summary>
    /// Checks on every PositionUpdate weather someone flashed their lights atleast 2 times and waited a bit
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="positionUpdate"></param>
    private void OnPositionUpdateReceived(EntryCar sender, in PositionUpdateIn positionUpdate)
    {
        if (RecordingStarted)
        {
            // GhostState are the same struct as PositionUpdateIn
            Write<GhostState>(positionUpdate);
        }

    }

    /// <summary>
    /// Adds a entry to RecorderBuffer. Wraps WriteBytes nicely.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    public void Write<T>(T value) where T : struct
    {
        WriteBytes(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1)));
    }

    private void WriteBytes(Span<byte> bytes)
    {
        bytes.CopyTo(RecorderBuffer.Slice(_writePosition).Span);
        _writePosition += bytes.Length;

        //check if the next update could fit into memory and abort if not
        if (_writePosition + SizeOfMessage > RecorderBuffer.Length)
        {
            Log.Debug($"[Ghost] A Recording Session from '{Client.Name}' was too long and got stopped.");
            RecordingStarted = false;
        }
    }

    /// <summary>
    /// Saves all recorded position updates to given filename. Only ".ghost" files allowed
    /// </summary>
    /// <param name="FullFileName">Full path to file where it should be saved.</param>
    private void SaveCurrentRecording(string FullFileName)
    {
        if (!FullFileName.EndsWith(".ghost"))
        {
            Log.Debug($"Filename '{FullFileName}' has wrong format for ghost recordings and won't be saved.");
            return;
        }

        FileStream file;

        file = new FileStream(FullFileName,
                          FileMode.Create,
                          FileAccess.Write);

        file.Write(RecorderBuffer.Slice(0, _writePosition).Span);

        file.Close();
    }

    /// <summary>
    /// Saves meta data to the given filename. Only ".meta" files allowed.
    /// </summary>
    /// <param name="FullFileName">Full path to file where it should be saved.</param>
    /// <param name="MetaData">Metada which should be saved.</param>
    public void SaveCurrentRecordingMetaData(string FullFileName, string MetaData)
    {
        if (!FullFileName.EndsWith(".meta"))
        {
            Log.Debug($"[Ghost] Filename '{FullFileName}' has wrong format for meta data and won't be saved.");
            return;
        }

        File.WriteAllText(FullFileName, MetaData);
    }

    /// <summary>
    /// Once called, every "GhostState/PositionUpdateIn" received by the server from this car will be recorded in RAM untill stopped.
    /// </summary>
    public void StartRecording()
    {
        // Allows to record for exactly 30 mins on a 32 hz server and takes roughly 3.5 MB ram per recording
        RecorderBuffer = new byte[SizeOfMessage * 32 * 60 * 30];

        _writePosition = 0;

        this.RecordingStarted = true;

        Log.Debug($"[Ghost] {Client.Name} started recording.");
    }

    /// <summary>
    /// Stops recording and deletes the recorded buffer. No saving to file happens
    /// </summary>
    public void StopRecordingAndDiscard()
    {
        RecordingStarted = false;

        RecorderBuffer = null;

        Log.Debug($"[Ghost] {Client.Name} stopped recording.");
    }

    /// <summary>
    /// Stops recording and saves the recorded data to the given path. Meta data will not be saved.
    /// </summary>
    /// <param name="FullFileName"></param>
    public void StopRecordingAndSave(string FullFileName)
    {
        this.RecordingStarted = false;

        this.SaveCurrentRecording(FullFileName);

        RecorderBuffer = null;

        Log.Debug($"[Ghost] {Client.Name} stopped recording and saved {FullFileName}");
    }

    /// <summary>
    /// Stops recoding, saves the recorded data and meta data to the given path (seperate files, only .ghost file path parametr needs to be given)
    /// </summary>
    /// <param name="FullFileName">Full path to the ".ghost" file.</param>
    /// <param name="BotName">The Name to be saved for the bot.</param>
    public void StopRecordingAndSave(string FullFileName, string BotName)
    {
        int RecordSize = (_writePosition / 61) - 1;

        StopRecordingAndSave(FullFileName);

        string MetaFullFileName = FullFileName.Replace(".ghost", ".meta");

        string MetaData = SourceMetaData(BotName, 0, 0, RecordSize, true);

        SaveCurrentRecordingMetaData(MetaFullFileName, MetaData);
    }

    /// <summary>
    /// Creates a serialized json string to be saved as meta data. Automaticly collects the last used start, loop and end values
    /// </summary>
    /// <param name="BotName"></param>
    /// <returns></returns>
    public string SourceMetaData(string BotName)
    {
        return SourceMetaData(BotName, GhostStart, GhostLoop, GhostEnd, GhostLoopMode);
    }

    /// <summary>
    /// Creates a serialized json string to be saved as meta data.
    /// </summary>
    /// <param name="BotName"></param>
    /// <param name="start"></param>
    /// <param name="loop"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public string SourceMetaData(string BotName, int start, int loop, int end, bool loopmode)
    {
        Dictionary<string, string> metadata = new Dictionary<string, string>();

        metadata.Add("metaformat_version", "V01");
        metadata.Add("name", BotName); // if empty or entry missing, default name will be taken
        metadata.Add("start", start.ToString());
        metadata.Add("loop", loop.ToString());
        metadata.Add("end", end.ToString());
        metadata.Add("loopmode", loopmode.ToString());
        // stuff which doesn't gets used internally but is nice to know
        metadata.Add("server_update_frequency", _configuration.Server.RefreshRateHz.ToString() + "Hz");
        metadata.Add("track", _configuration.FullTrackName);
        metadata.Add("car", Model.ToString());
        metadata.Add("skin", Skin.ToString());
        metadata.Add("time_saved", DateTime.Now.ToString("yyyy_MM_dd-HH_mm"));

        return System.Text.Json.JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
    }

}

