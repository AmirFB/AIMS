using System.Windows;
using System.Windows.Data;
using DevExpress.Xpf.Core;
using AIMS3.BackEnd.Site;
using AIMS3.FrontEnd.Interfaces;

using static AIMS3.BackEnd.Site.TelemetricSite;

namespace AIMS3.FrontEnd.Site
{
	public partial class ConnectionWindow : AIMSWindow
	{
		public ConnectionWindow(Plant plant)
		{
			InitializeComponent();

			Plant = plant;
			DataContext = Plant;
		}

		public override void Load()
		{
			foreach (var binding in BindingGroup.BindingExpressions)
				binding.UpdateTarget();
		}

		public async void Save()
		{
			string temp = Plant.GetStringSettings();

			BindingGroup.UpdateSources();
			string newData = Plant.GetStringSettings();
			bool? result = await Plant.Owner.AuthenticateSettings(newData).ConfigureAwait(false);

			if (Plant.Type == SiteType.Local)
			{
				Plant.SaveSettings();
				return;
			}

			if (result == true)
				Plant.SaveSettings();

			else
				Plant.UploadSettings(temp);

			ShowMessageBoxAuthentication(result);
		}

		private void OK_Click(object sender, RoutedEventArgs e)
		{
			Save();
			Close();
		}

		private void Apply_Click(object sender, RoutedEventArgs e) => Save();
		private void Default_Click(object sender, RoutedEventArgs e) => Load();
		private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
	}
}