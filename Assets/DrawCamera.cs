using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.WSA.WebCam;


//Draws cameras at the moment the image was taken
public class DrawCamera : MonoBehaviour {

    Vector3[] newVertices = { new Vector3( 0f, 0f, - 0.05f), new Vector3(-0.05f, -0.05f, 0.05f), new Vector3(-0.05f, 0.05f, 0.05f), new Vector3(0.05f, -0.05f, 0.05f), new Vector3(0.05f, 0.05f, 0.05f) };
    int[] newTriangles = { 0, 3, 4, 4, 3, 1, 0, 1, 3, 0, 2, 1, 1, 2, 4, 0, 4, 2 };

    //bool drawCamera = false;
	// Use this for initialization
	void Start () {
  
    }
	
	// Update is called once per frame
	void Update () {

	}

    public void NewMesh(Vector3 translation, Quaternion rotation)
    {

        GameObject cameraObject = GameObject.Find("CameraMesh");

        MeshFilter mf = cameraObject.GetComponent<MeshFilter>();

        //Debug.Log("Drawing camera at: " + translation.ToString() + ", rotation: " + rotation.ToString());

        Mesh mesh = mf.mesh;


        int numOfVertices = newVertices.Length + mesh.vertexCount;
        int numOfTriangles = newTriangles.Length + mesh.triangles.Length;
        int meshVertices = mesh.vertexCount;
        int meshTriangles = mesh.triangles.Length;

        Vector3[] tmpVertices = new Vector3[numOfVertices];
        int[] tmpTriangles = new int[numOfTriangles];


        for (int i = 0; i < meshVertices; i++)
        {
            tmpVertices[i] = mesh.vertices[i];
        }

        for (int i = 0; i < meshTriangles; i++)
        {
            tmpTriangles[i] = mesh.triangles[i];
        }

         int offset = meshVertices ;


         Matrix4x4 rotationMatrix = Matrix4x4.Rotate(rotation);


        for (int i = 0; i < newTriangles.Length; i++)
         {
             tmpTriangles[meshTriangles + i] = newTriangles[i] + offset;
         }


        for (int i = 0; i < newVertices.Length; i++)
        {
            tmpVertices[meshVertices + i] =  rotationMatrix * newVertices[i] + new Vector4(translation.x, translation.y, translation.z, 0);
        }



        mesh.vertices = tmpVertices;
        mesh.triangles = tmpTriangles;

        
        mf.mesh = mesh;


    }
}
