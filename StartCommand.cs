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

    class ElemToFillParam 
    {
        public Element elem;

        public Solid solid;
        public double width;

        public string FlatNumber;
        public string RoomNumber;

        public bool isFloor = false;
        public bool isCeiling = false;
        public bool isWall = false;

    }



    /// <summary>
    /// Revit external command.
    /// </summary>	
	[Transaction(TransactionMode.Manual)]
    public sealed class StartCommand : IExternalCommand
    {
        Document doc;
        Definition adskFlatNumber = null;
        Definition adskRoomNumber = null;


        Result IExternalCommand.Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Result result = Result.Succeeded;

            UIApplication ui_app = commandData?.Application;
            UIDocument ui_doc = ui_app?.ActiveUIDocument;
            Application app = ui_app?.Application;
            doc = ui_doc?.Document;

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
                var linkStatus = linkType.GetLinkedFileStatus();

                if (linkType == null/* || linkType.GetLinkedFileStatus() != LinkedFileStatus.Loaded*/) continue;

                Document linkDoc = linkInstance.GetLinkDocument();
                if (linkDoc != null)
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

            adskFlatNumber = null;
            adskRoomNumber = null;

            foreach (var group in sharedParamFile.Groups)
            {
                foreach (var def in group.Definitions)
                {
                    ExternalDefinition eDef = def as ExternalDefinition;
                    if (eDef.GUID.Equals(new Guid("10fb72de-237e-4b9c-915b-8849b8907695"))) adskFlatNumber = def;
                    if (eDef.GUID.Equals(new Guid("69890ae1-d66e-4fe9-aced-024c27719f53"))) adskRoomNumber = def;
                }
            }

            if (adskFlatNumber == null|| adskRoomNumber == null)
            {
                TaskDialog.Show("Внимание", "Не найдены общие параметры. Возможно не привязан файл общих параметров либо привязан не тот файл. Используйте файл общих параметров ADSK");

                return Result.Failed;
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


            //Поиск дублирования

            if (startWindow.cbCheckClash.Checked)
            { 
                FailureDefinitionId warnId = new FailureDefinitionId(new Guid("D8B622D3-5DA1-44B7-AB76-875DF259BCAF"));
                FailureMessage failMessage = new FailureMessage(warnId);

                List<ElementId> workElementIds = (from e in workElements select e.Id).ToList();

                List<ElementId[]> DuplicateDetected = new List<ElementId[]>();


                List<ElementId> checkedIds = new List<ElementId>();
                foreach (ElementId eId in workElementIds)
                {
                    FilteredElementCollector interferingCollector = new FilteredElementCollector(doc, workElementIds);
                    List<ElementId> excludedElements = new List<ElementId>();
                    excludedElements.Add(eId);
                    excludedElements.AddRange(checkedIds);
                    ExclusionFilter exclusionFilter = new ExclusionFilter(excludedElements);
                    interferingCollector.WherePasses(exclusionFilter);

                    Element checkElem = doc.GetElement(eId);
                    Solid checkSolid = GetElementSolid(checkElem);

                    ElementIntersectsElementFilter intersectionFilter = new ElementIntersectsElementFilter(checkElem);
                    List<Element> intersectedElements = interferingCollector.WherePasses(intersectionFilter).ToElements().ToList();

                    foreach (Element intElem in intersectedElements)
                    {
                        Solid secondSolid = GetElementSolid(intElem);
                        Solid intersectSolid = null;

                        try
                        {
                            intersectSolid = BooleanOperationsUtils.ExecuteBooleanOperation(checkSolid, secondSolid, BooleanOperationsType.Intersect);
                        }
                        catch
                        { }

                        if (intersectSolid == null) continue;

                        //если объем пересечения солидов элементов равен объему одного из элементов, то считаем, что это дублирование
                        if (Math.Round(intersectSolid.Volume, 3) == Math.Round(secondSolid.Volume, 3) ||
                            Math.Round(intersectSolid.Volume, 3) == Math.Round(secondSolid.Volume, 3))

                        {
                            //Если такая пара элементов не найдена, то добавляем
                            if (DuplicateDetected.FirstOrDefault(pe => pe.Contains(eId) && pe.Contains(intElem.Id)) == null)
                            {
                                DuplicateDetected.Add(new ElementId[] { eId, intElem.Id });
                            }
                        }
                    }
                    checkedIds.Add(eId);
                }


                if (DuplicateDetected.Count > 0)
                {
                    List<ElementId> failElems = (from p in DuplicateDetected select p[0]).ToList();
                    List<ElementId> secFailElems = (from p in DuplicateDetected select p[1]).ToList();

                    failMessage.SetFailingElements(failElems);
                    failMessage.SetAdditionalElements(secFailElems);

                    using (Transaction TR = new Transaction(doc, "Дублирование"))
                    {
                        TR.Start();
                        doc.PostFailure(failMessage);
                        TR.Commit();
                    }


                    return Result.Failed;
                }
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

            if (startWindow.rbSelElems.Checked)
            {
                addToFloor = floorElem.get_Parameter(adskFlatNumber) == null
                                || floorElem.get_Parameter(adskRoomNumber) == null;
                addToCeiling = ceilingElem.get_Parameter(adskFlatNumber) == null
                                || ceilingElem.get_Parameter(adskRoomNumber) == null;
            }

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

                    var ParamDefinition = SharedParameterElement.Lookup(doc, new Guid("10fb72de-237e-4b9c-915b-8849b8907695")).GetDefinition();
                    ParamDefinition.SetAllowVaryBetweenGroups(doc, true);

                    ParamDefinition = SharedParameterElement.Lookup(doc, new Guid("69890ae1-d66e-4fe9-aced-024c27719f53")).GetDefinition();
                    ParamDefinition.SetAllowVaryBetweenGroups(doc, true);

                    TR.Commit();
                }
            }



            List<ElemToFillParam> elementsToCheck = new List<ElemToFillParam>();

            foreach (Element sElem in workElements)
            {
                if (sElem is FamilyInstance) continue; //Обрабатываем ТОЛЬКО системные элементы

                ElemToFillParam e = new ElemToFillParam()
                {
                    elem = sElem,
                    RoomNumber = "",
                    FlatNumber = "",
                    solid = GetElementSolid(sElem),
                };
                if (sElem is Wall)
                {
                    e.isWall = true;
                    e.width  = (sElem as Wall).Width;
                }
                if (sElem is Floor)
                {
                    e.isWall = true;
                    Parameter thickParam = sElem.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM);
                    if (thickParam != null) e.width = thickParam.AsDouble();
                }
                if (sElem is Ceiling)
                {
                    e.isCeiling = true;
                    CeilingType type = doc.GetElement((sElem as Ceiling).GetTypeId()) as CeilingType;
                    if (type != null)
                    {
                        Parameter thickParam = type.get_Parameter(BuiltInParameter.CEILING_THICKNESS);
                        if (thickParam != null) e.width = thickParam.AsDouble();
                    }
                }

                elementsToCheck.Add(e);
            }


            
            //поиск по помещениям из текущего файла
            if (mainDocRooms.Count > 0)
            {
                elementsToCheck = FindRooms(mainDocRooms, elementsToCheck);
            }


            //Поиск по помещениям из связанных файлов
            if (linkDocs.Count > 0)
            {
                foreach(var lDoc in linkDocs)
                {
                    elementsToCheck = FindRooms(lDoc.docRooms, elementsToCheck, lDoc.transform);
                }
            }



            //Запись значений
            using (Transaction TR = new Transaction(doc, "Запись значений"))
            {
                TR.Start();
                foreach (var Elem in elementsToCheck)
                {
                    Parameter elemFlatNumParam = Elem.elem.get_Parameter(adskFlatNumber);
                    if (!elemFlatNumParam.IsReadOnly) elemFlatNumParam.Set(Elem.FlatNumber);
                    Parameter elemRoomNumParam = Elem.elem.get_Parameter(adskRoomNumber);
                    if (!elemRoomNumParam.IsReadOnly) elemRoomNumParam.Set(Elem.RoomNumber);
                }

                TR.Commit();
            }

            return result;
        }


        List<ElemToFillParam> FindRooms (List<Room> rooms, List<ElemToFillParam> elements, Transform transform = null)
        {
            //Список ИД элементов для фильтрации
            List<ElementId> workElementIds = (from e in elements select e.elem.Id).ToList();

            foreach (Room room in rooms)
            {
                //получаем BoundingBox помещения, расширяем его и делаем фильтр
                BoundingBoxXYZ roomBB = room.get_BoundingBox(null);
                XYZ MaxPoint = roomBB.Max;
                XYZ MinPoint = roomBB.Min;

                MaxPoint = MaxPoint.Add(XYZ.BasisZ).Add(XYZ.BasisX).Add(XYZ.BasisY);
                MinPoint = MinPoint.Subtract(XYZ.BasisZ).Subtract(XYZ.BasisX).Subtract(XYZ.BasisY);

                //Если помещение из связи, то преобразуем координаты в систему координат текущего файла
                if (transform != null)
                {
                    MaxPoint = transform.OfPoint(MaxPoint);
                    MinPoint = transform.OfPoint(MinPoint);

                    //Пересчет минимумов и максимумов после трансформации
                    MinPoint = new XYZ(Math.Min(MaxPoint.X, MinPoint.X), Math.Min(MaxPoint.Y, MinPoint.Y), Math.Min(MaxPoint.Z, MinPoint.Z));
                    MaxPoint = new XYZ(Math.Max(MaxPoint.X, MinPoint.X), Math.Max(MaxPoint.Y, MinPoint.Y), Math.Max(MaxPoint.Z, MinPoint.Z));
                }
                
                BoundingBoxIntersectsFilter bbFilter = new BoundingBoxIntersectsFilter(new Outline(MinPoint, MaxPoint));
                FilteredElementCollector roomNearCollector = new FilteredElementCollector(doc, workElementIds);

                List<ElementId> selectedElements = roomNearCollector.WherePasses(bbFilter).ToElementIds().ToList();

                List<int> workElementIndxs = new List<int>();

                //Находим индексы в основноим списке всех элементов, находящихся рядом с помещеним
                foreach(ElementId eId in selectedElements)
                {
                    int eInd = elements.IndexOf(elements.First(e => e.elem.Id.IntegerValue == eId.IntegerValue));
                    workElementIndxs.Add(eInd);
                }

                if (workElementIndxs.Count == 0) continue;

                //Параметры помещения
                Parameter rFlatNum = room.get_Parameter(adskFlatNumber);

                Parameter rRoomNum = room.get_Parameter(adskRoomNumber);


#if DEBUG
                rFlatNum = room.LookupParameter("Номер квартиры"); //Тест
                rRoomNum = room.LookupParameter("Номер"); //Тест
#endif

                foreach (int i in workElementIndxs)
                {
                    ElemToFillParam checkElem = elements[i];

                    XYZ trackPointEx = null;
                    XYZ trackPointInt = null;
                    XYZ solidCenter = checkElem.solid.ComputeCentroid();

                    if (checkElem.isWall)
                    {
                        Location wallLoc = (checkElem.elem as Wall).Location;
                        if (wallLoc != null && wallLoc is LocationCurve locCurve)
                        {
                            Curve wallCurve = locCurve.Curve;
                            if (wallCurve is Arc)
                            {
                                XYZ arcCenter = wallCurve.ComputeDerivatives(0.5, true).Origin;
                                solidCenter = new XYZ(arcCenter.X, arcCenter.Y, solidCenter.Z);
                            }
                        }

                        trackPointEx = solidCenter.Add((checkElem.elem as Wall).Orientation.Multiply(checkElem.width * 2));
                        trackPointInt = solidCenter.Add((checkElem.elem as Wall).Orientation.Multiply(checkElem.width * 2).Negate());
                    }

                    if (checkElem.isFloor)
                    {
                        trackPointInt = solidCenter.Add(XYZ.BasisZ.Multiply(checkElem.width * 2));
                    }

                    if (checkElem.isCeiling)
                    {
                        trackPointInt = solidCenter.Add(XYZ.BasisZ.Negate().Multiply(2));
                    }



                    //Если связанный файл, то транслируем проверочные точки в его координаты
                    if (transform != null)
                    {
                        trackPointInt = transform.Inverse.OfPoint(trackPointInt);
                        trackPointEx = transform.Inverse.OfPoint(trackPointEx);
                    }

                    bool pointInRoom = false;
                    pointInRoom = room.IsPointInRoom(trackPointInt);

                    if (!pointInRoom && trackPointEx != null) pointInRoom = room.IsPointInRoom(trackPointEx);

                    if (pointInRoom)
                    {
                        if (rFlatNum != null)
                        {
                            if (String.IsNullOrEmpty(elements[i].FlatNumber)) elements[i].FlatNumber = rFlatNum.AsString();
                            else elements[i].FlatNumber += "; " + rFlatNum.AsString();
                        }
                        if (rRoomNum != null)
                        {
                            if (String.IsNullOrEmpty(elements[i].RoomNumber)) elements[i].RoomNumber = rRoomNum.AsString();
                            else elements[i].RoomNumber += "; " + rRoomNum.AsString();
                        }
                    }
                }
            }

            return elements;
        }


        Solid GetElementSolid(Element intElem)
        {
            if (intElem is Wall && (intElem as Wall).WallType.Kind == WallKind.Curtain)
            {
                Wall wallElem = intElem as Wall;
                Curve wallLocLine = (intElem.Location as LocationCurve).Curve;
                Curve c1 = wallLocLine.CreateOffset(wallElem.WallType.Width / 2, XYZ.BasisZ);
                Curve c3 = wallLocLine.CreateOffset(-wallElem.WallType.Width / 2, XYZ.BasisZ).CreateReversed();
                Curve c2 = Line.CreateBound(c1.GetEndPoint(1), c3.GetEndPoint(0));
                Curve c4 = Line.CreateBound(c3.GetEndPoint(1), c1.GetEndPoint(0));
                CurveLoop cl = new CurveLoop();
                cl.Append(c1);
                cl.Append(c2);
                cl.Append(c3);
                cl.Append(c4);
                Solid curtainBoundSolid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { cl }, XYZ.BasisZ, wallElem.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM).AsDouble());

                return curtainBoundSolid;
            }

            GeometryElement elemGeometryElement = intElem.get_Geometry(new Options() { DetailLevel = ViewDetailLevel.Undefined });
            Solid elemSolid = null;
            try
            {
                elemSolid = elemGeometryElement.First(es => (es is Solid) && Math.Abs((es as Solid).Volume) > 0) as Solid;
            }
            catch
            {
                try
                {
                    GeometryInstance ElemGeometryInst = elemGeometryElement.First(gi => gi is GeometryInstance) as GeometryInstance;
                    elemSolid = ElemGeometryInst.GetInstanceGeometry().First(es => (es is Solid) && Math.Abs((es as Solid).Volume) > 0) as Solid;
                }
                catch
                {
                    Console.Write(intElem.Id.ToString());
                }
            }
            return elemSolid;
        }
    }
}
