using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Slb.Ocean.Petrel.Commands;
using Slb.Ocean.Petrel;

namespace DFMGenerator_Ocean
{
    class LaunchDFMGenerator : SimpleCommandHandler
    {
        public static string ID = "DFMGenerator_Ocean.LaunchDFMGenerator";

        #region SimpleCommandHandler Members

        public override bool CanExecute(Slb.Ocean.Petrel.Contexts.Context context)
        { 
            return true;
        }

        public override void Execute(Slb.Ocean.Petrel.Contexts.Context context)
        {          
            //TODO: Add command execution logic here
        }
    
        #endregion
    }
}
