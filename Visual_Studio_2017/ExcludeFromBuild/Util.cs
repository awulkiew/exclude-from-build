//------------------------------------------------------------------------------
// <copyright>
//     Copyright (c) Adam Wulkiewicz.
// </copyright>
//------------------------------------------------------------------------------

using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace ExcludeFromBuild
{
    class Util
    {
#pragma warning disable VSTHRD010
        public static void SetExcludedFromBuild(DTE2 dte, bool value)
        {
            if (dte == null)
                return;
            var items = dte.ToolWindows.SolutionExplorer.SelectedItems as Array;
            if (items == null)
                return;
            SetExcludedFromBuildRecursive(items, value);
        }

        private static void SetExcludedFromBuildRecursive(IEnumerable items, bool value)
        {
            foreach (var item in items)
            {
                UIHierarchyItem hitem = item as UIHierarchyItem;
                if (hitem != null)
                {
                    var pitem = hitem.Object as ProjectItem;
                    if (pitem != null)
                    {
                        VCFilter filter = pitem.Object as VCFilter;
                        if (filter != null)
                        {
                            SetExcludedFromBuildRecursive(hitem.UIHierarchyItems, value);
                        }
                        else
                        {
                            VCFile file = pitem.Object as VCFile;
                            if (file != null)
                            {
                                foreach (var f in file.FileConfigurations)
                                {
                                    var fc = f as VCFileConfiguration;
                                    if (fc != null)
                                    {
                                        fc.ExcludedFromBuild = value;
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
