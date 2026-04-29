using System;
using System.Collections.Generic;

using Slb.Ocean.Core;

namespace DFMGenerator_Ocean
{
    public class DFMGeneratorPlugin : Slb.Ocean.Core.Plugin
    {
        public override string AppVersion
        {
            get { return "2023.1"; }
        }

        public override string Author
        {
            get { return "Michael Welch and Mikael Lüthje"; }
        }

        public override string Contact
        {
            get { return "mwelch@dtu.dk"; }
        }

        public override IEnumerable<PluginIdentifier> Dependencies
        {
            get { return null; }
        }

        public override string Description
        {
            get { return "Dynamic Fracture Model Generator: Petrel version"; }
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
                yield return new ModuleReference(typeof(DFMGenerator_Ocean.DFMGeneratorModule));
            }
        }

        public override string Name
        {
            get { return "DFM Generator"; }
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
