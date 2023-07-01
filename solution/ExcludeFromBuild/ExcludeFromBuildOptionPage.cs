//------------------------------------------------------------------------------
// <copyright>
//     Copyright (c) Adam Wulkiewicz.
// </copyright>
//------------------------------------------------------------------------------

using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;

namespace ExcludeFromBuild
{
    public class ExcludeFromBuildOptionPage : DialogPage
    {
        public enum Configuration
        {
            [Description("Active")]
            Active,
            [Description("All")]
            All
        };

        [Category("General")]
        [DisplayName("Default Configuration(s)")]
        [Description("Defines which configurations are affected when the basic buttons are chosen.")]
        public Configuration DefaultConfiguration { get; set; } = Configuration.Active;
    }
}
