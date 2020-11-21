using AIMS3.BackEnd.Modules;
using AIMS3.FrontEnd.Modules.Common;
using AIMS3.FrontEnd.Modules.Interfaces;

namespace AIMS3.FrontEnd.Modules.ACU
{
	public partial class ACUView : ModuleView
    {
		public override ModuleBasicView BasicView => basicView;
		public override RelaysView RelaysView => relaysView;
		public override ModuleButtonView ButtonView => buttonView;

		public override IZoneView SOS => sos;

		public ACUView(AlarmControlUnit acu)
		{
			InitializeComponent();
			Initialize(acu);
		}
	}
}