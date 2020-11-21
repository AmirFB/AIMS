using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using AIMS3.BackEnd.Site;

namespace AIMS3.FrontEnd.Interfaces
{
	public class AIMSUserControl : UserControl
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

		public virtual event PropertyChangedEventHandler PropertyChanged;
		public void OnPropertyChanged([CallerMemberName]string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}