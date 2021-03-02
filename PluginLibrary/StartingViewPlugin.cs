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
            throw new NotImplementedException();
        }

        internal class StartingViewParameters : IPluginSettings
        {

            [AddInsParameter(VisibleName = "Путь к файлу семейства на сервере")]
            public string PathToFamilyIntoServer { get; set; }

            [AddInsParameter(VisibleName = "Имя вида")]
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
