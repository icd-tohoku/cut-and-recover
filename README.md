# cut-and-recover
IVRC2025提出作品『豆|頁：なんかきられてももどるやつ』の開発リポジトリ．

Google Driveリンク：https://drive.google.com/drive/folders/1KvYWx4rpzhcD5TQM9MyRKLe3K8uxGdDX?usp=sharing


## 使用方法
Unityのエディター画面でのPlayでのみ動作します．

### 使用デバイス
- Meta Quest 2 or 3
- ESP32 2台
  - 振動子を接続したもの（USBで接続する）
  - 圧力センサ，マスクを接続したもの（Bluetoothで接続する）

### マイコンとの連携
1. https://github.com/icd-tohoku/nankira_vibration-tentative においてある，pressure_sensor.inoとvibrartion.inoをマイコンに書き込む．
2. PCのBluetoothの設定から"ESP_Pressure_Sensor"を追加する．
3. 「その他のBluetooth設定」の「COMポート」から，「発信」となっているポートを確認し（写真だとCOM4），UnityのSerialManagerコンポーネントのうち，ESP32PairsのSensorPortに書く．
  <img width="296" height="416" alt="image" src="https://github.com/user-attachments/assets/4ca7f713-1043-46d8-aafc-be879ee1046e" />
  <img width="299" height="373" alt="image" src="https://github.com/user-attachments/assets/ac59d7eb-f004-4b66-b0f9-e504aceabdd6" />


4. 「デバイスマネージャー」の「ポート」から，振動子を接続したマイコンをPCに接続したときに表示されるもののポートを確認し（写真だとCOM6），UnityのSerialManagerコンポーネントのうち，ESP32PairsのVibratorPortに書く．
  <img width="582" height="419" alt="image" src="https://github.com/user-attachments/assets/5438807f-9663-475d-964b-8c7c1635979f" />

### 操作説明
ユーザー側の操作方法は紹介動画参照．

- 初期状態（起動後初めの状態）からスペースキーを押すと頭を斬る
- 元に戻った状態でRキーを押すと初期状態に戻る（またスペースキーを押すことで斬れる）
- Cキーを押すと少しの間椅子の高さが腰の位置に追従する（連続でデモするときに座高によってユーザーの高さが変わるときに調整するため）
