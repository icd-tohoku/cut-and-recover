using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Text;
using System.Globalization;
using System.Linq;

public class SerialHandler : MonoBehaviour
{
    public delegate void SerialDataReceivedEventHandler(string message);
    public event SerialDataReceivedEventHandler OnDataReceived;

    // 複数の圧力データを受信するためのイベント
    public delegate void MultiplePressureDataReceivedEventHandler(Dictionary<int, float> sensorData);
    public event MultiplePressureDataReceivedEventHandler OnMultiplePressureDataReceived;

    // portNameはprivateにして、外部から設定可能にする
    private string portName;
    [SerializeField] private int baudRate = 9600;

    private SerialPort serialPort_;
    private Thread thread_;
    private bool isRunning_ = false;

    private string message_;
    private bool isNewMessageReceived_ = false;
    
    // 複数の圧力センサーデータ用
    private Dictionary<int, float> currentSensorData_;
    private bool isNewSensorDataReceived_ = false;

    private string readline;
    private StringBuilder dataBuffer = new StringBuilder();

    // portNameを外部から設定するためのメソッド
    public void SetPortName(string port)
    {
        portName = port;
    }

    // baudRateを外部から設定するためのメソッド
    public void SetBaudRate(int rate)
    {
        baudRate = rate;
    }

    // 現在のportNameを取得するためのプロパティ
    public string PortName => portName;

    void Awake()
    {
        currentSensorData_ = new Dictionary<int, float>();
        
        // portNameが設定されていない場合は、Openを遅延させる
        if (!string.IsNullOrEmpty(portName))
        {
            Open();
        }
    }

    // 外部から明示的にOpenを呼び出せるようにする
    public void OpenPort()
    {
        if (!string.IsNullOrEmpty(portName) && (serialPort_ == null || !serialPort_.IsOpen))
        {
            Open();
        }
    }

    void Update()
    {
        if (isNewMessageReceived_)
        {
            OnDataReceived?.Invoke(message_);
            isNewMessageReceived_ = false;
        }

        if (isNewSensorDataReceived_)
        {
            OnMultiplePressureDataReceived?.Invoke(new Dictionary<int, float>(currentSensorData_));
            isNewSensorDataReceived_ = false;
        }
    }

    void OnDestroy()
    {
        Close();
    }

    private void Open()
    {
        if (string.IsNullOrEmpty(portName))
        {
            Debug.LogError($"SerialHandler on {gameObject.name}: portName is not set!");
            return;
        }

        try
        {
            serialPort_ = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
            serialPort_.Open();

            serialPort_.ReadTimeout = 50; // タイムアウトを短くしてより頻繁に読み取り

            isRunning_ = true;

            thread_ = new Thread(Read);
            thread_.Start();

            Debug.Log($"SerialHandler on {gameObject.name}: Opened port {portName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SerialHandler on {gameObject.name}: Failed to open port {portName}. Error: {e.Message}");
        }
    }

    private void Close()
    {
        Write("S;");
        isNewMessageReceived_ = false;
        isNewSensorDataReceived_ = false;
        isRunning_ = false;

        if (thread_ != null && thread_.IsAlive)
        {
            thread_.Join(1000); // 1秒でタイムアウト
        }

        if (serialPort_ != null && serialPort_.IsOpen)
        {
            serialPort_.Close();
            serialPort_.Dispose();
        }
    }

    private void Read()
    {
        while (isRunning_ && serialPort_ != null && serialPort_.IsOpen)
        {
            try
            {
                // 1文字ずつ読み取り、';'が来たら処理
                int byteData = serialPort_.ReadByte();
                if (byteData != -1)
                {
                    char receivedChar = (char)byteData;
                    
                    if (receivedChar == ';')
                    {
                        // データが完了したので処理
                        string completeData = dataBuffer.ToString().Trim();
                        if (!string.IsNullOrEmpty(completeData))
                        {
                            ProcessReceivedData(completeData);
                        }
                        dataBuffer.Clear();
                    }
                    else
                    {
                        dataBuffer.Append(receivedChar);
                    }
                }
            }
            catch (System.TimeoutException)
            {
                // タイムアウトは正常な動作なので何もしない
            }
            catch (System.Exception e)
            {
                if (isRunning_) // スレッド終了時のエラーは無視
                {
                    Debug.LogWarning($"SerialHandler Read Error: {e.Message}");
                }
            }
        }
    }

