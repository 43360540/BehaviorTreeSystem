# BehaviorTree.Core — 全面評估報告

> 評分制：0-10，每維度附 2-3 個具體觀察。
> 證據基礎：[`BT_ClassFirst_DEV_NOTES.md`](./BT_ClassFirst_DEV_NOTES.md) 三大 Part、PerfDump 實測數據、自寫 30+ 檔案的 hands-on 開發。
> 評估範圍：framework 本身為主；Class-First demo 作為個案研究提供證據。

---

## 維度分數總覽

| # | 維度 | 分數 |
|---|---|---|
| 1 | API 設計 | **6** / 10 |
| 2 | 可學習性 | **4** / 10 |
| 3 | 可擴充性 | **8** / 10 |
| 4 | 可測試性 | **8** / 10 |
| 5 | 可維護性 | **6** / 10 |
| 6 | 執行性能 | **9** / 10 |
| 7 | Unity 整合 | **5** / 10 |
| 8 | 文件 / 範例 | **2** / 10 |
| 9 | 錯誤處理 / Debug | **4** / 10 |
| 10 | 生態系 | N/A（資料不足） |

```
9 ┤   ●(Perf)
8 ┤   ●●(Extend, Test)
7 ┤
6 ┤   ●●(API, Maintain)
5 ┤   ●(UnityIntegration)
4 ┤   ●●(Learn, Debug)
3 ┤
2 ┤   ●(Docs)
1 ┤
```

**Pattern**：典型的「**核心引擎乾淨、但缺貨**」型 framework — 強項是 core engineering、弱項是 onboarding 與整合品質。

---

## 1. API 設計　**6/10**

**強**：
- 命名 Unity-friendly，不衝突（`ActionBase` / `ICondition` / `NodeStatus`）
- Fluent builder 設計流暢，縮排即視覺結構
- `NodeBase<TContext>` 用泛型 Context 而非強制繼承 `BlackboardBase`，給使用者完全自由
- 介面精簡（`ActionBase` 5 virtual + 1 abstract 剛好）

**弱**：
- **缺 `Func<TContext, bool>` overload** — `.When`/`.Do`/`.Check` 只給 `(ctx, dt)`、`(dt)`、`()` 三種。寫 `ctx => ...` 會解析到 `Func<float, bool>`，`ctx` 變 float，錯誤訊息誤導（`'float' does not contain 'HasTarget'`）
- `Reset()` 沒 ctx 參數，跟 `Start/Stop/Abort` 不一致
- `MonoBTRunner._context` 用 `[SerializeField]` 對 Class-First 完全無意義（純 C# class 無法 Inspector 設）

---

## 2. 可學習性　**4/10**

**強**：
- 概念符合 BT 文獻，老手能直觀讀懂
- NPCSample 提供「可跑」的範例可參考

**弱**：
- 完全沒 README / docs
- 沒有 Class-First 範例（NPCSample 是 State-Style）
- Sample stub 全是 `NotImplementedException`，直接跑會炸
- `Reset` / `TimeElapse+Tick` 兩階段的設計理由完全沒文件
- 「兩種風格何時選哪個」沒指南，使用者進來會分裂

---

## 3. 可擴充性　**8/10**

**強**：
- 加 13 個自訂 actions + 5 個 Runner + 4 個 specific actions **完全沒改 framework 源碼**
- composite / decorator / action / condition 各自有清楚擴充點
- QuickAction / QuickCondition 用 lambda 快速包裝，免寫 class

**弱**：
- 加 fluent builder extension method 需要看現有 `.Selector` 等模板照抄，新手不一定看懂泛型 + extension method
- 沒「擴充 cookbook」

---

## 4. 可測試性　**8/10**

**強**：
- Class-First Action 純 C#，可以 `new Sense().Tick(mockContext, dt)` 直接單元測試
- Context 是 plain class，mock 容易
- BTRunner / NodeBase 不依賴 Unity Editor

**弱**：
- State-Style 完全綁 MonoBehaviour，只能 PlayMode 測
- 沒官方 mock helper / test context builder

---

## 5. 可維護性　**6/10**

**強**：
- Class-First 「一檔一 class」拆得細，refactor 友善
- 大量 `sealed` 阻擋意外繼承

**弱**：
- Class-First 樣板程式碼多（200 NPC 規模 978 行 vs State-Style 729 行，多 25%）
- 加 stat / 加 ctor 參數要改三層（BTContext → BaseNPCRunner → setup script）
- Fluent builder lambda capture 跟 instance field 互動很多，refactor 時要小心

