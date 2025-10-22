using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class ESP32Pair
{
    [Tooltip("振動子用ESP32のポート名")]
    public string vibratorPort;
    
    [Tooltip("圧力センサ用ESP32のポート名")]
    public string sensorPort;
    
    [Tooltip("このペアの識別名（任意）")]
    public string pairName;
}

public class SerialManager : MonoBehaviour
{
    [Header("ESP32 Pair Settings")]
    [Tooltip("振動子と圧力センサのペアリスト")]
    [SerializeField] private ESP32Pair[] esp32Pairs = new ESP32Pair[]
    {
        new ESP32Pair { vibratorPort = "COM10", sensorPort = "COM7", pairName = "Pair1" }
    };

    [Header("Serial Settings")]
    [Tooltip("Baud rate for all serial connections")]
    [SerializeField] private int baudRate = 115200;

    // 振動子用SerialHandlerを管理
    private Dictionary<string, SerialHandler> vibratorHandlers = new Dictionary<string, SerialHandler>();
    
    // 圧力センサ用SerialHandlerを管理
    private Dictionary<string, SerialHandler> sensorHandlers = new Dictionary<string, SerialHandler>();
    
    // ペア名から対応するポート名を取得するための辞書
    private Dictionary<string, (string vibratorPort, string sensorPort)> pairMapping = new Dictionary<string, (string, string)>();

    private string receivedData;
    
    // 圧力センサデータを管理（センサーポート名 -> センサーインデックス -> 圧力値）
    private Dictionary<string, Dictionary<int, float>> pressureData = new Dictionary<string, Dictionary<int, float>>();

    // 圧力データ更新イベント
    public delegate void PressureDataUpdatedEventHandler(string sensorPort, Dictionary<int, float> sensorData);
    public event PressureDataUpdatedEventHandler OnPressureDataUpdated;

    public delegate void SinglePressureDataUpdatedEventHandler(string sensorPort, int sensorIndex, float pressure);
    public event SinglePressureDataUpdatedEventHandler OnSinglePressureDataUpdated;

    private readonly Dictionary<string, float> _avgPressure = new Dictionary<string, float>();

    void Start()
    {
        CreateSerialHandlers();
    }

    private void CreateSerialHandlers()
    {
        foreach (ESP32Pair pair in esp32Pairs)
        {
            if (string.IsNullOrEmpty(pair.vibratorPort) || string.IsNullOrEmpty(pair.sensorPort))
            {
                Debug.LogWarning($"Invalid pair configuration: {pair.pairName}");
                continue;
            }

            string pairName = string.IsNullOrEmpty(pair.pairName) 
                ? $"{pair.vibratorPort}_{pair.sensorPort}" 
                : pair.pairName;

            // ペアマッピングを保存
            pairMapping[pairName] = (pair.vibratorPort, pair.sensorPort);

            // 振動子用SerialHandlerを作成
            CreateVibratorHandler(pair.vibratorPort, pairName);
            
            // 圧力センサ用SerialHandlerを作成
            CreateSensorHandler(pair.sensorPort, pairName);

            Debug.Log($"Created ESP32 Pair '{pairName}': Vibrator={pair.vibratorPort}, Sensor={pair.sensorPort}");
        }
    }

    private void CreateVibratorHandler(string portName, string pairName)
    {
        if (vibratorHandlers.ContainsKey(portName))
        {
            Debug.LogWarning($"Vibrator port '{portName}' is already registered!");
            return;
        }

        GameObject handlerObject = new GameObject($"Vibrator_{pairName}_{portName}");
        handlerObject.transform.SetParent(this.transform);

        SerialHandler serialHandler = handlerObject.AddComponent<SerialHandler>();
        serialHandler.SetPortName(portName);
        serialHandler.SetBaudRate(baudRate);
        
        // ラムダ式でportNameをキャプチャ
        serialHandler.OnDataReceived += (message) => 
        {
            Debug.Log($"[Vibrator:{portName}] {message}");
        };
        
        vibratorHandlers[portName] = serialHandler;
        serialHandler.OpenPort();
    }

    private void CreateSensorHandler(string portName, string pairName)
    {
        if (sensorHandlers.ContainsKey(portName))
        {
            Debug.LogWarning($"Sensor port '{portName}' is already registered!");
            return;
        }

        GameObject handlerObject = new GameObject($"Sensor_{pairName}_{portName}");
        handlerObject.transform.SetParent(this.transform);

        SerialHandler serialHandler = handlerObject.AddComponent<SerialHandler>();
        serialHandler.SetPortName(portName);
        serialHandler.SetBaudRate(baudRate);
        
        // ラムダ式でportNameをキャプチャ
        serialHandler.OnDataReceived += (message) => 
        {
            receivedData = message;
            Debug.Log($"[Sensor:{portName}] {message}");
        };
        
        serialHandler.OnMultiplePressureDataReceived += (sensorData) => OnMultiplePressureDataReceived(portName, sensorData);
        
        sensorHandlers[portName] = serialHandler;
        
        if (!pressureData.ContainsKey(portName))
        {
            pressureData[portName] = new Dictionary<int, float>();
        }

        serialHandler.OpenPort();
    }

