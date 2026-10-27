using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CoreMedixBeta.Enums
{
    public enum QtyFilter
    {
        [Display(Name = "All")]
        None,

        [Display(Name = "Greater Than")]
        GreaterThan,

        [Display(Name = "Equal To")]
        EqualTo,

        [Display(Name = "Greater Than or Equal To")]
        GreaterThanOrEqualTo,

        [Display(Name = "Less Than")]
        LessThan,

        [Display(Name = "Less Than or Equal To")]
        LessThanOrEqualTo
    }
}