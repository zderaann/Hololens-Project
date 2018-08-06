using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.WSA.Persistence;
using UnityEngine.XR.WSA;

public class ReconstructionVisualizer : MonoBehaviour {

   // public string reconstructionFile = "";
    //private bool isEmpty = true;
    public Material material;

	// Use this for initialization
	void Start () {

    }



    // Update is called once per frame
    void Update () {

    }


    public bool DrawMesh(string reconstructionFile)
    {
        Mesh mesh = new PLYloader().LoadFile(reconstructionFile, false);

        if (mesh != null)
        {
            GameObject reconstructionObject = GameObject.Find("ReconstructionObject");

            MeshFilter mf = reconstructionObject.GetComponent<MeshFilter>();
            mf.mesh = mesh;
            // Graphics.DrawMesh(mesh, Vector3.zero, Quaternion.identity, material, 0);
            Debug.Log("Mesh drawn");
            return true;

        }
        else
        {
            Debug.Log("Model mesh is empty");
            return false;
        }

       
    }


    public void TransformMesh(CameraItem transform)
    {
        GameObject reconstructionObject = GameObject.Find("ReconstructionObject");
        // MeshFilter mf = reconstructionObject.GetComponent<MeshFilter>();

        // Matrix4x4 rotationMatrix = Matrix4x4.Rotate(transform.rotation);

        // Matrix4x4 scaleMatrix = Matrix4x4.Scale(transform.scale);


        /*for (int i = 0; i < mf.mesh.vertexCount; i++)
        {
            mf.mesh.vertices[i] = rotationMatrix * mf.mesh.vertices[i];
            mf.mesh.vertices[i] += transform.position;
           // mf.mesh.vertices[i] = scaleMatrix * mf.mesh.vertices[i];
        }*/

        reconstructionObject.transform.Rotate(transform.rotation.eulerAngles);
        reconstructionObject.transform.Translate(transform.position);
    }
}
