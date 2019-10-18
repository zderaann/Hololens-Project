using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.XR.WSA.WebCam;
using System.Linq;
using UnityEngine.Networking;
using System.Text;
using UnityEngine.XR.WSA.Input;
using System.IO;

using Windows.Media.Capture.Frames;
using Windows.Media.Capture;
using Windows.Graphics.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Perception.Spatial;
using System.Runtime.InteropServices;

public class CameraCapture : MonoBehaviour
{

    //Windows::Perception::Spatial::SpatialLocator::GetDefault();
    SpatialLocator locator;
    SpatialStationaryFrameOfReference originFrameOfReference;


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
    string filename;

    DrawCamera c = new DrawCamera();
    GestureRecognizer gr = null;

    List<CameraItem> captureCameras = new List<CameraItem>();

    CalculatedTransform cameraTransform;

    //   int reconstructionID = 0;

    // initialization
    void Start() {
        locator = SpatialLocator.GetDefault();
        originFrameOfReference = locator.CreateStationaryFrameOfReferenceAtCurrentLocation();
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
            //TakePhoto();  
            Debug.Log("Capture on");
            TakeImages();
            newHeadPosition = Camera.main.transform.position;
            startHeadPosition = newHeadPosition;
        }
        if (!reconstruction && !cameraCaptureOn && photoCount >= minPhoto /*&& sceneId > 0*/)
        {
            Debug.Log("Capture off, reconstruction query");
            reconstruction = true;
            // RunReconstruction();   TU
             //DownloadCameras(true);
            //GetTransformation();
        }
        else if (!cameraCaptureOn)
        {
            Debug.Log("Capture off");
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
                //TakePhoto();
                TakeImages();
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
        //string url = BASEURL + "/api/cv/run_reconstruction/"; CHANGED FOR RIG

        string url = BASEURL + "/api/cv/run_reconstruction_for_rig/";

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

                DownloadModel();
            }
        }
        catch (ArgumentException e)
        {
            Debug.Log("Something wrong with cameras and transform");
            Debug.Log("Please restart");
        }
    }


    void DownloadModel()
    {
        Debug.Log("Downloading model");

        string url = BASEURL + "/api/cv/get_textured_model/";

        UnityWebRequest request = UnityWebRequest.Get(url);


        UnityWebRequestAsyncOperation op = request.SendWebRequest();

        op.completed += HandleModelDownload;

    }

    //handler for http request
    void HandleModelDownload(AsyncOperation op)
    {
        UnityWebRequestAsyncOperation aop = (UnityWebRequestAsyncOperation)op;

        meshClass result = JsonUtility.FromJson<meshClass>(aop.webRequest.downloadHandler.text);
        result.initialize();

        Mesh mesh = new Mesh();
        // GetComponent<MeshFilter>().mesh = mesh; 

        /*Debug.Log(result.vertices[0][0]);
        Debug.Log(result.vertices[0][1]);
        Debug.Log(result.vertices[0][2]);*/
        mesh.vertices = result.vertices;
        //mesh.uv = newUV;
        /*Debug.Log(result.faces[0]);
        Debug.Log(result.faces[1]);
        Debug.Log(result.faces[2]);*/
        mesh.triangles = result.faces;

        mesh.uv = result.uv;

        // mesh.colors = result.colours;
        GameObject model = GameObject.Find("ReconstructionObject");
        MeshFilter mf = model.GetComponent<MeshFilter>();
        mesh.RecalculateNormals();
        mf.mesh = mesh;


        DownloadTexture();
        //Debug.Log(aop.webRequest.downloadHandler.text);
    }

    void DownloadTexture()
    {
        Debug.Log("Downloading texture");

        string url = BASEURL + "/api/cv/download_texture/";

        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);


        UnityWebRequestAsyncOperation op = request.SendWebRequest();

        op.completed += HandleTextureDownload;
    }

    void HandleTextureDownload(AsyncOperation op)
    {
        UnityWebRequestAsyncOperation aop = (UnityWebRequestAsyncOperation)op;

        Texture result = ((DownloadHandlerTexture)aop.webRequest.downloadHandler).texture;


        Renderer m_Renderer;
        GameObject model = GameObject.Find("ReconstructionObject");
        m_Renderer = model.GetComponent<MeshRenderer>();
        m_Renderer.material.SetTexture("_MainTex", result);


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
    void TakeImages()
    {
        Debug.Log("Taking picture");
        audioData.Play(0);
        photoCount++;
        filename = Time.time.ToString();

        Vector3 position = Camera.main.transform.position;
        Quaternion rotation = Camera.main.transform.rotation;
        Matrix4x4 rot = Matrix4x4.Rotate(rotation);

        rotation = Quaternion.LookRotation(-rot.GetColumn(2), rot.GetColumn(1));

        captureCameras.Add(new CameraItem(position, rotation));

        c.NewMesh(position, rotation);
        captureCameras[photoCount - 1].imageID = filename.Replace('.', '_');


        // Debug.Log("Rotation:" + Camera.main.transform.rotation.ToString());



        InitSensor(1, 2, 0);


        InitSensor(0, 4, 1);
        InitSensor(0, 5, 2);
        InitSensor(0, 6, 3);
        InitSensor(0, 7, 4);
    }

 

#if UNITY_UWP

    private async void InitSensor(int group, int sensor, int id)
    {
        var mediaFrameSourceGroupList = await MediaFrameSourceGroup.FindAllAsync();
        var mediaFrameSourceGroup = mediaFrameSourceGroupList[group];
        var mediaFrameSourceInfo = mediaFrameSourceGroup.SourceInfos[sensor];
        var mediaCapture = new MediaCapture();
        var settings = new MediaCaptureInitializationSettings()
        {
            SourceGroup = mediaFrameSourceGroup,
            SharingMode = MediaCaptureSharingMode.SharedReadOnly,
            StreamingCaptureMode = StreamingCaptureMode.Video,
            MemoryPreference = MediaCaptureMemoryPreference.Cpu,
            //PhotoCaptureSource = PhotoCaptureSource.Photo,
        };
        try
        {
            await mediaCapture.InitializeAsync(settings);


            var mediaFrameSource = mediaCapture.FrameSources[mediaFrameSourceInfo.Id];
            var mediaframereader = await mediaCapture.CreateFrameReaderAsync(mediaFrameSource, mediaFrameSource.CurrentFormat.Subtype);

            mediaframereader.FrameArrived += (sender, e) => FrameArrived(sender, e, id);
            await mediaframereader.StartAsync();
        }
        catch (Exception e)
        {
            UnityEngine.WSA.Application.InvokeOnAppThread(() => { Debug.Log(e); }, true);
        }
    }

    //https://mtaulty.com/2018/06/19/sketchy-experiments-with-hololens-facial-tracking-research-mode-rgb-streams-depth-streams/
    static Matrix4x4 ByteArrayToMatrix(byte[] bits)
    {
        Matrix4x4 matrix = Matrix4x4.identity;

        var handle = GCHandle.Alloc(bits, GCHandleType.Pinned);
        matrix = Marshal.PtrToStructure<Matrix4x4>(handle.AddrOfPinnedObject());
        handle.Free();

        return (matrix);
    }

    private void FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args, int sensor)
    {
        byte[] bytes = null;
        Texture2D tex = null;
        var mediaframereference = sender.TryAcquireLatestFrame();
        if (mediaframereference != null)
        {
            var videomediaframe = mediaframereference?.VideoMediaFrame;

            var frame_to_origin = mediaframereference?.CoordinateSystem.TryGetTransformTo(originFrameOfReference.CoordinateSystem);

            Guid MFSampleExtension_Spatial_CameraViewTransform = new Guid("4E251FA4-830F-4770-859A-4B8D99AA809B");

            Matrix4x4 cameraViewTransform;
            byte[] viewTransform = null;
            object value;

            if (mediaframereference.Properties.TryGetValue(MFSampleExtension_Spatial_CameraViewTransform, out value))
            {
                viewTransform = value as byte[];
                cameraViewTransform = ByteArrayToMatrix(viewTransform);
            }

            else
            {
                cameraViewTransform = Matrix4x4.zero;
            }


            var softwarebitmap = videomediaframe?.SoftwareBitmap;
            if (softwarebitmap != null)
            {
                int maxBitmapValue = 0;
                int w = softwarebitmap.PixelWidth;
                int h = softwarebitmap.PixelHeight;

                switch (softwarebitmap.BitmapPixelFormat)
                {
                    case BitmapPixelFormat.Gray16:
                        maxBitmapValue = 65535;
                        break;
                    case BitmapPixelFormat.Gray8:
                        maxBitmapValue = 255;
                        break;
                    case BitmapPixelFormat.Bgra8:
                        maxBitmapValue = 255;
                        /*if(sensor > 0)
                        {
                            w = w * 4;
                        }*/
                        break;
                }

                //BitmapBuffer bitmapBuffer = softwarebitmap.LockBuffer(BitmapBufferAccessMode.Read);


                SoftwareBitmap outputbitmap;
                if (sensor == 0)
                {
                    softwarebitmap = SoftwareBitmap.Convert(softwarebitmap, BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied);
                    outputbitmap = softwarebitmap;



                    if (bytes == null)
                    {
                        bytes = new byte[w * h * 4];
                    }
                    outputbitmap.CopyToBuffer(bytes.AsBuffer());
                    outputbitmap.Dispose();
                    softwarebitmap.Dispose();
                }
                else
                {
                    Debug.Log("1\n");
                    maxBitmapValue = 255;
                    int actualBitmapWidth = 4 * w;

                    string header = "P5\n" + actualBitmapWidth.ToString() + " " + h + "\n" + maxBitmapValue + "\n";
                    byte[] headerbytes = Encoding.ASCII.GetBytes(header);
                    byte[] imagebytes = new byte[actualBitmapWidth * h];
                    Debug.Log("2\n");
                    //BitmapBuffer bitmapBuffer = softwarebitmap.LockBuffer(Windows.Graphics.Imaging.BitmapBufferAccessMode.Read);

                    if (bytes == null)
                    {
                        bytes = new byte[actualBitmapWidth * h + headerbytes.Length];
                    }


                    Debug.Log("3\n");
                    softwarebitmap.CopyToBuffer(imagebytes.AsBuffer());
                    Debug.Log("4\n");
                    for (int i = 0; i < headerbytes.Length; i++)
                    {
                        bytes[i] = headerbytes[i];
                    }
                    Debug.Log("5\n");
                    for (int i = 0; i < imagebytes.Length; i++)
                    {
                        bytes[headerbytes.Length + i] = imagebytes[i];
                    }
                    Debug.Log("6\n");

                    // outputbitmap = new SoftwareBitmap(BitmapPixelFormat.Rgba8, w, h, BitmapAlphaMode.Premultiplied);

                    // LOOK INTO SENSORFRAME GETTING BITMAP FROM HOLOLENSFORCV SENSORRECORDER

                    /*softwarebitmap = SoftwareBitmap.Convert(softwarebitmap, BitmapPixelFormat.Gray8);
                    byte[] tmpbytes = new byte[w * h];

                    softwarebitmap.CopyToBuffer(tmpbytes.AsBuffer());
                   
                    if (bytes == null)
                    {
                        bytes = new byte[w * h * 4];
                    }

                    int j = 0;
                    for (int i = 0; i < w * h * 4; i+=4)
                    {
                        
                        bytes[i] = tmpbytes[j];
                        bytes[i+1] = tmpbytes[j];
                        bytes[i+2] = tmpbytes[j];
                        bytes[i+3] = 255;
                        j++;
                    }*/


                }


                
                UnityEngine.WSA.Application.InvokeOnAppThread(() => {
                    if (tex == null)
                    {
                        if (sensor == 0)
                        {
                            tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                        }
                        /*else
                        {
                            tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                        }*/
                        
                    }


                    
                    if (sensor == 0)
                    {
                        tex.LoadRawTextureData(bytes);
                        UploadImage(ImageConversion.EncodeToJPG(tex, 100), sensor, frame_to_origin, cameraViewTransform);
                        
                    }
                    else
                    {
                        UploadImage(bytes, sensor, frame_to_origin, cameraViewTransform);
                    }

                }, true);
            }

            mediaframereference.Dispose();
            sender.StopAsync();
        }



        //tady to ukoncit
    }
