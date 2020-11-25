using System.Windows.Controls;
using AIMS3.BackEnd.Modules;

namespace AIMS3.FrontEnd.Modules.Common
{
  public partial class RelaysView : UserControl
  {
    private IModule Module { get; set; }

    public RelaysView()
    {
      InitializeComponent();
    }

    public void SetModule(IModule module)
    {
      Module = module;
      grid.Children.Clear();

      for (int i = 0; i < Module.Relays.Count; i++)
      {
        Module.Relays[i].View = new RelayView(Module.Relays[i]);
        grid.Children.Add(Module.Relays[i].View);

        Grid.SetRow(Module.Relays[i].View, i / 2);
        Grid.SetColumn(Module.Relays[i].View, i % 2);
      }
    }

    public void Save()
    {
      foreach (Relay relay in Module.Relays)
        relay.Save();
    }

    public void Load()
    {
      foreach (Relay relay in Module.Relays)
        relay.Load();
    }
  }
}