using System.Collections.Generic;
using UnityEngine;

namespace MotiveStream
{
	[RequireComponent(typeof(MotiveClient))]
	public class MotiveClientTest : MonoBehaviour
	{
		public GameObject RigidBodyPrefab;
		public GameObject BonePrefab;

		private MotiveClient _MotiveClient;
		private Dictionary<int, GameObject> _DebugRigidBodies;
		private Dictionary<int, GameObject> _DebugBones;

		// Use this for initialization
		void Awake()
		{
			_DebugRigidBodies = new Dictionary<int, GameObject>();
			_DebugBones = new Dictionary<int, GameObject>();

			_MotiveClient = GetComponent<MotiveClient>();
			_MotiveClient.NewFrameReceived += OnNewFrameReceived;
        }

		void OnDestroy()
		{
			_MotiveClient.NewFrameReceived -= OnNewFrameReceived;
		}

		void OnNewFrameReceived(object sender, ReadOnlyEventArgs<FrameData> args)
		{
			//Debug.Log("New Frame received !");
			FrameData newFrame = args.Parameter;

			HashSet<int> toRemove = new HashSet<int>();
 
			// Update RigidBodies
			//// Update RigidBodies if needed
            foreach (var rb in newFrame.RigidBodies)
            {
				if (!_DebugRigidBodies.ContainsKey(rb.Key))
				{
					GameObject newRb = GameObject.Instantiate(RigidBodyPrefab);
					newRb.name = rb.Value.Name;
					_DebugRigidBodies.Add(rb.Key, newRb);
				}
				GameObject debugRb = _DebugRigidBodies[rb.Key];
				debugRb.transform.position = rb.Value.Position;
				debugRb.transform.rotation = rb.Value.Orientation;
			}
			//// Destroy RigidBodies if needed
			foreach(var debugRb in _DebugRigidBodies)
			{
				if(!newFrame.RigidBodies.ContainsKey(debugRb.Key))
				{
					GameObject.Destroy(debugRb.Value);
					toRemove.Add(debugRb.Key);
				}
			}
			foreach(int key in toRemove)
			{
				_DebugRigidBodies.Remove(key);
            }
			toRemove.Clear();

			// Update Bones
			//// Update Bones if needed
			foreach (var bone in newFrame.Bones)
			{
				if (!_DebugBones.ContainsKey(bone.Key))
				{
					GameObject newBone = GameObject.Instantiate(BonePrefab);
					newBone.name = bone.Value.Name;
					_DebugBones.Add(bone.Key, newBone);
				}
				GameObject debugBone = _DebugBones[bone.Key];
				debugBone.transform.position = bone.Value.Position;
				debugBone.transform.rotation = bone.Value.Orientation;
			}
			//// Destroy Bones if needed
			foreach (var debugBone in _DebugBones)
			{
				if (!newFrame.Bones.ContainsKey(debugBone.Key))
				{
					GameObject.Destroy(debugBone.Value);
					toRemove.Add(debugBone.Key);
				}
			}
			foreach(int key in toRemove)
			{
				_DebugBones.Remove(key);
			}
		}
	}
}