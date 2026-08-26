using System;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine.LowLevel;

namespace TaskFlow.TaskFlow.Manager.Tests
{
    /// <summary>
    /// PlayerLoopSystemExtensions 的单元测试。
    /// 使用标记类型构造虚拟 PlayerLoopSystem 树，不依赖真实的 PlayerLoop，
    /// 可在 EditMode 下运行。
    /// </summary>
    [TestFixture]
    public class PlayerLoopModifyTests
    {
        // ---------- 标记类型（仅用于填充 PlayerLoopSystem.type，构造虚拟树） ----------
        private class RootType { }
        private class TypeA { }
        private class TypeA1 { }
        private class TypeA2 { }
        private class TypeB { }
        private class TypeC { }
        private class Deep1 { }
        private class Deep2 { }
        private class Deep3 { }
        private class InsertedType { } // 被插入的新系统

        // ---------- 辅助构造方法 ----------

        /// <summary>构造一个节点，无子节点时 subSystemList 为 null（模拟叶节点）</summary>
        private static PlayerLoopSystem Node(Type type, params PlayerLoopSystem[] children)
            => new PlayerLoopSystem
            {
                type = type,
                subSystemList = children.Length == 0 ? null : children
            };

        private static PlayerLoopSystem NewSystem => Node(typeof(InsertedType));

        /// <summary>
        /// 构造标准测试树：
        /// Root( A( A1, A2 ), B, C )
        /// </summary>
        private static PlayerLoopSystem BuildSampleTree()
            => Node(typeof(RootType),
                Node(typeof(TypeA), Node(typeof(TypeA1)), Node(typeof(TypeA2))),
                Node(typeof(TypeB)),
                Node(typeof(TypeC)));

        // ---------- 辅助查找与断言方法 ----------

        /// <summary>深度优先查找指定类型的节点本身</summary>
        private static bool TryFindNode(PlayerLoopSystem node, Type type, out PlayerLoopSystem found)
        {
            found = default;
            var list = node.subSystemList;
            if (list == null) return false;

            for (int i = 0; i < list.Length; i++)
            {
                if (list[i].type == type)
                {
                    found = list[i];
                    return true;
                }
                if (TryFindNode(list[i], type, out found))
                    return true;
            }
            return false;
        }

        /// <summary>判断树中是否存在指定类型的节点</summary>
        private static bool Contains(PlayerLoopSystem node, Type type)
        {
            if (node.type == type) return true;
            if (node.subSystemList == null) return false;
            return node.subSystemList.Any(sub => Contains(sub, type));
        }

        /// <summary>将树序列化为字符串，便于整体结构断言，例如 "Root(A(A1,A2),B,C)"</summary>
        private static string Dump(PlayerLoopSystem node)
        {
            var name = node.type?.Name ?? "()";
            if (node.subSystemList == null || node.subSystemList.Length == 0)
                return name;
            var sb = new StringBuilder();
            sb.Append(name).Append('(');
            sb.Append(string.Join(",", node.subSystemList.Select(Dump)));
            sb.Append(')');
            return sb.ToString();
        }

        // =====================================================================
        // InsertSystemAfter：作为兄弟节点，插入到目标后方
        // =====================================================================

        [Test]
        public void InsertSystemAfter_顶层目标_插入为兄弟节点且顺序正确()
        {
            var root = BuildSampleTree();
            var result = root.InsertSystemAfter<TypeB>(NewSystem);

            Assert.IsNotNull(result.subSystemList);
            Assert.AreEqual(4, result.subSystemList.Length, "Root 本层节点数应为 4");
            Assert.AreEqual(typeof(InsertedType), result.subSystemList[2].type, "新系统应紧随 TypeB 之后");
        }

        [Test]
        public void InsertSystemAfter_嵌套目标_正确插入到深层()
        {
            var root = BuildSampleTree();
            var result = root.InsertSystemAfter<TypeA2>(NewSystem);

            Assert.IsTrue(TryFindNode(result, typeof(TypeA), out var nodeA));
            Assert.AreEqual(3, nodeA.subSystemList.Length, "A 的子列表应有 3 个节点");
            Assert.AreEqual(typeof(InsertedType), nodeA.subSystemList[2].type);

            Assert.AreEqual("RootType(TypeA(TypeA1,TypeA2,InsertedType),TypeB,TypeC)", Dump(result));
        }

