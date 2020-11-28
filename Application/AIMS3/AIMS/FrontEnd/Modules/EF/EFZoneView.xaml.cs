using System.Windows.Controls;
using AIMS3.FrontEnd.Modules.Common;
using AIMS3.FrontEnd.Modules.Interfaces;

namespace AIMS3.FrontEnd.Modules.EF
{
	public partial class EFZoneView : ZoneView
	{
		public override RelayAssign RelayAssign => relayAssign;
		public override CameraAssign CameraAssign => cameraAssign;
		public override TextBlock TextBlockName => textBlockName;

		public int MaxThreshold => (int)spinEditPower.EditValue * 100;

		private void Power_Changed(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e) => OnPropertyChanged(nameof(MaxThreshold));

		public EFZoneView()
		{
			InitializeComponent();
		}

		private void Enabled_Checked(object sender, System.Windows.RoutedEventArgs e)
		{
			if (checkEditHV.IsChecked == false && checkEditLV.IsChecked == false)
				checkEditHV.IsChecked = true;
		}

		private void HVLV_Unchecked(object sender, System.Windows.RoutedEventArgs e)
		{
			if (checkEditHV.IsChecked == false && checkEditLV.IsChecked == false)
				checkEditEnabled.IsChecked = false;
		}
	}
}