using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreationModelPlugin
{
    [Transaction(TransactionMode.Manual)]

    public class CreationModel : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            List<Level> listLevel;
            Level level1 = null;
            Level level2 = null;
            Wall wall = null;
            List<Wall> walls = null;

            CreateLevels();
            CreateWalls();
            AddDoor();
            AddWindow();

            void CreateLevels()
            {
                listLevel = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .OfType<Level>()
                .ToList();

                level1 = listLevel
                .Where(x => x.Name.Equals("Level 1"))
                .FirstOrDefault();

                level2 = listLevel
                    .Where(x => x.Name.Equals("Level 2"))
                    .FirstOrDefault();
            }

            void CreateWalls()
            {
                double width = UnitUtils.ConvertToInternalUnits(10000, UnitTypeId.Millimeters);
                double depth = UnitUtils.ConvertToInternalUnits(5000, UnitTypeId.Millimeters);
                double dx = width / 2;
                double dy = depth / 2;

                List<XYZ> points = new List<XYZ>();
                points.Add(new XYZ(-dx, -dy, 0));
                points.Add(new XYZ(dx, -dy, 0));
                points.Add(new XYZ(dx, dy, 0));
                points.Add(new XYZ(-dx, dy, 0));
                points.Add(new XYZ(-dx, -dy, 0));

                walls = new List<Wall>();

                Transaction transaction = new Transaction(doc, "Построение стен");
                transaction.Start();
                for (int i = 0; i < 4; i++)
                {
                    Line line = Line.CreateBound(points[i], points[i + 1]);
                    wall = Wall.Create(doc, line, level1.Id, false);
                    walls.Add(wall);
                    wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id);
                }
                transaction.Commit();
            }

            void AddDoor()
            {
                FamilySymbol doorType = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_Doors)
                    .OfType<FamilySymbol>()
                    .Where(x => x.Name.Equals("0915 x 2134mm"))
                    .Where(x => x.FamilyName.Equals("M_Single-Flush"))
                    .FirstOrDefault();

                LocationCurve hostCurve = walls[0].Location as LocationCurve;
                XYZ point1 = hostCurve.Curve.GetEndPoint(0);
                XYZ point2 = hostCurve.Curve.GetEndPoint(1);
                XYZ point = (point1 + point2) / 2;

                if (!doorType.IsActive)
                    doorType.Activate();
                Transaction trans = new Transaction(doc, "Построение дверей");
                trans.Start();
                doc.Create.NewFamilyInstance(point, doorType, walls[0], level1, StructuralType.NonStructural);
                trans.Commit();
            }

            void AddWindow()
            {
                FamilySymbol windowType = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_Windows)
                    .OfType<FamilySymbol>()
                    .Where(x => x.Name.Equals("1200 x 1500mm"))
                    .Where(x => x.FamilyName.Equals("M_Window-Casement-Double"))
                    .FirstOrDefault();

                for (int i = 1; i < 4; i++)
                {
                    LocationCurve hostCurve = walls[i].Location as LocationCurve;
                    XYZ point1 = hostCurve.Curve.GetEndPoint(0);
                    XYZ point2 = hostCurve.Curve.GetEndPoint(1);
                    XYZ point = (point1 + point2) / 2;

                    Transaction trans = new Transaction(doc, "Построение окон");
                    trans.Start();
                    if (!windowType.IsActive)
                        windowType.Activate();
                    FamilyInstance window = doc.Create.NewFamilyInstance(point, windowType, walls[i], level1, StructuralType.NonStructural);
                    Parameter param = window.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM);
                    param.Set(4);
                    trans.Commit();
                }
            }

            return Result.Succeeded;
        }
    }
}
