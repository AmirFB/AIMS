using System;
using System.Windows;
using DevExpress.Xpf.Core;
using AIMS3.BackEnd.Modules;
using AIMS3.FrontEnd.Modules.Common;
using AIMS3.FrontEnd.Modules.Interfaces;
using System.Threading.Tasks;

namespace AIMS3.FrontEnd.Modules.Cam
{
	public partial class CamView : ModuleView
	{
		public override ModuleBasicView BasicView => basicView;
		public override ModuleButtonView ButtonView => buttonView;

		public Camera Cam => Module as Camera;

		private async void Home_Click(object sender, RoutedEventArgs e) => await Cam.GoToHome().ConfigureAwait(false);
		private async void Preset_Click(object sender, RoutedEventArgs e) => await Cam.GoToPreset(listBoxEditPresets.SelectedIndex).ConfigureAwait(false);

		public CamView(Camera cam)
		{
			InitializeComponent();
			Initialize(cam);
			Cam.Player = player;
			player.Cam = Cam;
		}

		public override async Task<bool> Save()
		{
			if (!await base.Save().ConfigureAwait(false))
				return false;

			BindingGroup.UpdateSources();
			return true;
		}

		public override void Load()
		{
			DataContext = null;
			DataContext = Module;
			base.Load();

			foreach (var binding in BindingGroup.BindingExpressions)
				binding.UpdateTarget();

			Cam.OnPropertyChanged(nameof(Camera.Presets));
			Cam.OnPropertyChanged(nameof(Camera.Profiles));
		}

		public override async void Apply_Click(object sender, RoutedEventArgs e)
        {
            Save_Click(sender, e);

            if (await Cam.InitializeImmediate().ConfigureAwait(false))
				await Application.Current.Dispatcher.BeginInvoke(new Action(() => DXMessageBox.Show(this, (string)TryFindResource("Connected"))));

            else
				await Application.Current.Dispatcher.BeginInvoke(new Action(() => DXMessageBox.Show(this, (string)TryFindResource("NotConnected"))));
        }
    }
}