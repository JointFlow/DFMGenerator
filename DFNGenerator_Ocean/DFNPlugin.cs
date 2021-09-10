using System;
using System.Collections.Generic;

using Slb.Ocean.Core;

namespace DFNGenerator_Ocean
{
    public class DFNPlugin : Slb.Ocean.Core.Plugin
    {
        public override string AppVersion
        {
            get { return "2017.0"; }
        }

        public override string Author
        {
            get { return "Michael Welch and Mikael Lüthje"; }
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
            get { return new Uri("http://www.oilgas.dtu.dk/"); }
        }

        public override IEnumerable<ModuleReference> Modules
        {
            get 
            {
                yield return new ModuleReference(typeof(DFNGenerator_Ocean.DFNModule2));
                //yield return new ModuleReference(typeof(DFNGenerator_Ocean.DFNModule)); 
                // Please fill this method with your modules with lines like this:
                //yield return new ModuleReference(typeof(Module));

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
