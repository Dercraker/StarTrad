﻿using StarTrad.Helper;
using StarTrad.Tool;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows;
using System.Windows.Forms;

namespace StarTrad
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public const string PROGRAM_NAME = "StarTrad";

        // Command line arguments
        public const string ARGUMENT_INSTALL = "/install";
        public const string ARGUMENT_LAUNCH = "/launch";

        // Full path to the location where this program is running
        public static readonly string workingDirectoryPath = AppDomain.CurrentDomain.BaseDirectory;

        private static readonly ApplicationContext applicationContext = new ApplicationContext();
        private static readonly NotifyIcon notifyIcon = new NotifyIcon();

        private readonly ToolStripMenuItem installMenuItem;
        private readonly ToolStripMenuItem installAndLaunchMenuItem;
        private readonly ToolStripMenuItem uninstallMenuItem;

        public App() : base()
        {
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Setup exception handlers
            AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
            System.Windows.Forms.Application.ThreadException += OnApplicationThreadException;
            System.Windows.Forms.Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            // Initialize logger
            LoggerFactory.Setup();
            LoggerFactory.LogInformation("Démarrage de StarTrad");

            // Create notify icon
            this.installMenuItem = new ToolStripMenuItem("Installer la traduction", null, new EventHandler(this.InstallMenuItem_Click));
            this.installAndLaunchMenuItem = new ToolStripMenuItem("Installer la traduction et lancer le jeu", null, new EventHandler(this.InstallAndLaunchMenuItem_Click));
            this.uninstallMenuItem = new ToolStripMenuItem("Désinstaller la traduction", null, new EventHandler(this.UninstallMenuItem_Click));
            this.CreateNotifyIcon();

            // Handle command line arguments
            if (this.HandleCommandLineArguments()) {
                return;
            }

            // Makes sure only one instance can run at a time
            this.HandleSingleInstance();

            // Initialize update scheduler
            UpdateTranslation.OnUpdateTriggered += this.OnAutoUpdateTriggered;
            UpdateTranslation.StartAutoUpdate();

            // Say hello
            App.Notify(ToolTipIcon.Info, "StarTrad démarré ! Retrouvez-le dans la zone de notification en bas à droite.");

            System.Windows.Forms.Application.Run(applicationContext);
        }

        #region Static

        /// <summary>
        /// Displays a message from the notify icon.
        /// </summary>
        /// <param name="icon"></param>
        /// <param name="message"></param>
        public static void Notify(ToolTipIcon icon, string message)
        {
            App.notifyIcon.ShowBalloonTip(2000, PROGRAM_NAME, message, icon);
        }

        #endregion

        #region Private

        /// <summary>
        /// Handles arguments passed to the program.
        /// </summary>
        private bool HandleCommandLineArguments()
        {
            string[] args = Environment.GetCommandLineArgs();

            if (!this.Contains(args, ARGUMENT_INSTALL))
            {
                return false;
            }

            TranslationInstaller.Install(false, (sender, success) =>
            {
                if (success && this.Contains(args, ARGUMENT_LAUNCH))
                {
                    RsiLauncherFolder.ExecuteRsiLauncher();
                }

                this.ExitApplication();
            });

            return true;
        }

        /// <summary>
        /// Prevents multiple instances of the program to run at the same time.
        /// </summary>
        private void HandleSingleInstance()
        {
            bool createdNew;
            new Mutex(true, PROGRAM_NAME, out createdNew);  

            if (!createdNew) {
                this.ExitApplication();
            }
        }

        /// <summary>
        /// Creates the icon and its context menu to be displayed in the system tray.
        /// </summary>
        private void CreateNotifyIcon()
        {
            ContextMenuStrip cms = new ContextMenuStrip();

            ToolStripMenuItem titleMenuItem = new ToolStripMenuItem("Traduction :");
            titleMenuItem.Enabled = false;

            cms.Items.Add(installMenuItem);
            cms.Items.Add(installAndLaunchMenuItem);
            cms.Items.Add(uninstallMenuItem);
            cms.Items.Add(new ToolStripSeparator());
            cms.Items.Add(new ToolStripMenuItem("Options avancées", null, new EventHandler(this.SettingsMenuItem_Click)));
            cms.Items.Add(new ToolStripMenuItem("Quitter", null, new EventHandler(this.ExitMenuItem_Click)));

            notifyIcon.ContextMenuStrip = cms;
            notifyIcon.Icon = new Icon(workingDirectoryPath + @"\StarTrad.ico");
            notifyIcon.Visible = true;
            notifyIcon.Text = PROGRAM_NAME;
        }

        /// <summary>
        /// Checks if a given string list contains a certain string.
        /// </summary>
        /// <param name="strings"></param>
        /// <param name="needle"></param>
        /// <returns></returns>
        private bool Contains(string[] strings, string needle)
        {
            foreach (string str in strings)
            {
                if (str == needle)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Enables or disables the notify icon's menu items.
        /// </summary>
        /// <param name="enabled"></param>
        private void SetMenuItemsState(bool enabled)
        {
            this.installMenuItem.Enabled = enabled;
            this.installAndLaunchMenuItem.Enabled = enabled;
            this.uninstallMenuItem.Enabled = enabled;
        }

        /// <summary>
        /// Terminates the application.
        /// </summary>
        private void ExitApplication()
        {
            LoggerFactory.LogInformation("Fermeture de StarTrad");

            applicationContext.ExitThread();
            System.Windows.Forms.Application.Exit();
            this.Shutdown();

            Process process = Process.GetCurrentProcess();
            process.Kill();
            process.Dispose();
        }

        /// <summary>
		/// Writes a crash log file from an exception.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void WriteCrashLog(Exception e)
        {
            LoggerFactory.LogError(e.Message);
            LoggerFactory.LogError(e.Source);
            LoggerFactory.LogError(e.Data.ToString());
            LoggerFactory.LogError(e.ToString());
            LoggerFactory.LogError(e.StackTrace);

            this.ExitApplication();

            // Prevent from having a Windows messagebox about the crash
            Process process = Process.GetCurrentProcess();
            process.Kill();
            process.Dispose();
        }

        #endregion

        #region Event

        /// <summary>
        /// Called when clicking on the "Install" tray menu item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InstallMenuItem_Click(object? sender, EventArgs e)
        {
            LoggerFactory.LogInformation("Lancement de la recherche de mise a jour");

            this.SetMenuItemsState(false);

            TranslationInstaller.Install(false, (sender, success) =>
            {
                this.SetMenuItemsState(true);
            });
        }

        /// <summary>
        /// Called when clicking on the "Install & Launch" tray menu item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InstallAndLaunchMenuItem_Click(object? sender, EventArgs e)
        {
            this.SetMenuItemsState(false);

            TranslationInstaller.Install(false, (sender, success) =>
            {
                if (success) RsiLauncherFolder.ExecuteRsiLauncher();
                this.SetMenuItemsState(true);
            });
        }

        /// <summary>
        /// Called when clicking on the "Uninstall" tray menu item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UninstallMenuItem_Click(object? sender, EventArgs e)
        {
            this.SetMenuItemsState(false);

            TranslationInstaller.Uninstall(false);

            this.SetMenuItemsState(true);
        }

        /// <summary>
        /// Called when clicking on the "Settings" tray menu item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SettingsMenuItem_Click(object? sender, EventArgs e)
        {
            LoggerFactory.LogInformation("Ouverture des paramètres");
            View.Window.Settings settingsWindow = new View.Window.Settings();
            settingsWindow.ShowDialog();
        }

        /// <summary>
        /// Called when clicking on the "Exit" tray menu item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitMenuItem_Click(object? sender, EventArgs e)
        {
            this.ExitApplication();
        }

        /// <summary>
        /// Called when an unhandled exception happens on the Application thread.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnApplicationThreadException(object sender, ThreadExceptionEventArgs e)
        {
            this.WriteCrashLog(e.Exception);
        }

        /// <summary>
        /// Called when an unhandled exception happens for the current AppDomain.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            this.WriteCrashLog((Exception)e.ExceptionObject);
        }

        /// <summary>
        /// Called when the UpdateTranslation() tool triggers an automatic update of the translation.
        /// </summary>
        /// <param name="sender"></param>
        private void OnAutoUpdateTriggered(object? sender)
        {
            this.SetMenuItemsState(false);

            TranslationInstaller.Install(true, (sender, success) =>
            {
                this.SetMenuItemsState(true);
            });
        }

        #endregion
    }
}
