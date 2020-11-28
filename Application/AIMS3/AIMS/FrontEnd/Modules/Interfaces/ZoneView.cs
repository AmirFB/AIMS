using System;
using System.Windows.Controls;
using AIMS3.BackEnd.Modules;
using AIMS3.FrontEnd.Interfaces;
using AIMS3.FrontEnd.Modules.Common;

using static AIMS3.BackEnd.Common;
using static AIMS3.BackEnd.Site.TelemetricSite;

namespace AIMS3.FrontEnd.Modules.Interfaces
{
	public interface IZoneView
	{
		IFault Fault { get; }
		RelayAssign RelayAssign { get; }
		CameraAssign CameraAssign { get; }
		TextBlock TextBlockName { get; }

		void Save();
		void Load();
		void UpdateLabel(int address);
		void SetFault(IFault fault);
	}

	public abstract class ZoneView : AIMSUserControl, IZoneView
	{
		public IFault Fault { get; private set; }
		public abstract RelayAssign RelayAssign { get; }
		public abstract CameraAssign CameraAssign { get; }
		public abstract TextBlock TextBlockName { get; }

		public virtual void UpdateLabel(int address) => TextBlockName.Text = Fault.IsZone ? ("Zone" + (address * 2 + Fault.Index - 1)) : "SOS";

		public void SetFault(IFault fault)
		{
			Fault = fault;
			Plant = Fault.Plant;
			RelayAssign.Fault = Fault;
			CameraAssign.Fault = Fault;
			Load();
		}

		public async void Save()
		{
			try
			{
				string temp = Fault.GetString();

				BindingGroup.UpdateSources();
				RelayAssign.Save();
				CameraAssign.Save();


				if (Fault.OwnerType == Module.ModuleType.EF && Fault.Module.ZoneType == ElectroFence.ModuleZonesType.TwoZonesCommon)
					Fault.Module.CopyZones(Fault.Index);

				bool? result;

				Fault.Module.Initialized = false;

				result = await Site.AuthenticateModifyModule(Fault.Module).ConfigureAwait(false);

				if (result == true)
				{
					Plant.SaveModules(true);

					if (Site.Type == SiteType.Local)
						return;
				}

				else
				{
					Load();
					Fault.Module.Upload(temp);
				}

				ShowMessageBoxAuthentication(result);
			}
			catch (Exception ex) { WriteToDebug(typeof(ZoneView), Fault.Name, nameof(Save), ex); }
		}

		public void Load()
		{
			DataContext = Fault;

			foreach (var binding in BindingGroup.BindingExpressions)
				binding.UpdateTarget();

			UpdateLabel(Fault.Module.Index);
			RelayAssign.Load();
			CameraAssign.Load();
		}
	}
}