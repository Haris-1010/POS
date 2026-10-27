using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Web;

namespace CoreMedixBeta.Enums
{
    public static class EnumExtensions
    {
        // Extension method to get the Display Name of an enum value
        public static string GetDisplayName(this Enum value)
        {
            // Get the field info for the enum value
            var field = value.GetType().GetField(value.ToString());

            // Check if the Display attribute is applied to the enum value
            var attribute = field.GetCustomAttribute<DisplayAttribute>();

            // Return the Name from the Display attribute, or fallback to enum's string name
            return attribute?.Name ?? value.ToString();
        }
    }
}