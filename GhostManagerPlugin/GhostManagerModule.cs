using AssettoServer.Server.Plugin;
using Autofac;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostManagerPlugin
{
    public class GhostManagerModule : AssettoServerModule
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<GhostManagerPlugin>().AsSelf().As<IAssettoServerAutostart>().SingleInstance();
        }
    }
}
