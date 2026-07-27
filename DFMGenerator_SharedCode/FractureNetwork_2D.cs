using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace DFMGenerator_SharedCode
{
    /// <summary>
    /// Represents an individual segment of a fracture trace
    /// </summary>
    class FractureTraceSegment_2D
    {
        // References to external objects
        /// <summary>
        /// References to FractureTraceSegment_2D objects representing fracture segments connected to each end of this segment, or null if there is no connected segment
        /// </summary>
        public FractureTraceSegment_2D[] ConnectedSegments { get; private set; }

        // Geometric data
        /// <summary>
        /// Locations of the two endpoints of the segment as PointXYZ objects
        /// </summary>
        public PointXYZ[] EndPoints { get; private set; }
        /// <summary>
        /// Check if there is another segment connected to one end of this segment
        /// </summary>
        /// <param name="endPoint">Index number of the endpoint to check - must be 0 or 1</param>
        /// <returns>True if there is a connected segment at the specified end, otherwise false</returns>
        public bool IsConnected(int endPoint)
        {
            if ((endPoint < 0) || (endPoint > 1))
                return false;
            else
                return !(ConnectedSegments[endPoint] is null);
        }

        // Reset and data input functions
        /// <summary>
        /// Check to see if this segment connects to another segment (i.e. if either of the endpoints have the same location); if so create cross-references in both segments 
        /// </summary>
        /// <param name="segmentToCheck">FractureTraceSegment_2D to check for connection</param>
        /// <returns>True if either of the endpoints of this segment have the same location as either of the endpoints of the other segment; otherwise false</returns>
        public bool CreateConnection(FractureTraceSegment_2D segmentToCheck)
        {
            // Loop through both endpoints on this segment
            for (int thisSegmentEndPoint = 0; thisSegmentEndPoint < 2; thisSegmentEndPoint++)
            {
                // If the endpoint is already connected, move on to the next
                if (IsConnected(thisSegmentEndPoint))
                    continue;

                // Loop through both endpoints on the supplied segment
                for (int segmentToCheckEndPoint = 0; segmentToCheckEndPoint < 2; segmentToCheckEndPoint++)
                {
                    // If the endpoint is already connected, move on to the next
                    if (segmentToCheck.IsConnected(segmentToCheckEndPoint))
                        continue;

                    // If the two endpoints have the same location, connect the segments and return true
                    if (PointXYZ.comparePoints(EndPoints[thisSegmentEndPoint], segmentToCheck.EndPoints[segmentToCheckEndPoint]))
                    {
                        ConnectedSegments[thisSegmentEndPoint] = segmentToCheck;
                        segmentToCheck.ConnectedSegments[segmentToCheckEndPoint] = this;

                        // Endpoints of this segment should always connect to the opposite endpoint of the adjacent segment (e.g. the start point of this segment should connect to the end point of the adjacent segment and vice versa)
                        // Therefore if both endpoints are at the same end of their respective segments, we need to swap the endpoints of the other segment
                        // This will also swap the connecting segment references of the adjacent segment
                        if (thisSegmentEndPoint == segmentToCheckEndPoint)
                            segmentToCheck.SwapEndpoints(this);

                        return true;
                    }
                }
            }

            // If the segments are not connected, return false
            return false;
        }
        /// <summary>
        /// Swap the endpoints of this segment and any connected segments
        /// This is used to ensure segment endpoints can always connect to the opposite endpoint of an adjacent segment
        /// </summary>
        /// <param name="callingSegment"></param>
        private void SwapEndpoints(FractureTraceSegment_2D callingSegment)
        {
            // Swap the end points
            PointXYZ tempEndpoint = EndPoints[0];
            EndPoints[0] = EndPoints[1];
            EndPoints[1] = tempEndpoint;

            // Swap the references to connecting segments
            FractureTraceSegment_2D tempSegment = ConnectedSegments[0];
            ConnectedSegments[0] = ConnectedSegments[1];
            ConnectedSegments[1] = tempSegment;

            // If there are connected segments, these must be swapped as well (but do not call back to the segment that called this swap)
            for (int endPoint = 0; endPoint < 2; endPoint++)
                if ((IsConnected(endPoint)) && !object.ReferenceEquals(ConnectedSegments[endPoint], callingSegment))
                    ConnectedSegments[endPoint].SwapEndpoints(this);
        }

        // Constructors
        /// <summary>
        /// Create a new fracture trace segment with specified endpoints
        /// </summary>
        /// <param name="endPoint1">PointXYZ object representing the first endpoint of the new segment</param>
        /// <param name="endpoint2">PointXYZ object representing the second endpoint of the new segment</param>
        public FractureTraceSegment_2D(PointXYZ endPoint1, PointXYZ endpoint2)
        {
            // Create a new endpoint array and populate it with the specified endpoints
            EndPoints = new PointXYZ[2];
            EndPoints[0] = endPoint1;
            EndPoints[1] = endpoint2;

            // Create a new array for the connected segments and assign them both null values
            ConnectedSegments = new FractureTraceSegment_2D[2];
            ConnectedSegments[0] = null;
            ConnectedSegments[1] = null;
        }
    }

    /// <summary>
    /// Represents a component of a fracture trace between two significant nodes (i.e. nodes with nodality != 2)
    /// </summary>
    class FractureTraceComponent_2D
    {
        // Unique fracture trace component ID number
        /// <summary>
        /// Global fracture trace component counter - used to set an ID for each new fracture trace component object
        /// </summary>
        private static int fractureTraceComponentCounter = 0;
        /// <summary>
        /// Unique fracture trace component ID 
        /// </summary>
        public int FractureTraceComponentID { get; private set; }

        // References to external objects
        /// <summary>
        /// Reference to the parent fracture trace
        /// </summary>
        private FractureTrace_2D ft;

        // Geometric data
        /// <summary>
        /// Get the total length of the fracture trace component
        /// </summary>
        public double Length
        {
            get
            {
                double length = 0;
                for (int segmentNo = 0; segmentNo < NoSegments; segmentNo++)
                {
                    double dX = Nodes[segmentNo + 1].X - Nodes[segmentNo].X;
                    double dY = Nodes[segmentNo + 1].Y - Nodes[segmentNo].Y;
                    double dZ = Nodes[segmentNo + 1].Z - Nodes[segmentNo].Z;
                    length += Math.Sqrt((dX * dX) + (dY * dY) + (dZ * dZ));
                }
                return length;
            }

        }
        /// <summary>
        /// Get the distance between the two endpoints of the fracture trace component
        /// This will be equal to the trace component length only if the trace component is straight
        /// </summary>
        public double DistanceBetweenEndPoints
        {
            get
            {
                double dX = Nodes[NoSegments].X - Nodes[0].X;
                double dY = Nodes[NoSegments].Y - Nodes[0].Y;
                double dZ = Nodes[NoSegments].Z - Nodes[0].Z;
                double length = Math.Sqrt((dX * dX) + (dY * dY) + (dZ * dZ));
                return length;
            }

        }
        /// <summary>
        /// Get the azimuth of the trace endpoint from the start point
        /// </summary>
        public double MeanAzimuth
        {
            get
            {
                double dX = Nodes[NoSegments].X - Nodes[0].X;
                double dY = Nodes[NoSegments].Y - Nodes[0].Y;
                // NB we must supply dX ad dY in the opposite order to the prescribed orientation as we want the bearing clockwise from the Y axis, not anticlockwise from the X axis
                double azimuth = Math.Atan2(dX, dY);
                // Since the directionality is not important, we only want to return values from 0 to PI
                if (azimuth < 0)
                    azimuth += Math.PI;
                return azimuth;
            }
        }
        /// <summary>
        /// Positions of the nodes along the trace component as PointXYZ objects
        /// </summary>
        public List<PointXYZ> Nodes { get; private set; }
        /// <summary>
        /// Get the number of nodes in the trace component
        /// </summary>
        public int NoNodes { get { return Nodes.Count; } }
        /// <summary>
        /// Get the number of segments in the trace component; this is one less than the number of nodes
        /// </summary>
        public int NoSegments { get { return (NoNodes > 0) ? NoNodes - 1 : 0; } }
        /// <summary>
        /// Number of segments (of this or other fracture traces) connected to each node
        /// </summary>
        public List<int> Nodality { get; private set; }
        /// <summary>
        /// Get the nodality of an endpoint of the fracture trace component
        /// </summary>
        /// <param name="endPoint">Index number of the endpoint to check - must be 0 or 1</param>
        /// <returns></returns>
        public int GetEndPointNodality(int endPoint)
        {
            if (NoNodes < 2)
                return 0;
            else if (endPoint == 0)
                return Nodality[0];
            else if (endPoint == 1)
                return Nodality[NoNodes - 1];
            else return 0;
        }
        /// <summary>
        /// Set the nodality of an endpoint of the fracture trace component
        /// </summary>
        /// <param name="endPoint">Index number of the endpoint to set - must be 0 or 1</param>
        /// <param name="nodalityValue">Nodality to set the endpoint to</param>
        public void SetEndPointNodality(int endPoint, int nodalityValue)
        {
            if (endPoint == 0)
                Nodality[0] = nodalityValue;
            else if (endPoint == 1)
                Nodality[NoNodes - 1] = nodalityValue;
        }
        /// <summary>
        /// Increase the nodality of an endpoint of the fracture trace component by 1
        /// </summary>
        /// <param name="endPoint">Index number of the endpoint to set - must be 0 or 1</param>
        /// <param name="nodalityValue">Nodality to set the endpoint to</param>
        public void IncrementEndPointNodality(int endPoint)
        {
            if (endPoint == 0)
                Nodality[0]++;
            else if (endPoint == 1)
                Nodality[NoNodes - 1]++;
        }
        /// <summary>
        /// Lists of references to fracture trace components (of this or other fracture traces) connected to each endpoint of this component 
        /// </summary>
        public List<FractureTraceComponent_2D>[] ConnectedFractureTraceComponents { get; private set; }

        // Output data
        /// <summary>
        /// Get a list of the IDs of all fracture trace components connected to this trace component
        /// </summary>
        /// <returns></returns>
        public List<int> GetConnectedTraceComponentIDs()
        {
            List<int> output = new List<int>();
            for (int endPoint = 0; endPoint < 2; endPoint++)
                foreach (FractureTraceComponent_2D connectedComponent in ConnectedFractureTraceComponents[endPoint])
                    output.Add(connectedComponent.FractureTraceComponentID);
            return output;
        }
        /// <summary>
        /// Get a list of the IDs of all fracture traces connected to this trace component
        /// </summary>
        /// <returns></returns>
        public List<int> GetConnectedTraceIDs()
        {
            List<int> output = new List<int>();
            for (int endPoint = 0; endPoint < 2; endPoint++)
                foreach (FractureTraceComponent_2D connectedComponent in ConnectedFractureTraceComponents[endPoint])
                    output.Add(connectedComponent.ft.FractureTraceID);

            // Sort the list of trace IDs and remove duplicates
            output.Sort();
            for (int listElementNo = (output.Count - 1); listElementNo > 0; listElementNo--)
            {
                if (output[listElementNo] == output[listElementNo - 1])
                    output.Remove(listElementNo);
            }

            return output;
        }
        public List<FractureTraceSegment_2D> GetFractureTraceSegments()
        {
            // Create an output list
            // If there are less than two nodes, return the empty list
            List<FractureTraceSegment_2D> output = new List<FractureTraceSegment_2D>();
            if (NoNodes < 2)
                return output;

            // Loop through the node list creating segments, and add them to the segment list
            PointXYZ startPoint = Nodes[0];
            for (int nodeNo = 1; nodeNo < NoNodes; nodeNo++)
            {
                PointXYZ endPoint = Nodes[nodeNo];
                output.Add(new FractureTraceSegment_2D(startPoint, endPoint));
                startPoint = endPoint;
            }

            // Loop through the segment list, connecting adjacent segments
            for (int segmentNo = 1; segmentNo < output.Count; segmentNo++)
            {
                output[segmentNo - 1].ConnectedSegments[1] = output[segmentNo];
                output[segmentNo].ConnectedSegments[0] = output[segmentNo - 1];
            }

            // Return the output list
            return output;
        }
        /// <summary>
        /// Get the fracture trace component geometry data (location and nodality of each of the nodes) in text format
        /// </summary>
        /// <returns>String with the fracture trace component geometry data, one line per node</returns>
        public string GetFractureTraceComponentGeometry()
        {
            // Create a new string for the output and add header data
            string output = string.Format("Fracture trace component index:\t{0}\tNumber of nodes:\t{1}\n\n", FractureTraceComponentID, NoNodes);

            // Loop through each trace component adding data for that component
            for (int nodeNo = 0; nodeNo < NoNodes; nodeNo++)
            {
                // Create a new string for the node
                string nodeInfo = string.Empty;
                // Add node location
                PointXYZ node = Nodes[nodeNo];
                nodeInfo += string.Format("{0}\t{1}\t{2}\t", node.X, node.Y, node.Z);
                // Add nodality
                nodeInfo += string.Format("{0}\n", Nodality[nodeNo]);
                // Add to the output string
                output += nodeInfo;
            }

            // Add a final empty line and return the output string
            output += "\n";
            return output;
        }
        /// <summary>
        /// Get the length, azimuth and connectivity data for the fracture trace component in text format
        /// </summary>
        /// <returns>String with the fracture trace component length, azimuth and connectivity data, including a list of connected trace component IDs</returns>
        public string GetFractureTraceComponentLengthConnectivityData()
        {
            // Get the trace component length and endpoint nodality data
            string output = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t", FractureTraceComponentID, NoSegments, Length, MeanAzimuth, GetEndPointNodality(0), GetEndPointNodality(1));

            // Add the IDs of the connected fracture trace components
            List<int> connectedComponentIDs = GetConnectedTraceComponentIDs();
            foreach (int componentID in connectedComponentIDs)
                output += string.Format("{0}\t", componentID);

            // Return the output string
            return output;
        }

        // Reset and data input functions
        /// <summary>
        /// Insert a new node into the fracture trace component without splitting it
        /// </summary>
        /// <param name="NewNode">PointXYZ object representing the location of the new node</param>
        /// <param name="SegmentToInsertNode">Index number (0-based) of the segment within which the new node should be inserted; if this is equal to the number of segments, the node will be appended</param>
        /// <returns>True if the new node was successfully inserted, otherwise false</returns>
        private bool InsertNode(PointXYZ NewNode, int SegmentToInsertNode)
        {
            if ((SegmentToInsertNode < 0) || (SegmentToInsertNode > NoSegments))
                return false;

            Nodes.Insert(SegmentToInsertNode + 1, NewNode);
            Nodality.Insert(SegmentToInsertNode + 1, 2);

            return true;
        }
        /// <summary>
        /// Split the fracture trace component into two at an existing node by increasing its nodality
        /// </summary>
        /// <param name="NodeToSplit">Index number (0-based) of the node where the fracture trace component should be split</param>
        /// <returns>FractureTraceComponent_2D object representing the second half of the fracture trace after the split</returns>
        public FractureTraceComponent_2D SplitFractureTraceComponent(int NodeToSplit)
        {
            int noNodesInNewComponent = NoNodes - NodeToSplit;
            if ((NodeToSplit < 1) || (noNodesInNewComponent < 2))
                return null;

            // Create a new FractureTraceComponent_2D object for the second half of this fracture trace component after the split
            FractureTraceComponent_2D newFractureTraceComponent = new FractureTraceComponent_2D(ft, Nodes.GetRange(NodeToSplit, noNodesInNewComponent));
            newFractureTraceComponent.ConnectedFractureTraceComponents[1] = ConnectedFractureTraceComponents[1];
            newFractureTraceComponent.SetEndPointNodality(0, Nodality[NodeToSplit]);
            newFractureTraceComponent.SetEndPointNodality(1, GetEndPointNodality(1));

            // Remove the nodes and nodality representing the component of the trace after the split from the appropriate lists
            Nodes.RemoveRange(NodeToSplit + 1, noNodesInNewComponent - 1);
            Nodality.RemoveRange(NodeToSplit + 1, noNodesInNewComponent - 1);
            ConnectedFractureTraceComponents[1] = new List<FractureTraceComponent_2D>();

            // Add a reference to each trace component to the list of trace components connected to the respective endpoint of the other trace component
            ConnectedFractureTraceComponents[1].Add(newFractureTraceComponent);
            newFractureTraceComponent.ConnectedFractureTraceComponents[0].Add(this);

            // Return the FractureTraceComponent_2D object for the second half of this fracture trace component after the split
            return newFractureTraceComponent;
        }
        /// <summary>
        /// Split the fracture trace component into two by inserting a new node with nodality > 2 into the fracture trace component
        /// </summary>
        /// <param name="NewNode">PointXYZ object representing the location of the new node</param>
        /// <param name="SegmentToInsertNode">Index number (0-based) of the segment within which the new node should be inserted</param>
        /// <returns>FractureTraceComponent_2D object representing the second half of the fracture trace after the split</returns>
        public FractureTraceComponent_2D SplitFractureTraceComponent(PointXYZ NewNode, int SegmentToInsertNode)
        {
            // Insert a new node at the required position
            InsertNode(NewNode, SegmentToInsertNode);

            // Split the fracture trace component at the new node and return the new trace component
            return SplitFractureTraceComponent(SegmentToInsertNode + 1);
        }

        // Constructors
        /// <summary>
        /// Base constructor: Create lists for the node locations, nodality and references to endpoints but do not populate them
        /// </summary>
        /// <param name="ft_in">Reference to the parent FractureTrace_2D object</param>
        private FractureTraceComponent_2D(FractureTrace_2D ft_in)
        {
            // Assign the new object an ID number and increment the fracture trace component counter
            FractureTraceComponentID = ++fractureTraceComponentCounter;

            // Set the reference to the parent fracture trace object
            ft = ft_in;

            // Create a new node list
            Nodes = new List<PointXYZ>();

            // Create a new nodality list
            Nodality = new List<int>();

            // Create a lists of references to the fracture trace components connected to each endpoint of this fracture trace component
            ConnectedFractureTraceComponents = new List<FractureTraceComponent_2D>[2];
            ConnectedFractureTraceComponents[0] = new List<FractureTraceComponent_2D>();
            ConnectedFractureTraceComponents[1] = new List<FractureTraceComponent_2D>();
        }
        /// <summary>
        /// Create a FractureTraceComponent object from a list of FractureTraceSegment_2D objects
        /// </summary>
        /// <param name="ft_in">Reference to the parent FractureTrace_2D object</param>
        /// <param name="FractureTraceSegments_in">List of FractureTraceSegment_2D objects representing the segments of the fracture trace component, in order</param>
        public FractureTraceComponent_2D(FractureTrace_2D ft_in, List<FractureTraceSegment_2D> FractureTraceSegments_in) : this(ft_in)
        {
            // Populate both lists
            // The Nodes list will be populated by the endpoints of the supplied fracture trace segments
            // The end nodes will have nodality 1 and the internal nodes will have nodality 2
            if (FractureTraceSegments_in.Count > 0)
            {
                Nodes.Add(FractureTraceSegments_in[0].EndPoints[0]);
                Nodality.Add(1);
            }
            for (int nodeNo = 0; nodeNo < FractureTraceSegments_in.Count; nodeNo++)
            {
                Nodes.Add(FractureTraceSegments_in[nodeNo].EndPoints[1]);
                if (nodeNo < (FractureTraceSegments_in.Count - 1))
                    Nodality.Add(2);
                else
                    Nodality.Add(1);
            }

        }
        /// <summary>
        /// Create a FractureTraceComponent object from a list of PointXYZ objects
        /// </summary>
        /// <param name="ft_in">Reference to the parent FractureTrace_2D object</param>
        /// <param name="FractureTraceSegments_in">List of PointXYZ objects representing the nodes of the fracture trace component, in order</param>
        public FractureTraceComponent_2D(FractureTrace_2D ft_in, List<PointXYZ> FractureTraceNodes_in) : this(ft_in)
        {
            // Populate both lists
            // The Nodes list will be populated by the supplied fracture nodes
            // The end nodes will have nodality 1 and the internal nodes will have nodality 2
            Nodes.AddRange(FractureTraceNodes_in);
            if (FractureTraceNodes_in.Count > 0)
                Nodality.Add(1);
            for (int nodeNo = 1; nodeNo < FractureTraceNodes_in.Count; nodeNo++)
            {
                if (nodeNo < (FractureTraceNodes_in.Count - 1))
                    Nodality.Add(2);
                else
                    Nodality.Add(1);
            }
        }
    }

    /// <summary>
    /// Represents the trace of an entire fracture on a 2D plane; may comprise multiple components bounded by significant nodes (i.e. nodes with nodality != 2) 
    /// </summary>
    class FractureTrace_2D
    {
        // Unique fracture trace ID number
        /// <summary>
        /// Global fracture trace counter - used to set an ID for each new fracture trace object
        /// </summary>
        private static int fractureTraceCounter = 0;
        /// <summary>
        /// Unique fracture trace ID 
        /// </summary>
        public int FractureTraceID { get; private set; }

        // References to external objects


        // Geometric data
        /// <summary>
        /// Components of the fracture trace
        /// Each component represents a section of the total fracture traces between two significant nodes (i.e. nodes with nodality != 2)
        /// </summary>
        public List<FractureTraceComponent_2D> TraceComponents { get; private set; }
        /// <summary>
        /// Get the number of components of the fracture trace
        /// </summary>
        public int NoTraceComponents { get { return TraceComponents.Count; } }

        // Fracture data
        /// <summary>
        /// Index number of the set to which the fracture belongs
        /// </summary>
        public int FractureSetIndex { get; protected set; }

        // Output data
        /// <summary>
        /// Get the fracture trace geometry data (location and nodality of each of the nodes in each trace component) in text format
        /// </summary>
        /// <returns>String with the fracture trace geometry data, one line per node</returns>
        public string GetFractureTraceGeometry()
        {
            // Create a new string for the output and add header data
            string output = string.Format("Fracture trace index:\t{0}\tFracture set index:\t{1}\tNumber of components:\t{2}\n\n", FractureTraceID, FractureSetIndex, NoTraceComponents);

            // Loop through each trace component adding data for that component
            foreach (FractureTraceComponent_2D traceComponent in TraceComponents)
                output += traceComponent.GetFractureTraceComponentGeometry();

            // Return the output string
            return output;
        }
        /// <summary>
        /// Get the length, azimuth and connectivity data for each fracture trace component in text format
        /// </summary>
        /// <returns>String with the fracture trace component length and connectivity data, one line per trace component</returns>
        public string GetTraceComponentLengthConnectivityData()
        {
            // Create a new string for the output
            string output = string.Empty;

            // Loop through each trace component; adding data for the trace before adding data for the specific trace component
            foreach (FractureTraceComponent_2D traceComponent in TraceComponents)
            {
                string componentOutput = string.Format("{0}\t{1}\t", FractureSetIndex, FractureTraceID);
                componentOutput += traceComponent.GetFractureTraceComponentLengthConnectivityData();
                componentOutput += "\n";
                output += componentOutput;
            }

            // Return the output string
            return output;
        }
        /// <summary>
        /// Get the length, azimuth and connectivity data for each fracture trace in text format
        /// </summary>
        /// <returns>String with the fracture trace length and connectivity data, one line per trace</returns>
        public string GetTraceLengthConnectivityData()
        {
            // Create a new string for the output and add fracture set, fracture trace ID and number of trace components
            string output = string.Format("{0}\t{1}\t{2}\t", FractureSetIndex, FractureTraceID, NoTraceComponents);

            // Get the total trace length and create a list of the connected trace IDs
            double length = 0;
            List<int> connectedTraceIDs = new List<int>();
            // Loop through each trace component, adding trace component length and connectivity data
            foreach (FractureTraceComponent_2D traceComponent in TraceComponents)
            {
                length += traceComponent.Length;
                connectedTraceIDs.AddRange(traceComponent.GetConnectedTraceIDs());
            }
            // Sort the list of trace ID and remove duplicates
            connectedTraceIDs.Sort();
            for (int listElementNo = 1; listElementNo < connectedTraceIDs.Count; listElementNo++)
            {
                if (connectedTraceIDs[listElementNo] == connectedTraceIDs[listElementNo - 1])
                    connectedTraceIDs.Remove(listElementNo);
            }
            // Add the length, number of connected fractures and list of connected fracture IDs to the output string
            output += string.Format("{0}\t{1}\t", length, connectedTraceIDs.Count);
            foreach (int traceID in connectedTraceIDs)
                output += string.Format("{0}\t", traceID);

            // Add a line return and return the output string
            output += "\n";
            return output;
        }

        // Reset and data input functions

        // Constructors
        /// <summary>
        /// Create an empty FractureTrace_2D object
        /// </summary>
        protected FractureTrace_2D()
        {
            // Assign the new object an ID number and increment the fracture trace counter
            FractureTraceID = ++fractureTraceCounter;

            // Create a new fracture trace component list
            TraceComponents = new List<FractureTraceComponent_2D>();
        }
    }

    class UnconfinedFractureTrace_2D : FractureTrace_2D
    {
        // References to external objects
        /// <summary>
        /// Reference to the 3D UnconfinedFractureXYZ object corresponding to this fracture trace
        /// </summary>
        private UnconfinedFractureXYZ ucf;

        // Output data

        // Reset and data input functions

        // Constructors
        /// <summary>
        /// Create a new unconfined fracture traces representing the intersection between a specified UnconfinedFractureXYZ and a horizontal plane
        /// </summary>
        /// <param name="UCF_in">UnconfinedFractureXYZ object that the trace will represent</param>
        /// <param name="DepthOfSection">Depth of the horizontal plane on which the trace lies</param>
        public UnconfinedFractureTrace_2D(UnconfinedFractureXYZ UCF_in, double DepthOfSection) : base()
        {
            // Set the reference to the 3D UnconfinedFractureXYZ object and the fracture set index
            ucf = UCF_in;
            FractureSetIndex = UCF_in.SetIndex;

            // Get the intersection points of each patch in the supplied unconfined fracture with a horizontal plane at the specified depth and convert these into FractureTraceSegment_2D objects
            List<FractureTraceSegment_2D> segments = new List<FractureTraceSegment_2D>();
            double planeZ = -DepthOfSection;

            // Get a list of fracture patches and loop through them
            List<PointXYZ[]> fracturePatches = UCF_in.GetFracturePatchesInXYZ(false);
            foreach (PointXYZ[] fracturePatch in fracturePatches)
            {
                // Create a listfor the intersection points 
                List<PointXYZ> intersectionPoints = new List<PointXYZ>();
                int noVertices = fracturePatch.Length;
                if (noVertices > 2)
                {
                    // Loop through each of the edges of the fracture patch to check if it intersects the specified plane
                    for (int edgeNo = 0; edgeNo < noVertices; edgeNo++)
                    {
                        PointXYZ edgeStartPoint = (edgeNo > 0) ? fracturePatch[edgeNo - 1] : fracturePatch[noVertices - 1];
                        PointXYZ edgeEndPoint = fracturePatch[edgeNo];

                        // Check if the edge intersects the horizontal plane
                        double z1 = edgeStartPoint.Z;
                        double z2 = edgeEndPoint.Z;
                        if (((z1 < planeZ) && (z2 < planeZ)) || ((z1 > planeZ) && (z2 > planeZ)))
                            continue;
                        if (z1 == z2)
                            continue;

                        // If so calculate the coordinates of the intersection
                        double intersectionZRatio = (planeZ - z1) / (z2 - z1);
                        double x1 = edgeStartPoint.X;
                        double x2 = edgeEndPoint.X;
                        double intersectionX = x1 + (intersectionZRatio * (x2 - x1));
                        double y1 = edgeStartPoint.Y;
                        double y2 = edgeEndPoint.Y;
                        double intersectionY = y1 + (intersectionZRatio * (y2 - y1));

                        // Create a new intersection point and add it to the list
                        intersectionPoints.Add(new PointXYZ(intersectionX, intersectionY, planeZ));
                    }
                }

                // If there are two different intersection points, add a new segment
                if ((intersectionPoints.Count == 2) && !PointXYZ.comparePoints(intersectionPoints[0], intersectionPoints[1]))
                    segments.Add(new FractureTraceSegment_2D(intersectionPoints[0], intersectionPoints[1]));
            }

            // Loop through all the segments checking for connections
            for (int segment1Index = 0; segment1Index < segments.Count; segment1Index++)
                for (int segment2Index = segment1Index + 1; segment2Index < segments.Count; segment2Index++)
                    segments[segment1Index].CreateConnection(segments[segment2Index]);

            // Extract chains of connected segments and use them to create FractureTraceComponent_2D objects
            while (segments.Count > 0)
            {
                // Create an ordered list of segments making up this fracture trace component, and get a reference to the initial segment for this trace component
                // NB this is just the first segment in the unordered list, it is not necessarily the first or last segment in the trace component
                List<FractureTraceSegment_2D> nextTraceComponentSegments = new List<FractureTraceSegment_2D>();
                FractureTraceSegment_2D initialSegment = segments[0];
                nextTraceComponentSegments.Add(initialSegment);
                segments.Remove(initialSegment);

                // Loop backwards and forwards through the trace component adding segments to the trace component list, and removing them from the unordered list, until either an unconnected endpoint is reached or the initial segment is reached (i.e. a loop is formed)
                FractureTraceSegment_2D nextSegment = initialSegment;
                int direction = 0;
                while (nextSegment.IsConnected(direction) && !object.ReferenceEquals(initialSegment, nextSegment.ConnectedSegments[direction]))
                {
                    nextSegment = nextSegment.ConnectedSegments[direction];
                    nextTraceComponentSegments.Insert(0, nextSegment);
                    segments.Remove(nextSegment);
                }
                nextSegment = initialSegment;
                direction = 1;
                while (nextSegment.IsConnected(direction) && !object.ReferenceEquals(initialSegment, nextSegment.ConnectedSegments[direction]))
                {
                    nextSegment = nextSegment.ConnectedSegments[direction];
                    nextTraceComponentSegments.Add(nextSegment);
                    segments.Remove(nextSegment);
                }

                // Create a new FractureTraceComponent_2D for the trace component and add it to the list
                TraceComponents.Add(new FractureTraceComponent_2D(this, nextTraceComponentSegments));
            }
        }
    }

    /// <summary>
    /// Network of 2D fracture traces on a specified plane
    /// </summary>
    class FractureNetwork_2D
    {
        // References to external objects
        /// <summary>
        /// Reference to the GlobalDFN object representing the 3D fracture network corresponding to these fracture traces
        /// </summary>
        private GlobalDFN gdfn;
        /// <summary>
        /// Reference to the grandparent Grid object
        /// </summary>
        private FractureGrid gd;
        /// <summary>
        /// End time of last timestep used to generate this DFN
        /// </summary>
        public double CurrentTime { get { return gdfn.CurrentTime; } }

        // Fracture Data
        /// <summary>
        /// Plane of section; the fracture traces represent the intersection of the 3D fractures with this plane
        /// </summary>
        public PlaneXYZ PlaneOfSection { get; private set; }
        /// <summary>
        /// Number of fracture traces in the fracture network
        /// </summary>
        public int NoTraces { get { return FractureTraces.Count; } }
        /// <summary>
        /// List of fracture traces
        /// </summary>
        public List<FractureTrace_2D> FractureTraces { get; private set; }
        /// <summary>
        /// Value indicating the allowable mismatch for a fracture trace segment to be considered to intersect another trace segment, as a proportion of the segment length
        /// </summary>
        private const double IntersectionTolerance = 0.01;

        // Output data
        /// <summary>
        /// Write the fracture trace geometry data (location and nodality of each of the nodes in each trace component) to a text file
        /// </summary>
        /// <param name="outputFileLabel">Label representing the model name and stage to include in the output file names</param>
        public void WriteTraceGeometryToFile(string outputFileLabel)
        {
            // Only text file output is currently supported
            string fractureFileExtension = ".txt";

            // Create output file for fracture trace geometry information
            string fileName = "TraceGeometry_" + outputFileLabel + fractureFileExtension;
            String namecomb = gd.DFNControl.FolderPath + fileName;
            StreamWriter traceGeometry_outputFile = new StreamWriter(namecomb);

            // Write the header line
            traceGeometry_outputFile.WriteLine("Node X\tNode Y\tNode Z\tNumber of connected nodes\n");

            // Write the geometry data for each fracture trace
            foreach (FractureTrace_2D trace in FractureTraces)
                traceGeometry_outputFile.Write(trace.GetFractureTraceGeometry());

            // Close fracture trace geometry output file
            traceGeometry_outputFile.Close();
        }
        /// <summary>
        /// Write the fracture trace component length and connectivity data (trace component length, mean azimuth, nodality and list of connected fracture traces at the trace component endpoints) to a text file
        /// </summary>
        /// <param name="outputFileLabel">Label representing the model name and stage to include in the output file names</param>
        public void WriteTraceComponentDataToFile(string outputFileLabel)
        {
            // Only text file output is currently supported
            string fractureFileExtension = ".txt";

            // Create output file for fracture trace geometry information
            string fileName = "TraceComponentData_" + outputFileLabel + fractureFileExtension;
            String namecomb = gd.DFNControl.FolderPath + fileName;
            StreamWriter traceGeometry_outputFile = new StreamWriter(namecomb);

            // Write the header line
            traceGeometry_outputFile.WriteLine("Fracture set\tFracture trace ID\tFracture trace component ID\tNumber of segments\tLength\tMean azimuth\tStart point nodality\tEnd point nodality\tList of connected trace component IDs\n");

            // Write the geometry data for each fracture trace
            foreach (FractureTrace_2D trace in FractureTraces)
                traceGeometry_outputFile.Write(trace.GetTraceComponentLengthConnectivityData());

            // Close fracture trace geometry output file
            traceGeometry_outputFile.Close();
        }
        /// <summary>
        /// Write the fracture trace length and connectivity data (trace length, number of connected fracture traces and list of connected fracture traces at the trace component endpoints) to a text file
        /// </summary>
        /// <param name="outputFileLabel">Label representing the model name and stage to include in the output file names</param>
        public void WriteTraceDataToFile(string outputFileLabel)
        {
            // Only text file output is currently supported
            string fractureFileExtension = ".txt";

            // Create output file for fracture trace geometry information
            string fileName = "TraceData_" + outputFileLabel + fractureFileExtension;
            String namecomb = gd.DFNControl.FolderPath + fileName;
            StreamWriter traceGeometry_outputFile = new StreamWriter(namecomb);

            // Write the header line
            traceGeometry_outputFile.WriteLine("Fracture set\tFracture trace ID\tNumber of trace components\tTotal length\tNumber of connected fracture traces\tList of connected trace IDs\n");

            // Write the geometry data for each fracture trace
            foreach (FractureTrace_2D trace in FractureTraces)
                traceGeometry_outputFile.Write(trace.GetTraceLengthConnectivityData());

            // Close fracture trace geometry output file
            traceGeometry_outputFile.Close();
        }

        // Reset and data input functions

        // Constructors

        /// <summary>
        /// Create a fracture network object comprising the traces of all fractures from a specified DFN on a specified horizontal plane
        /// </summary>
        /// <param name="DFN_in">GlobalDFN object representing the DFN</param>
        /// <param name="gd_in">FractureGrid object representing the 3D grid</param>
        /// <param name="DepthOfSection">Depth of the horizontal plane on which the traces lie</param>
        public FractureNetwork_2D(GlobalDFN DFN_in, FractureGrid gd_in, double DepthOfSection)
        {
            // Set the reference to the GlobalDFN and FractureGrid objects
            gdfn = DFN_in;
            gd = gd_in;

            // Create a PlaneXYZ object representing the horizontal section at the specified depth
            PlaneOfSection = new PlaneXYZ(new PointXYZ(0, 0, -DepthOfSection), new VectorXYZ(0, 0, 1));

            // Create a new list object for the fracture traces
            FractureTraces = new List<FractureTrace_2D>();

            // Extract the traces of all microfractures in the specified DFN and add them to the fracture trace list
            // TO DO

            // Extract the traces of all layer-bound macrofractures in the specified DFN and add them to the fracture trace list
            // TO DO

            // Extract the traces of all unconfined fractures in the specified DFN and add them to the fracture trace list
            foreach (UnconfinedFractureXYZ ucf in DFN_in.GlobalDFNUnconfinedFractures)
            {
                UnconfinedFractureTrace_2D ucfTrace = new UnconfinedFractureTrace_2D(ucf, DepthOfSection);
                // If the fracture does not intersect the specified horizontal plane, the trace will contain no trace components
                // In this case it should not be added to the trace component list
                if (ucfTrace.NoTraceComponents > 0)
                    FractureTraces.Add(ucfTrace);
            }

            // Check for intersections and crossing points between traces and insert new nodes where required
            // This requires cross-checking every segment of every fracture trace component against every other segment of every other fracture trace component
            // NB we do not need to cross-check different components of the same fracture trace
            // Outer loop through traces, components and segments
            for (int trace1No = 0; trace1No < NoTraces; trace1No++)
            {
                FractureTrace_2D trace1 = FractureTraces[trace1No];
                for (int traceComponent1No = 0; traceComponent1No < trace1.NoTraceComponents; traceComponent1No++)
                {
                    FractureTraceComponent_2D traceComponent1 = trace1.TraceComponents[traceComponent1No];
                    for (int traceSegment1No = 0; traceSegment1No < traceComponent1.NoSegments; traceSegment1No++)
                    {
                        PointXYZ traceSegment1StartPoint = traceComponent1.Nodes[traceSegment1No];
                        PointXYZ traceSegment1EndPoint = traceComponent1.Nodes[traceSegment1No + 1];

                        // Inner loop through traces, components and segments
                        for (int trace2No = trace1No + 1; trace2No < NoTraces; trace2No++)
                        {
                            FractureTrace_2D trace2 = FractureTraces[trace2No];
                            for (int traceComponent2No = 0; traceComponent2No < trace2.NoTraceComponents; traceComponent2No++)
                            {
                                FractureTraceComponent_2D traceComponent2 = trace2.TraceComponents[traceComponent2No];

                                // Check if trace component 2 is already connected to trace component 1; if so move on to the next trace component
                                if (traceComponent2.ConnectedFractureTraceComponents[0].Contains(traceComponent1) || traceComponent2.ConnectedFractureTraceComponents[1].Contains(traceComponent1))
                                    continue;

                                for (int traceSegment2No = 0; traceSegment2No < traceComponent2.NoSegments; traceSegment2No++)
                                {
                                    PointXYZ traceSegment2StartPoint = traceComponent2.Nodes[traceSegment2No];
                                    PointXYZ traceSegment2EndPoint = traceComponent2.Nodes[traceSegment2No + 1];

                                    // Check if the two segments cross, within the defined tolerance
                                    bool[] intersectsPoint;
                                    PointXYZ crossoverPoint = PointXYZ.get2DCrossoverPoint(traceSegment1StartPoint, traceSegment1EndPoint, traceSegment2StartPoint, traceSegment2EndPoint, CrossoverType.Restrict, IntersectionTolerance, out intersectsPoint);

                                    // If the returned value is null, the segments do not cross so we can move on to the next
                                    if (crossoverPoint is null)
                                        continue;

                                    // Create flags to indicate whether the intersection point lies at the endpoint of either of the trace components
                                    // If it does it will form a Y node, otherwise it will form an X node
                                    bool traceSegment1StartPointIsEndNode = false;
                                    bool traceSegment1EndPointIsEndNode = false;
                                    bool traceSegment2StartPointIsEndNode = false;
                                    bool traceSegment2EndPointIsEndNode = false;

                                    // Split the two traces and add new nodes if neccesary
                                    FractureTraceComponent_2D newTrace1Component, newTrace2Component;
                                    if (intersectsPoint[0])
                                    {
                                        // The intersection is at the start point of trace segment 1
                                        newTrace1Component = traceComponent1.SplitFractureTraceComponent(traceSegment1No);
                                        // If no new trace component is returned, the intersection must be at the start point of trace component 1
                                        if (newTrace1Component is null)
                                            traceSegment1StartPointIsEndNode = true;
                                    }
                                    else if (intersectsPoint[1])
                                    {
                                        // The intersection is at the end point of trace segment 1
                                        newTrace1Component = traceComponent1.SplitFractureTraceComponent(traceSegment1No + 1);
                                        // If no new trace component is returned, the intersection must be at the end point of trace component 1
                                        if (newTrace1Component is null)
                                            traceSegment1EndPointIsEndNode = true;
                                    }
                                    else
                                    {
                                        // The intersection lies within trace segment 1 so a new node must be inserted
                                        newTrace1Component = traceComponent1.SplitFractureTraceComponent(crossoverPoint, traceSegment1No);
                                    }
                                    if (intersectsPoint[2])
                                    {
                                        // The intersection is at the start point of trace segment 2
                                        newTrace2Component = traceComponent2.SplitFractureTraceComponent(traceSegment2No);
                                        // If no new trace component is returned, the intersection must be at the start point of trace component 2
                                        if (newTrace2Component is null)
                                            traceSegment2StartPointIsEndNode = true;
                                    }
                                    else if (intersectsPoint[3])
                                    {
                                        // The intersection is at the end point of trace segment 2
                                        newTrace2Component = traceComponent2.SplitFractureTraceComponent(traceSegment2No + 1);
                                        // If no new trace component is returned, the intersection must be at the end point of trace component 2
                                        if (newTrace2Component is null)
                                            traceSegment2EndPointIsEndNode = true;
                                    }
                                    else
                                    {
                                        // The intersection lies within trace segment 2 so a new node must be inserted
                                        newTrace2Component = traceComponent2.SplitFractureTraceComponent(crossoverPoint, traceSegment2No);
                                    }

                                    // Update the nodality and connection information for all trace segments
                                    if (traceSegment1StartPointIsEndNode)
                                    {
                                        // Increment the nodality of the start point of trace component 1 and add a reference to trace component 2
                                        traceComponent1.IncrementEndPointNodality(0);
                                        traceComponent1.ConnectedFractureTraceComponents[0].Add(traceComponent2);
                                        // If the intersection does not lie at the start or end points of trace component 2, then it is an Y node so we must further increment the nodality of the start point of trace component 1 and add a reference to the new trace 2 component
                                        if (!traceSegment2StartPointIsEndNode && !traceSegment2EndPointIsEndNode)
                                        {
                                            traceComponent1.IncrementEndPointNodality(0);
                                            traceComponent1.ConnectedFractureTraceComponents[0].Add(newTrace2Component);
                                        }
                                    }
                                    else if (traceSegment1EndPointIsEndNode)
                                    {
                                        // Increment the nodality of the end point of trace component 1 and add a reference to trace component 2
                                        traceComponent1.IncrementEndPointNodality(1);
                                        traceComponent1.ConnectedFractureTraceComponents[1].Add(traceComponent2);
                                        // If the intersection does not lie at the start or end points of trace component 2, then it is an Y node so we must further increment the nodality of the start point of trace component 1 and add a reference to the new trace 2 component
                                        if (!traceSegment2StartPointIsEndNode && !traceSegment2EndPointIsEndNode)
                                        {
                                            traceComponent1.IncrementEndPointNodality(1);
                                            traceComponent1.ConnectedFractureTraceComponents[1].Add(newTrace2Component);
                                        }
                                    }
                                    else
                                    {
                                        // Increment the nodality of both new trace 1 component endpoints and add references to trace component 2
                                        traceComponent1.IncrementEndPointNodality(1);
                                        newTrace1Component.IncrementEndPointNodality(0);
                                        traceComponent1.ConnectedFractureTraceComponents[1].Add(traceComponent2);
                                        newTrace1Component.ConnectedFractureTraceComponents[0].Add(traceComponent2);
                                        // If the intersection does not lie at the start or end points of trace component 2, then it is an X node so we must further increment the nodality of both new trace 1 component endpoints and add references to the new trace 2 component
                                        if (!traceSegment2StartPointIsEndNode && !traceSegment2EndPointIsEndNode)
                                        {
                                            traceComponent1.IncrementEndPointNodality(1);
                                            newTrace1Component.IncrementEndPointNodality(0);
                                            traceComponent1.ConnectedFractureTraceComponents[1].Add(newTrace2Component);
                                            newTrace1Component.ConnectedFractureTraceComponents[0].Add(newTrace2Component);
                                        }
                                    }
                                    if (traceSegment2StartPointIsEndNode)
                                    {
                                        // Increment the nodality of the start point of trace component 2 and add a reference to trace component 1
                                        traceComponent2.IncrementEndPointNodality(0);
                                        traceComponent2.ConnectedFractureTraceComponents[0].Add(traceComponent1);
                                        // If the intersection does not lie at the start or end points of trace component 1, then it is an Y node so we must further increment the nodality of the start point of trace component 2 and add a reference to the new trace 1 component
                                        if (!traceSegment1StartPointIsEndNode && !traceSegment1EndPointIsEndNode)
                                        {
                                            traceComponent2.IncrementEndPointNodality(0);
                                            traceComponent2.ConnectedFractureTraceComponents[0].Add(newTrace1Component);
                                        }
                                    }
                                    else if (traceSegment2EndPointIsEndNode)
                                    {
                                        // Increment the nodality of the start point of trace component 2 and add a reference to trace component 1
                                        traceComponent2.IncrementEndPointNodality(1);
                                        traceComponent2.ConnectedFractureTraceComponents[1].Add(traceComponent1);
                                        // If the intersection does not lie at the start or end points of trace component 1, then it is an Y node so we must further increment the nodality of the start point of trace component 2 and add a reference to the new trace 1 component
                                        if (!traceSegment1StartPointIsEndNode && !traceSegment1EndPointIsEndNode)
                                        {
                                            traceComponent2.IncrementEndPointNodality(1);
                                            traceComponent2.ConnectedFractureTraceComponents[1].Add(newTrace1Component);
                                        }
                                    }
                                    else
                                    {
                                        // Increment the nodality of both new trace 2 component endpoints and add references to trace component 1
                                        traceComponent2.IncrementEndPointNodality(1);
                                        newTrace2Component.IncrementEndPointNodality(0);
                                        traceComponent2.ConnectedFractureTraceComponents[1].Add(traceComponent1);
                                        newTrace2Component.ConnectedFractureTraceComponents[0].Add(traceComponent1);
                                        // If the intersection does not lie at the start or end points of trace component 1, then it is an X node so we must further increment the nodality of both new trace 2 component endpoints and add references to the new trace 1 component
                                        if (!traceSegment1StartPointIsEndNode && !traceSegment1EndPointIsEndNode)
                                        {
                                            traceComponent2.IncrementEndPointNodality(1);
                                            newTrace2Component.IncrementEndPointNodality(0);
                                            traceComponent2.ConnectedFractureTraceComponents[1].Add(newTrace1Component);
                                            newTrace2Component.ConnectedFractureTraceComponents[0].Add(newTrace1Component);
                                        }
                                    }

                                    // Add any new trace components to the component arrays for each trace
                                    if (!traceSegment1StartPointIsEndNode && !traceSegment1EndPointIsEndNode)
                                    {
                                        trace1.TraceComponents.Add(newTrace1Component);
                                        // Update the start and end points of TraceSegment1 - these will have changed
                                        traceSegment1StartPoint = traceComponent1.Nodes[traceSegment1No];
                                        traceSegment1EndPoint = traceComponent1.Nodes[traceSegment1No + 1];
                                    }
                                    if (!traceSegment2StartPointIsEndNode && !traceSegment2EndPointIsEndNode)
                                    {
                                        trace2.TraceComponents.Add(newTrace2Component);
                                    }
                                }
                            }
                        } // End inner loop through traces, components and segments
                    }
                }
            } // End outer loop through traces, components and segments
        }
    }
}
