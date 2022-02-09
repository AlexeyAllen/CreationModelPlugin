using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;

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
            AddRoof();

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

                
                Transaction trans = new Transaction(doc, "Построение дверей");
                trans.Start();
                if (!doorType.IsActive)
                    doorType.Activate();
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

            void AddRoof()
            {
                RoofType roofType = new FilteredElementCollector(doc)
                    .OfClass(typeof(RoofType))
                    .OfType<RoofType>()
                    .Where(x => x.Name.Equals("Generic - 400mm"))
                    .Where(x => x.FamilyName.Equals("Basic Roof"))
                    .FirstOrDefault();

                double wallWidth = walls[0].Width;
                double dt = wallWidth / 2;
                List<XYZ> points = new List<XYZ>();
                points.Add(new XYZ(-dt, -dt, 0));
                points.Add(new XYZ(dt, -dt, 0));
                points.Add(new XYZ(dt, dt, 0));
                points.Add(new XYZ(-dt, dt, 0));
                points.Add(new XYZ(-dt, -dt, 0));

                //Application application = doc.Application;
                //CurveArray footPrint = application.Create.NewCurveArray();
                //for (int i = 0; i < 4; i++)
                //{
                //    LocationCurve curve = walls[i].Location as LocationCurve;
                //    XYZ p1 = curve.Curve.GetEndPoint(0);
                //    XYZ p2 = curve.Curve.GetEndPoint(1);
                //    Line line = Line.CreateBound(p1 + points[i], p2 + points[i+1]);
                //    footPrint.Append(line);
                //}

                //CurveArray curveArray = new CurveArray();
                Application application = doc.Application;
                CurveArray curveArray = application.Create.NewCurveArray();
                LocationCurve curve = walls[1].Location as LocationCurve;
                XYZ p1 = curve.Curve.GetEndPoint(0);
                XYZ p2 = curve.Curve.GetEndPoint(1);

                Parameter wallHeightParam = walls[0].get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);
                double d = wallHeightParam.AsDouble();
                XYZ p5 = new XYZ(0, 0, d);
                p1 += p5;
                p2 += p5;

                XYZ p4 = new XYZ(0, 0, 5);
                XYZ p3 = (p1 + p2) / 2 + p4;

                XYZ vx = XYZ.BasisY;
                XYZ vy = XYZ.BasisZ;

                curveArray.Append(Line.CreateBound(p1, p3));
                curveArray.Append(Line.CreateBound(p3, p2));

                LocationCurve lc = walls[2].Location as LocationCurve;
                double wallLength = lc.Curve.Length;

                //ModelCurveArray footPrintToModelCurveMapping = new ModelCurveArray();

                Transaction trans = new Transaction(doc, "Построение кровли");
                trans.Start();
                //FootPrintRoof footPrintRoof = doc.Create.NewFootPrintRoof(footPrint, level2, roofType, out footPrintToModelCurveMapping);
                ReferencePlane plane = doc.Create.NewReferencePlane2(p1, p1 + vy, p1 + vx, doc.ActiveView);
                ExtrusionRoof footPrintRoof = doc.Create.NewExtrusionRoof(curveArray, plane, level2, roofType, 0, wallLength);

                //ModelCurveArrayIterator iterator = footPrintToModelCurveMapping.ForwardIterator();
                //iterator.Reset();
                //while (iterator.MoveNext())
                //{
                //    ModelCurve modelCurve = iterator.Current as ModelCurve;
                //    footPrintRoof.set_DefinesSlope(modelCurve, true);
                //    footPrintRoof.set_SlopeAngle(modelCurve, 0.5);
                //}
                //foreach (ModelCurve m in footPrintToModelCurveMapping)
                //{
                //    footPrintRoof.set_DefinesSlope(m, true);
                //    footPrintRoof.set_SlopeAngle(m, 0.5);
                //}

                trans.Commit();
            }

            return Result.Succeeded;
        }
    }
}
