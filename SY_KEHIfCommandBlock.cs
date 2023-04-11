using System.Collections;
using System.Linq;
using DG.Tweening;
using JSchool.Common.Extensions;
using JSchool.Common.Script.BaseFramework.Lecture;
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
    public class SY_KEHIfCommandBlock : KEHCommandBlockContainer, IPointerClickHandler, INeedShareData
    {
        public enum IfCommandBlockState
        {
            None,
            True,
            False
        }
        private SYIfCodingShareData _shareData;
        private Camera _cam;
        [HideInInspector]public bool isPlaced;
        public UnityEvent onClick;
        public UnityEvent onClosePanel;

        [HideInInspector] public int currentIfType;
        [Header("UI 컨포넌트")]
        [SerializeField] private Image highlightTrue;
        [SerializeField] private Image highlightFalse;
        [SerializeField] private Image iconTrue;
        [SerializeField] private Image iconFalse;
        
        [SerializeField] private RectTransform clickArea;
        public RectTransform ifPanel;
        public Button[] ifPanelButtons;
        public RectTransform ClickArea => clickArea;
        public virtual Rect ClickAreaRect => UIUtils.GetWorldRect(clickArea);
        private IfCommandBlockState _state;
        public Image IconImage => iconImage;

        public virtual IfCommandBlockState State
        {
            get => _state;
            set
            {
                _state = value;
                switch (value)
                {
                    case IfCommandBlockState.None:
                        highlightTrue.DOFillAmount(0, 0).From(0);
                        highlightFalse.DOFillAmount(0, 0).From(0);
                        iconTrue.gameObject.SetActive(false);
                        iconFalse.gameObject.SetActive(false);
                        break;
                    case IfCommandBlockState.True:
                        highlightTrue.DOFillAmount(1, 0).From(1);
                        highlightFalse.DOFillAmount(0, 0).From(0);
                        iconTrue.gameObject.SetActive(true);
                        iconFalse.gameObject.SetActive(false);
                        break;
                    case IfCommandBlockState.False:
                        highlightTrue.DOFillAmount(0, 0).From(0);
                        highlightFalse.DOFillAmount(1, 0).From(1);
                        iconTrue.gameObject.SetActive(false);
                        iconFalse.gameObject.SetActive(true);
                        break;
                }
            }
        }
        protected override void Awake()
        {
            base.Awake();
            _cam = Camera.main;
        }

        IEnumerator Start()
        {
            yield return new WaitUntil(() => _shareData);
            SetIfType(0);
        }

        public override void SetChildBlock(KEHCommandBlock block)
        {
            if (ChildBlock)
            {
                var oldCollider = ChildBlock.GetComponentInChildren<Collider2D>();
                if(oldCollider)
                    oldCollider.enabled = true;
            }
            base.SetChildBlock(block);
            if (ChildBlock)
            {
                var newCollider = ChildBlock.GetComponentInChildren<Collider2D>();
                if(newCollider)
                    newCollider.enabled = false;
            }
            childBlockConnector.SetActive(!ChildBlock);
        }

        public void SetIfType(int val)
        {
            var temp = _shareData.ifTypeInfo.FirstOrDefault(x => x.type.Equals(val));
            iconImage.sprite = temp!.icon.sprite;
            currentIfType = temp!.type;
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
            ifPanel.SetActive(val);
            if (val)
            {
                bool AllMouseButtonDown()
                {
                    return Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
                }
                ExpTweenUtils.Appear(ifPanel);
                // 외부 영역 클릭시 Hide처리.
                this.UpdateAsObservable()
                    .TakeUntilDisable(this)
                    .Where(_ => AllMouseButtonDown() && ifPanel.gameObject.activeInHierarchy)
                    .Select(_ => _cam.ScreenToWorldPoint(Input.mousePosition))
                    .Where(pos => UIUtils.GetWorldRect(ifPanel).Contains(pos) == false)
                    .Subscribe(_ => SetOpenPanel(false)); 
            }
            else
            {
                onClosePanel.Invoke();
            }
        }
        public void PlaySfxFromKey(string name) => SoundLoader.PlaySfxFromKey(name);
        public void PlayFromKey(string name) => SoundLoader.PlayFromKey(name);
        public void LinkShareData(ShareData shareData)
        {
            _shareData = (SYIfCodingShareData)shareData;
        }
    }
}