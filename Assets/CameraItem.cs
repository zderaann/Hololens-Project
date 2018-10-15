using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

//class for representing cameras and their poses
public class CameraItem
    {
    public Vector3 position;
    public Quaternion rotation;
    public float imageID;
    public float scale;
    public bool estimated;
    public Vector3 center;

    public CameraItem()
    {
        position = new Vector3();
        rotation = new Quaternion();
        imageID = -1;
        scale = 1;
        estimated = true;
    }

        public CameraItem(Vector3 pos, Quaternion rot)
    {
        position = pos;
        rotation = rot;
        imageID  = -1;
        scale = 1;
        estimated = true;
        center = -(Matrix4x4.Rotate(Quaternion.Inverse(rot)) * pos);
    }

    public CameraItem(Vector3 pos, Quaternion rot, float s)
    {
        position = pos;
        rotation = rot;
        imageID = -1;
        scale = s;
        estimated = true;
        center = -(Matrix4x4.Rotate(Quaternion.Inverse(rot)) * pos);
    }

    public CameraItem(Vector3 pos, Quaternion rot, float s, float id)
    {
        position = pos;
        rotation = rot;
        imageID = id;
        scale = s;
        estimated = true;
        center = -(Matrix4x4.Rotate(Quaternion.Inverse(rot)) * pos);
    }

    public CameraItem(Vector3 pos, Quaternion rot, int id)
    {
        position = pos;
        rotation = rot;
        imageID = id;
        scale = 1; 
        estimated = true;
        center = -(Matrix4x4.Rotate(Quaternion.Inverse(rot)) * pos);
    }

    public void SetImageID(int id)
    {
        imageID = id;
    }

    //parses cameras from COLMAP creates a list of cameras with poses
    public List<CameraItem> ParseCameras(string file, int numberOfCameras)
    {
        List<CameraItem> cameras = new List<CameraItem>();

        byte[] byteArray = Encoding.UTF8.GetBytes(file);
        //byte[] byteArray = Encoding.ASCII.GetBytes(contents);
        MemoryStream stream = new MemoryStream(byteArray);

        int numberOfCams=0;
        int index = 1;

        // convert stream to string
        StreamReader r = new StreamReader(stream);
        while (true)
        {
            string line = r.ReadLine();
            if (line == null || line.Length == 0)
                break;
            if(line.StartsWith("# Number of images:"))
            {
                string[] v = line.Substring(0).Split(' ');
                string[] vv = v[4].Split(',');
                numberOfCams = Int32.Parse(vv[0]);
                if(numberOfCameras != numberOfCams)
                {
                    Debug.Log("Number of cameras does not match " + numberOfCameras + " vs. " + numberOfCams);
                    return null;
                }

                //get image number
            }
            if(index > 4)
            {
                string[] v = line.Substring(0).Split(' ');
                Quaternion q = new Quaternion(float.Parse(v[2]),float.Parse(v[3]),float.Parse(v[4]), float.Parse(v[1]));
                Vector3 t = new Vector3(float.Parse(v[5]), float.Parse(v[6]), float.Parse(v[7]));
                string[] i = v[9].Substring(0).Split('_');



                CameraItem cam = new CameraItem(t, q, 1,  float.Parse(i[0] + "." + i[1]));
                cameras.Add(cam);
                r.ReadLine();
            }
            index++;
            if(index > 4 + numberOfCams)
            {
                break;
            }
        }

        return cameras;
    }
}

