using System.Collections.Generic;
using System.Windows.Controls;
using AIMS3.BackEnd.Modules;
using AIMS3.FrontEnd.Modules.Interfaces;

using static AIMS3.BackEnd.Modules.Module;

namespace AIMS3.FrontEnd.Modules.FG
{
	public partial class FGList : ModuleList2
	{
		public override ItemsControl ItemsControl => itemsControl;
		public override ScrollViewer ScrollViewer => scrollViewer;

		public override List<IModule> Modules
		{
			get
			{
				List<IModule> modules = Plant.FG.GetRange(0, Plant.FG.Count);

				if (NewModule != null && !Plant.FG.Contains(NewModule) && NewModule.State != ModuleState.Deleted)
					modules.Add(NewModule);

				return modules;
			}
		}

		public FGList()
		{
			InitializeComponent();
			DataContext = this;
		}

		public override bool InitializeNew()
		{
			if (Modules.Count != Plant.FG.Count)
				return false;

			NewModule = new FlexiGuard(Plant);
			return true;
		}
	}
}