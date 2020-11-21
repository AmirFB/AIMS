using System.Collections.Generic;
using System.Windows.Controls;
using AIMS3.BackEnd.Modules;
using AIMS3.FrontEnd.Modules.Interfaces;
using static AIMS3.BackEnd.Modules.Module;

namespace AIMS3.FrontEnd.Modules.EF
{
	public partial class EFList : ModuleList2
	{
		public override ItemsControl ItemsControl => itemsControl;
		public override ScrollViewer ScrollViewer => scrollViewer;

		public override List<IModule> Modules
		{
			get
			{
				List<IModule> modules = Plant.EF.GetRange(0, Plant.EF.Count);

				if (NewModule != null && !Plant.EF.Contains(NewModule) && NewModule.State != ModuleState.Deleted)
					modules.Add(NewModule);

				return modules;
			}
		}

		public EFList()
		{
			InitializeComponent();
			DataContext = this;
		}

		public override bool InitializeNew()
		{
			if (Modules.Count != Plant.EF.Count)
				return true;

			NewModule = new ElectroFence(Plant) { ZoneType = ElectroFence.ModuleZonesType.TwoZonesCommon };
			return true;
		}
	}
}