using System.Windows.Controls;
using AIMS3.BackEnd.Modules;
using AIMS3.FrontEnd.Modules.Common;
using AIMS3.FrontEnd.Modules.Interfaces;

namespace AIMS3.FrontEnd.Modules.FG
{
	public partial class FGZoneView : ZoneView
	{
		FGFault FGFault => Fault as FGFault;

		public override RelayAssign RelayAssign => relayAssign;
		public override CameraAssign CameraAssign => cameraAssign;
		public override TextBlock TextBlockName => textBlockName;

		public double PreTime { get => FGFault.PreTime / 10.0; set => FGFault.PreTime = (int)(value * 10); }

		public FGZoneView()
		{
			InitializeComponent();
		}
	}
}