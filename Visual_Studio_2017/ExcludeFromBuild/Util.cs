//------------------------------------------------------------------------------
// <copyright>
//     Copyright (c) Adam Wulkiewicz.
// </copyright>
//------------------------------------------------------------------------------

using EnvDTE;
using EnvDTE80;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace ExcludeFromBuild
{
    class Util
    {
        public enum Configuration { Active, All };

#pragma warning disable VSTHRD010
        public static void SetExcludedFromBuild(DTE2 dte, bool value,
                                                Configuration configuration = Configuration.Active)
        {
            if (dte == null)
                return;
            var items = dte.ToolWindows.SolutionExplorer.SelectedItems as Array;
            if (items == null)
                return;
            SetExcludedFromBuildRecursive(items, value, configuration);
        }

        // Casting COM objects to VCFile and VCFilter works but the problem is that
        // for VS2017 and VS2019 one has to add different Microsoft.VisualStudio.VCProjectEngine
        // version than for VS2015. Otherwise casting resutls in null object.
        // So below call properties of CPM objects manually.

        private static void SetExcludedFromBuildRecursive(IEnumerable items,
                                                          bool value,
                                                          Configuration configuration)
        {
            foreach (var item in items)
            {
                UIHierarchyItem hitem = item as UIHierarchyItem;
                if (hitem != null)
                {
                    var pitem = hitem.Object as ProjectItem;
                    if (pitem != null)
                    {
                        if (hitem.UIHierarchyItems.Count > 0)
                        {
                            SetExcludedFromBuildRecursive(hitem.UIHierarchyItems, value, configuration);
                        }
                        else
                        {
                            var kind = pitem.Object.GetType().InvokeMember("Kind",
                                            BindingFlags.GetProperty,
                                            null, pitem.Object, null) as string;
                            if (kind == "VCFile")
                            {
                                var activeConfig = pitem.ContainingProject.ConfigurationManager.ActiveConfiguration;
                                string activeConfigName = activeConfig.ConfigurationName;
                                string activePlatformName = activeConfig.PlatformName;

                                var fileConfigurations = pitem.Object.GetType().InvokeMember("FileConfigurations",
                                                            BindingFlags.GetProperty,
                                                            null, pitem.Object, null) as IEnumerable;

                                foreach (var c in fileConfigurations)
                                {
                                    bool set = (configuration == Configuration.All);

                                    if (!set)
                                    {
                                        var config = c.GetType().InvokeMember("ProjectConfiguration",
                                                        BindingFlags.GetProperty,
                                                        null, c, null);
                                        var cPath = config.GetType().InvokeMember("PersistPath",
                                                        BindingFlags.GetProperty,
                                                        null, config, null) as string;
                                        var cName = config.GetType().InvokeMember("ConfigurationName",
                                                        BindingFlags.GetProperty,
                                                        null, config, null) as string;
                                        var cPlat = config.GetType().InvokeMember("PlatformName",
                                                        BindingFlags.GetProperty,
                                                        null, config, null) as string;

                                        set = activeConfigName == cName
                                                && activePlatformName == cPlat;
                                    }

                                    if (set)
                                    {
                                        c.GetType().InvokeMember("ExcludedFromBuild",
                                                BindingFlags.SetProperty,
                                                null, c, new object[] { value });
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
#pragma warning restore VSTHRD010
    }
}
