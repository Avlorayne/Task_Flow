# 1) 安装 mermaid-cli（跳过 Chromium 下载，安装时环境变量必须设）
$env:PUPPETEER_SKIP_DOWNLOAD = "true"
npm install -g @mermaid-js/mermaid-cli

# 2) 生成 puppeteer 配置，指向本机 Edge
$cfg = "$env:USERPROFILE\puppeteer-config.json"
@'
{
  "executablePath": "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe",
  "headless": "new",
  "args": ["--no-sandbox", "--disable-gpu"]
}
'@ | Set-Content $cfg -Encoding UTF8
