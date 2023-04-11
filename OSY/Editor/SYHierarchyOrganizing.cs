using System.Collections.Generic;
using System.Linq;
using JSchool.Modules.Common.LCH.Attributes;
using UnityEngine;

namespace JSchool.SYLab
{
    public class SYHierarchyOrganizing : MonoBehaviour
    {
        public Transform[] targets;

        [ShowInInspector]
        public void DoOrganizing()
        {
            foreach (var target in targets)
            {
                List<Transform> childs = new List<Transform>();
                for (int i = 0; i < target.childCount; i++)
                    childs.Add(target.GetChild(i));
                childs = childs.OrderBy(x => x.name.Length).ToList();
                for (int i = 0; i < childs.Count; i++)
                {
                    int index2 = 0;
                    while (index2 < childs.Count)
                    {
                        if (childs[i].Equals(childs[index2]))
                        {
                            index2++;
                            continue;
                        }

                        if (childs[index2].name.Contains(childs[i].name))
                        {
                            childs[index2].SetParent(childs[i]);
                            childs.RemoveAt(index2);
                        }
                        else
                            index2++;
                    }
                }
            }
        }
    }
}