    private void ProcessReceivedData(string data)
    {
        // ESP32からの自動センサーデータかどうかをチェック
        if (data.StartsWith("SENSOR_AUTO:"))
        {
            // "SENSOR_AUTO:" プレフィックスを除去してセンサーデータを処理
            string sensorData = data.Substring("SENSOR_AUTO:".Length);
            Debug.Log($"[{portName}] Auto sensor data received: {sensorData}");
            ProcessMultiplePressureData(sensorData);
            return;
        }

        // カンマ区切りのデータかどうかを判定
        if (data.Contains(","))
        {
            // カンマ区切りの圧力センサーデータとして処理
            ProcessMultiplePressureData(data);
        }
        else
        {
            // 単一データとして処理（数値かメッセージか判定）
            if (float.TryParse(data, NumberStyles.Float, CultureInfo.InvariantCulture, out float singleValue))
            {
                // 単一の圧力値として処理（センサーインデックス0として扱う）
                var sensorData = new Dictionary<int, float> { { 0, singleValue } };
                
                // 既存のセンサーデータに追加（他のセンサーデータを削除しない）
                currentSensorData_[0] = singleValue;
                isNewSensorDataReceived_ = true;
                
                Debug.Log($"[{portName}] Single Pressure: Sensor 0 = {singleValue:F3} units");
            }
            else
            {
                // 通常のメッセージとして処理
                message_ = data;
                isNewMessageReceived_ = true;
                Debug.Log($"[{portName}] Message: {data}");
            }
        }
    }

