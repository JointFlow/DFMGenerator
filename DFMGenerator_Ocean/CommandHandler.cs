using System.Collections.Generic;
using Slb.Ocean.Petrel.Commands;
using Slb.Ocean.Petrel.Contexts;

namespace DFNGenerator_Ocean
{
    class CommandHandler : GroupCommandHandler
    {
        public static string ID = "OceanDFN.NewCommand";

        #region Methods

        public override bool CanExecute(Context context)
        {
            //TODO: Add your logic here
            return true;
        }

        public override void Execute(Context context)
        {
            //TODO: Add your logic here
        }

        public override IEnumerable<CommandItem> GetCommands(Context context)
        {
            //TODO: Add commands by template below
            //yield return new CommandItem("Command.Id");
            return null;
        }

        #endregion
    }
}