﻿using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Collections.Generic;
using StarTrad.Helper;
using StarTrad.Helper.ComboxList;
using StarTrad.Tool;
using StarTrad.Enum;

namespace StarTrad.View.Window
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : System.Windows.Window
    {
        public const string CHANNEL_ALL = "<Tous>";

        public Settings()
        {
            InitializeComponent();

            // Add ComboBox items
            this.SetupChannelsComboBox();
            this.AddComboBoxItemsFromEnum<TranslationUpdateMethodEnum>(this.ComboBox_TranslationUpdateMethod);

            this.CheckBox_StartWithWindows.IsChecked = File.Exists(ShortcutTool.startupShortcutPath);

            // Bind the Checked events after the initial IsChecked assignation so they won't be triggered by it
            this.CheckBox_StartWithWindows.Checked += this.CheckBox_StartWithWindows_Checked;
            this.CheckBox_StartWithWindows.Unchecked += this.CheckBox_StartWithWindows_Unchecked;

            this.TextBox_LibraryFolder.Text = Properties.Settings.Default.RsiLauncherLibraryFolder;

            this.ComboBox_Channel.Text = Properties.Settings.Default.RsiLauncherChannel;
            this.ComboBox_Channel.SelectionChanged += this.ComboBox_Channel_SelectionChanged;

            this.ComboBox_TranslationUpdateMethod.SelectedIndex = Properties.Settings.Default.TranslationUpdateMethod;
            this.ComboBox_TranslationUpdateMethod.SelectionChanged += this.ComboBox_TranslationUpdateMethod_SelectionChanged;
        }

        #region Events

        /// <summary>
        /// Called when checking the "Start with Windows" checkbox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_StartWithWindows_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            LoggerFactory.LogInformation("Activation du démarrage de StarTrad avec windows");
            ShortcutTool.CreateStartupShortcut();
        }

        /// <summary>
        /// Called when unchecking the "Start with Windows" checkbox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_StartWithWindows_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            LoggerFactory.LogInformation("Désactivation du démarrage de StarTrad avec windows");

            if (!File.Exists(ShortcutTool.startupShortcutPath)) {
                return;
            }

            try {
                File.Delete(ShortcutTool.startupShortcutPath);
            } catch (Exception ex) {
                LoggerFactory.LogError(ex);
            }
        }

        /// <summary>
        /// Called when the channel ComboBox's dropdown gets closed.
        /// </summary>
        private void ComboBox_Channel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoggerFactory.LogInformation($"Changement de la valeur du canal par : {this.ComboBox_Channel.Text.Trim()}");
        }

        /// <summary>
        /// Called when the Translation update methods ComboBox's dropdown gets closed.
        /// </summary>
        private void ComboBox_TranslationUpdateMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoggerFactory.LogInformation($"Changement de la valeur de la méthode d'update par : {this.ComboBox_TranslationUpdateMethod.Text.Trim()}");
        }

        /// <summary>
        /// Called when clicking on the "CreateDesktopShortcut" button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_CreateDesktopShortcut_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DesktopShortcutCreationResult result = ShortcutTool.CreateDesktopShortcut();

            switch (result) {
                case DesktopShortcutCreationResult.AlreadyExists: MessageBox.Show("Le raccourci existe déjà sur le bureau."); break;
                case DesktopShortcutCreationResult.CreationFailed: MessageBox.Show("la création du raccourci a échouée."); break;
                case DesktopShortcutCreationResult.SuccessfulyCreated: MessageBox.Show("Raccourci créé avec succès !"); break;
            }
        }

        /// <summary>
        /// Called when clicking on the "Configure" button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_ExternalTools_Click(object sender, System.Windows.RoutedEventArgs e)
		{
            ExternalTools externalToolsWindow = new ExternalTools(this);
            externalToolsWindow.ShowDialog();
        }

        /// <summary>
        /// Called when clicking on the "Save" button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Save_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            LoggerFactory.LogInformation("Sauvegarde et fermeture des paramètres");
            string libraryFolderPath = this.TextBox_LibraryFolder.Text.Trim();

            if (libraryFolderPath.Length > 0 && !Directory.Exists(this.TextBox_LibraryFolder.Text)) {
                MessageBox.Show($"Le dossier \"{this.TextBox_LibraryFolder.Text}\" n'existe pas.\n\nVous pouvez laisser le champ vide pour que le programme le détecte automatiquement.");

                return;
            }

            Properties.Settings.Default.RsiLauncherLibraryFolder = libraryFolderPath;
            Properties.Settings.Default.RsiLauncherChannel = this.ComboBox_Channel.Text;
            Properties.Settings.Default.TranslationUpdateMethod = (byte)(TranslationUpdateMethodEnum)((ComboBoxItem)this.ComboBox_TranslationUpdateMethod.SelectedItem).Tag;

            Properties.Settings.Default.Save();

            UpdateTranslation.ReloadAutoUpdate();

            this.Close();
        }

        #endregion

        /// <summary>
        /// Adds all the values of an Enum as items for a ComboBox.
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="comboBox"></param>
        /// <param name="e"></param>
        private void AddComboBoxItemsFromEnum<TEnum>(System.Windows.Controls.ComboBox comboBox)
        {
            foreach (System.Enum value in System.Enum.GetValues(typeof(TEnum))) {
                ComboBoxItem item = new ComboBoxItem();
                item.Tag = value;
                item.Content = EnumHelper.GetDescription(value);

                comboBox.Items.Add(item);
            }
        }

        /// <summary>
        /// Adds items to the channel ComboBox depending on the existing channel directories.
        /// Also update the channel in settings if the existing value isn't valid.
        /// </summary>
        private void SetupChannelsComboBox()
        {
            List<string> channelDirectoryPaths = LibraryFolder.ListAvailableChannelFolderPaths();

            this.ComboBox_Channel.Items.Add(CHANNEL_ALL);

            foreach (string channelDirectoryPath in channelDirectoryPaths) {
                this.ComboBox_Channel.Items.Add(System.IO.Path.GetFileName(channelDirectoryPath));
            }

            if (channelDirectoryPaths.Count < 1) {
                Properties.Settings.Default.RsiLauncherChannel = "";
                this.ComboBox_Channel.IsEnabled = false;
                this.Label_ChannelNotFound.Content = "Aucun canal trouvé";
            } else if (!this.ComboBox_Channel.Items.Contains(Properties.Settings.Default.RsiLauncherChannel)) {
                Properties.Settings.Default.RsiLauncherChannel = System.IO.Path.GetFileName(channelDirectoryPaths[0]);
            }

            Properties.Settings.Default.Save();
        }
    }
}
