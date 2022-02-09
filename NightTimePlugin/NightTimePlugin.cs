using AssettoServer.Server;
using AssettoServer.Server.Plugin;
using AssettoServer.Network.Packets.Shared;
using Serilog;
using TimeZoneConverter;

namespace NightPlugin;

public class NightTimePlugin : IAssettoServerPlugin
{
    ACServer _server;
    public void Initialize(ACServer server)
    {
        Log.Debug("[NightTimePlugin] NightTime plugin V0.1 initialized");
        _server = server;
        _ = LoopAsync();
    }

    public bool CheckForDawn()
    {
        var DawnTime = TimeZoneInfo.ConvertTimeToUtc(TimeZoneInfo.ConvertTimeFromUtc(_server.CurrentDateTime, _server.TimeZone).Date + TimeSpan.FromSeconds(22000), _server.TimeZone);
        if (_server.CurrentDateTime >= DawnTime)
            return true;

        return false;
    }

    public void SetNightTime()
    {
        _server.SetTime((float)0.0);
        _server.BroadcastPacket(new ChatMessage { SessionId = 255, Message = "[NightTimePlugin] Woah, it's almost morning. Let's skip time untill it's night again." });
        Log.Debug("[NightTimePlugin] Time has been set to 0.");
    }

    internal async Task LoopAsync()
    {
        while (true)
        {
            try
            {
                Update();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during night plugin run");
            }
            finally
            {
                await Task.Delay(5000);
            }
        }
        // ReSharper disable once FunctionNeverReturns
    }

    public void Update()
    {
        if (CheckForDawn())
            SetNightTime();
    }
}