using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PluginStandard;

namespace PluginLibrary
{
    public class PluginSelectionDialog:Form
    {
        PluginSelectionDialog(IEnumerable<IPlugin> plugins)
        {
            var info = new Label();
            info.Text = "Выберите плагин для настройки";
            info.Dock = DockStyle.Fill;

            var selector = new ComboBox();
            selector.DataSource = plugins;
            selector.DisplayMember = "Name";
            selector.SelectedItem = plugins.First();
            selector.Dock = DockStyle.Fill;

            var acceptBtn = new Button();
            acceptBtn.Text = "Настройть";
            acceptBtn.Dock = DockStyle.Fill;

            var container = new TableLayoutPanel();
            container.ColumnCount = 1;
            container.RowCount = 3;
            container.Dock = DockStyle.Fill;
            container.Controls.Add(info);
            container.Controls.Add(selector);
            container.Controls.Add(acceptBtn);
            this.Controls.Add(container);
            



        }



    }
}
