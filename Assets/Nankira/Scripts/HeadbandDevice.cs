using System.Collections.Generic;

namespace Nankira.Devices
{
    /// <summary>
    /// カチューシャデバイス
    /// </summary>
    public sealed class HeadbandDevice
    {
        private List<Vibrator> _vibrators = new();

        // Singleton
        private static readonly HeadbandDevice _instance = new();
        public static HeadbandDevice Instance => _instance;

        private HeadbandDevice()
        {
            // TODO: 振動子の初期化
        }
    }
}