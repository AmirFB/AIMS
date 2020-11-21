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
using System.Windows.Shapes;
using AIMS2.BackEnd.Site;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;

using static AIMS2.BackEnd.Common;
using static AIMS2.BackEnd.DataBase;

namespace AIMS2.FrontEnd.Site
{
	public partial class SettingsWindow : ThemedWindow
	{
		public Plant Plant { get; set; }

		public SettingsWindow(Plant plant)
		{
			InitializeComponent();

			Plant = plant;
			DataContext = plant;
		}

		private void Save()
		{
			textEditName.GetBindingExpression(TextEdit.TextProperty).UpdateSource();
			comboBoxEditLanguage.GetBindingExpression(ComboBoxEdit.SelectedIndexProperty).UpdateSource();
			comboBoxEditConnection.GetBindingExpression(ComboBoxEdit.SelectedIndexProperty).UpdateSource();

			spinEditSerialTimoutBias.GetBindingExpression(SpinEdit.EditValueProperty).UpdateSource();
			spinEditSerialDelayBias.GetBindingExpression(SpinEdit.EditValueProperty).UpdateSource();
			spinEditTCPTimoutBias.GetBindingExpression(SpinEdit.EditValueProperty).UpdateSource();
			spinEditTCPDelayBias.GetBindingExpression(SpinEdit.EditValueProperty).UpdateSource();

			SaveConfig();
			Plant.SaveSettings();
		}

		private void Load()
		{
			textEditName.GetBindingExpression(TextEdit.TextProperty).UpdateTarget();
			comboBoxEditLanguage.GetBindingExpression(ComboBoxEdit.SelectedIndexProperty).UpdateTarget();
			comboBoxEditConnection.GetBindingExpression(ComboBoxEdit.SelectedIndexProperty).UpdateTarget();

			spinEditSerialTimoutBias.GetBindingExpression(SpinEdit.EditValueProperty).UpdateTarget();
			spinEditSerialDelayBias.GetBindingExpression(SpinEdit.EditValueProperty).UpdateTarget();
			spinEditTCPTimoutBias.GetBindingExpression(SpinEdit.EditValueProperty).UpdateTarget();
			spinEditTCPDelayBias.GetBindingExpression(SpinEdit.EditValueProperty).UpdateTarget();
		}

		private void OK_Click(object sender, RoutedEventArgs e)
		{
			Save();
			Close();
		}

		private void Apply_Click(object sender, RoutedEventArgs e)
		{
			Save();
		}

		private void Default_Click(object sender, RoutedEventArgs e)
		{
			Load();
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}