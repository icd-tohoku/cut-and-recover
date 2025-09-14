using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SerialManager : MonoBehaviour
{
    [Header("Port Settings")]
    [Tooltip("List of port names to connect to")]
    [SerializeField] private string[] portNames = { "COM3", "COM4", "COM5" };

    [Header("Serial Settings")]
    [Tooltip("Baud rate for all serial connections")]
    [SerializeField] private int baudRate = 9600;

    [Header("Transmission Settings")]
    [SerializeField] private float _transmissionSpan = 0.1f;

    // 動的に生成されたSerialHandlerを管理
    private Dictionary<string, SerialHandler> serialHandlers = new Dictionary<string, SerialHandler>();

    private string receivedData;
    private Dictionary<string, string> sendData = new Dictionary<string, string>();
    
    // 複数の圧力センサデータを管理（ポート名 -> センサーインデックス -> 圧力値）
    private Dictionary<string, Dictionary<int, float>> pressureData = new Dictionary<string, Dictionary<int, float>>();
    
    private bool isSendable = true;
    private float time;

    // 圧力データ更新イベント（複数センサー対応）
    public delegate void PressureDataUpdatedEventHandler(string portName, Dictionary<int, float> sensorData);
    public event PressureDataUpdatedEventHandler OnPressureDataUpdated;

    // 単一センサーデータ更新イベント（個別通知用）
    public delegate void SinglePressureDataUpdatedEventHandler(string portName, int sensorIndex, float pressure);
    public event SinglePressureDataUpdatedEventHandler OnSinglePressureDataUpdated;


    private readonly Dictionary<string, float> _avgPressure = new Dictionary<string, float>();


    void Start()
    {
        // 設定されたポート名を使用してSerialHandlerを動的に生成
        CreateSerialHandlers();
    }

    // 設定されたポート名を使用してSerialHandlerを動的に生成
    private void CreateSerialHandlers()
    {
        foreach (string portName in portNames)
        {
            if (string.IsNullOrEmpty(portName))
            {
                Debug.LogWarning("Empty port name found in settings!");
                continue;
            }

            CreateSingleSerialHandler(portName);
        }
    }

    private void CreateSingleSerialHandler(string portName)
    {
        if (serialHandlers.ContainsKey(portName))
        {
            Debug.LogWarning($"Port '{portName}' is already registered!");
            return;
        }

        // 新しいGameObjectを作成してSerialHandlerを追加
        GameObject handlerObject = new GameObject($"SerialHandler_{portName}");
        handlerObject.transform.SetParent(this.transform);

        SerialHandler serialHandler = handlerObject.AddComponent<SerialHandler>();
        
        // SerialHandlerを初期化
        serialHandler.SetPortName(portName);
        serialHandler.SetBaudRate(baudRate);
        serialHandler.OnDataReceived += OnDataReceived;
        serialHandler.OnMultiplePressureDataReceived += (sensorData) => OnMultiplePressureDataReceived(portName, sensorData);
        
        // 辞書に追加
        serialHandlers[portName] = serialHandler;
        
        // sendDataとpressureDataにも追加
        if (!sendData.ContainsKey(portName))
        {
            sendData[portName] = "S;";
        }
        if (!pressureData.ContainsKey(portName))
        {
            pressureData[portName] = new Dictionary<int, float>();
        }

        // ポートを開く
        serialHandler.OpenPort();
        
        Debug.Log($"Created SerialHandler for port: {portName}");
    }

    void OnDataReceived(string message)
    {
        receivedData = message;
        Debug.Log($"受信データ: {message}");
    }


    

    // 受信ハンドラの末尾を少しだけ修正（差分受信でも正しい平均にする）
    void OnMultiplePressureDataReceived(string portName, Dictionary<int, float> sensorData)
    {
        if (!pressureData.TryGetValue(portName, out var portDict))
        {
            portDict = new Dictionary<int, float>();
            pressureData[portName] = portDict;
        }

        // 既存の上書き更新
        foreach (var kvp in sensorData)
        {
            portDict[kvp.Key] = kvp.Value;
            OnSinglePressureDataUpdated?.Invoke(portName, kvp.Key, kvp.Value);
        }

        OnPressureDataUpdated?.Invoke(portName, new Dictionary<int, float>(portDict));

        // ★ここで「ポート内の全センサー現在値」から平均を更新（LINQなし＝小GCも出ない）
        if (portDict.Count > 0)
        {
            float sum = 0f;
            foreach (var v in portDict.Values) sum += v;
            _avgPressure[portName] = sum / portDict.Count;
        }
        else
        {
            _avgPressure[portName] = 0f;
        }
    }

    private void Update()
    {
        time += Time.deltaTime;

        // デフォルト値の設定（停止コマンド）
        foreach (var portName in serialHandlers.Keys)
        {
            if (!sendData.ContainsKey(portName))
            {
                sendData[portName] = "S;";
            }
        }

        // 定期的なコマンド送信
        if (time > _transmissionSpan)
        {
            TransmitCommands();
            time = 0;
        }
    }

    private void TransmitCommands()
    {
        foreach (var kvp in sendData)
        {
            string portName = kvp.Key;
            string command = kvp.Value;
            
            if (serialHandlers.TryGetValue(portName, out SerialHandler serialHandler))
            {
                serialHandler.Write(command);
                // Debug.Log($"sendData[{portName}]: {command}");
            }
            else
            {
                Debug.LogWarning($"ポート '{portName}' に対応するSerialHandlerが見つかりませんでした。");
            }
        }

        // 送信後、全てのコマンドを停止コマンドにリセット
        var keys = sendData.Keys.ToList();
        foreach (var key in keys)
        {
            sendData[key] = "S;";
        }
    }

    // 外部から呼び出されるコマンド送信メソッド群
    public void SendCommandToAllPorts(string command)
    {
        if (isSendable)
        {
            foreach (var key in sendData.Keys.ToList())
            {
                sendData[key] = command;
            }
            Debug.Log($"All ports Command: {command}");
        }
    }

    public void SendCommandToSpecificPort(string portName, string command)
    {
        if (isSendable && serialHandlers.ContainsKey(portName))
        {
            sendData[portName] = command;
            Debug.Log($"[{portName}] Command: {command}");
        }
        else
        {
            Debug.LogWarning($"Port '{portName}' not found or not sendable.");
        }
    }

    // 圧力センサ取得API（見つからなければ0f）基本はこれを使う
    public float GetAveragePressure(string portName)
    {
        return _avgPressure.TryGetValue(portName, out var v) ? v : 0f;
    }

    // 圧力データ取得メソッド（複数センサー対応）
    /*
    public Dictionary<int, float> GetPressureData(string portName)
    {
        if (pressureData.TryGetValue(portName, out Dictionary<int, float> sensorData))
        {
            return new Dictionary<int, float>(sensorData);
        }
        return new Dictionary<int, float>();
    }

    // 特定のセンサーの圧力データを取得
    public float GetPressureData(string portName, int sensorIndex)
    {
        if (pressureData.TryGetValue(portName, out Dictionary<int, float> sensorData))
        {
            if (sensorData.TryGetValue(sensorIndex, out float pressure))
            {
                return pressure;
            }
        }
        return 0.0f;
    }
    */

    // 全ポートの全センサーデータを取得
    public Dictionary<string, Dictionary<int, float>> GetAllPressureData()
    {
        var result = new Dictionary<string, Dictionary<int, float>>();
        foreach (var kvp in pressureData)
        {
            result[kvp.Key] = new Dictionary<int, float>(kvp.Value);
        }
        return result;
    }

    // 特定ポートのセンサー数を取得
    public int GetSensorCount(string portName)
    {
        if (pressureData.TryGetValue(portName, out Dictionary<int, float> sensorData))
        {
            return sensorData.Count;
        }
        return 0;
    }

    // ランタイムで新しいSerialHandlerを追加する場合のメソッド
    public void AddSerialHandler(string portName)
    {
        CreateSingleSerialHandler(portName);
    }

    // SerialHandlerを削除する場合のメソッド
    public void RemoveSerialHandler(string portName)
    {
        if (serialHandlers.TryGetValue(portName, out SerialHandler serialHandler))
        {
            serialHandler.OnDataReceived -= OnDataReceived;
            
            // GameObjectを破棄
            if (serialHandler != null && serialHandler.gameObject != null)
            {
                DestroyImmediate(serialHandler.gameObject);
            }
            
            serialHandlers.Remove(portName);
            sendData.Remove(portName);
            pressureData.Remove(portName);
            Debug.Log($"Removed SerialHandler for port: {portName}");
        }
    }

    // 利用可能なポート一覧を取得
    public List<string> GetAvailablePorts()
    {
        return new List<string>(serialHandlers.Keys);
    }

    // 特定のポートのSerialHandlerを取得
    public SerialHandler GetSerialHandler(string portName)
    {
        serialHandlers.TryGetValue(portName, out SerialHandler handler);
        return handler;
    }

    // 送信可能状態を設定/取得
    public bool IsSendable
    {
        get => isSendable;
        set => isSendable = value;
    }

    // Inspectorで設定されたポート名一覧を取得
    public string[] GetConfiguredPortNames()
    {
        return (string[])portNames.Clone();
    }

    // ランタイムでポート名一覧を更新
    public void UpdatePortNames(string[] newPortNames)
    {
        // 現在のハンドラーを全て削除
        var currentPorts = new List<string>(serialHandlers.Keys);
        foreach (string port in currentPorts)
        {
            RemoveSerialHandler(port);
        }

        // 新しいポート名を設定
        portNames = (string[])newPortNames.Clone();
        
        // 新しいハンドラーを作成
        CreateSerialHandlers();
    }

    void OnDestroy()
    {
        // 全てのSerialHandlerを適切に終了
        foreach (var kvp in serialHandlers)
        {
            if (kvp.Value != null)
            {
                kvp.Value.OnDataReceived -= OnDataReceived;
            }
        }
    }
}