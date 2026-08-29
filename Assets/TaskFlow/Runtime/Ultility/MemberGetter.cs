using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

namespace TaskFlow.Utility
{
    /// <summary>
    /// 信号字段反射缓存（强类型 / 线程安全版）。
    /// 对外仅一个泛型入口；内部：元数据查找 → 委托编译与调用。
    /// 所有字典为 ConcurrentDictionary，可被多线程同时访问。
    /// </summary>
    public sealed class MemberGetter
    {
        // ── 缓存层 ──────────────────────────────────────────
        // 元数据：(Type, name) -> FieldInfo，值为 null 表示"确认不存在"（负缓存）
        private readonly ConcurrentDictionary<(Type, string), FieldInfo> _lookup = new();

        // 委托：(FieldInfo, T的类型) -> 编译好的委托；值为 null 表示"已知类型不兼容"
        private readonly ConcurrentDictionary<(FieldInfo, Type), Delegate> _getters = new();
        
        /// 弱类型取值。无法确定字段类型时使用，值类型字段会装箱一次。
        public bool TryGetValue(object target, string name, out object value)
        {
            return TryGetValue<object>(target, name, out value);
        }
        
        /// 直取弱类型值。字段不存在或不可读时返回 null。
        public object GetValue(object target, string name)
        {
            // null 目标直接短路，避免 NRE 蔓延
            if (target == null) return null;

            var info = FindInfo(target.GetType(), name);
            if (info == null) return null; // 字段不存在（错误已由 FindInfo 报过一次）

            var getter = GetOrCreateGetter<object>(info);
            if (getter == null) return null; // 类型不兼容（错误已由 TryCompileGetter 报过一次）

            return getter(target);
        }
        
        /// 强类型取值。零装箱（字段类型与 T 一致时），线程安全。
        public bool TryGetValue<T>(object target, string name, out T value)
        {
            value = default;

            // null 目标防护，避免 NRE 蔓延到调用方
            if (target == null) return false;

            var info = FindInfo(target.GetType(), name);
            if (info == null) return false;

            var getter = GetOrCreateGetter<T>(info);
            if (getter == null) return false;

            value = getter(target);
            return true;
        }

        // ── 私有辅助链 ──────────────────────────────────────
        /// <summary>查找 FieldInfo，正/负双缓存，错误只报一次</summary>
        private FieldInfo FindInfo(Type type, string name)
        {
            return _lookup.GetOrAdd((type, name), static (key, args) =>
            {
                var found = args.type.GetField(args.name,
                    BindingFlags.Instance | BindingFlags.Public);

                if (found == null)
                    Debug.LogError($"{args.type.Name}'s field '{args.name}' not found!");

                return found;   // 未找到也写入（null）→ 负缓存生效
            }, (type, name));
        }

        /// <summary>获取或编译该字段的强类型委托；编译失败也只发生一次</summary>
        private Func<object, T> GetOrCreateGetter<T>(FieldInfo info)
        {
            var key = (info, typeof(T));

            if (_getters.TryGetValue(key, out var cached))
                return (Func<object, T>)cached;     // cached 为 null 时返回 null，
                                                    // 由上层按失败处理，不再重复报错

            // 先在锁外编译，再原子写入 —— 避免工厂闭包分配，
            // 竞争时最多多编译几次，结果幂等，无害
            var compiled = TryCompileGetter<T>(info);
            var stored = _getters.GetOrAdd(key, (Delegate)compiled);

            return (Func<object, T>)stored;         // 可能被其他线程抢先写入，取共享值
        }

        /// <summary>构建表达式树并编译（每个 成员+T 组合全程最多几次）</summary>
        private static Func<object, T> TryCompileGetter<T>(FieldInfo info)
        {
            var fieldType = info.FieldType;

            // 缺陷修复：解包 Nullable<int?> 之类的包装后再做兼容性检查
            var expectedT = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

            if (!expectedT.IsAssignableFrom(fieldType))
            {
                Debug.LogError(
                    $"Field '{info.DeclaringType.Name}.{info.Name}' is {fieldType.Name}, " +
                    $"cannot read as {typeof(T).Name}!");
                return null;
            }

            var objParam = Expression.Parameter(typeof(object), "o");
            Expression access = Expression.Field(
                Expression.Convert(objParam, info.DeclaringType), info);

            // 字段类型与 T 完全一致时不插入任何转换（返回值零装箱路径）
            if (access.Type != typeof(T))
                access = Expression.Convert(access, typeof(T));   // 含引用收窄 或 int→int? 提升

            return Expression.Lambda<Func<object, T>>(access, objParam).Compile();
        }
    }
}
