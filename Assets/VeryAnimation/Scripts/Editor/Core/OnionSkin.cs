using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace VeryAnimation
{
    internal class OnionSkin : IDisposable
    {
        private VeryAnimationWindow VAW { get { return VeryAnimationWindow.instance; } }

        private class OnionSkinObject : IDisposable
        {
            public DummyObject dummyObject;

            public bool active;

            public OnionSkinObject(GameObject go)
            {
                dummyObject = new DummyObject();
                dummyObject.Initialize(go);
                dummyObject.ChangeTransparent();
                active = false;
            }
            ~OnionSkinObject()
            {
                Assert.IsNull(dummyObject);
            }
            public void Dispose()
            {
                if (dummyObject != null)
                {
                    dummyObject.Dispose();
                    dummyObject = null;
                }
            }

            public void SetRenderQueue(int renderQueue)
            {
                dummyObject.SetTransparentRenderQueue(renderQueue);
            }

            public void SetColor(Color color)
            {
                dummyObject.SetColor(color);
            }
        }
        private readonly Dictionary<int, OnionSkinObject> onionSkinObjects;

        private bool IsShow { get { return VAW.IsShowSceneGizmo(); } }

        public OnionSkin()
        {
            onionSkinObjects = new Dictionary<int, OnionSkinObject>();
        }
        ~OnionSkin()
        {
            Assert.IsTrue(onionSkinObjects.Count == 0);
        }
        public void Dispose()
        {
            if (onionSkinObjects.Count > 0)
            {
                foreach (var pair in onionSkinObjects)
                {
                    pair.Value.Dispose();
                }
                onionSkinObjects.Clear();
            }
        }

        public void Update()
        {
            if (!VAW.VA.extraOptionsOnionSkin)
            {
                Dispose();
                return;
            }

            foreach (var pair in onionSkinObjects)
            {
                pair.Value.active = false;
            }

            if (IsShow)
            {
                var lastFrame = VAW.VA.GetLastFrame();

                if (VAW.EditorSettings.SettingExtraOnionSkinMode == EditorSettings.OnionSkinMode.Keyframes)
                {
                    #region Keyframes
                    float[] nextTimes = VAW.EditorSettings.SettingExtraOnionSkinNextCount > 0 ? new float[VAW.EditorSettings.SettingExtraOnionSkinNextCount] : null;
                    float[] prevTimes = VAW.EditorSettings.SettingExtraOnionSkinPrevCount > 0 ? new float[VAW.EditorSettings.SettingExtraOnionSkinPrevCount] : null;
                    VAW.VA.UAw.GetNearKeyframeTimes(nextTimes, prevTimes);
                    #region Next
                    if (nextTimes != null)
                    {
                        var frame = VAW.VA.UAw.GetCurrentFrame();
                        for (int i = 0; i < VAW.EditorSettings.SettingExtraOnionSkinNextCount; i++)
                        {
                            if (Mathf.Approximately(VAW.VA.CurrentTime, nextTimes[i])) break;
                            frame = VAW.VA.UAw.TimeToFrameRound(nextTimes[i]);
                            if (frame < 0 || frame > lastFrame) break;
                            var oso = SetFrame((i + 1), VAW.VA.GetFrameTime(frame));
                            var color = VAW.EditorSettings.SettingExtraOnionSkinNextColor;
                            var rate = VAW.EditorSettings.SettingExtraOnionSkinNextCount > 1 ? i / (float)(VAW.EditorSettings.SettingExtraOnionSkinNextCount - 1) : 0f;
                            color.a = Mathf.Lerp(color.a, VAW.EditorSettings.SettingExtraOnionSkinNextMinAlpha, rate);
                            oso.SetColor(color);
                        }
                    }
                    #endregion
                    #region Prev
                    if (prevTimes != null)
                    {
                        var frame = VAW.VA.UAw.GetCurrentFrame();
                        for (int i = 0; i < VAW.EditorSettings.SettingExtraOnionSkinPrevCount; i++)
                        {
                            if (Mathf.Approximately(VAW.VA.CurrentTime, prevTimes[i])) break;
                            frame = VAW.VA.UAw.TimeToFrameRound(prevTimes[i]);
                            if (frame < 0 || frame > lastFrame) break;
                            var oso = SetFrame(-(i + 1), VAW.VA.GetFrameTime(frame));
                            var color = VAW.EditorSettings.SettingExtraOnionSkinPrevColor;
                            var rate = VAW.EditorSettings.SettingExtraOnionSkinPrevCount > 1 ? i / (float)(VAW.EditorSettings.SettingExtraOnionSkinPrevCount - 1) : 0f;
                            color.a = Mathf.Lerp(color.a, VAW.EditorSettings.SettingExtraOnionSkinPrevMinAlpha, rate);
                            oso.SetColor(color);
                        }
                    }
                    #endregion
                    #endregion
                }
                else if (VAW.EditorSettings.SettingExtraOnionSkinMode == EditorSettings.OnionSkinMode.Frames)
                {
                    #region Frames
                    #region Next
                    {
                        var frame = VAW.VA.UAw.GetCurrentFrame();
                        for (int i = 0; i < VAW.EditorSettings.SettingExtraOnionSkinNextCount; i++)
                        {
                            frame += VAW.EditorSettings.SettingExtraOnionSkinFrameIncrement;
                            if (frame < 0 || frame > lastFrame) break;
                            var oso = SetFrame((i + 1), VAW.VA.GetFrameTime(frame));
                            var color = VAW.EditorSettings.SettingExtraOnionSkinNextColor;
                            var rate = VAW.EditorSettings.SettingExtraOnionSkinNextCount > 1 ? i / (float)(VAW.EditorSettings.SettingExtraOnionSkinNextCount - 1) : 0f;
                            color.a = Mathf.Lerp(color.a, VAW.EditorSettings.SettingExtraOnionSkinNextMinAlpha, rate);
                            oso.SetColor(color);
                        }
                    }
                    #endregion
                    #region Prev
                    {
                        var frame = VAW.VA.UAw.GetCurrentFrame();
                        for (int i = 0; i < VAW.EditorSettings.SettingExtraOnionSkinPrevCount; i++)
                        {
                            frame -= VAW.EditorSettings.SettingExtraOnionSkinFrameIncrement;
                            if (frame < 0 || frame > lastFrame) break;
                            var oso = SetFrame(-(i + 1), VAW.VA.GetFrameTime(frame));
                            var color = VAW.EditorSettings.SettingExtraOnionSkinPrevColor;
                            var rate = VAW.EditorSettings.SettingExtraOnionSkinPrevCount > 1 ? i / (float)(VAW.EditorSettings.SettingExtraOnionSkinPrevCount - 1) : 0f;
                            color.a = Mathf.Lerp(color.a, VAW.EditorSettings.SettingExtraOnionSkinPrevMinAlpha, rate);
                            oso.SetColor(color);
                        }
                    }
                    #endregion
                    #endregion
                }
            }

            foreach (var pair in onionSkinObjects)
            {
                if (pair.Value.active)
                    continue;
                pair.Value.dummyObject.GameObject.SetActive(false);
            }
        }

        private OnionSkinObject SetFrame(int frame, float time)
        {
            if (!onionSkinObjects.TryGetValue(frame, out OnionSkinObject oso))
            {
                oso = new OnionSkinObject(VAW.GameObject);
                {
                    const int QueueOffset = 300;
                    var offset = Math.Abs(frame);
                    offset = offset * 2 + (frame > 0 ? 1 : 0);
                    oso.SetRenderQueue((int)RenderQueue.Transparent - QueueOffset + offset);
                }
                onionSkinObjects.Add(frame, oso);
            }

            oso.active = true;
            if (!oso.dummyObject.GameObject.activeSelf)
                oso.dummyObject.GameObject.SetActive(true);
            oso.dummyObject.UpdateState();
            oso.dummyObject.SetTransformStart();
            oso.dummyObject.SampleAnimation(VAW.VA.CurrentClip, time);

            if (EditorApplication.isPlaying && EditorApplication.isPaused) //Is there a bug that will not be updated while pausing? Therefore, it forcibly updates it.
                oso.dummyObject.RendererForceUpdate();

            return oso;
        }
    }
}
