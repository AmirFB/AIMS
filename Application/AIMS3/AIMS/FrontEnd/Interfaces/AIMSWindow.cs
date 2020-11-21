using System.ComponentModel;
using System.Windows.Data;
using System.Windows;
using DevExpress.Xpf.Core;
using AIMS3.BackEnd.Site;
using System.Runtime.CompilerServices;

namespace AIMS3.FrontEnd.Interfaces
{
	public interface IAIMSWindow : INotifyPropertyChanged
	{
		new event PropertyChangedEventHandler PropertyChanged;
		ITelemetricSite Site { get; }
		Plant Plant { get; }

		void OnPropertyChanged(string name);
		void PopUp();
		void Load();
		void Window_Closing(object sender, CancelEventArgs e);
	}

	public abstract class AIMSWindow : ThemedWindow
	{
		private ITelemetricSite site;
		public virtual ITelemetricSite Site
		{
			get => site;
			set
			{
				site = value;
				plant = site.Plant;
			}
		}

		private Plant plant;
		public virtual Plant Plant
		{
			get => plant;
			set
			{
				plant = value;
				site = plant.Owner;
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		private delegate void UpdateSourcesCallBack(BindingGroup group);
		private delegate void UpdateTargetsCallBack(BindingGroup group);

		public void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

		public virtual void Load() { }

		public virtual void PopUp()
		{
			if (!IsVisible)
			{
				Show();
				Load();
			}
		}

		public virtual void Window_Closing(object sender, CancelEventArgs e)
		{
			e.Cancel = true;
			Visibility = Visibility.Collapsed;
		}
	}
}