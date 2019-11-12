using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugManager_Canvas : MonoBehaviour
{
    public GameObject InputPanel;
    public InputField CommandLine;
    public Text PrintOutput;

    private void Awake()
    {
        InputPanel.SetActive(false);
        PrintOutput.text = "";
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            InputPanel.SetActive(!InputPanel.activeSelf);
            if (InputPanel.activeSelf)
            {
                CommandLine.ActivateInputField();
            }
        }
    }

    public void CommandInputCallback()
    {
        print(CommandLine.text);
        DebugManager.Instance.CommandInput(CommandLine.text);
    }

    public void Print(string s)
    {
        if (s != null && !PrintOutput.text.Contains(s))
        {
            PrintOutput.text += "\n" + s;
        }
    }
}
