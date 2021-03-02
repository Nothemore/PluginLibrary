using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PluginStandard;
using System.Reflection;

namespace PluginLibrary
{
    //Маркировочный интерфейс
    public interface IPluginSettings
    { }


    //Попробовать выщить значениеатрибуа
    //Enum сереализуеться, addins параметр атрибута ми

    public static class IPluginSettingExtension
    {
        public static IEnumerable<AddInsParameter> GetParameters(this IPluginSettings settings)
        {
            var properties = settings.GetType().GetProperties();
            var result = new List<AddInsParameter>();
            foreach (var prop in properties)
            {
                var attribute = prop.GetCustomAttribute<AddInsParameter>();
                if (attribute == null) continue;
                attribute.PropertyName = prop.Name;
                if (prop.PropertyType.Name == "Boolean") attribute.ControlType = ControlType.CheckBox;
                else if (attribute.AvailableValue != null) attribute.ControlType = ControlType.ComboBox;
                else attribute.ControlType = ControlType.TextBox;

                //attribute.ControlType = IPluginSettingExtension.GetInputType(prop.PropertyType);
                result.Add(attribute);
            }
            return result;
        }

        //Можно ли устранить дублирование кода
        public static bool SetParameters(this IPluginSettings settings, IEnumerable<AddInsParameter> parameters)
        {

            var result = true;
            var type = settings.GetType();
            //Нужно проверять, что все свойства получили значение, а не количество свойств.
            if (type.GetProperties().Length != parameters.Count()) return false;
            foreach (var param in parameters)
            {
                if (param.Value == null || param.Value ==string.Empty)
                {
                    param.ErrorMessage = "Значение не задано";
                    result = false;
                    continue;
                }
                if (param.AvailableValue != null && !param.AvailableValue.Contains(param.Value))
                {
                    param.ErrorMessage = "Задано недопустимое значение";
                    result = false;
                    continue;
                }

                var prop = type.GetProperty(param.PropertyName);
                if (prop != null && !(param.Value == null))
                {
                    switch (prop.PropertyType.Name)
                    {
                        case "Int32":
                            int parseInt = 0;
                            if (int.TryParse(param.Value, out parseInt))
                                prop.SetValue(settings, parseInt);
                            else
                                param.ErrorMessage = "Не является целым числом";
                            break;
                        case "Double":
                            double parseDouble = 0;
                            if (double.TryParse(param.Value, out parseDouble))
                                prop.SetValue(settings, double.Parse(param.Value));
                            else
                            {
                                param.ErrorMessage = "Не является числом с плавающей точкой";
                                result = false;
                            }
                            break;
                        case "Boolean":
                            bool parseBool = false;
                            if (bool.TryParse(param.Value, out parseBool))
                                prop.SetValue(settings, parseBool);
                            else
                            {
                                param.ErrorMessage = "Задано некорректное значение";
                                result = false;
                            }
                            break;
                        default:
                            prop.SetValue(settings, param.Value);
                            break;
                    }
                }
                else
                {
                    param.ErrorMessage += "Не удалось найти свойство";
                    result = false;
                }
                var propValue = prop.GetValue(settings);
                if (propValue != null && propValue.ToString() != param.Value)
                {
                    param.ErrorMessage = "Не удалось установить значение для свойтсва";
                    result = false;
                }
            }
            return result;
        }

        //Move to another class?
        public static ControlType GetInputType(Type propertyType)
        {
            switch (propertyType.Name)
            {
                case "Boolean":
                    return ControlType.CheckBox;
                default:
                    return ControlType.TextBox;
            }
        }
    }

}
