using System.Windows.Media;
using System.Windows.Data;
using DevExpress.Xpf.Core;
using AIMS3.BackEnd.Modules;
using AIMS3.FrontEnd.Modules.Interfaces;

namespace AIMS3.FrontEnd.Modules.FG
{
	public partial class FGFaultStatus : FaultStatus
	{
		private FGFault FGFault => Fault as FGFault;
		public override BindingGroup Group => BindingGroup;

		public override SimpleButton SimpleButtonReset => simpleButtonReset;
		public override SimpleButton SimpleButtonAcknowledge => simpleButtonAcknowledge;

		public string CutRaised => GetHealthy(FGFault.CutRaised);
		public string ClimbRaised => GetHealthy(FGFault.ClimbRaised);
		public string CircuitRaised => GetHealthy(FGFault.CircuitRaised);

		public Brush CutColor => GetColor(FGFault.CutRaised);
		public Brush ClimbColor => GetColor(FGFault.ClimbRaised);
		public Brush CircuitColor => GetColor(FGFault.CircuitRaised);

		public FGFaultStatus(IFault fault)
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
					OnPropertyChanged(nameof(CutRaised));
					OnPropertyChanged(nameof(CutColor));
					OnPropertyChanged(nameof(ClimbRaised));
					OnPropertyChanged(nameof(ClimbColor));
					OnPropertyChanged(nameof(CircuitRaised));
					OnPropertyChanged(nameof(CircuitColor));
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