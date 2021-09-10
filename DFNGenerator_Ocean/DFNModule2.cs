using System;
using Slb.Ocean.Core;
using Slb.Ocean.Petrel;
using Slb.Ocean.Petrel.UI;
using Slb.Ocean.Petrel.Workflow;

namespace DFNGenerator_Ocean
{
    /// <summary>
    /// This class will control the lifecycle of the Module.
    /// The order of the methods are the same as the calling order.
    /// </summary>
    public class DFNModule2 : IModule
    {
        private Process m_dfngeneratorInstance;
        public DFNModule2()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        #region IModule Members

        /// <summary>
        /// This method runs once in the Module life; when it loaded into the petrel.
        /// This method called first.
        /// </summary>
        public void Initialize()
        {
            // TODO:  Add DFNModule2.Initialize implementation
            //       this.BackColor = System.Drawing.Color.Aqua;
            CoreLogger.Info("Initializing DFN Generator");
        }

        /// <summary>
        /// This method runs once in the Module life. 
        /// In this method, you can do registrations of the not UI related components.
        /// (eg: datasource, plugin)
        /// </summary>
        public void Integrate()
        {
            CoreLogger.Info("Registering DFN Generator");
            // Register DFNGenerator_Ocean.DFNGenerator
            DFNGenerator_Ocean.DFNGenerator dfngeneratorInstance = new DFNGenerator_Ocean.DFNGenerator();
            PetrelSystem.WorkflowEditor.AddUIFactory<DFNGenerator_Ocean.DFNGenerator.Arguments>(new DFNGenerator_Ocean.DFNGenerator.UIFactory());
            PetrelSystem.WorkflowEditor.Add(dfngeneratorInstance);
            m_dfngeneratorInstance = new Slb.Ocean.Petrel.Workflow.WorkstepProcessWrapper(dfngeneratorInstance);
            PetrelSystem.ProcessDiagram.Add(m_dfngeneratorInstance, "Plug-ins");
            // Register OceanDFN.DFN_2
            //DFNGenerator_Ocean.DFN_2 dfn_2Instance = new DFNGenerator_Ocean.DFN_2();
            //PetrelSystem.WorkflowEditor.AddUIFactory<DFNGenerator_Ocean.DFN_2.Arguments>(new DFNGenerator_Ocean.DFN_2.UIFactory());
            //PetrelSystem.WorkflowEditor.Add(dfn_2Instance);
            // Register CommandHandler
            PetrelSystem.CommandManager.CreateCommand(DFNGenerator_Ocean.CommandHandler.ID, new DFNGenerator_Ocean.CommandHandler());
            // Register CommandHandler2
            PetrelSystem.CommandManager.CreateCommand(DFNGenerator_Ocean.CommandHandler2.ID, new DFNGenerator_Ocean.CommandHandler2());
            // Register OceanDFN.DFNWorkstep
            //DFNGenerator_Ocean.DFNWorkstep dfnworkstepInstance = new DFNGenerator_Ocean.DFNWorkstep();
            //PetrelSystem.WorkflowEditor.Add(dfnworkstepInstance);
            //PetrelSystem.ProcessDiagram.Add(new Slb.Ocean.Petrel.Workflow.WorkstepProcessWrapper(dfnworkstepInstance), "DFN Module");
 
            // TODO:  Add DFNModule2.Integrate implementation
        }

        /// <summary>
        /// This method runs once in the Module life. 
        /// In this method, you can do registrations of the UI related components.
        /// (eg: settingspages, treeextensions)
        /// </summary>
        public void IntegratePresentation()
        {
            // Add Ribbon Configuration file
            PetrelSystem.ConfigurationService.AddConfiguration(DFNGenerator_Ocean.Properties.Resources.OceanRibbonConfiguration);

            // TODO:  Add DFNModule2.IntegratePresentation implementation
        }

        /// <summary>
        /// This method runs once in the Module life.
        /// right before the module is unloaded. 
        /// It usually happens when the application is closing.
        /// </summary>
        public void Disintegrate()
        {
            // Unregister DFNGenerator_Ocean.DFNGenerator
            PetrelSystem.WorkflowEditor.RemoveUIFactory<DFNGenerator_Ocean.DFNGenerator.Arguments>();
            PetrelSystem.ProcessDiagram.Remove(m_dfngeneratorInstance);
            // Unregister OceanDFN.DFN_2
            //PetrelSystem.WorkflowEditor.RemoveUIFactory<DFNGenerator_Ocean.DFN_2.Arguments>();
            // TODO:  Add DFNModule2.Disintegrate implementation
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            // TODO:  Add DFNModule2.Dispose implementation
        }

        #endregion

    }


}