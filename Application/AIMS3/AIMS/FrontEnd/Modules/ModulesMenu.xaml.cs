using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using AIMS3.BackEnd.Modules;
using AIMS3.BackEnd.Site;
using AIMS3.FrontEnd.Interfaces;
using AIMS3.FrontEnd.Modules.Cam;
using AIMS3.FrontEnd.Modules.Interfaces;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Core.Native;

using static AIMS3.BackEnd.Common;
using static AIMS3.BackEnd.Modules.Module;

namespace AIMS3.FrontEnd.Modules
{
	public partial class ModulesMenu : AIMSWindow
	{
		Plant plant;
		public override Plant Plant
		{
			get => plant;
			set
			{
				plant = value;
				base.Plant = plant;
				moduleList.Plant = plant;
			}
		}

		public ModulesMenu() => InitializeComponent();

		public void Refresh() => moduleList.Refresh();

		public override void Load() => Plant.Modules.ForEach(module => module.View.Load());

		public override void PopUp()
		{
			if (!IsVisible)
			{
				Load();
				ShowDialog();
			}
		}

		public override void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			foreach (Camera cam in Plant.Cam)
				(cam.View as CamView).player.Stop();

			e.Cancel = true;
			Visibility = Visibility.Collapsed;
		}
	}

	public class ModuleTree
	{
		private Plant plant;
		public string Name { get; set; }
		private ModuleType type;
		public ImageSource Image { get; }

		public List<IModule> Modules
		{
			get
			{
				switch (type)
				{
					case ModuleType.EF:
						return EF;
					case ModuleType.FG:
						return FG;
					case ModuleType.ACU:
						return ACU;
					case ModuleType.Cam:
						return Cam;
				}
				return null;
			}
		}

		private List<IModule> EF
		{
			get
			{
				List<IModule> modules = plant.EF.GetRange(0, plant.EF.Count);

				if (newEF != null && !plant.EF.Contains(newEF) && newEF.State != ModuleState.Deleted)
					modules.Add(newEF);

				return modules;
			}
		}

		private List<IModule> FG
		{
			get
			{
				List<IModule> modules = plant.FG.GetRange(0, plant.FG.Count);

				if (newFG != null && !plant.FG.Contains(newFG) && newFG.State != ModuleState.Deleted)
					modules.Add(newFG);

				return modules;
			}
		}

		private List<IModule> ACU
		{
			get
			{
				List<IModule> modules = plant.ACU.GetRange(0, plant.ACU.Count);

				if (newACU != null && !plant.ACU.Contains(newACU) && newACU.State != ModuleState.Deleted)
					modules.Add(newACU);

				return modules;
			}
		}

		private List<IModule> Cam
		{
			get
			{
				List<IModule> modules = plant.Cam.GetRange(0, plant.Cam.Count);

				if (newCam != null && !plant.Cam.Contains(newCam) && newCam.State != ModuleState.Deleted)
					modules.Add(newCam);

				return modules;
			}
		}

		private static IModule newEF { get; set; }
		private static IModule newFG { get; set; }
		private static IModule newACU { get; set; }
		private static IModule newCam { get; set; }

		public ModuleTree(ModuleType type, Plant plant)
		{
			this.type = type;
			Name = GetResourceString(type.ToString());
			this.plant = plant;

			switch (type)
			{
				case ModuleType.EF:
					Image = WpfSvgRenderer.CreateImageSource(SvgImageHelper.CreateImage(
			new Uri("pack://application:,,,/Resources/Icons/Map/EFIdle.svg")), 1d, null, null, true);
					break;

				case ModuleType.FG:
					Image = WpfSvgRenderer.CreateImageSource(SvgImageHelper.CreateImage(
			new Uri("pack://application:,,,/Resources/Icons/Map/FGIdle.svg")), 1d, null, null, true);
					break;

				case ModuleType.ACU:
					Image = WpfSvgRenderer.CreateImageSource(SvgImageHelper.CreateImage(
			new Uri("pack://application:,,,/Resources/Icons/Map/OutIdle.svg")), 1d, null, null, true);
					break;

				case ModuleType.Cam:
					Image = WpfSvgRenderer.CreateImageSource(SvgImageHelper.CreateImage(
			new Uri("pack://application:,,,/Resources/Icons/MainWindow/Camera.svg")), 1d, null, null, true);
					break;
			}
		}
	}
}