namespace Nankira
{
    /// <summary>
    /// 体験の流れ定義
    /// </summary>
    public enum ExperienceStateType
    {
        /// <summary>
        /// 体験準備
        /// </summary>
        Preparing,

        /// <summary>
        /// 被切断（刀で切られている瞬間）
        /// </summary>
        Cutting,

        /// <summary>
        /// 被切断状態（頭が分かれている状態）
        /// </summary>
        Cut,

        /// <summary>
        /// 手で頬を押して再生している間
        /// </summary>
        Recovering,

        /// <summary>
        /// 体験終了
        /// </summary>
        Ending
    }
}