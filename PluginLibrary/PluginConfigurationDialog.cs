using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PluginStandard;
using System.Windows.Forms;
using System.Drawing;



namespace PluginLibrary
{
    class PluginConfigurationDialog : Form
    {

        private List<Label> Errors { get; set; } = new List<Label>();
        private List<ParameterControlBinding> ParamControlBinds { get; set; } = new List<ParameterControlBinding>();


        public PluginConfigurationDialog(IPlugin plugin, bool isValidationContext)
        {
            var parameters = isValidationContext ? plugin.GetValidationParameters() : plugin.GetConfigurationParameters();
            if (parameters == null) throw new ArgumentException("Plugin's paramters was empty");

            var container = new TableLayoutPanel();
            container.ColumnCount = 3;
            container.RowCount = parameters.Count();
            container.Dock = DockStyle.Fill;
            container.CellBorderStyle = TableLayoutPanelCellBorderStyle.InsetDouble;
            for (int j = 0; j < container.ColumnCount; j++)
            {
                container.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
            }


            
            int i = 0;

            foreach (var param in parameters)
            {
                var paramName = new Label() { Dock = DockStyle.Fill };
                paramName.Text = param.VisibleName;
                Control paramControl = param.CreatControl();
                var paramError = new Label() { Dock = DockStyle.Fill};

                var paramControlBinding = new ParameterControlBinding()
                {
                    Parameter = param,
                    InputControl = paramControl,
                    ErrorLabel = paramError
                };
                
                ParamControlBinds.Add(paramControlBinding);
                container.Controls.Add(paramName, 0, i);
                container.Controls.Add(paramControl, 1, i);
                container.Controls.Add(paramError, 2, i);
                i++;

            }
            this.Controls.Add(container);
            this.Size = container.PreferredSize;
            this.Load += (sender, args) => { this.Size = container.PreferredSize; };
        }


        internal class ParameterControlBinding
        {
            internal AddInsParameter Parameter { get; set; }
            internal Control InputControl { get; set; }
            internal Label ErrorLabel { get; set; }
        }




    }


    internal static class AddInsParameterExtensions
    {
        internal static Control CreatControl(this AddInsParameter parameter)
        {
            Control paramControl = null;
            switch (parameter.ControlType)
            {
                case ControlType.TextBox:
                    paramControl = new TextBox();
                    break;
                case ControlType.ComboBox:
                    var paramAsCombo = new ComboBox();
                    paramAsCombo.DataSource = parameter.AvailableValue;
                    //paramAsCombo.SelectedItem = parameter.Value;
                    //paramAsCombo.SelectedIndex = 0;
                    paramControl = paramAsCombo;
                    break;
                case ControlType.CheckBox:
                    paramControl = new CheckBox();
                    break;

            }
            paramControl.Name = parameter.PropertyName;
            paramControl.Dock = DockStyle.Fill;
            return paramControl;
        }
    }




}
