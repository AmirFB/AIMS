using AIMS3.BackEnd.Modules;
using AIMS3.FrontEnd.Modules.Common;
using AIMS3.FrontEnd.Modules.Interfaces;

namespace AIMS3.FrontEnd.Modules.FG
{
	public partial class FGView : ModuleView
	{
		public override ModuleBasicView BasicView => basicView;
		public override RelaysView RelaysView => relaysView;
		public override ModuleButtonView ButtonView => buttonView;

		public override IZoneView Zone1 => zone1;
		public override IZoneView Zone2 => zone2;
		public override IZoneView SOS => sos;

		public FGView(FlexiGuard fg)
		{
			InitializeComponent();
			Initialize(fg);
		}
	}
}