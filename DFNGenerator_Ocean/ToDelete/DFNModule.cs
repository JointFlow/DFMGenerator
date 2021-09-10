using System;
using Slb.Ocean.Core;
using Slb.Ocean.Petrel;
using Slb.Ocean.Petrel.UI;
using Slb.Ocean.Petrel.Workflow;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;


namespace DFNGenerator_Ocean
{
    /// <summary>
    /// This class will control the lifecycle of the Module.
    /// The order of the methods are the same as the calling order.
    /// </summary>
    public class DFNModule : IModule
    {
        private Process m_workstep2Instance;
        //private Process m_hellogridInstance;
        public DFNModule()
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
            // TODO:  Add DFNModule.Initialize implementation
            CoreLogger.Info(" HelloModule class : Initialize method");
        }

        /// <summary>
        /// This method runs once in the Module life. 
        /// In this method, you can do registrations of the not UI related components.
        /// (eg: datasource, plugin)
        /// </summary>
        public void Integrate()
        {
            // Register OceanDFN.Workstep2
            DFNGenerator_Ocean.Workstep2 workstep2Instance = new DFNGenerator_Ocean.Workstep2();
            PetrelSystem.WorkflowEditor.Add(workstep2Instance);
            m_workstep2Instance = new Slb.Ocean.Petrel.Workflow.WorkstepProcessWrapper(workstep2Instance);
            PetrelSystem.ProcessDiagram.Add(m_workstep2Instance, "Plug-ins");
            // Register OceanDFN.HelloGrid
            //OceanDFN.HelloGrid hellogridInstance = new OceanDFN.HelloGrid();
            //PetrelSystem.WorkflowEditor.Add(hellogridInstance);
            //m_hellogridInstance = new Slb.Ocean.Petrel.Workflow.WorkstepProcessWrapper(hellogridInstance);
            //PetrelSystem.ProcessDiagram.Add(m_hellogridInstance, "Plug-ins");

            // TODO:  Add DFNModule.Integrate implementation
            CoreLogger.Info(" HelloModule class : Integrate method");
        }

        /// <summary>
        /// This method runs once in the Module life. 
        /// In this method, you can do registrations of the UI related components.
        /// (eg: settingspages, treeextensions)
        /// </summary>
        public void IntegratePresentation()
        {

            // TODO:  Add DFNModule.IntegratePresentation implementation
            CoreLogger.Info(" HelloModule class : IntegratePresentation method");
        }

        /// <summary>
        /// This method runs once in the Module life.
        /// right before the module is unloaded. 
        /// It usually happens when the application is closing.
        /// </summary>
        public void Disintegrate()
        {
            PetrelSystem.ProcessDiagram.Remove(m_workstep2Instance);
            //PetrelSystem.ProcessDiagram.Remove(m_hellogridInstance);
            // TODO:  Add DFNModule.Disintegrate implementation
            CoreLogger.Info(" HelloModule class : Disintegrate method");
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            // TODO:  Add DFNModule.Dispose implementation
            CoreLogger.Info(" HelloModule class : Dispose method ");
        }

        #endregion

    }


}