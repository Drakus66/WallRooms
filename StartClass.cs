using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace WallRooms
{
    public class WallRooms : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            string assemblyName = Assembly.GetExecutingAssembly().Location;

            PushButtonData pushButtonData = new PushButtonData("WallRooms", "Помещения\nдля стен", assemblyName, "WallRooms.StartCommand")
            {
                ToolTip = "Определение помещений для стен",
                LongDescription = "",
            };

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            throw new NotImplementedException();
        }
    }
}