    void OnMultiplePressureDataReceived(string sensorPort, Dictionary<int, float> sensorData)
    {
        if (!pressureData.TryGetValue(sensorPort, out var portDict))
        {
            portDict = new Dictionary<int, float>();
            pressureData[sensorPort] = portDict;
        }


        foreach (var kvp in sensorData)
        {
            portDict[kvp.Key] = kvp.Value;
            OnSinglePressureDataUpdated?.Invoke(sensorPort, kvp.Key, kvp.Value);
            Debug.Log($"[Sensor:{sensorPort}] Sensor {kvp.Key}: {kvp.Value:F3}");
        }

        OnPressureDataUpdated?.Invoke(sensorPort, new Dictionary<int, float>(portDict));

        if (portDict.Count > 0)
        {
            float sum = 0f;
            foreach (var v in portDict.Values) sum += v;

            _avgPressure[sensorPort] = sum / (15 * portDict.Count);
            Debug.Log($"[Sensor:{sensorPort}] Average pressure: {_avgPressure[sensorPort]:F3} (from {portDict.Count} sensors)");
        }
        else
        {
            _avgPressure[sensorPort] = 0f;
        }
    }


    // === ペア単位での振動コマンド送信 ===

    /// <summary>
    /// 特定ペアの振動子にコマンドを送信
    /// </summary>
    public void SendCommandToPair(string pairName, string command)
    {
        if (pairMapping.TryGetValue(pairName, out var ports))
        {
            SendCommandToVibrator(ports.vibratorPort, command);
        }
        else
        {
            Debug.LogWarning($"Pair '{pairName}' not found.");
        }
    }


    /// <summary>
    /// 全ペアの振動子にコマンドを送信
    /// </summary>

    public void SendCommandToAllPairs(string command)
{
    // 並列で送信して遅延を削減
    System.Threading.Tasks.Parallel.ForEach(vibratorHandlers, kvp => 
    {
        kvp.Value.Write(command);
        Debug.Log($"[Vibrator:{kvp.Key}] Command: {command}");
    });
}

    /// <summary>
    /// 特定の振動子ポートにコマンドを送信
    /// </summary>
    public void SendCommandToVibrator(string vibratorPort, string command)
    {
        if (vibratorHandlers.TryGetValue(vibratorPort, out SerialHandler handler))
        {
            handler.Write(command);
            Debug.Log($"[Vibrator:{vibratorPort}] Command: {command}");
        }
        else
        {
            Debug.LogWarning($"Vibrator port '{vibratorPort}' not found.");
        }
    }

    /// <summary>
    /// 特定の振動子ポートにコマンドを送信
    /// </summary>
    public void SendCommandToSpecificVibrator(string command)
    {
        SendCommandToVibrator(vibratorPort: esp32Pairs[0].vibratorPort, command: command);
    }

    // === 振動制御メソッド ===
    
    /// <summary>
    /// 特定ペアの振動子を制御
    /// </summary>
    public void SetVibratorForPair(string pairName, int channel, int intensity)
    {
        string command = $"V{channel}{intensity:000};";
        SendCommandToPair(pairName, command);
    }

    /// <summary>
    /// 全ペアの振動子を制御
    /// </summary>
    public void SetVibratorForAllPairs(int channel, int intensity)
    {
        string command = $"V{channel}{intensity:000};";
        SendCommandToAllPairs(command);
    }

    /// <summary>
    /// 特定ペアの振動子を停止
    /// </summary>
    public void StopPair(string pairName)
    {
        SendCommandToPair(pairName, "STOP;");
    }

    /// <summary>
    /// 全ペアの振動子を停止
    /// </summary>
    public void StopAllPairs()
    {
        SendCommandToAllPairs("STOP;");
    }

    /// <summary>
    /// 特定ペアにプリセットパターンを送信
    /// </summary>
    public void ExecutePresetForPair(string pairName, int presetNumber)
    {
        string command = $"P{presetNumber};";
        SendCommandToPair(pairName, command);
    }

    /// <summary>
    /// 全ペアにプリセットパターンを送信
    /// </summary>
    public void ExecutePresetForAllPairs(int presetNumber)
    {
        string command = $"P{presetNumber};";
        SendCommandToAllPairs(command);
    }

    // === 圧力データ取得メソッド ===