        [Test]
        public void InsertSystemAfter_目标为非叶节点_作为兄弟而非子节点插入()
        {
            var root = BuildSampleTree();
            var result = root.InsertSystemAfter<TypeA>(NewSystem);

            Assert.IsTrue(TryFindNode(result, typeof(TypeA), out var nodeA));
            Assert.IsNotNull(nodeA.subSystemList);
            Assert.AreEqual(2, nodeA.subSystemList.Length, "TypeA 的子列表不应被修改");

            Assert.AreEqual(typeof(InsertedType), result.subSystemList[1].type);
        }

        [Test]
        public void InsertSystemAfter_目标不存在_原样返回()
        {
            var root = BuildSampleTree();
            var result = root.InsertSystemAfter<DateTime>(NewSystem);

            Assert.IsFalse(Contains(result, typeof(InsertedType)), "不应插入新节点");
            Assert.AreEqual(Dump(BuildSampleTree()), Dump(result), "树结构应保持不变");
        }

        [Test]
        public void InsertSystemAfter_根节点为叶节点_安全返回()
        {
            var leafRoot = Node(typeof(RootType));
            var result = leafRoot.InsertSystemAfter<TypeA>(NewSystem);

            Assert.IsNull(result.subSystemList, "叶根节点不应被修改");
        }

        [Test]
        public void InsertSystemAfter_重复类型_只匹配第一个()
        {
            // Root( A1, B, B )  —— 两个 TypeB，应插在第一个之后
            var root = Node(typeof(RootType),
                Node(typeof(TypeA1)), Node(typeof(TypeB)), Node(typeof(TypeB)));

            var result = root.InsertSystemAfter<TypeB>(NewSystem);

            Assert.AreEqual("RootType(TypeA1,TypeB,InsertedType,TypeB)", Dump(result));
        }

        // =====================================================================
        // InsertSystemEndOf：插入到目标的子列表末尾
        // =====================================================================

        [Test]
        public void InsertSystemEndOf_目标已有子节点_追加到末尾()
        {
            var root = BuildSampleTree();
            var result = root.InsertSystemEndOf<TypeA>(NewSystem);

            Assert.IsTrue(TryFindNode(result, typeof(TypeA), out var nodeA));
            Assert.AreEqual(3, nodeA.subSystemList.Length, "A 的子列表应有 3 个节点");
            Assert.AreEqual(typeof(InsertedType), nodeA.subSystemList[2].type, "新系统应在 TypeA 子列表的最后");

            Assert.AreEqual("RootType(TypeA(TypeA1,TypeA2,InsertedType),TypeB,TypeC)", Dump(result));
        }

        [Test]
        public void InsertSystemEndOf_目标是叶节点_自动创建子列表()
        {
            var root = BuildSampleTree();
            var result = root.InsertSystemEndOf<TypeC>(NewSystem);

            Assert.IsTrue(TryFindNode(result, typeof(TypeC), out var nodeC));
            Assert.IsNotNull(nodeC.subSystemList, "TypeC 原本无子节点，现在应自动创建非空数组");
            Assert.AreEqual(1, nodeC.subSystemList.Length, "TypeC 子列表应有 1 个节点");
            Assert.AreEqual(typeof(InsertedType), nodeC.subSystemList[0].type);

            Assert.AreEqual("RootType(TypeA(TypeA1,TypeA2),TypeB,TypeC(InsertedType))", Dump(result));
        }

        [Test]
        public void InsertSystemEndOf_深层嵌套目标_修改正确写回()
        {
            var root = Node(typeof(RootType),
                Node(typeof(Deep1),
                    Node(typeof(Deep2),
                        Node(typeof(Deep3)))));

            var result = root.InsertSystemEndOf<Deep3>(NewSystem);

            Assert.AreEqual("RootType(Deep1(Deep2(Deep3(InsertedType))))", Dump(result));
        }

        [Test]
        public void InsertSystemEndOf_目标不存在_原样返回()
        {
            var root = BuildSampleTree();
            var result = root.InsertSystemEndOf<DateTime>(NewSystem);

            Assert.IsFalse(Contains(result, typeof(InsertedType)));
            Assert.AreEqual(Dump(BuildSampleTree()), Dump(result));
        }

        // =====================================================================
        // 通用行为：结构体值语义与无副作用
        // =====================================================================

        [Test]
        public void 插入操作_不应修改传入的原始树()
        {
            var root = BuildSampleTree();
            var originalDump = Dump(root);

            var resultA = root.InsertSystemAfter<TypeB>(NewSystem);
            var resultB = root.InsertSystemEndOf<TypeA>(NewSystem);

            Assert.AreEqual(originalDump, Dump(root), "原始树不应被修改");
            Assert.AreNotEqual(originalDump, Dump(resultA), "After 插入应产生新树");
            Assert.AreNotEqual(originalDump, Dump(resultB), "EndOf 插入应产生新树");
        }
    }
}
