using AIMS3.BackEnd.Modules;
using DevExpress.Xpf.Editors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using static AIMS3.BackEnd.Modules.Module;

namespace AIMS3.FrontEnd.Modules.Common
{
  public partial class RelayView : UserControl
  {
    Relay Relay { get; set; }

    public RelayView() { }

    public RelayView(Relay relay)
    {
      InitializeComponent();

      Relay = relay;
      DataContext = Relay;
    }

    public void Save()
    {
      toggleSwitchEnable.GetBindingExpression(ToggleSwitch.IsCheckedProperty).UpdateSource();
      toggleSwitchState.GetBindingExpression(ToggleSwitch.IsCheckedProperty).UpdateSource();
    }

    public void Load()
    {
      toggleSwitchEnable.GetBindingExpression(ToggleSwitch.IsCheckedProperty).UpdateTarget();
      toggleSwitchState.GetBindingExpression(ToggleSwitch.IsCheckedProperty).UpdateTarget();
    }

    private void Set_Click(object sender, RoutedEventArgs e)
    {
      Relay.SetNeeded = true;
      Relay.ResetNeeded = false;
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
      Relay.ResetNeeded = false;
      Relay.SetNeeded = true;
    }
  }
}