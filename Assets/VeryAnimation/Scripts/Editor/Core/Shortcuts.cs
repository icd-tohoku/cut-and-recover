using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace VeryAnimation
{
    internal static class Shortcuts
    {
#pragma warning disable IDE0051, IDE0060
        public const string poseQuickSave1 = "Very Animation/Editor/Pose Quick Save 1";
        [Shortcut(poseQuickSave1, typeof(VeryAnimationEditorWindow), KeyCode.Alpha1)]
        static void PoseQuickSave1(ShortcutArguments args) { }

        public const string poseQuickLoad1 = "Very Animation/Editor/Pose Quick Load 1";
        [Shortcut(poseQuickLoad1, typeof(VeryAnimationEditorWindow), KeyCode.Alpha3)]
        static void PoseQuickLoad1(ShortcutArguments args) { }

        public const string changeClamp = "Very Animation/Editor/Change Clamp";
        [Shortcut(changeClamp, typeof(VeryAnimationEditorWindow), KeyCode.O)]
        static void ChangeClamp(ShortcutArguments args) { }

        public const string changeFootIK = "Very Animation/Editor/Change Foot IK";
        [Shortcut(changeFootIK, typeof(VeryAnimationEditorWindow), KeyCode.J)]
        static void ChangeFootIK(ShortcutArguments args) { }

        public const string changeMirror = "Very Animation/Editor/Change Mirror";
        [Shortcut(changeMirror, typeof(VeryAnimationEditorWindow), KeyCode.M)]
        static void ChangeMirror(ShortcutArguments args) { }

        public const string changeRootCorrectionMode = "Very Animation/Editor/Change Root Correction Mode";
        [Shortcut(changeRootCorrectionMode, typeof(VeryAnimationEditorWindow), KeyCode.L)]
        static void ChangeRootCorrectionMode(ShortcutArguments args) { }

        public const string changeSelectionBoneIK = "Very Animation/Editor/Change selection bone IK";
        [Shortcut(changeSelectionBoneIK, typeof(VeryAnimationEditorWindow), KeyCode.I)]
        static void ChangeSelectionBoneIK(ShortcutArguments args) { }

        public const string forceRefresh = "Very Animation/Animation Window/Force refresh";
        [Shortcut(forceRefresh, typeof(VeryAnimationEditorWindow), KeyCode.F5)]
        static void ForceRefresh(ShortcutArguments args) { }

        public const string nextAnimationClip = "Very Animation/Animation Window/Next animation clip";
        [Shortcut(nextAnimationClip, typeof(VeryAnimationEditorWindow), KeyCode.PageDown)]
        static void NextAnimationClip(ShortcutArguments args) { }

        public const string previousAnimationClip = "Very Animation/Animation Window/Previous animation clip";
        [Shortcut(previousAnimationClip, typeof(VeryAnimationEditorWindow), KeyCode.PageUp)]
        static void PreviousAnimationClip(ShortcutArguments args) { }

        public const string addInbetween = "Very Animation/Animation Window/Edit Keys/Add In between";
        [Shortcut(addInbetween, typeof(VeryAnimationEditorWindow), KeyCode.KeypadPlus, ShortcutModifiers.None)]
        static void AddInbetween(ShortcutArguments args) { }

        public const string removeInbetween = "Very Animation/Animation Window/Edit Keys/Remove In between";
        [Shortcut(removeInbetween, typeof(VeryAnimationEditorWindow), KeyCode.KeypadMinus, ShortcutModifiers.None)]
        static void RemoveInbetween(ShortcutArguments args) { }

        public const string hideSelectBones = "Very Animation/Hierarchy/Hide select bones";
        [Shortcut(hideSelectBones, typeof(VeryAnimationEditorWindow), KeyCode.H)]
        static void HideSelectBones(ShortcutArguments args) { }

        public const string showSelectBones = "Very Animation/Hierarchy/Show select bones";
        [Shortcut(showSelectBones, typeof(VeryAnimationEditorWindow), KeyCode.H, ShortcutModifiers.Shift)]
        static void ShowSelectBones(ShortcutArguments args) { }

        public const string previewChangePlaying = "Very Animation/Preview/Change playing";
        [Shortcut(previewChangePlaying, typeof(VeryAnimationEditorWindow),
#if UNITY_EDITOR_OSX
            KeyCode.Space, ShortcutModifiers.Alt)]
#else
            KeyCode.Space, ShortcutModifiers.Action)]
