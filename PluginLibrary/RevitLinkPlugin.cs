using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using PluginStandard;

namespace PluginLibrary
{
   
    public class RevitLinkPlugin : IPlugin
    {
        private RevitLinkParameters Settings { get; set; }
        public RevitLinkPlugin()
        {
            Name = typeof(RevitLinkPlugin).Name;
            Settings = new RevitLinkParameters();
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
            throw new NotImplementedException();
        }
        public bool ValidateModel(Document doc, ref string report, IEnumerable<AddInsParameter> parameters)
        {
            throw new NotImplementedException();
        }

        internal class RevitLinkParameters : IPluginSettings
        {
            [AddInsParameter(VisibleName = "Обработать закрепление")]
            public bool CheckPin { get; set; }

            [AddInsParameter(VisibleName = "Обработать тип пути")]
            public bool CheckPathType { get; set; }

            [AddInsParameter(VisibleName = "Обработать тип связи")]
            public bool CheckReferenceType { get; set; }

            [AddInsParameter(
                VisibleName = "Тип пути",
                AvailableValue = new[] { "Относительный", "Абсолютный" },
                Value = "Абсолютный")]
            public string PathType { get; set; }

            [AddInsParameter(
             VisibleName = "Тип связи",
               
             AvailableValue = new[] { "Внедрение", "Наложение" },
             Value = "Наложение")]
            public string AttachmentType { get; set; }
        }

        #endregion

    }
}
