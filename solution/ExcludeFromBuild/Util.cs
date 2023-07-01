//------------------------------------------------------------------------------
// <copyright>
//     Copyright (c) Adam Wulkiewicz.
// </copyright>
//------------------------------------------------------------------------------

using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace ExcludeFromBuild
{
    class Util
    {
        public enum Configuration { Active, All };

        //VSLangProj.prjBuildAction
        private enum BuildAction
        {
            None = 0,
            Compile = 1,
            Content = 2,
            EmbeddedResource = 3
        }

        private class ItemType
        {
            //public static readonly string None = "None";
            //public static readonly string Compile = "Compile";
            public static readonly string ApplicationDefinition = "ApplicationDefinition";
            public static readonly string Page = "Page";
            public static readonly string Resource = "Resource";
        }

#pragma warning disable VSTHRD010
        public static void SetExcludedFromBuild(DTE2 dte, bool value, Configuration configuration)
        {
            if (dte == null)
                return;
            var items = dte.ToolWindows.SolutionExplorer.SelectedItems as Array;
            if (items == null)
                return;
            HashSet<string> visited = new HashSet<string>();
            SetExcludedFromBuildRecursive(items, value, configuration, visited);
        }

        // Casting COM objects to VCFile and VCFilter works but the problem is that
        // for VS2017 and VS2019 one has to add different Microsoft.VisualStudio.VCProjectEngine
        // version than for VS2015. Otherwise casting resutls in null object.
        // So below call properties of COM objects manually.

        private static void SetExcludedFromBuildRecursive(IEnumerable items,
                                                          bool value,
                                                          Configuration configuration,
                                                          HashSet<string> visited)
        {
            foreach (var item in items)
            {
                UIHierarchyItem hitem = item as UIHierarchyItem;
                if (hitem == null)
                    continue;

                // Get unique name of current item
                string name = null;
                if (!IsVisitableItem(hitem, out name))
                    continue;

                // Ignore already visited items if possible
                if (name != null)
                {
                    if (visited.Contains(name))
                        continue;
                    else
                        visited.Add(name);
                }

                // Not expanded UIHierarchyItems report 0 Items
                if (!hitem.UIHierarchyItems.Expanded)
                {
                    hitem.UIHierarchyItems.Expanded = true;
                    hitem.UIHierarchyItems.Expanded = false;
                }

                // Any container, e.g. filter or C# XAML, etc.
                if (hitem.UIHierarchyItems.Count > 0)
                {
                    SetExcludedFromBuildRecursive(hitem.UIHierarchyItems,
                                                  value,
                                                  configuration,
                                                  visited);
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

                // C#, VB
                if (extension == ".cs" || extension == ".vb")
                {
                    SetPropertyValue(pitem, "BuildAction",
                        (int)(value ? BuildAction.None : BuildAction.Compile));
                }
                // WPF
                else if (extension == ".xaml")
                {
                    if (value)
                        SetPropertyValue(pitem, "BuildAction", (int)BuildAction.None);
                    else
                    {
                        bool isSet = false;
                        string url = GetPropertyValue(pitem, "URL") as string;
                        if (url != null)
                        {
                            string rootName = RootXMLElementName(url);
                            if (rootName != null)
                            {
                                if (rootName == "Application")
                                    SetPropertyValue(pitem, "ItemType", ItemType.ApplicationDefinition);
                                else
                                    SetPropertyValue(pitem, "ItemType", ItemType.Page);
                                isSet = true;
                            }
                        }

                        if (!isSet)
                            SetPropertyValue(pitem, "ItemType", ItemType.Page);
                    }
                }
                // C, C++
                else if (extension == ".c" || extension == ".cc"
                    || extension == ".cpp" || extension == ".cxx" || extension == ".c++"
                    || extension == ".m" || extension == ".mm")
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

        private static bool IsVisitableItem(UIHierarchyItem hitem, out string name)
        {
            name = null;

            var pitem = hitem.Object as ProjectItem;
            if (pitem != null)
            {
                name = GetPropertyValue(pitem, "FullPath") as string;
                if (name == null)
                {
                    if (pitem.Kind == VSConstants.ItemTypeGuid.VirtualFolder_string
                        && pitem.ContainingProject != null)
                    {
                        name = pitem.ContainingProject.FullName + "\\"
                             + GetPropertyValue(pitem, "CanonicalName") as string;
                    }
                }
            }
            else
            {
                var proj = hitem.Object as Project;
                if (proj != null)
                {
                    name = proj.FullName;
                }
                else
                {
                    var solution = hitem.Object as Solution;
                    if (solution != null)
                        name = solution.FullName;
                    else
                        // Could not be casted to neither ProjectItem, Project nor Solution.
                        // This is probably a reference, so ignore it.
                        return false;
                }
            }

            return true;
        }

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

        private static Property GetProperty(Project proj, string name)
        {
            try
            {
                return proj.Properties.Item(name);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static Property GetProperty(Solution solution, string name)
        {
            try
            {
                return solution.Properties.Item(name);
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

        private static object GetPropertyValue(Project proj, string name)
        {
            Property prop = GetProperty(proj, name);
            return prop != null ? prop.Value : null;
        }

        private static object GetPropertyValue(Solution solution, string name)
        {
            Property prop = GetProperty(solution, name);
            return prop != null ? prop.Value : null;
        }

        private static void SetPropertyValue(ProjectItem pitem, string name, object value)
        {
            Property prop = GetProperty(pitem, name);
            if (prop != null)
                prop.Value = value;
        }

        private static void SetPropertyValue(Project proj, string name, object value)
        {
            Property prop = GetProperty(proj, name);
            if (prop != null)
                prop.Value = value;
        }

        private static void SetPropertyValue(Solution solution, string name, object value)
        {
            Property prop = GetProperty(solution, name);
            if (prop != null)
                prop.Value = value;
        }

        private static List<string> GetPropertiesNames(ProjectItem pitem)
        {
            List<string> result = new List<string>();
            foreach (Property p in pitem.Properties)
                result.Add(p.Name);
            return result;
        }

        private static List<string> GetPropertiesNames(Project proj)
        {
            List<string> result = new List<string>();
            foreach (Property p in proj.Properties)
                result.Add(p.Name);
            return result;
        }

        private static List<string> GetPropertiesNames(Solution solution)
        {
            List<string> result = new List<string>();
            foreach (Property p in solution.Properties)
                result.Add(p.Name);
            return result;
        }

        public static Configuration GetConfigurationOption()
        {
            var package = ExcludeFromBuildPackage.Instance;
            var options = (ExcludeFromBuildOptionPage)package.GetDialogPage(typeof(ExcludeFromBuildOptionPage));
            return options.DefaultConfiguration == ExcludeFromBuildOptionPage.Configuration.All
                 ? Configuration.All
                 : Configuration.Active;
        }

        private static string RootXMLElementName(string url)
        {
            try
            {
                using (System.Xml.XmlReader reader = System.Xml.XmlReader.Create(url))
                {
                    reader.MoveToContent();
                    return reader.Name;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