#endif
        static void PreviewChangePlaying(ShortcutArguments args) { }

        public const string addIKLevel = "Very Animation/IK/Add IK - Level / Direction";
        [Shortcut(addIKLevel, typeof(VeryAnimationEditorWindow), KeyCode.KeypadPlus, ShortcutModifiers.Action)]
        static void AddIKLevel(ShortcutArguments args) { }

        public const string subIKLevel = "Very Animation/IK/Sub IK - Level / Direction";
        [Shortcut(subIKLevel, typeof(VeryAnimationEditorWindow), KeyCode.KeypadMinus, ShortcutModifiers.Action)]
        static void SubIKLevel(ShortcutArguments args) { }

        private static bool EqualKeyCombinationSequence(string id, Event e)
        {
            foreach (var key in ShortcutManager.instance.GetShortcutBinding(id).keyCombinationSequence)
            {
                if (key.action == IsKeyControl(e) && key.alt == e.alt && key.shift == e.shift && key.keyCode == e.keyCode)
                    return true;
            }
            return false;
        }
#pragma warning restore IDE0051, IDE0060


        public static bool IsKeyControl(Event e)
        {
#if UNITY_EDITOR_OSX
            return e.command;
#else
            return e.control;
#endif
        }

        #region Very Animation Shortcuts
        public static bool IsPoseQuickSave1(Event e)
        {
            return EqualKeyCombinationSequence(poseQuickSave1, e);
        }

        public static bool IsPoseQuickLoad1(Event e)
        {
            return EqualKeyCombinationSequence(poseQuickLoad1, e);
        }

        public static bool IsChangeClamp(Event e)
        {
            return EqualKeyCombinationSequence(changeClamp, e);
        }

        public static bool IsChangeFootIK(Event e)
        {
            return EqualKeyCombinationSequence(changeFootIK, e);
        }

        public static bool IsChangeMirror(Event e)
        {
            return EqualKeyCombinationSequence(changeMirror, e);
        }

        public static bool IsChangeRootCorrectionMode(Event e)
        {
            return EqualKeyCombinationSequence(changeRootCorrectionMode, e);
        }

        public static bool IsChangeSelectionBoneIK(Event e)
        {
            return EqualKeyCombinationSequence(changeSelectionBoneIK, e);
        }

        public static bool IsForceRefresh(Event e)
        {
            return EqualKeyCombinationSequence(forceRefresh, e);
        }

        public static bool IsNextAnimationClip(Event e)
        {
            return EqualKeyCombinationSequence(nextAnimationClip, e);
        }

        public static bool IsPreviousAnimationClip(Event e)
        {
            return EqualKeyCombinationSequence(previousAnimationClip, e);
        }

        public static bool IsAddInbetween(Event e)
        {
            return EqualKeyCombinationSequence(addInbetween, e);
        }

        public static bool IsRemoveInbetween(Event e)
        {
            return EqualKeyCombinationSequence(removeInbetween, e);
        }

        public static bool IsHideSelectBones(Event e)
        {
            return EqualKeyCombinationSequence(hideSelectBones, e);
        }

        public static bool IsShowSelectBones(Event e)
        {
            return EqualKeyCombinationSequence(showSelectBones, e);
        }

        public static bool IsPreviewChangePlaying(Event e)
        {
            return EqualKeyCombinationSequence(previewChangePlaying, e);
        }

        public static bool IsAddIKLevel(Event e)
        {
            return EqualKeyCombinationSequence(addIKLevel, e);
        }

        public static bool IsSubIKLevel(Event e)
        {
            return EqualKeyCombinationSequence(subIKLevel, e);
        }
        #endregion

        #region Animation Window Shortcuts
        public static bool IsAnimationChangePlaying(Event e)
        {
            return EqualKeyCombinationSequence("Animation/Play Animation", e);
        }

        public static bool IsAnimationSwitchBetweenCurvesAndDopeSheet(Event e)
        {
            return EqualKeyCombinationSequence("Animation/Show Curves", e);
        }

        public static bool IsAddKeyframe(Event e)
        {
            return EqualKeyCombinationSequence("Animation/Key Selected", e);
        }

        public static bool IsMoveToNextFrame(Event e)
        {
            return EqualKeyCombinationSequence("Animation/Next Frame", e);
        }

        public static bool IsMoveToPrevFrame(Event e)
        {
            return EqualKeyCombinationSequence("Animation/Previous Frame", e);
        }

        public static bool IsMoveToNextKeyframe(Event e)
        {
            return EqualKeyCombinationSequence("Animation/Next Keyframe", e);
        }

        public static bool IsMoveToPreviousKeyframe(Event e)
        {
            return EqualKeyCombinationSequence("Animation/Previous Keyframe", e);
        }

        public static bool IsMoveToFirstKeyframe(Event e)
        {
            return EqualKeyCombinationSequence("Animation/First Keyframe", e);
        }

        public static bool IsMoveToLastKeyframe(Event e)
        {
            return EqualKeyCombinationSequence("Animation/Last Keyframe", e);
        }
        #endregion
    }
}
