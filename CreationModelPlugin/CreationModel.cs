using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
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
            CreateLevels();
            CreateWalls();

            List<Level> listLevel;
            Level level1;
            Level level2;

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

                List<Wall> walls = new List<Wall>();

                Transaction transaction = new Transaction(doc, "Построение стен");
                transaction.Start();
                for (int i = 0; i < 4; i++)
                {
                    Line line = Line.CreateBound(points[i], points[i + 1]);
                    Wall wall = Wall.Create(doc, line, level1.Id, false);
                    walls.Add(wall);
                    wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id);
                }

                transaction.Commit();
            }

            return Result.Succeeded;
        }
    }
}
