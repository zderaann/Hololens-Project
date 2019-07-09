using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.XR.WSA.WebCam;
using System.Linq;
using UnityEngine.Networking;
using System.Text;
using UnityEngine.XR.WSA.Input;
using System.IO;


public class CameraCapture : MonoBehaviour
{
    int minPhoto = 5;
    bool debug = true;
    bool holograms = false;

    string folder;

    private Vector3 startHeadPosition;
    private Vector3 newHeadPosition;
    private double distance = 0.3;

    string quality = "medium";

    PhotoCapture photoCaptureObject = null;

    AudioSource audioData;

    public TextMesh textMesh;
    private int photoCount = 0;

    private bool cameraCaptureOn = false;
    private bool reconstruction = false;


    Resolution m_cameraResolution;
    Matrix4x4 cameraToWorldMatrix;
    Matrix4x4 projectionMatrix;
    
    public GameObject reconstructionObject;

    string BASEURL = "http://10.35.100.210:9099";
    //string BASEURL = "http://10.37.1.210:9099";
    //string BASEURL = "http://requestbin.fullcontact.com/1h9vub91";
    string authName = "xxx";
    string authPassword = "xxx";

    DrawCamera c = new DrawCamera();
    GestureRecognizer gr = null;

    List<CameraItem> captureCameras = new List<CameraItem>();

    CalculatedTransform cameraTransform;

    //   int reconstructionID = 0;

    // initialization
    void Start() {  
        audioData = GetComponent<AudioSource>();
        startHeadPosition = Camera.main.transform.position;
        newHeadPosition = startHeadPosition;
        textMesh.text = photoCount.ToString();

        Debug.Log("Airtap to start/stop capture");

        //CreateScene("Test");

        CreateFolder();

        gr = new GestureRecognizer();
        gr.TappedEvent += Tap;
        gr.StartCapturingGestures();
    }

    //waiting for tap to start/stop taking pictures
    void Tap(InteractionSourceKind source, int tapCount, Ray headRay)
    {
        //Debug.Log("tap");
        cameraCaptureOn = !cameraCaptureOn;
        if (cameraCaptureOn)
        {
            reconstruction = false;
            TakePhoto();
            newHeadPosition = Camera.main.transform.position;
            startHeadPosition = newHeadPosition;
        }
        if (!reconstruction && !cameraCaptureOn && photoCount >= minPhoto /*&& sceneId > 0*/)
        {
            Debug.Log("Capture off, reconstruction query");
            reconstruction = true;
             RunReconstruction();
             //DownloadCameras(true);
            //GetTransformation();
        }
    }



