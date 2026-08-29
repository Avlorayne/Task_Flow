import json
import sys


def pretty_print_file(input_path: str, output_path: str = None, indent: int = 4, sort_keys: bool = False):
    """
    读取 JSON 文件，格式化后写入输出文件

    参数:
        input_path:  输入 JSON 文件路径
        output_path: 输出文件路径（不传则自动命名为 xxx_pretty.json）
        indent:      缩进空格数
        sort_keys:   是否按键名排序
    """
    # 默认输出文件名：data.json -> data_pretty.json
    if output_path is None:
        stem, _, ext = input_path.rpartition(".")
        output_path = f"{stem}_pretty.{ext}" if stem else input_path + "_pretty"

    try:
        # 读
        with open(input_path, "r", encoding="utf-8") as f:
            data = json.load(f)

        # 格式化
        formatted = json.dumps(
            data,
            indent=indent,
            sort_keys=sort_keys,
            ensure_ascii=False,   # 中文正常显示
        )

        # 写
        with open(output_path, "w", encoding="utf-8") as f:
            f.write(formatted)

        print(f"✅ 已输出到: {output_path}")

    except json.JSONDecodeError as e:
        print(f"❌ JSON 解析失败: {e}", file=sys.stderr)
        sys.exit(1)
    except FileNotFoundError:
        print(f"❌ 文件不存在: {input_path}", file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description="JSON 文件格式化工具")
    parser.add_argument("input", help="输入 JSON 文件路径")
    parser.add_argument("-o", "--output", help="输出文件路径（默认 xxx_pretty.json）")
    parser.add_argument("-i", "--indent", type=int, default=4, help="缩进空格数（默认 4）")
    parser.add_argument("-s", "--sort", action="store_true", help="按键名排序")
    args = parser.parse_args()

    pretty_print_file(args.input, args.output, args.indent, args.sort)
