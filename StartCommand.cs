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
    class LinkDocWithRooms
    {
        public Document doc;
        public List<Room> docRooms;
        public Transform transform;

        public LinkDocWithRooms(RevitLinkInstance link)
        {
            doc = link.GetLinkDocument();
            transform = link.GetTransform();

            docRooms = new List<Room>();

            ElementCategoryFilter roomCategoryFilter = new ElementCategoryFilter(BuiltInCategory.OST_Rooms);
            FilteredElementCollector docRoomsCollector = new FilteredElementCollector(doc);
            List<Element> eRooms = docRoomsCollector.WherePasses(roomCategoryFilter).ToElements().ToList();
            foreach (Element eRoom in eRooms)
            {
                Room r = eRoom as Room;
                if (r != null && r.Area > 0) docRooms.Add(r);
            }
        }
    }



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


            //Получение связаных файлов в проекте
            FilteredElementCollector linksCollector = new FilteredElementCollector(doc);
            linksCollector.OfClass(typeof(RevitLinkInstance));

            List<LinkDocWithRooms> linkDocs = new List<LinkDocWithRooms>();
            FilteredElementCollector docRooms;
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
                    docRooms = new FilteredElementCollector(linkDoc);
                    if (docRooms.WherePasses(roomCategoryFilter).ToElementIds().Count > 0)
                    {
                        linkDocs.Add(new LinkDocWithRooms(linkInstance));
                    }
                }
            }

            //Поиск общих параметров в ФОП
            DefinitionFile sharedParamFile = commandData.Application.Application.OpenSharedParameterFile();
            if (sharedParamFile == null)
            {
                TaskDialog.Show("Error", "Файл общих параметров не привязан");
                return Result.Failed;
            }

            Definition adskFlatNumber = null;
            Definition adskRoomNumber = null;

            foreach ( var group in sharedParamFile.Groups)
            {
                foreach(var def in group.Definitions)
                {
                    ExternalDefinition eDef = def as ExternalDefinition;
                    if (eDef.GUID.Equals(new Guid("10fb72de-237e-4b9c-915b-8849b8907695"))) adskFlatNumber = def;
                    if (eDef.GUID.Equals(new Guid("669890ae1-d66e-4fe9-aced-024c27719f53"))) adskRoomNumber = def;
                }
            }            

            //Помещения из текущего файла
            List<Room> mainDocRooms = new List<Room>();
            docRooms = new FilteredElementCollector(doc);
            List<Element> eRoomsMainFile = docRooms.WherePasses(roomCategoryFilter).ToElements().ToList();
            foreach (Element eRoom in eRoomsMainFile)
            {
                Room r = eRoom as Room;
                if (r != null && r.Area > 0) mainDocRooms.Add(r);
            }


            List<Element> workElements = new List<Element>();
            
            //Получаем выбранные элементы
            Selection selectedElements = ui_doc.Selection;
            foreach (ElementId elemId in selectedElements.GetElementIds())
            {
                Element sElem = doc.GetElement(elemId);
                if (sElem is Wall) workElements.Add(sElem);
                if (sElem is Floor) workElements.Add(sElem);
                if (sElem is Ceiling) workElements.Add(sElem);
            }

            //Создаем окно
            StartWindow startWindow = new StartWindow();
            //Если предварительно выбраны элементы то выбрать эту опцию
            if (workElements.Count > 0)
            {
                startWindow.rbSelElems.Checked = true;
            }
            else //если нет, то выбрать элементы на виде
            {
                startWindow.rbOnView.Checked = true;
            }

            //Отображаем окно
            startWindow.ShowDialog();

            //Если окно закрыто или нажата Отмена, то останавливаем выполнение
            if (!startWindow.OkStart) return Result.Cancelled;

            if (startWindow.rbSelElems.Checked)
            {
                if (workElements.Count == 0)
                {
                    TaskDialog.Show("Ошибка", "Не выбраны подходящие элементы");
                    return Result.Cancelled;
                }
            }
            
            ElementCategoryFilter wallsfilter = new ElementCategoryFilter(BuiltInCategory.OST_Walls);

            if (startWindow.rbOnView.Checked)
            {
                //Получаем активный вид
                View curView = ui_doc.ActiveView;
                FilteredElementCollector collector = new FilteredElementCollector(doc, curView.Id);
                //Стены с активного вида
                workElements = collector.WherePasses(wallsfilter).ToElements().ToList();
            }

            if (startWindow.rbAll.Checked)
            {
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                //Все стены из проекта
                workElements = collector.WherePasses(wallsfilter).ToElements().ToList();
            }

            if (workElements.Count == 0)
            {
                TaskDialog.Show("Ошибка", "Нет подходящих элементов");
                return Result.Cancelled;
            }


            //Проверка наличия параметра у конструкций
            bool addToWall = false;
            bool addToFloor = false;
            bool addToCeiling = false;

            Element wallElem = workElements.FirstOrDefault(e => e is Wall && !(e is FamilyInstance));
            Element floorElem = workElements.FirstOrDefault(e => e is Floor && !(e is FamilyInstance));
            Element ceilingElem = workElements.FirstOrDefault(e => e is Ceiling && !(e is FamilyInstance));

            addToWall = wallElem.get_Parameter(adskFlatNumber) == null 
                            || wallElem.get_Parameter(adskRoomNumber) == null;
            addToFloor = floorElem.get_Parameter(adskFlatNumber) == null 
                            || floorElem.get_Parameter(adskRoomNumber) == null;
            addToCeiling = ceilingElem.get_Parameter(adskFlatNumber) == null 
                            || ceilingElem.get_Parameter(adskRoomNumber) == null;

            //Добавляем параметры, если они не найдены у одной из категорий
            if (addToWall || addToFloor || addToCeiling)
            {
                //Открываем транзакцию для изменения модели
                using ( Transaction TR = new Transaction(doc, "Добавление параметров"))
                {
                    TR.Start();
                    
                    CategorySet myCategories = new CategorySet(); //создание набора категорий

                    if (addToWall)
                    {
                        Category wallCategory = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Walls);
                        myCategories.Insert(wallCategory);
                    }
                    //К полам и потолкам добавляем только если был выбран первый вариант
                    if (startWindow.rbSelElems.Checked)
                    {
                        if (addToFloor)
                        {
                            Category floorCategory = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Floors);
                            myCategories.Insert(floorCategory);
                        }
                        if (addToCeiling)
                        {
                            Category ceilCategory = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Ceilings);
                            myCategories.Insert(ceilCategory);
                        }
                    }

                    InstanceBinding insBinding = new InstanceBinding(myCategories);
                    doc.ParameterBindings.Insert(adskFlatNumber, insBinding);
                    doc.ParameterBindings.Insert(adskRoomNumber, insBinding);

                    TR.Commit();
                }
            }




            return result;
        }


        public void FindRooms (List<Room> rooms, List<Element> elements, Transform transform = null)
        {

        }
    }
}
