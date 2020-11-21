using System.Windows;
using DevExpress.Xpf.Core;
using AIMS3.FrontEnd.Interfaces;
using AIMS3.BackEnd.Site;

using static AIMS3.BackEnd.Common;
using static AIMS3.BackEnd.Site.TelemetricSite;

namespace AIMS3.FrontEnd.Site
{
	public partial class SiteSettingsWindow : AIMSWindow
	{
		public string TitleText => string.Format("{0}: {1}", FindResource("Settings"), Site.Name);

		public int TypeIndex { get => Site.Type == SiteType.Local ? 0 : 1; set => Site.Type = value == 0 ? SiteType.Local : SiteType.Remote; }
		public int AccessIndex { get => Site.Access == SiteAccess.Internal ? 0 : 1; set => Site.Access = value == 0 ? SiteAccess.Internal : SiteAccess.Internet; }
		public Visibility RemoteVisibility => Site.IsRemote ? Visibility.Visible : Visibility.Collapsed;

		private void ShowMessageBox(string key) => DXMessageBox.Show((string)TryFindResource(key), (string)TryFindResource("SaveFailure"), MessageBoxButton.OK, MessageBoxImage.Error);

		public SiteSettingsWindow(ITelemetricSite site)
		{
			InitializeComponent();

			Site = site;
			DataContext = Site;
		}

		public override void Load()
		{
			foreach (var binding in BindingGroup.BindingExpressions)
				binding.UpdateTarget();
		}

		public bool Save()
		{
			if (Sites.Find(site => site.Name == textEditName.Text && site != Site) != null)
			{
				ShowMessageBox("NameExists");
				return false;
			}

			if (Site.Type == SiteType.Remote && Sites.Find(site => site.HostAddress + site.Port == textEditIP.Text + textEditPort.Text && site != Site && site is RemoteConnection) != null)
			{
				ShowMessageBox("IPPortExists");
				return false;
			}

			if (Site.Type == SiteType.Local && Sites.Find(site =>site.Port.ToString() == textEditPort.Text && site != Site && site is LocalSite) != null)
			{
				ShowMessageBox("PortExists");
				return false;
			}

			var temp = Site.Name;
			BindingGroup.UpdateSources();
			return Site.SaveSettings(temp);
		}

		private void OK_Click(object sender, RoutedEventArgs e)
		{
			if (Save())
				Close();
		}

		private void Apply_Click(object sender, RoutedEventArgs e) => Save();
		private void Default_Click(object sender, RoutedEventArgs e) => Load();
		private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
	}
}