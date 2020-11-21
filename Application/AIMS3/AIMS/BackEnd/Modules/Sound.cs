using System;
using System.Media;
using System.Windows;

namespace AIMS3.BackEnd.Modules
{
    public static class Sound
    {
		private static SoundPlayer DefaultPlayer1 { get; } = new SoundPlayer(Application.GetResourceStream(new Uri(@"pack://application:,,,/Resources/Sirens/Siren1.wav")).Stream);
		private static SoundPlayer DefaultPlayer2 { get; } = new SoundPlayer(Application.GetResourceStream(new Uri(@"pack://application:,,,/Resources/Sirens/Siren2.wav")).Stream);
        private static bool isPlaying1 = false, isPlaying2 = false;

        public static void Play(bool index)
        {
            if (index)
            {
                //if (isPlaying1)
                //    return;

                try
                { 
                    DefaultPlayer1.PlayLooping();
                    isPlaying1 = true;
                    isPlaying2 = false;
                }
                catch (Exception ex) { }
            }

            else
            {
                //if (isPlaying2)
                //    return;

                try
                {
                    DefaultPlayer2.PlayLooping();
                    isPlaying2 = true;
                    isPlaying1 = false;
                }
                catch (Exception ex) { }
            }
        }

		public static void Stop()
		{
            try
            {
                DefaultPlayer1.Stop();
                isPlaying1 = false;
                isPlaying2 = false;
            }
            catch (Exception ex) { }
        }
	}
}