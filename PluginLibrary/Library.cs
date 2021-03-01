using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using PluginStandard;

namespace PluginLibrary
{
    public static class Library
    {
        public static List<IPlugin> Plugins { get; private set; }

        static Library()
        {
            Plugins = new List<IPlugin>();
         


        }


    }






    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    public class ForTestingPlugin : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            var isNull = Library.Plugins == null;

            var text = isNull.ToString();
            TaskDialog.Show("asdas", text);

            Library.Plugins.Add(new TemplateClass());
            var plugCount = Library.Plugins.Count();
            TaskDialog.Show("asdas", Library.Plugins.Count().ToString());

            return Result.Succeeded;

        }
    }



}
