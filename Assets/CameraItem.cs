using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class CameraItem
    {
    public Vector3 position;
    public Quaternion rotation;
    public int imageID;
    public Vector3 scale;
    public bool estimated;

    public CameraItem(Vector3 pos, Quaternion rot)
    {
        position = pos;
        rotation = rot;
        imageID  = -1;
        scale = new Vector3(1,1,1);
        estimated = true;
    }

    public CameraItem(Vector3 pos, Quaternion rot, Vector3 s)
    {
        position = pos;
        rotation = rot;
        imageID = -1;
        scale = s;
        estimated = true;
    }

    public CameraItem(Vector3 pos, Quaternion rot, Vector3 s, int id)
    {
        position = pos;
        rotation = rot;
        imageID = id;
        scale = s;
        estimated = true;
    }

    public void SetImageID(int id)
    {
        imageID = id;
    }
}

