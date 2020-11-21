using AIMS3.BackEnd.Modules;
using AIMS3.FrontEnd.Modules.Interfaces;

namespace AIMS3.FrontEnd.Modules.EF
{
	public partial class EFPopUp : SettingsPopUp
	{
        public override IZoneView View => view;

		public EFPopUp(IFault fault)
		{
			InitializeComponent();

			SetFault(fault);
		}
	}
}