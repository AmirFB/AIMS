using System.Windows;
using AIMS3.BackEnd.Modules;
using AIMS3.FrontEnd.Interfaces;

using static AIMS3.BackEnd.Site.TelemetricSite;

namespace AIMS3.FrontEnd.Modules.Interfaces
{
	public interface ISettingsPopUp : IAIMSWindow
	{
		IFault Fault { get; }
		IZoneView View { get; }

		void Save_Click(object sender, RoutedEventArgs e);
		void Default_Click(object sender, RoutedEventArgs e);
		void Test_Click(object sender, RoutedEventArgs e);
	}

	public abstract class SettingsPopUp : AIMSWindow, ISettingsPopUp
	{
		public IFault Fault { get; private set; }
		public abstract IZoneView View { get; }

		public override void Load() => View.Load();

		public void SetFault(IFault fault)
		{
			Fault = fault;
			Plant = Fault.Plant;
			View.SetFault(Fault);
		}

		private async void Test()
		{
			var result = await Site.AuthenticateRaiseFault(Fault).ConfigureAwait(false);
			ShowMessageBoxAuthentication(result);
		}

		public void Default_Click(object sender, RoutedEventArgs e) => View.Load();
		public void Test_Click(object sender, RoutedEventArgs e) => Test();

		public void Save_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				IsEnabled = false;
				View.Save();
			}
			finally { IsEnabled = true; }
		}
	}
}