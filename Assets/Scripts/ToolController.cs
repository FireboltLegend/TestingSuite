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
    public string peltierCondition = "";
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
                        string dataLine = string.Format("{0},{1},{2},{3},{4}",
                            "Participant Number",
                            "Trial Number",
                            "Wet",
                            "Temp",
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
                string dataLine = string.Format("{0},{1},{2},{3},{4}",
                    "P" + participantNumber,
                    trialNumber,
                    wetCondition,
                    peltierCondition,
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
