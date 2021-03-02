using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using PluginStandard;
using System.IO;

namespace PluginLibrary
{
    public class StartingViewPlugin : IPlugin
    {

        private StartingViewParameters Settings { get; set; }
        private bool ViewExist { get; set; }
        private bool FamilyLoaded { get; set; }
        private bool ViewTypeCorrect { get; set; }
        private bool FamilyExistOnView { get; set; }
        private bool StartingViewSettedCorrect { get; set; }
        private bool HaveViewWithSameName { get; set; }
        private bool IsModelValidated { get; set; }
        private Family StartingViewFamily { get; set; }
        private View StartingView { get; set; }


        public StartingViewPlugin()
        {
            Name = typeof(StartingViewPlugin).Name;
            Settings = new StartingViewParameters();
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
            if (!Settings.SetParameters(parameters)) return false;
            var pathToFamily = parameters.Where(param => param.PropertyName == "PathToFamilyIntoServer").First();
            if (!File.Exists(pathToFamily.Value))
            {
                pathToFamily.ErrorMessage += "Файл не существует";
                return false;
            }
            if (!pathToFamily.Value.Contains(".rfa"))
            {
                pathToFamily.ErrorMessage += "Файл не является файлом семейства";
                return false;
            }
            return true;
        }
        public bool ValidateModel(Document doc, ref string report, IEnumerable<AddInsParameter> parameters)
        {
            report += "Проерка начального вида";
            if (!VerifyValidationParameters(parameters))
            {
                report += $"\nПараметры проверки не прошли проверку";
                return false;
            }

            var onServerFamily = new FileInfo(Settings.PathToFamilyIntoServer);
            var familyName = onServerFamily.Name.Replace(".rfa", string.Empty);
            var family = doc.GetFirstClassAndNameRelatedInstance<Family>(familyName);
            var views = doc.GetClassAndNameRelatedInstance<View>(Settings.ViewName);
            if (views.Count() > 1)
            {
                report += $"\n В проекте существуется больше одного вида с именем \"{Settings.ViewName}\", удалите ненужные виды";
                HaveViewWithSameName = true;
                return false;
            }
            var view = views.Count() == 1 ? views.First() : null;
            FamilyLoaded = !(family == null);
            ViewExist = !(view == null);
            ViewTypeCorrect = ((view is ViewSheet) && Settings.ViewType == "Лист" || ((view is ViewPlan) && Settings.ViewType == "Вид"));
            var startViewSettings = StartingViewSettings.GetStartingViewSettings(doc);
            StartingViewSettedCorrect = (ViewExist && view.Id == startViewSettings.ViewId);

            if (FamilyLoaded && ViewExist)
            {
                var collector = (new FilteredElementCollector(doc)).OwnedByView(view.Id).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>().Where(c => c.Symbol.Family.Id == family.Id);
                FamilyExistOnView = collector.Count() > 0;
            }

            report += $"\n\t Семество найдено - {FamilyLoaded}";
            report += $"\n\t Вид создан - {ViewExist}";
            report += $"\n\t Тип вида соответствует требованиям - {ViewTypeCorrect}";
            report += $"\n\t Семейство размещено на виде - {FamilyExistOnView}";
            report += $"\n\t Начальный вид установлен верно -{StartingViewSettedCorrect}";
            IsModelValidated = true;
            return (FamilyLoaded && ViewExist && ViewTypeCorrect && FamilyExistOnView && StartingViewSettedCorrect);
        }

        internal class StartingViewParameters : IPluginSettings
        {

            [AddInsParameter(VisibleName = "Путь к файлу семейства на сервере",
                Value = @"\\OIS-REVIT1\Revit\Семейства\Общие семейства (только чтение)\Начальный вид\Начальный вид.rfa")]
            public string PathToFamilyIntoServer { get; set; }

            [AddInsParameter(VisibleName = "Имя вида",
                Value = "Начальный вид")]
            public string ViewName { get; set; }

            [AddInsParameter(
                VisibleName = "Тип вида",
                AvailableValue = new[] { "Вид", "Лист" },
                Value = "Лист",
                ControlType = ControlType.ComboBox
                )]
            public string ViewType { get; set; }


        }

        #endregion

    }

    internal static class DocumentExtensions
    {
        public static IEnumerable<T> GetClassAndNameRelatedInstance<T>(this Document doc, string nameToFind)
                    where T : Element
        {
            var collector = new FilteredElementCollector(doc).OfClass(typeof(T)).Cast<T>().Where(c => c.Name == nameToFind);
            return collector;
        }


        public static T GetFirstClassAndNameRelatedInstance<T>(this Document doc, string nameToFind)
                           where T : Element
        {
            var firstMeet = new FilteredElementCollector(doc).OfClass(typeof(T)).Cast<T>().Where(c => c.Name == nameToFind).FirstOrDefault(); ;
            return firstMeet;
        }

        public static bool SetStartingView(this Document doc, View view)
        {
            var startViewSettings = StartingViewSettings.GetStartingViewSettings(doc);
            if (startViewSettings.IsAcceptableStartingView(view.Id))
            {
                using (var transaction = new Transaction(doc, "Установка стартового вида"))
                {
                    transaction.Start();
                    startViewSettings.ViewId = view.Id;
                    transaction.Commit();
                }
                return true;
            }
            return false;
        }
    }
}
