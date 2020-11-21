using AIMS3.BackEnd.Modules;
using AIMS3.FrontEnd.Modules.Interfaces;

namespace AIMS3.FrontEnd.Modules.FG
{
	public partial class FGPopUp : SettingsPopUp
	{
        public override IZoneView View => view;

		public FGPopUp(IFault fault)
		{
			InitializeComponent();

			SetFault(fault);
		}
	}
}