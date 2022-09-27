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
    public class StartClass : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            
            // define a new failure id for a warning about walls
            FailureDefinitionId warnId = new FailureDefinitionId(new Guid("D8B622D3-5DA1-44B7-AB76-875DF259BCAF"));

            // register the new warning using FailureDefinition
            FailureDefinition failDef = FailureDefinition.CreateFailureDefinition(warnId, FailureSeverity.Error, "Обнаружены доблирующиеся элементы");



            string assemblyName = Assembly.GetExecutingAssembly().Location;

            PushButtonData pushButtonData = new PushButtonData("WallRooms", "Помещения\nдля стен", assemblyName, "WallRooms.StartCommand")
            {
                ToolTip = "Определение помещений для стен",
                LongDescription = "",
            };

            RibbonPanel panel = application.CreateRibbonPanel("Стены");
            panel.AddItem(pushButtonData);

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
