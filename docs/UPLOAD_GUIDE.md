# 发布 / 上传指南（供作者本人使用，未执行任何上传）

本工具链**不会**自动上传 Steam 创意工坊或 GitHub——按项目要求，最终交付只在本机，确认 mod 没问题后再由你手动上传。

## 一、本地安装测试（不影响工坊版本）

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
powershell -ExecutionPolicy Bypass -File .\deploy.ps1 -Mode Local
```

- 会安装到 `...\Escape from Duckov\Duckov_Data\Mods\BetterHydrationEnergyHUD\`。
- 如果同时订阅了工坊版，请在游戏 Mods 列表里**只启用一个**（同名 mod 会显示名字冲突）。
- 测试完删除该文件夹即可。

## 二、更新工坊版本

### 方式 A：直接替换本机工坊文件（最快，适合自己先验证）

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
powershell -ExecutionPolicy Bypass -File .\deploy.ps1 -Mode Workshop
```

- 会把新 DLL + info.ini + preview.png 写入
  `...\Steam\steamapps\workshop\content\3167020\3591875771\`；
- 原文件备份在 `backups\workshop-<时间戳>\`，可用
  `.\deploy.ps1 -Mode Restore -Backup <备份目录>` 还原。
- 注意：Steam 校验文件 / 工坊重新下载会覆盖这些文件，所以这只适合临时验证。

### 方式 B：用游戏内置上传器正式更新（推荐）

1. `.\build.ps1` 生成 `dist\BetterHydrationEnergyHUD\`。
2. 把整个 `dist\BetterHydrationEnergyHUD\` 复制到
   `...\Escape from Duckov\Duckov_Data\Mods\`，确保文件夹里是：
   ```
   BetterHydrationEnergyHUD\
     BetterHydrationEnergyHUD.dll
     info.ini          （name / publishedFileId = 3591875771 已写好）
     preview.png
   ```
3. 启动游戏 → Mods 列表找到 `BetterHydrationEnergyHUD` → 点右侧「上传」按钮（作者账号才会显示）。
4. 上传完成后可在工坊页面确认版本；工坊标题/描述需要另外在「编辑工坊页面」中更新，可粘贴
   [WORKSHOP_DESCRIPTION.md](WORKSHOP_DESCRIPTION.md) 的内容。

## 三、GitHub 仓库更新（本地准备，未 push）

仓库已按源码工程整理好（`src/`、`docs/`、`build.ps1` 等）。提交前请：

```powershell
cd <repo>
git status
git diff
git add -A
git commit -m "Rewrite BetterHydrationEnergyHUD for game 2.3.30 (v2.0.0)"
# 确认无误后再自行 git push
```

> 按项目要求，本次没有执行 `git push`、也没有上传任何在线平台。

## 四、上传前检查清单

- [ ] `.\build.ps1` 通过（0 error / 0 warning）
- [ ] `verify.ps1` 通过（入口类型、继承关系、程序集白名单 / 零前置）
- [ ] 游戏内实测：进档后 HUD 数值显示正确、F10 面板可开、设置保存生效
- [ ] 关闭 mod / 换场景无残留 UI、无 `[BHE]` 报错
- [ ] 工坊页面描述已更新（功能、修复项、兼容版本、反馈方式）
- [ ] 备份旧工坊文件（deploy.ps1 自动备份）
