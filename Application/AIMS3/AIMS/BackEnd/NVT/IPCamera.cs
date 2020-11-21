using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Threading;
using utils;
using onvif.services;
using odm.core;

namespace AIMS3.BackEnd.NVT
{
	public partial class IPCamera
    {
        public string IP { get; set; }
		public int Port { get; set; } = 80;
        public string Username { get; set; }
        public string Password { get; set; }
        public string Address { get; set; }

        public byte DefaultPreset { get; set; } = 0;
        NetworkCredential account;
        NvtSessionFactory factory;
        INvtSession session;
		public List<Profile> Profiles { get; } = new List<Profile>();
		public List<PTZPreset> Presets { get; } = new List<PTZPreset>();

		Profile selectedProfile;
        public Profile Profile => selectedProfile;

		private static readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(5, 5);

		public async Task<bool> Initialize()
		{
			await semaphoreSlim.WaitAsync().ConfigureAwait(false);

			try { return await InitializeImmediate().ConfigureAwait(false); }
			finally { semaphoreSlim.Release(); }
		}

		public async Task<bool> InitializeImmediate()
		{
			Profile[] profiles;

			try
			{
				Address = "http://" + IP + ":" + Port + "/onvif/device_service";
				account = new NetworkCredential(Username, Password);
				factory = new NvtSessionFactory(account);

				Uri[] uris = (new Uri[] { new Uri(Address) });

				session = await factory.CreateSession(uris);

				profiles = await session.GetProfiles();

				selectedProfile = Profiles.FirstOrDefault();
				Profiles.Clear();
				Profiles.AddRange(profiles);

				return true;
			}
			catch (Exception ex) { }
			return false;
		}

		public async Task<PTZPreset[]> GetPresets()
		{
			try { return await session.GetPresets(selectedProfile.token); }
			catch (Exception ex) { return null; }
		}

        public async Task<bool> GoToPreset(int preset)
        {
			try
			{
				PTZSpeed speed = new PTZSpeed();
				speed = new PTZSpeed { zoom = new Vector1D() };
				speed.zoom.x = 0;
				speed.zoom.space = "";

				speed.panTilt = new Vector2D { x = 1, y = 1 };

				await session.GotoPreset(selectedProfile.token, Presets[preset].token, speed);

				return true;
			}
			catch (Exception ex) { }
			return false;
        }

        public async Task<bool> GoToHome()
        {
            try
            {
                PTZSpeed speed = new PTZSpeed();
				speed = new PTZSpeed { zoom = new Vector1D() };
				speed.zoom.x = 1;
                speed.zoom.space = "";

				speed.panTilt = new Vector2D { x = 1, y = 1 };
				await session.GotoHomePosition(selectedProfile.token, speed);// GoToPreset(DefaultPreset);

                return true;
            }

            catch (Exception ex)
            {
                return false;
            }
        }
    }

    public partial class PTZControl : IDisposable
    {
        INvtSession session;
        string profileToken;
        string nodeToken;

        public PTZControl(INvtSession session, string profileToken, string nodeToken)
        {
            this.session = session;
            this.profileToken = profileToken;
            this.nodeToken = nodeToken;

            InitPTZ();
            StartWorkingThread();
        }

        CompositeDisposable disposables = new CompositeDisposable();
        CompositeDisposable ptzDisposables = new CompositeDisposable();
        Space2DDescription pantiltSpace;
        Space1DDescription zoomSpace;
        object sync = new object();

