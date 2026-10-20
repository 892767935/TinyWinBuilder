# TinyWin Builder

开源 Windows 离线镜像精简工具，类似 NTLite 的免费替代品。

## 功能

- 📁 **离线镜像管理** - 挂载/卸载 install.wim，离线修改
- 📱 **预装应用移除** - 一键移除 Xbox、Bing、Cortana 等 UWP 应用
- ⚙️ **系统功能禁用** - IE、WMP、写字板等可选功能
- 🔧 **注册表 Tweaks** - 禁用 Defender、OneDrive、遥测等
- 🚀 **自动部署** - 生成 autounattend.xml 实现无人值守安装

## 下载

前往 [Releases](https://github.com/892767935/TinyWinBuilder/releases) 下载最新版本。

## 使用

1. 以管理员身份运行 `TinyWinBuilder.exe`
2. 选择 `install.wim` 文件
3. 挂载镜像
4. 勾选要移除的应用和功能
5. 卸载并提交更改

## 技术栈

- WPF (.NET 8)
- DISM (离线镜像操作)
- C#

## 开源协议

MIT
