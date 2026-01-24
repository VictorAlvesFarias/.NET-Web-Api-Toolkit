using System.ComponentModel;
using System.Reflection;

namespace Application.Extensions
{
    public static class EnumExtensions
    {
        public static string ToDescription(this Enum value)
        {
            FieldInfo field = value.GetType().GetField(value.ToString());
            
            if (field == null)
            {
                return value.ToString();
            }

            DescriptionAttribute attribute = field.GetCustomAttribute<DescriptionAttribute>();
            
            return attribute?.Description ?? value.ToString();
        }
    }
}

