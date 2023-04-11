using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JSchool.Common.Extensions;
using JSchool.Common.Script.BaseFramework.Lecture.Addressable.Sound;
using JSchool.Common.Script.BaseFramework.Spine;
using JSchool.Common.Utils;
using JSchool.PJLab;
using Spine;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace JSchool.Modules.Common.OSY
{
    //Spine 애니메이션에 관련된 기능 제공
    //특정 애니메이션이 재생될 때 마다 이벤트를 발생시키거나,
    //특정 상태로 진입할 때 최초로 한번 이벤트를 발생시키는 등 이벤트 관련 확장성 기능 제공
    
    //Animation: 애니메이션
    
    //State: 상태
    //- 상태는 특정 애니메이션들을 계속 반복함
    //- 애니메이션을 완료후, 가장 최근 상태로 돌아감.
     
    
    public class SYMonoSpineController : MonoSpineController
    {
        [Serializable]
        public struct SYSpineState
        {
            public string name;
            public bool isRandom;
            public UnityEvent _startEvent;
            public SYStateEvent[] stateEvent;
            public SYAnimation_CustomDuration[] animations;

            public SYSpineState(string name, bool isRandom, UnityEvent startEvent, SYStateEvent[] stateEvent, SYAnimation_CustomDuration[] animations)
            {
                this.name = name;
                this.isRandom = isRandom;
                _startEvent = startEvent;
                this.stateEvent = stateEvent == null ? new SYStateEvent[] { } : stateEvent;
                this.animations = animations == null ? new SYAnimation_CustomDuration[] { } : animations;
            }
        }

        [Serializable]
        public class SYStateEvent
        {
            public string eventName;
            public bool isInvokeStart = true;
            public UnityEvent _event;
            public float minCycleTime;
            public float maxCycleTime;

            public SYStateEvent(string eventName, bool isInvokeStart, UnityEvent @event, float minCycleTime, float maxCycleTime)
            {
                this.eventName = eventName;
                this.isInvokeStart = isInvokeStart;
                _event = @event;
                this.minCycleTime = minCycleTime;
                this.maxCycleTime = maxCycleTime;
            }
        }

        public abstract class SYAnimation
        {
            [SpineAnimation] public string animationName;
        }

        [Serializable]
        public class SYAnimation_CustomDuration : SYAnimation
        {
            public float duration;

            public SYAnimation_CustomDuration(string animationName, float duration)
            {
                this.animationName = animationName;
                this.duration = duration;
            }
        }

        [Serializable]
        public class SYAnimation_CustomEvent : SYAnimation
        {
            public SYAnimationEvent[] events;

            public SYAnimation_CustomEvent(string animationName, SYAnimationEvent[] events)
            {
                this.animationName = animationName;
                this.events = events;
            }
        }

        [Serializable]
        public class SYOverrideAnimation : SYAnimation
        {
            public string overrideName;
        }

        [Serializable]
        public struct SYAnimationEvent
        {
            public string eventName;
            public UnityEvent _event;
            [Range(0, 100)] public float invokeMoment; // 시간이 아닌 해당 애니메이션 전체시간의 %로 계산

            public SYAnimationEvent(string eventName, UnityEvent @event, float invokeMoment)
            {
                this.eventName = eventName;
                _event = @event;
                this.invokeMoment = invokeMoment;
            }
        }


        private Transform _transform;

        public Transform Transform
        {
            get => _transform ? _transform : _transform = transform;
        }
        private Material _material;
        [HideInInspector] public Renderer spineRenderer;
        public Material Material => _material;

        public List<SYSpineState> states = new List<SYSpineState>(); // 등록된 상태 재생 시, 특정 애니메이션 목록을 반복 재생함

        public SYAnimation_CustomEvent[]
            animationEvents; // 등록된 애니메이션 재생 시, 해당 애니메이션을 특정길이만큼 재생하거나, 특정 이벤트를 원하는 순간에 실행함
        
        public SYOverrideAnimation[]
            overrideAnimationNames; // 애니메이션 이름 오버라이드

        public Coroutine aniProgress;
        public Coroutine eventProgress;

        private float _currentAnimationTimeScale = 1;
        
        protected Dictionary<int, string> currentAnimationName = new Dictionary<int,string>();

        public Dictionary<int, string>  CurrentAnimationName
        {
            get => currentAnimationName;
        }

        private Dictionary<int,TrackEntry> _currentAnimationTrack = new Dictionary<int, TrackEntry>();
        public Dictionary<int,TrackEntry> CurrentAnimationTrack
        {
            get => _currentAnimationTrack;
        }

        private Dictionary<int,IDisposable> _currentSpineState = new Dictionary<int,IDisposable>();
        private PJDisposer _scheduledEvent = new PJDisposer();
        public Dictionary<int, string> LastStateName { get; private set; }
        public Dictionary<int, string> CurrentStateName { get; private set; }
        private Coroutine[] _stateEvents;

        [SerializeField] private bool isInitState = true;

        protected override void Awake()
        {
            base.Awake();
            CurrentStateName = new Dictionary<int, string>();
            LastStateName = new Dictionary<int, string>();
            if (SkeletonAnimation)
            {
                spineRenderer = GetComponent<Renderer>();
                _material = spineRenderer.material;
            }
            else if (SkeletonGraphic)
            {
                _material = GetComponent<SkeletonGraphic>().materialForRendering;
            }

            AnimationState.Start += entry =>
            {
                _scheduledEvent.Dispose();
                if(eventProgress != null)
                {
                    StopCoroutine(eventProgress);
                    eventProgress = null;
                }
                if(gameObject.activeInHierarchy)
                    eventProgress = StartCoroutine(InvokeEnvets(entry));
            };
        }

        IEnumerator InvokeEnvets(TrackEntry entry)
        {
            do
            {
                float totalAniSec;
                SYAnimation_CustomEvent ani =
                    animationEvents.FirstOrDefault(x => x.animationName.Equals(entry.ToString()));
                if (ani != null && ani.events != null)
                {
                    float[] momentSec = new float[ani.events.Length];
                    if (animationEvents.Length > 0)
                    {
                        totalAniSec = GetAnimationTime(entry.ToString());
                        for (var i = 0; i < momentSec.Length; i++)
                            momentSec[i] = (totalAniSec * ani.events[i].invokeMoment / 100) / AnimationState.TimeScale;
                        
                        for (var i = 0; i < ani.events.Length; i++)
                        {
                            var i1 = i;
                            // DelayCall이기 때문에, momentSec[i]초후 사용하는 i의 값은 달라질 수 밖에 없다.
                            //따라서, i1에 미리 변수값을 캐싱해 두고, DelayCall 에서 사용한다.
                            _scheduledEvent.Add(DelayCallUtils.DelayCall(momentSec[i],
                                () => ani.events[i1]._event.Invoke()));
                        }
                    }
                }
                else
                    break;

                yield return new WaitForSpineAnimationComplete(AnimationState.GetCurrent(0), true);
            } while (AnimationState.GetCurrent(0).Loop);
        }

        private void Start()
        {
            if (isInitState && states.Count > 0)
                StatePlay(states[0], 0);
        }

        /// <summary>
        /// ※Inspector 등록용※
        /// </summary>
        public void SetPauseAnimation(bool pause) => this.Pause(pause);

        /// <summary>
        /// ※Inspector 등록용※
        /// </summary>
        public void SetAnimationTimeScale(float timeScale)
        {
            this.SetTimeScale(_currentAnimationTimeScale = timeScale);
            
            if (SkeletonGraphic)
                SkeletonGraphic.timeScale = _currentAnimationTimeScale;
        }

        
        public float GetCurrentAnimationTime =>
            SkeletonDataAsset.GetSkeletonData(true).FindAnimation(currentAnimationName[0])
                .Duration / _currentAnimationTimeScale;

        public float GetCurrentAnimationTimeOfLayer(int layer)
        {
            return SkeletonDataAsset.GetSkeletonData(true).FindAnimation(currentAnimationName[layer])
                .Duration / _currentAnimationTimeScale;
        }
            
        
        public float GetCurrentAnimationRemainTime =>
            GetCurrentAnimationTime - SkeletonAnimation.state.GetCurrent(0).TrackTime / _currentAnimationTimeScale;

        public void SetAlpha(float value) => Skeleton.A = value;

        public void SetSortingLayer(string val)
        {
            if (!SkeletonAnimation) return;
            spineRenderer.sortingLayerName = val;
        }
        public void SetSortingLayer(int val)
        {
            if (!SkeletonAnimation) return;
            spineRenderer.sortingLayerID = val;
        }
        public void SetSortingOrder(int val)
        {
            if (!SkeletonAnimation) return;
            spineRenderer.sortingOrder = val;
        }

        /// <summary>
        /// 애니메이션 시간 가져오기.
        /// </summary>
        /// <param name="animationName">가져올 애니메이션 시간</param>
        /// <returns></returns>
        public virtual float GetAnimationTime(string animationName)
        {
            if (animationName == null || animationName == "")
                return 0;
            return SkeletonDataAsset.GetSkeletonData(true).FindAnimation(animationName).Duration / _currentAnimationTimeScale;
        }

        public void DisableState(int layer = 0)
        {
            StopAllStateEvents();
            CurrentStateName[layer] = "";
            if (_currentSpineState.ContainsKey(layer) && _currentSpineState[layer] != null) 
                _currentSpineState[layer].Dispose();
            _currentSpineState.Clear();
        }

        /// <summary>
        /// ※Inspector 등록용※
        /// 애니메이션 설정
        /// MixDuration = 0.25f
        /// 특정 애니메이션 이후, 가장 최근 상태로 돌아옴
        /// </summary>
        public float SetAnimation(string aniStateName)
        {
            DisableState();

            var animationName = overrideAnimationNames.FirstOrDefault(x => x.overrideName.Equals(aniStateName))?.animationName;
            animationName = animationName == null || animationName.Equals("") ? aniStateName : animationName;

            currentAnimationName[0] = animationName;
            AnimationCoroutineStart(currentAnimationName[0], false, 0.25f,
                () =>
                {
                    if(LastStateName.ContainsKey(0))
                        SetState(LastStateName[0]);
                });
            return GetAnimationTime(animationName);
        }

        /// <summary>
        /// 애니메이션 설정
        /// </summary>
        public float SetAnimation(string aniStateName, bool loop, float mixDuration = 0.25f,
            UnityAction endAction = null, int layer = 0)
        {
            DisableState(layer);

            var animationName = overrideAnimationNames.FirstOrDefault(x => x.overrideName.Equals(aniStateName))?.animationName;
            animationName = animationName == null || animationName.Equals("") ? aniStateName : animationName;
            
            if(currentAnimationName.ContainsKey(layer))
                currentAnimationName[layer] = animationName;
            else
                currentAnimationName.Add(layer, animationName);
            
            AnimationCoroutineStart(currentAnimationName[layer], loop, mixDuration,
                endAction == null ? () =>
                {
                    if(LastStateName.ContainsKey(layer))
                        SetState(LastStateName[layer],0,0,mixDuration);
                } : endAction, layer);
            if (animationName == null || animationName.Equals(""))
                return 0;
            return GetAnimationTime(animationName);
        }

        void StopAllStateEvents()
        {
            if (_stateEvents != null && _stateEvents.Length > 0)
            {
                for (var i = 0; i < _stateEvents.Length; i++)
                    if (_stateEvents[i] != null)
                    {
                        StopCoroutine(_stateEvents[i]);
                        _stateEvents[i] = null;
                    }

                _stateEvents = null;
            }
        }

        /// <summary>
        /// ※Inspector 등록용※
        /// 상태 설정
        /// </summary>
        public void SetState(string stateName)
        {
            StopAllStateEvents();
            if (states.Count.Equals(0)) return;
            var state = stateName == null || stateName.Equals("") ? states[0] : states.First(x => x.name.Equals(stateName));
            
            StatePlay(state, 0);
            
            _stateEvents = new Coroutine[state.stateEvent.Length];
            for (var i = 0; i < _stateEvents.Length; i++)
                _stateEvents[i] = StartCoroutine(StateEventCoroutine(stateName, state.stateEvent[i],
                    state.stateEvent[i].minCycleTime, state.stateEvent[i].maxCycleTime));

            if (state._startEvent != null)
                state._startEvent.Invoke();

        }

        /// <summary>
        /// 상태 설정
        /// </summary>
        public void SetState(string stateName, int index = 0, int layer = 0, float mixDuration = 0.25f)
        {
            StopAllStateEvents();
            if (states.Count.Equals(0)) return;
            var state = stateName == null || stateName.Equals("") ? states[0] : states.First(x => x.name.Equals(stateName));

            StatePlay(state, index, layer, mixDuration);
            
            _stateEvents = new Coroutine[state.stateEvent.Length];
            for (var i = 0; i < _stateEvents.Length; i++)
                _stateEvents[i] = StartCoroutine(StateEventCoroutine(stateName, state.stateEvent[i],
                    state.stateEvent[i].minCycleTime, state.stateEvent[i].maxCycleTime));

            if (state._startEvent != null)
                state._startEvent.Invoke();
        }

        /// <summary>
        /// 상태 플레이
        /// </summary>
        private void StatePlay(SYSpineState state, int index, int layer = 0, float mixDuration = 0.25f)
        {
            var stateName = state.name;
            
            if(LastStateName.ContainsKey(layer))
                LastStateName[layer] = stateName;
            else
                LastStateName.Add(layer, stateName);
            
            if(CurrentStateName.ContainsKey(layer))
                CurrentStateName[layer] = stateName;
            else
                CurrentStateName.Add(layer, stateName);

            if (_currentSpineState.ContainsKey(layer) && _currentSpineState[layer] != null)
                _currentSpineState[layer].Dispose();

            if(currentAnimationName.ContainsKey(layer))
                currentAnimationName[layer] = state.animations[index].animationName;
            else
                currentAnimationName.Add(layer, state.animations[index].animationName);

            float time = state.animations[index].duration.Equals(0)
                ? GetAnimationTime(currentAnimationName[layer])
                : state.animations[index].duration;


            AnimationCoroutineStart(currentAnimationName[layer], true, mixDuration, null, layer);

            index = state.isRandom
                ? RandomNewInt(index, 0, state.animations.Length)
                : (index + 1) >= state.animations.Length
                    ? 0
                    : index + 1;
            
            if(_currentSpineState.ContainsKey(layer))
            {
                if(_currentSpineState[layer]!= null)
                    _currentSpineState[layer].Dispose();
                _currentSpineState[layer] =
                    DelayCallUtils.DelayCall(time, () => StatePlay(state, index, layer, mixDuration));
            }
            else
                _currentSpineState.Add(layer,
                    DelayCallUtils.DelayCall(time, () => StatePlay(state, index, layer, mixDuration)));
        }


        /// <summary>
        /// 애니메이션 코루틴 스타트
        /// </summary>
        /// <param name="aniName">플레이 할 애니메이션 이름</param>
        /// <param name="mixDuration">애니메이션 전환되는 시간</param>
        /// <param name="endAction">애니메이션 종료 후 액션</param>
        /// <param name="loop">애니메이션 반복 여부</param>
        private void AnimationCoroutineStart(string aniName, bool loop, float mixDuration = 0.25f,
            UnityAction endAction = null, int layer = 0)
        {
            if (aniProgress != null)
            {
                StopCoroutine(aniProgress);
                aniProgress = null;
            }

            _scheduledEvent.Dispose();

            //Debug.LogWarning("애니메이션 재생 = " + aniName);
            if(gameObject.activeInHierarchy)
                aniProgress = StartCoroutine(AnimationPlay(aniName, loop, mixDuration, endAction, layer));
        }

        IEnumerator StateEventCoroutine(string stateName, SYStateEvent stateEvent, float minCycleTime,
            float maxCycleTime)
        {
            minCycleTime = minCycleTime < 0 ? 0 : minCycleTime;
            maxCycleTime = maxCycleTime < minCycleTime ? minCycleTime : maxCycleTime;
            if (stateEvent.isInvokeStart)
                stateEvent._event.Invoke();

            while (true)
            {
                yield return new WaitForSeconds(Random.Range(minCycleTime, maxCycleTime));
                stateEvent._event.Invoke();
            }
        }

        /// <summary>
        /// 애니메이션 플레이 코루틴
        /// </summary>
        /// <param name="aniName">플레이 할 애니메이션 이름</param>
        /// <param name="mixDuration">애니메이션 전환되는 시간</param>
        /// <param name="endAction">애니메이션 종료 후 액션</param>
        /// <param name="loop">애니메이션 반복 여부</param>
        /// <returns></returns>
        IEnumerator AnimationPlay(string aniName, bool loop, float mixDuration = 0.25f,
            UnityAction endAction = null, int layer = 0)
        {
            do
            {
                this.Pause(false);
                AnimationState.TimeScale = _currentAnimationTimeScale;
                
                if (SkeletonGraphic)
                    SkeletonGraphic.timeScale = _currentAnimationTimeScale;
                //SetPauseAnimation(false);
                //yield return new WaitUntil(() => Manager().isLoadComplete);

                /*if (mixDuration) _currentAniamtionTrack.MixDuration = 0.25f;
                else _currentAniamtionTrack.MixDuration = 0f;*/

                if (aniName != "")
                {
                    if (_currentAnimationTrack.ContainsKey(layer))
                        _currentAnimationTrack[layer] =
                            AnimationState.SetAnimation(layer, aniName,
                                false);
                    else
                        _currentAnimationTrack.Add(layer,
                            AnimationState.SetAnimation(layer, aniName,
                                false));
                }
                else if (_currentAnimationTrack.ContainsKey(layer))
                    _currentAnimationTrack[layer] =
                        AnimationState.SetEmptyAnimation(layer, 0);
                else
                    _currentAnimationTrack.Add(layer,
                        AnimationState.SetEmptyAnimation(layer, 0));

                _currentAnimationTrack[layer].MixDuration = mixDuration;

                yield return new WaitForSpineAnimationComplete(_currentAnimationTrack[layer], true);

                if (!loop)
                {
                    endAction?.Invoke();
                    break;
                }


                if (aniName != currentAnimationName[layer])
                {
                    _scheduledEvent.Dispose();
                    break;
                }
            } while (loop);
        }

        public IEnumerator NowAnimationEndCheck(int layer = 0)
        {
            yield return new WaitForSpineAnimationComplete(_currentAnimationTrack[layer], true);
        }

        private void OnDisable()
        {
            _currentSpineState.Values.ToList().ForEach(x =>
            {
                if (x != null)
                    x.Dispose();
            });
            _currentSpineState.Clear();
            
            if (_scheduledEvent != null)
                _scheduledEvent.Dispose();
            StopAllCoroutines();
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

        public void SetAttachment(string slotName, string attachmentName) => SkeletonAnimation.skeleton.SetAttachment(slotName,attachmentName);

        /// <summary>
        /// ※Inspector 등록용※
        /// 사운드 재생
        /// </summary>
        public void PlaySfxFromKey(string key)
        {
            SoundLoader.PlaySfxFromKey(key);
        }
        public void PlayFromKey(string key)
        {
            SoundLoader.PlayFromKey(key);
        }
    }
}