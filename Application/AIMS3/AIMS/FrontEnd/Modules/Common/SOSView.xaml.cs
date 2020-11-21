using System.Windows.Controls;
using AIMS3.FrontEnd.Modules.Interfaces;

namespace AIMS3.FrontEnd.Modules.Common
{
	public partial class SOSView : ZoneView
	{
		public override RelayAssign RelayAssign => relayAssign;
		public override CameraAssign CameraAssign => cameraAssign;
		public override TextBlock TextBlockName => textBlockName;

		public SOSView()
		{
			InitializeComponent();
		}
	}
}