    /// <summary>
    /// 特定ペアの圧力センサの平均値を取得
    /// </summary>
    public float GetAveragePressureForPair(string pairName)
    {
        if (pairMapping.TryGetValue(pairName, out var ports))
        {
            return GetAveragePressure(ports.sensorPort);
        }
        return 0f;
    }

    /// <summary>
    /// 特定センサポートの平均圧力を取得
    /// </summary>
    public float GetAveragePressure(string sensorPort)
    {
        return _avgPressure.TryGetValue(sensorPort, out var v) ? v : 0f;
    }

    /// <summary>
    /// 特定ペアの全センサーデータを取得
    /// </summary>
    public Dictionary<int, float> GetPressureDataForPair(string pairName)
    {
        if (pairMapping.TryGetValue(pairName, out var ports))
        {
            return GetPressureData(ports.sensorPort);
        }
        return new Dictionary<int, float>();
    }

    /// <summary>
    /// 特定センサポートの全センサーデータを取得
    /// </summary>
    public Dictionary<int, float> GetPressureData(string sensorPort)
    {
        if (pressureData.TryGetValue(sensorPort, out Dictionary<int, float> sensorData))
        {
            return new Dictionary<int, float>(sensorData);
        }
        return new Dictionary<int, float>();
    }

    /// <summary>
    /// 特定センサポート・特定センサーの圧力値を取得
    /// </summary>
    public float GetPressureData(string sensorPort, int sensorIndex)
    {
        if (pressureData.TryGetValue(sensorPort, out Dictionary<int, float> sensorData))
        {
            if (sensorData.TryGetValue(sensorIndex, out float pressure))
            {
                return pressure;
            }
        }
        return 0.0f;
    }

    /// <summary>
    /// 全センサポートの全データを取得
    /// </summary>
    public Dictionary<string, Dictionary<int, float>> GetAllPressureData()
    {
        var result = new Dictionary<string, Dictionary<int, float>>();
        foreach (var kvp in pressureData)
        {
            result[kvp.Key] = new Dictionary<int, float>(kvp.Value);
        }
        return result;
    }

    /// <summary>
    /// 特定センサポートのセンサー数を取得
    /// </summary>
    public int GetSensorCount(string sensorPort)
    {
        if (pressureData.TryGetValue(sensorPort, out Dictionary<int, float> sensorData))
        {
            return sensorData.Count;
        }
        return 0;
    }

    // === ペア情報取得メソッド ===

    /// <summary>
    /// 登録されている全ペア名を取得
    /// </summary>
    public List<string> GetAllPairNames()
    {
        return new List<string>(pairMapping.Keys);
    }

    /// <summary>
    /// 登録されている全センサポート名を取得
    /// </summary>
    public List<string> GetAllSensorPorts()
    {
        return new List<string>(sensorHandlers.Keys);
    }

    /// <summary>
    /// 登録されている全振動子ポート名を取得
    /// </summary>
    public List<string> GetAllVibratorPorts()
    {
        return new List<string>(vibratorHandlers.Keys);
    }

    /// <summary>
    /// 最初に登録されているセンサポート名を取得
    /// </summary>
    public string GetFirstSensorPort()
    {
        if (sensorHandlers.Count > 0)
        {
            return sensorHandlers.Keys.First();
        }
        return null;
    }

    /// <summary>
    /// 最初に登録されている振動子ポート名を取得
    /// </summary>
    public string GetFirstVibratorPort()
    {
        if (vibratorHandlers.Count > 0)
        {
            return vibratorHandlers.Keys.First();
        }
        return null;
    }

    /// <summary>
    /// 最初のペアのセンサポート名を取得
    /// </summary>
    public string GetFirstPairSensorPort()
    {
        if (pairMapping.Count > 0)
        {
            var firstPair = pairMapping.Values.First();
            return firstPair.sensorPort;
        }
        return null;
    }

    /// <summary>
    /// 最初のペアの振動子ポート名を取得
    /// </summary>
    public string GetFirstPairVibratorPort()
    {
        if (pairMapping.Count > 0)
        {
            var firstPair = pairMapping.Values.First();
            return firstPair.vibratorPort;
        }
        return null;
    }

    /// <summary>
    /// 特定ペアの振動子ポート名を取得
    /// </summary>
    public string GetVibratorPortForPair(string pairName)
    {
        if (pairMapping.TryGetValue(pairName, out var ports))
        {
            return ports.vibratorPort;
        }
        return null;
    }

    /// <summary>
    /// 特定ペアのセンサポート名を取得
    /// </summary>
    public string GetSensorPortForPair(string pairName)
    {
        if (pairMapping.TryGetValue(pairName, out var ports))
        {
            return ports.sensorPort;
        }
        return null;
    }

    void OnDestroy()
    {
        StopAllPairs();
        
        // SerialHandlerのクリーンアップは各HandlerのOnDestroyで行われる
    }
}