using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CheckpointController : MonoBehaviour
{
    [System.Serializable]
    public struct Checkpoint
    {
        public Transform checkpointTransform;
        public BoxCollider collider;
        public int index;
        public bool isActive;
    }

    public List<Checkpoint> checkpoints = new List<Checkpoint>();

    private void OnValidate()
    {
        LoadCheckpoints();
    }

    private void Start()
    {
        LoadCheckpoints();
    }

    private void LoadCheckpoints()
    {
        checkpoints.Clear();

        foreach (Transform trans in transform.GetComponentsInChildren<Transform>().Where(t => t.gameObject.name.StartsWith("Checkpoint_") || t.gameObject.name == "Start/Finish"))
        {
            //Handle StartFinish
            if (trans.gameObject.name == "Start/Finish") 
            {
                Checkpoint startFinish = new Checkpoint
                {
                    checkpointTransform = trans,
                    collider = trans.GetComponent<BoxCollider>(),
                    index = 0,
                    isActive = false
                };
                checkpoints.Add(startFinish);
                continue;
            }

            Checkpoint checkpoint = new Checkpoint
            {
                checkpointTransform = trans,
                collider = trans.GetComponent<BoxCollider>(),
                index = int.Parse(trans.gameObject.name.Replace("Checkpoint_", "")),
                isActive = false
            };
            checkpoints.Add(checkpoint);
        }

        checkpoints.Sort((a, b) => a.index.CompareTo(b.index));
    }

}
