
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;


namespace Dfc.ProviderPortal.ChangeFeedListener.Helpers
{
    public static class EnumHelper
    {
        /// <summary>
        /// Gets the Description attribute on an enum value
        /// </summary>
        public static string Description(this Enum value)
        {
            object attribute = value.GetType()
                                   ?.GetMember(value.ToString())
                                   ?.FirstOrDefault()
                                   ?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                                   ?.FirstOrDefault();
            return ((DescriptionAttribute)attribute)?.Description;
        }
    }
}
