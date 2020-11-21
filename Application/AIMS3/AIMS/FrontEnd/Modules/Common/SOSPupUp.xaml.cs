using AIMS3.BackEnd.Modules;
using AIMS3.FrontEnd.Modules.Interfaces;

namespace AIMS3.FrontEnd.Modules.Common
{
	public partial class SOSPopUp : SettingsPopUp
	{
        public override IZoneView View => view;

		public SOSPopUp(IFault fault)
		{
			InitializeComponent();

			SetFault(fault);
		}
	}
}