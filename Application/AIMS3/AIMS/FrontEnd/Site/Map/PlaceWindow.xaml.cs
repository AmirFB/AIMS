using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using DevExpress.Xpf.Core;
using AIMS3.BackEnd.Modules;
using AIMS3.BackEnd.Site;
using AIMS3.FrontEnd.Interfaces;

using static AIMS3.BackEnd.Site.TelemetricSite;

namespace AIMS3.FrontEnd.Site.Map
{
	public partial class PlaceWindow : AIMSWindow
	{
		public List<IFault> Faults => Plant.Faults;
		public List<Relay> Relays => Plant.Relays;
		public List<IModule> Cameras => Plant.Cam;
		private bool canEdit;

		public PlaceWindow()
		{
			InitializeComponent();

			DataContext = this;
		}

		public PlaceWindow(Plant plant)
		{
			InitializeComponent();

			Plant = plant;
			DataContext = this;
		}

		public override void PopUp()
		{
			base.PopUp();
			canEdit = Plant.Map.CanEdit;
			Plant.Map.CanEdit = true;
		}

		public override void Window_Closing(object sender, CancelEventArgs e)
		{
			base.Window_Closing(sender, e);
			Plant.Map.CanEdit = canEdit;
		}

		private void Faults_Click(object sender, RoutedEventArgs e)
		{
			if (comboBoxEditFault.SelectedItem == null)
				return;

			Plant.Map.Place((comboBoxEditFault.SelectedItem as IIcon).Icon);
		}

		private void Relays_Click(object sender, RoutedEventArgs e)
		{
			if (comboBoxEditRelay.SelectedItem == null)
				return;

			Plant.Map.Place((comboBoxEditRelay.SelectedItem as IIcon).Icon);
		}

		private void Camera_Click(object sender, RoutedEventArgs e)
		{
			if (comboBoxEditCamera.SelectedItem == null)
				return;

			Plant.Map.Place((comboBoxEditCamera.SelectedItem as IIcon).Icon);
		}

		private async void ClearAll_Click(object sender, RoutedEventArgs e)
		{
			await System.Threading.Tasks.Task.Run(async () =>
			{
				if (DXMessageBox.Show((string)TryFindResource("AskClearAllIcons"), (string)TryFindResource("ClearAll"), MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.No)
					return;

				var result = await Site.AuthenticateRemoveAllIcons().ConfigureAwait(false);

				if (result == true)
				{
					Plant.Map.RemoveAll();
					Plant.SaveModules(false);
				}

				ShowMessageBoxAuthentication(result);

			}).ConfigureAwait(false);
		}
	}
}