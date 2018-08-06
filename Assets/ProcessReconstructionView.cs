using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class ProcessReconstructionView
    {
    Vector3[] newVertices = { new Vector3(0f, 0f, -0.05f), new Vector3(-0.05f, -0.05f, 0.05f), new Vector3(-0.05f, 0.05f, 0.05f), new Vector3(0.05f, -0.05f, 0.05f), new Vector3(0.05f, 0.05f, 0.05f) };
    int[] newTriangles = { 0, 3, 4, 4, 3, 1, 0, 1, 3, 0, 2, 1, 1, 2, 4, 0, 4, 2 };


    public List<CameraItem> ConvertToCameraItem(ReconstructionViewItem view)
    {
        int viewLength = view.views.Length;
        List<CameraItem> cameras = new List<CameraItem>();


        for(int i = 0; i < viewLength; i++)
        {

                Vector3 position = new Vector3(view.views[i].position[0], view.views[i].position[1], view.views[i].position[2]);
                Vector3 r = new Vector3(view.views[i].rotation[0], view.views[i].rotation[1], view.views[i].rotation[2]);

                r.Normalize();

                float angle = (float)Math.Sqrt(r.x * r.x + r.y * r.y + r.z * r.z);

            //angle?
                Quaternion rotation = Quaternion.AngleAxis(angle * 180 / (float)Math.PI , r * (1 / angle));
                Quaternion rotT = Quaternion.Inverse(rotation);

                Vector3 c = -(Matrix4x4.Rotate(rotT) * position);

                CameraItem cam = new CameraItem(c, rotation);

                cam.SetImageID(view.views[i].imageid);

                if (!view.views[i].estimated)
                {
                cam.estimated = false;
                }

                cameras.Add(cam);
            
        }
        
        return cameras;
    }

    public CameraItem DrawReconstructionView(List<CameraItem> reconstructionCameras, List<CameraItem> photocaptureCameras)
    {

        int numOfCameras = reconstructionCameras.Count();

        int trianglesCount = newTriangles.Length;
        int verticesCount = newVertices.Length;

        int numOfVertices = verticesCount * numOfCameras;
        int numOfTriangles = trianglesCount * numOfCameras;

        Vector3[] tmpVertices = new Vector3[numOfVertices];
        int[] tmpTriangles = new int[numOfTriangles];

        GameObject cameraObject = GameObject.Find("ReconstructionCamerasMesh");


        MeshFilter mf = cameraObject.GetComponent<MeshFilter>();

        Mesh mesh = mf.mesh;

        CameraItem transform = Ransac(reconstructionCameras, photocaptureCameras);

        for (int i = 0; i < numOfCameras; i++)
        {
            if (reconstructionCameras[i].imageID != -1)
            {
                        reconstructionCameras[i].position = Matrix4x4.Rotate(transform.rotation) * reconstructionCameras[i].position;
                        reconstructionCameras[i].position += transform.position;
                        reconstructionCameras[i].position = Matrix4x4.Scale(transform.scale) * reconstructionCameras[i].position;


                        Matrix4x4 rotationMatrix = Matrix4x4.Rotate(reconstructionCameras[i].rotation);

                        int offset = i * verticesCount;

                        for (int j = 0; j < trianglesCount; j++)
                        {
                            tmpTriangles[i * trianglesCount + j] = newTriangles[j] + offset;
                        }



                        for (int j = 0; j < verticesCount; j++)
                        {
                            tmpVertices[i * verticesCount + j] = rotationMatrix * newVertices[j] + new Vector4(reconstructionCameras[i].position.x, reconstructionCameras[i].position.y, reconstructionCameras[i].position.z,0);
                        }
                    
                
            }
        }

        mesh.vertices = tmpVertices;
        mesh.triangles = tmpTriangles;


        mf.mesh = mesh;

       // cameraObject.transform.Rotate(r.eulerAngles);
        //Debug.Log("Rotation of cameras: " + r.ToString() + ", "  + r.eulerAngles.ToString());
       // cameraObject.transform.Translate(translation);
       // cameraObject.transform.localScale = new Vector3(scale, scale, scale);


        return transform;
    }

    private float GetDistance(Vector3 a, Vector3 b)
    {
        return (float)Math.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y) + (a.z - b.z) * (a.z - b.z));
    }

    private CameraItem Ransac(List<CameraItem> reconstructionCameras, List<CameraItem> photocaptureCameras)
    {
        List<CameraItem> results = new List<CameraItem>();
        int cameraCount = reconstructionCameras.Count;

        for(int i = 0; i < cameraCount; i++)
        {
            if (reconstructionCameras[i].imageID != -1)
            {
                List<CameraItem> tmpRecCams = reconstructionCameras;

                Quaternion r = new Quaternion();

                Matrix4x4 R1 = Matrix4x4.Rotate(photocaptureCameras[i].rotation);
                Matrix4x4 R2 = Matrix4x4.Rotate(reconstructionCameras[i].rotation);


                Matrix4x4 R21 = R2.transpose * R1;

                Vector3 rot = new Vector3();
                float angle;


                R21.rotation.ToAngleAxis(out angle, out rot);

                 r = Quaternion.AngleAxis(angle, rot);

                for(int j = 0; j < cameraCount; j++)
                {
                    tmpRecCams[j].position = Matrix4x4.Rotate(r) * tmpRecCams[j].position;
                }

                Vector3 translation = photocaptureCameras[i].position - tmpRecCams[i].position;

                for(int j = 0; j < cameraCount; j++)
                {
                    tmpRecCams[j].position += translation;
                }
  
                float max = 0;
                Vector3 maxRecCam = tmpRecCams[i].position;
                Vector3 maxPhotoCam = photocaptureCameras[i].position;

                for (int k = 0; k < cameraCount; k++)
                {
                    if (k != i && tmpRecCams[k].imageID != -1 && GetDistance(reconstructionCameras[i].position, reconstructionCameras[k].position) > max)
                    {

                        max = GetDistance(reconstructionCameras[i].position, reconstructionCameras[k].position);
                        maxRecCam = reconstructionCameras[k].position;
                        maxPhotoCam = photocaptureCameras[k].position;
                    }    
                }

                Vector3 scale = new Vector3(maxPhotoCam.x / maxRecCam.x, maxPhotoCam.y / maxRecCam.y, maxPhotoCam.z / maxRecCam.z);

                for (int j = 0; j < cameraCount; j++)
                {
                    int matches = 0;
                    if(IsInlier(photocaptureCameras[j], tmpRecCams[j]))
                    {
                        matches++;
                    }
                    results.Add(new CameraItem(translation, r, scale, matches));
                }

            }
        }

        results = results.OrderBy(o => o.imageID).ToList();

        return results.Last<CameraItem>();
    }

    private bool IsInlier(CameraItem source, CameraItem reconstruction)
    {
        float rotLim = 5, transLim = 0.05f, scaleLim = 0.05f;

        Vector3 diff = source.position - reconstruction.position;

        if ((float)Math.Abs(diff.x) > transLim || (float)Math.Abs(diff.y) > transLim || (float)Math.Abs(diff.z) > transLim)
            return false;

        return true;
    }

}


