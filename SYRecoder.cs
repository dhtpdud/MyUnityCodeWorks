using System;
using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using JSchool.Common.Script.BaseFramework.Lecture;
using JSchool.Common.Script.Manager;
using JSchool.Common.Script.Manager.API;
using JSchool.Common.Utils;
using UniRx;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
namespace JSchool.Modules.Common.OSY
{
    public class SYRecoder : MonoBehaviour, INeedShareData
    {
        public enum ERecordState
        {
            None,
            Ready,
            Recording,
            Complete,
            Cancel,
            Error
        }
        public enum ERecordAnimation
        {
            answering,
            complete_mic,
            error,
            intro_mic,
            listening,
            processing,
            waiting
        }
        [Serializable]
        public class Reaction
        {
            public string[] answerList;
            public UnityEvent onUnderstand;
            public Reaction(string[] answerList, UnityAction onUnderstand)
            {
                this.answerList = answerList;
                this.onUnderstand = new UnityEvent();
                this.onUnderstand.AddListener(onUnderstand);
            }
        }
        public Button buttonRecorder;
        public UnityEvent onClickRecordButton;
        public UnityEvent onFail;
        public UnityEvent onRecording;
        public UnityEvent onCancel;
        public UnityEvent onError;
        public UnityEvent onReady;
        public Reaction[] reactions;
        protected ShareData shareData;
        protected ERecordState currentRecordState;
        public ERecordState CurrentRecordState => currentRecordState;
        private int _recordVolume;
        [NonSerialized]public SYMonoSpineController spine;
        /*private Action<ERecordType, int, string> _action;
        public void Init(Action<ERecordType, int, string> action)
        {
            _action = action;
        }*/
        protected virtual void Awake()
        {
            if (!spine)
                spine = GetComponent<SYMonoSpineController>();
        }
        public async UniTask<bool> SetPermissionCheck()
        {
            var permission = await APIManager.Instance.CheckForAudioRecordPermission();
            return permission;
        }
        public ERecordState GetRecordType()
        {
            return currentRecordState;
        }
        public string lastPreResultString = String.Empty;
        public string lastRealResultString = String.Empty;
        [SerializeField] protected Image _micBackground;
        private ERecordState _lastRecordState = ERecordState.Ready;
        public void OnClickRecord()
        {
            if(shareData.HandGuide.Sequence() != null)
            {
                shareData.HandGuide.Stop();
                shareData.HandGuide.CreateNewSequenceIfNotExist();
            }
            shareData.HandGuide.Hide();
            if (currentRecordState == ERecordState.Complete || currentRecordState == ERecordState.Error)
                return;
            SoundManager.Instance.StopSfx();
            lastPreResultString = String.Empty;
            buttonRecorder.GetComponent<CanvasGroup>().blocksRaycasts = false;
            
            onClickRecordButton.Invoke();
            if (currentRecordState != ERecordState.Recording)
            {
                DelayCallUtils.DelayCall(0.8f, () => StartRecord());
            }
            else
                StartRecord();
        }
        public virtual async void StartRecord()
        {
            var permission = await SetPermissionCheck();
            if (_micBackground != null) _micBackground.color = !permission ? Color.gray : Color.white;
            //ResetCharacterSound(false);
            if (!permission)
            {
                buttonRecorder.GetComponent<CanvasGroup>().blocksRaycasts = true;
                return;
            }
            if (currentRecordState == ERecordState.Recording)
                StopRecord();
            else
            {
                var isConnect = await APIManager.Instance.StartForRecordAudio(OnChangeRecordStatus);
                if (!isConnect)
                {
                    OnRecordCallback(ERecordState.Error, 0, "");
                }
            }
        }
        public virtual async void StartRecord(string text)
        {
            var permission = await SetPermissionCheck();
            if (_micBackground != null) _micBackground.color = !permission ? Color.gray : Color.white;
            //ResetCharacterSound(false);
            if (!permission)
            {
                buttonRecorder.GetComponent<CanvasGroup>().blocksRaycasts = true;
                return;
            }
            if (currentRecordState == ERecordState.Recording)
                StopRecord();
            else
                RecordAction(text);
        }
        public void StopRecord()
        {
            APIManager.Instance.StopForRecordAudio();
            currentRecordState = ERecordState.Ready;
            OnRecordCallback(currentRecordState, 0, "");
        }
        protected virtual void OnChangeRecordStatus(string recordStatusType, string errorCode, int audioVolume,
            string translatedString)
        {
            string result = "";
            if (recordStatusType == RecordStatusType.Recording)
            {

    
          
            
    

          
    
    
  
                currentRecordState = ERecordState.Recording;
            }
            else if (recordStatusType == RecordStatusType.Complete)
            {
                currentRecordState = ERecordState.Complete;
                result = translatedString;
            }
            else if (recordStatusType == RecordStatusType.Cancel)
            {
                currentRecordState = ERecordState.Cancel;
            }
            else if (recordStatusType == RecordStatusType.Error)
            {
                currentRecordState = ERecordState.Error;
            }
            Debug.Log("녹음 상태 변경: "+recordStatusType);
            OnRecordCallback(currentRecordState, audioVolume, result);
        }
        public void OnRecordCallback(ERecordState type, int volume, string result)
        {
            if (type == ERecordState.Ready)
            {
                Debug.Log("녹음 준비");
                onReady.Invoke();
                spine.SetAnimation(ERecordAnimation.complete_mic.ToString(), false, 0.25f, () =>
                {
                    buttonRecorder.GetComponent<CanvasGroup>().blocksRaycasts = true;
                    buttonRecorder.interactable = true;
                    /*if (!_isNarraition && !_help && !_stageEnd)
                    {
                        PlaySound((int)EC0163AudioType.MicActive);
                    }*/
                });
                _recordVolume = volume;
                _lastRecordState = currentRecordState = type;
                /*ResetHint(true);
                ResetCharacterSound(true);*/
            }
            else if (type == ERecordState.Recording)
            {
                onRecording.Invoke();
                
                if (_lastRecordState == ERecordState.Ready)
                {
                    spine.SetAnimation(ERecordAnimation.intro_mic.ToString(), false, 0.25f,
                        () =>
                        {
                            buttonRecorder.GetComponent<CanvasGroup>().blocksRaycasts = true;
                            spine.SetState("Waiting");
                            if (_recordVolume == 0 && volume > 0)
                            {
                                spine.SetAnimation(ERecordAnimation.listening.ToString(), true);
                                _recordVolume = volume;
                            }
                        });
                }
                _lastRecordState = type;
            }
            else if (type == ERecordState.Complete)
            {
                Debug.Log("녹음 완료");
                if (!string.IsNullOrEmpty(result))
                    lastRealResultString = result;
                _lastRecordState = type;
                RecordAction(lastRealResultString);
            }
            else if (type == ERecordState.Cancel)
            {
                Debug.Log("녹음 취소");
                spine.SetAnimation("complete_mic");
                if (_lastRecordState == ERecordState.Complete)
                {
                    Debug.Log("녹음 완료 직후");
                    RecordAction(lastRealResultString);
                    return;
                }
                _lastRecordState = type;
                onCancel.Invoke();
                OnRecordCallback(ERecordState.Ready, 0, "");
            }
            else if (type == ERecordState.Error)
            {
                Debug.Log("녹음 오류");
                onError.Invoke();
                _lastRecordState = type;
                APIManager.Instance.StopForRecordAudio();
                StartCoroutine(ErrorCor());
            }
        }
        protected virtual IEnumerator ErrorCor()
        {
            buttonRecorder.interactable = false;
            spine.SetAnimation(ERecordAnimation.error.ToString(), false, 0.25f, () => spine.SetState("Processing"));
            yield return new WaitForSeconds(5);
            OnRecordCallback(ERecordState.Ready, 0, "");
            buttonRecorder.interactable = true;
            
        }
        protected virtual void RecordAction(string talk)
        {
            var action = reactions.FirstOrDefault(x => x.answerList.Contains(talk));
            if (action != null)
            {
                lastPreResultString = action.answerList[0];
                action.onUnderstand.Invoke();
                OnRecordCallback(ERecordState.Ready, 0, "");
            }
            else
                onFail.Invoke();
        }
        protected virtual void OnDestroy()
        {
            if (currentRecordState != ERecordState.Recording)
                APIManager.Instance.StopForRecordAudio();
        }
        public void LinkShareData(ShareData shareData) => this.shareData = shareData;
    }
}