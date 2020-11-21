using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AIMS3.BackEnd.Modules;
using AIMS3.BackEnd.Site;
using AIMS3.FrontEnd.Modules.Interfaces;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Core.Native;

using static AIMS3.BackEnd.Common;
using static AIMS3.BackEnd.Modules.Module;

namespace AIMS3.FrontEnd.Modules.Common
{
	public partial class ModuleList : UserControl, INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		public void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

		private ITelemetricSite site;
		public ITelemetricSite Site
		{
			get => site;
			set
			{
				site = value;
				plant = site.Plant;
			}
		}

		private Plant plant;
		public Plant Plant
		{
			get => plant;
			set
			{
				plant = value;
				site = plant.Owner;

				Modules.Add(new ModuleTree(ModuleType.EF, Plant));
				Modules.Add(new ModuleTree(ModuleType.FG, Plant));
				Modules.Add(new ModuleTree(ModuleType.ACU, Plant));
				Modules.Add(new ModuleTree(ModuleType.Cam, Plant));
				treeView.ItemsSource = Modules;
				Refresh();
			}
		}

		private IModule selectedModule;
		public IModule SelectedModule
		{
			get
			{
				if (treeView.SelectedItem is IModule)
					selectedModule = treeView.SelectedItem as IModule;

				else if (Plant.Modules.Count > 0)
					selectedModule = Plant.Modules[0];

				return selectedModule;
			}
		}

		public IModuleView ModuleView => SelectedModule.View;
		public ObservableCollectionCore<ModuleTree> Modules { get; } = new ObservableCollectionCore<ModuleTree>();

		public Visibility ModuleVisibility => (treeView.SelectedItem is IModule) ? Visibility.Visible : Visibility.Collapsed;

		public ModuleList()
        {
            InitializeComponent();
			DataContext = this;
		}

		public void Refresh()
		{
			//OnPropertyChanged(nameof(Modules));
			//treeView.GetBindingExpression(TreeView.ItemsSourceProperty).UpdateTarget();
			var temp = treeView.ItemsSource;
			treeView.ItemsSource = null;
			//treeView.ItemsSource = Modules;
			treeView.ItemsSource = temp;
			Plant.Modules.ForEach(module => module.View.List = this);
		}

		public void Select(IModule module)
		{
			OnPropertyChanged(nameof(ModuleView));
		}

		private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			//OnPropertyChanged(nameof(Modules));

			if (!(treeView.SelectedItem is IModule))
				return;

