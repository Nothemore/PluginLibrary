using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PluginStandard;
using System.Windows.Forms;
using System.Drawing;
using Autodesk.Revit.DB;



namespace PluginLibrary
{
    class PluginConfigurationDialog : System.Windows.Forms.Form
    {

        private List<Label> Errors { get; set; } = new List<Label>();
        private List<ParameterControlBinding> ParamControlBinds { get; set; } = new List<ParameterControlBinding>();


        public PluginConfigurationDialog(IPlugin plugin, bool isValidationContext, Document doc)
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

            int numberOfParam = 0;
            foreach (var param in parameters)
            {
                var paramName = new Label() { Dock = DockStyle.Fill };
                paramName.Text = param.VisibleName;
                System.Windows.Forms.Control paramControl = param.CreatControl();
                var paramError = new Label() { Dock = DockStyle.Fill };

                var paramControlBinding = new ParameterControlBinding()
                {
                    Parameter = param,
                    InputControl = paramControl,
                    ErrorLabel = paramError
                };

                ParamControlBinds.Add(paramControlBinding);
                container.Controls.Add(paramName, 0, numberOfParam);
                container.Controls.Add(paramControl, 1, numberOfParam);
                container.Controls.Add(paramError, 2, numberOfParam);
                numberOfParam++;
            }

            var report = new TextBox() { Dock = DockStyle.Fill,Multiline = true,ScrollBars = ScrollBars.Both};
            
            report.ReadOnly = true;
            container.RowCount += 1;
            var acceptBtn = new Button();
            acceptBtn.Text = isValidationContext ? "Проверить" : "Настроить";
            acceptBtn.Dock = DockStyle.Fill;
            acceptBtn.Click += (sender, args) =>
            {
                foreach (var param in ParamControlBinds)
                    param.WriteValueIntoParameter();

                var succefullyVerified = false;
                if (isValidationContext) succefullyVerified = plugin.VerifyValidationParameters(parameters);
                else succefullyVerified = plugin.VerifyConfigurationParameters(parameters);

                foreach (var param in ParamControlBinds)
                    param.DisplayError();
                if (!succefullyVerified) return;

                var tempReport = string.Empty;
                if (isValidationContext) plugin.ValidateModel(doc, ref tempReport, parameters);
                else plugin.ConfigurateModel(doc, ref tempReport, parameters);

                report.Text = tempReport.Replace("\n","\r\n");
                
                this.Size = new Size(1000, container.RowCount * 35 + report.Lines.Count() * 12+50);


            };

            container.Controls.Add(acceptBtn, 1, numberOfParam);
            for (int i = 0; i < container.RowCount; i++)
                container.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));

            var withReportContainer = new TableLayoutPanel();
            withReportContainer.ColumnCount = 1;
            withReportContainer.RowCount = 2;
            withReportContainer.Dock = DockStyle.Fill;
            withReportContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, container.PreferredSize.Height + container.RowCount * 2));
            withReportContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            withReportContainer.Controls.Add(container, 0, 0);
            withReportContainer.Controls.Add(report, 0, 1);

            this.Size = new Size(1000, container.RowCount * 35 + 70);
            this.Controls.Add(withReportContainer);
            //32 - 30(высота строки в container + 2 запаса. Пофиксить волшебные числа ?
            this.Load += (sender, args) => { this.Size = new Size(1000, container.RowCount * 35 + 70); };

        }


        internal class ParameterControlBinding
        {
            internal AddInsParameter Parameter { get; set; }
            internal System.Windows.Forms.Control InputControl { get; set; }
            internal Label ErrorLabel { get; set; }

            internal void WriteValueIntoParameter()
            {

                var controlAsTextBox = InputControl as TextBox;
                if (controlAsTextBox != null) Parameter.Value = controlAsTextBox.Text;

                var controlAsComboBox = InputControl as ComboBox;
                if (controlAsComboBox != null) Parameter.Value = controlAsComboBox.SelectedItem.ToString();

                var controlAsCheckBox = InputControl as CheckBox;
                if (controlAsCheckBox != null) Parameter.Value = controlAsCheckBox.Checked.ToString();
            }
            internal void DisplayError()
            {
                if (Parameter.ErrorMessage == null) Parameter.ErrorMessage = string.Empty;
                ErrorLabel.Text = Parameter.ErrorMessage;
                Parameter.ErrorMessage = string.Empty;
            }

        }





    }


    internal static class AddInsParameterExtensions
    {
        internal static System.Windows.Forms.Control CreatControl(this AddInsParameter parameter)
        {
            System.Windows.Forms.Control paramControl = null;
            switch (parameter.ControlType)
            {
                case ControlType.TextBox:
                    paramControl = new TextBox();
                    paramControl.Text = parameter.Value ?? string.Empty;
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
