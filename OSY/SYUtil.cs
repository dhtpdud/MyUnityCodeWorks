using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using JSchool.Common.Script.BaseFramework.Lecture.Addressable.Sound;
using JSchool.Common.Script.Manager;
using JSchool.Modules.Common.LCH.Extensions;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace JSchool.SYLab
{
    public class SYUtil
    {
        public class ReplaceableWaiter : IDisposable
        {
            private Coroutine coroutine;

            public void ReplacealbeDelayCall(float sec, Action action)
            {
                if (coroutine != null)
                    GameMainManager.Instance.StopCoroutine(coroutine);
                coroutine = GameMainManager.Instance.StartCoroutine(DelayCallCoroutine(sec, action));
            }

            public IEnumerator DelayCallCoroutine(float sec, Action action)
            {
                yield return new WaitForSeconds(sec);
                action.Invoke();
                coroutine = null;
            }

            public void Dispose()
            {
                if (coroutine == null) return;
                GameMainManager.Instance.StopCoroutine(coroutine);
                coroutine = null;
            }
        }

        public static async UniTask DoFadeWithChild(SpriteRenderer renderer, float endVale, float duration)
        {
            for (int i = 0; i < renderer.transform.childCount; i++)
            {
                var child = renderer.transform.GetChild(i).GetComponent<SpriteRenderer>();
                if (child)
#pragma warning disable CS4014
                    DoFadeWithChild(child, endVale, duration);
#pragma warning restore CS4014
            }

            await renderer.DOFade(endVale, duration);
        }

        //자주 사용되는 코루틴은 최적화에 좋지 않음
        public static IEnumerator DelayCallCoroutine(float delaySec, Action action)
        {
            yield return new WaitForSeconds(delaySec);
            action.Invoke();
        }

        public static IDisposable DelayCall(float delaySec, Action action)
        {
            return Observable
                .Timer(TimeSpan.FromSeconds(delaySec))
                .Subscribe(_ => action.Invoke());
        }

        public static IEnumerator SFXLoop(AudioSource target, float loopTime = 0, params string[] key)
        {

            int index = 0;
            YieldInstruction yieldCache = null;
            while (target)
            {
                if (loopTime > 0)
                    yieldCache = new WaitForSeconds(loopTime);
                else
                {
                    var tempTime = SoundLoader.GetAudioClipLengthFromKey(key[index]) + loopTime;
                    if (tempTime > 0)
                        yieldCache = new WaitForSeconds(tempTime);
                    else
                        yieldCache = YieldInstructionCache.WaitForEndOfFrame;
                }

                var tween = DOTween.TweensByTarget(target, true);
                if(tween == null || tween.Count == 0)
                    target.volume = SoundLoader.GetVolumeFromKey(key[index]);
                SoundLoader.PlayOneShotAudioSourceFromKey(target, key[index++]);
                if (index >= key.Length)
                    index = 0;
                yield return yieldCache;
            }
        }

        public static int RandomNewInt(int exept, int minInclucive, int maxExclusive)
        {
            if ((maxExclusive - minInclucive).Equals(1) && exept.Equals(minInclucive))
                return minInclucive;
            int val;
            do
            {
                val = Random.Range(minInclucive, maxExclusive);
            } while (val.Equals(exept));

            return val;
        }

        public static List<RaycastResult> IsPointerOverGameObject()
        {
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            return results;
        }

        internal static class YieldInstructionCache
        {
            /*class FloatComparer : IEqualityComparer<float>
            {
                bool IEqualityComparer<float>.Equals(float x, float y) => Mathf.Approximately(x,y);
                int IEqualityComparer<float>.GetHashCode(float obj) => obj.GetHashCode();
            }*/

            private static WaitForEndOfFrame _waitForEndOfFrame;
            private static WaitForFixedUpdate _waitForFixedUpdate;
            private static WaitForSeconds _waitFor1Sec;
            public static WaitForEndOfFrame WaitForEndOfFrame
            {
                get
                {
                    if (_waitForEndOfFrame == null)
                        return _waitForEndOfFrame = new WaitForEndOfFrame();
                    else
                        return _waitForEndOfFrame;
                }
            }
            public static WaitForFixedUpdate WaitForFixedUpdate
            {
                get
                {
                    if (_waitForFixedUpdate == null)
                        return _waitForFixedUpdate = new WaitForFixedUpdate();
                    else
                        return _waitForFixedUpdate;
                }
            }
            public static WaitForSeconds WaitFor1Sec
            {
                get
                {
                    if (_waitFor1Sec == null)
                        return _waitFor1Sec = new WaitForSeconds(1);
                    else
                        return _waitFor1Sec;
                }
            }

            /*private static readonly Dictionary<float, WaitForSeconds> _timeInterval =
                new Dictionary<float, WaitForSeconds>(new FloatComparer());

            public static WaitForSeconds WaitForSeconds(float seconds)
            {
                WaitForSeconds wfs;
                if (!_timeInterval.TryGetValue(seconds, out wfs))
                    _timeInterval.Add(seconds, wfs = new WaitForSeconds(seconds));
                return wfs;
            }*/
        }
        public static ParticleSystem SpawnPlayParticle(ParticleSystem particleSystem, Vector3 position)
        {
            if (!particleSystem) return null;
            var particle = Object.Instantiate(particleSystem).GetComponent<ParticleSystem>();
            particle.SetActive(true);
            particle.transform.position = position;
            particle.Play();
            return particle;
        }
        public static float GetAngle (Vector2 vStart, Vector2 vEnd)
        {
            return Quaternion.FromToRotation(Vector3.up, vEnd - vStart).eulerAngles.z;
        }
        public static string ToHexString(System.Drawing.Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        public static string ToRgbString(System.Drawing.Color c) => $"RGB({c.R}, {c.G}, {c.B})";
    }
}