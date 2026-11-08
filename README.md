# FloatSnap — Windows 一键全屏截图悬浮按钮

点击桌面右上角的半透明悬浮按钮，**立即把整个屏幕（多显示器时为全部屏幕）截图，保存到「图片\FloatSnap」并复制到剪贴板**；既可以 `Ctrl+V` 粘贴，也可以直接去图片文件夹里找文件。

## 功能

- 🟢 半透明绿色圆形悬浮按钮，常驻屏幕**右上角**（距顶部 180 DIP，不挡窗口的关闭按钮），置顶显示
- 🖱️ 单击按钮 = 全屏截图 → **存为 PNG** 到 `图片\FloatSnap\`（文件名带时间）并放入剪贴板；截图前按钮自动隐藏，不会把自己截进去
- ✨ 鼠标悬停时透明度加深（0.55 → 0.85），方便点击又不遮挡画面
- 🖐️ **点击不抢焦点**：窗口带 `WS_EX_NOACTIVATE`，点按钮时前台窗口始终还是你原来的窗口，终端 / 浏览器不会失焦，Windows 也就不会因为「焦点重新进入输入区域」弹出屏幕软键盘
- 💬 截图完成后托盘气泡提示文件名与尺寸；失败会提示原因
- 📌 托盘图标：右键菜单可开关**开机自启动**、**打开截图文件夹**、**退出**
- 🔒 单实例运行，重复启动会提示
- 🚀 自包含单文件，**目标机器不需要安装 .NET 运行时**

## 安装（推荐）

`dist\` 里有两个安装包，按电脑架构选一个：

| 电脑类型 | 安装包 |
|---|---|
| ARM64（骁龙 / 骁龙 X 平板、Surface Pro X 等） | `FloatSnap_Setup_arm64.msi` |
| 普通 Intel / AMD 电脑 | `FloatSnap_Setup_x64.msi` |

双击安装（会弹一次管理员授权），装完：

- 程序在 `C:\Program Files\FloatSnap\FloatSnap.exe`
- 开始菜单 + 桌面都有 **FloatSnap** 快捷方式
- **默认开机自启**（写入 `HKCU\...\Run`，托盘菜单里可随时关掉）
- 「设置 → 应用」里能看到 FloatSnap，可正常卸载（卸载会清掉程序、快捷方式和自启项）

> 不确定自己是哪种架构？`Win + R` 输入 `msinfo32`，看「系统类型」：显示 `ARM 64 位` 就装 arm64 版，显示 `x64` 就装 x64 版。

### 免安装单文件

`dist\FloatSnap_arm64.exe` / `dist\FloatSnap_x64.exe` 是自包含单文件版本，**拷到 U 盘或任意目录双击就能跑**，不需要安装、不需要 .NET 运行时。适合临时用或不想装进系统的情况。

## 从源码构建

### 只构建单文件 exe（开发调试）

双击 `publish.cmd`，脚本会按当前 CPU 架构自动选择 `win-arm64` / `win-x64`，产物在 `publish\FloatSnap.exe`（依赖系统 .NET 9 Desktop Runtime）。

### 构建安装包（发布用）

双击 `build-installer.cmd`，一条命令完成：

1. `dotnet tool restore` 还原 WiX（工具版本写在 `.config/dotnet-tools.json`，不污染全局环境）
2. 分别发布 `win-arm64` / `win-x64` 自包含单文件 exe
3. 生成两个 MSI

产物全部在 `dist\`：

```
dist\
├── FloatSnap_Setup_arm64.msi   # ARM64 安装包
├── FloatSnap_Setup_x64.msi     # x64 安装包
├── FloatSnap_arm64.exe         # ARM64 免安装单文件
└── FloatSnap_x64.exe           # x64 免安装单文件
```

### 环境要求

- Windows 10 / Windows 11（x64 或 ARM64）
- .NET 9 SDK（构建用）：<https://dotnet.microsoft.com/download/dotnet/9.0>
- 首次构建需要能访问 NuGet（下载 WiX）

## 验证清单

| 项目 | 预期结果 |
|---|---|
| 安装 MSI | 无报错；`C:\Program Files\FloatSnap\FloatSnap.exe` 存在 |
| 开始菜单 / 桌面 | 出现绿色剪刀图标的 FloatSnap 快捷方式 |
| 双击快捷方式 | 右上角偏下出现半透明圆形按钮（不压住关闭按钮） |
| 单击按钮 | 按钮闪一下消失，约 0.1 秒后回来 |
| 打开 `图片\FloatSnap` | 多一张 `FloatSnap_日期_时间.png`，尺寸等于屏幕分辨率 |
| 打开画图 / 微信，Ctrl+V | 粘贴出刚才的整屏截图，且画面里没有悬浮按钮 |
| 托盘右键 → 打开截图文件夹 | 直接弹出 `图片\FloatSnap` |
| 重启电脑 | 程序自动运行，按钮出现（默认自启） |
| 「设置 → 应用」卸载 | 程序目录、快捷方式、自启项全部消失 |

## 项目结构

```
FloatSnap/
├── FloatSnap.csproj          # 工程文件（net9.0-windows，WPF + WinForms 托盘）
├── app.manifest              # DPI 感知 + 普通用户权限
├── App.xaml / App.xaml.cs    # 应用入口，单实例 Mutex
├── MainWindow.xaml/.cs       # 透明无边框悬浮按钮窗口 + 点击逻辑
├── NoActivateBehavior.cs     # 让窗口永不激活（不抢前台焦点，避免终端弹软键盘）
├── TouchKeyboardGuard.cs     # 兜底：万一软键盘还是冒出来，新出现的才收起
├── NativeMethods.cs          # 用到的 Win32 API 声明
├── ScreenCapture.cs          # 全屏截图 + 存图（图片\FloatSnap）+ 写入剪贴板（带重试）
├── AutoStart.cs              # HKCU Run 注册表自启动开关
├── TrayService.cs            # 托盘图标、菜单与结果提示
├── Assets/FloatSnap.ico      # 程序 / 快捷方式 / 托盘图标
├── installer/FloatSnap.wxs   # WiX 安装包定义
├── .config/dotnet-tools.json # 固定 WiX 版本
├── publish.cmd               # 开发用：只出 exe
└── build-installer.cmd       # 发布用：出 exe + 两个 MSI
```

## 常见问题

- **双击 exe 弹「You must install .NET to run this application」？** 这是旧的开发版 exe（依赖运行时且架构不匹配）。请用 `dist\` 里的自包含单文件或安装包装。
- **该装哪个 MSI？** ARM64 平板装 `FloatSnap_Setup_arm64.msi`，Intel/AMD 电脑装 `FloatSnap_Setup_x64.msi`；装错了也不会有破坏，卸载后换一个即可（两者升级码不同，互不影响）。
- **图存到哪了？** 存在系统的「图片」库里：通常是 `C:\Users\<用户名>\Pictures\FloatSnap\`；如果开了 OneDrive 图片备份，则是 `C:\Users\<用户名>\OneDrive\图片\FloatSnap\`。托盘右键 →「打开截图文件夹」可直接打开。
- **不想开机自启？** 托盘图标右键 → 取消勾选「开机自启动」。
- **怎么彻底卸载？** 设置 → 应用 → FloatSnap → 卸载；或命令行 `msiexec /x FloatSnap_Setup_*.msi`。
- **点了没反应？** 看托盘气泡提示。若提示"剪贴板被其他程序占用"，图其实已经保存到 `图片\FloatSnap` 了；若程序已退出，从开始菜单重新打开。
- **截图里有鼠标指针？** 当前版本不包含鼠标指针，这是正常现象。
- **在终端里点截图，会弹出系统软键盘？** 1.0.3 已修。原因是点按钮时窗口抢了前台焦点，终端随之失去/重获焦点，Windows 就把屏幕键盘顶上来了（浏览器因为 Chromium 自己会压掉，所以不明显）。现在窗口带 `WS_EX_NOACTIVATE`，点按钮完全不改变前台窗口，焦点不跳就不会有键盘；另有一层兜底：若键盘是这次点击新冒出来的，会自动收起（你自己开的键盘不受影响）。
- **按钮位置不对？** 程序按主屏工作区（去掉任务栏）定位：距右 16、距顶 180。想再往下挪，修改 `MainWindow.xaml.cs` 中 `PositionAtTopRight()` 里的 `180`。
- **想只截一块区域？** 当前版本不做框选，点一下就是整屏；需要框选可以再加。
