using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using DevExpress.Xpf.Grid;
using AIMS3.BackEnd.Modules;
using AIMS3.BackEnd.Site;
using System.Linq;

#pragma warning disable CA2227

namespace AIMS3.FrontEnd.Modules.Common
{
	public partial class RelayAssign : UserControl
    {
        public IFault Fault { get; set; }

		public Plant Plant => Fault.Plant;
		public ObservableCollection<RelayModule> Modules { get; } = new ObservableCollection<RelayModule>();

		public RelayAssign()
        {
            InitializeComponent();

            DataContext = this;
        }

        public class RelayModule : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
			public void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

			public IModule Module { get; set; }
			public ObservableCollection<Relay> Relays => new ObservableCollection<Relay>(Module.Relays);
			public List<object> SelectedObjects { get => ToObject(); set => ToRelay(value); }
			public List<Relay> SelectedRelays { get; set; } = new List<Relay>();

			private bool selected;
            public bool Selected
            {
                get => selected;
                set
                {
                    selected = value;
                    OnPropertyChanged(nameof(Selected));
                }
            }

            public string Name => Module.Name;

			private List<object> ToObject() => SelectedRelays.Select(relay => (object)relay).ToList();

			private void ToRelay(List<object> objs)
			{
				SelectedRelays.Clear();
				objs.Select(obj => (Relay)obj).ToList().ForEach(relay => SelectedRelays.Add(relay));
			}
		}

		public void Save()
        {
            Fault.Relays.Clear();

			foreach (RelayModule module in Modules)
                if (module.Selected)
					Fault.Relays.AddRange(module.SelectedRelays);
        }

        public void Load()
        {
			Modules.Clear();

			foreach (AlarmControlUnit acu in Plant.ACU)
				if (Fault.Relays.Intersect(acu.Relays).Any())
					Modules.Add(new RelayModule() { Module = acu });
			
            foreach (ElectroFence ef in Plant.EF)
				if (Fault.Relays.Intersect(ef.Relays).Any())
					Modules.Add(new RelayModule() { Module = ef });

            foreach (FlexiGuard fg in Plant.FG)
				if (Fault.Relays.Intersect(fg.Relays).Any())
					Modules.Add(new RelayModule() { Module = fg });

			foreach (AlarmControlUnit acu in Plant.ACU)
				if (!Fault.Relays.Intersect(acu.Relays).Any())
					Modules.Add(new RelayModule() { Module = acu });

			foreach (ElectroFence ef in Plant.EF)
				if (!Fault.Relays.Intersect(ef.Relays).Any())
					Modules.Add(new RelayModule() { Module = ef });

			foreach (FlexiGuard fg in Plant.FG)
				if (!Fault.Relays.Intersect(fg.Relays).Any())
					Modules.Add(new RelayModule() { Module = fg });

			foreach (RelayModule module in Modules)
			{
				module.Selected = Fault.Relays.Intersect(module.Relays).Any();
				module.SelectedRelays.Clear();

				if (module.Selected)
					Fault.Relays.Intersect(module.Relays).ToList().ForEach(relay => module.SelectedRelays.Add(relay));
				
				module.OnPropertyChanged(nameof(Modules));
				module.OnPropertyChanged(nameof(RelayModule.SelectedObjects));
			}

			gridControl.GetBindingExpression(GridControl.ItemsSourceProperty).UpdateTarget();
            gridControl.RefreshData();
        }

	}
}