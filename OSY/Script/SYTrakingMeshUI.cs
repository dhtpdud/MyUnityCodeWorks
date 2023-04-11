using System;
using UnityEngine;
using UnityEngine.UI;

namespace JSchool.Modules.Common.OSY
{
    //UI의 2D메쉬를 특정 오브젝트의 위치에 맞게 넓히거나 줄일 수 있는 기능 제공
    //크기가 유동적인 UI마스크를 만들고 싶을때 사용하면 유용
    public class SYTrakingMeshUI : MaskableGraphic
    {
        public enum MaskingSyncMode
        {
            SyncFit,
            SyncBigger
        }
        public bool isAutoUpdate = true;
        public Transform target;
        private Transform _transform;
        private RectTransform _rectTransform;
        private Vector4 _initRectInfo;

        public MaskingSyncMode syncMode;
        

        private Transform Transform
        {
            get => _transform ? _transform : _transform = transform;
        }
        private RectTransform RectTransform
        {
            get => _rectTransform ? _rectTransform : _rectTransform = rectTransform;
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            var r = GetPixelAdjustedRect();
            _initRectInfo = new Vector4(r.x, r.y, r.x + r.width, r.y + r.height);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            base.OnPopulateMesh(vh);
            if (target)
            {
                var targetPivot = target.position - Transform.position;
                switch (syncMode)
                {
                    case MaskingSyncMode.SyncFit:
                        vh.SetUIVertex(new UIVertex { position = new Vector3(targetPivot.x < _initRectInfo.x ? targetPivot.x : _initRectInfo.x, targetPivot.y < _initRectInfo.y ? targetPivot.y : _initRectInfo.y) , color = color},0);
                        vh.SetUIVertex(new UIVertex { position = new Vector3(targetPivot.x < _initRectInfo.x ? targetPivot.x : _initRectInfo.x, targetPivot.y > _initRectInfo.w ? targetPivot.y : _initRectInfo.w) , color = color},1);
                        vh.SetUIVertex(new UIVertex { position = new Vector3(targetPivot.x > _initRectInfo.z ? targetPivot.x : _initRectInfo.z, targetPivot.y > _initRectInfo.w ? targetPivot.y : _initRectInfo.w) , color = color},2);
                        vh.SetUIVertex(new UIVertex { position = new Vector3(targetPivot.x > _initRectInfo.z ? targetPivot.x : _initRectInfo.z, targetPivot.y < _initRectInfo.y ? targetPivot.y : _initRectInfo.y) , color = color},3);
                        break;
                    case MaskingSyncMode.SyncBigger:
                        UIVertex[] biggerVertices = new UIVertex[4]; 
                        vh.SetUIVertex(biggerVertices[0] = new UIVertex { position = new Vector3(targetPivot.x < _initRectInfo.x ? targetPivot.x : _initRectInfo.x, targetPivot.y < _initRectInfo.y ? targetPivot.y : _initRectInfo.y) , color = color},0);
                        vh.SetUIVertex(biggerVertices[1] = new UIVertex { position = new Vector3(targetPivot.x < _initRectInfo.x ? targetPivot.x : _initRectInfo.x, targetPivot.y > _initRectInfo.w ? targetPivot.y : _initRectInfo.w) , color = color},1);
                        vh.SetUIVertex(biggerVertices[2] = new UIVertex { position = new Vector3(targetPivot.x > _initRectInfo.z ? targetPivot.x : _initRectInfo.z, targetPivot.y > _initRectInfo.w ? targetPivot.y : _initRectInfo.w) , color = color},2);
                        vh.SetUIVertex(biggerVertices[3] = new UIVertex { position = new Vector3(targetPivot.x > _initRectInfo.z ? targetPivot.x : _initRectInfo.z, targetPivot.y < _initRectInfo.y ? targetPivot.y : _initRectInfo.y) , color = color},3);
                        _initRectInfo = new Vector4(biggerVertices[0].position.x,biggerVertices[0].position.y,biggerVertices[2].position.x,biggerVertices[2].position.y);
                        break;
                }
            }
        }

        public void ResetVertices()
        {
            var r = GetPixelAdjustedRect();
            _initRectInfo = new Vector4(r.x, r.y, r.x + r.width, r.y + r.height);
            
            SetVerticesDirty();
        }

        private void OnGUI()
        {
            if(isAutoUpdate)
                SetVerticesDirty();
        }
    }
}