			OnPropertyChanged(nameof(ModuleView));
			SelectedModule.View.Load();
		}

		private void TreeView_New(object sender, RoutedEventArgs e)
		{
			IModule newModule;
			var selected = treeView.SelectedValue;

			if (selected is ElectroFence || selected == Modules[0])
			{
				Modules[0].IsExpanded = true;

				if (Modules[0].NewModule == null || Plant.Modules.Contains(Modules[0].NewModule))
					Modules[0].NewModule = new ElectroFence(Plant) { ZoneType = ElectroFence.ModuleZonesType.TwoZonesCommon };

				else
				{
					Refresh();
					Modules[0].NewModule.IsSelected = true;
					return;
				}

				newModule = Modules[0].NewModule;
				Modules[0].NewModule.IsSelected = true;
			}

			else if (selected is FlexiGuard || selected == Modules[1])
			{
				Modules[1].IsExpanded = true;

				if (Modules[1].NewModule == null || Plant.Modules.Contains(Modules[1].NewModule))
					Modules[1].NewModule = new FlexiGuard(Plant);

				else
				{
					Refresh();
					Modules[1].NewModule.IsSelected = true;
					return;
				}

				newModule = Modules[1].NewModule;
				Modules[1].NewModule.IsSelected = true;
			}

			else if (selected is AlarmControlUnit || selected == Modules[2])
			{
				Modules[2].IsExpanded = true;

				if (Modules[2].NewModule == null || Plant.Modules.Contains(Modules[2].NewModule))
					Modules[2].NewModule = new AlarmControlUnit(Plant);

				else
				{
					Refresh();
					Modules[2].NewModule.IsSelected = true;
					return;
				}

				newModule = Modules[2].NewModule;
				Modules[2].NewModule.IsSelected = true;
			}

			else //if (sender is Camera || sender == Modules[3])
			{
				Modules[3].IsExpanded = true;

				if (Modules[3].NewModule == null || Plant.Modules.Contains(Modules[3].NewModule) || Modules[3].NewModule.State == ModuleState.Deleted)
					Modules[3].NewModule = new Camera(Plant);

				else
				{
					Refresh();
					Modules[3].NewModule.IsSelected = true;
					return;
				}

				newModule = Modules[3].NewModule;
				Modules[3].NewModule.IsSelected = true;
			}

			newModule.GetNewIndex();
			newModule.GetNewAddress();
			newModule.GenerateHash();
			newModule.InitializeView();
			newModule.State = ModuleState.New;
			newModule.View.List = this;
			newModule.Zones.ForEach(zone => zone.Icon.IsPlaced = true);

			Refresh();
		}

		private void TreeView_Remove(object sender, RoutedEventArgs e) => SelectedModule.View.Delete_Click(sender, e);

		private void TreeView_RemoveAll(object sender, RoutedEventArgs e)
		{

		}

		private void TreeView_PreviewRightClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			TreeViewItem treeViewItem =
				(TreeViewItem)SearchTreeView<TreeViewItem> ((DependencyObject)e.OriginalSource);

			if (treeViewItem != null)
			{
				treeViewItem.IsSelected = true;
				e.Handled = true;
			}

			OnPropertyChanged(nameof(ModuleVisibility));
		}

		private static DependencyObject SearchTreeView<T>(DependencyObject source)
		{
			while (source != null && source.GetType() != typeof(T))
				source = VisualTreeHelper.GetParent(source);

			return source;
		}
	}

	public class ModuleTree
	{
		public event PropertyChangedEventHandler PropertyChanged;
		public void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

		private Plant plant;
		public string Name { get; set; }
		private ModuleType type;
		public ImageSource Image { get; }
		public int Count => Modules.Count;

		public bool IsExpanded { get; set; }

		public ObservableCollection<IModule> Modules
		{
			get
			{
				switch (type)
				{
					case ModuleType.EF: return new ObservableCollection<IModule>(EF);
					case ModuleType.FG: return new ObservableCollection<IModule>(FG);
					case ModuleType.ACU: return new ObservableCollection<IModule>(ACU);
					case ModuleType.Cam: return new ObservableCollection<IModule>(Cam);
				}
				return null;
			}
		}

		private List<IModule> EF
		{
			get
			{
				List<IModule> modules = plant.EF.GetRange(0, plant.EF.Count);

				if (NewModule != null && NewModule is ElectroFence && !plant.EF.Contains(NewModule) && NewModule.State != ModuleState.Deleted)
					modules.Add(NewModule);

				return modules;
			}
		}

		private List<IModule> FG
		{
			get
			{
				List<IModule> modules = plant.FG.GetRange(0, plant.FG.Count);

				if (NewModule != null && NewModule is FlexiGuard && !plant.FG.Contains(NewModule) && NewModule.State != ModuleState.Deleted)
					modules.Add(NewModule);

				return modules;
			}
		}

		private List<IModule> ACU
		{
			get
			{
				List<IModule> modules = plant.ACU.GetRange(0, plant.ACU.Count);

				if (NewModule != null && NewModule is AlarmControlUnit && !plant.ACU.Contains(NewModule) && NewModule.State != ModuleState.Deleted)
					modules.Add(NewModule);

				return modules;
			}
		}

		private List<IModule> Cam
		{
			get
			{
				List<IModule> modules = plant.Cam.GetRange(0, plant.Cam.Count);

				if (NewModule != null && NewModule is Camera && !plant.Cam.Contains(NewModule) && NewModule.State != ModuleState.Deleted)
					modules.Add(NewModule);

				return modules;
			}
		}

		public IModule NewModule { get; set; }

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

		public void Refresh() => OnPropertyChanged(nameof(Modules));
	}
}