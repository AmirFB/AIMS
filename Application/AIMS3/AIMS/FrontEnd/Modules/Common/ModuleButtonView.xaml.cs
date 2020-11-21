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
using AIMS3.BackEnd.Site;
using AIMS3.FrontEnd.Interfaces;

namespace AIMS3.FrontEnd.Modules.Common
{
	public partial class ModuleButtonView : AIMSUserControl
	{
		public Visibility LocalVisibility => Site is LocalSite ? Visibility.Visible : Visibility.Collapsed;

		public ModuleButtonView()
		{
			InitializeComponent();
			DataContext = this;
		}
	}
}