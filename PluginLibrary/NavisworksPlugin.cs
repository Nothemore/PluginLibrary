using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using PluginStandard;

namespace PluginLibrary
{
    public class NavisworksPlugin : IPlugin
    {
        internal NavisworksParameters Settings { get; set; }
        public NavisworksPlugin()
        {
            Name = typeof(NavisworksPlugin).Name;
            Settings = new NavisworksParameters();
        }


        public string Name { get; set; }

        #region "Настройка"
        public IEnumerable<AddInsParameter> GetConfigurationParameters()
        {
            throw new NotImplementedException();
        }
        public bool VerifyConfigurationParameters(IEnumerable<AddInsParameter> parameters)
        {
            throw new NotImplementedException();
        }
        public bool ConfigurateModel(Document doc, ref string report, IEnumerable<AddInsParameter> parameters)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region "Проверка"
        public IEnumerable<AddInsParameter> GetValidationParameters()
        {
            return Settings.GetParameters();
        }
        public bool VerifyValidationParameters(IEnumerable<AddInsParameter> parameters)
        {
            return Settings.SetParameters(parameters);
        }
        public bool ValidateModel(Document doc, ref string report, IEnumerable<AddInsParameter> parameters)
        {
            var result = true;
            report += $"Проверка вида Navisworks";
            if (!Settings.SetParameters(parameters))
            {
                report += "\nПараметры проверки не прошли проверку";
                return false;
            }

            var matchedNameViews = new FilteredElementCollector(doc)
                .OfClass(typeof(View3D))
                .WhereElementIsNotElementType()
                .Where(c => c.Name == Settings.ViewName);
            if (matchedNameViews.Count() == 0)
            {
                report += $"\nВ проекте отсутсвует трехмерный вид с именем \"{Settings.ViewName}\".Необходимо создать этот вид.";
                return false;
            }

            var navisView = matchedNameViews.Cast<View3D>().First();
            Parameter groupParameter = null;

            if (Settings.GroupingParameterGuid != null)
            {
                Guid parseGuid;
                if (Guid.TryParse(Settings.GroupingParameterGuid, out parseGuid))
                    groupParameter = navisView.get_Parameter(parseGuid);
            }

            if (groupParameter == null)
            {
                var viewParameters = navisView.Parameters;
                foreach (Parameter param in viewParameters)
                    if (param.Definition.Name == Settings.GroupingParameterName)
                        groupParameter = param;
            }


            var verifyResult = groupParameter != null;
            result &= verifyResult;
            report += $"\n\tДля вида задан параметер с именем \"{Settings.GroupingParameterName}\" и GUID \"{Settings.GroupingParameterGuid}\"  - {verifyResult}";

            if (groupParameter != null)
            {
                verifyResult = groupParameter.AsString() == Settings.GroupingParameterValue;
                result &= verifyResult;
                report += $"\n\tЗначение параметра \"{Settings.GroupingParameterName}\" установлено верно - {verifyResult}";
            }

            verifyResult = navisView.Discipline == ViewDiscipline.Coordination;
            result &= verifyResult;
            report += $"\n\tУстановлена дисциплина \"Координация\" - {verifyResult}";

            verifyResult = !navisView.IsSectionBoxActive;
            result &= verifyResult;
            report += $"\n\tОтключены границы 3D вида - {verifyResult}";

            verifyResult = navisView.DetailLevel == ViewDetailLevel.Fine;
            result &= verifyResult;
            report += $"\n\tУстановлен высокий уровень детализации - {verifyResult}";

            verifyResult = !navisView.CropBoxActive;
            result &= verifyResult;
            report += $"\n\tНе пременена обрезка вида - {verifyResult}";

            verifyResult = navisView.GetFilters().Count == 0;
            result &= verifyResult;
            report += $"\n\tНе применены фильтры - {verifyResult}";

            verifyResult = navisView.ViewTemplateId == null || navisView.ViewTemplateId == ElementId.InvalidElementId;
            result &= verifyResult;
            report += $"\n\tНе установлен шаблон - {verifyResult}";

            verifyResult = !new FilteredElementCollector(doc)
                .OfClass(typeof(RevitLinkType))
                .Any(c => !c.IsHidden(navisView));
            result &= verifyResult;
            report += $"\n\tСкрыты все связые файлы - {verifyResult}";

            //CADLink Заменить на ImportInstance;
            verifyResult = !new FilteredElementCollector(doc)
             .OfClass(typeof(CADLinkType))
             .Where(c => navisView.CanCategoryBeHidden(c.Category.Id))
             .Any(c => !c.IsHidden(navisView));
            result &= verifyResult;
            report += $"\n\tСкрыты все имортированные категории, кроме \"Импорт в семейства\" - {verifyResult}";

            //Нужно ли проверять подкатегории
            var importsInFamilies = doc.Settings.Categories.get_Item(BuiltInCategory.OST_ImportObjectStyles);
            verifyResult = importsInFamilies.get_Visible(navisView);
            result &= verifyResult;
            report += $"\n\tКатегория \"Импорт в семейства\" включена - {verifyResult}";

            verifyResult = !navisView.AreImportCategoriesHidden;
            result &= verifyResult;
            report += $"\n\tОтображение импортированных категории включено - {verifyResult}";

            verifyResult = !navisView.AreModelCategoriesHidden;
            result &= verifyResult;
            report += $"\n\tОтображение категорий модели включено - {verifyResult}";

            verifyResult = true;
            var categories = doc.Settings.Categories;
            var mustBeHiddenCategory = new HashSet<int>()
            {
                categories.get_Item(BuiltInCategory.OST_Parts).Id.IntegerValue,
                categories.get_Item(BuiltInCategory.OST_Mass).Id.IntegerValue,
                categories.get_Item(BuiltInCategory.OST_Lines).Id.IntegerValue
            };

            foreach (Category category in categories)
            {
                if (category.CategoryType != CategoryType.Model) continue;
                if (category.Name.Contains("dwg") || category.Name.Contains("Import")) continue;
                //Может убрать эту проверку
                if (!navisView.CanCategoryBeHidden(category.Id)) continue;

                var categoryVisible = category.get_Visible(navisView);
                var categoryMustBeHidden = mustBeHiddenCategory.Contains(category.Id.IntegerValue);
                if ((categoryMustBeHidden && categoryVisible)
                    || (!categoryMustBeHidden && !categoryVisible))
                {
                    verifyResult = false;
                    break;
                }
            }

            result &= verifyResult;
            report += $"\n\tОтображены все категории модели, кроме: \"Линии\", \"Формы\", \"Части\" - {verifyResult}";


            report += $"\nПроверка пройдена успешно - {result}";
            return result;
        }

        internal class NavisworksParameters : IPluginSettings
        {
            [AddInsParameter(VisibleName = "Имя вида",
              Value = "Navisworks")]
            public string ViewName { get; set; }

            [AddInsParameter(VisibleName = "Имя параметра группирования",
               Value = "Назначение вида")]
            public string GroupingParameterName { get; set; }

            [AddInsParameter(VisibleName = "Значение параметра группирования",
                Value = "Экспорт")]
            public string GroupingParameterValue { get; set; }

            [AddInsParameter(VisibleName = "GUID параметра группирования",
                Value = "e313f126-7e51-4a5d-a45a-7c6dfe02124a")]
            public string GroupingParameterGuid { get; set; }
        }

        #endregion

    }
}