    private void ProcessMultiplePressureData(string data)
    {
        try
        {
            string[] values = data.Split(',');
            bool hasValidData = false;
            
            // 現在の一時的なセンサーデータを格納
            var tempSensorData = new Dictionary<int, float>();

            for (int i = 0; i < values.Length; i++)
            {
                string trimmedValue = values[i].Trim();
                if (float.TryParse(trimmedValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float pressure))
                {
                    tempSensorData[i] = pressure;
                    hasValidData = true;
                }
                else
                {
                    Debug.LogWarning($"[{portName}] Invalid sensor data at index {i}: '{trimmedValue}'");
                    // 無効なデータの場合は前の値を保持（新しい値は設定しない）
                }
            }

            if (hasValidData)
            {
                // 有効なデータのみcurrentSensorData_に反映
                foreach (var kvp in tempSensorData)
                {
                    currentSensorData_[kvp.Key] = kvp.Value;
                }
                
                isNewSensorDataReceived_ = true;
                
                // Debug用ログ - 全センサーの現在値を表示
                var sensorInfo = string.Join(", ", currentSensorData_.Select(kvp => $"S{kvp.Key}={kvp.Value:F3}"));
                Debug.Log($"[{portName}] Current sensors: {sensorInfo}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[{portName}] Error processing multiple pressure data '{data}': {e.Message}");
        }
    }

    public void Write(string message)
    {
        try
        {
            if (serialPort_ != null && serialPort_.IsOpen)
            {
                // メッセージの末尾に';'が含まれていない場合は追加
                if (!message.EndsWith(";"))
                {
                    message += ";";
                }
                
                serialPort_.Write(message);
                Debug.Log($"[{portName}] Sent: {message}");
            }
            else
            {
                Debug.LogWarning($"[{portName}] Cannot send message: port is not open");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[{portName}] SerialHandler Write Error: {e.Message}");
        }
    }

    // 現在の全センサーデータを取得するメソッド
    public Dictionary<int, float> GetCurrentSensorData()
    {
        return new Dictionary<int, float>(currentSensorData_);
    }

    // 特定のセンサーの現在値を取得するメソッド
    public float GetCurrentPressure(int sensorIndex = 0)
    {
        if (currentSensorData_.TryGetValue(sensorIndex, out float pressure))
        {
            return pressure;
        }
        return 0.0f;
    }

    // 接続されているセンサー数を取得
    public int GetSensorCount()
    {
        return currentSensorData_.Count;
    }

    // 利用可能なセンサーインデックスを取得
    public List<int> GetAvailableSensorIndices()
    {
        return new List<int>(currentSensorData_.Keys);
    }

    // 接続状態を確認するプロパティ
    public bool IsConnected
    {
        get { return serialPort_ != null && serialPort_.IsOpen && isRunning_; }
    }

    // 最後に受信したセンサーデータを取得するプロパティ（デバッグ用）
    public Dictionary<int, float> LastSensorData => new Dictionary<int, float>(currentSensorData_);
}

// using UnityEngine;
// using System.Collections;
// using System.Collections.Generic;
// using System.IO.Ports;
// using System.Threading;
// using System.Text;
// using System.Globalization;
// using System.Linq;

// public class SerialHandler : MonoBehaviour
// {
//     public delegate void SerialDataReceivedEventHandler(string message);
//     public event SerialDataReceivedEventHandler OnDataReceived;

//     // 複数の圧力データを受信するためのイベント
//     public delegate void MultiplePressureDataReceivedEventHandler(Dictionary<int, float> sensorData);
//     public event MultiplePressureDataReceivedEventHandler OnMultiplePressureDataReceived;

//     // portNameはprivateにして、外部から設定可能にする
//     private string portName;
//     [SerializeField] private int baudRate = 9600;

//     private SerialPort serialPort_;
//     private Thread thread_;
//     private bool isRunning_ = false;

//     private string message_;
//     private bool isNewMessageReceived_ = false;
    
//     // 複数の圧力センサーデータ用
//     private Dictionary<int, float> currentSensorData_;
//     private bool isNewSensorDataReceived_ = false;

//     private string readline;
//     private StringBuilder dataBuffer = new StringBuilder();

//     // portNameを外部から設定するためのメソッド
//     public void SetPortName(string port)
//     {
//         portName = port;
//     }

//     // baudRateを外部から設定するためのメソッド
//     public void SetBaudRate(int rate)
//     {
//         baudRate = rate;
//     }

//     // 現在のportNameを取得するためのプロパティ
//     public string PortName => portName;

//     void Awake()
//     {
//         currentSensorData_ = new Dictionary<int, float>();
        
//         // portNameが設定されていない場合は、Openを遅延させる
//         if (!string.IsNullOrEmpty(portName))
//         {
//             Open();
//         }
//     }

//     // 外部から明示的にOpenを呼び出せるようにする
//     public void OpenPort()
//     {
//         if (!string.IsNullOrEmpty(portName) && (serialPort_ == null || !serialPort_.IsOpen))
//         {
//             Open();
//         }
//     }

//     void Update()
//     {
//         if (isNewMessageReceived_)
//         {
//             OnDataReceived?.Invoke(message_);
//             isNewMessageReceived_ = false;
//         }

//         if (isNewSensorDataReceived_)
//         {
//             OnMultiplePressureDataReceived?.Invoke(new Dictionary<int, float>(currentSensorData_));
//             isNewSensorDataReceived_ = false;
//         }
//     }

//     void OnDestroy()
//     {
//         Close();
//     }

//     private void Open()
//     {
//         if (string.IsNullOrEmpty(portName))
//         {
//             Debug.LogError($"SerialHandler on {gameObject.name}: portName is not set!");
//             return;
//         }

//         try
//         {
//             serialPort_ = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
//             serialPort_.Open();

//             serialPort_.ReadTimeout = 100; // タイムアウトを短くしてより頻繁に読み取り

//             isRunning_ = true;

//             thread_ = new Thread(Read);
//             thread_.Start();

//             Debug.Log($"SerialHandler on {gameObject.name}: Opened port {portName}");
//         }
//         catch (System.Exception e)
//         {
//             Debug.LogError($"SerialHandler on {gameObject.name}: Failed to open port {portName}. Error: {e.Message}");
//         }
//     }

//     private void Close()
//     {
//         Write("S;");
//         isNewMessageReceived_ = false;
//         isNewSensorDataReceived_ = false;
//         isRunning_ = false;

//         if (thread_ != null && thread_.IsAlive)
//         {
//             thread_.Join(1000); // 1秒でタイムアウト
//         }

//         if (serialPort_ != null && serialPort_.IsOpen)
//         {
//             serialPort_.Close();
//             serialPort_.Dispose();
//         }
//     }

//     private void Read()
//     {
//         while (isRunning_ && serialPort_ != null && serialPort_.IsOpen)
//         {
//             try
//             {
//                 // 1文字ずつ読み取り、';'が来たら処理
//                 int byteData = serialPort_.ReadByte();
//                 if (byteData != -1)
//                 {
//                     char receivedChar = (char)byteData;
                    
//                     if (receivedChar == ';')
//                     {
//                         // データが完了したので処理
//                         string completeData = dataBuffer.ToString().Trim();
//                         if (!string.IsNullOrEmpty(completeData))
//                         {
//                             ProcessReceivedData(completeData);
//                         }
//                         dataBuffer.Clear();
//                     }
//                     else
//                     {
//                         dataBuffer.Append(receivedChar);
//                     }
//                 }
//             }
//             catch (System.TimeoutException)
//             {
//                 // タイムアウトは正常な動作なので何もしない
//             }
//             catch (System.Exception e)
//             {
//                 if (isRunning_) // スレッド終了時のエラーは無視
//                 {
//                     Debug.LogWarning($"SerialHandler Read Error: {e.Message}");
//                 }
//             }
//         }
//     }

//     private void ProcessReceivedData(string data)
//     {
//         // カンマ区切りのデータかどうかを判定
//         if (data.Contains(","))
//         {
//             // カンマ区切りの圧力センサーデータとして処理
//             ProcessMultiplePressureData(data);
//         }
//         else
//         {
//             // 単一データとして処理（数値かメッセージか判定）
//             if (float.TryParse(data, NumberStyles.Float, CultureInfo.InvariantCulture, out float singleValue))
//             {
//                 // 単一の圧力値として処理（センサーインデックス0として扱う）
//                 var sensorData = new Dictionary<int, float> { { 0, singleValue } };
//                 currentSensorData_ = sensorData;
//                 isNewSensorDataReceived_ = true;
//                 // Debug.Log($"[{portName}] Single Pressure: {singleValue:F3} units");
//             }
//             else
//             {
//                 // 通常のメッセージとして処理
//                 message_ = data;
//                 isNewMessageReceived_ = true;
//                 Debug.Log($"[{portName}] Message: {data}");
//             }
//         }
//     }

//     private void ProcessMultiplePressureData(string data)
//     {
//         try
//         {
//             string[] values = data.Split(',');
//             var sensorData = new Dictionary<int, float>();
//             bool hasValidData = false;

//             for (int i = 0; i < values.Length; i++)
//             {
//                 string trimmedValue = values[i].Trim();
//                 if (float.TryParse(trimmedValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float pressure))
//                 {
//                     sensorData[i] = pressure;
//                     hasValidData = true;
//                 }
//                 else
//                 {
//                     Debug.LogWarning($"[{portName}] Invalid sensor data at index {i}: '{trimmedValue}'");
//                     sensorData[i] = 0.0f; // デフォルト値を設定
//                 }
//             }

//             if (hasValidData)
//             {
//                 currentSensorData_ = sensorData;
//                 isNewSensorDataReceived_ = true;
                
//                 // Debug用ログ（コメントアウト可能）
//                 var sensorInfo = string.Join(", ", sensorData.Select(kvp => $"S{kvp.Key}={kvp.Value:F3}"));
//                 Debug.Log($"[{portName}] Multi Pressure: {sensorInfo}");
//             }
//         }
//         catch (System.Exception e)
//         {
//             Debug.LogError($"[{portName}] Error processing multiple pressure data '{data}': {e.Message}");
//         }
//     }

//     public void Write(string message)
//     {
//         try
//         {
//             if (serialPort_ != null && serialPort_.IsOpen)
//             {
//                 serialPort_.Write(message);
//             }
//         }
//         catch (System.Exception e)
//         {
//             Debug.LogWarning($"SerialHandler Write Error: {e.Message}");
//         }
//     }

//     // 現在の全センサーデータを取得するメソッド
//     public Dictionary<int, float> GetCurrentSensorData()
//     {
//         return new Dictionary<int, float>(currentSensorData_);
//     }

//     // 特定のセンサーの現在値を取得するメソッド
//     public float GetCurrentPressure(int sensorIndex = 0)
//     {
//         if (currentSensorData_.TryGetValue(sensorIndex, out float pressure))
//         {
//             return pressure;
//         }
//         return 0.0f;
//     }

//     // 接続されているセンサー数を取得
//     public int GetSensorCount()
//     {
//         return currentSensorData_.Count;
//     }

//     // 利用可能なセンサーインデックスを取得
//     public List<int> GetAvailableSensorIndices()
//     {
//         return new List<int>(currentSensorData_.Keys);
//     }

//     // 接続状態を確認するプロパティ
//     public bool IsConnected
//     {
//         get { return serialPort_ != null && serialPort_.IsOpen && isRunning_; }
//     }

//     // 最後に受信したセンサーデータを取得するプロパティ（デバッグ用）
//     public Dictionary<int, float> LastSensorData => new Dictionary<int, float>(currentSensorData_);
// }