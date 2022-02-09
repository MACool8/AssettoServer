using AssettoServer.Server;
using AssettoServer.Server.Plugin;
using Serilog;

namespace LightCommunicationPlugin
{
    public class LightCommunicationPlugin : IAssettoServerPlugin
    {
        internal static readonly Dictionary<int, EntryCarLight> Instances = new();

        public void Initialize(ACServer server)
        {
            Log.Debug("[LightCommunicationPlugin] LightCommunication plugin V0.1 initialized");
            foreach (var entryCar in server.EntryCars)
            {
                Instances.Add(entryCar.SessionId, new EntryCarLight(entryCar));
            }
        }
    }
}