#endif

    string MatrixToString(Matrix4x4 mat)
    {
        string res = "";
        for(int i = 0; i < 4; i++)
            for(int j = 0; j < 4; j++)
            {
                res = res + mat[i,j].ToString();
                if(!(i == 4 && j == 4))
                {
                    res = res + ",";
                }
            }
        return res;
    }

    string MatrixToString(System.Numerics.Matrix4x4 mat)
    {
        string res = mat.M11.ToString() + "," + mat.M12.ToString() + "," + mat.M13.ToString() + "," + mat.M14.ToString() + "," +
                     mat.M21.ToString() + "," + mat.M22.ToString() + "," + mat.M23.ToString() + "," + mat.M24.ToString() + "," +
                     mat.M31.ToString() + "," + mat.M32.ToString() + "," + mat.M33.ToString() + "," + mat.M34.ToString() + "," +
                     mat.M41.ToString() + "," + mat.M42.ToString() + "," + mat.M43.ToString() + "," + mat.M44.ToString();
        return res;
    }

    void UploadImage(byte[] bytes, int sensor, System.Numerics.Matrix4x4? frame_to_origin, Matrix4x4 cameraViewTransform)
    {

        Debug.Log("Image taken");
        string url = BASEURL + "/api/cv/save_sensor_data/";
        //string url = BASEURL + "/api/cv/register_images/";
        System.Numerics.Matrix4x4 frameToOrigin = (System.Numerics.Matrix4x4)frame_to_origin;

        WWWForm form = new WWWForm();
        form.AddField("sensor", sensor.ToString());
        form.AddField("name", filename);
        if (sensor == 0)
            form.AddField("format", "jpg");
        else
            form.AddField("format", "pgm");

        form.AddField("cameraViewTransform", MatrixToString(cameraViewTransform));
        form.AddField("frameToOrigin", MatrixToString(frameToOrigin));
        form.AddBinaryData("jpgdata", bytes);

        //form.AddBinaryData("jpgdata", ImageConversion.EncodeToJPG(tex, 75).ToArray());
        // Construct Form data



        UnityWebRequest request = UnityWebRequest.Post(url, form);


        // Fire request
        UnityWebRequestAsyncOperation op = request.SendWebRequest();
        op.completed += ImageUploadHandle;
    }

    //handler for http request
    void ImageUploadHandle(AsyncOperation op)
    {
        UnityWebRequestAsyncOperation uop = (UnityWebRequestAsyncOperation)op;
    }

}
