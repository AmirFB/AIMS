using System.Windows.Data;
using DevExpress.Xpf.Core;
using AIMS3.BackEnd.Modules;
using AIMS3.FrontEnd.Modules.Interfaces;

namespace AIMS3.FrontEnd.Modules.ACU
{
	public partial class ACUFaultStatus : FaultStatus
	{
		public override BindingGroup Group => BindingGroup;

		public override SimpleButton SimpleButtonReset => simpleButtonReset;
		public override SimpleButton SimpleButtonAcknowledge => simpleButtonAcknowledge;

		public ACUFaultStatus(IFault fault)
		{
			InitializeComponent();

			SetFault(fault);
			DataContext = this;
			Timer.Elapsed += Timer_Elapsed;
			Timer.Start();

			//Evaluate();
		}
	}
}