using System.Collections.Generic;
using R3;

namespace Nankira.Devices
{
    /// <summary>
    /// マスクデバイス
    /// </summary>
    public sealed class MaskDevice
    {
        private List<Vibrator> _vibrators;
        private ReactiveProperty<float> _pressureLeft;
        private ReactiveProperty<float> _pressureRight;

        /// <summary>
        /// 左の圧力 .Valueで値を取得
        /// </summary>
        public ReadOnlyReactiveProperty<float> PressureLeft =>
            _pressureLeft.ToReadOnlyReactiveProperty();

        /// <summary>
        /// 右の圧力 .Valueで値を取得
        /// </summary>
        public ReadOnlyReactiveProperty<float> PressureRight =>
            _pressureRight.ToReadOnlyReactiveProperty();

        // Singleton
        private static readonly MaskDevice _instance = new();
        public static MaskDevice Instance => _instance;

        private MaskDevice()
        {
            // TODO: 振動子の初期化
        }

        // TODO: ここでデバイスの値をReactiveProperty.Valueにセットする処理を実装する
        // 必要に応じてSingletonMonoBehaviourを継承
    }
}