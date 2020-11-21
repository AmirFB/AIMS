using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Input;
using AIMS3.BackEnd.Modules;
using AIMS3.BackEnd.Site;
using DevExpress.Utils.Filtering;
using DevExpress.Xpf.Charts;

namespace AIMS3.FrontEnd.Basic
{
    public partial class FaultList : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public Plant Plant { get; set; }

        public ObservableCollection<IFault> Faults => Plant != null ? new ObservableCollection<IFault>(Plant.RaisedFaults) : null;
        private object lockObject = new object();

        public FaultList()
        {
            InitializeComponent();

            DataContext = this;
        }

        public void Refresh()
        {
            lock (lockObject)
            {
                for (int i = 0; i < Plant.RaisedFaults.Count; i++)
                {
                    if (!Plant.Faults.Contains(Faults[i]))
                        Faults[i--].Reset(Fault.ResetStatus.Local, false);
                }

                OnPropertyChanged(nameof(Faults));
            }
        }

        private void Fault_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if ((listBoxEdit.ItemsSource as ObservableCollection<IFault>).Count <= listBoxEdit.SelectedIndex)
                return;

            (listBoxEdit.SelectedItem as Fault)?.PopUpStatus();
        }
    }
}