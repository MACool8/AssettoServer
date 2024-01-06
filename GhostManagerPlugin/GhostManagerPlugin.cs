using AssettoServer.Server;
using AssettoServer.Server.Configuration;
using AssettoServer.Server.Plugin;
using AssettoServer.Shared.Services;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace GhostManagerPlugin
{
    public class GhostManagerPlugin : CriticalBackgroundService, IAssettoServerAutostart
    {
        internal static ACServerConfiguration _configuration;
        internal static EntryCarManager _entryCarManager;
        internal static string GhostBasePath;
        public GhostManagerPlugin(IHostApplicationLifetime applicationLifetime, ACServerConfiguration configuration, EntryCarManager entryCarManager) : base(applicationLifetime)
        {
            Log.Debug($"[GhostManagerPlugin] GhostManagerPlugin V0.3 is starting");

            _configuration = configuration;
            _entryCarManager = entryCarManager;

            string contentPath = "content";
            const string contentPathCMWorkaround = "content~tmp";
            // CM renames the content folder to content~tmp when enabling the "Disable integrity verification" checkbox. We still need to load an Ghost files from there, even when checksums are disabled
            if (!Directory.Exists(contentPath) && Directory.Exists(contentPathCMWorkaround))
            {
                contentPath = contentPathCMWorkaround;
            }

            string AIBasePath = Path.Join(contentPath, $"tracks/{_configuration.Server.Track}/ai");
            GhostBasePath = Path.Join(contentPath, $"tracks/{_configuration.Server.Track}/ai/ghosts/");

            if (!Path.Exists(AIBasePath))
            {
                Directory.CreateDirectory(AIBasePath);
            }

            if (!Path.Exists(GhostBasePath))
            {
                Directory.CreateDirectory(GhostBasePath);
            }
            Log.Debug($"[GhostManagerPlugin] GhostManagerPlugin V0.3 constructed.");
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Log.Debug("[GhostManagerPlugin] GhostManagerPlugin autostart called");

            int aiCount = 0;
            foreach (var entryCar in _entryCarManager.EntryCars)
            {
                if (entryCar.AiControlled)
                    aiCount++;
            }

            Log.Debug($"[GhostManagerPlugin] GhostManagerPlugin plugin V0.3 initialized for {aiCount} Ghosts");
            return Task.CompletedTask;
        }
    }
}
