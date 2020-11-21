using DevExpress.Xpf.Editors;
using AIMS3.BackEnd.Modules;
using AIMS3.FrontEnd.Modules.Common;
using AIMS3.FrontEnd.Modules.Interfaces;

namespace AIMS3.FrontEnd.Modules.EF
{
	public partial class EFView : ModuleView
	{
		public override ModuleBasicView BasicView => basicView;
		public override RelaysView RelaysView => relaysView;
		public override ModuleButtonView ButtonView => buttonView;

		public override IZoneView Zone1 => zone1;
		public override IZoneView Zone2 => zone2;
		public override IZoneView SOS => sos;

		public EFView(ElectroFence ef)
		{
			InitializeComponent();
			Initialize(ef);

			zone1.spinEditPower.EditValueChanged += Zone1_Changed;
			zone1.spinEditThreshold.EditValueChanged += Zone1_Changed;
			zone1.spinEditRepeat.EditValueChanged += Zone1_Changed;

			zone2.spinEditPower.EditValueChanged += Zone2_Changed;
			zone2.spinEditThreshold.EditValueChanged += Zone2_Changed;
			zone2.spinEditRepeat.EditValueChanged += Zone2_Changed;
		}

        private void Zone1To2()
        {
			zone2.spinEditPower.EditValue = zone1.spinEditPower.EditValue;
			zone2.spinEditThreshold.EditValue = zone1.spinEditThreshold.EditValue;
			zone2.spinEditRepeat.EditValue = zone1.spinEditRepeat.EditValue;
		}

		private void Zone2To1()
		{
			zone1.spinEditPower.EditValue = zone2.spinEditPower.EditValue;
			zone1.spinEditThreshold.EditValue = zone2.spinEditThreshold.EditValue;
			zone1.spinEditRepeat.EditValue = zone2.spinEditRepeat.EditValue;
		}

		private void Zone1_Changed(object sender, EditValueChangedEventArgs e) => Zone1To2();
		private void Zone2_Changed(object sender, EditValueChangedEventArgs e) => Zone2To1();
	}
}