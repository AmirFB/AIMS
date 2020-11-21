using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using DevExpress.Xpf.Core;
using AIMS3.BackEnd.Modules;
using AIMS3.FrontEnd.Interfaces;

namespace AIMS3.FrontEnd.Modules.Cam
{
    public partial class CamWindow : AIMSWindow
    {
        public Player Player { get => player; set => player = value; }

        public double LeftPosition { get; set; }
        public double TopPosition { get; set; }
        public double WidthSize { get; set; }
        public double HeightSize { get; set; }
        public String CamName { get; set; }
        private static int count;

		private delegate void ChangeCallBack(double left, double top, double width, double height, string text);

		public CamWindow()
        {
            InitializeComponent();

            player.Stop();
            DataContext = this;
        }
		
        public override void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
			count--;
            player.Stop();
			Visibility = Visibility.Collapsed;
		}

        public void Play() => player.Play();
        public void Stop() => player.Stop();

        public void SetLocation(string text)
        {
            LeftPosition = SystemParameters.WorkArea.Width / 2 * (count % 2) + SystemParameters.WorkArea.Left;
            TopPosition = SystemParameters.WorkArea.Height / 2 * ((count / 2) % 2) + SystemParameters.WorkArea.Top;
            WidthSize = SystemParameters.WorkArea.Width / 2;
            HeightSize = SystemParameters.WorkArea.Height / 2;
			CamName = text;

			ChangeDimensions(LeftPosition, TopPosition, WidthSize, HeightSize, text);

			//OnPropertyChanged(nameof(LeftPosition");
   //         OnPropertyChanged(nameof(TopPosition");
   //         OnPropertyChanged(nameof(WidthSize");
   //         OnPropertyChanged(nameof(HeightSize");
   //         OnPropertyChanged(nameof(CamName");
            count++;
        }

		private void ChangeDimensions(double left, double top, double width, double height, string text)
		{
			if (Dispatcher.CheckAccess())
			{
				Left = left;
				Top = top;
				MinWidth = width - 1;
				MinWidth = width;
				MinHeight = height;
				Title = text;
			}

			else
			{
				ChangeCallBack d = new ChangeCallBack(ChangeDimensions);
				Application.Current.Dispatcher.Invoke(d, left, top, width, height, text);
			}
		}
	}
}