using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PluginLibrary;
using PluginStandard;


namespace ConsoleForUITest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(PluginLibrary.Library.Plugins.Count.ToString());
            PluginLibrary.Library.Plugins.Add(new TestClass());
            Console.ReadKey();
        }
    }
}
