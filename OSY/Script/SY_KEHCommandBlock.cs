using System;
using System.Linq;
using JSchool.Common.Extensions;
using JSchool.Common.Script.BaseFramework.Lecture.Addressable.Sound;
using JSchool.Common.Utils;
using JSchool.Common.Utils.Lecture;
using JSchool.Modules.Common.KEH.Lecture.Coding.CommandBlock;
using JSchool.Modules.Common.LCH.Extensions;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JSchool.Modules.Common.OSY.Lecture.Coding.CommandBlock
{
    [Serializable]
    public class CommandInfo
    {
        public Image image;
        public int command;
    }
    public class SY_KEHCommandBlock : KEHCommandBlock, IPointerClickHandler
    {
        private Camera _cam;
        public bool isPlaced;
        public UnityEvent onClick;
        public UnityAction onClosePanel;

        public CommandInfo[] commandInfo;
        [SerializeField] private RectTransform clickArea;
        [SerializeField] private RectTransform commandPanel;
        public RectTransform ClickArea => clickArea;
        protected override void Awake()
        {
            base.Awake();
            _cam = Camera.main;
        }

        public void SetCommand(int val)
        {
            var temp = commandInfo.FirstOrDefault(x => x.command.Equals(val));
            iconImage.sprite = temp!.image.sprite;
            Command = temp!.command;
            SetOpenPanel(false);
        }
        public void OnPointerClick(PointerEventData eventData)
        {
            // 스크립트에 올려저 있지 않거나, 드래그가 되었다면, 클릭 영역을 누른게 아니라면 return.
            if (!isPlaced || eventData.dragging || !clickArea.ExContainsScreenPoint(eventData))
                return;

            onClick?.Invoke();
        }

        public void SetOpenPanel(bool val)
        {
            commandPanel.SetActive(val);
            if (val)
            {
                bool AllMouseButtonDown()
                {
                    return Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
                }
                ExpTweenUtils.Appear(commandPanel);
                // 외부 영역 클릭시 Hide처리.
                this.UpdateAsObservable()
                    .TakeUntilDisable(this)
                    .Where(_ => AllMouseButtonDown() && commandPanel.gameObject.activeInHierarchy)
                    .Select(_ => _cam.ScreenToWorldPoint(Input.mousePosition))
                    .Where(pos => UIUtils.GetWorldRect(commandPanel).Contains(pos) == false)
                    .Subscribe(_ => SetOpenPanel(false)); 
            }
            else
            {
                onClosePanel?.Invoke();
            }
        }
        public void PlaySfxFromKey(string name) => SoundLoader.PlaySfxFromKey(name);
        public void PlayFromKey(string name) => SoundLoader.PlayFromKey(name);
    }
}