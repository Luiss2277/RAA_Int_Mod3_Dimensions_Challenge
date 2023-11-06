#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using static Autodesk.Revit.DB.SpecTypeId;
using System.Text.RegularExpressions;

#endregion

namespace RAA_Int_Mod3_Dimensions_Challenge
{
    [Transaction(TransactionMode.Manual)]
    public class Command2 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // this is a variable for the Revit application
            UIApplication uiapp = commandData.Application;

            // this is a variable for the current Revit model
            Document doc = uiapp.ActiveUIDocument.Document;

            // Get all rooms in the document
            FilteredElementCollector collector = new FilteredElementCollector(doc, doc.ActiveView.Id);
            collector.OfCategory(BuiltInCategory.OST_Rooms);

            // Reference array and point list
            ReferenceArray vertReferenceArray = new ReferenceArray();
            ReferenceArray horizReferenceArray = new ReferenceArray();
            List<XYZ> vPointList = new List<XYZ>();
            List<XYZ> hPointList = new List<XYZ>();

            // Set options and get room boundaries
            SpatialElementBoundaryOptions options = new SpatialElementBoundaryOptions();
            options.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish;

            // Dimension counter
            int counter = 0;

            // Start transaction
            using (Transaction t = new Transaction(doc))
            {
                t.Start("Create dimensions");
                // Loop through rooms
                foreach (Room curRoom in collector)
                {
                    if (curRoom != null)
                    {
                        List<BoundarySegment> boundSegList = curRoom.GetBoundarySegments(options).First().ToList();
                        XYZ segPoint = FindRoomTag(doc, curRoom);

                        // Loop through room boundaries
                        foreach (BoundarySegment curSeg in boundSegList)
                        {
                            // Get boundary geometry
                            Curve boundCurve = curSeg.GetCurve();
                            XYZ midPoint = boundCurve.Evaluate(0.5, true);

                            // Check if line is vertical
                            if (IsLineVertical(boundCurve) == false)
                            {
                                // Get boundary wall
                                Element curWall = doc.GetElement(curSeg.ElementId);
                                if (curWall == null) { continue; }

                                // Add ref and point array
                                horizReferenceArray.Append(new Autodesk.Revit.DB.Reference(curWall));
                                hPointList.Add(new XYZ(segPoint.X, midPoint.Y, midPoint.Z));
                            }
                            else
                            {
                                // Get boundary wall
                                Element curWall = doc.GetElement(curSeg.ElementId);
                                if (curWall == null) { continue; }

                                // Add ref and point array
                                vertReferenceArray.Append(new Autodesk.Revit.DB.Reference(curWall));
                                vPointList.Add(new XYZ(midPoint.X, segPoint.Y, midPoint.Z));
                            }

                        }
                        // Create lines for dimensions
                        XYZ vPoint1 = vPointList.First();
                        XYZ vPoint2 = vPointList.Last();
                        XYZ hPoint1 = hPointList.First();
                        XYZ hPoint2 = hPointList.Last();
                        Line vdimLine = Line.CreateBound(vPoint1, new XYZ(vPoint2.X, vPoint1.Y, 0));
                        Line hdimLine = Line.CreateBound(hPoint1, new XYZ(hPoint1.X, hPoint2.Y, 0));

                        // Create dimensions
                        Dimension newDim1 = doc.Create.NewDimension(doc.ActiveView, vdimLine, vertReferenceArray);
                        counter++;
                        Dimension newDim2 = doc.Create.NewDimension(doc.ActiveView, hdimLine, horizReferenceArray);
                        counter++;
                    }
                }
                TaskDialog.Show("Test","Number of Dimensions created: " +  counter);
                t.Commit();
            }

            return Result.Succeeded;
        }
        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btnCommand2";
            string buttonTitle = "Button 2";

            ButtonDataClass myButtonData1 = new ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Blue_32,
                Properties.Resources.Blue_16,
                "This is a tooltip for Button 2");

            return myButtonData1.Data;
        }
        public bool IsLineVertical(Curve curLine)
        {
            XYZ p1 = curLine.GetEndPoint(0);
            XYZ p2 = curLine.GetEndPoint(1);

            if (Math.Abs(p1.X - p2.X) < Math.Abs(p1.Y - p2.Y))
                return true;
            return false;
        }
        public XYZ FindRoomTag(Document doc, Room room)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(SpatialElementTag));

            foreach (RoomTag tag in collector)
            {
                string temp = tag.TagText;
                Match match = Regex.Match(temp, @"\d+");
                if (match.Success)
                {
                    // Insert a space before the matched numbers
                    int index = match.Index;
                    temp = temp.Insert(index, " ");
                }

                if (room.Name == temp)
                {
                    // Get the location of the room tag
                    Location location = tag.Location;
                    LocationPoint locationPoint = location as LocationPoint;
                    XYZ tagLocation = locationPoint.Point;
                    XYZ offsetLoc = tagLocation + new XYZ(3, 3, 0);
                    return offsetLoc;
                }
            }

            return null;
        }

    }
}
