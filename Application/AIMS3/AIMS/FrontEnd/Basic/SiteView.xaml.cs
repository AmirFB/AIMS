using System;
using System.Linq;
using System.Windows;
using DevExpress.Xpf.Dialogs;
using AIMS3.BackEnd.Modules;
using AIMS3.BackEnd.Site;
using AIMS3.FrontEnd.Modules;
using AIMS3.FrontEnd.Site;
using AIMS3.FrontEnd.Site.Map;
using AIMS3.FrontEnd.Site.Users;
using AIMS3.FrontEnd.Interfaces;

namespace AIMS3.FrontEnd.Basic
{
	public partial class SiteView : AIMSUserControl
	{
		public MapView Map => map;

		ITelemetricSite site;
		public override ITelemetricSite Site
		{
			get => site;
			set
			{
				site = value;
				base.Site = site;
				map.Site = site;
				map.UpdateImage();
				faultList.Plant = Plant;
				Plant.FaultList = faultList;
			}
		}

		public void RefreshFaultList() => faultList.Refresh();
		public void EnableMapEdit() => map.CanEdit = true;
		public void DisableMapEdit() => map.CanEdit = false;

		public SiteView(ITelemetricSite site)
		{
			InitializeComponent();

			Site = site;
			DataContext = this;
		}

		public void PopulateIcons()
		{
			foreach (IFault fault in Plant.Faults)
				Map.Add(fault.Icon);

			foreach (Relay relay in Plant.Relays)
				Map.Add(relay.Icon);
		}

		public void UploadImage(string path) => map.UploadImage(path);
	}
}