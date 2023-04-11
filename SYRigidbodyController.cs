using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JSchool.Common.Script.Manager;
using JSchool.Common.Script.Manager.API;
using JSchool.Common.Script.Utils;
using JSchool.Common.Utils;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Events;

namespace JSchool.Modules.Common.OSY
{
    public class SYRigidbodyController : MonoBehaviour
    {
        [SerializeField] protected bool isBasedJob = true;
        [SerializeField] protected int jobBatchCount = 100;
        protected bool isUseGyro;
        public List<Rigidbody2D> targets;
        public Vector2 minBoundaryPos;
        public Vector2 maxBoundaryPos;
        public UnityAction<Rigidbody2D> onOutOfBoundary;
        
        public UnityAction onStartJob;
        public UnityAction onEndJob;

        public float gyroPower = 20;
        public float gravityPower = 1200;
        [SerializeField] protected float limitGyroVal = 1f;

        protected List<Rigidbody2D> activeTargets;
        protected Vector2 accel;

        
        private int _lastOrientation;

        private bool IsConvertingAcceleration =>
            (SystemInfoUtils.IsValidIOSVersion && SystemInfoUtils.IOSVersionMajorNumber < 15) &&
            _lastOrientation == 4;

        protected virtual void Awake()
        {
#if UNITY_EDITOR
            isUseGyro = false;
#else
            isUseGyro = true;
#endif
            _lastOrientation = GameMainManager.Instance.UIOrientation.Value;
        }

        IEnumerator Start()
        {
            Debug.Log("IOS버젼: "+SystemInfoUtils.IOSVersionMajorNumber);
            if(isBasedJob)
                Debug.Log("잡기반");
            else
            
                Debug.Log("단순반복");
            while (true)
            {
                Debug.Log(
                    $"Input.acceleration: {Input.acceleration.x}, {Input.acceleration.y}, {Input.acceleration.z}\r\n" +
                    $"Input.gyro.userAcceleration: {Input.gyro.userAcceleration.x}, {Input.gyro.userAcceleration.y}, {Input.gyro.userAcceleration.z}\r\n" +
                    $"Input.gyro.gravity: {Input.gyro.gravity.x}, {Input.gyro.gravity.y}, {Input.gyro.gravity.z}");
                yield return new WaitForSeconds(1);
            }
        }
        
        protected Vector2 _UpdateAcceleration()
        {
            var orientation = GameMainManager.Instance.UIOrientation.Value;
            if (orientation == 4 ||
                orientation == 3)
            {
                _lastOrientation = orientation;
            }
            // var gyroInput = Input.acceleration;
            // if (IsConvertingAcceleration)
            // {
            //     FWDebug.CLog("Convert Input.acceleration value");
            //     gyroInput *= -1;
            // }
            return APIManager.Instance.Acceleration.Value;
        }
        
        protected virtual void FixedUpdate()
        {
            var gyroInput = _UpdateAcceleration();
            
            onStartJob?.Invoke();
            activeTargets = targets.Where(x => x.gameObject.activeInHierarchy).ToList();
            if (activeTargets.Count.Equals(0)) return;

            if (!isBasedJob)
            {
                foreach (var activeTarget in activeTargets)
                {
                    var targetTransform = activeTarget.transform;
                    if (targetTransform.position.x < minBoundaryPos.x ||
                        targetTransform.position.x > maxBoundaryPos.x ||
                        targetTransform.position.y < minBoundaryPos.y || targetTransform.position.y > maxBoundaryPos.y)
                    {
                        onOutOfBoundary.Invoke(activeTarget);
                        break;
                    }

                    Vector2 temp;
                    Vector2 acceleration = APIManager.Instance.Acceleration.Value;
                    accel = isUseGyro && (temp = acceleration).magnitude >
                        limitGyroVal
                            ? temp * gyroPower
                            : Vector2.down * gravityPower;
                    activeTarget.velocity += accel * Time.deltaTime;
                }

                return;
            }

            NativeArray<Vector2> targetVelocityArray = new NativeArray<Vector2>(activeTargets.Count, Allocator.TempJob);
            NativeArray<Vector2> targetPosArray = new NativeArray<Vector2>(activeTargets.Count, Allocator.TempJob);
            NativeArray<bool> isOutOfBoundaryArray = new NativeArray<bool>(activeTargets.Count, Allocator.TempJob);
            for (var i = 0; i < activeTargets.Count; i++)
            {
                targetVelocityArray[i] = activeTargets[i].velocity;
                targetPosArray[i] = activeTargets[i].position;
            }
            
            new JobGyroPhysics(Time.deltaTime, isUseGyro, targetPosArray, targetVelocityArray, minBoundaryPos,
                    maxBoundaryPos, isOutOfBoundaryArray, gyroInput, isUseGyro ? gyroPower : gravityPower,
                    limitGyroVal)
                .Schedule(activeTargets.Count, jobBatchCount).Complete();

            for (var i = 0; i < activeTargets.Count; i++)
            {
                if (isOutOfBoundaryArray[i])
                {
                    onOutOfBoundary.Invoke(activeTargets[i]);
                    continue;
                }

                activeTargets[i].velocity = targetVelocityArray[i];
            }

            onEndJob?.Invoke();
            targetPosArray.Dispose();
            isOutOfBoundaryArray.Dispose();
            targetVelocityArray.Dispose();
        }

        protected struct JobGyroPhysics : IJobParallelFor
        {
            private float _deltaTime;
            private bool _isBasedGyro;

            private NativeArray<Vector2> _targetPos;
            private NativeArray<Vector2> _targetVelocity;

            private Vector2 _minBoundaryPos;
            private Vector2 _maxBoundaryPos;

            private Vector2 _inputAcceleration;

            private NativeArray<bool> _isOutOfBoundary;

            private float _power;
            private float _limitGyroVal;


            public JobGyroPhysics(float deltaTime, bool isBasedGyro, NativeArray<Vector2> targetPos,
                NativeArray<Vector2> targetVelocity,
                Vector2 minBoundaryPos, Vector2 maxBoundaryPos, NativeArray<bool> isOutOfBoundary,
                Vector2 inputAcceleration, float power, float limitGyroVal)
            {
                _deltaTime = deltaTime;
                _isBasedGyro = isBasedGyro;
                _targetPos = targetPos;
                _targetVelocity = targetVelocity;
                _minBoundaryPos = minBoundaryPos;
                _maxBoundaryPos = maxBoundaryPos;
                _isOutOfBoundary = isOutOfBoundary;
                _inputAcceleration = inputAcceleration;
                _power = power;
                _limitGyroVal = limitGyroVal;
            }

            public void Execute(int i)
            {
                if (_targetPos[i].x < _minBoundaryPos.x || _targetPos[i].x > _maxBoundaryPos.x ||
                    _targetPos[i].y < _minBoundaryPos.y || _targetPos[i].y > _maxBoundaryPos.y)
                {
                    _isOutOfBoundary[i] = true;
                    return;
                }

                _isOutOfBoundary[i] = false;

                Vector2 tempAccel;
                Vector2 accel = _isBasedGyro && (tempAccel = new Vector2(_inputAcceleration.x, _inputAcceleration.y)).magnitude > _limitGyroVal
                    ? tempAccel
                    : Vector2.down;
                _targetVelocity[i] += accel * _power * _deltaTime;
            }
        }
    }
}