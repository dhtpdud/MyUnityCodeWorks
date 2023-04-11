using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using JSchool.Common.Extensions;
using JSchool.Common.Script.BaseFramework.Touch;
using JSchool.Common.Utils;
using JSchool.Common.Utils.Lecture;
using JSchool.Modules.Common.KEH.Lecture.Coding;
using JSchool.Modules.Common.KEH.Lecture.Coding.CommandBlock;
using JSchool.Modules.Common.LCH.Extensions;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JSchool.Modules.Common.OSY.Lecture.Coding
{
    public interface IMultiStreamCodingController
    {
        public IObservable<KEHCommandBlock> OnBlockAddToScript { get; }
        public IObservable<KEHCommandBlock> OnBlockRemoveFromScript { get; }
        public IObservable<KEHCommandBlock> OnBlockCreate { get; }
        public IObservable<KEHCommandBlock> OnBlockDestroy { get; }

        public KEHCommandBlock[] RootBlock { get; }
        public IEnumerable<KEHCommandBlock> GetBlocks(int streamIndex);
        public void DestroyAllBlock(int streamIndex);
        public void SetLockStream(int streamIndex, bool val);
    }

    public class SY_KEHMultiStreamCodingController : MonoBehaviour, IMultiStreamCodingController
    {
        [SerializeField] private CommandAndCommandBlockPrefab[] commandAndCommandBlocks;

        [SerializeField] private KEHCommandBlockPanel blockPanel;
        [SerializeField] private List<Image> scriptAreasImages = new List<Image>();
        [SerializeField] private List<KEHCodingScriptArea> scriptAreas = new List<KEHCodingScriptArea>();
        [SerializeField] private Image[] streamLockImages;

        [SerializeField] private Transform dragParent;

        [SerializeField] private KEHSimulationBlockController simulationBlockController;

        public KEHCommandBlock[] CommandBlockRoot => scriptAreas.ToArray();

        private KEHCommandBlockCreator _blockCreator;
        public KEHCommandBlockPanel BlockPanel => blockPanel;
        private Sequence _blockUpdateSizeAndPositionSequence;

        private readonly Subject<KEHCommandBlock> _onBlockAddToScript = new Subject<KEHCommandBlock>();
        private readonly Subject<KEHCommandBlock> _onBlockRemoveFromScript = new Subject<KEHCommandBlock>();
        private readonly Subject<KEHCommandBlock> _onBlockCreate = new Subject<KEHCommandBlock>();
        private readonly Subject<KEHCommandBlock> _onBlockDestroy = new Subject<KEHCommandBlock>();

        public IObservable<KEHCommandBlock> OnBlockAddToScript => _onBlockAddToScript;
        public IObservable<KEHCommandBlock> OnBlockRemoveFromScript => _onBlockRemoveFromScript;
        public IObservable<KEHCommandBlock> OnBlockCreate => _onBlockCreate;
        public IObservable<KEHCommandBlock> OnBlockDestroy => _onBlockDestroy;
        public KEHCommandBlock[] RootBlock => scriptAreas.ToArray();

        private void Start()
        {
            _blockCreator = new KEHCommandBlockCreator(commandAndCommandBlocks);
            blockPanel.Initialize(_blockCreator);

            var sampleBlocks =
                blockPanel.SetupSampleBlocks(commandAndCommandBlocks.Select(item => item.command).ToArray());

            foreach (var sampleBlock in sampleBlocks)
            {
                AddDragEvent(sampleBlock.GetComponent<DragEventObject>());
                _onBlockCreate.OnNext(sampleBlock);
            }
        }

        private void AddDragEvent(DragEventObject dragEventObject)
        {
            dragEventObject.OnBeginDragEvent += OnBlockBeginDrag;
            dragEventObject.OnDragEvent += OnBlockDrag;
            dragEventObject.OnEndDragEvent += OnBlockEndDrag;
            dragEventObject.OnEndDragEvent += OnBlockFirstEndDrag;
        }

        /*
        private void RemoveDragEvent(DragEventObject dragEventObject)
        {
            dragEventObject.OnBeginDragEvent -= OnBlockBeginDrag;
            dragEventObject.OnDragEvent -= OnBlockDrag;
            dragEventObject.OnEndDragEvent -= OnBlockEndDrag;
        }
        */

        private void OnBlockBeginDrag(PointerEventData eventData, DragEventObject dragEventObject)
        {
            var block = dragEventObject.GetComponent<KEHCommandBlock>();

            if (block.ParentBlock)
            {
                var stream = (KEHCodingScriptArea)block.FindRoot();
                KEHCommandBlock.RemoveBlock(block);
                _onBlockRemoveFromScript.OnNext(block);
                stream.UpdateTempSize();
            }

            block.transform.SetParent(dragParent);

            simulationBlockController.SetupSimulationBlock(block);
        }

        public KEHCodingScriptArea GetClosestArea(KEHCommandBlock targetBlock)
        {
            var connectPoint = UIUtils.GetWorldRect(targetBlock.transform as RectTransform).center;
            float minDistance = float.MaxValue;
            KEHCodingScriptArea closestArea = null;
            for (var i = 0; i < scriptAreasImages.Count; i++)
            {
                if(IsLockStream(i)) continue;
                float tempDistance = Vector2.Distance(connectPoint, scriptAreasImages[i].transform.position);
                if (tempDistance < minDistance)
                {
                    minDistance = tempDistance;
                    closestArea = scriptAreas[i];
                }
            }

            return closestArea;
        }

        private void OnBlockDrag(PointerEventData eventData, DragEventObject dragEventObject)
        {
            var block = dragEventObject.GetComponent<KEHCommandBlock>();
            var worldPoint = dragEventObject.ScreenToWorldPosition(eventData);

            if (simulationBlockController.IsSimulationBlockConnected)
            {
                RemoveSimulationBlock();
            }

            var closestArea = GetClosestArea(block);
            var connectState = closestArea.TryConnect(simulationBlockController.CurrentSimulationBlock, block,
                worldPoint, true, true);

            simulationBlockController.CurrentSimulationBlock.gameObject.SetActive(connectState);
            if (!connectState)
                simulationBlockController.CurrentSimulationBlock.transform.SetParent(closestArea.transform);

            UpdateSizeAndChildrenPosition(true, closestArea);
        }

        private void OnBlockEndDrag(PointerEventData eventData, DragEventObject dragEventObject)
        {
            var block = dragEventObject.GetComponent<KEHCommandBlock>();
            var worldPoint = dragEventObject.ScreenToWorldPosition(eventData);

            var closestArea = GetClosestArea(block);
            
            if (simulationBlockController.IsSimulationBlockConnected)
            {
                RemoveSimulationBlock();

                simulationBlockController.CurrentSimulationBlock.transform.SetParent(closestArea.transform);
                simulationBlockController.CurrentSimulationBlock.gameObject.SetActive(false);
            }


            var connectState = closestArea.TryConnect(block, block, worldPoint, true, true);

            if (!connectState)
                DestroyBlock(block);
            else
            {
                _onBlockAddToScript.OnNext(block);
            }

            UpdateSizeAndChildrenPosition(true, closestArea);
        }

        private void RemoveSimulationBlock()
        {
            var simulationBlock = simulationBlockController.CurrentSimulationBlock;
            var rootStream = (KEHCodingScriptArea)simulationBlock.FindRoot();
            var childBlock = simulationBlockController.CurrentSimulationBlock.ChildBlock;
            if (childBlock)
            {
                simulationBlock.SetChildBlock(null);
                simulationBlock.SetNextBlock(childBlock, true);
            }

            KEHCommandBlock.RemoveBlock(simulationBlock);
            rootStream.UpdateTempSize();
        }

        private void OnBlockFirstEndDrag(PointerEventData eventData, DragEventObject dragEventObject)
        {
            dragEventObject.OnEndDragEvent -= OnBlockFirstEndDrag;
            var dragBlock = dragEventObject.GetComponent<KEHCommandBlock>();

            var block = blockPanel.CreateSampleBlock(dragBlock.Command);
            ExpTweenUtils.Appear(block.transform);
            AddDragEvent(block.GetComponent<DragEventObject>());
            _onBlockCreate.OnNext(block);
        }

        private void DestroyBlock(KEHCommandBlock block)
        {
            _onBlockDestroy.OnNext(block);
            Destroy(block.gameObject);
        }

        public IEnumerable<KEHCommandBlock> GetBlocks(int streamIndex) => scriptAreas[streamIndex].ChildBlock
            ? scriptAreas[streamIndex].ChildBlock
            : Enumerable.Empty<KEHCommandBlock>();

        public void DestroyAllBlock(int streamIndex)
        {
            var blocks = scriptAreas[streamIndex].ToArray();

            foreach (var block in blocks)
            {
                if (block == scriptAreas[streamIndex])
                    continue;

                DestroyBlock(block);
            }

            scriptAreas[streamIndex].SetChildBlock(null);
        }

        public void SetLockStream(int streamIndex, bool val)
        {
            if(streamLockImages.Length > 0)
                streamLockImages[streamIndex].SetActive(val);
        }
        public bool IsLockStream(int streamIndex)
        {
            if(streamLockImages.Length > 0)
                return streamLockImages[streamIndex].gameObject.activeInHierarchy;
            else
                return false;
        }

        private void UpdateSizeAndChildrenPosition(bool withAnimation, KEHCodingScriptArea stream)
        {
            _blockUpdateSizeAndPositionSequence.ExKillIfActive();
            _blockUpdateSizeAndPositionSequence = stream.UpdateSizeAndChildrenPosition(withAnimation);
        }
        public void UpdateSizeAndChildrenPosition(bool withAnimation, int streamIndex)
        {
            _blockUpdateSizeAndPositionSequence.ExKillIfActive();
            _blockUpdateSizeAndPositionSequence = scriptAreas[streamIndex].UpdateSizeAndChildrenPosition(withAnimation);
        }
    }
}