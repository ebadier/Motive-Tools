using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml;
using UnityEngine;

namespace MotiveStream
{
	public class SlipStream
	{
		private readonly int Port = 16000;
		private readonly int kMaxSubPacketSize = 1400;

		private IPEndPoint mRemoteIpEndPoint;
		private Socket mListener;
		private byte[] mReceiveBuffer;
		private string mPacket;
		private int mPreviousSubPacketIndex = 0;
		private XmlDocument mXmlDoc;

		public FrameData LastFrame { get; private set; }

		public bool Connected { get { return (mListener != null); } }

		#region Public Methods
		public SlipStream()
		{
			
        }

		public void Connect()
		{
			LastFrame = new FrameData();
			mXmlDoc = new XmlDocument();
			mReceiveBuffer = new byte[kMaxSubPacketSize];
			mPacket = string.Empty;

			mRemoteIpEndPoint = new IPEndPoint(IPAddress.Any, Port);
			mListener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			mListener.Bind(mRemoteIpEndPoint);
			mListener.Blocking = true;
			//mListener.Blocking = false;

			mListener.ReceiveBufferSize = 128 * 1024;
		}

		public void Close()
		{
			mListener.Close();
		}

		public bool GetLastFrame()
		{
			try
			{
				int bytesReceived = mListener.Receive(mReceiveBuffer);

				int maxSubPacketProcess = 200;

				while (bytesReceived > 0 && maxSubPacketProcess > 0)
				{
					//== ensure header is present ==--
					if (bytesReceived >= 2)
					{
						int subPacketIndex = mReceiveBuffer[0];
						bool lastPacket = mReceiveBuffer[1] == 1;

						if (subPacketIndex == 0)
						{
							mPacket = string.Empty;
						}

						if (subPacketIndex == 0 || subPacketIndex == mPreviousSubPacketIndex + 1)
						{
							mPacket += Encoding.ASCII.GetString(mReceiveBuffer, 2, bytesReceived - 2);

							mPreviousSubPacketIndex = subPacketIndex;

							if (lastPacket)
							{
								//== ok packet has been created from sub packets and is complete ==--
								// Parse packet
								ParsePacket(mPacket);
								return true;
                            }
						}
					}

					bytesReceived = mListener.Receive(mReceiveBuffer);

					//== time this out of packets are coming in faster than we can process ==--
					maxSubPacketProcess--;
				}
			}
			catch (System.Exception ex)
			{
				Debug.LogError(ex.Message);
			}

			return false;
		}
		#endregion

		#region Private Methods
		private void ParsePacket(string Packet)
		{
			LastFrame.Clear();
			mXmlDoc.LoadXml(Packet);

			//== skeletons ==--
			XmlNodeList boneList = mXmlDoc.GetElementsByTagName("Bone");
			for (int index = 0; index < boneList.Count; index++)
			{
				int boneID = System.Convert.ToInt32(boneList[index].Attributes["ID"].InnerText);
				string boneName = boneList[index].Attributes["Name"].InnerText;

				float x = (float)System.Convert.ToDouble(boneList[index].Attributes["x"].InnerText);
				float y = (float)System.Convert.ToDouble(boneList[index].Attributes["y"].InnerText);
				float z = (float)System.Convert.ToDouble(boneList[index].Attributes["z"].InnerText);

				float qx = (float)System.Convert.ToDouble(boneList[index].Attributes["qx"].InnerText);
				float qy = (float)System.Convert.ToDouble(boneList[index].Attributes["qy"].InnerText);
				float qz = (float)System.Convert.ToDouble(boneList[index].Attributes["qz"].InnerText);
				float qw = (float)System.Convert.ToDouble(boneList[index].Attributes["qw"].InnerText);

				//== coordinate system conversion (right to left handed) ==--
				Vector3 position = new Vector3(-x, y, z);
				Quaternion orientation = new Quaternion(-qx, qy, qz, -qw);

				LastFrame.Bones.Add(boneID, new BoneData(boneName, position, orientation));
            }

			//== rigid bodies ==--
			XmlNodeList rbList = mXmlDoc.GetElementsByTagName("RigidBody");
			for (int index = 0; index < rbList.Count; index++)
			{
				int rbID = System.Convert.ToInt32(rbList[index].Attributes["ID"].InnerText);
				string rbName = "RigidBody_" + rbID.ToString();

				float x = (float)System.Convert.ToDouble(rbList[index].Attributes["x"].InnerText);
				float y = (float)System.Convert.ToDouble(rbList[index].Attributes["y"].InnerText);
				float z = (float)System.Convert.ToDouble(rbList[index].Attributes["z"].InnerText);

				float qx = (float)System.Convert.ToDouble(rbList[index].Attributes["qx"].InnerText);
				float qy = (float)System.Convert.ToDouble(rbList[index].Attributes["qy"].InnerText);
				float qz = (float)System.Convert.ToDouble(rbList[index].Attributes["qz"].InnerText);
				float qw = (float)System.Convert.ToDouble(rbList[index].Attributes["qw"].InnerText);

				//== coordinate system conversion (right to left handed) ==--
				Vector3 position = new Vector3(-x, y, z);
				Quaternion orientation = new Quaternion(-qx, qy, qz, -qw);

				LastFrame.RigidBodies.Add(rbID, new RigidBodyData(rbName, position, orientation));
			}
		}
		#endregion
	}
}