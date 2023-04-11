using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace JSchool.Modules.Common.OSY
{
    //원하는 오브젝트가 특정 방향으로 계속 움직이게하고, 특정 위치를 벗어나면 위치값을 다시 초기화 하는 기능 제공
    public class SYInfiniteFlowSlider : MonoBehaviour
    {
        [Serializable]
        public class FlowTarget
        {
            public Transform transform;
            [NonSerialized] public TweenerCore<Vector3, Vector3, VectorOptions> flowTweens;
            public UnityAction onReset;

            public FlowTarget(Transform transform, UnityAction onReset = null)
            {
                this.transform = transform;
                this.onReset = onReset;
            }

            public FlowTarget(FlowTarget target)
            {
                this.transform = target.transform;
                this.flowTweens = target.flowTweens;
                this.onReset = target.onReset;
            }
        }
        [Serializable]
        public class SYFlow
        {
            public float speed;

            //endPos에 도착하면 초기화될 위치값 Min ~ Max
            public Vector3 lastTargetSidePosMin;
            public Vector3 lastTargetSidePosMax;
            public Transform endPos;
            public List<FlowTarget> targets;

        }

        public SYFlow[] flows;

        /*private void Start()
        {
            StartFlow();
        }*/

        public void StartFlow()
        {
            foreach (var syFlow in flows)
            foreach (var syFlowTarget in syFlow.targets)
                syFlowTarget.flowTweens = Flow(syFlow, syFlowTarget, syFlow.speed, 1);
        }

        public void SetPauseFlow(bool val)
        {
            if (val)
                foreach (var syFlow in flows)
                foreach (var target in syFlow.targets)
                    target.flowTweens.Pause();
            else
                foreach (var syFlow in flows)
                foreach (var target in syFlow.targets)
                    target.flowTweens.Play();
        }

        public void SetTimeScaleFlow(float val)
        {
            foreach (var syFlow in flows)
            foreach (var target in syFlow.targets)
                target.flowTweens.timeScale = val;
        }

        public TweenerCore<Vector3, Vector3, VectorOptions> Flow(SYFlow flow, FlowTarget target,
            float speed, float timeScale)
        {
            var result =
                target.transform.DOMove(flow.endPos.position, speed).SetEase(Ease.Linear).SetSpeedBased();
            result.OnComplete(() =>
            {
                var randomPos = new Vector3(
                    flow.lastTargetSidePosMin.x > flow.lastTargetSidePosMax.x ? flow.lastTargetSidePosMin.x:Random.Range(flow.lastTargetSidePosMin.x, flow.lastTargetSidePosMax.x),
                    flow.lastTargetSidePosMin.y > flow.lastTargetSidePosMax.y ? flow.lastTargetSidePosMin.y:Random.Range(flow.lastTargetSidePosMin.y, flow.lastTargetSidePosMax.y),
                    flow.lastTargetSidePosMin.z > flow.lastTargetSidePosMax.z ? flow.lastTargetSidePosMin.z:Random.Range(flow.lastTargetSidePosMin.z, flow.lastTargetSidePosMax.z));
                target.transform.position =
                    flow.targets[flow.targets.Count - 1].transform.position + randomPos;
                flow.targets.Insert(flow.targets.Count, target);
                flow.targets.RemoveAt(0);
                target.flowTweens = Flow(flow, target, speed, result.timeScale);
                StartCoroutine(FixPosition(flow, target, randomPos));
                target.onReset?.Invoke();
                // Tween안에서 UnityEvent.Invoke() 호출 시, Invoke호출도 취소되고 Tween자체가 끊어 지는 상황이 발생함..., 왜인지는 모르겠으나 UnityAction으로 해결됨
            });
            result.timeScale = timeScale;
            return result;
        }

        IEnumerator FixPosition(SYFlow flow, FlowTarget target, Vector3 randomPos)
        {
            float time = 0;
            do
            {
                target.transform.position =
                    flow.targets[flow.targets.Count - 2].transform.position + randomPos;
                time += Time.deltaTime;
                if (time > 2)
                    yield break;
                yield return new WaitForEndOfFrame();
            } while (true);
        }
    }
}