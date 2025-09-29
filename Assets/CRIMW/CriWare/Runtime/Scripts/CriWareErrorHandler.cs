/****************************************************************************
 *
 * Copyright (c) 2012 CRI Middleware Co., Ltd.
 *
 ****************************************************************************/

#pragma warning disable 0618

using UnityEngine;
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/**
 * \addtogroup CRIWARE_UNITY_COMPONENT
 * @{
 */
 namespace CriWare {

/**
 * <summary>CRIWAREエラーオブジェクト</summary>
 * <remarks>
 * <para header='説明'>CRIWAREライブラリのエラーメッセージを取得し出力するコンポーネントです。<br/></para>
 * </remarks>
 */
[AddComponentMenu("CRIWARE/Error Handler")]
public class CriWareErrorHandler : CriMonoBehaviour{
	/**
	 * <summary>コンソールデバッグ出力を有効にするかどうか</summary>
	 * <remarks>
	 * <para header='注意'>Unityデバッグウィンドウではなく、コンソールデバッグ出力を有効にするかどうか
	 * PCの場合はデバッグウィンドウに出力されます。</para>
	 * </remarks>
	 */
	public bool enableDebugPrintOnTerminal = false;

	/**
	 * <summary>デバッグ向け強制クラッシュフラグ</summary>
	 * <remarks>
	 * <para header='説明'>trueの場合、エラー発生時に強制的にクラッシュさせます。<br/>
	 * <see cref='OnCallback'/> にイベントが登録されている場合のみ作用します。</para>
	 * </remarks>
	 */
	public bool enableForceCrashOnError = false;

	/** シーンチェンジ時にエラーハンドラを削除するかどうか */
	public bool dontDestroyOnLoad = true;

	/**
	 * \deprecated
	 * 削除予定の非推奨APIです。本値は使用されていません。
	 */
	public static string errorMessage { get; set; }

	/**
	 * <summary>ログメッセージプレフィックス</summary>
	 * <remarks>
	 * <para header='説明'>CRIWAREによるログメッセージを示すプレフィックスです。</para>
	 * </remarks>
	 */
	public static readonly string logPrefix = "[CRIWARE]";

	/**
	 * <summary>エラーコールバックデリゲート</summary>
	 * <remarks>
	 * <para header='説明'>CRIWAREネイティブライブラリ内でエラーが発生した際に呼び出されるコール
	 * バックデリゲートです。<br/>
	 * 引数の文字列には、"エラーID:エラー内容"のフォーマットでメッセージが
	 * 記載されています。</para>
	 * </remarks>
	 */
	public delegate void Callback(string message);

	private static event Callback _onCallback = null;
	/**
	 * <summary>エラーコールバックイベント</summary>
	 * <remarks>
	 * <para header='説明'>CRIWAREネイティブライブラリ内でエラーが発生した際に呼び出されるコール<br/>
	 * バックイベントです。<br/>
	 * 未設定時には、本クラス内に定義されているデフォルトのログ出力関数が<br/>
	 * 呼び出されます。<br/>
	 * エラーメッセージを元に独自の処理を記述したい場合、デリゲートを登録して<br/>
	 * コールバック関数内で処理を行ってください。<br/>
	 * 本イベントは必ずメインスレッドから呼び出されます。</para>
	 * <para header='注意'>登録したコールバックは、CriWareErrorHandlerが生存中は常に呼び出される<br/>
	 * 可能性があります。<br/>
	 * 呼び出し先関数の実体が、CriWareErrorHandlerよりも先に解放されないように<br/>
	 * ご注意ください。</para>
	 * </remarks>
	 */
	public static event Callback OnCallback
	{
		add {
			bool previous = IsEnableNativePrintMessageFunc();

			_onCallback += value;

			if (previous && !IsEnableNativePrintMessageFunc()) {
				RegisterErrorCallback();
			}
		}
		remove {
			_onCallback -= value;
			RegisterErrorCallback();
		}
	}

	/**
	 * \deprecated
	 * 削除予定の非推奨APIです。
	 * CriWareErrorHandler.OnCallback event の使用を検討してください。
	 * <summary>エラーコールバック</summary>
	 * <remarks>
	 * <para header='説明'>CRIWAREネイティブライブラリ内でエラーが発生した際に呼び出されるコール
	 * バックです。<br/>
	 * 未設定時には、本クラス内に定義されているデフォルトのログ出力関数が
	 * 呼び出されます。<br/>
	 * エラーメッセージを元に独自の処理を記述したい場合、デリゲートを登録して
	 * コールバック関数内で処理を行ってください。<br/>
	 * 登録を解除する場合は null を設定してください。</para>
	 * <para header='注意'>登録したコールバックは、CriWareErrorHandlerが生存中は常に呼び出される
	 * 可能性があります。<br/>
	 * 呼び出し先関数の実体が、CriWareErrorHandlerよりも先に解放されないように
	 * ご注意ください。</para>
	 * </remarks>
	 */
	[Obsolete("CriWareErrorHandler.callback is deprecated. Use CriWareErrorHandler.OnCallback event", false)]
	public static Callback callback = null;

	/**
	* \deprecated
	* 削除予定の非推奨APIです。
	* 本値によらずエラーメッセージがキューイングできるようになったため、本値は参照されません。
	 */
	public uint messageBufferCounts = 8;

	private ConcurrentQueue<string> unThreadSafeMessages = new ConcurrentQueue<string>();
	private static bool _enableDebugPrintOnTerminal = false;

	/* オブジェクト作成時の処理 */
	void Awake() {
		/* 初期化カウンタの更新 */
		initializationCount++;
		if (initializationCount != 1) {
			/* 多重初期化は許可しない */
			GameObject.Destroy(this);
			return;
		}

		if (!CriErrorNotifier.IsRegistered(HandleMessage)) {
			CriErrorNotifier.OnCallbackThreadUnsafe += HandleMessage;
		}

		/* シーンチェンジ後もオブジェクトを維持するかどうかの設定 */
		if (dontDestroyOnLoad) {
			DontDestroyOnLoad(transform.gameObject);
		}
	}

	/* Execution Order の設定を確実に有効にするために OnEnable をオーバーライド */
	protected override void OnEnable() {
		base.OnEnable();
		_enableDebugPrintOnTerminal = enableDebugPrintOnTerminal;
		RegisterErrorCallback();

		if (!CriErrorNotifier.IsRegistered(HandleMessage)) {
			CriErrorNotifier.OnCallbackThreadUnsafe += HandleMessage;
		}
	}

	protected override void OnDisable() {
		base.OnDisable();
		if (CriErrorNotifier.IsRegistered(HandleMessage)) {
			CriErrorNotifier.OnCallbackThreadUnsafe -= HandleMessage;
		}
	}

	public override void CriInternalUpdate() {
		DequeueErrorMessages();
	}

	public override void CriInternalLateUpdate() { }

	void OnDestroy() {
		/* 初期化カウンタの更新 */
		initializationCount--;
		if (initializationCount != 0) {
			return;
		}

		/* エラー処理の終了処理 */
		if (CriErrorNotifier.IsRegistered(HandleMessage)) {
			CriErrorNotifier.OnCallbackThreadUnsafe -= HandleMessage;
		}

		CriErrorNotifier.SetCallbackNative(IntPtr.Zero);
	}

	private static bool IsEnableNativePrintMessageFunc() {
		return !Application.isEditor
			&& _onCallback == null
			&& callback == null
			&& _enableDebugPrintOnTerminal;
	}

	private static void RegisterErrorCallback() {
		if (IsEnableNativePrintMessageFunc()) {
			CriErrorNotifier.SetCallbackNative(IntPtr.Zero);
			CriErrorNotifier.SetCallbackNative(NativeMethod.criWareUnity_GetErrorCallbackFunction());
		} else {
			CriErrorNotifier.SetCallbackNative(IntPtr.Zero);
			CriErrorNotifier.SetCallbackNative(CriErrorNotifier.GetManagedPluginFunc());
		}
	}

	/* エラーメッセージのポーリングと出力 */
	private void DequeueErrorMessages() {
		string dequeuedMessage;
		while (unThreadSafeMessages.Count != 0) {
			if (!unThreadSafeMessages.TryDequeue(out dequeuedMessage)) {
				continue;
			}
			if (_onCallback != null) {
				_onCallback(dequeuedMessage);
			}
			if (callback != null) {
				callback(dequeuedMessage);
			}
		}
	}

	private void HandleMessage(string errmsg) {
		if (errmsg == null) {
			return;
		}

		if (_onCallback == null && callback == null) {
			OutputDefaultLog(errmsg);
		} else {
			unThreadSafeMessages.Enqueue(errmsg);
		}
		if (enableForceCrashOnError) {
			UnityEngine.Diagnostics.Utils.ForceCrash(UnityEngine.Diagnostics.ForcedCrashCategory.Abort);
		}
	}

	/** デフォルトのログ出力 */
	private static void OutputDefaultLog(string errmsg)
	{
		if (errmsg.StartsWith("E")) {
			Debug.LogError(logPrefix + " Error:" + errmsg);
		} else if (errmsg.StartsWith("W")) {
			Debug.LogWarning(logPrefix + " Warning:" + errmsg);
		} else {
			Debug.Log(logPrefix + errmsg);
		}
	}

	/** 初期化カウンタ */
	private static int initializationCount = 0;

	private static class NativeMethod {
#if !CRIWARE_ENABLE_HEADLESS_MODE
		[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
		internal static extern IntPtr criWareUnity_GetErrorCallbackFunction();
#else
		internal static IntPtr criWareUnity_GetErrorCallbackFunction() { return IntPtr.Zero; }
#endif
	}
} // end of class

	/**
	 * <summary>CRIWAREネイティブライブラリのエラーログ取得</summary>
	 * <remarks>
	 * <para header='説明'>CRIWAREネイティブライブラリ内で発生したエラーログを取得するクラスです。<br/></para>
	 * </remarks>
	 */
	public static class CriErrorNotifier {
		/**
		 * <summary>エラーコールバックデリゲート</summary>
		 * <remarks>
		 * <para header='説明'>CRIWAREネイティブライブラリ内でエラーが発生した際に呼び出されるコール
		 * バックデリゲートです。<br/>
		 * 引数の文字列には、"エラーID:エラー内容"のフォーマットでメッセージが
		 * 記載されています。</para>
		 * </remarks>
		 * <seealso cref='CriErrorNotifier::OnCallbackThreadUnsafe'/>
		 */
		public delegate void Callback(string message);
		private static event Callback _onCallbackThreadUnsafe = null;
		private static object objectLock = new System.Object();

		/**
		 * <summary>エラーコールバックイベント</summary>
		 * <remarks>
		 * <para header='説明'>CRIWAREネイティブライブラリ内でエラーが発生した際に呼び出されるコールバックイベントです。<br/>
		 * 未設定時はログが出力されません。<br/></para>
		 * <para header='注意'>本イベントはメインスレッド外から呼ばれることがあります。<br/>
		 * したがって、本イベントには必ずスレッドセーフなAPIを登録してください。<br/></para>
		 * </remarks>
		 * <seealso cref='CriErrorNotifier::IsRegistered'/>
		 */
		public static event Callback OnCallbackThreadUnsafe {
			add {
				lock (objectLock) {
					if (_onCallbackThreadUnsafe == null || _onCallbackThreadUnsafe.GetInvocationList().Length <= 0) {
						SetCallbackNative(null);
						SetCallbackNative(ErrorCallbackFromNative);
					}
					_onCallbackThreadUnsafe += value;
				}
			}
			remove {
				lock (objectLock) {
					_onCallbackThreadUnsafe -= value;
					if (_onCallbackThreadUnsafe == null || _onCallbackThreadUnsafe.GetInvocationList().Length <= 0) {
						SetCallbackNative(null);
					}
				}
			}
		}

		/**
		 * <summary>登録済みエラーコールバックイベントの確認</summary>
		 * <param name='target'>評価したいメソッド</param>
		 * <returns>登録されているかどうか</returns>
		 * <remarks>
		 * <para header='説明'><see cref='CriErrorNotifier.OnCallbackThreadUnsafe'/> に登録されているメソッドかどうか調べます。<br/>
		 * 多重登録や、解放忘れなどを調べたい場合に使用できます。<br/></para>
		 * </remarks>
		 * <seealso cref='CriErrorNotifier::Callback'/>
		 * <seealso cref='CriErrorNotifier::OnCallbackThreadUnsafe'/>
		 */
		public static bool IsRegistered(Callback target) {
			if (_onCallbackThreadUnsafe == null) {
				return false;
			}
			foreach (Callback item in _onCallbackThreadUnsafe.GetInvocationList()) {
				if (item == target) {
					return true;
				}
			}
			return false;
		}

		/**
		 * <summary>プラグイン内部関数</summary>
		 * <para header='注意'>ユーザーが本関数を呼び出すことは想定されていません。<br/></para>
		 */
		public static void CallEvent(string message) {
			// for expansion
			if (_onCallbackThreadUnsafe != null) {
				_onCallbackThreadUnsafe(message);
			}
		}

		/**
		 * <summary>ネイティブエラーコールバック関数の登録(関数ポインタ型)</summary>
		 * <param name='errorCallback'>エラーコールバック関数ポインタ</param>
		 * <remarks>
		 * <para header='説明'>エラーコールバック発生時に呼び出されるネイティブ関数ポインタを設定できます。<br/>
		 * 独自のネイティブ関数ポインタを登録したい場合のみ利用してください。<br/></para>
		 * <para header='注意'>IntPtr.Zeroを指定するとエラーコールバックのネイティブへの登録が解除されます。<br/>
		 * <br/>
		 * このAPIを利用した場合、 <see cref='OnCallbackThreadUnsafe'/> に登録された event は<br/>
		 * 呼び出されなくなります。</para>
		 * </remarks>
		 */
		public static void SetCallbackNative(IntPtr errorCallback) {
			NativeMethod.criErr_SetCallback(errorCallback);
		}

		internal static void SetCallbackNative(ErrorCallbackFunc errorCallback) {
			NativeMethod.criErr_SetCallback(errorCallback);
		}

		internal static ErrorCallbackFunc GetManagedPluginFunc() {
			return ErrorCallbackFromNative;
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void ErrorCallbackFunc(IntPtr errmsgPtr, System.UInt32 p1, System.UInt32 p2, IntPtr parray);

		[AOT.MonoPInvokeCallback(typeof(ErrorCallbackFunc))]
		private static void ErrorCallbackFromNative(IntPtr errmsgPtr, System.UInt32 p1, System.UInt32 p2, IntPtr parray) {
			string errmsg = Marshal.PtrToStringAnsi(NativeMethod.criErr_ConvertIdToMessage(errmsgPtr, p1, p2));
			CallEvent(errmsg);
		}

		private static class NativeMethod {
#if !CRIWARE_ENABLE_HEADLESS_MODE
			[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
			internal static extern void criErr_SetCallback(ErrorCallbackFunc callback);
			[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
			internal static extern void criErr_SetCallback(IntPtr callback);
			[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
			internal static extern IntPtr criErr_ConvertIdToMessage(IntPtr errmsgPtr, System.UInt32 p1, System.UInt32 p2);
#else
			internal static void criErr_SetCallback(ErrorCallbackFunc callback) { }
			internal static void criErr_SetCallback(IntPtr callback) { }
			internal static IntPtr criErr_ConvertIdToMessage(IntPtr errmsgPtr, System.UInt32 p1, System.UInt32 p2) { return IntPtr.Zero; }
#endif
		}
	}

} //namespace CriWare
/** @} */

/* --- end of file --- */
