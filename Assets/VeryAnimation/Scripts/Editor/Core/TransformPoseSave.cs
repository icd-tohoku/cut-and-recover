using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VeryAnimation
{
    internal class TransformPoseSave
    {
        public GameObject RootObject { get; private set; }
        public Vector3 StartPosition { get; private set; }
        public Quaternion StartRotation { get; private set; }
        public Vector3 StartScale { get; private set; }
        public Vector3 StartLocalPosition { get; private set; }
        public Quaternion StartLocalRotation { get; private set; }
        public Vector3 StartLocalScale { get; private set; }
        public Vector3 OriginalPosition { get; private set; }
        public Quaternion OriginalRotation { get; private set; }
        public Vector3 OriginalScale { get; private set; }
        public Vector3 OriginalLocalPosition { get; private set; }
        public Quaternion OriginalLocalRotation { get; private set; }
        public Vector3 OriginalLocalScale { get; private set; }

        public Matrix4x4 StartMatrix { get { return Matrix4x4.TRS(StartPosition, StartRotation, StartScale); } }
        public Matrix4x4 OriginalMatrix { get { return Matrix4x4.TRS(OriginalPosition, OriginalRotation, OriginalScale); } }

        public class SaveData
        {
            public SaveData()
            {
            }
            public SaveData(Transform t)
            {
                Save(t);
            }
            public void Save(Transform t)
            {
                localPosition = t.localPosition;
                localRotation = t.localRotation;
                localScale = t.localScale;
                position = t.position;
                rotation = t.rotation;
                scale = t.lossyScale;
            }
            public void LoadLocal(Transform t)
            {
                if (t.localPosition != localPosition ||
                    t.localRotation != localRotation)
                {
#if UNITY_2022_3_OR_NEWER
                    t.SetLocalPositionAndRotation(localPosition, localRotation);
#else
                    t.localPosition = localPosition;
                    t.localRotation = localRotation;
#endif
                }
                if (t.localScale != localScale)
                    t.localScale = localScale;
            }
            public void LoadWorld(Transform t)
            {
                t.SetPositionAndRotation(position, rotation);
            }
            public Matrix4x4 LocalMatrix { get { return Matrix4x4.TRS(localPosition, localRotation, localScale); } }
            public Matrix4x4 Matrix { get { return Matrix4x4.TRS(position, rotation, scale); } }

            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
        }
        private readonly Dictionary<Transform, SaveData> originalTransforms;
        private Dictionary<Transform, SaveData> bindTransforms;
        private Dictionary<Transform, SaveData> tposeTransforms;
        private Dictionary<Transform, SaveData> prefabTransforms;
        private Dictionary<Transform, SaveData> humanDescriptionTransforms;

        public TransformPoseSave(GameObject gameObject)
        {
            RootObject = gameObject;
            StartPosition = OriginalPosition = gameObject.transform.position;
            StartRotation = OriginalRotation = gameObject.transform.rotation;
            StartScale = OriginalScale = gameObject.transform.lossyScale;
            StartLocalPosition = OriginalLocalPosition = gameObject.transform.localPosition;
            StartLocalRotation = OriginalLocalRotation = gameObject.transform.localRotation;
            StartLocalScale = OriginalLocalScale = gameObject.transform.localScale;
            #region originalTransforms
            {
                originalTransforms = new Dictionary<Transform, SaveData>();
                void SaveTransform(Transform t, Transform root)
                {
                    if (!originalTransforms.ContainsKey(t))
                    {
                        var saveTransform = new SaveData(t);
                        originalTransforms.Add(t, saveTransform);
                    }
                    for (int i = 0; i < t.childCount; i++)
                        SaveTransform(t.GetChild(i), root);
                }

                SaveTransform(gameObject.transform, gameObject.transform);
            }
            #endregion
        }
        public void CreateExtraTransforms()
        {
            #region saveTransforms
            {
                var bindPathTransforms = new Dictionary<string, SaveData>();
                var tposePathTransforms = new Dictionary<string, SaveData>();
                var prefabPathTransforms = new Dictionary<string, SaveData>();
                var humanDescriptionPathTransforms = new Dictionary<string, SaveData>();
                var defaultPathTransforms = new Dictionary<string, SaveData>();
                {
                    var uAvatarSetupTool = new UAvatarSetupTool();

                    static void SaveTransform(Dictionary<string, SaveData> transforms, Transform t, Transform root, bool scaleOverwrite)
                    {
                        var path = AnimationUtility.CalculateTransformPath(t, root);
                        if (!transforms.ContainsKey(path))
                        {
                            var saveTransform = new SaveData(t);
                            transforms.Add(path, saveTransform);
                        }
                        else if (scaleOverwrite)
                        {
                            transforms[path].localScale = t.localScale;
                            transforms[path].scale = t.lossyScale;
                        }
                        for (int i = 0; i < t.childCount; i++)
                            SaveTransform(transforms, t.GetChild(i), root, scaleOverwrite);
                    }

                    {
                        List<GameObject> goList = new();
                        void AddList(GameObject obj)
                        {
                            goList.Add(obj);
                            for (int i = 0; i < obj.transform.childCount; i++)
                            {
                                AddList(obj.transform.GetChild(i).gameObject);
                            }
                        }

                        void GetBindPose(GameObject go)
                        {
                            if (go.GetComponentInChildren<SkinnedMeshRenderer>() == null)
                                return;

                            var goTmp = GameObject.Instantiate<GameObject>(go);
                            goTmp.hideFlags |= HideFlags.HideAndDontSave;
                            goTmp.transform.SetParent(null);
#if UNITY_2022_3_OR_NEWER
                            goTmp.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
#else
                            goTmp.transform.localPosition = Vector3.zero;
                            goTmp.transform.localRotation = Quaternion.identity;
#endif
                            goTmp.transform.localScale = Vector3.one;
                            AddList(goTmp);
                            if (uAvatarSetupTool.SampleBindPose(goTmp))
                            {
                                var rootT = goTmp.transform;
                                #region Root
#if UNITY_2022_3_OR_NEWER
                                rootT.SetLocalPositionAndRotation(RootObject.transform.localPosition, RootObject.transform.localRotation);
#else
                                rootT.localPosition = RootObject.transform.localPosition;
                                rootT.localRotation = RootObject.transform.localRotation;
#endif
                                rootT.localScale = RootObject.transform.localScale;
                                #endregion
                                SaveTransform(defaultPathTransforms, rootT, rootT, false);
                                SaveTransform(bindPathTransforms, rootT, rootT, false);
                            }
                            GameObject.DestroyImmediate(goTmp);
                        }
                        void GetTPoseHumanDescriptionPose(GameObject go)
                        {
                            var animator = go.GetComponent<Animator>();
                            if (animator == null || !animator.isHuman || animator.avatar == null)
                                return;

                            var goTmp = GameObject.Instantiate<GameObject>(go);
                            goTmp.hideFlags |= HideFlags.HideAndDontSave;
                            goTmp.transform.SetParent(null);
#if UNITY_2022_3_OR_NEWER
                            goTmp.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
#else
                            goTmp.transform.localPosition = Vector3.zero;
                            goTmp.transform.localRotation = Quaternion.identity;
#endif
                            goTmp.transform.localScale = Vector3.one;
                            AddList(goTmp);
                            if (uAvatarSetupTool.SampleBindPose(goTmp) &&   //Reset
                                uAvatarSetupTool.SampleTPose(goTmp))
                            {
                                var rootT = goTmp.transform;
                                #region Root
#if UNITY_2022_3_OR_NEWER
                                rootT.SetLocalPositionAndRotation(RootObject.transform.localPosition, RootObject.transform.localRotation);
#else
                                rootT.localPosition = RootObject.transform.localPosition;
                                rootT.localRotation = RootObject.transform.localRotation;
#endif
                                rootT.localScale = RootObject.transform.localScale;
                                #endregion
                                SaveTransform(defaultPathTransforms, rootT, rootT, false);
                                SaveTransform(tposePathTransforms, rootT, rootT, false);
                            }
                            if (uAvatarSetupTool.SampleBindPose(goTmp))   //Reset
                            {
                                HumanDescription? humanDescription = null;
                                humanDescription = animator.avatar.humanDescription;
                                if (!humanDescription.HasValue)
                                {
                                    var modelImporter = AssetImporter.GetAtPath(EditorCommon.GetAssetPath(animator.avatar)) as ModelImporter;
                                    if (modelImporter != null)
                                        humanDescription = modelImporter.humanDescription;
                                }
                                if (humanDescription.HasValue)
                                {
                                    var hd = humanDescription.Value;
                                    var transforms = goTmp.GetComponentsInChildren<Transform>(true);
                                    for (int i = 0; i < hd.skeleton.Length; i++)
                                    {
                                        var t = Array.Find(transforms, x => x.name == hd.skeleton[i].name);
                                        if (t == null)
                                            continue;
#if UNITY_2022_3_OR_NEWER
                                        t.SetLocalPositionAndRotation(hd.skeleton[i].position, hd.skeleton[i].rotation);
#else
                                        t.localPosition = hd.skeleton[i].position;
                                        t.localRotation = hd.skeleton[i].rotation;
#endif
                                        t.localScale = hd.skeleton[i].scale;
                                    }
                                    var rootT = goTmp.transform;
                                    #region Root
#if UNITY_2022_3_OR_NEWER
                                    rootT.SetLocalPositionAndRotation(RootObject.transform.localPosition, RootObject.transform.localRotation);
#else
                                    rootT.localPosition = RootObject.transform.localPosition;
                                    rootT.localRotation = RootObject.transform.localRotation;
#endif
                                    rootT.localScale = RootObject.transform.localScale;
                                    #endregion
                                    SaveTransform(defaultPathTransforms, rootT, rootT, false);
                                    SaveTransform(humanDescriptionPathTransforms, rootT, rootT, false);
                                }
                            }
                            GameObject.DestroyImmediate(goTmp);
                        }

                        #region BindPose
                        GetBindPose(RootObject);
                        #endregion
                        #region TPose
                        GetTPoseHumanDescriptionPose(RootObject);
                        #endregion

                        var prefab = PrefabUtility.GetCorrespondingObjectFromSource(RootObject) as GameObject;
                        if (prefab != null)
                        {
                            var go = GameObject.Instantiate<GameObject>(prefab);
                            AnimatorUtility.DeoptimizeTransformHierarchy(go);
                            go.hideFlags |= HideFlags.HideAndDontSave;
                            AddList(go);
                            #region PrefabPose
                            {  //Root
#if UNITY_2022_3_OR_NEWER
                                go.transform.SetLocalPositionAndRotation(RootObject.transform.localPosition, RootObject.transform.localRotation);
#else
                                go.transform.localPosition = RootObject.transform.localPosition;
                                go.transform.localRotation = RootObject.transform.localRotation;
#endif
                                go.transform.localScale = RootObject.transform.localScale;
                            }
                            SaveTransform(defaultPathTransforms, go.transform, go.transform, true);
                            SaveTransform(prefabPathTransforms, go.transform, go.transform, false);
                            #endregion
                            GameObject.DestroyImmediate(go);
                        }
                        foreach (var go in goList)
                        {
                            if (go != null)
                                GameObject.DestroyImmediate(go);
                        }
                    }
                    //GameObjectPose
                    SaveTransform(defaultPathTransforms, RootObject.transform, RootObject.transform, false);
                }
                bindTransforms = Paths2Transforms(bindPathTransforms, RootObject.transform);
                tposeTransforms = Paths2Transforms(tposePathTransforms, RootObject.transform);
                prefabTransforms = Paths2Transforms(prefabPathTransforms, RootObject.transform);
                humanDescriptionTransforms = Paths2Transforms(humanDescriptionPathTransforms, RootObject.transform);
            }
            #endregion
        }

        public void ChangeStartTransform()
        {
            var transform = RootObject.transform;
            StartPosition = transform.position;
            StartRotation = transform.rotation;
            StartScale = transform.lossyScale;
            StartLocalPosition = transform.localPosition;
            StartLocalRotation = transform.localRotation;
            StartLocalScale = transform.localScale;
            ChangeTransform(transform);
        }
        public void ChangeTransform(Transform transform)
        {
            static void SetTransform(Dictionary<Transform, SaveData> list, Transform t)
            {
                if (list == null)
                    return;
                if (!list.TryGetValue(t, out SaveData save))
                    return;
                save.Save(t);
            }
            SetTransform(originalTransforms, transform);
        }
        public void ChangeTransformReference(GameObject gameObject)
        {
            var paths = new List<string>(originalTransforms.Count);
            var transforms = new List<Transform>(originalTransforms.Count);
            foreach (var pair in originalTransforms)
            {
                paths.Add(AnimationUtility.CalculateTransformPath(pair.Key, RootObject.transform));
                transforms.Add(pair.Key);
            }

            void SaveTransform(Transform t, Transform root)
            {
                var path = AnimationUtility.CalculateTransformPath(t, root);
                var index = paths.IndexOf(path);
                if (index >= 0)
                {
                    void ChangeTransform(Dictionary<Transform, SaveData> list, Transform oldT, Transform newT)
                    {
                        if (list != null && list.Count > 0)
                        {
                            if (list.TryGetValue(oldT, out SaveData saveData))
                            {
                                list.Remove(oldT);
                                list.Add(newT, saveData);
                            }
                        }
                    }
                    ChangeTransform(originalTransforms, transforms[index], t);
                    ChangeTransform(bindTransforms, transforms[index], t);
                    ChangeTransform(tposeTransforms, transforms[index], t);
                    ChangeTransform(prefabTransforms, transforms[index], t);
                    ChangeTransform(humanDescriptionTransforms, transforms[index], t);
                }
                for (int i = 0; i < t.childCount; i++)
                    SaveTransform(t.GetChild(i), root);
            }

            SaveTransform(gameObject.transform, gameObject.transform);
            RootObject = gameObject;
        }

        public bool IsRootStartTransform()
        {
            if (RootObject != null)
            {
                var t = RootObject.transform;
                if (t.position == StartPosition &&
                    t.rotation == StartRotation)
                {
                    return true;
                }
            }
            return false;
        }
        public void ResetRootStartTransform()
        {
            if (RootObject != null)
            {
                RootObject.transform.SetPositionAndRotation(StartPosition, StartRotation);
            }
        }
        public void ResetRootOriginalTransform()
        {
            if (RootObject != null)
            {
                RootObject.transform.SetPositionAndRotation(OriginalPosition, OriginalRotation);
            }
        }

        public bool ResetDefaultTransform()
        {
            if (ResetBindTransform()) return true;
            if (ResetPrefabTransform()) return true;
            if (ResetOriginalTransform()) return true;
            return false;
        }

        public bool IsEnableOriginalTransform()
        {
            return (originalTransforms != null && originalTransforms.Count > 0);
        }
        public bool ResetOriginalTransform()
        {
            if (IsEnableOriginalTransform())
            {
                foreach (var trans in originalTransforms)
                {
                    if (trans.Key != null)
                        trans.Value.LoadLocal(trans.Key);
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        public SaveData GetOriginalTransform(Transform t)
        {
            if (originalTransforms != null)
            {
                if (originalTransforms.TryGetValue(t, out SaveData data))
                {
                    return data;
                }
            }
            return null;
        }

        public bool IsEnableBindTransform()
        {
            return (bindTransforms != null && bindTransforms.Count > 0);
        }
        public bool ResetBindTransform()
        {
            if (IsEnableBindTransform())
            {
                foreach (var trans in bindTransforms)
                {
                    if (trans.Key != null)
                        trans.Value.LoadLocal(trans.Key);
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        public SaveData GetBindTransform(Transform t)
        {
            if (bindTransforms != null)
            {
                if (bindTransforms.TryGetValue(t, out SaveData data))
                {
                    return data;
                }
            }
            return null;
        }
        public bool IsEnableTPoseTransform()
        {
            return (tposeTransforms != null && tposeTransforms.Count > 0);
        }
        public bool ResetTPoseTransform()
        {
            if (IsEnableTPoseTransform())
            {
                foreach (var trans in tposeTransforms)
                {
                    if (trans.Key != null)
                        trans.Value.LoadLocal(trans.Key);
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        public SaveData GetTPoseTransform(Transform t)
        {
            if (tposeTransforms != null)
            {
                if (tposeTransforms.TryGetValue(t, out SaveData data))
                {
                    return data;
                }
            }
            return null;
        }

        public bool IsEnablePrefabTransform()
        {
            return (prefabTransforms != null && prefabTransforms.Count > 0);
        }
        public bool ResetPrefabTransform()
        {
            if (IsEnablePrefabTransform())
            {
                foreach (var trans in prefabTransforms)
                {
                    if (trans.Key != null)
                        trans.Value.LoadLocal(trans.Key);
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        public SaveData GetPrefabTransform(Transform t)
        {
            if (prefabTransforms != null)
            {
                if (prefabTransforms.TryGetValue(t, out SaveData data))
                {
                    return data;
                }
            }
            return null;
        }

        public bool IsEnableHumanDescriptionTransforms()
        {
            return (humanDescriptionTransforms != null && humanDescriptionTransforms.Count > 0);
        }
        public bool ResetHumanDescriptionTransforms()
        {
            if (IsEnableHumanDescriptionTransforms())
            {
                foreach (var trans in humanDescriptionTransforms)
                {
                    if (trans.Key != null)
                        trans.Value.LoadLocal(trans.Key);
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        public SaveData GetHumanDescriptionTransforms(Transform t)
        {
            if (humanDescriptionTransforms != null)
            {
                if (humanDescriptionTransforms.TryGetValue(t, out SaveData data))
                {
                    return data;
                }
            }
            return null;
        }

        private Dictionary<Transform, SaveData> Paths2Transforms(Dictionary<string, SaveData> src, Transform transform)
        {
            var dst = new Dictionary<Transform, SaveData>(src.Count);
            void SaveTransform(Transform t, Transform root)
            {
                var path = AnimationUtility.CalculateTransformPath(t, root);
                if (src.ContainsKey(path))
                    dst.Add(t, src[path]);
                for (int i = 0; i < t.childCount; i++)
                    SaveTransform(t.GetChild(i), root);
            }

            SaveTransform(transform, transform);
            return dst;
        }
    }
}