        void InitPTZ()
        {
            var errMsg = "";

            ptzDisposables.Add(session
                .GetNode(nodeToken)
                .ObserveOnCurrentDispatcher()
                .Subscribe(node => {
                    if (node == null)
                    {
                        errMsg = "Error getting PTZ node. node == null ";
                        return;
                    }
                    if (node.supportedPTZSpaces == null)
                    {
                        errMsg = "Error getting PTZ node. SupportedPTZSpaces == null ";
                        return;
                    }
                    if (node.supportedPTZSpaces.continuousPanTiltVelocitySpace == null)
                    {
                        errMsg = "Error getting PTZ node. ContinuousPanTiltVelocitySpace == null ";
                        return;
                    }
                    if (node.supportedPTZSpaces.continuousZoomVelocitySpace == null)
                    {
                        errMsg = "Error getting PTZ node. ContinuousZoomVelocitySpace == null ";
                        return;
                    }

                    if (node.supportedPTZSpaces.continuousPanTiltVelocitySpace.Any())
                    {
                        errMsg = "Error getting PTZ node. ContinuousPanTiltVelocitySpace count = 0 ";
                        return;
                    }
                    if (!node.supportedPTZSpaces.continuousZoomVelocitySpace.Any())
                    {
                        errMsg = "Error getting PTZ node. ContinuousPanTiltVelocitySpace count = 0 ";
                        return;
                    }
                    pantiltSpace = node.supportedPTZSpaces.continuousPanTiltVelocitySpace.FirstOrDefault();
                    zoomSpace = node.supportedPTZSpaces.continuousZoomVelocitySpace.FirstOrDefault();

                    InitDirectionMove();
                }, err => {
                    errMsg = "Error getting PTZ node " + err.Message;
                }
            ));
        }

        void InitDirectionMove()
        {
            //    disposables.Add(Observable.Merge(
            //        Observable.FromEventPattern(btnUp, "PreviewMouseUp"),
            //        Observable.FromEventPattern(btnRight, "PreviewMouseUp"),
            //        Observable.FromEventPattern(btnDown, "PreviewMouseUp"),
            //        Observable.FromEventPattern(btnLeft, "PreviewMouseUp"),
            //        Observable.FromEventPattern(btnZoomOut, "PreviewMouseUp"),
            //        Observable.FromEventPattern(btnZoomIn, "PreviewMouseUp")).Subscribe(onstop => {
            //            //Stop
            //            StopMoving();
            //        })
            //    );
            //    //Directions
            //    disposables.Add(Observable.FromEventPattern(btnUp, "PreviewMouseDown")
            //        .Subscribe(click => {
            //            //move up
            //            MoveUp();
            //        })
            //    );
            //    disposables.Add(Observable.FromEventPattern(btnRight, "PreviewMouseDown")
            //        .Subscribe(click => {
            //            //move right
            //            MoveRight();
            //        })
            //    );
            //    disposables.Add(Observable.FromEventPattern(btnDown, "PreviewMouseDown")
            //        .Subscribe(click => {
            //            //move down
            //            MoveDown();
            //        })
            //    );
            //    disposables.Add(Observable.FromEventPattern(btnLeft, "PreviewMouseDown")
            //        .Subscribe(click => {
            //            //move left
            //            MoveLeft();
            //        })
            //    );
            //    //Zoom
            //    disposables.Add(Observable.FromEventPattern(btnZoomIn, "PreviewMouseDown")
            //        .Subscribe(click => {
            //            //zoom in
            //            ZoomIn();
            //        })
            //    );
            //    disposables.Add(Observable.FromEventPattern(btnZoomOut, "PreviewMouseDown")
            //        .Subscribe(click => {
            //            //zoom out
            //            ZoomOut();
            //        })
            //    );
        }

        bool IsStopped = false;

        void StartWorkingThread()
        {
            Task.Factory.StartNew(() => {
                Directions directionCopy = Directions.Initial;

                while (!IsStopped)
                {
                    bool isStop = false;
                    bool isMove = false;

                    PTZSpeed speedcopy = new PTZSpeed();

                    if (Interlocked.Equals(directionCopy, direction))
                    {
                        Thread.Sleep(50);
                    }
                    else
                    {
                        lock (sync)
                        {
                            if (direction == Directions.Stop)
                            {
                                isStop = true;
                            }
                            else
                            {
                                if (speed.zoom != null)
                                {
                                    speedcopy.zoom = new Vector1D();
                                    speedcopy.zoom.x = speed.zoom.x;
                                }
                                if (speed.panTilt != null)
                                {
                                    speedcopy.panTilt = new Vector2D();
                                    speedcopy.panTilt.x = speed.panTilt.x;
                                    speedcopy.panTilt.y = speed.panTilt.y;
                                }
                                directionCopy = direction;
                                isMove = true;
                            }
                        }

                        if (isStop)
                        {
                            Stop();
                            directionCopy = Directions.Stop;
                        }
                        if (isMove)
                        {
                            Move(speedcopy);
                        }
                    }
                }
            });

            disposables.Add(Disposable.Create(() => {
                IsStopped = true;
            }));
        }

