using System.Windows.Media;
using System.Windows.Data;
using DevExpress.Xpf.Core;
using AIMS3.BackEnd.Modules;
using AIMS3.FrontEnd.Modules.Interfaces;

namespace AIMS3.FrontEnd.Modules.EF
{
	public partial class EFFaultStatus : FaultStatus
	{
		private EFFault EFFault => Fault as EFFault;
		public override BindingGroup Group => BindingGroup;

		public override SimpleButton SimpleButtonReset => simpleButtonReset;
		public override SimpleButton SimpleButtonAcknowledge => simpleButtonAcknowledge;

		public string HVRaised => GetHealthy(EFFault.HVRaised);
        public string LVRaised => GetHealthy(EFFault.LVRaised);

        public Brush HVColor => GetColor(EFFault.HVRaised);
        public Brush LVColor => GetColor(EFFault.LVRaised);

        public EFFaultStatus(IFault fault)
		{
			InitializeComponent();

			SetFault(fault);
            DataContext = this;
			Timer.Elapsed += Timer_Elapsed;
			Timer.Start();

			Evaluate();
		}

		public override void Evaluate()
		{
			if (!IsVisible)
				return;

			new System.Threading.Thread(() =>
			{
				base.Evaluate();

				if (Fault.IsZone)
				{
					OnPropertyChanged(nameof(HVRaised));
					OnPropertyChanged(nameof(HVColor));
					OnPropertyChanged(nameof(HVRaised));
					OnPropertyChanged(nameof(LVColor));
				}

				else
				{
					OnPropertyChanged(nameof(TamperRaised));
					OnPropertyChanged(nameof(TamperColor));
				}
			}).Start();
		}
	}
}