using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConsoleManager : MonoBehaviour
{
    public TMP_InputField inputField;
    public TextMeshProUGUI suggestionText;
    public TextMeshProUGUI outputText;
    public ScrollRect scrollRect;

    private Dictionary<string, Action> commandMap = new Dictionary<string, Action>();
    private int currentSuggestionIndex = 0;
    private List<string> currentSuggestions = new List<string>();

    private void Awake()
    {
        RegisterAllCommandMethods();
    }

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }


    void HandleLog(string logString, string stackTrace, LogType type)
    {
        string formattedLog = logString;

        switch (type)
        {
            case LogType.Warning:
                formattedLog = $"<color=yellow>[Warning] {logString}</color>";
                break;

            case LogType.Error:
                formattedLog = $"<color=red>[Error] {logString}</color>\n{stackTrace}\n";
                break;

            case LogType.Exception:
                formattedLog = $"<color=red>[Exception] {logString}</color>\n{stackTrace}\n";
                break;

            // Du kannst weitere LogType-Fälle hinzufügen, wenn du möchtest.
        }

        outputText.text += formattedLog + "\n";
    
        LimitOutputTextLength();
        AutoScrollToBottom();
    }



    void LimitOutputTextLength()
    {
        // Als Beispiel beschränken wir den Text auf 30 Zeilen:
        int maxLines = 30;
        var lines = outputText.text.Split('\n');
        if (lines.Length > maxLines)
        {
            outputText.text = string.Join("\n", lines.Skip(lines.Length - maxLines));
        }
    }
    
    private void AutoScrollToBottom()
    {
        // Dies stellt sicher, dass alle UI-Updates abgeschlossen sind, bevor es versucht zu scrollen.
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            AutoFillCommand();
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            SelectPreviousSuggestion();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            SelectNextSuggestion();
        }
        else
        {
            UpdateSuggestion();
        }
    }

    // ...

    private void UpdateSuggestion()
    {
        var input = inputField.text;

        if (string.IsNullOrEmpty(input))
        {
            suggestionText.text = string.Empty;
            currentSuggestions.Clear();
            return;
        }

        currentSuggestions = commandMap.Keys
            .Where(key => key.StartsWith(input, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (currentSuggestions.Count > 0)
        {
            currentSuggestionIndex %= currentSuggestions.Count; // Wraparound.
            RenderSuggestionText();
        }
        else
        {
            suggestionText.text = string.Empty;
        }
    }

    private void RenderSuggestionText()
    {
        for (int i = 0; i < currentSuggestions.Count; i++)
        {
            string suggestion = currentSuggestions[i];

            if (i == currentSuggestionIndex)
            {
                // Setze die blaue Hintergrundfarbe für den ausgewählten Vorschlag
                currentSuggestions[i] = $"<mark=#0000FF80>{suggestion}</mark>";
            }
        }

        suggestionText.text = string.Join("\n", currentSuggestions);
    }

    private void SelectNextSuggestion()
    {
        if (currentSuggestions.Count > 0)
        {
            currentSuggestionIndex++;
            currentSuggestionIndex %= currentSuggestions.Count;
            RenderSuggestionText();
        }
    }

    private void SelectPreviousSuggestion()
    {
        if (currentSuggestions.Count > 0)
        {
            currentSuggestionIndex--;
            if (currentSuggestionIndex < 0)
                currentSuggestionIndex = currentSuggestions.Count - 1;
            RenderSuggestionText();
        }
    }
    

    public void ExecuteCommand(string input)
    {
        var commandKey = commandMap.Keys.FirstOrDefault(k => k.Equals(input, StringComparison.OrdinalIgnoreCase));

        if (commandKey != null)
        {
            commandMap[commandKey].Invoke();
            outputText.text += $"\nBefehl '{input}' erfolgreich ausgeführt!";
            inputField.text = string.Empty;
        }
        else
        {
            outputText.text += $"\nUnbekannter Befehl: {input}";
            inputField.text = string.Empty;  // Diese Zeile setzt das Input-Feld zurück, wenn der Befehl nicht bekannt ist.
        }

        // Dies stellt sicher, dass nach jeder Eingabe automatisch zum Ende des Ausgabefensters gescrollt wird.
        AutoScrollToBottom();
    }



// ... (Rest des Codes bleibt unverändert)


    private void RegisterAllCommandMethods()
    {
        foreach (var monoBehaviour in FindObjectsOfType<MonoBehaviour>())
        {
            foreach (var method in monoBehaviour.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (Attribute.IsDefined(method, typeof(CommandAttribute)))
                {
                    var attribute = (CommandAttribute) Attribute.GetCustomAttribute(method, typeof(CommandAttribute));
                    var commandName = attribute.CustomCommandName ?? method.Name;
                    commandMap[commandName] = () => method.Invoke(monoBehaviour, null);
                }
            }
        }
    }

    private void AutoFillCommand()
    {
        if (currentSuggestionIndex >= 0 && currentSuggestionIndex < currentSuggestions.Count)
        {
            var selectedSuggestion = RemoveColorTags(currentSuggestions[currentSuggestionIndex]);
            inputField.text = selectedSuggestion;
            suggestionText.text = ""; // Nach dem Ausfüllen die Vorschlagstextanzeige leeren
        }
    }

    private string RemoveColorTags(string textWithColor)
    {
        // Farb-Tags entfernen
        return System.Text.RegularExpressions.Regex.Replace(textWithColor, @"<.*?>", string.Empty);
    }



}
