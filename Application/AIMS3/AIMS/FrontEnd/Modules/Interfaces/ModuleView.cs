using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using AIMS3.BackEnd.Modules;
using AIMS3.BackEnd.Site;
using AIMS3.FrontEnd.Interfaces;
using AIMS3.FrontEnd.Modules.Common;

using static AIMS3.BackEnd.Common;
using static AIMS3.BackEnd.Modules.Module;
using static AIMS3.BackEnd.Site.TelemetricSite;

namespace AIMS3.FrontEnd.Modules.Interfaces
{
	public interface IModuleView
	{
		Plant Plant { get; }
		IModule Module { get; }
		List<IModule> Collection { get; }

		IZoneView Zone1 { get; }
		IZoneView Zone2 { get; }
		IZoneView SOS { get; }
		RelaysView RelaysView { get; }
		ModuleList List { get; set; }
		ModuleBasicView BasicView { get; }
		SpinEdit SpinEditIndex { get; }

		Task<bool> Save();
		void Load();
		void Check();

		void Apply_Click(object sender, RoutedEventArgs e);
		void Save_Click(object sender, RoutedEventArgs e);
		void Check_Click(object sender, RoutedEventArgs e);
		void Default_Click(object sender, RoutedEventArgs e);
		void Delete_Click(object sender, RoutedEventArgs e);
		void Index_Changed(object sender, EditValueChangedEventArgs e);
	}

	public abstract class ModuleView : AIMSUserControl, IModuleView
	{
		public IModule Module { get; private set; }
		public List<IModule> Collection => Module.Collection;

		public virtual IZoneView Zone1 => null;
		public virtual IZoneView Zone2 => null;
		public virtual IZoneView SOS => null;
		public virtual RelaysView RelaysView => null;
		public ModuleList List { get; set; }
		public abstract ModuleBasicView BasicView { get; }
		public abstract ModuleButtonView ButtonView { get; }
		public SpinEdit SpinEditIndex => BasicView.SpinEditIndex;

		public void Initialize(IModule module)
		{
			SetModule(module);
			BasicView.SpinEditIndex.EditValueChanged += Index_Changed;
			DataContext = Module;
			Load();
		}

		public void SetModule(IModule module)
		{
			Module = module;
			Plant = Module.Plant;
			BasicView.SetModule(Module);
			Zone1?.SetFault(Module.Zone1);
			Zone2?.SetFault(Module.Zone2);
			SOS?.SetFault(Module.SOS);
			RelaysView?.SetModule(Module);

			ButtonView.Site = Site;
			ButtonView.simpleButtonCheck.Click += Check_Click;
			ButtonView.simpleButtonApply.Click += Apply_Click;
			ButtonView.simpleButtonSave.Click += Save_Click;
			ButtonView.simpleButtonDefault.Click += Default_Click;
			ButtonView.simpleButtonDelete.Click += Delete_Click;
		}

		public virtual async Task<bool> Save()
		{
			bool old = Collection.Contains(Module);
			bool? result;
			IsEnabled = false;

			try
			{
				if (!BasicView.Check())
					return false;

				var temp = Module.ToString();

				BasicView.Save();
				Zone1?.Save();
				Zone2?.Save();
				SOS?.Save();
				RelaysView?.Save();
				Module.Initialized = false;

				if (old)
					result = await Site.AuthenticateModifyModule(Module).ConfigureAwait(false);

				else
				{
					Collection.Add(Module);
					result = await Site.AuthenticateAddModule(Module).ConfigureAwait(false);
					Module.Zones.ForEach(zone => Site.Map.SetLocationDefault(zone.Icon));
				}

				if (result == true)
				{
					Plant.SaveModules(true);

					if (Site.Type == SiteType.Local)
						return true;
				}

				else
				{
					if (!old)
						Collection.Remove(Module);

					Load();
					Module.Upload(temp);
				}

				ShowMessageBoxAuthentication(result);
			}
			finally
			{
				IsEnabled = true;
				List.Refresh();
			}
			return true;
		}

		public virtual void Load()
		{
			BasicView.Load();
			Zone1?.Load();
			Zone2?.Load();
			SOS?.Load();
			RelaysView?.Load();
		}

		public virtual async void Check()
		{
			var result = await Site.AuthenticateCheckModule(Module).ConfigureAwait(false);

			if (result == true)
				DXMessageBox.Show(GetResourceString("Initialized"), GetResourceString("Result"));

			else if (result == false)
				DXMessageBox.Show(GetResourceString("NotInitialized"), GetResourceString("Result"));

			else
				ShowMessageBoxAuthentication(result);
		}

		public async void Delete()
		{
			if (!Collection.Contains(Module))
			{
				Module.State = ModuleState.Deleted;
				List.Refresh();
			}

			else if (DXMessageBox.Show(this, GetResourceString("AskIfDeleteModule"), GetResourceString("Delete"),
				MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
			{
				bool? result = await Site.AuthenticateDeleteModule(Module).ConfigureAwait(false);

				if (result == true)
				{
					Plant.DeleteModule(Module, true);
					List.Refresh();

					IModule module = null;

					if (Collection.Count > 0)
					{
						module = Collection.FindLast(mod => mod.Index < Module.Index);

						if (module == null)
							module = Collection[0];
					}

					else if (Plant.Modules.Count > 0)
						module = Plant.Modules[0];

					List.Select(module);

					if (Site.Type == SiteType.Local)
						return;
				}

				ShowMessageBoxAuthentication(result);
				List.Refresh();
			}

			else
				return;

		}

		public virtual void Apply_Click(object sender, RoutedEventArgs e)
		{
			Save();
			Check();
		}

		public void Check_Click(object sender, RoutedEventArgs e) => Check();
		public void Default_Click(object sender, RoutedEventArgs e) => Load();
		public async void Save_Click(object sender, RoutedEventArgs e) => await Save().ConfigureAwait(false);

		public void Delete_Click(object sender, RoutedEventArgs e) => Delete();

		public virtual void Index_Changed(object sender, EditValueChangedEventArgs e)
		{
			Zone1?.UpdateLabel((int)SpinEditIndex.EditValue);
			Zone2?.UpdateLabel((int)SpinEditIndex.EditValue);
		}
	}
}