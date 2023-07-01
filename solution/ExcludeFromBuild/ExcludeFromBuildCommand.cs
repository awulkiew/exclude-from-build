//------------------------------------------------------------------------------
// <copyright>
//     Copyright (c) Adam Wulkiewicz.
// </copyright>
//------------------------------------------------------------------------------

using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace ExcludeFromBuild
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class ExcludeFromBuildCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("29f38563-d98e-4d70-ae65-94c712f4b871");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        private readonly DTE2 dte;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExcludeFromBuildCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private ExcludeFromBuildCommand(AsyncPackage package, OleMenuCommandService commandService, DTE2 dte)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            this.dte = dte ?? throw new ArgumentNullException(nameof(dte));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static ExcludeFromBuildCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            DTE2 dte = await package.GetServiceAsync(typeof(DTE)) as DTE2;
            Instance = new ExcludeFromBuildCommand(package, commandService, dte);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Util.SetExcludedFromBuild(dte, true, Util.GetConfigurationOption());
        }

    }
}
