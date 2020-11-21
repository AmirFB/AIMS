using System;
using System.Collections.Generic;
using System.Windows;
using DevExpress.Xpf.Dialogs;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Core;
using AIMS3.BackEnd.Site;
using AIMS3.BackEnd;
using AIMS3.FrontEnd.Interfaces;
using AIMS3.FrontEnd.Site;
using AIMS3.FrontEnd.Site.Map;

using static AIMS3.BackEnd.Common;
using AIMS3.BackEnd.Modules;
using AIMS3.FrontEnd.Basic;
using AIMS3.FrontEnd.Site.Users;

namespace AIMS3.FrontEnd.Main
{
	public partial class MainWindow : AIMSWindow
	{
		public List<ITelemetricSite> Sites { get; private set; }
		public override ITelemetricSite Site => Sites.Count > 0 ? Sites[tabControl.SelectedIndex] : null;
		public override Plant Plant => Site?.Plant;
		public bool LoggedOut { get; set; }

		public bool FreezeEnabled => Site?.Type == TelemetricSite.SiteType.Local;

		public bool? ShowStatistics { get => Plant.MapShowStatistics; set => Plant.MapShowStatistics = value == true; }
		public bool? ShowTemperature { get => Plant.MapShowTemperature; set => Plant.MapShowTemperature = value == true; }
		public bool? Freezed { get => Plant.IsPaused; set => Plant.IsPaused = value == true; }
		public bool? CanMove { get => Plant.Map.CanEdit; set => Plant.Map.CanEdit = value == true; }

		public MainWindow(List<ITelemetricSite> sites)
		{
			InitializeComponent();

			Sites = sites;
			DataContext = this;
		}

		public void AddSite(ITelemetricSite site)
		{
			var tabItem = new DXTabItem() { Content = site.View, Header = site.Name };
			tabControl.Items.Add(tabItem);
			tabItem.VerticalContentAlignment = VerticalAlignment.Stretch;
		}

		public void DeleteSite(ITelemetricSite site)
		{
			foreach (DXTabItem tab in tabControl.Items)
			{
				if (tab.Content == site.View)
				{
					tabControl.Items.Remove(tab);
					return;
				}
			}
		}

		private void Sites_Click(object sender, ItemClickEventArgs e) => (new SiteListWindow()).ShowDialog();
		private void NewLocal_Click(object sender, RoutedEventArgs e) => (new SiteSettingsWindow(new LocalSite() { IsNew = true })).ShowDialog();
		private void NewRemote_Click(object sender, RoutedEventArgs e) => (new SiteSettingsWindow(new RemoteSite() { IsNew = true })).ShowDialog();
		private void Users_Click(object sender, RoutedEventArgs e) => (new UsersWindow()).ShowDialog();
		private void ApplicationSettings_Click(object sender, ItemClickEventArgs e) => (new ApplicationSettingsWindow()).ShowDialog();

		private void Logout_Click(object sender, RoutedEventArgs e)
		{
			if (DXMessageBox.Show(this, GetResourceString("AskLogout"), GetResourceString("Logout"),
				MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
			{
				LoggedOut = true;
				Close();
			}
		}

		private void Exit_Click(object sender, RoutedEventArgs e)
		{
			if (DXMessageBox.Show(this, GetResourceString("AskExit"), GetResourceString("Exit"),
				MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
				Environment.Exit(0);
		}

		private void Settings_Click(object sender, ItemClickEventArgs e) => Site?.ShowSettings();
		private void Connection_Click(object sender, ItemClickEventArgs e) => Plant?.ShowConnection();
		private void Modules_Click(object sender, ItemClickEventArgs e) => Plant?.ShowModulesMenu();
		private void Report_Click(object sender, ItemClickEventArgs e) => Plant?.ShowReport();

		private void Upload_Click(object sender, ItemClickEventArgs e)
		{
			DXOpenFileDialog dialog = new DXOpenFileDialog
			{
				Filter = "Image Files|*.PNG;*.BMP;*.JPG;*.GIF|All files|*.*",
				OpenFileDialogMode = OpenFileDialogMode.Files,
				Multiselect = false
			};

			if (dialog.ShowDialog() == true)
				Site.UploadImage(dialog.FileName);
		}

		private void Place_Click(object sender, ItemClickEventArgs e)
		{
			if (Plant.PlaceWindow == null)
				Plant.PlaceWindow = new PlaceWindow(Plant);

			Plant.PlaceWindow.PopUp();
		}

		private void Statistics_Changed(object sender, ItemClickEventArgs e)
		{
			Plant.MapShowStatistics = (sender as BarCheckItem).IsChecked == true;
			Plant.SaveSettings();
		}

		private void Temperature_Changed(object sender, ItemClickEventArgs e)
		{
			Plant.MapShowTemperature = (sender as BarCheckItem).IsChecked == true;
			Plant.SaveSettings();
		}

		private void tabControl_SelectionChanged(object sender, TabControlSelectionChangedEventArgs e)
		{
			OnPropertyChanged(nameof(ShowStatistics));
			OnPropertyChanged(nameof(ShowTemperature));
			OnPropertyChanged(nameof(Freezed));
		}

		private async void AcknowledgeFaults_Click(object sender, ItemClickEventArgs e) => TelemetricSite.ShowMessageBoxAuthentication(await Site.AuthenticateAcknowledgeAllFaults().ConfigureAwait(false));
		private async void ResetFaults_Click(object sender, ItemClickEventArgs e) => TelemetricSite.ShowMessageBoxAuthentication(await Site.AuthenticateResetAllFaults().ConfigureAwait(false));
		
		private async void InitializeAll_Click(object sender, ItemClickEventArgs e) => TelemetricSite.ShowMessageBoxAuthentication(await Site.AuthenticateInitializeAllModules().ConfigureAwait(false));
		private async void InitializeCams_Click(object sender, ItemClickEventArgs e) => TelemetricSite.ShowMessageBoxAuthentication(await Site.AuthenticateInitializeAllCams().ConfigureAwait(false));
		
		private void Freeze_Click(object sender, ItemClickEventArgs e)
		{
			if (Plant.IsPaused)
				SerialConnection.Disconnect();
		}

		private void Mute_Click(object sender, ItemClickEventArgs e) => Sound.Stop();

		private async void SetAlarms_Click(object sender, ItemClickEventArgs e) => TelemetricSite.ShowMessageBoxAuthentication(await Site.AuthenticateSetAlarms().ConfigureAwait(false));
		private async void ResetAlarms_Click(object sender, ItemClickEventArgs e) => TelemetricSite.ShowMessageBoxAuthentication(await Site.AuthenticateResetAlarms().ConfigureAwait(false));
	}
}