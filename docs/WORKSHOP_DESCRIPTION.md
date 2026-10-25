# Steam 创意工坊说明文本（v2.0）

> Steam 创意工坊的「编辑标题与描述」页面支持按语种分别保存标题 / 描述（语言下拉菜单共 30 种）。
> 本文件保留简体中文、繁體中文、English 三个主要语种的文案；其余 27 个语种的完整文案见 [workshop-localizations.txt](workshop-localizations.txt)。
> 更新方式：工坊页面右侧「所有者控制 → 编辑标题与描述」→ 语言下拉选择语种 → 粘贴对应 TITLE / DESC → 保存。

---

# 更好的吃喝数值可视化 v2.0 更新啦！

这次是彻底的 2.0 重写，适配最新的游戏本体（2.3.30）。
还是那个水/饱腹圆环旁的小数字，但界面、性能、稳定性都重新做了一遍，应该会好用很多！

◆ 这次改了什么
· 数值改成事件驱动，不再高频轮询，性能开销几乎为零（实测约 0.3%~0.5% 帧耗时）
· 圆环上方显示差多少补满，下方显示“当前 / 上限”（当前值右对齐、斜杠居中、上限左对齐，看起来更整齐）
· 新增游戏内 F10 面板：打开时游戏暂停、鼠标可用，可以一边看 HUD 一边调位置/字号/格式，改完自动保存
· 每个数值旁边都有独立「重置」按钮，再也不怕手滑调坏，也不用为了一个数值重置全部
· 快捷键可以改了！F10 和其他 mod 冲突的鸭宝可以换键（默认 F10，也可以设成别的）
· 设置里新增了一个「吃喝数值」标签页，用的是游戏原生的样式，只放快捷键，不占常规设置的位置
· 单 DLL 多语言：跟随游戏语言自动切换简中 / 繁中 / English，不用再替换 language 文件夹里的 DLL 了
· 修复：进出场景时黑屏出现 “0 100/100” 的问题
· 修复：关闭面板后角色会“补开枪 / 补移动”的输入回放问题
· 依旧零前置！不需要 ModConfig / ModSetting / HarmonyLib，订阅就能用

◆ 操作
F10：打开 / 关闭本 mod 的设置面板（可在设置里改键）
每个数值右侧的「重置」：只重置这一项
面板底部「恢复默认」：重置全部

◆ 从旧版升级
旧的 settings.ini 会自动读取，位置 / 字号 / 显示开关都不用重新设置。
旧的 language 文件夹可以删掉了，2.0 只需要一个 DLL。

◆ 兼容性
游戏 2.3.30 实测通过；1.3.10 及以上应该都可以（官方设置页接口从 1.3.10 开始提供）。
不改游戏 UI 资源、不用 Harmony、不 patch 游戏函数。
如果装了完全替换 HUD 的 mod，数值位置可能需要用「位置微调」自己挪一下。

◆ 常见问题
看不到数字：先确认 mod 已启用，进档后按 F10 点一下“恢复默认”试试。
低分辨率下面板显示不全：面板支持滚动，鼠标滚轮往下滑就好。
只想要上面或下面：在设置里把不需要的那项关掉即可。

PS. 感谢大家一直以来的支持！从一开始只会显示两个数字，到现在可调位置、字号、快捷键、多语言，都是大家陪我一起把这个 mod 慢慢补完的。我爱你们 ♥
还有，之前说这个 mod 没用的群友……哼，现在有用了啦！（小声）
对了，立项的时候都没几个 mod，没想到刚写好就发现已经有大佬发了类似的，不过还是厚着脸皮发了（“更好”是相对游戏原版显示来说的，不是针对那位大佬啊啊）。
有些群友说这个 mod 没用，当时有点伤心……这次我会努力让它变得更有用的！

---

# 更好的吃喝數值可視化 v2.0 更新啦！

這次是徹底的 2.0 重寫，適配最新的遊戲本體（2.3.30）。
還是那個水/飽足圓環旁的小數字，但介面、效能、穩定性都重新做了一遍，應該會好用很多！

