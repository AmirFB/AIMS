using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using AIMS3.BackEnd.Modules;

namespace AIMS3.FrontEnd.Modules.Cam
{
	public partial class Player : UserControl
    {
        public Camera Cam { get; set; }
        public Player Control { get; set; }

        public Uri URI => new Uri(@"rtsp://" + Cam.IP + ":" + Cam.RtspPort + Cam.RTSPAddress + Cam.Token);

        private delegate void PlayerCallBack();

        public Player()
        {
            InitializeComponent();
        }

        public void Play()
        {
            if (player.SourceProvider.MediaPlayer == null)
                CreateMediaPlayer();

            ConnectSafe();
		}

		public void Stop()
		{
			try { player.SourceProvider.MediaPlayer?.ResetMedia(); }
			catch (Exception ex) { }
		}

		private void CreateMediaPlayer()
        {
            var currentAssembly = Assembly.GetEntryAssembly();
            var currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;
            var libDirectory = new DirectoryInfo(System.IO.Path.Combine(currentDirectory, "libvlc", IntPtr.Size == 4 ? "win-x86" : "win-x64"));

            player.SourceProvider.CreatePlayer(libDirectory);
        }

        private void ConnectSafe()
        {
            if (player.Dispatcher.CheckAccess())
            {
                try
                {
                    string[] options = new string[] { ":aspect-ratio=16:9", ":rtsp-" + (Cam.RtspStream == Camera.Stream.UDP ? "udp" : "tcp"), ":network-caching=" + Cam.Cache, ":rtsp-user=" + Cam.Username, ":rtsp-pwd=" + Cam.Password };
                    Stop();
                    player.SourceProvider.MediaPlayer.SetMedia(URI, options);
					player.SourceProvider.MediaPlayer.Play();
                    player.SourceProvider.MediaPlayer.Audio.IsMute = true;
				}
                catch (Exception ex) { }
            }

            else
            {
                PlayerCallBack d = new PlayerCallBack(ConnectSafe);
                Dispatcher.Invoke(d);
            }
        }

		private void Play_Click(object sender, RoutedEventArgs e)
		{
			Play();
		}

		private void Stop_Click(object sender, RoutedEventArgs e)
		{
			Stop();
		}
	}
}