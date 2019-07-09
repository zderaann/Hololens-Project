using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//class to get the name of the folder from JSON
[Serializable]
public class CalculatedTransform
{
    public float scale;
    public List<float> translation;
    public List<float> rotation1;
    public List<float> rotation2;
    public List<float> rotation3;
    public List<float> cameras;
    public List<float> rotcams;
    public List<float> XHx;
    public List<float> XHy;
    public List<float> XHz;
    public Vector3 trans;
    public Matrix4x4 rot;
    public Quaternion[] camrotation;

    public void initialize()
    {
        trans = new Vector3(translation[0], translation[1], translation[2]);
        rot = new Matrix4x4();
        rot.SetRow(0, new Vector4(rotation1[0], rotation1[1], rotation1[2], 0f));
        rot.SetRow(1, new Vector4(rotation2[0], rotation2[1], rotation2[2], 0f));
        rot.SetRow(2, new Vector4(rotation3[0], rotation3[1], rotation3[2], 0f));
        rot.SetRow(3, new Vector4(0f, 0f, 0f, 1f));

        int cnt = rotcams.Count / 4;
        camrotation = new Quaternion[cnt];
        
        for(int i = 0; i < cnt; i++)
        {
            int index = 4 * i;
            Quaternion q = new Quaternion( rotcams[index + 1], rotcams[index + 2], rotcams[index + 3], rotcams[index]); 
            /*Vector3 myEulerAngles = q.eulerAngles;
            Quaternion reversedY = Quaternion.Euler(myEulerAngles.x, -myEulerAngles.y, -myEulerAngles.z);*/
            camrotation[i] =  q;
        }
    }

}


