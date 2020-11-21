using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Xpf.Core;

using static AIMS3.BackEnd.Common;

namespace AIMS3.FrontEnd.Basic
{
    public partial class ApplicationSettingsWindow : ThemedWindow
    {
        public static IEnumerable<Language> LanguageTypeValues => Enum.GetValues(typeof(Language)).Cast<Language>();
        public int SelectedLanguageIndex {
            get => (int)Tongue;
            set => Tongue = (Language)value; }

        public ApplicationSettingsWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void Apply_Click(object sender, System.Windows.RoutedEventArgs e) { }// => comboBoxEditLanguages.GetBindingExpression(ComboBoxEdit.SelectedIndexProperty).UpdateTarget();
        private void Default_Click(object sender, System.Windows.RoutedEventArgs e) { }//=> OnPropertyChanged(nameof(SelectedLanguageIndex));
        private void OK_Click(object sender, System.Windows.RoutedEventArgs e) { }//=> Close();
    }
}