// Set this flag to output detailed information on new templates generated by the module
// Use for debugging only
//#define DEBUG_FRACS

using System;
using Slb.Ocean.Core;
using Slb.Ocean.Petrel;
using Slb.Ocean.Petrel.UI;
using Slb.Ocean.Petrel.Workflow;
using Slb.Ocean.Petrel.DomainObject;
using Slb.Ocean.Units;

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
            CoreLogger.Info("Initializing DFN Generator");

            //       this.BackColor = System.Drawing.Color.Aqua;

            // Register the WorkspaceEvents.Opened event and create an event handler that is called whenever a new project is opened
            DataManager.WorkspaceEvents.Opened += new System.EventHandler<WorkspaceEventArgs>(WSOpened);
        }

        /// <summary>
        /// This method runs once in the Module life. 
        /// In this method, you can do registrations of the not UI related components.
        /// (eg: datasource, plugin)
        /// </summary>
        public void Integrate()
        {
            CoreLogger.Info("Registering DFN Generator");

            // Register DFNGenerator_Ocean.DFNGenerator as a workstep and a process (using WorkstepProcessWrapper)
            DFNGenerator_Ocean.DFNGenerator dfngeneratorInstance = new DFNGenerator_Ocean.DFNGenerator();
            PetrelSystem.WorkflowEditor.AddUIFactory<DFNGenerator_Ocean.DFNGenerator.Arguments>(new DFNGenerator_Ocean.DFNGenerator.UIFactory());
            PetrelSystem.WorkflowEditor.Add(dfngeneratorInstance);
            m_dfngeneratorInstance = new Slb.Ocean.Petrel.Workflow.WorkstepProcessWrapper(dfngeneratorInstance);
            PetrelSystem.ProcessDiagram.Add(m_dfngeneratorInstance, "Fracture network modeling");

            // Register LaunchDFNGenerator Command Handler
            // This is currently not used as the process is launched via the Core.Services.ShowHideProcessDialog command
            PetrelSystem.CommandManager.CreateCommand(DFNGenerator_Ocean.LaunchDFNGenerator.ID, new DFNGenerator_Ocean.LaunchDFNGenerator());
        }

        /// <summary>
        /// This method runs once in the Module life. 
        /// In this method, you can do registrations of the UI related components.
        /// (eg: settingspages, treeextensions)
        /// </summary>
        public void IntegratePresentation()
        {
            // Add Ribbon Configuration file
            PetrelSystem.ConfigurationService.AddConfiguration(DFNGenerator_Ocean.Properties.Resources.DFNGeneratorConfig);

            // Add help content via PetrelSystem.HelpService
            string helpDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            HelpService helpService = PetrelSystem.HelpService;
            PluginHelpManifest helpContentMain = new PluginHelpManifest(System.IO.Path.Combine(helpDirectory, @"HelpFiles\DFN_Generator_Petrel_UserGuide_v2.htm"))
            {
                Text = "DFN Generator Help",
            };
            helpService.Add(helpContentMain);
            PluginHelpManifest helpContentPDF = new PluginHelpManifest(System.IO.Path.Combine(helpDirectory, @"HelpFiles\DFN_Generator_Petrel_UserGuide_v2.pdf"))
            {
                Text = "DFN Generator Help, pdf format",
            };
            helpService.Add(helpContentPDF);
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

            // Remove the help content
            string helpDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            HelpService helpService = PetrelSystem.HelpService;
            PluginHelpManifest helpContentMain = new PluginHelpManifest(System.IO.Path.Combine(helpDirectory, @"HelpFiles\DFN_Generator_Petrel_UserGuide_v2.htm"));
            helpService.Remove(helpContentMain);
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            // TODO:  Add DFNModule2.Dispose implementation
        }

        #endregion

        #region Event Handlers and auxiliary functions

        /// <summary>
        /// Event handler for the WorkspaceEvents.Opened event that is called whenever a new project is opened
        /// </summary>
        /// <param name="s"></param>
        /// <param name="E"></param>
        private void WSOpened(object s, WorkspaceEventArgs E)
        {
            // Create new templates for P30 and P32 fracture intensity, if they do not already exist
            GetP30Template();
            GetP32Template();
        }

        /// <summary>
        /// Return a consistent GUID for the P30 fracture intensity template
        /// </summary>
        private static Guid P30Template_GUID
        {
            get
            {
                return new Guid("62c333f1-5f96-4c8b-ac51-62855aa4468f");
            }
        }

        /// <summary>
        /// Return a consistent GUID for the P32 fracture intensity template
        /// </summary>
        private static Guid P32Template_GUID
        {
            get
            {
                return new Guid("f1ff3a24-6392-4166-a5a4-98240d71e2af");
            }
        }

        /// <summary>
        /// Return a template for the P30 fracture intensity; create a new one if one does not already exist
        /// </summary>
        /// <returns>Slb.Ocean.Petrel.DomainObject.Template for P30 fracture intensity</returns>
        public static Template GetP30Template()
        {
            // Get the template, if it already exists
            Template outputTemplate = PetrelSystem.TemplateService.FindTemplateByGuid(P30Template_GUID);

            // If not, we need to create it
            if (outputTemplate == null)
            {
                // Use the dimensionless fracture intensity template as a base
                Template fractureIntensityReferenceTemplate = PetrelProject.WellKnownTemplates.MiscellaneousGroup.Intensity;
                ITemplateSettingsInfoFactory fractureIntensitySettingsFactory = CoreSystem.GetService<ITemplateSettingsInfoFactory>(fractureIntensityReferenceTemplate);
                TemplateSettingsInfo fractureIntensityTemplateSettings = fractureIntensitySettingsFactory.GetTemplateSettingsInfo(fractureIntensityReferenceTemplate);

                // However we will put the new template in the  TemplateCollection
                TemplateCollection fractureIntensityTemplateCollection = PetrelProject.WellKnownTemplateCollections.FractureProperty;

                // Create a name for the new templates; if there is already a template with that name, return that instead
                string templateName = "P30 Fracture Intensity";
                Template existingTemplate = PetrelSystem.TemplateService.FindTemplateByName(templateName);
                if (existingTemplate != null)
                    return existingTemplate;

                // Get the units for the new templates; if we cannot find the correct units, return the dimensionless fracture intensity template instead
                IUnitMeasurement templateUnitSystem = PetrelUnitSystem.GetUnitMeasurement("Inverse_Volume");
                if (templateUnitSystem == null)
                    return fractureIntensityReferenceTemplate;

                // Create a transaction to create the new templates
                using (ITransaction transactionCreateNewTemplates = DataManager.NewTransaction())
                {
                    // Lock the database
                    transactionCreateNewTemplates.Lock(fractureIntensityTemplateCollection);

                    // Create the new template
                    outputTemplate = fractureIntensityTemplateCollection.CreateTemplate(templateName, fractureIntensityTemplateSettings.DefaultColorTable, templateUnitSystem, null, P30Template_GUID);
                    outputTemplate.Comments = "P30 fracture intensity: number of fractures per unit volume";
                    outputTemplate.TemplateType = fractureIntensityReferenceTemplate.TemplateType;
#if DEBUG_FRACS
                    PetrelLogger.InfoOutputWindow(string.Format("Created P30 fracture intensity template {0}: Type {1}, units {2}", outputTemplate.Name, outputTemplate.TemplateType.ToString(), outputTemplate.UnitMeasurement.ToString()));
#endif

                    // Commit the changes to the Petrel database
                    transactionCreateNewTemplates.Commit();
                }
            }

            // Return the template
            return outputTemplate;
        }

        /// <summary>
        /// Return a template for the P32 fracture intensity; create a new one if one does not already exist
        /// </summary>
        /// <returns>Slb.Ocean.Petrel.DomainObject.Template for P32 fracture intensity</returns>
        public static Template GetP32Template()
        {
            // Get the template, if it already exists
            Template outputTemplate = PetrelSystem.TemplateService.FindTemplateByGuid(P32Template_GUID);

            // If not, we need to create it
            if (outputTemplate == null)
            {
                // Use the dimensionless fracture intensity template as a base
                Template fractureIntensityReferenceTemplate = PetrelProject.WellKnownTemplates.MiscellaneousGroup.Intensity;
                ITemplateSettingsInfoFactory fractureIntensitySettingsFactory = CoreSystem.GetService<ITemplateSettingsInfoFactory>(fractureIntensityReferenceTemplate);
                TemplateSettingsInfo fractureIntensityTemplateSettings = fractureIntensitySettingsFactory.GetTemplateSettingsInfo(fractureIntensityReferenceTemplate);

                // However we will put the new template in the  TemplateCollection
                TemplateCollection fractureIntensityTemplateCollection = PetrelProject.WellKnownTemplateCollections.FractureProperty;

                // Create a name for the new templates; if there is already a template with that name, return that instead
                string templateName = "P32 Fracture Intensity";
                Template existingTemplate = PetrelSystem.TemplateService.FindTemplateByName(templateName);
                if (existingTemplate != null)
                    return existingTemplate;

                // Get the units for the new templates; if we cannot find the correct units, return the dimensionless fracture intensity template instead
                IUnitMeasurement templateUnitSystem = PetrelUnitSystem.GetUnitMeasurement("Inverse_Length");
                if (templateUnitSystem == null)
                    return fractureIntensityReferenceTemplate;

                // Create a transaction to create the new templates
                using (ITransaction transactionCreateNewTemplates = DataManager.NewTransaction())
                {
                    // Lock the database
                    transactionCreateNewTemplates.Lock(fractureIntensityTemplateCollection);

                    // Create the new template
                    outputTemplate = fractureIntensityTemplateCollection.CreateTemplate(templateName, fractureIntensityTemplateSettings.DefaultColorTable, templateUnitSystem, null, P32Template_GUID);
                    outputTemplate.Comments = "P32 fracture intensity: total fracture area per unit volume";
                    outputTemplate.TemplateType = fractureIntensityReferenceTemplate.TemplateType;
#if DEBUG_FRACS
                    PetrelLogger.InfoOutputWindow(string.Format("Created P32 fracture intensity template {0}: Type {1}, units {2}", outputTemplate.Name, outputTemplate.TemplateType.ToString(), outputTemplate.UnitMeasurement.ToString()));
#endif

                    // Commit the changes to the Petrel database
                    transactionCreateNewTemplates.Commit();
                }
            }

            // Return the template
            return outputTemplate;
        }
        #endregion
    }


}