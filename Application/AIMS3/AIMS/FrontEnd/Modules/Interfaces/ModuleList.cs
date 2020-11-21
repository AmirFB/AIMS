using System;
using System.Collections.Generic;
using System.Windows.Controls;
using AIMS3.BackEnd.Modules;
using AIMS3.BackEnd.Site;
using AIMS3.FrontEnd.Interfaces;

using static AIMS3.BackEnd.Modules.Module;

namespace AIMS3.FrontEnd.Modules.Interfaces
{
	public interface IModuleList2
	{
		Plant Plant { get; }
		List<IModule> Modules { get; }
		List<IModuleView> Views { get; }
		ItemsControl ItemsControl { get; }
		ScrollViewer ScrollViewer { get; }

		bool InitializeNew();

		void Refresh();
		void Refresh_Click(object sender, EventArgs e);
		void Save_Click(object sender, EventArgs e);
		void Delete_Click(object sender, EventArgs e);
		void New_Click(object sender, EventArgs e);
	}

	public abstract class ModuleList2 : AIMSUserControl, IModuleList2
	{
		public IModule NewModule { get; set; }
		public abstract List<IModule> Modules { get; }
		public List<IModuleView> Views => Modules.ConvertAll(module => module.View);
		public abstract ItemsControl ItemsControl { get; }
		public abstract ScrollViewer ScrollViewer { get; }

		public void Refresh()
		{
			for (int i = 0; i < ItemsControl.Items.Count; i++)
				if (!Modules.Contains((ItemsControl.Items[i] as IModuleView).Module))
					ItemsControl.Items.RemoveAt(i--);

			Modules.ForEach(module => Insert(module));

			if (NewModule?.State == ModuleState.Deleted)
				ItemsControl.Items.Remove(NewModule.View);
		}

		public void Refresh_Click(object sender, EventArgs e)
		{
			Views.ForEach(view => view.Load());
			Refresh();
		}

		public void Insert(IModule module)
		{
			if (module.State == ModuleState.Deleted)
				ItemsControl.Items.Remove(module.View);

			for (int i = 0; i < ItemsControl.Items.Count; i++)
			{
				var index = (ItemsControl.Items[i] as IModuleView).Module.Index;

				if (index < module.Index)
					continue;

				if (index == module.Index)
					return;

				if (ItemsControl.Items.Contains(module.View))
					ItemsControl.Items.Remove(module.View);

				ItemsControl.Items.Insert(i, module.View);
				return;
			}

			ItemsControl.Items.Add(module.View);
		}

		public void Delete_Click(object sender, EventArgs e) => Views.ForEach(view => view.Delete_Click(sender, null));

		public abstract bool InitializeNew();

		public void New_Click(object sender, EventArgs e)
		{
			if (NewModule != null && !NewModule.Collection.Contains(NewModule) && ItemsControl.Items.Contains(NewModule?.View))
				ItemsControl.Items.Remove(NewModule?.View);

			if (!InitializeNew())
				return;

			NewModule.GetNewIndex();
			NewModule.GetNewAddress();
			NewModule.GenerateHash();
			NewModule.InitializeView();
			NewModule.State = ModuleState.New;
			NewModule.Zones.ForEach(zone => zone.Icon.IsPlaced = true);

			if (!ItemsControl.Items.Contains(NewModule.View))
				ItemsControl.Items.Add(NewModule.View);

			ScrollViewer.ScrollToRightEnd();
		}

		public void Save_Click(object sender, EventArgs e)
		{
			Views.ForEach(view => view.Save_Click(sender, null));
			Refresh();
		}
	}
}