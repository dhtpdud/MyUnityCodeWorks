using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using JSchool.SYLab;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JSchool.Modules.Common.OSY
{
#if UNITY_EDITOR
    //-제공 기능-
    //특정 오브젝트를 마우스를 따라다니게 하거나, 다른 오브젝트를 따라다니게 함
    //이동중 흔들리거나 간격을 유지하게 할 수 있음
    [CustomEditor(typeof(SYPositionChaser))]
    public class SYFollowTargetEditor : Editor
    {
        public SYPositionChaser selected;

        // Editor의 OnEnable은 에디터에서 오브젝트를 눌렀을때 호출됨
        private void OnEnable()
        {
            if (AssetDatabase.Contains(target))
                selected = null;
            else
                selected = target as SYPositionChaser;
        }

        public override void OnInspectorGUI()
        {
            if (selected == null)
                return;
            if(EditorApplication.isPlaying)
                selected.IsFollow = EditorGUILayout.Toggle("추적 On/Off", selected.IsFollow);
            base.OnInspectorGUI();

            selected.isTargetTouch = EditorGUILayout.Toggle("터치 추적 여부", selected.isTargetTouch);
            if (!selected.isTargetTouch)
                selected.toOBJ =
                    (Transform)EditorGUILayout.ObjectField("\t추적할 타겟", selected.toOBJ, typeof(Transform), true);
            else
                selected.toOBJ = null;

            selected.isHoldOriginPosition = EditorGUILayout.Toggle("시작 위치 고정 여부", selected.isHoldOriginPosition);
            if (selected.isHoldOriginPosition)
                selected.maxMove = EditorGUILayout.FloatField("\t최대 움직임", selected.maxMove);

            selected.isMoveAnimation = EditorGUILayout.Toggle("흔들림 애니메이션", selected.isMoveAnimation);
            if (selected.isMoveAnimation)
            {
                selected.animationInterval = EditorGUILayout.FloatField("\t애니메이션 간격", selected.animationInterval);
                selected.shakePower = EditorGUILayout.FloatField("\t진도", selected.shakePower);
            }
        }
    }
#endif
    public class SYPositionChaser : MonoBehaviour
    {
        private Rigidbody2D _rigidbody;
        [SerializeField] private bool _isFollow;
        private List<Vector2> _originPositions = new List<Vector2>();
        private List<Vector2> _originLocalPositions = new List<Vector2>();
        public Transform[] fromOBJs;
        private List<Rigidbody2D> fromRigidbodys = new List<Rigidbody2D>();

        [HideInInspector] public Transform toOBJ;
        private Vector2 _toPosition;
        [HideInInspector] public bool isTargetTouch;

        [HideInInspector] public bool isHoldOriginPosition;
        [HideInInspector] public float maxMove;
        public float maxMoveX;
        public float maxMoveY;

        public bool IsFollow
        {
            get => _isFollow;
            set
            {
                _isFollow = value;
                
                if (_isFollow)
                    DoFollow();
                else
                {
                    if (_doFollowCoroutine != null)
                        for (var i = 0; i < _doFollowCoroutine.Length; i++)
                        {
                            if (_doFollowCoroutine[i] != null)
                            {
                                StopCoroutine(_doFollowCoroutine[i]);
                                _doFollowCoroutine[i] = null;
                            }
                        }

                    if (_stopFollowCoroutine == null)
                        _stopFollowCoroutine = StartCoroutine(StopFollow());
                }
            }
        }

        private Coroutine[] _doFollowCoroutine;
        private Coroutine _stopFollowCoroutine;
        public float stopDistance;
        public float holdDistance;

        [Tooltip("0 == 타겟과 위치 즉시 동기화")] public float followSpeed = 1;
        public float initSpeed = 1;
        public float accelationSpeed = 1;
        public float breakPower = 1;

        [HideInInspector] public bool isMoveAnimation;
        [HideInInspector] public float animationInterval = 200;
        [HideInInspector] public float shakePower = 0.5f;
        [HideInInspector] public Camera cam;

        public UnityEvent onTouchBegin;
        public UnityEvent onTouchEnd;
        public UnityEvent onShake;

        public void Awake()
        {
            cam = Camera.main;
            for (var i = 0; i < fromOBJs.Length; i++)
            {
                fromRigidbodys.Add(fromOBJs[i].GetComponent<Rigidbody2D>());
                _originPositions.Add(fromOBJs[i].position);
                _originLocalPositions.Add(fromOBJs[i].parent
                    ? fromOBJs[i].position - fromOBJs[i].parent.position
                    : fromOBJs[i].position);
            }

            _doFollowCoroutine = new Coroutine[fromOBJs.Length];
            IsFollow = _isFollow;
        }

        private void OnMouseDown()
        {
            if (isTargetTouch) 
                IsFollow = true;
        }

        private void DoFollow()
        {
            if (_stopFollowCoroutine != null)
            {
                StopCoroutine(_stopFollowCoroutine);
                _stopFollowCoroutine = null;
            }

            for (var i = 0; i < fromOBJs.Length; i++)
            {
                if (_doFollowCoroutine == null) return;
                if (_doFollowCoroutine[i] != null)
                {
                    StopCoroutine(_doFollowCoroutine[i]);
                    _doFollowCoroutine[i] = null;
                }
                _doFollowCoroutine[i] = StartCoroutine(DoFollowCoroutine(i));
            }
        }

        IEnumerator DoFollowCoroutine(int i)
        {
            float accelVal = 0;
            float currentAnimationInterval = animationInterval;
            Vector2 preFromPosition =
                fromRigidbodys[i] == null ? (Vector2)fromOBJs[i].position : fromRigidbodys[i].position;
            Transform child = fromOBJs[i].GetChild(0);
            Tweener animation = null;

            bool isLoop = true;


            do
            {
                if (isTargetTouch)
                {
#if UNITY_EDITOR || UNITY_STANDALONE
                    isLoop = Input.GetMouseButton(0);
#elif UNITY_ANDROID || UNITY_IOS
                isLoop = Input.touchCount > 0;
#endif
                    if (isLoop)
                        onTouchBegin.Invoke();
                    else
                        break;
                }
                
                if (toOBJ)
                    _toPosition = toOBJ.position;
                else
                {
#if UNITY_EDITOR || UNITY_STANDALONE
                    _toPosition = cam.ScreenToWorldPoint(Input.mousePosition);
#elif UNITY_ANDROID || UNITY_IOS
                    _toPosition = cam.ScreenToWorldPoint(Input.GetTouch(0).position);
#endif
                }

                //var currentLocalPosition = _originLocalPositions[i] - (Vector2)fromOBJs[i].parent.position;
                var localPosition = fromOBJs[i].parent
                    ? fromOBJs[i].position - fromOBJs[i].parent.position
                    : fromOBJs[i].position;
                var fromPosition = !isHoldOriginPosition
                    ? fromOBJs[i].parent
                        ? (Vector2)fromOBJs[i].parent.position + (Vector2)localPosition
                        : (Vector2)localPosition
                    : fromOBJs[i].parent
                        ? (Vector2)fromOBJs[i].parent.position + _originLocalPositions[i]
                        : _originLocalPositions[i];

                var toFromVector = fromPosition - _toPosition;
                var fromToVector = _toPosition + toFromVector.normalized * holdDistance - fromPosition;

                Vector2 destinationVector = fromToVector + fromPosition;
                if (isHoldOriginPosition && fromToVector.magnitude > maxMove)
                    destinationVector = fromToVector.normalized * maxMove + fromPosition;
                if (destinationVector.x > fromToVector.x + maxMoveX)
                    destinationVector.x = fromToVector.x + maxMoveX;
                if (destinationVector.y > fromToVector.y + maxMoveY)
                    destinationVector.y = fromToVector.y + maxMoveY;


                if (toFromVector.magnitude <= stopDistance)
                {
                    if (accelVal > 0)
                        accelVal -= breakPower * Time.deltaTime;
                    accelVal = accelVal < 0 ? 0 : accelVal;
                }
                else
                {
                    if (accelVal < 1)
                        accelVal += accelationSpeed * Time.deltaTime;
                    accelVal = accelVal > 1 ? 1 : accelVal;
                }

                if (fromRigidbodys[i] == null)
                {
                    if (followSpeed.Equals(0))
                        fromOBJs[i].position = destinationVector;
                    else
                    {
                        fromOBJs[i].position =
                            Vector2.Lerp(fromOBJs[i].position, destinationVector,
                                followSpeed * accelVal * Time.deltaTime);
                    }
                }
                else
                {
                    if (followSpeed.Equals(0))
                        fromRigidbodys[i].MovePosition(destinationVector);
                    else
                    {
                        fromRigidbodys[i].MovePosition(
                            Vector2.Lerp(fromRigidbodys[i].position, destinationVector,
                                followSpeed * accelVal * Time.deltaTime));
                    }
                }

                if (isMoveAnimation)
                {
                    var difference =
                        ((fromRigidbodys[i] == null ? (Vector2)fromOBJs[i].position : fromRigidbodys[i].position) -
                         preFromPosition).magnitude;
                    currentAnimationInterval -= difference;
                    //Debug.Log(currentAnimationInterval);
                    if (currentAnimationInterval < 0)
                    {
                        if (animation != null && !animation.IsComplete())
                            animation.Complete();
                        animation = child.DOShakePosition(0.5f, Vector2.one * difference * shakePower).SetRelative(true)
                            .OnComplete(() => child.localPosition = Vector3.zero);
                        onShake.Invoke();

                        currentAnimationInterval = animationInterval;
                    }
                }

                preFromPosition = (fromRigidbodys[i] == null
                    ? (Vector2)fromOBJs[i].position
                    : fromRigidbodys[i].position);

                yield return SYUtil.YieldInstructionCache.WaitForEndOfFrame;
            } while (isLoop);
            if(isTargetTouch)
                onTouchEnd.Invoke();
        }

        IEnumerator StopFollow()
        {
            while (true)
            {
                for (var i = 0; i < fromOBJs.Length; i++)
                    if (fromRigidbodys[i] == null)
                    {
                        fromOBJs[i].position = Vector2.Lerp(fromOBJs[i].position,
                            (Vector2)fromOBJs[i].parent.position + _originLocalPositions[i],
                            initSpeed * Time.deltaTime);
                        if (Vector2.Distance(fromOBJs[i].position,
                                (Vector2)fromOBJs[i].parent.position + _originLocalPositions[i]) < 0.05f)
                        {
                            fromOBJs[i].position = (Vector2)fromOBJs[i].parent.position + _originLocalPositions[i];
                            yield break;
                        }
                    }
                    else
                    {
                        fromRigidbodys[i].MovePosition(Vector2.Lerp(fromRigidbodys[i].position,
                            (Vector2)fromOBJs[i].parent.position + _originLocalPositions[i],
                            initSpeed * Time.deltaTime));
                        if (Vector2.Distance(fromRigidbodys[i].position,
                                (Vector2)fromOBJs[i].parent.position + _originLocalPositions[i]) < 0.05f)
                        {
                            fromRigidbodys[i]
                                .MovePosition((Vector2)fromOBJs[i].parent.position + _originLocalPositions[i]);
                            yield break;
                        }
                    }

                yield return SYUtil.YieldInstructionCache.WaitForEndOfFrame;
            }
        }

        private void OnDisable()
        {
            if (_stopFollowCoroutine != null)
            {
                StopCoroutine(_stopFollowCoroutine);
                _stopFollowCoroutine = null;
            }

            if (_doFollowCoroutine != null)
                for (var i = 0; i < _doFollowCoroutine.Length; i++)
                {
                    if (_doFollowCoroutine[i] != null)
                    {
                        StopCoroutine(_doFollowCoroutine[i]);
                        _doFollowCoroutine[i] = null;
                    }
                }
        }
    }
}