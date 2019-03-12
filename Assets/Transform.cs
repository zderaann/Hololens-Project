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
    public Vector3 trans;
    public Matrix4x4 rot;

}