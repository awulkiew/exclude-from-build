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

        private class BuildAction
        {
            public BuildAction(DTE2 dte)
            {
                int i = dte.Version.IndexOf('.');
                if (i < 0)
                    i = dte.Version.Length;
                version = int.Parse(dte.Version.Substring(0, i));
            }

            private readonly int version;

            public int None { get { return 0; } }
            public int Compile { get { return 1; } }
            public int ApplicationDefinition { get { return version >= 15 ? 6 : 4; } }
            public int Page { get { return version >= 15 ? 7 : 5; } }            
        }

#pragma warning disable VSTHRD010
        public static void SetExcludedFromBuild(DTE2 dte, bool value, Configuration configuration)
        {
            if (dte == null)
                return;
            var items = dte.ToolWindows.SolutionExplorer.SelectedItems as Array;
            if (items == null)
                return;
            BuildAction buildAction = new BuildAction(dte);
            SetExcludedFromBuildRecursive(items, value, configuration, buildAction);
        }

        // Casting COM objects to VCFile and VCFilter works but the problem is that
        // for VS2017 and VS2019 one has to add different Microsoft.VisualStudio.VCProjectEngine
        // version than for VS2015. Otherwise casting resutls in null object.
        // So below call properties of COM objects manually.

        private static void SetExcludedFromBuildRecursive(IEnumerable items,
                                                          bool value,
                                                          Configuration configuration,
                                                          BuildAction buildAction)
        {
            foreach (var item in items)
            {
                UIHierarchyItem hitem = item as UIHierarchyItem;
                if (hitem == null)
                    continue;

                // Any container, e.g. filter or C# XAML, etc.
                if (hitem.UIHierarchyItems.Count > 0)
                {
                    SetExcludedFromBuildRecursive(hitem.UIHierarchyItems, value, configuration, buildAction);
                }

                // For C++ this is Microsoft.VisualStudio.VCProjectEngine.VCProjectItem
                // For C# this is VSLangProj.VSProjectItem (?)
                var pitem = hitem.Object as ProjectItem;
                if (pitem == null)
                    continue;

                string extension = GetPropertyValue(pitem, "Extension") as string;
                if (extension == null)
                    continue;
                extension = extension.ToLowerInvariant();

                // C#
                if (extension == ".cs" || extension == ".xaml")
                {
                    Property buildActionProp = GetProperty(pitem, "BuildAction");
                    if (buildActionProp == null)
                        continue;
                    if (extension == ".cs")
                        buildActionProp.Value = value ? buildAction.None : buildAction.Compile;
                    else //if (extension == ".xaml")
                        buildActionProp.Value = value ? buildAction.None : buildAction.Page;
                }
                else if (extension == ".cpp" || extension == ".c" || extension == ".cc" || extension == ".cxx")
                {
                    string kind = GetPropertyValue(pitem, "Kind") as string;
                    if (kind == "VCFile")
                    {
                        var activeConfig = pitem.ContainingProject.ConfigurationManager.ActiveConfiguration;
                        string activeConfigName = activeConfig.ConfigurationName;
                        string activePlatformName = activeConfig.PlatformName;

                        Property fileConfigurationsProp = GetProperty(pitem, "FileConfigurations");
                        if (fileConfigurationsProp == null)
                            continue;
                        var fileConfigurations = fileConfigurationsProp.Object as IEnumerable;
                        if (fileConfigurations == null)
                            continue;

                        foreach (var c in fileConfigurations)
                        {
                            bool set = (configuration == Configuration.All);

                            if (!set)
                            {
                                var config = GetPropertyValue(c, "ProjectConfiguration");
                                if (config != null)
                                {
                                    var cName = GetPropertyValue(config, "ConfigurationName") as string;
                                    var cPlat = GetPropertyValue(config, "PlatformName") as string;

                                    set = activeConfigName == cName
                                            && activePlatformName == cPlat;
                                }
                            }

                            if (set)
                            {
                                SetPropertyValue(c, "ExcludedFromBuild", value);
                            }
                        }
                    }
                }
            }
        }
#pragma warning restore VSTHRD010

        private static void SetPropertyValue(object o, string name, object value)
        {
            o.GetType().InvokeMember(name,
                                     BindingFlags.SetProperty,
                                     null, o, new object[] { value });
        }

        private static object GetPropertyValue(object o, string name)
        {
            try
            {
                return o.GetType().InvokeMember(name,
                                BindingFlags.GetProperty,
                                null, o, null);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static Property GetProperty(ProjectItem pitem, string name)
        {
            try
            {
                return pitem.Properties.Item(name);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static object GetPropertyValue(ProjectItem pitem, string name)
        {
            Property prop = GetProperty(pitem, name);
            return prop != null ? prop.Value : null;
        }

        public static Configuration GetConfigurationOption()
        {
            var package = ExcludeFromBuildPackage.Instance;
            var options = (ExcludeFromBuildOptionPage)package.GetDialogPage(typeof(ExcludeFromBuildOptionPage));
            return options.DefaultConfiguration == ExcludeFromBuildOptionPage.Configuration.All
                 ? Configuration.All
                 : Configuration.Active;
        }
    }
}
