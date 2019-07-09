using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

//Processes cameras from COLMAP
public class ProcessReconstructionView
    {
    Vector3[] newVertices = { new Vector3(0f, 0f, 0.05f), new Vector3(-0.05f, -0.05f, -0.05f), new Vector3(-0.05f, 0.05f, -0.05f), new Vector3(0.05f, -0.05f,- 0.05f), new Vector3(0.05f, 0.05f, -0.05f) };
    int[] newTriangles = { 0, 3, 4, 4, 3, 1, 0, 1, 3, 0, 2, 1, 1, 2, 4, 0, 4, 2 };

    //Converts cameras from server to camera items
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

                //cam.SetImageID(view.views[i].imageid);

                cameras.Add(cam);
            
        }
        
        return cameras;
    }

    public void DrawCameras(List<float> cams, Quaternion[] rot, Matrix4x4 transrot, List<CameraItem> rotcams)
    {
        int numOfCameras = cams.Count / 3;

        int trianglesCount = newTriangles.Length;
        int verticesCount = newVertices.Length;

        int numOfVertices = verticesCount * numOfCameras;
        int numOfTriangles = trianglesCount * numOfCameras;

        Vector3[] tmpVertices = new Vector3[numOfVertices];
        int[] tmpTriangles = new int[numOfTriangles];


        GameObject cameraObject = GameObject.Find("ReconstructionCamerasMesh");


        MeshFilter mf = cameraObject.GetComponent<MeshFilter>();

        Mesh mesh = mf.mesh;


        for (int i = 0; i < numOfCameras; i++)
        {
            int offset = i * verticesCount;
            int index = i * 3;

            for (int j = 0; j < trianglesCount; j++)
            {
                tmpTriangles[i * trianglesCount + j] = newTriangles[j] + offset;
            }


            for (int j = 0; j < verticesCount; j++)
            {
                Vector3 coords = new Vector3(cams[index], cams[index + 1], cams[index + 2]);
                Matrix4x4 R = transrot * Matrix4x4.Rotate(rot[i]) * transrot.inverse;

                //Matrix4x4 R = Matrix4x4.Rotate(rotcams[i].rotation);
                Vector4 final = R * newVertices[j];
               // Vector4 final = transrot * rotated;
                tmpVertices[i * verticesCount + j] =  new Vector3(final[0], final[1], final[2]) + coords;
            }



        }

        mesh.vertices = tmpVertices;
        mesh.triangles = tmpTriangles;


        mf.mesh = mesh;

        /*
        return trans;
        */
    }


    public void DrawTransformedColmapCams(List<float>XHx, List<float> XHy, List<float> XHz, int numOfCams, List<float> cams)
    {
         int[] camTriangles = { 0, 3, 4, 4, 3, 1, 0, 1, 3, 0, 2, 1, 1, 2, 4, 0, 4, 2 };
        int index = 0;
        int cameraIndex = 0;

        int trianglesCount = camTriangles.Length;
        int verticesCount = newVertices.Length;

        int numOfVertices = verticesCount * numOfCams;
        int numOfTriangles = trianglesCount * numOfCams;

        Vector3[] tmpVertices = new Vector3[numOfVertices];
        int[] tmpTriangles = new int[numOfTriangles];

        GameObject cameraObject = GameObject.Find("ReconstructionCamerasMesh");


        MeshFilter mf = cameraObject.GetComponent<MeshFilter>();

        Mesh mesh = mf.mesh;


        for (int i = 0; i < numOfCams; i++)
        {
            int offset = i * verticesCount;
            Vector3[] camVertices = { new Vector3(cams[cameraIndex], cams[cameraIndex + 1], cams[cameraIndex + 2]), //camera centre
                                      new Vector3(XHx[index+2], XHy[index+2], XHz[index+2]),
                                      new Vector3(XHx[index+1], XHy[index+1], XHz[index+1]),                      
                                      new Vector3(XHx[index+3], XHy[index+3], XHz[index+3]),
                                      new Vector3(XHx[index+4], XHy[index+4], XHz[index+4])};


            for (int j = 0; j < trianglesCount; j++)
            {
                tmpTriangles[i * trianglesCount + j] = camTriangles[j] + offset;
            }


            for (int j = 0; j < verticesCount; j++)
            {
                tmpVertices[i * verticesCount + j] = new Vector3(camVertices[j][0], camVertices[j][1], camVertices[j][2]);
            }


            cameraIndex += 3;
            index += 5;
        }

        mesh.vertices = tmpVertices;
        mesh.triangles = tmpTriangles;


        mf.mesh = mesh;

    }

    //Transforms and draws COLMAP cameras
    public void DrawReconstructionView(List<CameraItem> reconstructionCameras, List<CameraItem> photocaptureCameras, CalculatedTransform cameraTransform)
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


        for (int i = 0; i < numOfCameras; i++)
        {

            Matrix4x4 rotationMatrix = Matrix4x4.Rotate(reconstructionCameras[i].rotation);

            

            int offset = i * verticesCount;

             for (int j = 0; j < trianglesCount; j++)
             {
                tmpTriangles[i * trianglesCount + j] = newTriangles[j] + offset;
             }


             for (int j = 0; j < verticesCount; j++)
             {
                tmpVertices[i * verticesCount + j] = rotationMatrix * newVertices[j] + new Vector4(reconstructionCameras[i].position.x, reconstructionCameras[i].position.y, reconstructionCameras[i].position.z, 1);
            }
                    
                
            
        }

        mesh.vertices = tmpVertices;
        mesh.triangles = tmpTriangles;


        mf.mesh = mesh;

        cameraObject.transform.Translate(cameraTransform.trans);
        cameraObject.transform.Rotate(cameraTransform.rot.rotation.eulerAngles);
        cameraObject.transform.localScale = cameraObject.transform.localScale * cameraTransform.scale;

        /*
        return trans;
        */

    }

    private RansacItem Ransac(List<CameraItem> reconstructionCameras, List<CameraItem> photocaptureCameras, int tests, int numOfPerm)
    {
        int ncams = photocaptureCameras.Count();
        Quaternion[] R1 = new Quaternion[ncams];
        Quaternion[] R2 = new Quaternion[ncams];
        Vector3[] C1 = new Vector3[ncams];
        Vector3[] C2 = new Vector3[ncams];
        int maxInl = 2;
        int minD = int.MaxValue;

        for(int i =0; i < ncams; i++)
        {
            R1[i] = photocaptureCameras[i].rotation;
            R2[i] = reconstructionCameras[i].rotation;
            C1[i] = Matrix4x4.Rotate(R1[i]).transpose * photocaptureCameras[i].position;
            C2[i] = Matrix4x4.Rotate(R2[i]).transpose * reconstructionCameras[i].position;
        }

        RansacItem result = new RansacItem();

        for(int i = 0; i < tests; i++)
        {
            int[] idx = new int[numOfPerm];
            System.Random rnd = new System.Random();
            for (int k = 0; k < numOfPerm; k++)
            {

                idx[k] = rnd.Next(0, ncams);
            }

            RansacItem r = EstSt(C1, C2, R1, R2, idx);
            while(r.numOfInl >= maxInl && getD(r.d, r.inl) < minD)
            {
                maxInl = r.numOfInl;
                r = EstSt(C1, C2, R1, R2, getInlIdx(r.inl, r.numOfInl));

                if(getD(r.d, r.inl) < minD)
                {
                    result = r;
                }
            }
            if(result.numOfInl == ncams)
            {
                break;
            }
        }
       
        return result;

    }

    private float getD(float[] d, bool[] inl)
    {
        float dist = 0;
        int cnt = inl.Length;
        for (int i = 0; i < cnt; i++)
            if (inl[i])
                dist += d[i];
        return dist;
    }

    private int[] getInlIdx(bool [] inl, int numOfInl)
    {
        int[] res = new int[numOfInl];
        int len = inl.Length, j = 0;
        for(int i = 0; i < len; i++)
            if (inl[i])
            {
                res[j] = i;
                j++;
            }

        return res;
    }

    private RansacItem EstSt(Vector3[] C1,Vector3[] C2,Quaternion[] R1,Quaternion[] R2,int[] idx)
    {

        RansacItem res = E3sFit2( C1, C2, R1, R2, idx);
        int cnt = C1.Length;
        Vector3[] C1Est = new Vector3[cnt];
        Matrix4x4 R = new Matrix4x4();
        for(int i = 0; i < cnt; i++)
        {
            for(int k = 0; k < 4; k++)
            {
                for(int l = 0; l < 4; l++)
                {
                    R[k, l] = res.s * res.R[k, l];
                }
            }

            C1Est[i] =  (R * C2[i]) ;
            C1Est[i] = C1Est[i] + res.t;
        }

        RansacItem inld = FindInliers(C1, C1Est);

        res.d = inld.d;
        res.inl = inld.inl;
        res.numOfInl = inld.numOfInl;
        

        return res;
    }



    private RansacItem E3sFit2(Vector3[] X1, Vector3[] X2, Quaternion[] R1, Quaternion[] R2, int[] idx)
    {
        int nop = X1.Length;
        int IdxCount = idx.Length;
        if(nop != X2.Length)
        {
            Debug.Log("Number of points does not match.");
            return null;
        }

        Vector3 c1 = new Vector3(0, 0, 0);
        Vector3 c2 = new Vector3(0, 0, 0);
        for(int i = 0; i < IdxCount; i++)
        {
            c1 += X1[idx[i]];
            c2 += X2[idx[i]];
        }

        c1 /= nop;
        c2 /= nop;

        Vector3[] Y1 = new Vector3[IdxCount];
        Vector3[] Y2 = new Vector3[IdxCount];
        Vector3 axis = new Vector3(0,0,0);
        float angle = 0;

        for (int i = 0; i < IdxCount; i++)
        {
            Y1[i] = X1[idx[i]] - c1;
            Y2[i] = X2[idx[i]] - c2;

            Matrix4x4 R21 = new Matrix4x4();
            R21 = Matrix4x4.Rotate(R2[idx[i]]) * (Matrix4x4.Rotate(R1[idx[i]]).transpose);
            Vector3 aa = new Vector3();
            float a;
            R21.rotation.ToAngleAxis(out a, out aa);
            axis += aa;
            angle += a;
        }

        axis /= nop;
        angle /= nop;

        Quaternion q = Quaternion.AngleAxis(angle, axis);
        Matrix4x4 R = Matrix4x4.Rotate(q);
        Vector3[] Y2R = new Vector3[IdxCount];
        float s = 0;

        for(int i = 0; i < IdxCount; i++)
        {
            Y2R[i] = R * Y2[i];
            s += Y2R[i].x / Y1[i].x;
            s += Y2R[i].y / Y1[i].y;
            s += Y2R[i].z / Y1[i].z;
            s /= 3;
        }
        s /= nop;
        Vector3 t = new Vector3();
        Vector3 c = c2 - c1;
        Vector4 r = R.GetColumn(0);
        t.x = s * (c.x * r.x + c.y * r.y + c.z * r.z);
        r = R.GetColumn(1);
        t.y = s * (c.x * r.x + c.y * r.y + c.z * r.z);
        r = R.GetColumn(2);
        t.z = s * (c.x * r.x + c.y * r.y + c.z * r.z);

        Matrix4x4 RN = new Matrix4x4();

        for(int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
                RN[i, j] = - R[i, j];
        }

        t = RN * t;

        RansacItem res = new RansacItem();
        res.R = R;
        res.s = s;
        res.t = t;

        return res;
    }

    private RansacItem FindInliers(Vector3[] C1,Vector3[] C1Est)
    { 
        int ncams = C1.Length; 
        int inlCount = 0;
        bool[] inl = new bool[ncams];
        float[] d = new float[ncams];

        float lim = 0;
        float x = 0;
        for(int i = 0; i < ncams; i++)
        {
            d[i] = (float) Math.Sqrt((C1[i].x - C1Est[i].x) * (C1[i].x - C1Est[i].x) + (C1[i].y - C1Est[i].y) * (C1[i].y - C1Est[i].y) + (C1[i].z - C1Est[i].z) * (C1[i].z - C1Est[i].z));
            x += (C1[i].x * C1[i].x + C1[i].y * C1[i].y + C1[i].z * C1[i].z);

        }

        x /= ncams;
        float sumx = 0;

        for(int i = 0; i < ncams; i++)
        {
            sumx +=(((C1[i].x * C1[i].x + C1[i].y * C1[i].y + C1[i].z * C1[i].z) - x) * ((C1[i].x * C1[i].x + C1[i].y * C1[i].y + C1[i].z * C1[i].z) - x));
        }

        lim = (float)Math.Sqrt(sumx / ncams);

        for (int i = 0; i < ncams; i++)
        {
            
            if(d[i] < lim)
            {
                inlCount++;
                inl[i] = true;
            }
            else
            {
                inl[i] = false;
            }
        }

        RansacItem res = new RansacItem();
        res.d = d;
        res.inl = inl;
        res.numOfInl = inlCount;

        return res;
    }

    private bool IsInlier(CameraItem source, CameraItem reconstruction)
    {
        float posLim = 0.01f;

        float distance = (float)Math.Sqrt((source.position.x - reconstruction.position.x) * (source.position.x - reconstruction.position.x) + (source.position.y - reconstruction.position.y) * (source.position.y - reconstruction.position.y) + (source.position.z - reconstruction.position.z) * (source.position.z - reconstruction.position.z));

        return distance < posLim;
    }



}

// Class to carry transformation results from ransac
public class RansacItem
{
    public bool[] inl;
    public int numOfInl;
    public float[] d;
    public Matrix4x4 R;
    public Vector3 t;
    public float s;

    public RansacItem(bool[] i, int numOfI, float[] dis, Matrix4x4 Rot, Vector3 Trans, float sc)
    {
        inl = i;
        numOfInl = numOfI;
        d = dis;
        R = Rot;
        t = Trans;
        s = sc;
    }

    public RansacItem()
    {
        inl = new bool[0];
        numOfInl = 0;
        d = new float[0];
        R = new Matrix4x4();
        t = new Vector3();
        s = 1;
    }
}



