using System;
using System.Collections.Generic;

using Slb.Ocean.Core;

namespace DFNGenerator_Ocean
{
    public class DFNPlugin : Slb.Ocean.Core.Plugin
    {
        public override string AppVersion
        {
            get { return "2020.0"; }
        }

        public override string Author
        {
            get { return "Michael Welch and Mikael L�thje"; }
        }

        public override string Contact
        {
            get { return "mwelch@dtu.dk or mikael@dtu.dk"; }
        }

        public override IEnumerable<PluginIdentifier> Dependencies
        {
            get { return null; }
        }

        public override string Description
        {
            get { return "DFN Generator: Petrel version"; }
        }

        public override string ImageResourceName
        {
            get { return null; }
        }

        public override Uri PluginUri
        {
            get { return new Uri("https://offshore.dtu.dk/english"); }
        }

        public override IEnumerable<ModuleReference> Modules
        {
            get 
            {
                yield return new ModuleReference(typeof(DFNGenerator_Ocean.DFNModule2));
            }
        }

        public override string Name
        {
            get { return "DFN_Plugin"; }
        }

        public override PluginIdentifier PluginId
        {
            get { return new PluginIdentifier(GetType().FullName, GetType().Assembly.GetName().Version); }
        }

        public override ModuleTrust Trust
        {
            get { return new ModuleTrust("Default"); }
        }
    }
}
