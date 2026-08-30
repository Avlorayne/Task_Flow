#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
proj2txt.py —— 项目源码一键拼接工具（专为投喂网页端 AI 设计）

把散落在各个子目录里的代码 / 配置 / 文档文件，合并成一个结构清晰的大文本，
并自动生成目录树、文件索引、行号定位与「给 AI 的阅读说明」，
方便直接粘贴给 ChatGPT / Claude / Gemini / DeepSeek / 通义 / 文心等网页端 AI。

快速上手
    python proj2txt.py                          # 拼接当前目录 -> project_bundle.txt
    python proj2txt.py /path/to/project         # 拼接指定项目
    python proj2txt.py --clip                   # 生成并复制到剪贴板
    python proj2txt.py --prompt "帮我找出潜在 bug"
    python proj2txt.py --dry-run                # 只预览，不写文件
    python proj2txt.py --init-config            # 生成配置文件模板
"""

from __future__ import annotations

import argparse
import fnmatch
import json
import os
import re
import shutil
import subprocess
import sys
from dataclasses import dataclass
from datetime import datetime
from pathlib import Path

VERSION = "1.0.0"
TOOL = "proj2txt"
CONFIG_FILENAME = "proj2txt.json"
DEFAULT_OUTPUT = "project_bundle.txt"
W = 70  # 输出横幅宽度

# ─────────────────────────── 默认规则 ───────────────────────────

DEFAULT_EXTS = {
    # 编程语言
    "py", "pyw", "js", "mjs", "cjs", "ts", "jsx", "tsx", "java", "c", "h", "cpp",
    "cc", "hpp", "cs", "go", "rs", "rb", "php", "swift", "kt", "kts", "scala",
    "dart", "m", "mm", "pl", "pm", "lua", "r", "jl", "hs", "clj", "ex", "exs",
    "erl", "groovy", "asm", "zig", "nim", "v",
    # Web / 模板
    "html", "htm", "css", "scss", "sass", "less", "styl", "vue", "svelte",
    "astro", "ejs", "hbs", "pug", "jinja", "j2", "liquid", "twig",
    # 数据 / 配置
    "json", "yml", "yaml", "toml", "ini", "cfg", "conf", "properties", "xml",
    "csv", "tsv", "sql", "graphql", "gql", "proto",
    # 文档 / 脚本
    "md", "markdown", "mdx", "rst", "txt", "adoc", "tex",
    "sh", "bash", "zsh", "fish", "bat", "cmd", "ps1", "psm1",
}

DEFAULT_EXCLUDE_DIRS = {
    ".git", ".svn", ".hg", ".idea", ".vscode", ".vs", ".settings",
    "node_modules", "bower_components", "jspm_packages",
    "__pycache__", ".mypy_cache", ".pytest_cache", ".ruff_cache", ".tox",
    "venv", ".venv", "env", "virtualenv", ".env",
    "dist", "build", "out", "target", "obj", "bin",
    "vendor", "Pods", "Carthage", ".gradle", ".terraform",
    ".next", ".nuxt", ".output", ".svelte-kit", ".astro", ".cache",
    "coverage", ".nyc_output", ".parcel-cache",
}

DEFAULT_EXCLUDE_FILES = {
    "package-lock.json", "yarn.lock", "pnpm-lock.yaml", "poetry.lock",
    "pipfile.lock", "composer.lock", "cargo.lock", "gemfile.lock",
}

DEFAULT_EXCLUDE_PATTERNS = [
    "*.min.js", "*.min.css", "*.map", "*.log",
    "*.pyc", "*.pyo", "*.class", "*.o", "*.so", "*.dll", "*.exe",
    "*.bin", "*.woff", "*.woff2", "*.ttf", "*.eot", "*.otf", "*.ico",
    "*.png", "*.jpg", "*.jpeg", "*.gif", "*.bmp", "*.svg", "*.webp",
    "*.pdf", "*.zip", "*.tar", "*.gz", "*.rar", "*.7z",
    "*.mp3", "*.mp4", "*.avi", "*.mov", "*.db", "*.sqlite",
]

# 无扩展名但属于文本的文件名
DEFAULT_FILENAMES = {
    "dockerfile", "makefile", "rakefile", "gemfile", "procfile", "brewfile",
    "justfile", "vagrantfile", "license", "licence", "notice",
    ".gitignore", ".gitattributes", ".dockerignore", ".editorconfig",
    ".npmrc", ".nvmrc", ".python-version", ".env.example", ".env.sample",
}

LANGUAGE_BY_EXT = {
    "py": "Python", "pyw": "Python", "js": "JavaScript", "mjs": "JavaScript",
    "cjs": "JavaScript", "ts": "TypeScript", "tsx": "TypeScript React",
    "jsx": "JavaScript React", "java": "Java", "c": "C", "h": "C Header",
    "cpp": "C++", "cc": "C++", "hpp": "C++ Header", "cs": "C#", "go": "Go",
    "rs": "Rust", "rb": "Ruby", "php": "PHP", "swift": "Swift", "kt": "Kotlin",
    "kts": "Kotlin", "scala": "Scala", "dart": "Dart", "lua": "Lua",
    "pl": "Perl", "r": "R", "jl": "Julia", "m": "MATLAB/ObjC",
    "html": "HTML", "htm": "HTML", "css": "CSS", "scss": "SCSS",
    "sass": "Sass", "less": "Less", "vue": "Vue", "svelte": "Svelte",
    "json": "JSON", "yml": "YAML", "yaml": "YAML", "toml": "TOML",
    "ini": "INI", "cfg": "Config", "conf": "Config", "env": "Env",
    "xml": "XML", "sql": "SQL", "graphql": "GraphQL", "proto": "Protobuf",
    "md": "Markdown", "markdown": "Markdown", "mdx": "MDX", "rst": "reST",
    "txt": "Text", "csv": "CSV", "tsv": "TSV", "tex": "LaTeX",
    "sh": "Shell", "bash": "Shell", "zsh": "Shell", "fish": "Shell",
    "bat": "Batch", "cmd": "Batch", "ps1": "PowerShell", "psm1": "PowerShell",
}

LANGUAGE_BY_NAME = {
    "dockerfile": "Dockerfile", "makefile": "Makefile", "rakefile": "Ruby Rake",
    "gemfile": "Ruby Gemfile", "justfile": "Justfile", "license": "License",
    "licence": "License", ".gitignore": "Git Ignore",
    ".dockerignore": "Docker Ignore", ".editorconfig": "EditorConfig",
}

# 智能排序：优先级从高到低
CONFIG_MANIFESTS = {
    "package.json", "pyproject.toml", "setup.py", "setup.cfg",
    "requirements.txt", "go.mod", "go.sum", "cargo.toml", "pom.xml",
    "build.gradle", "composer.json", "gemfile", "dockerfile",
    "docker-compose.yml", "docker-compose.yaml", "manage.py", ".env.example",
}
ENTRY_STEMS = {"main", "app", "index", "server", "wsgi", "asgi", "__init__", "cli", "run"}
ENTRY_EXTS = {".py", ".js", ".ts", ".jsx", ".tsx", ".go", ".rs", ".rb", ".php", ".java"}

CJK_RE = re.compile(r"[\u3000-\u9fff\uff00-\uffef]")

AI_NOTES = """本文件是「{root}」项目的源码拼接合集，由 proj2txt 工具生成。请按以下约定阅读：
 1. 「目录结构」＝项目整体布局；「文件索引」＝各文件的路径 / 语言 / 行数，
    以及其正文在本合集中的起始行号（Start 列，可用于快速定位）。
 2. 每个文件的正文被如下两行标记包裹，其中的路径均相对项目根目录：
        ===== FILE START: <相对路径> =====
        ===== FILE END: <相对路径> =====
 3. 个别文件若被截断，其正文末尾会有一行「……该文件共 N 行……」的提示。
 4. 引用代码时请使用「相对路径:行号」格式（例如 src/main.py:42）；
    若正文行首带有「 行号 | 」前缀，请以该前缀中的数字为文件内行号。
 5. 若我没有在「我的需求」中给出具体任务，请先简要总结项目结构与所用技术栈。"""

EPILOG = """\
常用示例:
  python proj2txt.py                            # 拼接当前目录 -> project_bundle.txt
  python proj2txt.py myproject -o bundle.txt    # 指定项目目录与输出文件
  python proj2txt.py --only-ext py md           # 只拼接 Python 与 Markdown 文件
  python proj2txt.py --ext proto graphql        # 在默认范围上追加扩展名
  python proj2txt.py --exclude-dir tests docs   # 额外排除某些目录
  python proj2txt.py --include-pattern "src/*"  # 强制包含匹配的文件（优先级最高）
  python proj2txt.py --line-numbers             # 正文带行号，AI 引用更精准
  python proj2txt.py --max-file-lines 300       # 单文件超过 300 行则截断
  python proj2txt.py --max-total-kb 200         # 总体积预算 200KB
  python proj2txt.py --split-tokens 60000       # 体积过大时自动切成多个文件
  python proj2txt.py --prompt "帮我审查代码" --clip   # 附带需求并复制到剪贴板
  python proj2txt.py --dry-run                  # 预览将拼接哪些文件
  python proj2txt.py --init-config              # 生成 proj2txt.json 配置模板

