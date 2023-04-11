using System;
using JSchool.Common.Script.BaseFramework.Lecture;
using UnityEngine;

namespace JSchool.Modules.Common.OSY.Lecture.Coding.CommandBlock
{
    public class SYIfCodingShareData : ShareData
    {
        [Serializable]
        public class IfInfo
        {
            public SpriteRenderer icon;
            public int type;
        }

        public IfInfo[] ifTypeInfo;
    }
}