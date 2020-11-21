using System;
using System.Collections.Generic;
using System.Windows;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using AIMS3.BackEnd.Modules;
using AIMS3.FrontEnd.Interfaces;

using static AIMS3.BackEnd.Modules.Module;
using static AIMS3.BackEnd.Modules.Connection;

namespace AIMS3.FrontEnd.Modules.Common
{
	public partial class ModuleBasicView : AIMSUserControl
	{
        public IModule Module { get; private set; }
        private List<IModule> Collection => Module.Collection;
		private List<IModule> Modules => Module.Modules;
		private ModuleType type;
		public ModuleType Type
		{
			get => type;
			set
			{
				type = value;

				CamVisibility = type == ModuleType.Cam ? Visibility.Visible : Visibility.Collapsed;
				NotCamVisibility = type != ModuleType.Cam ? Visibility.Visible : Visibility.Collapsed;

				OnPropertyChanged(nameof(CamVisibility));
				OnPropertyChanged(nameof(NotCamVisibility));
			}
		}

		public SpinEdit SpinEditIndex => spinEditIndex;

        public string NameText => Module.PreName + spinEditIndex.EditValue;
		public bool IsTCP
		{
			get => Module.ConnectionType == ModuleConnectionType.TCP;
			set
			{
				if (value)
					Module.ConnectionType = ModuleConnectionType.TCP;

				else
					Module.ConnectionType = ModuleConnectionType.Serial;
			}
		}
		public string Port { get => Module.Port.ToString(); set => Module.Port = int.Parse(value); }

		public ToggleSwitch ToggleSwitchConnection => toggleSwitchConnection;

        public Visibility CamVisibility { get; set; }
        public Visibility NotCamVisibility { get; set; }

		private void ShowMessageBox(string key) => DXMessageBox.Show((string)TryFindResource(key), (string)TryFindResource("SaveFailure"), MessageBoxButton.OK, MessageBoxImage.Error);
		public void SetModule(IModule module) => Module = module;

		public ModuleBasicView()
        {
            InitializeComponent();

			DataContext = Module;
		}

		public bool Check()
        {
            if (Collection.FindIndex(module => module.Index == (int)spinEditIndex.EditValue && module != Module) >= 0)
            {
                ShowMessageBox("IndexExists");
                return false;
            }

            if (toggleSwitchConnection.IsChecked == false && Collection.FindIndex(module => module.Address == (int)spinEditAddress.EditValue && module != Module) >= 0)
            {
                ShowMessageBox("AddressExists");
                return false;
            }

            if (toggleSwitchConnection.IsChecked == true && type != ModuleType.Cam
				&& Modules.FindIndex(module =>
				module.IP == textEditIP.Text
				&& module.Port == int.Parse(textEditPort.Text)
				&& module.Address == (int)spinEditAddress.EditValue
				&& module != Module) >= 0)
            {
                ShowMessageBox("EndPointExists");
                return false;
            }

			if (textEditPort.IsEnabled && type == ModuleType.Cam && Collection.FindIndex(module => module.IP == textEditIP.Text && module != Module) >= 0)
			{
				ShowMessageBox("IPExists");
				return false;
			}

			return true;
        }

        public bool Save()
        {
			BindingGroup.UpdateSources();

            return true;
        }

        public void Load()
		{
			DataContext = Module;

			foreach (var binding in BindingGroup.BindingExpressions)
				binding.UpdateTarget();

			Type = type;
			textBlockName.Text = NameText;
		}

		public void Index_Changing(object sender, EditValueChangingEventArgs e)
		{
			int index = (int)e.NewValue;
			int sign = Math.Sign((int)e.NewValue - (e.OldValue != null ? (int)e.OldValue : 0));
			List<IModule> collection = Collection;

			while (collection.FindIndex(module => module.Index == index && module != Module) >= 0 && index > 0)
				index += sign;

			if (index == 0)
				spinEditIndex.EditValue = e.OldValue;

			else if (index != (int)e.NewValue)
				spinEditIndex.EditValue = index;

			//OnPropertyChanged(nameof(NameText");
			textBlockName.Text = NameText;
		}

		public void Address_Changing(object sender, EditValueChangingEventArgs e)
		{
			int address = (int)e.NewValue;
			int sign = Math.Sign((int)e.NewValue - (e.OldValue != null ? (int)e.OldValue : 0));
			List<IModule> collection = Collection;

			while (collection.FindIndex(module => module.Address == address && module != Module) >= 0 && address > 0)
				address += sign;

			if (address == 0)
				spinEditAddress.EditValue = e.OldValue;

			else if (address != (int)e.NewValue)
				spinEditAddress.EditValue = address;
		}
	}
}