---

## 6. 執行性能　**9/10**

**強**：
- **200 NPC 的 BT tick 只吃 0.476 ms**（PerfDump 實測）
- 平均每 NPC tick ~2.4 μs，包含 5 層 nested + Parallel + Sense + Action
- 200 NPC 戰場 130 fps，BT 佔 frame budget 不到 6%
- 10000 NPC 壓力測試實測 scripts 39.6 ms / frame（佔 frame budget 18%），實際 BT 在 **~8000 NPC** 才會變主要 CPU 瓶頸（rendering 在 10000 NPC 時是更大瓶頸）
- Zero framework-side GC

**弱**：
- 沒做 micro-optimization（如 Selector 短路 cache）— 在這個規模看不出影響
- 沒官方 benchmark 數字宣傳

---

## 7. Unity 整合　**5/10**

**強**：
- `MonoBTRunner<TContext>` 是乾淨的入口
- TickRate 可選 Update / FixedUpdate
- 不強制 NavMesh / Animator

**弱**：
- NPCSample 直接綁 NavMeshAgent → 給新人「Class-First 也該綁 agent」的暗示，導致 200 NPC 規模踩 NavMesh 雷 4 個（NavMeshSurface.size、bake 時序、disabled agent throw、asset 持久化）
- 沒「移動策略可插拔」的官方範例
- 沒處理 Editor PlayMode 的 setup race condition

---

## 8. 文件 / 範例　**2/10**

整個 framework 最大短板。

**弱**：
- 沒 README
- 沒 API doc / XML comment
- 沒 quick start
- Sample stubs 全 NotImplementedException
- 沒 Class-First 範例
- 沒「兩種風格 vs」guide
- 沒踩雷文件
- 沒 cheat sheet
- 沒整合 cookbook

---

## 9. 錯誤處理 / Debug　**4/10**

**強**：
- `BTDebugger.DrawTree()` 印整棵樹狀態
- NodeStatus 概念清晰
- 部分例外訊息有寫

**弱**：
- Action 內 throw 中斷整個 Tick — framework 應在 `NodeBase.Tick` 內 try-catch
- `Get(State.X)` 找不到丟裸 `KeyNotFoundException`
- StateStyle 方法簽名錯不在 compile time 抓
- `BTDebugger` 沒標記「哪個 node last exception」

---

## 10. 生態系　**N/A**

未實際探查 GitHub stars / community / tooling。粗略觀察：個人專案、沒有第三方 plugin、沒看到 community tutorial。建議使用者自己 review。

---

# 適合誰用

✅ **適合**：
- 已熟 BT 概念、想要輕量框架的中階以上 Unity 開發者
- 中型專案（< 5000 NPC、< 100 種 NPC 類型）
- 追求可單元測試、Action 可重用、code-first 開發風格的團隊
- 願意自己摸 source 補 docs 的人

❌ **不適合**：
- BT 新手（沒文件容易卡死）
- 想要視覺化編輯器的 designer-friendly 團隊
- plug-and-play 立即可用 prototype 階段
- 不想處理 NavMesh / Animator 整合細節的人

---

# 給 framework 作者的優先級建議

按 ROI（修改成本 / 影響度）排序：

| # | 改動 | 預估時間 | 影響 |
|---|---|---|---|
| 1 | 補 `Func<TContext, bool>` overload | 1 小時 | 每個新使用者必踩 |
| 2 | 寫一頁 README + cheat sheet | 4 小時 | 學習曲線大幅平緩 |
| 3 | 補完 Sample stubs 或刪掉 | 2 小時 | 避免第一印象炸 |
| 4 | 加 `NodeBase.Tick` try-catch wrap | 4 小時 | debug 大改善 |
| 5 | 寫 Class-First 範例 | 1 天 | 文件痛點解決一半 |
| 6 | NavMesh / Animator 整合 cookbook | 2 天 | user-facing 體驗大幅提升 |

修完前 4 項，可學習性 / 文件 / Debug 三項分數可以各拉 2-3 分，整體 framework 可從「**個人專案級**」升到「**中型團隊願意採用**」級。

---

# 整體判斷

- **設計瑕疵**（難改、會 breaking change）→ **基本沒有**。Core engine 已成熟。
- **缺貨**（補上就好、不會 break user code）→ **多**。文件、整合範例、debug helper 都是「**作者願不願意花時間補**」的問題，非結構性。

技術層面已過關，剩下純粹是 **maintenance investment** 的問題。
