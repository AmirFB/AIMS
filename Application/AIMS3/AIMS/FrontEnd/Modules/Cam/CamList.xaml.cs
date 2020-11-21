using System.Collections.Generic;
using System.Windows.Controls;
using AIMS3.BackEnd.Modules;
using AIMS3.FrontEnd.Modules.Interfaces;

using static AIMS3.BackEnd.Modules.Module;

namespace AIMS3.FrontEnd.Modules.Cam
{
	public partial class CamList : ModuleList2
	{
		public override ItemsControl ItemsControl => itemsControl;
		public override ScrollViewer ScrollViewer => scrollViewer;

		public override List<IModule> Modules
		{
			get
			{
				List<IModule> modules = Plant.Cam.GetRange(0, Plant.Cam.Count);

				if (NewModule != null && !Plant.Cam.Contains(NewModule) && NewModule.State != ModuleState.Deleted)
					modules.Add(NewModule);

				return modules;
			}
		}

		public CamList()
		{
			InitializeComponent();

			DataContext = this;
		}

		public override bool InitializeNew()
		{
			if (Modules.Count != Plant.Cam.Count)
				return false;

			NewModule = new Camera(Plant);
			return true;
		}
	}
}