◆ 這次改了什麼
· 數值改成事件驅動，不再高頻輪詢，效能開銷幾乎為零（實測約 0.3%~0.5% 幀耗時）
· 圓環上方顯示差多少補滿，下方顯示「目前 / 上限」（目前值右對齊、斜線居中、上限左對齊，看起來更整齊）
· 新增遊戲內 F10 面板：開啟時遊戲暫停、滑鼠可用，可以一邊看 HUD 一邊調位置/字級/格式，改完自動儲存
· 每個數值旁邊都有獨立「重置」按鈕，再也不怕手滑調壞，也不用為了一個數值重置全部
· 快捷鍵可以改了！F10 和其他 mod 衝突的鴨寶可以換鍵（預設 F10，也可以設成別的）
· 設定裡新增了一個「吃喝數值」標籤頁，用的是遊戲原生的樣式，只放快捷鍵，不佔一般設定的位置
· 單 DLL 多語言：跟隨遊戲語言自動切換簡中 / 繁中 / English，不用再替換 language 資料夾裡的 DLL 了
· 修復：進出場景時黑屏出現「0 100/100」的問題
· 修復：關閉面板後角色會「補開槍 / 補移動」的輸入回放問題
· 依舊零前置！不需要 ModConfig / ModSetting / HarmonyLib，訂閱就能用

◆ 操作
F10：開啟 / 關閉本 mod 的設定面板（可在設定裡改鍵）
每個數值右側的「重置」：只重置這一項
面板底部「恢復預設」：重置全部

◆ 從舊版升級
舊的 settings.ini 會自動讀取，位置 / 字級 / 顯示開關都不用重新設定。
舊的 language 資料夾可以刪掉了，2.0 只需要一個 DLL。

◆ 相容性
遊戲 2.3.30 實測通過；1.3.10 及以上應該都可以。
不改遊戲 UI 資源、不用 Harmony、不 patch 遊戲函式。
如果裝了完全替換 HUD 的 mod，數值位置可能需要用「位置微調」自己挪一下。

◆ 常見問題
看不到數字：先確認 mod 已啟用，進檔後按 F10 點一下「恢復預設」試試。
低解析度下面板顯示不全：面板支援捲動，滑鼠滾輪往下滑就好。

PS. 感謝大家一直以來的支持！從一開始只會顯示兩個數字，到現在可調位置、字級、快捷鍵、多語言，都是大家陪我一起把這個 mod 慢慢補完的。我愛你們 ♥

---

# Better Hydration & Energy HUD v2.0 is here!

This is a full 2.0 rewrite, updated for the latest game build (2.3.30).
Same little numbers next to the water / hunger rings, but the UI, performance and stability are rebuilt — hopefully much nicer to use!

◆ What's new
· Event-driven values: no more high-frequency polling, near-zero performance cost (about 0.3%~0.5% of frame time in my test)
· Top: how much is missing to fill the ring. Bottom: current / max (current right-aligned, slash centered, max left-aligned)
· New in-game panel (F10): the game pauses while it's open and the mouse is usable, so you can tune position / font size / format while watching the HUD; changes save automatically
· Every value has its own “Reset” button — no more resetting everything just because you messed up one setting
· The hotkey is now rebindable! If F10 conflicts with another mod, just change it (default F10)
· A new native-styled “HUD Values” tab in the game settings — it only contains the hotkeys and doesn't take over the Common page
· One DLL for all languages: follows the game language (Simplified Chinese / Traditional Chinese / English). No more replacing the DLL in the language folder
· Fixed: “0 100/100” appearing on the loading screen when entering/leaving a level
· Fixed: after closing the panel, the character would “replay” clicks / movement (input buffering)
· Still zero prerequisites! No ModConfig / ModSetting / HarmonyLib needed

◆ Controls
F10: open / close the mod panel (rebindable in the settings)
“Reset” on each value row: reset only that value
“Reset to defaults” at the bottom: reset everything

◆ Upgrading from 1.x
Your old settings.ini is read automatically — positions, font size and visibility toggles are kept.
The old language folder can be deleted; 2.0 needs only one DLL.

◆ Compatibility
Tested on game 2.3.30; should work on 1.3.10+ (the official options hook was added in 1.3.10).
Doesn't modify game UI assets, doesn't use Harmony, doesn't patch game functions.
If another mod fully replaces the HUD, you may need to adjust the position with the offset sliders.

◆ FAQ
Can't see the numbers: make sure the mod is enabled, then load a save, press F10 and click “Reset to defaults”.
Panel doesn't fit on low resolutions: the panel scrolls — use the mouse wheel.
Only want the top or bottom line: toggle the one you don't need in the settings.

PS. Thank you all for the support! From a simple two-number display to adjustable positions, font sizes, hotkeys and multi-language — we built this together. Love you all ♥
