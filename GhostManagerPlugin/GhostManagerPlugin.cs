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
        internal static SessionManager _sessionManager;
        internal static string GhostBasePath;
        public static bool AdminRequired = true;
        public static bool QuickCommandsEnabled = true;
        private readonly List<EntryCarGhostExtended> ExtendedGhostCars = new();
        public GhostManagerPlugin(IHostApplicationLifetime applicationLifetime, ACServerConfiguration configuration, EntryCarManager entryCarManager, SessionManager sessionManager) : base(applicationLifetime)
        {
            Log.Debug($"[GhostManagerPlugin] GhostManagerPlugin V0.3.1 is starting");

            _configuration = configuration;
            _entryCarManager = entryCarManager;
            _sessionManager = sessionManager;

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
            Log.Debug($"[GhostManagerPlugin] GhostManagerPlugin V0.3.1 constructed.");
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Log.Debug("[GhostManagerPlugin] GhostManagerPlugin autostart called");

            int aiCount = 0;
            foreach (var entryCar in _entryCarManager.EntryCars)
            {

                ExtendedGhostCars.Add(new EntryCarGhostExtended(entryCar, _entryCarManager, _sessionManager));

                if (entryCar.AiControlled)
                    aiCount++;                    
            }

            Log.Debug($"[GhostManagerPlugin] GhostManagerPlugin plugin V0.3.1 initialized for {aiCount} Ghosts");
            return Task.CompletedTask;
        }
    }
}
