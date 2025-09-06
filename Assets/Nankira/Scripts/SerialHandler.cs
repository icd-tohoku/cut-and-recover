using UnityEngine;
using System.Collections;
using System.IO.Ports;
using System.Threading;
using System.Text;

public class SerialHandler : MonoBehaviour
{
    public delegate void SerialDataReceivedEventHandler(string message);
    public event SerialDataReceivedEventHandler OnDataReceived;

    // 圧力データを受信するためのイベント
    public delegate void PressureDataReceivedEventHandler(float pressure);
    public event PressureDataReceivedEventHandler OnPressureDataReceived;

    // portNameはprivateにして、外部から設定可能にする
    private string portName;
    [SerializeField] private int baudRate = 9600;

    private SerialPort serialPort_;
    private Thread thread_;
    private bool isRunning_ = false;

    private string message_;
    private bool isNewMessageReceived_ = false;
    
    // 圧力データ用
    private float pressureData_;
    private bool isNewPressureDataReceived_ = false;

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

        if (isNewPressureDataReceived_)
        {
            OnPressureDataReceived?.Invoke(pressureData_);
            isNewPressureDataReceived_ = false;
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

            serialPort_.ReadTimeout = 100; // タイムアウトを短くしてより頻繁に読み取り

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
        isNewPressureDataReceived_ = false;
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
        // 受信したデータが数値（圧力値）かどうかを判定
        if (float.TryParse(data, out float pressureValue))
        {
            // 圧力データとして処理
            pressureData_ = pressureValue;
            isNewPressureDataReceived_ = true;
            // Debug.Log($"[{portName}] Pressure: {pressureValue:F3} units");
        }
        else
        {
            // 通常のメッセージとして処理
            message_ = data;
            isNewMessageReceived_ = true;
            Debug.Log($"[{portName}] Message: {data}");
        }
    }

    public void Write(string message)
    {
        try
        {
            if (serialPort_ != null && serialPort_.IsOpen)
            {
                serialPort_.Write(message);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"SerialHandler Write Error: {e.Message}");
        }
    }

    // 現在の圧力値を取得するメソッド
    public float GetCurrentPressure()
    {
        return pressureData_;
    }

    // 接続状態を確認するプロパティ
    public bool IsConnected
    {
        get { return serialPort_ != null && serialPort_.IsOpen && isRunning_; }
    }

    // 最後に受信した圧力データの時刻を取得するプロパティ（デバッグ用）
    public float LastPressureData => pressureData_;
}