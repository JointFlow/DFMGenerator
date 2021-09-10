//using System;

//using Slb.Ocean.Core;
//using Slb.Ocean.Petrel;
//using Slb.Ocean.Petrel.UI;
//using Slb.Ocean.Petrel.Workflow;
//using Slb.Ocean.Petrel.DomainObject.PillarGrid;

//using Slb.Ocean.Basics;
//using Slb.Ocean.Geometry;
//using Slb.Ocean.Petrel.DomainObject;
//using System.Collections.Generic;

//namespace OceanDFN
//{
//    /// <summary>
//    /// This class contains all the methods and subclasses of the HelloGrid.
//    /// Worksteps are displayed in the workflow editor.
//    /// </summary>
//    class HelloGrid : Workstep<HelloGrid.Arguments>, IExecutorSource, IAppearance, IDescriptionSource
//    {
//        #region Overridden Workstep methods

//        /// <summary>
//        /// Creates an empty Argument instance
//        /// </summary>
//        /// <returns>New Argument instance.</returns>

//        protected override HelloGrid.Arguments CreateArgumentPackageCore(IDataSourceManager dataSourceManager)
//        {
//            return new Arguments(dataSourceManager);
//        }
//        /// <summary>
//        /// Copies the Arguments instance.
//        /// </summary>
//        /// <param name="fromArgumentPackage">the source Arguments instance</param>
//        /// <param name="toArgumentPackage">the target Arguments instance</param>
//        protected override void CopyArgumentPackageCore(Arguments fromArgumentPackage, Arguments toArgumentPackage)
//        {
//            DescribedArgumentsHelper.Copy(fromArgumentPackage, toArgumentPackage);
//        }

//        /// <summary>
//        /// Gets the unique identifier for this Workstep.
//        /// </summary>
//        protected override string UniqueIdCore
//        {
//            get
//            {
//                return "82cbe274-a9b4-4a04-8150-bf1d2758a5b1";
//            }
//        }
//        #endregion

//        #region IExecutorSource Members and Executor class

//        /// <summary>
//        /// Creates the Executor instance for this workstep. This class will do the work of the Workstep.
//        /// </summary>
//        /// <param name="argumentPackage">the argumentpackage to pass to the Executor</param>
//        /// <param name="workflowRuntimeContext">the context to pass to the Executor</param>
//        /// <returns>The Executor instance.</returns>
//        public Slb.Ocean.Petrel.Workflow.Executor GetExecutor(object argumentPackage, WorkflowRuntimeContext workflowRuntimeContext)
//        {
//            return new Executor(argumentPackage as Arguments, workflowRuntimeContext);
//        }

//        public class Executor : Slb.Ocean.Petrel.Workflow.Executor
//        {
//            Arguments arguments;
//            WorkflowRuntimeContext context;

//            public Executor(Arguments arguments, WorkflowRuntimeContext context)
//            {
//                this.arguments = arguments;
//                this.context = context;
//            }

//            public override void ExecuteSimple()
//            {
//                // TODO: Implement the workstep logic here.
//                Grid grid = arguments.Grid;
//                if (grid == null)
//                {
//                    PetrelLogger.ErrorStatus("HelloGrid: Arguments cannot be empty.");
//                    return;
//                }

//                // General access:
//                // get the total number of cells in the grid
//                Index3 numcells = grid.NumCellsIJK;
//                arguments.NumCells = numcells.I * numcells.J * numcells.K;

//                // Get cell and volume
//                double px, py, pz;
//                px = PetrelUnitSystem.ConvertFromUI(Domain.X, arguments.PointX);
//                py = PetrelUnitSystem.ConvertFromUI(Domain.Y, arguments.PointY);
//                pz = PetrelUnitSystem.ConvertFromUI(grid.Domain, arguments.PointZ);
//                Point3 point = new Point3(px, py, pz);
//                Index3 cellIndex = grid.GetCellAtPoint(point);
//                if (cellIndex == null)
//                {
//                    PetrelLogger.InfoOutputWindow("the point is outside the grid");
//                    return;
//                }

