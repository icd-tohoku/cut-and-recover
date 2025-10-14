using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace VeryAnimation
{
    internal class AnimationClipValueSave : IDisposable
    {
        private GameObject rootObject;

        private EditorCurveBinding[] bindings;
        private float?[] floatValues;

        private EditorCurveBinding[] refBindings;
        private UnityEngine.Object[] refValues;

        private AnimationClip animationClip;

        public AnimationClipValueSave(GameObject gameObject, AnimationClip clip, AnimationClip[] layerClips = null)
        {
            Save(gameObject, clip, layerClips);
        }
        ~AnimationClipValueSave()
        {
            Assert.IsTrue(animationClip == null);
        }

        public void Save(GameObject gameObject, AnimationClip clip, AnimationClip[] layerClips = null)
        {
            Dispose();

            this.rootObject = gameObject;

            animationClip = new AnimationClip() { name = clip.name };
            animationClip.hideFlags |= HideFlags.HideAndDontSave;
            animationClip.legacy = clip.legacy;

            {
                HashSet<EditorCurveBinding> bindingSet = new(AnimationUtility.GetCurveBindings(clip));
                if (layerClips != null)
                {
                    foreach (var layerClip in layerClips)
                    {
                        var layerBindings = AnimationUtility.GetCurveBindings(layerClip);
                        foreach (var binding in layerBindings)
                        {
                            bindingSet.Add(binding);
                        }
                    }
                }
                bindings = bindingSet.ToArray();
            }
            floatValues = new float?[bindings.Length];
            for (int i = 0; i < bindings.Length; i++)
            {
                if (AnimationUtility.GetFloatValue(rootObject, bindings[i], out float floatValue))
                {
                    floatValues[i] = floatValue;
                    AnimationUtility.SetEditorCurve(animationClip, bindings[i], new AnimationCurve(new Keyframe[] { new(0f, floatValue) }));
                }
            }

            {
                HashSet<EditorCurveBinding> bindingSet = new(AnimationUtility.GetObjectReferenceCurveBindings(clip));
                if (layerClips != null)
                {
                    foreach (var layerClip in layerClips)
                    {
                        var layerBindings = AnimationUtility.GetObjectReferenceCurveBindings(layerClip);
                        foreach (var binding in layerBindings)
                        {
                            bindingSet.Add(binding);
                        }
                    }
                }
                refBindings = bindingSet.ToArray();
            }
            refValues = new UnityEngine.Object[refBindings.Length];
            for (int i = 0; i < refBindings.Length; i++)
            {
                if (AnimationUtility.GetObjectReferenceValue(rootObject, refBindings[i], out UnityEngine.Object refValue))
                {
                    refValues[i] = refValue;
                    AnimationUtility.SetObjectReferenceCurve(animationClip, refBindings[i], new ObjectReferenceKeyframe[] { new() { time = 0f, value = refValue } });
                }
            }
        }

        public void Dispose()
        {
            if (animationClip != null)
            {
                AnimationClip.DestroyImmediate(animationClip);
                animationClip = null;
            }
        }

        public void Load()
        {
            if (rootObject == null)
                return;

            if (animationClip != null)
            {
                animationClip.SampleAnimation(rootObject, 0f);
            }
        }

        public void LoadProperty()
        {
            if (rootObject == null)
                return;

            Load();

            if (bindings != null)
            {
                for (int i = 0; i < bindings.Length; i++)
                {
                    if (!floatValues[i].HasValue)
                        continue;

                    var t = rootObject.transform.Find(bindings[i].path);
                    if (t == null)
                        continue;

                    Component component;
                    try
                    {
                        if (!t.TryGetComponent(bindings[i].type, out component))
                            continue;
                    }
                    catch
                    {
                        continue;
                    }

                    var so = new SerializedObject(component);
                    var sp = so.FindProperty(bindings[i].propertyName);
                    if (sp == null)
                    {
                        //Debug.LogWarning($"<color=blue>[Very Animation]</color>Property not found: {bindings[i].propertyName} on {comp.GetType().Name}");
                        continue;
                    }

                    var type = AnimationUtility.GetEditorCurveValueType(rootObject, bindings[i]);
                    if (type == typeof(float))
                    {
                        sp.floatValue = floatValues[i].Value;
                    }
                    else if (type == typeof(int))
                    {
                        sp.intValue = (int)floatValues[i].Value;
                    }
                    else if (type == typeof(bool))
                    {
                        sp.boolValue = floatValues[i].Value != 0f;
                    }
                    else
                    {
                        Assert.IsTrue(false);
                        continue;
                    }

                    so.ApplyModifiedProperties();
                }
            }

            if (refBindings != null)
            {
                for (int i = 0; i < refBindings.Length; i++)
                {
                    var t = rootObject.transform.Find(refBindings[i].path);
                    if (t == null)
                        continue;

                    Component component;
                    try
                    {
                        if (!t.TryGetComponent(bindings[i].type, out component))
                            continue;
                    }
                    catch
                    {
                        continue;
                    }

                    var so = new SerializedObject(component);
                    var sp = so.FindProperty(refBindings[i].propertyName);
                    if (sp == null)
                    {
                        //Debug.LogWarning($"<color=blue>[Very Animation]</color>Property not found: {bindings[i].propertyName} on {comp.GetType().Name}");
                        continue;
                    }

                    sp.objectReferenceValue = refValues[i];

                    so.ApplyModifiedProperties();
                }
            }
        }
    }
}
