using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.XR.WSA.WebCam;
using System.Linq;
using UnityEngine.Networking;
using System.Threading.Tasks;
//using Windows.Storage;
using System.Text;


//using UnityEngine.XR;

public class CameraCapture : MonoBehaviour {
    private Vector3 startHeadPosition;
    private Vector3 newHeadPosition;
    public double distance;
    public Text dIndikator;
    PhotoCapture photoCaptureObject = null;
    private string folderPath;

    Resolution m_cameraResolution;
    Matrix4x4 cameraToWorldMatrix;
    Matrix4x4 projectionMatrix;



    string BASEURL = "https://replicate.vision.ee.ethz.ch/api";
    string authName = "xxx";
    string authPassword = "xxx";

    private int sceneId = -1;

    // Use this for initialization
    void Start() {  //get starting head-position and new head-position

        startHeadPosition = Camera.main.transform.position;
        newHeadPosition = startHeadPosition;
        dIndikator.text = "";

       CreateScene("Test");

        Debug.Log("\n Taking picture started \n");
        
        TakePhoto();
        Debug.Log("\n Debug 2 \n");
        

       
    }

    void TakePhoto()
    {

        // Create a PhotoCapture object
        Debug.Log("\nCreating a PhotoCapture object\n");
        PhotoCapture.CreateAsync(false, delegate (PhotoCapture captureObject) {

            photoCaptureObject = captureObject;

            Debug.Log("\nSetting camera info\n");
            m_cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).Last();

            CameraParameters cameraParameters = new CameraParameters();
            cameraParameters.hologramOpacity = 0.0f;
            cameraParameters.cameraResolutionWidth = m_cameraResolution.width;
            cameraParameters.cameraResolutionHeight = m_cameraResolution.height;
            cameraParameters.pixelFormat = CapturePixelFormat.JPEG;

            // Activate the camera
            photoCaptureObject.StartPhotoModeAsync(cameraParameters, delegate (PhotoCapture.PhotoCaptureResult result) {
                // Take a picture
                Debug.Log("\nTaking the picture\n");
                photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
            });
        });
    }

    /*
    void OnPhotoCaptureCreated(PhotoCapture captureObject)
    {
        Debug.Log("\n Debug 1 \n");
        photoCaptureObject = captureObject;

        m_cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).Last();

        CameraParameters c = new CameraParameters();
        c.hologramOpacity = 0.0f;
        c.cameraResolutionWidth = m_cameraResolution.width;
        c.cameraResolutionHeight = m_cameraResolution.height;
        c.pixelFormat = CapturePixelFormat.JPEG;

        Debug.Log("\n PhotoModeStarted \n");

        captureObject.StartPhotoModeAsync(c, OnPhotoModeStarted);
    }*/

    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        Debug.Log("Stopped photo mode");
        photoCaptureObject.Dispose();
        photoCaptureObject = null;
    }

    private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
    {
        if (result.success)
        {
            Debug.Log("\n Image taken \n");
            photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
        }
        else
        {
            Debug.LogError("Unable to start photo mode!");
        }
    }

    async void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        if (result.success)
        {
            Debug.Log("\n Saving picture \n");
            List<byte> imageBufferList = new List<byte>();

            // Copy the raw IMFMediaBuffer data into our empty byte list.
            photoCaptureFrame.CopyRawImageDataIntoBuffer(imageBufferList);

            photoCaptureFrame.TryGetCameraToWorldMatrix(out cameraToWorldMatrix);
            photoCaptureFrame.TryGetProjectionMatrix(out projectionMatrix);

            if (sceneId > 0)
            {
                UploadImageToScene(imageBufferList.ToArray(), sceneId);
            }

        }
        // Clean up
        photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);    
    }



    // Update is called once per frame
    void Update() { // updating new head-position, if the distance between new head-position and starting position is same or larger than distance new starting head-position is the new head-position
        newHeadPosition = Camera.main.transform.position;
        double headPositionDistance = Math.Sqrt((newHeadPosition.x - startHeadPosition.x) * (newHeadPosition.x - startHeadPosition.x) + (newHeadPosition.y - startHeadPosition.y) * (newHeadPosition.y - startHeadPosition.y) + (newHeadPosition.z - startHeadPosition.z) * (newHeadPosition.z - startHeadPosition.z));
        if (headPositionDistance >= distance)
        {
            startHeadPosition = newHeadPosition;
            dIndikator.text = "You moved 30cm, taking picture";

            TakePhoto();

            if (sceneId > 0)
            {
                QueryReconstructions();
            }
        }
        else if (headPositionDistance >= distance / 10)
        {
            dIndikator.text = "";
        }


    }

    private string GetAuthorizationHash()
    {
        byte[] authBytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(authName + ":" + authPassword);
        return Convert.ToBase64String(authBytes);
    }

    //creates a scene
    void CreateScene(string description)
    {
        string url = BASEURL + "/scene";
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
        string url = BASEURL + "/images?sceneid=" + sceneID;

        // Construct Form data
        WWWForm form = new WWWForm();
        form.AddBinaryData("jpgdata", data);

        UnityWebRequest request = UnityWebRequest.Post(url, form);

        // Specify HTTP headers
        request.SetRequestHeader("Authorization", "Basic " + GetAuthorizationHash());
        //request.SetRequestHeader("Cache-Control", "no-cache");

        // Fire request
        UnityWebRequestAsyncOperation op = request.SendWebRequest();
        //op.completed += SceneCreated;
    }

    void QueryReconstructions()
    {
        string url = BASEURL + "/reconstruction?sceneid=" + sceneId;

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
            Debug.Log("Reconstruction available");
        }
    }


}