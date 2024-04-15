using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using UnityVolumeRendering;
using CandyCoded.env;
using UnityEngine.UI;

[Serializable]
public class Patients
{
    public int id;
    public string patientID;
    public string name;
    public string birthDate;
}

[Serializable]
public class Studies
{
    public int id;
    public int patientID;
    public string instanceUID;
    public string description;
    public string time;
}

[Serializable]
public class Series
{
    public int id;
    public int studyID;
    public string instanceUID;
    public string filepath;
    public string bitspath;
    public string description;
}

[Serializable]
public class Instances
{
    public int id;
    public int seriesID;
    public string filename;
}

public class BypassCertificate : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        // Always returns true, indicating that the certificate is valid
        return true;
    }
}

public class PythonAPI : MonoBehaviour
{
    public List<Patients> patients;
    public List<Studies> studies;
    public List<Series> series;

    private string baseURL;

    private GameObject currentObj = null;

    private Button[] seriesBtns = null;
    private Button patientsBtn = null;
    private Button studiesBtn = null;

    private Text loadingTxt = null;

    private void Awake()
    {
        if (env.TryParseEnvironmentVariable("BASE_URL", out string url))
        {
            baseURL = url;
        }
    }

    private void Start()
    {
        //patientsBtn = GameObject.Find("Canvas")
        //    .transform.Find("PatientsBtn").GetComponent<Button>();

        //studiesBtn = GameObject.Find("Canvas")
        //    .transform.Find("StudiesBtn").GetComponent<Button>();

        //loadingTxt = GameObject.Find("Canvas")
        //    .transform.Find("LoadingTxt").GetComponent<Text>();
    }

    public IEnumerator Get(string uri, string type)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(uri))
        {
            request.certificateHandler = new BypassCertificate();
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log("Error on get data: " + request.error);
            }
            else
            {
                string json = request.downloadHandler.text;
                Debug.Log(json);

                if (type == "patients")
                {
                    patients = JsonConvert.DeserializeObject<List<Patients>>(json);
                }
                else if (type == "studies")
                {
                    studies = JsonConvert.DeserializeObject<List<Studies>>(json);
                }
                else
                {
                    series = JsonConvert.DeserializeObject<List<Series>>(json);
                }
                
                Debug.Log("Data successfully retrieved!");
            }
        }
    }

    public IEnumerator GetData(Series series)
    {
        //seriesBtns = GameObject.Find("Canvas")
        //    .transform.Find("SeriesTable")
        //    .transform.Find("SeriesEntryContainer").GetComponentsInChildren<Button>();

        //DisableBtns(seriesBtns);

        if (currentObj != null) Destroy(currentObj);

        string path = series.filepath;
        string bitspath = series.bitspath;

        string uri = baseURL + "dicom/3d?path=" + path + "&bitspath=" + bitspath;

        using (UnityWebRequest request = UnityWebRequest.Get(uri))
        {
            request.certificateHandler = new BypassCertificate();
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log("Error on get data: " + request.error);
            }
            else
            {
                string json = request.downloadHandler.text;

                VolumeDataset dataset = JsonConvert.DeserializeObject<VolumeDataset>(json);

                dataset.FixDimensions();

                if (bitspath == null) bitspath = dataset.bitspath;

                using (UnityWebRequest request2 = UnityWebRequest.Get(baseURL + bitspath))
                {
                    request2.certificateHandler = new BypassCertificate();
                    yield return request2.SendWebRequest();

                    if (request2.result == UnityWebRequest.Result.ConnectionError)
                    {
                        Debug.Log("Error on data download: " + request2.error);
                    }
                    else
                    {
                        dataset.jdlskald = request2.downloadHandler.data;

                        VolumeRenderedObject obj = VolumeObjectFactory.CreateObject(dataset, series);
                        obj.transform.position = new Vector3(0.0f, 0.0f, 1.3f);

                        obj.tag = "Interactable";

                        obj.gameObject.AddComponent<Rigidbody>();
                        obj.GetComponent<Rigidbody>().useGravity = false;
                        obj.GetComponent<Rigidbody>().velocity = Vector3.zero;

                        obj.gameObject.AddComponent<BoxCollider>(); 

                        currentObj = obj.gameObject;

                        //EnableBtns(seriesBtns);
                    }
                }
            }
        }
    }

    private void DisableBtns(Button[] btns)
    {
        // disable header buttons
        patientsBtn.interactable = false;
        studiesBtn.interactable = false;

        // disable action buttons
        foreach (Button btn in btns)
        {
            btn.interactable = false;
        }
    }


    private void EnableBtns(Button[] btns)
    {
        // enable header buttons
        patientsBtn.interactable = true;
        studiesBtn.interactable = true;

        // enable action buttons
        foreach (Button btn in btns)
        {
            btn.interactable = true;
        }
    }
}
