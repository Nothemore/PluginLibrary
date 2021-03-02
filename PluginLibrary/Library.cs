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
            Plugins = new List<IPlugin>() {
                new StartingViewPlugin(),
                new RevitLinkPlugin()
            };
        }


    }






    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    public class ForTestingPlugin : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var psd = new PluginSelectionDialog(Library.Plugins,commandData.Application.ActiveUIDocument.Document);
            psd.ShowDialog();
          
            return Result.Succeeded;

        }
    }


   
}
