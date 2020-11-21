using System;
using System.Collections.Generic;
using System.Windows.Controls;
using AIMS3.BackEnd.Modules;
using AIMS3.FrontEnd.Modules.Interfaces;

using static AIMS3.BackEnd.Modules.Module;

namespace AIMS3.FrontEnd.Modules.ACU
{
	public partial class ACUList : ModuleList2
	{
		public override ItemsControl ItemsControl => itemsControl;
		public override ScrollViewer ScrollViewer => scrollViewer;

		public override List<IModule> Modules
		{
			get
			{
				List<IModule> modules = Plant.ACU.GetRange(0, Plant.ACU.Count);

				if (NewModule != null && !Plant.ACU.Contains(NewModule) && NewModule.State != ModuleState.Deleted)
					modules.Add(NewModule);

				return modules;
			}
		}

		public ACUList()
		{
			InitializeComponent();
			DataContext = this;
		}

		public override bool InitializeNew()
		{
			if (Modules.Count != Plant.ACU.Count)
				return false;

			NewModule = new AlarmControlUnit(Plant);
			return true;
		}
	}
}