using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class ToolController : MonoBehaviour
{
    public string drawingSavePath;
    public ToolTCP toolTCP;

    public TrialResponse trialResponse;
    public GameObject idleScreen;
    public GameObject midTrialScreen;
    public GameObject nextTrialScreen;
    public List<GameObject> responseScreens;
    public int currentQuestion;

    public int participantNumber;
    public int trialNumber;
    public string saveString;
    public string wetCondition = "";
    public string wetLocation = "";
    public string peltierLocation = "";
    public string excelFilePath = "";
    public string excelFileName = "";
    public string excelFullPath = "";
    public string pathTime = "";
    public float waitTime;
    public float restTime;

    public bool skipThermalOnNeutral;
    public bool testing = false;
    public bool starting = false;
    private DateTime startTime;
    private bool experimentStarted = false;
    public GameObject visualCue;
    private Dictionary<string, Vector3> locationMapper = new Dictionary<string, Vector3>()
    {
        {"Index2", new Vector3(-4.6f,37.1f,0)},
        {"Index1", new Vector3(-2.2f,12.61f,0)},
        {"Middle2", new Vector3(12.2f,41.8f,0)},
        {"Middle1", new Vector3(11.6f,15.34f,0)},
        {"Ring2", new Vector3(27.2f,35.3f,0)},
        {"Ring1", new Vector3(22.96f,10.7f,0)},
        {"Pinky2", new Vector3(44f,18.5f,0)},
        {"Pinky1", new Vector3(34.88f,0.6f,0)},
        {"Thumb2", new Vector3(-35.84f,-20.1f,0)},
        {"Thumb1", new Vector3(-21.9f,-28.3f,0)},
        {"Palm1", new Vector3(2.8f,-6.8f,0)},
        {"Palm2", new Vector3(22.9f,-6.8f,0)},
        {"Palm3", new Vector3(2.8f,-26.2f,0)},
        {"Palm4", new Vector3(22.9f,-26.2f,0)}
    };

    // Start is called before the first frame update
    void Start()
    {
        skipThermalOnNeutral = false;
        startTime = DateTime.Now;
        pathTime = startTime.ToString("yyyy-MM-dd_HH-mm-ss");
    }

    // Update is called once per frame
    void Update()
    {
        if (testing)
        {
            Test();
        }
        if (starting)
        {
            starting = false;
            visualCue.transform.localPosition = locationMapper[peltierLocation];
            if (experimentStarted == false)
            {
                experimentStarted = true;
                Debug.Log("Experiment started for participant " + participantNumber);
                excelFilePath = Application.persistentDataPath;
                excelFileName = "P" + participantNumber + "_" + pathTime + ".csv";
                excelFullPath = Path.Combine(excelFilePath, excelFileName);
                try
                {
                    using (StreamWriter sw = new StreamWriter(excelFullPath, true))
                    {
                        string dataLine = string.Format("{0},{1},{2},{3},{4},{5}",
                            "Participant Number",
                            "Trial Number",
                            "Wet",
                            "Wet Location",
                            "Peltier Location",
                            "Q1"
                        );
                        sw.WriteLine(dataLine);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Failed to write to CSV file: " + e.Message);
                }
            }
            StartTrial();
        }
    }

    public void StartTrial()
    {
        Debug.Log(string.Format("Staring trial {0}, waiting for {1}", trialNumber, waitTime / 1000));

        trialResponse = new()
        {
            participantNumber = participantNumber,
            trialNumber = trialNumber
        };

        LoadMidTrialScreen();
        StartCoroutine(LoadResponseScreensHelper(waitTime));
    }

    public IEnumerator LoadResponseScreensHelper(float waitTime)
    {
        yield return new WaitForSeconds(waitTime / 1000f);
        Debug.Log("Trial finished, loading response UI...");
        LoadResponseScreens();
    }

    public void LoadMidTrialScreen()
    {
        idleScreen.SetActive(false);
        midTrialScreen.SetActive(true);
    }

    public void LoadResponseScreens()
    {
        Debug.Log("Loading response screen");
        idleScreen.SetActive(false);
        midTrialScreen.SetActive(false);

        foreach (GameObject responseScreen in responseScreens)
        {
            responseScreen.SetActive(false);
        }

        currentQuestion = 0;
        responseScreens[currentQuestion].SetActive(true);
    }

    public void NextQuestion()
    {
        responseScreens[currentQuestion].SetActive(false);

        currentQuestion++;
        if ((currentQuestion > responseScreens.Count - 1) || (currentQuestion > responseScreens.Count - 2 && skipThermalOnNeutral))
        {
            skipThermalOnNeutral = false;
            EndTrial();
        }
        else
        {
            responseScreens[currentQuestion].SetActive(true);
        }
    }

    public void SkipRestOfTrial()
    {
        responseScreens[currentQuestion].SetActive(false);
        currentQuestion = responseScreens.Count - 1;
        responseScreens[currentQuestion].SetActive(true);
    }

    public void SkipThermalOnNeutral()
    {
        skipThermalOnNeutral = true;
    }

    public void EndTrial()
    {
        foreach (GameObject responseScreen in responseScreens)
        {
            responseScreen.SetActive(false);
        }
        try
        {
            using (StreamWriter sw = new StreamWriter(excelFullPath, true))
            {
                string dataLine = string.Format("{0},{1},{2},{3},{4},{5}",
                    "P" + participantNumber,
                    trialNumber,
                    wetCondition,
                    wetLocation,
                    peltierLocation,
                    trialResponse.responses[0]
                );
                sw.WriteLine(dataLine);
            }
        }
        catch(System.Exception e)
        {
            Debug.LogError("Failed to write to CSV file: " + e.Message);
        }
        StartCoroutine(EndTrialHelper());
    }

    private IEnumerator EndTrialHelper()
    {
        // toolTCP.SendMessageToSuite(string.Format("response," + trialResponse.ToListString()));
        midTrialScreen.SetActive(true);
        yield return new WaitForSeconds(restTime);
        midTrialScreen.SetActive(false);
        nextTrialScreen.SetActive(true);
    }

    public void EndExperiment()
    {
        idleScreen.SetActive(true);
    }

    public void RecordTrialResponse(string response)
    {
        trialResponse.responses.Add(response);
    }

    public void HandleMessageFromSuite(string message)
    {
        if (message.StartsWith("$trialstart"))
        {
            string[] messageParams = message.Split(",");
            participantNumber = int.Parse(messageParams[1]);
            trialNumber = int.Parse(messageParams[2]);
            waitTime = float.Parse(messageParams[3]);

            starting = true;    // Must use bool instead of func because SetActive only possible in main thread
        }

        if (message.StartsWith("$experimentend"))
        {
            EndExperiment();
        }
    }

    public void Test()
    {
        trialNumber = 999;
        waitTime = 3000;
        StartTrial();
        testing = false;
    }

    public void NextTrial()
    {
        nextTrialScreen.SetActive(false);
        idleScreen.SetActive(true);
        // string message = "nexttrial";
        // toolTCP.SendMessageToSuite(message);
    }

    public class TrialResponse
    {
        public int participantNumber;
        public int trialNumber;
        public List<string> responses = new();

        public string ToListString()
        {
            return string.Format("{0},{1},{2}",
                participantNumber,
                trialNumber,
                string.Join(",", responses)
            );
        }
    }
}
