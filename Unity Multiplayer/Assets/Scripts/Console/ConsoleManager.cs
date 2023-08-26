using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConsoleManager : MonoBehaviour
{

    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TextMeshProUGUI suggestionText;
    [SerializeField] private TextMeshProUGUI outputText;
    [SerializeField] private ScrollRect scrollRect;
    
    private const string Indentation = "    ";
    private const int MaxCharsPerLine = 50;
    private string _previousInputText;

    
    private RectTransform _suggestionRectTransform;
    private Dictionary<string, CommandData> _commandMap = new Dictionary<string, CommandData>();
    private int _currentSuggestionIndex = 0;
    private List<string> _currentSuggestions = new List<string>();
    
    private class CommandData
    {
        public Action<string> Action { get; set; }
        public string ParameterHint { get; set; }
    }
    private void Awake()
    {
        RegisterAllCommandMethods();
        InitializeInputField();
        _suggestionRectTransform = suggestionText.GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void Update()
    {
        HandleInputActions();
    }
    public void ExecuteCommand(string input)
    {
        input = input.StartsWith(">") ? input.Substring(1) : input;

        var commandParts = input.Split(' ', 2); // Befehl und Parameter trennen
        var commandKey = commandParts[0];
        var param = commandParts.Length > 1 ? commandParts[1] : null;

        if (_commandMap.ContainsKey(commandKey))
        {
            AddTextToConsole($"<color=#008800>Befehl '{commandKey}' erfolgreich ausgeführt!</color>");
            _commandMap[commandKey].Action(param); 

            inputField.text = string.Empty;
        }
        else
        {
            AddTextToConsole($"<color=#FF6666>Unbekannter Befehl: {input}</color>");
            inputField.text = string.Empty;
        }
    }
    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        ProcessLogMessage(logString, stackTrace, type);
    }
    private void InitializeInputField()
    {
        inputField.text = ">";
        inputField.caretPosition = 1;
    }
    private void AutoScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    private void HandleInputActions()
    {
        if (string.IsNullOrEmpty(inputField.text) || !inputField.text.StartsWith(">"))
        {
            inputField.text = ">" + inputField.text;
            inputField.caretPosition = inputField.text.Length;
        }
    
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
        if (Input.GetKeyDown(KeyCode.Backspace) && inputField.caretPosition == inputField.text.Length)
        {
            RemoveBracketedTextAtEnd();
            return;
        }
        else
        {
            UpdateSuggestion();
        }
        _previousInputText = inputField.text;
    }

    private void RemoveBracketedTextAtEnd()
    {
        if (_previousInputText.EndsWith("]") && !inputField.text.EndsWith("]"))
        {
            int lastOpenBracketIndex = inputField.text.LastIndexOf("[");
        
            // Wenn ein öffnender Klammer gefunden wurde, schneiden Sie den Text ab.
            if (lastOpenBracketIndex >= 0)
            {
                inputField.text = inputField.text.Substring(0, lastOpenBracketIndex);
                inputField.caretPosition = inputField.text.Length; // Setzen Sie den Cursor zurück ans Ende.
            }
        }
    }





    private void ProcessLogMessage(string logString, string stackTrace, LogType type)
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
        }

        AddTextToConsole(formattedLog);
    }

    private void AddTextToConsole(string text)
    {
        var words = text.Split(' ');
        var currentLine = Indentation;

        foreach (var word in words)
        {
            if (currentLine.Length + word.Length <= MaxCharsPerLine)
            {
                currentLine += word + " ";
            }
            else
            {
                outputText.text += "\n" + currentLine;
                currentLine = Indentation + word + " ";
            }
        }

        outputText.text += "\n" + currentLine;
        AutoScrollToBottom();
    }

    private void PositionSuggestionsAboveInput()
    {
        _suggestionRectTransform.pivot = new Vector2(0.5f, 0f);
        _suggestionRectTransform.anchorMin = new Vector2(0.5f, 0f);
        _suggestionRectTransform.anchorMax = new Vector2(0.5f, 0f);

        Vector3 newPosition = inputField.transform.position;
        newPosition.y += inputField.textComponent.fontSize + 5;
        _suggestionRectTransform.position = newPosition;

        float heightPerSuggestion = 20f;  // Dies kann je nach Bedarf angepasst werden.
        _suggestionRectTransform.sizeDelta = new Vector2(_suggestionRectTransform.sizeDelta.x, _currentSuggestions.Count * heightPerSuggestion);
    }


    private void UpdateSuggestion()
    {
        var rawInput = inputField.text;

        if (rawInput.StartsWith(">"))
        {
            rawInput = rawInput.Substring(1);
        }

        if (string.IsNullOrEmpty(rawInput))
        {
            suggestionText.text = string.Empty;
            _currentSuggestions.Clear();
            return;
        }

        _currentSuggestions = _commandMap
            .Where(entry => entry.Key.StartsWith(rawInput, StringComparison.OrdinalIgnoreCase))
            .Select(entry => $"{entry.Key} {entry.Value.ParameterHint}")
            .ToList();

        if (_currentSuggestions.Count > 0)
        {
            _currentSuggestionIndex %= _currentSuggestions.Count;
            RenderSuggestionText();
        }
        else
        {
            suggestionText.text = string.Empty;
        }
    
        PositionSuggestionsAboveInput();
    }


    private void RenderSuggestionText()
    {
        for (int i = 0; i < _currentSuggestions.Count; i++)
        {
            string suggestion = _currentSuggestions[i];

            if (i == _currentSuggestionIndex)
            {
                _currentSuggestions[i] = $"<mark=#ADD8E680>{suggestion}</mark>";
            }
        }

        suggestionText.text = string.Join("\n", _currentSuggestions);
    }

    private void SelectNextSuggestion()
    {
        if (_currentSuggestions.Count > 0)
        {
            _currentSuggestionIndex++;
            _currentSuggestionIndex %= _currentSuggestions.Count;
            RenderSuggestionText();
        }
    }

    private void SelectPreviousSuggestion()
    {
        if (_currentSuggestions.Count > 0)
        {
            _currentSuggestionIndex--;
            if (_currentSuggestionIndex < 0)
                _currentSuggestionIndex = _currentSuggestions.Count - 1;
            RenderSuggestionText();
        }
    }

    private void RegisterAllCommandMethods()
    {
        foreach (var monoBehaviour in FindObjectsOfType<MonoBehaviour>())
        {
            foreach (var method in monoBehaviour.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (Attribute.IsDefined(method, typeof(CommandAttribute)))
                {
                    var attribute = (CommandAttribute)Attribute.GetCustomAttribute(method, typeof(CommandAttribute));
                    var commandName = attribute.CustomCommandName ?? method.Name;
                    var parameters = method.GetParameters();

                    CommandData commandData = new CommandData();

                    if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
                    {
                        commandData.Action = (param) => method.Invoke(monoBehaviour, new object[] { param });
                        commandData.ParameterHint = "[string]";
                    }
                    else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(bool))
                    {
                        commandData.Action = (param) =>
                        {
                            bool boolValue;
                            if (bool.TryParse(param, out boolValue))
                            {
                                method.Invoke(monoBehaviour, new object[] { boolValue }); 
                            }
                            else
                            {
                                AddTextToConsole($"<color=#FF6666>Falscher Parameterwert: {param}. Erwartet: true oder false.</color>");
                            }
                        };
                        commandData.ParameterHint = "[bool]"; 
                    }
                    else if (parameters.Length == 0)
                    {
                        commandData.Action = _ => method.Invoke(monoBehaviour, null);
                        commandData.ParameterHint = "";
                    }
                    else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(float))
                    {
                        commandData.Action = (param) =>
                        {
                            float floatValue;
                            if (float.TryParse(param, out floatValue))
                            {
                                method.Invoke(monoBehaviour, new object[] { floatValue });
                            }
                            else
                            {
                                AddTextToConsole($"<color=#FF6666>Falscher Parameterwert: {param}. Erwartet: Eine Gleitkommazahl.</color>");
                            }
                        };
                        commandData.ParameterHint = "[float]";
                    }

                    _commandMap[commandName] = commandData;
                }
            }
        }
    }



    private void AutoFillCommand()
    {
        if (_currentSuggestionIndex >= 0 && _currentSuggestionIndex < _currentSuggestions.Count)
        {
            var selectedSuggestion = RemoveColorTags(_currentSuggestions[_currentSuggestionIndex]);
            inputField.text = selectedSuggestion;
            suggestionText.text = ""; 
        }
    }

    private string RemoveColorTags(string textWithColor)
    {
        return System.Text.RegularExpressions.Regex.Replace(textWithColor, @"<.*?>", string.Empty);
    }
    
}
