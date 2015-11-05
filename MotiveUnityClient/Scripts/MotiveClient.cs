using System;
using System.Threading;
using UnityEngine;

namespace MotiveStream
{
	public class MotiveClient : MonoBehaviour
	{
		//public int SleepTime = 10;
		private FrameData _Frame, _LastFrame;
		private SlipStream _Client;
		private Thread _Thread;
		private object _Mutex;
		private volatile bool _Receive = false;

		public event EventHandler<ReadOnlyEventArgs<FrameData>> NewFrameReceived;

		#region Private Methods
		private void _ReceptionThread()
		{
			_Client = new SlipStream();
			_Client.Connect();
			_Receive = true;

			while (_Receive && (_Client != null))
			{
				if(_Client.Connected)
				{
					//Debug.Log("Client Connected !");

					if (_Client.GetLastFrame())
					{
						lock (_Mutex)
						{
							// Copy to avoid thread concurrency.
							_LastFrame = new FrameData(_Client.LastFrame);
						}
					}
					//if (SleepTime > 0)
					//{
					//	Thread.Sleep(SleepTime);
					//}
				}
			}
		}
		#endregion

		#region Public Methods
		// Use this for initialization
		void Start()
		{
			try
			{
				_Receive = false;
				_Frame = new FrameData();
				_LastFrame = new FrameData();

				_Mutex = new object();
				_Thread = new Thread(new ThreadStart(_ReceptionThread));
				_Thread.Start();
			}
			catch (Exception ex)
			{
				Debug.LogError(ex.Message);
			}
		}

		void OnDestroy()
		{
			try
			{
				//Debug.Log("Killing Thread...");
				_Receive = false;
				if(_Thread != null)
				{
					//Debug.Log("Thread Join !");
					if(!_Thread.Join(100))
					{
						_Thread.Interrupt();
						//Debug.Log("Thread Interrupted !");
					}
				}
				//Debug.Log("Thread Killed !");

				//Debug.Log("Closing Client...");
				if (_Client != null)
				{
					_Client.Close();
					//Debug.Log("Client Closed !");
				}
			}
			catch (Exception ex)
			{
				Debug.LogError(ex.Message);
			}
		}

		// Update is called once per frame
		void Update()
		{
			if ( (_Client != null) && _Client.Connected)
			{
				//Debug.Log("Client connected !");
				lock (_Mutex)
				{
					_Frame = _LastFrame;
                }

				if (NewFrameReceived != null)
				{
					NewFrameReceived(this, new ReadOnlyEventArgs<FrameData>(_Frame));
				}
			}
			//else
			//{
			//    Debug.Log("Client Disconnected !");
			//}
		}
		#endregion
	}
}