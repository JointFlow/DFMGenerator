The OceanSDK installs a new context menu item into Visual Studio. 
It can be reached by right clicking on the DeployList.xml file in the Solution Explorer. 
For more help use "Editing the DeployList.xml" guide from OceanWizardsAndDevelopersTools.pdf.

Top 2 most common solutions
1. Context menu item "Edit DeployList.xml" does not display the editor form. 
Solution: In a Visual Studio Command Prompt window (Start > All programs > Microsoft Visual Studio > Visual Studio Tools > Command Prompt)
          enter "devenv /ResetAddin *". If this does not work, then enter "devenv /ResetSkipPkgs".

2. How do I add a 3rd party assembly to the assemblies deployed with my .PIP file? 
Solution: Drag the file from Windows Explorer or Solution Explorer to the left side of the DeployList Editor form. 

If you have any problem using DeployList Editor please contact Ocean Support. 