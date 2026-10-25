# 发布 / 上传指南（供作者本人使用）

本指南记录 v2.0.0 实际上架时使用的流程。Steam 工坊与 GitHub 的更新都已由作者账号完成；
以后更新可以照下面的步骤复用。

## 一、本地安装测试（不影响工坊版本）

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
powershell -ExecutionPolicy Bypass -File .\deploy.ps1 -Mode Local
```

- 会安装到 `...\Escape from Duckov\Duckov_Data\Mods\BetterHydrationEnergyHUD\`。
- 如果同时订阅了工坊版，请在游戏 Mods 列表里**只启用一个**（同名 mod 会显示名字冲突）。
- 测试完删除该文件夹即可。

## 二、更新工坊内容（DLL / info.ini / preview）

### 方式 A：游戏内置上传器（推荐给日常更新）

1. `.\build.ps1` 生成 `dist\BetterHydrationEnergyHUD\`。
2. 把整个文件夹复制到
   `...\Escape from Duckov\Duckov_Data\Mods\`，确保里面有：
   ```
   BetterHydrationEnergyHUD\
     BetterHydrationEnergyHUD.dll
     info.ini          （name / publishedFileId = 3591875771 已写好）
     preview.png
   ```
3. 启动游戏 → Mods 列表找到 `BetterHydrationEnergyHUD` → 点右侧「上传」按钮（作者账号才会显示）。
4. ⚠️ **上传器会用 `info.ini` 的 `displayName` 覆盖工坊的默认（英语）标题。**
   当前 `info.ini` 的 `displayName` 是中文标题；上传后请到工坊页面把英语标题改回
   `Better Hydration & Energy HUD`（见第三节），否则英语页面会显示中文标题。

### 方式 B：Steamworks 独立上传脚本（本机内部工具，可选）

本机升级时使用了一个独立的小工具（`work\uploader\SteamUploader.exe`，未放入仓库）：
本机 Steam 客户端已登录时，直接对同一个 PublishedFileId 调用 `SteamUGC` 提交，
不依赖游戏进程（游戏是提权进程时也能用），并会附带更新说明（Change Note）。

参数：`SteamUploader.exe <mod文件夹绝对路径> <标题> [更新说明]`

> 该工具只用于作者本机；普通更新用游戏内置上传器即可。

## 三、工坊描述（30 种语言）

Steam 工坊的「编辑标题与描述」页面支持按语种分别保存标题 / 描述（语言下拉共 30 种）：

1. 打开工坊条目页面 → 右侧「所有者控制 → 编辑标题与描述」；
2. 语言下拉选择语种 → 填标题 / 描述（BBCode 可用 `[h1] [b] [list] [*] [hr] [i]`，**不支持 `[center]`**）→ 保存；
3. 全部语种文案源文件：[workshop-localizations.txt](workshop-localizations.txt)；
   简中 / 繁中 / English 预览见：[WORKSHOP_DESCRIPTION.md](WORKSHOP_DESCRIPTION.md)。

**本次已保存 30 个语种**（含原本空着的马来语），每个语种的标题也做了本地化。

## 四、GitHub 仓库更新

仓库已由作者账号 `DoreiHime` 推送（v2.0.0 提交）。以后更新：

```powershell
cd <repo>
git status
git diff
git add -A
git commit -m "v2.0.0: update for game version ..."
git push origin main
```

仓库 About 简介与 topics 可在 GitHub 页面 Settings 或 `gh repo edit` 中更新。

## 五、上传前检查清单

- [ ] `.\build.ps1` 通过（0 error / 0 warning）
- [ ] `verify.ps1` 通过（入口类型、继承关系、程序集白名单 / 零前置）
- [ ] 游戏内实测：进档后 HUD 数值显示正确、F10 面板可开、设置保存生效
- [ ] 关闭 mod / 换场景无残留 UI、无 `[BHE]` 报错
- [ ] 工坊描述已按语种更新（30 种）
- [ ] 更新后确认英语标题仍为 `Better Hydration & Energy HUD`
- [ ] 确认工坊页面「更新日期 / 文件大小」已变化、改动说明正常
- [ ] 备份旧工坊文件（`deploy.ps1` 自动备份到 `backups\workshop-<时间戳>\`）
