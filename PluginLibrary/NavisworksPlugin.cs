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
            report += $"\nПроверка вида Navisworks";
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
            if (matchedNameViews.Count() > 1)
            {
                report += $"\nВ проекте больше одного вида с именем \"{Settings.ViewName}\".Удалите ненужные виды";
                return false;
            }

            var navisView = matchedNameViews.Cast<View3D>().First();
            var groupParameter = navisView.get_Parameter(new Guid(Settings.GroupingParameterGuid));
            if (groupParameter == null)
            {
                report += $"\nДля вида \"{Settings.ViewName}\" не назначен параметер с Guid\"{Settings.GroupingParameterGuid}\"";
                result = false;
            }

            if (groupParameter != null && groupParameter.AsValueString() != Settings.ViewGrouping)
            {
                report += $"\nДля вида \"{Settings.ViewName}\" зачение параметера с Guid\"{Settings.GroupingParameterGuid}\" не соответсвуте требованиям";
                result = false;
            }



           
            var verifyResult = navisView.Discipline == ViewDiscipline.Coordination;
            result &= verifyResult;
            report += $"\n\tДля вида \"{Settings.ViewName}\" установлена дисциплина \"Координация\" - {verifyResult}";

            verifyResult = !navisView.IsSectionBoxActive;
            result &= verifyResult;
            report += $"\n\tДля вида \"{Settings.ViewName}\" отключена граница 3D вида- {verifyResult}";

            verifyResult = !navisView.IsSectionBoxActive;
            result &= verifyResult;
            report += $"\n\tДля вида \"{Settings.ViewName}\" отключена граница 3D вида- {verifyResult}";








            return false;
        }

        internal class NavisworksParameters : IPluginSettings
        {

            [AddInsParameter(VisibleName = "Имя вида",
              Value = "Navisworks")]
            internal string ViewName { get; set; }

            [AddInsParameter(VisibleName = "Значение параметра группирования",
                Value = "Экспорт")]
            internal string ViewGrouping { get; set; }

            [AddInsParameter(VisibleName = "GUID параметра группирования",
                Value = "Назначение вида")]
            internal string GroupingParameterGuid { get; set; }

        }

        #endregion

    }
}
