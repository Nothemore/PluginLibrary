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
            if (!Settings.SetParameters(parameters)) return false;
            if (Settings.CheckServerPath && (Settings.ServerName == null || Settings.ServerName == string.Empty))
            {
                parameters
                    .Where(c => c.PropertyName == "ServerName")
                    .First()
                    .ErrorMessage = "Не задан путь к серверу";
                return false;
            }
            return true;

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
            var linkInstances = new FilteredElementCollector(doc).OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>().Where(c=>c.Name.Contains("rvt")).ToList();
            var messageTitle = string.Empty;

            if (Settings.CheckPathType)
            {
                messageTitle = $"\nУстановите значение \"{Settings.PathType}\" у параметра \"Пути\" для следующих связанных файлов Revit:";
                var pathType = Settings.PathType == "Абсолютный" ? PathType.Absolute : PathType.Relative;
                var linksWithWrongPathType = linkTypes.Where(linkType => linkType.PathType != pathType);
                result &= AddMessageToReport(linksWithWrongPathType, messageTitle, ref report);
            }

            if (Settings.CheckReferenceType)
            {
                messageTitle = $"\nУстановите тип связи \"{Settings.AttachmentType}\" для следующих связанных файлов Revit:";
                var attachemntType = Settings.AttachmentType == "Внедрение" ? AttachmentType.Attachment : AttachmentType.Overlay;
                var linksWithWrongAttachemtType = linkTypes.Where(linkType => linkType.AttachmentType != attachemntType);
                result &= AddMessageToReport(linksWithWrongAttachemtType, messageTitle, ref report);
            }


            if (Settings.CheckPin)
            {
                messageTitle = $"\nЗакрепите булавкой следующие связанные файлы Revit:";
                Func<RevitLinkInstance, bool> IsWrongFileStatus = (linkInstance) =>
                {
                    var linkedFileStatus = doc
                                            .GetElement(linkInstance.GetTypeId())
                                            .GetExternalFileReference()
                                            .GetLinkedFileStatus();
                    return !(linkedFileStatus == LinkedFileStatus.Invalid || linkedFileStatus == LinkedFileStatus.Unloaded);
                };
                var unpinnedLinkIstances = linkInstances.Where(c => IsWrongFileStatus(c) && !c.Pinned);
                result &= AddMessageToReport(unpinnedLinkIstances, messageTitle, ref report);
            }

            if (Settings.CheckDuplicatedInstance)
            {
                messageTitle = $"\nОбратите внимание, в проекте имеются одинаковые экземпляры связанных файлов:";
                var duplicatedInstance = linkInstances
                    .GroupBy(u => u.GetTypeId())
                    .Where(u => u.Count() > 1)
                    .Select(u => u.First());
                //.Select(u => doc.GetElement(u.Key));
                result &= AddMessageToReport(duplicatedInstance, messageTitle, ref report);
            }

            if (Settings.CheckServerPath)
            {
                messageTitle = $"\nВсе связанные файлы Revit должны быть загружены с сервера \"{Settings.ServerName}\".Файлы не соответствующие требованию:";
                Func<RevitLinkType, bool> IsLoadFromForbiddenSource = (linkType) =>
                {
                    var externalFileReference = linkType.GetExternalFileReference();
                    var linkedFileStatus = externalFileReference.GetLinkedFileStatus();
                    if (linkedFileStatus == LinkedFileStatus.Invalid || linkedFileStatus == LinkedFileStatus.Unloaded) return false;
                    var linkedFilePath = ModelPathUtils.ConvertModelPathToUserVisiblePath(externalFileReference.GetPath());
                    return !(linkedFilePath.ToLower().Contains(Settings.ServerName.ToLower()));
                };

                var loadFromForbiddenSource = linkTypes.Where(c => IsLoadFromForbiddenSource(c));
                result &= AddMessageToReport(loadFromForbiddenSource, messageTitle, ref report);
            }

            if (Settings.CheckBasePointMatching)
            {
                messageTitle = $"\nБазовая точка  связном файлен не совпадает с базовой точкой проекта.Проверьте расположение связных файлов:";
                Func<Document, XYZ> GetBasePoint = (document) =>
                     {
                         return new FilteredElementCollector(document)
                       .OfClass(typeof(BasePoint))
                       .Cast<BasePoint>()
                       .Where(c => !c.IsShared)
                       .First()
                       .get_BoundingBox(null)
                       .Max;
                     };
                var projectBasePointCoordinate = GetBasePoint(doc);
                Func<RevitLinkInstance, bool> IsBasePointMismatched = (linkInstance) =>
                {
                    var linkTransform = linkInstance.GetTotalTransform();
                    var linkBasePoint = GetBasePoint(linkInstance.GetLinkDocument());
                    var linkPointBaseIntoProjectCoordinate = linkTransform.OfPoint(linkBasePoint);
                    return !projectBasePointCoordinate.IsAlmostEqualTo(linkPointBaseIntoProjectCoordinate);
                };
                var basePointMismatchedLinks = linkInstances.Where(c => IsBasePointMismatched(c));
                result &= AddMessageToReport(basePointMismatchedLinks, messageTitle, ref report);
            }

            report += $"\nПроверка пройдена успешно - {result}";
            return result;
        }

        internal bool AddMessageToReport<T>(IEnumerable<T> links, string messageTitle, ref string report)
            where T : Element
        {
            if (links.Count() == 0) return true;
            report += messageTitle;
            foreach (var link in links)
                report += $"\n\t{link.Name}";
            return false;
        }


        internal class RevitLinkParameters : IPluginSettings
        {

            [AddInsParameter(VisibleName = "Проверить, что файл загружен с сервера")]
            public bool CheckServerPath { get; set; }

            [AddInsParameter(VisibleName = "Проверить дублирования связи")]
            public bool CheckDuplicatedInstance { get; set; }

            [AddInsParameter(VisibleName = "Обработать совпадение базовой точки")]
            public bool CheckBasePointMatching { get; set; }

            [AddInsParameter(VisibleName = "Обработать закрепление")]
            public bool CheckPin { get; set; }

            [AddInsParameter(VisibleName = "Обработать тип пути")]
            public bool CheckPathType { get; set; }

            [AddInsParameter(VisibleName = "Обработать тип связи")]
            public bool CheckReferenceType { get; set; }

            [AddInsParameter(
                VisibleName = "Тип пути",
                AvailableValue = new[] { "Абсолютный", "Относительный" },
                Value = "Абсолютный")]
            public string PathType { get; set; }

            [AddInsParameter(
             VisibleName = "Тип связи",
             AvailableValue = new[] { "Наложение", "Внедрение" },
             Value = "Наложение")]
            public string AttachmentType { get; set; }

            [AddInsParameter(
            VisibleName = "Путь к серверу",
            Value = @"\\ois-revit1")]
            public string ServerName { get; set; }
        }

        #endregion

    }
}