//                arguments.CellVolume = double.NaN;
//                if (cellIndex != null)
//                {
//                    if (grid.HasCellVolume(cellIndex))
//                    {
//                        arguments.CellVolume = PetrelUnitSystem.ConvertToUI(PetrelUnitSystem.GetUnitMeasurement("Volume"),
//                                                                            grid.GetCellVolume(cellIndex));
//                    }
//                }

//                // get the cell corners
//                PetrelLogger.InfoOutputWindow("The corner points of the cell are: ");
//                IEnumerable<Point3> pts = grid.GetCellCorners(cellIndex, CellCornerSet.All);
//                IEnumerable<Point3> uipts = PetrelUnitSystem.ConvertToUI(grid.Domain, pts);
//                foreach (Point3 uip in uipts)
//                {
//                    PetrelLogger.InfoOutputWindow(uip.X + ", " + uip.Y + ", " + uip.Z);
//                }

//                // check if the node is defined at a given i,j,k location and direction.
//                Index3 index = new Index3(38, 84, 22);
//                Direction direction = Direction.NorthWest;
//                // convert internal index to UI index for printing message to user
//                Index3 uiIdx = ModelingUnitSystem.ConvertIndexToUI(grid, index);
//                // check if the node is inside the grid first, elsewise it will raise exception when using IsNodeDefined
//                if (!grid.IsCellInside(index))
//                {
//                    PetrelLogger.InfoOutputWindow("the node defined at " + uiIdx.ToString() + " is not in the grid");
//                    return;
//                }

//                if (grid.IsNodeDefined(index, direction))
//                    PetrelLogger.InfoOutputWindow("The node is defined at " + uiIdx.ToString() + " with NorthWest direction");

//                // check if the node is faulted at index 38, 84
//                Index2 nodeIndex = new Index2(38, 84);
//                if (grid.IsNodeFaulted(nodeIndex))
//                {
//                    // then get faults at the given node
//                    foreach (Fault fault in grid.GetPillarFaultsAtNode(nodeIndex))
//                        PetrelLogger.InfoOutputWindow("The fault name at node is " + fault.Description.Name);
//                }

//                // get all the horizons in the grid and output each one's name, type, and K index
//                PetrelLogger.InfoOutputWindow("\nThere are " + grid.HorizonCount + " horizons in the grid, and their names are:");
//                foreach (Horizon hz in grid.Horizons)
//                    PetrelLogger.InfoOutputWindow(hz.Name + " is of type " + hz.HorizonType.ToString() + " and is at K index " + hz.K);

//                // get all the zones in grid and output their names; these are hierachical
//                PetrelLogger.InfoOutputWindow("\nThere are " + grid.ZoneCount + " zones in the grid, and their names are:");
//                foreach (Zone z in grid.Zones)
//                {
//                    PetrelLogger.InfoOutputWindow(z.Name + " has a zone count of " + z.ZoneCount + " and contains these zones:");
//                    foreach (Zone subZone in z.Zones)
//                    { PetrelLogger.InfoOutputWindow(subZone.Name); }
//                }

//                //              PrintPillarFaultsInCollection(grid.FaultCollection);

//                // get all the segments in the grid and output their names and cell count
//                PetrelLogger.InfoOutputWindow("\nThere are " + grid.SegmentCount + " segments in the grid, and their names are:");
//                foreach (Segment seg in grid.Segments)
//                    PetrelLogger.InfoOutputWindow(seg.Name + " has a cell count of " + seg.CellCount);
//                return;
//            }

//            // get all faults in the fault collection and output their names, face count and node count
//            private void PrintPillarFaultsInCollection(FaultCollection faultCollection)
//            {
//                PetrelLogger.InfoOutputWindow("\nFault collection " + faultCollection.Name + " has a pillar fault count of " + faultCollection.PillarFaultCount);
//                foreach (PillarFault fault in faultCollection.PillarFaults)
//                    PetrelLogger.InfoOutputWindow(fault.Name + " has a face count of " + fault.FaceCount + " and a node count of " + fault.NodeCount);
//                foreach (FaultCollection fc in faultCollection.FaultCollections)
//                    PrintPillarFaultsInCollection(fc);
//            }








//        }

//        #endregion

