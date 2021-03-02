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
            return Settings.SetParameters(parameters);
        }
        public bool ValidateModel(Document doc, ref string report, IEnumerable<AddInsParameter> parameters)
        {
            report += "Проверка связанных файлов";
            if (!Settings.SetParameters(parameters))
            {
                report += $"\n\tПараметры проверки не прошли проверку";
                return false;
            }
            var result = true;
            var linkTypes = new FilteredElementCollector(doc).OfClass(typeof(RevitLinkType)).Cast<RevitLinkType>().ToList();
            var linkInstances = new FilteredElementCollector(doc).OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>().ToList();
            var reportPrefix = string.Empty;

            if (Settings.CheckPathType)
            {
                reportPrefix = $"Установите значение \"{Settings.PathType}\" у параметра \"Пути\" для следующих связанных файлов Revit:";
                var pathType = Settings.PathType == "Абсолютный" ? PathType.Absolute : PathType.Relative;
                var linksWithWrongPathType = linkTypes.Where(linkType => linkType.PathType != pathType);
                result &= AddMessageToReport(linksWithWrongPathType, reportPrefix, ref report);
            }

            if (Settings.CheckReferenceType)
            {
                reportPrefix = $"Установите тип связи \"{Settings.AttachmentType}\" для следующих связанных файлов Revit:";
                var attachemntType = Settings.AttachmentType == "Внедрение" ? AttachmentType.Attachment : AttachmentType.Overlay;
                var linksWithWrongAttachemtType = linkTypes.Where(linkType => linkType.AttachmentType != attachemntType);
                result &= AddMessageToReport(linksWithWrongAttachemtType, reportPrefix, ref report);
            }

            if (Settings.CheckPin)
            {
                reportPrefix = $"Закрепите булавкой следующие связанные файлы Revit:";
                Func<RevitLinkInstance, bool> IsWrongFileStatus = (linkInstance) =>
                {
                    var linkedFileStatus = doc
                                            .GetElement(linkInstance.GetTypeId())
                                            .GetExternalFileReference()
                                            .GetLinkedFileStatus();
                    return !(linkedFileStatus == LinkedFileStatus.Invalid || linkedFileStatus == LinkedFileStatus.Unloaded);
                };
                var unpinnedLinkIstances = linkInstances.Where(c => IsWrongFileStatus(c) && !c.Pinned);
                result &= AddMessageToReport(unpinnedLinkIstances, reportPrefix, ref report);
            }
            return result;
        }

        internal bool AddMessageToReport<T>(IEnumerable<T> links, string reportPrefix, ref string report)
            where T : Element
        {
            if (links.Count() == 0) return true;
            report += report + reportPrefix;
            foreach (var link in links)
                report += $"\n\t{link.Name}";
            return false;
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
