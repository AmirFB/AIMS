using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using onvif.services;
using DevExpress.Xpf.Grid;
using AIMS3.BackEnd.Modules;
using AIMS3.BackEnd.Site;

namespace AIMS3.FrontEnd.Modules.Common
{
	public partial class CameraAssign : UserControl
	{
		public IFault Fault { get; set; }
		public Plant Plant => Fault.Plant;

		public ObservableCollection<Nvt> Cameras { get; } = new ObservableCollection<Nvt>();

		public void EnableSettings() => columnSelected.IsEnabled = true;
		public void DisableSettings() => columnSelected.IsEnabled = false;

		public CameraAssign()
		{
			InitializeComponent();

			DataContext = this;
		}

		public class Nvt : INotifyPropertyChanged
		{
			public event PropertyChangedEventHandler PropertyChanged;
			public void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

			public Camera Camera { get; set; }
			public ObservableCollection<PTZPreset> Presets => new ObservableCollection<PTZPreset>(Camera.Presets);
			public PTZPreset SelectedPreset { get; set; }
			private bool selected;
			public string Name => Camera.Name;
			public int Preset => Presets.IndexOf(SelectedPreset);

			public bool Selected
			{
				get => selected;
				set
				{
					selected = value;
					OnPropertyChanged(nameof(Selected));
				}
			}
		}

		public void Save()
		{
			Fault.Cams.Clear();

			foreach (Nvt cam in Cameras)
				if (cam.Selected)
					Fault.Cams.Add(new Surveliance() { Camera = cam.Camera, Preset = cam.Preset });
		}

		public void Load()
		{
			Cameras.Clear();

			foreach (Camera cam in Plant.Cam)
				if (Fault.Cams.FindIndex(nvt => nvt.Camera == cam) >= 0)
					Cameras.Add(new Nvt() { Camera = cam });

			foreach (Camera cam in Plant.Cam)
				if (Fault.Cams.FindIndex(nvt => nvt.Camera == cam) < 0)
					Cameras.Add(new Nvt() { Camera = cam });

			foreach (Nvt cam in Cameras)
			{
				try
				{
					cam.Selected = Fault.Cams.FindIndex(nvt => nvt.Camera == cam.Camera) >= 0;
					cam.SelectedPreset = cam.Selected && cam.Presets.Count > 0 ? cam.Presets[Fault.Cams.Find(nvt => nvt.Camera == cam.Camera).Preset] : null;

					cam.OnPropertyChanged(nameof(Nvt.Presets));
					cam.OnPropertyChanged(nameof(Nvt.SelectedPreset));
				}
				catch (Exception ex) { }
			}

			gridControl.GetBindingExpression(GridControl.ItemsSourceProperty).UpdateTarget();
			gridControl.RefreshData();
		}
	}
}