说明: 通配符规则同 fnmatch，* 可跨目录层级（如 "src/*" 匹配 src 下所有文件）。"""

CONFIG_TEMPLATE = {
    "_说明": [
        "proj2txt 配置文件。命令行参数优先级高于本文件；",
        "不需要的键可直接删除（恢复默认）；exts 为空列表 [] 时使用内置默认扩展名。",
    ],
    "output": "project_bundle.txt",
    "exts": [],
    "any_text": False,
    "exclude_dirs": [],
    "exclude_files": [],
    "exclude_patterns": [],
    "include_patterns": [],
    "line_numbers": False,
    "max_file_lines": 0,
    "max_file_kb": 512,
    "max_total_kb": 0,
    "split_tokens": 0,
    "show_tree": True,
    "show_index": True,
    "ai_header": True,
    "smart_order": True,
    "clip": False,
}

_ASCII_FALLBACK = str.maketrans({
    "═": "=", "─": "-", "├": "|", "└": "`", "│": "|", "▶": ">",
    "✔": "[OK]", "⚠": "[!]", "❌": "[X]", "★": "*", "…": "...",
    "·": "-", "（": "(", "）": ")", "「": '"', "」": '"',
})


# ─────────────────────────── 数据结构 ───────────────────────────

@dataclass
class FileRec:
    rel: Path
    abspath: Path
    language: str = ""
    encoding: str = ""
    content: str = ""
    lines: int = 0
    chars: int = 0
    nbytes: int = 0
    truncated: bool = False
    orig_lines: int = 0


@dataclass
class Config:
    root: Path
    output: Path
    exts: set
    any_text: bool
    exclude_dirs: set
    exclude_files: set
    exclude_patterns: list
    include_patterns: list
    line_numbers: bool
    max_file_lines: int
    max_file_kb: float
    max_total_kb: float
    split_tokens: int
    show_tree: bool
    show_index: bool
    ai_header: bool
    smart_order: bool
    clip: bool
    config_path: Path


# ─────────────────────────── 小工具 ───────────────────────────

def cprint(*args, **kw):
    """安全打印：终端编码不支持中文符号时自动降级为 ASCII。"""
    s = " ".join(str(a) for a in args)
    try:
        print(s, **kw)
    except UnicodeEncodeError:
        print(s.translate(_ASCII_FALLBACK), **kw)


def normalize_ext(e: str) -> str:
    return str(e).strip().lower().lstrip(".")


def fmt_size(n) -> str:
    n = float(n)
    for u in ("B", "KB", "MB", "GB"):
        if n < 1024 or u == "GB":
            return f"{n:.0f} {u}" if u == "B" else f"{n:.1f} {u}"
        n /= 1024
    return f"{n:.1f} GB"


def estimate_tokens(text: str) -> int:
    """粗略估算 token：中文按 ~1.1 token/字，其他按 ~3.8 字符/token。"""
    cjk = len(CJK_RE.findall(text))
    return int(cjk * 1.1 + (len(text) - cjk) / 3.8)


def token_hint(tok: int) -> str:
    if tok < 30_000:
        return "✅ 体量适中，可直接粘贴给绝大多数网页 AI"
    if tok < 100_000:
        return "⚠️ 较长，部分 AI 输入框有长度限制，建议裁剪或分卷"
    if tok < 200_000:
        return "⚠️ 很长，仅长上下文模型（Claude/Gemini 等）能完整读取，建议 --split-tokens 分卷"
    return "❌ 过长，强烈建议 --exclude-dir / --max-file-lines / --only-ext / --split-tokens 裁剪"


def lang_of(p: Path) -> str:
    name_l = p.name.lower()
    if name_l in LANGUAGE_BY_NAME:
        return LANGUAGE_BY_NAME[name_l]
    ext = p.suffix.lower().lstrip(".")
    if ext in LANGUAGE_BY_EXT:
        return LANGUAGE_BY_EXT[ext]
    return ext.upper() if ext else "Text"


# ─────────────────────────── 文件发现与读取 ───────────────────────────

def _match_any(rel_posix: str, name_l: str, patterns) -> bool:
    for pat in patterns:
        pat_l = str(pat).lower()
        if fnmatch.fnmatch(name_l, pat_l) or fnmatch.fnmatch(rel_posix, pat_l):
            return True
    return False


def discover(cfg: Config):
    root = cfg.root
    out_abs = cfg.output.expanduser().resolve()
    cfg_abs = cfg.config_path.resolve() if cfg.config_path else None
    self_abs = Path(__file__).resolve() if "__file__" in globals() else None
    found = []
    for dirpath, dirnames, filenames in os.walk(root):
        # 原地剪枝：跳过排除目录（不进入）
        dirnames[:] = sorted(d for d in dirnames if d.lower() not in cfg.exclude_dirs)
        for fn in sorted(filenames):
            p = Path(dirpath) / fn
            rel = p.relative_to(root)
            rel_posix = rel.as_posix()
            name_l = fn.lower()
            try:
                pa = p.resolve()
            except OSError:
                pa = p
            if pa == out_abs or (cfg_abs and pa == cfg_abs) or (self_abs and pa == self_abs):
                continue
            included_override = _match_any(rel_posix, name_l, cfg.include_patterns)
            if name_l in cfg.exclude_files and not included_override:
                continue
            if _match_any(rel_posix, name_l, cfg.exclude_patterns) and not included_override:
                continue
            ext = p.suffix.lower().lstrip(".")
            if not (cfg.any_text or ext in cfg.exts or name_l in DEFAULT_FILENAMES):
                if not included_override:
                    continue
            found.append((p, rel))
    return found


def read_text(p: Path):
    """自动识别编码读取文本；返回 (text, encoding, err)。二进制返回 (None, None, 原因)。"""
    try:
        raw = p.read_bytes()
    except OSError as e:
        return None, None, f"读取失败 {e.__class__.__name__}"
    if b"\x00" in raw[:8192]:
        return None, None, "疑似二进制"
    for enc in ("utf-8-sig", "utf-8", "gbk", "big5", "latin-1"):
        try:
            return raw.decode(enc), enc, None
        except (UnicodeDecodeError, LookupError):
            continue
    return None, None, "无法解码"


def file_priority(p: Path) -> int:
    name_l = p.name.lower()
    if name_l.startswith("readme"):
        return 0
    if name_l in CONFIG_MANIFESTS or name_l in (".gitignore", ".dockerignore", ".editorconfig"):
        return 1
    if p.stem.lower() in ENTRY_STEMS and p.suffix.lower() in ENTRY_EXTS:
        return 2
    if p.stem.lower() in ("config", "settings"):
        return 2
    return 3


def order_key(item):
    p, rel = item
    return (file_priority(p), rel.as_posix().lower())


def build_records(cfg: Config, candidates):
    records, skipped = [], []
    total = 0
    budget = int(cfg.max_total_kb * 1024) if cfg.max_total_kb else 0
    for p, rel in candidates:
        try:
            size = p.stat().st_size
        except OSError as e:
            skipped.append((rel.as_posix(), f"无法读取（{e.__class__.__name__}）"))
            continue
        if cfg.max_file_kb and size > cfg.max_file_kb * 1024:
            skipped.append((rel.as_posix(),
                            f"超过单文件上限 {cfg.max_file_kb:g} KB（实际 {fmt_size(size)}），可用 --max-file-kb 调整"))
            continue
        text, enc, err = read_text(p)
        if text is None:
            skipped.append((rel.as_posix(), f"{err}，未纳入"))
            continue
        if budget and records and total + len(text) > budget:
            skipped.append((rel.as_posix(), "超出 --max-total-kb 总预算，未纳入"))
            continue
        text = text.replace("\r\n", "\n").replace("\r", "\n")
        if not text.endswith("\n"):
            text += "\n"
        orig_lines = len(text.splitlines())
        truncated = False
        if cfg.max_file_lines and orig_lines > cfg.max_file_lines:
            keep = cfg.max_file_lines
            text = "\n".join(text.split("\n")[:keep])
            if not text.endswith("\n"):
                text += "\n"
            text += (f"……（该文件共 {orig_lines} 行，超过 --max-file-lines={keep} 限制，"
                     f"此处仅保留前 {keep} 行）\n")
            truncated = True
        if not text.strip():
            text = "（空文件）\n"
        records.append(FileRec(
            rel=rel, abspath=p, language=lang_of(p), encoding=enc,
            content=text, lines=len(text.splitlines()), chars=len(text),
            nbytes=size, truncated=truncated, orig_lines=orig_lines,
        ))
        total += len(text)
    return records, skipped


# ─────────────────────────── 渲染 ───────────────────────────

def build_tree(records, root_label: str) -> str:
    tree = {}
    for r in records:
        node = tree
        parts = list(r.rel.parts)
        for part in parts[:-1]:
            node = node.setdefault(part, {})
        node[parts[-1]] = None
    lines = [root_label + "/"]

    def walk(node, prefix):
        items = sorted(node.items(), key=lambda kv: (kv[1] is None, kv[0].lower()))
        for i, (name, child) in enumerate(items):
            last = (i == len(items) - 1)
            lines.append(prefix + ("└── " if last else "├── ") + name + ("/" if child else ""))
            if child:
                walk(child, prefix + ("    " if last else "│   "))

    walk(tree, "")
    return "\n".join(lines) + "\n"


def render(cfg: Config, records, skipped, prompt_text: str, root_name: str,
           part_label: str = "") -> str:
    bar = "═" * W
    sub = "─" * W
    now = datetime.now().strftime("%Y-%m-%d %H:%M")

    def block(title, body=None):
        s = f"{bar}\n {title}\n{sub}\n"
        if body:
            s += body if body.endswith("\n") else body + "\n"
        return s + bar + "\n"

    n = len(records)
    tot_lines = sum(r.lines for r in records)
    tot_chars = sum(r.chars for r in records)
    tot_tokens = sum(estimate_tokens(r.content) for r in records)

    meta_body = "\n".join([
        f" 生成时间 : {now}",
        f" 项目名称 : {root_name}",
        f" 文件数量 : {n} 个" + (f"（另有 {len(skipped)} 个被跳过，见文末附录）" if skipped else ""),
        f" 代码行数 : {tot_lines:,} 行",
        f" 代码体积 : {fmt_size(tot_chars)}",
        f" Token预估: 约 {tot_tokens:,}（粗略估算，实际以平台为准）",
    ])

    pre = [block(f"项目代码合集 · proj2txt 生成{part_label}", meta_body)]
    if cfg.ai_header:
        pre.append(block("给 AI 的阅读说明", AI_NOTES.format(root=root_name)))
    if prompt_text:
        pre.append(block("★ 我的需求（请优先阅读）", prompt_text))
    if cfg.show_tree:
        pre.append(block("目录结构", build_tree(records, root_name)))

    pre_text = "\n".join(pre)
    L_P = pre_text.count("\n")

    # ── 文件块（同时计算每个文件正文的起始行号）──
    pw = min(max(len(r.rel.as_posix()) for r in records), 58)
    file_blocks, content_starts = [], []
    if cfg.show_index:
        cur = L_P + n + 5 + 3   # 索引块共 n+5 行 + 1 空行衔接
    else:
        cur = L_P + 2

    for i, r in enumerate(records, 1):
        relp = r.rel.as_posix()
        info = f" ▶ 文件 {i}/{n} · {relp} · {r.language} · {r.lines} 行"
        if r.encoding and not r.encoding.startswith("utf"):
            info += f" · 编码 {r.encoding}"
        if r.truncated:
            info += " · 已截断"
        content = r.content
        if cfg.line_numbers:
            ls = content.split("\n")
            if ls and ls[-1] == "":
                ls.pop()
            content = "\n".join(f"{k:>5} | {ln}" for k, ln in enumerate(ls, 1)) + "\n"
        file_blocks.append(
            f"{sub}\n{info}\n===== FILE START: {relp} =====\n{content}===== FILE END: {relp} =====\n")
        content_starts.append(cur + 3)
        cur += content.count("\n") + 5   # 块内 4 行 + 块间 1 空行

    # ── 索引块 ──
    idx_block = ""
    if cfg.show_index:
        head = [bar, " 文件索引（Start = 正文在合集中的起始行号）", sub,
                f" {'No':>3}. {'Path'.ljust(pw)}  {'Language':<12} {'Lines':>7} {'Start':>7}"]
        rows = []
        for i, (r, s) in enumerate(zip(records, content_starts), 1):
            rows.append(f" {i:>3}. {r.rel.as_posix():<{pw}}  {r.language:<12} {r.lines:>7} {s:>7}")
        idx_block = "\n".join(head + rows + [bar, ""])

    # ── 组装 ──
    parts = pre + ([idx_block] if cfg.show_index else []) + file_blocks
    if skipped:
        body_lines = [f" - {rel}  （{reason}）" for rel, reason in skipped[:50]]
        if len(skipped) > 50:
            body_lines.append(f" ……另有 {len(skipped) - 50} 个文件未列出")
        parts.append(block("附录：以下文件未包含在正文中", "\n".join(body_lines)))
    parts.append(block(
        f"END · 共 {n} 个文件 · {tot_lines:,} 行 · 约 {tot_tokens:,} tokens",
        f"由 {TOOL} v{VERSION} 生成于 {now} · 如需调整拼接范围请重新运行 proj2txt",
    ))
    return "\n".join(parts)


# ─────────────────────────── 剪贴板 ───────────────────────────

def _win_clipboard(text: str) -> bool:
    import tempfile
    fd, path = tempfile.mkstemp(suffix=".txt")
    try:
        with os.fdopen(fd, "w", encoding="utf-8-sig") as f:
            f.write(text)
        ps = ("$t = Get-Content -LiteralPath '%s' -Raw -Encoding UTF8; "
              "Set-Clipboard -Value $t" % path.replace("'", "''"))
        subprocess.run(["powershell", "-NoProfile", "-Command", ps],
                       check=True, timeout=60, capture_output=True)
        return True
    finally:
        try:
            os.unlink(path)
        except OSError:
            pass


def copy_clipboard(text: str):
    try:
        import pyperclip  # type: ignore
        pyperclip.copy(text)
        return True, "pyperclip"
    except Exception:
        pass
    try:
        if sys.platform == "win32":
            try:
                if _win_clipboard(text):
                    return True, "PowerShell"
            except Exception:
                subprocess.run(["clip"], input=text.encode("utf-16-le"),
                               check=True, capture_output=True)
                return True, "clip"
        elif sys.platform == "darwin":
            subprocess.run(["pbcopy"], input=text.encode("utf-8"),
                           check=True, capture_output=True)
            return True, "pbcopy"
        else:
            for cmd in (["wl-copy"], ["xclip", "-selection", "clipboard"], ["xsel", "--clipboard", "--input"]):
                if shutil.which(cmd[0]):
                    subprocess.run(cmd, input=text.encode("utf-8"),
                                   check=True, capture_output=True)
                    return True, cmd[0]
    except Exception:
        pass
    return False, ""


# ─────────────────────────── 报告输出 ───────────────────────────

def print_summary(out_path: Path, records, skipped, text: str):
    tot_lines = sum(r.lines for r in records)
    tot_tokens = sum(estimate_tokens(r.content) for r in records)
    size = len(text.encode("utf-8"))
    cprint()
    cprint(f"✔ 已生成: {out_path}")
    cprint(f"   ├─ 文件     : {len(records)} 个" + (f"（跳过 {len(skipped)} 个）" if skipped else ""))
    cprint(f"   ├─ 行数     : {tot_lines:,}")
    cprint(f"   ├─ 大小     : {fmt_size(size)}（UTF-8）")
    cprint(f"   └─ Token预估: ~{tot_tokens:,} → {token_hint(tot_tokens)}")
    cprint()
    cprint("提示: 直接把文件内容粘贴给网页 AI 即可，开头已含「给 AI 的阅读说明」。")
    cprint("     常用组合: --line-numbers 精确引用行号 · --clip 复制到剪贴板 · --prompt \"你的需求\"")


def dry_run_report(cfg: Config, records, skipped):
    tot = sum(estimate_tokens(r.content) for r in records)
    cprint(f"· 预览：以下 {len(records)} 个文件将被拼接"
           f"（共 {sum(r.lines for r in records):,} 行，~{tot:,} tokens）")
    if cfg.show_tree:
        cprint()
        cprint(build_tree(records, cfg.root.name).rstrip("\n"))
    cprint()
    for i, r in enumerate(records, 1):
        flag = "  [截断]" if r.truncated else ""
        cprint(f" {i:>3}. {r.rel.as_posix()}  ({r.language}, {r.lines} 行, {fmt_size(r.nbytes)}){flag}")
    if skipped:
        cprint(f"\n· 另有 {len(skipped)} 个文件将被跳过:")
        for rel, reason in skipped[:20]:
            cprint(f"   - {rel}（{reason}）")
        if len(skipped) > 20:
            cprint(f"   ……及另外 {len(skipped) - 20} 个")
    cprint(f"\n· Token 预估: ~{tot:,} → {token_hint(tot)}")
    cprint("· dry-run 模式，未写入任何文件")


# ─────────────────────────── CLI ───────────────────────────

def parse_args(argv=None):
    p = argparse.ArgumentParser(
        prog=TOOL,
        description="项目源码一键拼接工具：把整个项目合并成单个文本，方便投喂给网页端 AI。",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=EPILOG)
    p.add_argument("root", nargs="?", default=".", help="项目根目录（默认当前目录）")
    p.add_argument("-o", "--output", default=None, help=f"输出文件路径（默认 {DEFAULT_OUTPUT}）")
    p.add_argument("--ext", nargs="+", metavar="EXT", help="在默认范围上追加扩展名，如 --ext py md")
    p.add_argument("--only-ext", nargs="+", metavar="EXT", help="只包含指定扩展名（替换默认范围）")
    p.add_argument("--any-text", action="store_true", help="包含所有非二进制文本文件（忽略扩展名白名单）")
    p.add_argument("--exclude-dir", nargs="+", metavar="DIR", help="额外排除的目录名")
    p.add_argument("--exclude-file", nargs="+", metavar="NAME", help="额外排除的文件名")
    p.add_argument("--exclude-pattern", nargs="+", metavar="PAT", help="额外排除的通配符，如 *.min.js tests/*")
    p.add_argument("--include-pattern", nargs="+", metavar="PAT", help="强制包含的通配符（优先级高于排除规则）")
    p.add_argument("--line-numbers", action="store_true", help="正文每行前加行号，便于 AI 精确引用")
    p.add_argument("--max-file-lines", type=int, default=None, metavar="N",
                   help="单文件最多保留 N 行，超出截断（0=不限制）")
    p.add_argument("--max-file-kb", type=float, default=None, metavar="KB",
                   help="超过此大小的文件直接跳过（默认 512）")
    p.add_argument("--max-total-kb", type=float, default=None, metavar="KB",
                   help="合集总大小预算（KB），超出后停止追加文件")
    p.add_argument("--split-tokens", type=int, default=None, metavar="N",
                   help="按 token 预估把合集切成多个文件（如 --split-tokens 60000）")
    p.add_argument("--no-tree", action="store_true", help="不输出目录结构")
    p.add_argument("--no-index", action="store_true", help="不输出文件索引")
    p.add_argument("--no-ai-header", action="store_true", help="不输出「给 AI 的阅读说明」")
    p.add_argument("--no-smart-order", action="store_true", help="禁用智能排序（README/配置/入口优先）")
    p.add_argument("--prompt", default=None, help="附带你的需求/问题，将置于合集最前")
    p.add_argument("--prompt-file", default=None, help="从文件读取需求描述（UTF-8）")
    p.add_argument("--clip", action="store_true", help="生成后复制到系统剪贴板")
    p.add_argument("--stdout", action="store_true", help="输出到标准输出而不写文件")
    p.add_argument("--dry-run", action="store_true", help="只预览将拼接的文件与统计，不生成")
    p.add_argument("--config", default=None, help=f"指定配置文件（默认自动查找 {CONFIG_FILENAME}）")
    p.add_argument("--no-config", action="store_true", help="忽略已存在的配置文件")
    p.add_argument("--init-config", action="store_true", help=f"生成 {CONFIG_FILENAME} 模板后退出")
    p.add_argument("--quiet", action="store_true", help="静默模式，只输出结果路径")
    p.add_argument("--version", action="store_true", help="显示版本号")
    return p.parse_args(argv)


def build_config(root: Path, args, data: dict, cfg_path) -> Config:
    def v(key, cli, default):
        if cli is not None:
            return cli
        if key in data and data[key] is not None:
            return data[key]
        return default

    def flag(key, no_cli, default):
        if no_cli:
            return False
        return bool(data.get(key, default))

    def pos_flag(key, cli):
        return bool(cli) or bool(data.get(key, False))

    exts = set(DEFAULT_EXTS)
    cfg_exts = data.get("exts")
    if isinstance(cfg_exts, list) and cfg_exts:
        exts = {normalize_ext(e) for e in cfg_exts}
    if args.only_ext:
        exts = {normalize_ext(e) for e in args.only_ext}
    elif args.ext:
        exts |= {normalize_ext(e) for e in args.ext}

    def merge_set(defaults, key, cli_val):
        s = set(defaults)
        cv = data.get(key)
        if isinstance(cv, list):
            s |= {str(x).lower() for x in cv}
        if cli_val:
            s |= {str(x).lower() for x in cli_val}
        return s

    def merge_list(defaults, key, cli_val):
        out = list(defaults)
        cv = data.get(key)
        if isinstance(cv, list):
            out += [str(x) for x in cv]
        if cli_val:
            out += [str(x) for x in cli_val]
        return out

    return Config(
        root=root,
        output=Path(v("output", args.output, DEFAULT_OUTPUT)),
        exts=exts,
        any_text=pos_flag("any_text", args.any_text),
        exclude_dirs=merge_set(DEFAULT_EXCLUDE_DIRS, "exclude_dirs", args.exclude_dir),
        exclude_files=merge_set(DEFAULT_EXCLUDE_FILES, "exclude_files", args.exclude_file),
        exclude_patterns=merge_list(DEFAULT_EXCLUDE_PATTERNS, "exclude_patterns", args.exclude_pattern),
        include_patterns=merge_list([], "include_patterns", args.include_pattern),
        line_numbers=pos_flag("line_numbers", args.line_numbers),
        max_file_lines=int(v("max_file_lines", args.max_file_lines, 0) or 0),
        max_file_kb=float(v("max_file_kb", args.max_file_kb, 512) or 0),
        max_total_kb=float(v("max_total_kb", args.max_total_kb, 0) or 0),
        split_tokens=int(v("split_tokens", args.split_tokens, 0) or 0),
        show_tree=flag("show_tree", args.no_tree, True),
        show_index=flag("show_index", args.no_index, True),
        ai_header=flag("ai_header", args.no_ai_header, True),
        smart_order=flag("smart_order", args.no_smart_order, True),
        clip=pos_flag("clip", args.clip),
        config_path=cfg_path,
    )


def load_prompt(args) -> str:
    if args.prompt:
        return args.prompt.strip()
    if args.prompt_file:
        try:
            return Path(args.prompt_file).read_text(encoding="utf-8").strip()
        except Exception as e:
            cprint(f"警告：读取 --prompt-file 失败（{e}），已忽略。")
    return ""


# ─────────────────────────── 主流程 ───────────────────────────

def main(argv=None):
    args = parse_args(argv)
    if args.version:
        cprint(f"{TOOL} v{VERSION}")
        return 0

    root = Path(args.root).expanduser().resolve()
    if not root.is_dir():
        cprint(f"错误：项目目录不存在或不是目录: {root}")
        return 1
    quiet = args.quiet

    cfg_path = (Path(args.config).expanduser().resolve() if args.config
                else root / CONFIG_FILENAME)

    if args.init_config:
        if cfg_path.exists():
            cprint(f"错误：配置文件已存在: {cfg_path}（如需重新生成请先删除）")
            return 1
        cfg_path.parent.mkdir(parents=True, exist_ok=True)
        cfg_path.write_text(json.dumps(CONFIG_TEMPLATE, ensure_ascii=False, indent=2) + "\n",
                            encoding="utf-8")
        cprint(f"✔ 已生成配置模板: {cfg_path}")
        cprint("  按需修改后再次运行 proj2txt 即可自动读取（命令行参数优先级更高）。")
        return 0

    data = {}
    if not args.no_config and cfg_path.is_file():
        try:
            loaded = json.loads(cfg_path.read_text(encoding="utf-8"))
            if not isinstance(loaded, dict):
                raise ValueError("配置根节点必须是 JSON 对象")
            data = loaded
            if not quiet:
                cprint(f"· 已加载配置文件: {cfg_path}")
        except Exception as e:
            cprint(f"警告：配置文件解析失败（{e}），已忽略。")
            data = {}

    cfg = build_config(root, args, data, cfg_path)

    candidates = discover(cfg)
    if not candidates:
        cprint("错误：没有找到任何可拼接的文件。可用 --ext / --include-pattern / --any-text 调整范围。")
        return 1
    if cfg.smart_order:
        candidates.sort(key=order_key)

    records, skipped = build_records(cfg, candidates)
    if not records:
        cprint("错误：所有候选文件都被跳过（过大 / 二进制 / 预算不足）。")
        return 1

    prompt_text = load_prompt(args)

    if args.dry_run:
        dry_run_report(cfg, records, skipped)
        return 0

    # ── 分卷模式 ──
    if cfg.split_tokens and not args.stdout:
        chunks, cur_chunk, cur_tok = [], [], 0
        for r in records:
            t = estimate_tokens(r.content)
            if cur_chunk and cur_tok + t > cfg.split_tokens:
                chunks.append((cur_chunk, cur_tok))
                cur_chunk, cur_tok = [], 0
            cur_chunk.append(r)
            cur_tok += t
        if cur_chunk:
            chunks.append((cur_chunk, cur_tok))
        if len(chunks) > 1:
            base = cfg.output.expanduser()
            base.parent.mkdir(parents=True, exist_ok=True)
            stem, suf = base.stem, base.suffix or ".txt"
            for i, (chunk, tok) in enumerate(chunks, 1):
                label = f" · 第 {i}/{len(chunks)} 部分"
                sk = skipped if i == len(chunks) else []
                txt = render(cfg, chunk, sk, prompt_text, root.name, part_label=label)
                p = base.with_name(f"{stem}.part{i}{suf}")
                with open(p, "w", encoding="utf-8", newline="\n") as fh:
                    fh.write(txt)
                if not quiet:
                    cprint(f"✔ {p.name}  （{len(chunk)} 个文件 · ~{tok:,} tokens · "
                           f"{fmt_size(len(txt.encode('utf-8')))}）")
            if not quiet:
                cprint(f"\n提示: 已按 --split-tokens={cfg.split_tokens} 切成 {len(chunks)} 卷，请按 part1 → part2 顺序投喂。")
            return 0

    text = render(cfg, records, skipped, prompt_text, root.name)

    if args.stdout:
        sys.stdout.write(text)
        return 0

    out = cfg.output.expanduser()
    out.parent.mkdir(parents=True, exist_ok=True)
    with open(out, "w", encoding="utf-8", newline="\n") as fh:
        fh.write(text)

    if quiet:
        cprint(str(out))
    else:
        print_summary(out, records, skipped, text)

    if cfg.clip:
        ok, how = copy_clipboard(text)
        if ok:
            cprint(f"✔ 已复制到剪贴板（via {how}）")
        else:
            cprint("⚠ 复制到剪贴板失败：建议 pip install pyperclip，或手动打开输出文件复制。")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except KeyboardInterrupt:
        cprint("\n已取消。")
        sys.exit(130)
