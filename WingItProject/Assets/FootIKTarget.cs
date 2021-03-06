﻿using UnityEngine;
using UnityEngine.Animations.Rigging;

public class FootIKTarget : MonoBehaviour
{
    [HideInInspector]
    public Vector3 position { get { return transform.position; } set { transform.position = value; } }
    public Quaternion rotation { get { return transform.rotation; } set { transform.rotation = value; } }
    public Transform mTransform { get { return transform; } }
    public Transform tip
    {
        get
        {
            if (mTwoBoneIKConstraint) return mTwoBoneIKConstraint.data.tip;
            else
            {
                Debug.LogWarning("Tip transform is not yet ready.");
                return null;
            }
        }
    }
    public Vector3 stablePosition { get; set; } //***//the position of this foot the last time it was touching the ground.

    public LimbSide limbSide;

    private TwoBoneIKConstraint mTwoBoneIKConstraint;

    private void OnEnable()
    {
        //Get the two bone IK component running for the foor.
        mTwoBoneIKConstraint = GetComponent<TwoBoneIKConstraint>();

        //initialize the position of the foot target as the tip of the two bone ik.
        transform.position = tip.position;
    }
}
