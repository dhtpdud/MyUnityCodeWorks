using System.Linq;
using JSchool.Modules.Common.LCH.Extensions;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Random = UnityEngine.Random;

namespace JSchool.Modules.Common.OSY
{
    public class SYJobTest_TargetRegister : MonoBehaviour
    {
        [SerializeField] private Transform targetsParent;
        [SerializeField] private bool isBasedJob;
        [SerializeField] private SYRigidbodyController _controller;
        [SerializeField] private Vector2 minSpawnPos;
        [SerializeField] private Vector2 maxSpawnPos;

        [SerializeField] private int maxCount;
        [SerializeField] private int currentCount;

        private void Awake()
        {
            _controller.targets = targetsParent.GetComponentsInChildren<Rigidbody2D>(true).ToList();
            currentCount = 0;
            _controller.onOutOfBoundary += (x) =>
            {
                x.gameObject.SetActive(false);
                currentCount--;
            };
        }

        private void Update()
        {
            if (!maxCount.Equals(-1) && currentCount >= maxCount)
                return;

            var clickableObjects = _controller.targets.Where(x => !x.gameObject.activeInHierarchy).ToArray();
            if (clickableObjects.Length.Equals(0)) return;

            if (!isBasedJob)
            {
                foreach (var clickableObject in clickableObjects)
                {
                    if (currentCount >= maxCount) break;
                    clickableObject.velocity *= 0;
                    clickableObject.transform.position = new Vector2(
                        UnityEngine.Random.Range(minSpawnPos.x, maxSpawnPos.x),
                        UnityEngine.Random.Range(minSpawnPos.y, maxSpawnPos.y));
                    clickableObject.SetActive(true);
                    currentCount++;
                }

                return;
            }

            var targetsVelocityArray = new NativeArray<Vector2>(clickableObjects.Length, Allocator.TempJob);
            var targetsPositionArray = new NativeArray<Vector2>(clickableObjects.Length, Allocator.TempJob);
            var targetsSetActiveArray = new NativeArray<bool>(clickableObjects.Length, Allocator.TempJob);
            var nativeCurrentCount = new NativeArray<int>(1, Allocator.TempJob);

            for (var i = 0; i < clickableObjects.Length; i++)
            {
                targetsVelocityArray[i] = clickableObjects[i].velocity;
                targetsPositionArray[i] = clickableObjects[i].position;
            }

            nativeCurrentCount[0] = currentCount;

            new JobResetObjects(targetsVelocityArray, targetsPositionArray, targetsSetActiveArray,
                    nativeCurrentCount,
                    minSpawnPos, maxSpawnPos, maxCount, (uint)Random.Range(0, int.MaxValue))
                //※반복횟수와 batch값을 통일한 이유:
                //batch를 통해 job을 나누게 되면, 다음프레임으로 currentCount값이 넘어감. 따라서, 이전 프레임과 currentCount값에 오차가 생김
                //batch값을 반복횟수와 통일시킴으로써 한프레임안에 완료시킴
                .Schedule(clickableObjects.Length, clickableObjects.Length).Complete();

            currentCount = nativeCurrentCount[0];
            for (var i = 0; i < clickableObjects.Length; i++)
            {
                clickableObjects[i].velocity = targetsVelocityArray[i];
                clickableObjects[i].transform.position = targetsPositionArray[i];
                clickableObjects[i].SetActive(targetsSetActiveArray[i]);
            }


            nativeCurrentCount.Dispose();
            targetsVelocityArray.Dispose();
            targetsPositionArray.Dispose();
            targetsSetActiveArray.Dispose();
        }

        public struct JobResetObjects : IJobParallelFor
        {
            private NativeArray<Vector2> _velocity;
            private NativeArray<Vector2> _targetPos;
            private Vector2 _minSpawnPos;
            private Vector2 _maxSpawnPos;
            private NativeArray<bool> _setActive;
            private int _maxCount;
            [NativeDisableParallelForRestriction] private NativeArray<int> _currentCount;


            private Unity.Mathematics.Random _random;

            public JobResetObjects(NativeArray<Vector2> velocity, NativeArray<Vector2> targetPos,
                NativeArray<bool> setActive, NativeArray<int> currentCount, Vector2 minSpawnPos, Vector2 maxSpawnPos,
                int maxCount, uint randomSeed)
            {
                _velocity = velocity;
                _targetPos = targetPos;
                _setActive = setActive;
                _maxCount = maxCount;
                _currentCount = currentCount;
                _minSpawnPos = minSpawnPos;
                _maxSpawnPos = maxSpawnPos;
                _random = new Unity.Mathematics.Random(randomSeed);
            }

            public void Execute(int i)
            {
                if (_currentCount[0] >= _maxCount)
                {
                    _setActive[i] = false;
                    return;
                }

                _velocity[i] *= 0;
                _targetPos[i] = new Vector2(_random.NextFloat(_minSpawnPos.x, _maxSpawnPos.x),
                    _random.NextFloat(_minSpawnPos.y, _maxSpawnPos.y));
                _setActive[i] = true;
                _currentCount[0]++;
            }
        }
    }
}