# Toashot

> 一个更加方便的截图工具

基于 Avalonia 的跨平台截图工具，常驻托盘，全局快捷键随按随截

---

## Toashot功能

### 截图

按 `PrintScreen`（可改）截图，弹出Toast

- **单击卡片** = 保存到目标目录
- **按住拖动** = 触发系统级文件拖放，移动文件（按`Ctrl`复制），可以直接丢进聊天窗口、文件夹、编辑器
- 可通过Toast的三个按钮操作：
- - 保存并用系统默认看图程序打开
- - `打开文件夹` 保存并定位到该文件
- - `忽略` 丢进回收站

### 快速截图

按 `Alt+PrintScreen`（可改），截当前屏幕后完全静默地写进目标目录

### 截图暂存区

Toast超时未处理的截图将自动放入暂存区（`%TEMP%\Toashot`），可通过托盘菜单打开查看

可复制到剪贴板、保存到目标目录、忽略，可一键清空

## 其它

### 构建

需要 [.NET 10 SDK](https://dotnet.microsoft.com/download) 和 [WiX v4+](https://wixtoolset.org/)（`dotnet tool install --global wix`，仅 MSI 需要）。

```powershell
pwsh build.ps1                          # 全部产物 -> dist/
pwsh build.ps1 -Target single           # 只出单 exe
pwsh build.ps1 -Target msi,linux        # 只出安装包 / linux 版
pwsh build.ps1 -Version 1.2.0           # 指定版本号（默认读 csproj 的 <Version>）
```

| Target | 产物 | 说明 |
| --- | --- | --- |
| `portable` | `dist/portable/` | 框架依赖，目标机需装 .NET 10 Desktop Runtime |
| `single` | `dist/win-x64-single/Toashot.exe` | win-x64 自包含单文件，双击即用 |
| `msi` | `dist/Toashot.msi` | 免管理员安装到 `%LOCALAPPDATA%\Programs\Toashot`，带开始菜单快捷方式 |
| `linux` | `dist/linux-x64/` | 仅编译通过：全局钩子与截屏依赖 Win32，运行时会不可用 |

### 图标

via [AppIcon Forge](https://zhangyu1818.github.io/appicon-forge/)

author: [Material Design Authors](https://github.com/material-icons/material-icons)

license: [Apache 2.0](https://github.com/material-icons/material-icons/blob/master/LICENSE)