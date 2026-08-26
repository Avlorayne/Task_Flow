using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.LowLevel;

namespace TaskFlow.TaskFlow.Manager
{
    public static class PlayerLoopSystemExtensions
    {
        public static void PrintEvents(this PlayerLoopSystem system)
        {
            foreach (var header in system.subSystemList)
            {
                Debug.LogFormat("------{0}------", header.type.Name);
                foreach (var subSystem in header.subSystemList)
                {
                    Debug.LogFormat("{0}.{1}", header.type.Name, subSystem.type.Name);
                }
            }
        }
        
        /// <summary>
        /// 在找到类型 T 的节点后方，作为兄弟节点插入
        /// </summary>
        public static PlayerLoopSystem InsertSystemAfter<T>(this PlayerLoopSystem root, PlayerLoopSystem system)
        {
            TryInsertAfter(root, out var newRoot);
            return newRoot;

            bool TryInsertAfter(PlayerLoopSystem current, out PlayerLoopSystem result)
            {
                result = current;
                if (current.subSystemList == null || current.subSystemList.Length == 0)
                    return false;

                for (int i = 0; i < current.subSystemList.Length; i++)
                {
                    var sub = current.subSystemList[i];

                    // 情况1：找到目标节点，在其后方作为兄弟节点插入
                    if (sub.type == typeof(T))
                    {
                        var list = current.subSystemList.ToList();
                        list.Insert(i + 1, system);
                        current.subSystemList = list.ToArray(); // 修改当前层级的数组
                        result = current;
                        return true;
                    }

                    // 情况2：未找到，向下递归查找
                    if (TryInsertAfter(sub, out var updatedSub))
                    {
                        // 修复副作用：先复制整个数组，再修改副本，最后赋值给当前节点
                        var newList = current.subSystemList.ToArray();
                        newList[i] = updatedSub;
                        current.subSystemList = newList;

                        result = current;
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// 在找到类型 T 的节点的子列表最后插入
        /// </summary>
        public static PlayerLoopSystem InsertSystemEndOf<T>(this PlayerLoopSystem root, PlayerLoopSystem system)
        {
            TryInsertEndOf(root, out var newRoot);
            return newRoot;

            bool TryInsertEndOf(PlayerLoopSystem current, out PlayerLoopSystem result)
            {
                result = current;
                if (current.subSystemList == null || current.subSystemList.Length == 0)
                    return false;

                for (int i = 0; i < current.subSystemList.Length; i++)
                {
                    var sub = current.subSystemList[i];

                    // 情况1：找到目标节点，插入到其子列表末尾
                    if (sub.type == typeof(T))
                    {
                        var list = sub.subSystemList?.ToList() ?? new List<PlayerLoopSystem>();
                        list.Add(system);
                        sub.subSystemList = list.ToArray(); // 修改副本的子列表

                        // 修复副作用：先复制当前层数组，再替换目标节点
                        var newList = current.subSystemList.ToArray();
                        newList[i] = sub;
                        current.subSystemList = newList;

                        result = current;
                        return true;
                    }

                    // 情况2：未找到，向下递归查找
                    if (TryInsertEndOf(sub, out var updatedSub))
                    {
                        // 修复副作用：先复制数组，再替换
                        var newList = current.subSystemList.ToArray();
                        newList[i] = updatedSub;
                        current.subSystemList = newList;

                        result = current;
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
