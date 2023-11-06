#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Media.Media3D;

#endregion

namespace RAA_Int_Mod3_Dimensions_Challenge
{
    [Transaction(TransactionMode.Manual)]
    public class Command1 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // this is a variable for the Revit application
            UIApplication uiapp = commandData.Application;

            // this is a variable for the current Revit model
            Document doc = uiapp.ActiveUIDocument.Document;

            // Get grid lines
            FilteredElementCollector collector = new FilteredElementCollector(doc, doc.ActiveView.Id);
            collector.OfClass(typeof(Grid));

            // Create reference array and point list
            ReferenceArray vertReferenceArray = new ReferenceArray();
            List<XYZ> vertPointList = new List<XYZ>();
            ReferenceArray horizReferenceArray = new ReferenceArray();
            List<XYZ> horizPointList = new List<XYZ>();

            // Create dimensions
            using (Transaction t = new Transaction(doc))
            {
                t.Start("Create dimension lines");

                // Loop through lines
                foreach (Grid curLine in collector)
                {
                    // Check if line is vertical
                    Curve curve = curLine.Curve;
                    if (IsLineVertical(curve))
                    {
                        // Add lines to vertical ref array
                        vertReferenceArray.Append(new Reference(curLine));

                        // Add insertion point to vertical list
                        XYZ insPoint = curve.GetEndPoint(1) + new XYZ(0,-3,0);
                        vertPointList.Add(insPoint);
                    }
                    else
                    {
                        // Add lines to horizontal ref array
                        horizReferenceArray.Append(new Reference(curLine));

                        // Add insertion point to horizontal list
                        XYZ insPoint = curve.GetEndPoint(1) + new XYZ(3, 0, 0);
                        horizPointList.Add(insPoint);
                    }    
                }

                // Order vertical list left to right
                List<XYZ> vertSortedList = vertPointList.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
                XYZ vPoint1 = vertSortedList.First();
                XYZ vPoint2 = vertSortedList.Last();

                // Order horizontal list bottom to top
                List<XYZ> horizSortedList = horizPointList.OrderBy(p => p.Y).ThenBy(p => p.X).ToList();
                XYZ hPoint1 = horizSortedList.First();
                XYZ hPoint2 = horizSortedList.Last();

                // Create lines for dimensions
                Line vDimLine = Line.CreateBound(vPoint1, new XYZ(vPoint2.X, vPoint1.Y, 0));
                Line hDimLine = Line.CreateBound(hPoint1, new XYZ(hPoint1.X, hPoint2.Y, 0));

            
                Dimension vnewDim = doc.Create.NewDimension(doc.ActiveView, vDimLine, vertReferenceArray);
                Dimension hnewDim = doc.Create.NewDimension(doc.ActiveView, hDimLine, horizReferenceArray);
                t.Commit();
            }

            return Result.Succeeded;
        }
        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btnCommand1";
            string buttonTitle = "Button 1";

            ButtonDataClass myButtonData1 = new ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Blue_32,
                Properties.Resources.Blue_16,
                "This is a tooltip for Button 1");

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
    }
}