    // Updating the position of the head, if photocapture is on, takes picture every *distance*cm
    void Update()
    {
        newHeadPosition = Camera.main.transform.position;
        double headPositionDistance = Math.Sqrt((newHeadPosition.x - startHeadPosition.x) * (newHeadPosition.x - startHeadPosition.x) + (newHeadPosition.y - startHeadPosition.y) * (newHeadPosition.y - startHeadPosition.y) + (newHeadPosition.z - startHeadPosition.z) * (newHeadPosition.z - startHeadPosition.z));
        textMesh.text = photoCount.ToString();

        if (headPositionDistance >= distance)
        {
            startHeadPosition = newHeadPosition;
            if (cameraCaptureOn)
            {
                TakePhoto();
            }
        }

    }

    
    void TakePhoto()
    {
        Vector3 position = new Vector3(newHeadPosition.x, newHeadPosition.y, newHeadPosition.z);
       // Debug.Log("New head position: " + newHeadPosition.ToString());
        Debug.Log("Taking picture");
       // Debug.Log("Rotation:" + Camera.main.transform.rotation.ToString());
        audioData.Play(0);
        photoCount++;

        // Create a PhotoCapture object
        PhotoCapture.CreateAsync(holograms, delegate (PhotoCapture captureObject) {

            photoCaptureObject = captureObject;

            
            m_cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).Last();

            CameraParameters cameraParameters = new CameraParameters();
            cameraParameters.hologramOpacity = 0.0f;
            cameraParameters.cameraResolutionWidth = m_cameraResolution.width;
            cameraParameters.cameraResolutionHeight = m_cameraResolution.height;
            cameraParameters.pixelFormat = CapturePixelFormat.JPEG;

            // Activate the camera
            photoCaptureObject.StartPhotoModeAsync(cameraParameters, delegate (PhotoCapture.PhotoCaptureResult result) {
                // Take a picture
                photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
            });
        });
    }


    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        //Debug.Log("Stopped photo mode");
        photoCaptureObject.Dispose();
        photoCaptureObject = null;
    }

    private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
    {
        if (result.success)
        {
            //Debug.Log("\n Image taken \n");
            photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
        }
        else
        {
            Debug.LogError("Unable to start photo mode!");
        }
    }

    //Get the image, pose of camera 
    async void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        if (result.success)
        {
            //Debug.Log("\n Saving picture \n");
            List<byte> imageBufferList = new List<byte>();

            // Copy the raw IMFMediaBuffer data into our empty byte list.
            photoCaptureFrame.CopyRawImageDataIntoBuffer(imageBufferList);

            photoCaptureFrame.TryGetCameraToWorldMatrix(out cameraToWorldMatrix);
            photoCaptureFrame.TryGetProjectionMatrix(out projectionMatrix);

            Vector3 position = cameraToWorldMatrix.MultiplyPoint(Vector3.zero);
            Quaternion rotation = Quaternion.LookRotation(cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));


            captureCameras.Add(new CameraItem(position, rotation));
            
            c.NewMesh( position, rotation);

            UploadImage(imageBufferList.ToArray());

            /*if ( sceneId > 0)
            {
               UploadImageToScene(imageBufferList.ToArray(), sceneId);
            }*/

        }
        // Clean up
        photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
    }


    //authorization for replicate server
    private string GetAuthorizationHash()
    {
        byte[] authBytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(authName + ":" + authPassword);
        return Convert.ToBase64String(authBytes);
    }

    //uploading image to server
    void UploadImage(byte[] data)
    {
        Debug.Log("Image taken");
        string url = BASEURL + "/api/cv/save_images/";

        // Construct Form data
        WWWForm form = new WWWForm();
        form.AddBinaryData("jpgdata", data);

        UnityWebRequest request = UnityWebRequest.Post(url, form);

        // Specify HTTP headers
        //request.SetRequestHeader("Authorization", "Basic " + GetAuthorizationHash());
        //request.SetRequestHeader("Cache-Control", "no-cache");

        // Fire request
        UnityWebRequestAsyncOperation op = request.SendWebRequest();
        op.completed += ImageUploaded;
    }

    //handler for http request
    void ImageUploaded(AsyncOperation op)
    {
        UnityWebRequestAsyncOperation uop = (UnityWebRequestAsyncOperation)op;
        CreatedFolderItem result = JsonUtility.FromJson<CreatedFolderItem>(uop.webRequest.downloadHandler.text);
        captureCameras[photoCount - 1].imageID = result.file;
    }


    //creating folder at the server to save the images in
    void CreateFolder()
    {
        string url = BASEURL + "/api/cv/create_folder/";

        UnityWebRequest request = UnityWebRequest.Put(url, JsonUtility.ToJson(new CreatedFolderItem("Test")));
        request.method = "POST";

        // Specify HTTP headers
        request.SetRequestHeader("Authorization", "Basic " + GetAuthorizationHash());
        request.SetRequestHeader("Content-Type", "application/json");

        // Fire request
        UnityWebRequestAsyncOperation op = request.SendWebRequest();
        op.completed += FolderCreated;
    }

    //handler for http request
    void FolderCreated(AsyncOperation op)
    {
        UnityWebRequestAsyncOperation uop = (UnityWebRequestAsyncOperation)op;
        CreatedFolderItem result = JsonUtility.FromJson<CreatedFolderItem>(uop.webRequest.downloadHandler.text);
        folder = result.folder;
        Debug.Log("Data folder: " + folder);
    }

    //makes a request for the server to run COLMAP
    void RunReconstruction()
    {
        Debug.Log("Running reconstruction");
        string url = BASEURL + "/api/cv/run_reconstruction/";

        string json = JsonUtility.ToJson(new CreatedFolderItem(quality));

       // Debug.Log(json);

        UnityWebRequest request = UnityWebRequest.Put(url, json);
        request.method = "POST";

        // Specify HTTP headers
        request.SetRequestHeader("Content-Type", "application/json");

        UnityWebRequestAsyncOperation op = request.SendWebRequest();

        op.completed += HandleReconstructionRun;

    }

    //handler for http request
    void HandleReconstructionRun(AsyncOperation op)
    {
        UnityWebRequestAsyncOperation aop = (UnityWebRequestAsyncOperation)op;
        //WriteCaptureCamsToFile();
        GetTransformation();

        //DownloadCameras();
        //GetTransformation();
    }

    //Downloads reconstruction from the server
    void DownloadReconstruction()
    {
        string url = BASEURL + "/api/cv/download_model/";

        UnityWebRequest request = UnityWebRequest.Get(url);


        UnityWebRequestAsyncOperation op = request.SendWebRequest();

        op.completed += HandleReconstructionDownload;

    }

    //handler for http request, draws reconstruction
    void HandleReconstructionDownload(AsyncOperation op)
    {
        UnityWebRequestAsyncOperation aop = (UnityWebRequestAsyncOperation)op;
        try
        {
            if (aop.webRequest.downloadHandler.data.Length == 0)
            {
                Debug.Log("Empty file");
            }
            else
            {

                Debug.Log("Reconstruction dowloaded");
                //Debug.Log(aop.webRequest.downloadHandler.text);

                meshClass result = JsonUtility.FromJson<meshClass>(aop.webRequest.downloadHandler.text);
                result.initialize();

                GameObject model = GameObject.Find("ReconstructionObject");
                MeshFilter mf = model.GetComponent<MeshFilter>();
                mf.mesh.Clear();

                mf.mesh.vertices = result.vertices;
                mf.mesh.triangles = result.faces;
                mf.mesh.colors = result.colours;

                //mf.mesh = mesh;

                Debug.Log("Mesh drawn");

                /*Matrix4x4 modrot = Matrix4x4.Rotate(model.transform.localRotation);
                Matrix4x4 rot = modrot * cameraTransform.rot;
                model.transform.localRotation = QuaternionFromMatrix(rot);
                model.transform.localScale *= cameraTransform.scale;
                mf.mesh.RecalculateNormals();

                model.transform.localPosition = model.transform.localPosition + cameraTransform.trans;*/
                // 
                // model.transform.Translate(cameraTransform.trans);



            }
        }
        catch(ArgumentException e)
        {
            Debug.Log("Something wrong with the reconstruction");
            Debug.Log("Please restart");
        }
    }

    public Quaternion QuaternionFromMatrix(Matrix4x4 m)
    {
        // Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
        Quaternion q = new Quaternion();
        q.w = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] + m[1, 1] + m[2, 2])) / 2;
        q.x = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] - m[1, 1] - m[2, 2])) / 2;
        q.y = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] + m[1, 1] - m[2, 2])) / 2;
        q.z = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] - m[1, 1] + m[2, 2])) / 2;
        q.x *= Mathf.Sign(q.x * (m[2, 1] - m[1, 2]));
        q.y *= Mathf.Sign(q.y * (m[0, 2] - m[2, 0]));
        q.z *= Mathf.Sign(q.z * (m[1, 0] - m[0, 1]));
        return q;
    }

    //Downloads camera poses from COLMAP
    void DownloadCameras()
    {
        Debug.Log("Downloading cameras");
        string url = BASEURL + "/api/cv/query_cameras/";

        UnityWebRequest request = UnityWebRequest.Get(url);


        UnityWebRequestAsyncOperation op = request.SendWebRequest();

        op.completed += HandleCameraDownload;

    }

    //handler for http request, draws cameras
    void HandleCameraDownload(AsyncOperation op)
    {
        UnityWebRequestAsyncOperation aop = (UnityWebRequestAsyncOperation)op;
        try
        {

            string result = aop.webRequest.downloadHandler.text;

            if (result.Length == 0)
            {
                Debug.Log("Empty file");
            }
            else
            {

                Debug.Log("Cameras dowloaded");

                // WriteCaptureCamsToFile();

                List<CameraItem> reconstructionCams = new CameraItem().ParseCameras(result, photoCount);
                if (reconstructionCams != null)
                {
                    /* reconstructionCams = reconstructionCams.OrderBy(o => o.imageID).ToList();

                     for (int i = 0; i < photoCount; i++)
                     {

                         captureCameras[i].imageID = reconstructionCams[i].imageID;
                     }*/

                    WriteCaptureCamsToFile();

                    ProcessReconstructionView p = new ProcessReconstructionView();
                    p.DrawReconstructionView(reconstructionCams, captureCameras, cameraTransform);



                    DownloadReconstruction();


                }


            }
        }
        catch(ArgumentException e)
        {
            Debug.Log("Something wrong with cameras");
            Debug.Log("Please restart");
        }

    }


    //makes a request for the server to run COLMAP
    void GetTransformation() //TODO return transformation
    {

        string url = BASEURL + "/api/cv/get_transformation/";

        string json = JsonUtility.ToJson(new AllCamerasItem(photoCount, captureCameras));

        //Debug.Log("\n" + json + "\n");

   

        UnityWebRequest request = UnityWebRequest.Put(url, json);
        request.method = "POST";

        // Specify HTTP headers
        request.SetRequestHeader("Content-Type", "application/json");



        UnityWebRequestAsyncOperation op = request.SendWebRequest();

        op.completed += HandleGetTransformation;

    }

    //handler for http request
    void HandleGetTransformation(AsyncOperation op)
    {
        Debug.Log("Downloading cameras");
        UnityWebRequestAsyncOperation aop = (UnityWebRequestAsyncOperation)op;
        try
        {
            if (aop.webRequest.downloadHandler.text.Length == 0)
            {
                Debug.Log("Something wrong with cameras and transform");
            }
            else
            {
                CalculatedTransform result = JsonUtility.FromJson<CalculatedTransform>(aop.webRequest.downloadHandler.text);
                result.initialize();
                //Debug.Log(aop.webRequest.downloadHandler.text);

                //DownloadCameras();

                WriteCaptureCamsToFile();
                Debug.Log("Transformation and cameras dowloaded");
                 ProcessReconstructionView p = new ProcessReconstructionView();
                //p.DrawCameras(result.cameras, result.camrotation, result.rot, captureCameras);
                p.DrawTransformedColmapCams(result.XHx, result.XHy, result.XHz, result.cameras.Count/3, result.cameras);

                 Debug.Log("Cameras drawn");

                cameraTransform = result;

                DownloadReconstruction();
            }
        }
        catch (ArgumentException e)
        {
            Debug.Log("Something wrong with cameras and transform");
            Debug.Log("Please restart");
        }
    }



    void WriteCaptureCamsToFile()
{
  string path = Path.Combine(Application.persistentDataPath, "CaptureCameras.txt");

  //Debug.Log("Saving to " + path);

  using (TextWriter writer = File.CreateText(path))
  {
      for (int i = 0; i < captureCameras.Count; i++)
      {
          writer.WriteLine(captureCameras[i].position.x + " " + captureCameras[i].position.y + " " + captureCameras[i].position.z + " " + captureCameras[i].rotation.w + " " + captureCameras[i].rotation.x + " " + captureCameras[i].rotation.y + " " + captureCameras[i].rotation.z + " " +  captureCameras[i].imageID);
      }
  }

}

}