//        /// <summary>
//        /// ArgumentPackage class for HelloGrid.
//        /// Each public property is an argument in the package.  The name, type and
//        /// input/output role are taken from the property and modified by any
//        /// attributes applied.
//        /// </summary>
//        public class Arguments : DescribedArgumentsByReflection
//        {
//            public Arguments()
//                : this(DataManager.DataSourceManager)
//            {                
//            }

//            public Arguments(IDataSourceManager dataSourceManager)
//            {
//            }

//            private Slb.Ocean.Petrel.DomainObject.PillarGrid.Grid grid;
//            private double pointX = 453829.1;
//            private double pointY = 6788392.2;
//            private double pointZ = -2024.4;
//            private int numCells;
//            private double cellVolume;

//            [Description("Grid", "Grid from a model")]
//            public Slb.Ocean.Petrel.DomainObject.PillarGrid.Grid Grid
//            {
//                internal get { return this.grid; }
//                set { this.grid = value; }
//            }

//            [Description("PointX", "Grid from a model")]
//            public double PointX
//            {
//                internal get { return this.pointX; }
//                set { this.pointX = value; }
//            }

//            [Description("PointY", "Y coord of point in the model")]
//            public double PointY
//            {
//                internal get { return this.pointY; }
//                set { this.pointY = value; }
//            }

//            [Description("PointZ", "Z coord of point in the model")]
//            public double PointZ
//            {
//                internal get { return this.pointZ; }
//                set { this.pointZ = value; }
//            }

//            [Description("numCells", "Number of cells in grid")]
//            public int NumCells
//            {
//                get { return this.numCells; }
//                internal set { this.numCells = value; }
//            }

//            [Description("cellVolume", "Volume of cell where point is")]
//            public double CellVolume
//            {
//                get { return this.cellVolume; }
//                internal set { this.cellVolume = value; }
//            }


//        }
    
//        #region IAppearance Members
//        public event EventHandler<TextChangedEventArgs> TextChanged;
//        protected void RaiseTextChanged()
//        {
//            if (this.TextChanged != null)
//                this.TextChanged(this, new TextChangedEventArgs(this));
//        }

//        public string Text
//        {
//            get { return Description.Name; }
//            private set 
//            {
//                // TODO: implement set
//                this.RaiseTextChanged();
//            }
//        }

//        public event EventHandler<ImageChangedEventArgs> ImageChanged;
//        protected void RaiseImageChanged()
//        {
//            if (this.ImageChanged != null)
//                this.ImageChanged(this, new ImageChangedEventArgs(this));
//        }

//        public System.Drawing.Bitmap Image
//        {
//            get { return PetrelImages.Modules; }
//            private set 
//            {
//                // TODO: implement set
//                this.RaiseImageChanged();
//            }
//        }
//        #endregion

//        #region IDescriptionSource Members

//        /// <summary>
//        /// Gets the description of the HelloGrid
//        /// </summary>
//        public IDescription Description
//        {
//            get { return HelloGridDescription.Instance; }
//        }

//        /// <summary>
//        /// This singleton class contains the description of the HelloGrid.
//        /// Contains Name, Shorter description and detailed description.
//        /// </summary>
//        public class HelloGridDescription : IDescription
//        {
//            /// <summary>
//            /// Contains the singleton instance.
//            /// </summary>
//            private  static HelloGridDescription instance = new HelloGridDescription();
//            /// <summary>
//            /// Gets the singleton instance of this Description class
//            /// </summary>
//            public static HelloGridDescription Instance
//            {
//                get { return instance; }
//            }

//            #region IDescription Members

//            /// <summary>
//            /// Gets the name of HelloGrid
//            /// </summary>
//            public string Name
//            {
//                get { return "HelloGrid"; }
//            }
//            /// <summary>
//            /// Gets the short description of HelloGrid
//            /// </summary>
//            public string ShortDescription
//            {
//                get { return "Hello Grid"; }
//            }
//            /// <summary>
//            /// Gets the detailed description of HelloGrid
//            /// </summary>
//            public string Description
//            {
//                get { return "Navigate a pillar grid and display information about it"; }
//            }

//            #endregion
//        }
//        #endregion


//    }
//}