        float GetPanSpeed()
        {
            try
            {
                if (pantiltSpace.xRange.max - pantiltSpace.xRange.min == 0)
                    return 0;
                else
                    return (float)pantiltSpace.xRange.max;
            }
            catch (Exception err)
            {
                var errMsg = "Error during getting Panspeed" + err.Message;
                return 0;
            }
        }
        float GetTiltSpeed()
        {
            try
            {
                if (pantiltSpace.yRange.max - pantiltSpace.yRange.min == 0)
                    return 0;
                else
                    return (float)pantiltSpace.yRange.max;
            }
            catch //(Exception err)
            {
                return 0;
            }
        }
        float GetZoomSpeed()
        {
            try
            {
                if (zoomSpace.xRange.max - zoomSpace.xRange.min == 0)
                    return 0;
                else
                    return (float)zoomSpace.xRange.max;
            }
            catch /*(Exception err)*/
            {
                return 0;
            }
        }

        public enum Directions
        {
            Initial,
            Stop,
            Up,
            Down,
            Left,
            Right,
            ZoomIn,
            ZoomOut
        }

        private Directions direction = Directions.Initial;
        PTZSpeed speed = new PTZSpeed();

        void MoveUp()
        {
            lock (sync)
            {
				speed = new PTZSpeed() { zoom = null };
				speed.panTilt = new Vector2D { x = 0, y = GetTiltSpeed() };
				direction = Directions.Up;
            }
        }
        void MoveDown()
        {
            lock (sync)
            {
                speed = new PTZSpeed();
                speed.zoom = null;

                speed.panTilt = new Vector2D();
                speed.panTilt.x = 0;
                speed.panTilt.y = -1 * GetTiltSpeed();
                direction = Directions.Down;
            }
        }
        void MoveLeft()
        {
            lock (sync)
            {
                speed = new PTZSpeed();
                speed.zoom = null;

                speed.panTilt = new Vector2D();
                speed.panTilt.x = -1 * GetPanSpeed();
                speed.panTilt.y = 0;
                direction = Directions.Left;
            }
        }
        void MoveRight()
        {
            lock (sync)
            {
                speed = new PTZSpeed();
                speed.zoom = null;

                speed.panTilt = new Vector2D();
                speed.panTilt.x = GetPanSpeed();
                speed.panTilt.y = 0;
                direction = Directions.Right;
            }
        }

        void ZoomIn()
        {
            lock (sync)
            {
                speed = new PTZSpeed();
                speed.panTilt = null;

                speed.zoom = new Vector1D();
                speed.zoom.x = GetZoomSpeed();
                direction = Directions.ZoomIn;
            }
        }
        void ZoomOut()
        {
            lock (sync)
            {
                speed = new PTZSpeed();
                speed.panTilt = null;

                speed.zoom = new Vector1D();
                speed.zoom.x = -1 * GetZoomSpeed();
                direction = Directions.ZoomOut;
            }
        }

        public void Move(PTZSpeed speed)
        {
            try
            {
                session.ContinuousMove(profileToken, speed, null).RunSynchronously();
            }
            catch /*(Exception err)*/
            {
                //Catch any errors
            }
        }
        public void Stop()
        {
            try
            {
                session.Stop(profileToken, true, true).RunSynchronously();
            }
            catch /*(Exception err)*/
            {
                //Catch any errors
            }
        }

        void StopMoving()
        {
            lock (sync)
            {
                speed = new PTZSpeed();
                direction = Directions.Stop;
            }
        }

        PTZSpeed GetMaxSpeed()
        {
            PTZSpeed maxspeed = new PTZSpeed();

            maxspeed.panTilt = new Vector2D();
            maxspeed.panTilt.x = 1;
            maxspeed.panTilt.y = 1;

            maxspeed.zoom = new Vector1D();
            maxspeed.zoom.x = 1;

            return maxspeed;
        }

        void GoHome()
        {
            ptzDisposables.Add(session
                .GotoHomePosition(profileToken, GetMaxSpeed()).Subscribe(
                success => {
                    //Do something if success
                }, err => {
                    //Do something if error
                })
            );
        }

        public void Dispose()
        {
            Stop();
            ptzDisposables.Dispose();
            disposables.Dispose();
        }
    }
}