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

    PhotoCapture photoCaptureObject = null;

    AudioSource audioData;

    public TextMesh textMesh;
    private int photoCount = 0;

    private bool cameraCaptureOn = false;
    private bool reconstruction = false;

    RansacItem transform;


    Resolution m_cameraResolution;
    Matrix4x4 cameraToWorldMatrix;
    Matrix4x4 projectionMatrix;
    
    public GameObject reconstructionObject;

    string BASEURL = "http://10.35.100.210:9099";
    string authName = "xxx";
    string authPassword = "xxx";

    DrawCamera c = new DrawCamera();
    GestureRecognizer gr = null;

    List<CameraItem> captureCameras = new List<CameraItem>();

 //   int reconstructionID = 0;

    // initialization
    void Start() {  
        audioData = GetComponent<AudioSource>();
        startHeadPosition = Camera.main.transform.position;
        newHeadPosition = startHeadPosition;
        textMesh.text = photoCount.ToString();

        Debug.Log("Start");

        //CreateScene("Test");

        CreateFolder();

        gr = new GestureRecognizer();
        gr.TappedEvent += Tap;
        gr.StartCapturingGestures();
    }

    //waiting for tap to start/stop taking pictures
    void Tap(InteractionSourceKind source, int tapCount, Ray headRay)
    {
        Debug.Log("tap");
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
            Debug.Log("Camera capture off, reconstruction query");
            reconstruction = true;
            RunReconstruction();
           // DownloadCameras(true);
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
        Debug.Log("Taking picture at: " + position.ToString());
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
            Quaternion rotation = Quaternion.LookRotation(-cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));


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
        Debug.Log("Sending image");
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
        Debug.Log("Created folder " + folder);
    }

    //makes a request for the server to run COLMAP
    void RunReconstruction()
    {

        string url = BASEURL + "/api/cv/run_reconstruction/";

        UnityWebRequest request = UnityWebRequest.Post(url, JsonUtility.ToJson(new CreatedFolderItem("Test")));


        UnityWebRequestAsyncOperation op = request.SendWebRequest();

        op.completed += HandleReconstructionRun;

    }

    //handler for http request
    void HandleReconstructionRun(AsyncOperation op)
    {
        UnityWebRequestAsyncOperation aop = (UnityWebRequestAsyncOperation)op;
        DownloadCameras();
    }

    //Downloads reconstruction from the server
    void DownloadReconstruction()
    {
        string url = BASEURL + "/api/cv/query_reconstruction/";

        UnityWebRequest request = UnityWebRequest.Get(url);


        UnityWebRequestAsyncOperation op = request.SendWebRequest();

        op.completed += HandleReconstructionDownload;

    }

    //handler for http request, draws reconstruction
    void HandleReconstructionDownload(AsyncOperation op)
    {
        UnityWebRequestAsyncOperation aop = (UnityWebRequestAsyncOperation)op;

        byte[] result = aop.webRequest.downloadHandler.data;

        if (result.Length == 0)
        {
            Debug.Log("Empty file");
        }
        else
        {
  
           Debug.Log("Reconstruction Dowloaded");

           ReconstructionVisualizer rv = new ReconstructionVisualizer();
           rv.DrawMesh(result, transform);
            holograms = true;
           
            
        }
    }

    //Downloads camera poses from COLMAP
    void DownloadCameras()
    {
        string url = BASEURL + "/api/cv/query_cameras/";

        UnityWebRequest request = UnityWebRequest.Get(url);


        UnityWebRequestAsyncOperation op = request.SendWebRequest();

        op.completed += HandleCameraDownload;

    }

    //handler for http request, draws cameras
    void HandleCameraDownload(AsyncOperation op)
    {
        UnityWebRequestAsyncOperation aop = (UnityWebRequestAsyncOperation)op;

        string result = aop.webRequest.downloadHandler.text;

        if (result.Length == 0)
        {
            Debug.Log("Empty file");
        }
        else
        {

            Debug.Log("Cameras Dowloaded");

           // WriteCaptureCamsToFile();

            List<CameraItem> reconstructionCams = new CameraItem().ParseCameras(result, photoCount);
            if(reconstructionCams != null)
            {
                reconstructionCams = reconstructionCams.OrderBy(o => o.imageID).ToList();

                for (int i = 0; i < photoCount; i++)
                {

                    captureCameras[i].imageID = reconstructionCams[i].imageID;
                }

                ProcessReconstructionView p = new ProcessReconstructionView();
                transform =  p.DrawReconstructionView(reconstructionCams, captureCameras);



                DownloadReconstruction();
                

            }


        }
    }



    //creates a scene
    /*void CreateScene(string description)
    {
        string url = BASEURL + "/api/scene";
        SceneItem scene = new SceneItem(description);

        UnityWebRequest request = UnityWebRequest.Put(url, JsonUtility.ToJson(scene));
        request.method = "POST";

        // Specify HTTP headers
        request.SetRequestHeader("Authorization", "Basic " + GetAuthorizationHash());
        request.SetRequestHeader("Content-Type", "application/json");

        // Fire request
        UnityWebRequestAsyncOperation op = request.SendWebRequest();
        op.completed += SceneCreated;
    }

    void SceneCreated(AsyncOperation op)
    {
        UnityWebRequestAsyncOperation uop = (UnityWebRequestAsyncOperation)op;
        CreatedSceneItem result = JsonUtility.FromJson<CreatedSceneItem>(uop.webRequest.downloadHandler.text);
        sceneId = result.id;
    }

    void UploadImageToScene(byte[] data, int sceneID)
    {
        Debug.Log("Sending image");
        string url = BASEURL + "/api/images?sceneid=" + sceneID;

        // Construct Form data
        WWWForm form = new WWWForm();
        form.AddBinaryData("jpgdata", data);

        UnityWebRequest request = UnityWebRequest.Post(url, form);

        // Specify HTTP headers
        request.SetRequestHeader("Authorization", "Basic " + GetAuthorizationHash());
        //request.SetRequestHeader("Cache-Control", "no-cache");

        // Fire request
        UnityWebRequestAsyncOperation op = request.SendWebRequest();

    }*/


    /* void QueryReconstructions()
     {

         string url = BASEURL + "/api/reconstruction?sceneid=" + sceneId;

         UnityWebRequest request = UnityWebRequest.Get(url);

         request.SetRequestHeader("Authorization", "Basic " + GetAuthorizationHash());

         UnityWebRequestAsyncOperation op = request.SendWebRequest();

         op.completed += HandleReconstructionQuery; 

     }

     void HandleReconstructionQuery(AsyncOperation op)
     {
         UnityWebRequestAsyncOperation aop = (UnityWebRequestAsyncOperation)op;
         ReconstructionQueryItem result = JsonUtility.FromJson<ReconstructionQueryItem>(aop.webRequest.downloadHandler.text);

         if (result.reconstructions.Length == 0)
         {
             Debug.Log("No reconstruction available");
         }
         else
         {
             if(debug)
             Debug.Log("Reconstruction available");

            // Debug.Log("\nStarting reconstruction download\n");

             DownloadReconstruction(result.reconstructions[result.reconstructions.Length - 1].url);

             reconstructionID = result.reconstructions[result.reconstructions.Length - 1].id;

         }
     }

     void DownloadReconstruction(string reconstructionURL)
     {
         string url = BASEURL + reconstructionURL + ".ply";

         UnityWebRequest request = UnityWebRequest.Get(url);

         request.SetRequestHeader("Authorization", "Basic " + GetAuthorizationHash());

         UnityWebRequestAsyncOperation op = request.SendWebRequest();

         op.completed += HandleReconstructionDownload;

     }

     void HandleReconstructionDownload(AsyncOperation op)
     {
         UnityWebRequestAsyncOperation aop = (UnityWebRequestAsyncOperation)op;

         string resultText = aop.webRequest.downloadHandler.text;

         if (resultText.Length == 0)
         {
             Debug.Log("Empty file");
         }
         else
         {
             if(debug)
             Debug.Log("Reconstruction Dowloaded");

             ReconstructionVisualizer rv = new ReconstructionVisualizer();

             if (rv.DrawMesh(resultText))
             {

                 DownloadCameras(true);
             }

         }
     }

     void DownloadCameras(bool visualize)
     {
         if(debug)
         Debug.Log("Downloading reconstruction cameras");


         string url = BASEURL + "/api/recview?recid=" + reconstructionID;

         UnityWebRequest request = UnityWebRequest.Get(url);

         request.SetRequestHeader("Authorization", "Basic " + GetAuthorizationHash());

         UnityWebRequestAsyncOperation op = request.SendWebRequest();

         if (visualize)
         {
             op.completed += HandleDownloadCameras;
         }


     }

     void HandleDownloadCameras(AsyncOperation op)
     {
         UnityWebRequestAsyncOperation aop = (UnityWebRequestAsyncOperation)op;
         ReconstructionViewItem result = JsonUtility.FromJson<ReconstructionViewItem>(aop.webRequest.downloadHandler.text);

         if (result.views.Length == 0)
         {
             Debug.Log("No cameras available");
         }
         else
         {
             if (result.views.Length == captureCameras.Count)
             {
                 Debug.Log("Cameras available");
                 debug = true;

                 WriteReconstructionCamsToFile(result);

                 List<CameraItem> reconstructionCameras = new List<CameraItem>();

                 ProcessReconstructionView p = new ProcessReconstructionView();


                 reconstructionCameras = p.ConvertToCameraItem(result);

                 reconstructionCameras = reconstructionCameras.OrderBy(o => o.imageID).ToList();




                 int camerasCount = reconstructionCameras.Count;

                 for (int i = 0; i < camerasCount; i++)
                 {
                     if (!reconstructionCameras[i].estimated)
                     {
                         reconstructionCameras[i].imageID = -1;
                     }
                     captureCameras[i].imageID = reconstructionCameras[i].imageID;
                 }

                 CameraItem transform = p.DrawReconstructionView(reconstructionCameras, captureCameras);

                 WriteCaptureCamsToFile();

                 /*ReconstructionVisualizer rv = new ReconstructionVisualizer();
                 rv.TransformMesh(transform);*/
    /*  }
      else
      {
          Debug.Log("Waiting for full list of cameras");
          debug = false;
          QueryReconstructions();
      }


  }
}
*/
void WriteCaptureCamsToFile()
{
  string path = Path.Combine(Application.persistentDataPath, "CaptureCameras.txt");

  Debug.Log("Saving to " + path);

  using (TextWriter writer = File.CreateText(path))
  {
      for (int i = 0; i < captureCameras.Count; i++)
      {
          writer.WriteLine(captureCameras[i].position.x + " " + captureCameras[i].position.y + " " + captureCameras[i].position.z + " " + captureCameras[i].rotation.w + " " + captureCameras[i].rotation.x + " " + captureCameras[i].rotation.y + " " + captureCameras[i].rotation.z + " " +  captureCameras[i].imageID);
      }
  }

}
/*
void WriteReconstructionCamsToFile(ReconstructionViewItem result)
{
  string path = Path.Combine(Application.persistentDataPath, "ReconstructionCameras.txt");
  using (TextWriter writer = File.CreateText(path))
  {
      for (int i = 0; i < result.views.Count(); i++)
      {
          writer.WriteLine(result.views[i].position[0] + " " + result.views[i].position[1] + " " + result.views[i].position[2] + " " + result.views[i].rotation[0] + " " + result.views[i].rotation[1] + " " + result.views[i].rotation[2] + " "  + result.views[i].imageid);
      }
  }
}
*/
}
