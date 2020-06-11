//------------------------------------------------------------------------------
// <copyright>
//     Copyright (c) Adam Wulkiewicz.
// </copyright>
//------------------------------------------------------------------------------

using Microsoft.VisualStudio.Shell;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ExcludeFromBuild
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(ExcludeFromBuildPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideOptionPage(typeof(ExcludeFromBuildOptionPage), "Exclude From Build", "General", 0, 0, true)]
    public sealed class ExcludeFromBuildPackage : Package
    {
        /// <summary>
        /// ExcludeFromBuildPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "90dd6cdb-b4c3-4e74-9445-b4b045cb6b7e";

        /// <summary>
        /// ExcludeFromBuildPackage Instance set during initialization of the package.
        /// </summary>
        public static ExcludeFromBuildPackage Instance { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExcludeFromBuildPackage"/> class.
        /// </summary>
        public ExcludeFromBuildPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Instance = this;

            base.Initialize();

            ExcludeFromBuildCommand.Initialize(this);
            IncludeInBuildCommand.Initialize(this);
            ExcludeFromBuildAllCommand.Initialize(this);
        }

        public new object GetService(Type serviceType)
        {
            return base.GetService(serviceType);
        }

        public new DialogPage GetDialogPage(Type dialogPageType)
        {
            return base.GetDialogPage(dialogPageType);
        }
    }
}
