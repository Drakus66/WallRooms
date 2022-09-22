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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using WPF = System.Windows;

#endregion

namespace WallRooms
{
    /// <summary>
    /// Revit external command.
    /// </summary>	
	[Transaction(TransactionMode.Manual)]
    public sealed class StartCommand : IExternalCommand
    {  
        Result IExternalCommand.Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Result result = Result.Failed;

            UIApplication ui_app = commandData?.Application;
            UIDocument ui_doc = ui_app?.ActiveUIDocument;
            Application app = ui_app?.Application;
            Document doc = ui_doc?.Document;

            //Список всех найденых помещений
            List<Room> allRooms = new List<Room>();

            //Получение связаных файлов в проекте
            FilteredElementCollector linksCollector = new FilteredElementCollector(doc);
            linksCollector.OfClass(typeof(RevitLinkInstance));

            List<Document> linkDocs = new List<Document>();
            ElementCategoryFilter roomCategoryFilter = new ElementCategoryFilter(BuiltInCategory.OST_Rooms);

            foreach (RevitLinkInstance linkInstance in linksCollector)
            {
                //для экземпляра связи получаем тип связи и проверяем загружена ли связь
                RevitLinkType linkType = doc.GetElement(linkInstance.GetTypeId()) as RevitLinkType;
                //Если не загружена пропускаем
                if (linkType != null || linkType.GetLinkedFileStatus() != LinkedFileStatus.Loaded) continue;

                Document linkDoc = linkInstance.GetLinkDocument();
                if (linkDoc != null )
                {
                    FilteredElementCollector docRooms = new FilteredElementCollector(linkDoc);
                    if (docRooms.WherePasses(roomCategoryFilter).ToElementIds().Count > 0) linkDocs.Add(linkDoc);
                }
            }


            return result;
        }
    }
}
