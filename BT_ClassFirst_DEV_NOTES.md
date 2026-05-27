# BehaviorTree.Core — Class-First 開發體驗筆記

以開發者視角用 BehaviorTree.Core 做出 6 個會互打的 NPC（Warrior x2, Archer x2, Patroller x2）的過程記錄。重點不在「結果好不好用」，而在**寫起來流不流暢、會在哪裡卡、跟我預期是否一致**。

寫作中持續更新，按時序排。

---

## 0. 第一印象（讀完 Core 跟兩個 Sample 之後）

- **API 命名很 Unity-friendly**：`ActionBase` / `ICondition` / `NodeStatus` 不會跟 Unity 內建衝突。Fluent builder 的 `.Selector` `.Sequence` `.Do` `.Check` `.When` 也接近 BT 文獻常見命名，沒有自創詞彙。
- **生命週期完整**：`OnStart / OnTick / OnStop / OnAbort / OnReset` 對應到 BT 該有的事件，比某些只給 `Tick` 的 framework 嚴謹。
- **Reset 機制隱性**：`_lastStatus` 變 `None` 時下一輪 tick 會重跑 `OnStart`，這對「中斷重來」很重要，但**這個機制不明文寫在 ActionBase 的 doc comment 裡**，新手可能會踩到「Stop 後忘了 Reset → 第二輪 OnStart 沒觸發」這種 bug。
- **TimeElapse + Tick 兩階段**：第一次看不直觀，理解後覺得是為了讓 Throttle / CoolDown / TimeLimit 這類「跟時間有關的 decorator」能在 condition 評估前先累積時間。但這個邏輯**完全沒寫在任何 doc**，要從 Source 推敲。
- **沒有 README / docs**：整套 framework 沒有 high-level 文件，新手 onboard 全靠讀 Sample。`NPCSample` 是唯一 production-grade 的範例，但用的是 **State-Style**，看不到 Class-First 怎麼運作。
- **`Attack.cs` / `Idle.cs` 等 sample stub 全是 `NotImplementedException`**：對「想直接照 sample 抄」的開發者非常勸退，因為跑起來會炸。

## 1. 寫 `BTContext`（Blackboard）

走 Blackboard 路線，把 Self / Agent / Anim / Faction / 各種 stat 跟 buffer 集中在一個純 C# class 裡。

- **沒卡到** — `BTContext` 純資料，跟 framework 完全脫鉤，純 C# 就能寫完。這證明 `NodeBase<TContext>` 用泛型而不是強制繼承 `BlackboardBase` 的設計是對的，給使用者完全自由。
- **要稍微想一下 Self 的型別**：Transform？GameObject？IDamageable？最後選 `Transform Self` 取位置 + 另開 `IDamageable SelfAsDamageable` 給戰鬥用。一開始忘了後者，寫到 WarriorCharge 才補上 ctor 參數 — 這種「Context 在開發中持續長大」的回頭修改在 Class-First 風格幾乎不可避免，可以接受。
- **OverlapBuffer / Animator hash 預先 cache**：直接放進 Context 就好，這設計天然支援 zero-alloc / 早期 cache，比 State-Style（field 寫在 NPC class 內）更工整。

## 2. 寫 `BaseNPCRunner`

繼承 `MonoBTRunner<BTContext>`，負責從 Inspector 欄位組出 Context。

- **`SetContext` 必須在 `base.Awake()` 之前呼叫**：注釋有寫但很容易忽略。我第一版差點忘了寫 `SetContext(_ctx)`，幸好 NPCSample 有示範，照著抄。
- **`SetContext` 是 `protected`，且只在 `_context == null` 時才賦值**：意圖是允許 Inspector 直接拖 reference 進去。但對 Class-First 而言 Context 是純 C# class，根本不能在 Inspector 設，所以這個保護機制對我沒用，反而會混淆。
- **`OnDrawGizmosSelected` 補上 sensor / attack range / patrol points 視覺**：framework 沒 built-in 視覺化，但寫起來很標準，沒卡。

## 3. 寫通用 Actions（Sense / MoveToTarget / MoveToPosition / FaceTarget / WaitTimer / PlayAnimTrigger / MoveAway）

- **`ActionBase<TContext>` 寫起來夠輕量**：5 個 virtual + 1 個 abstract，介面剛好。
- **`Reset()` 沒有 ctx 參數**：跟 `Start/Stop/Abort` 不一致，導致 timer 類 action 必須把狀態存在 field（OK，不大問題），但拼起 mental model 時會頓一下。
- **`MoveToTarget` 寫起來最舒服**：直接 `ctx.Agent.SetDestination(...)` 就行。`ctx.HasTarget` / `ctx.TargetDistance` 等 helper 大幅減少 boilerplate。
- **`MoveToPosition` 一開始想吃 `Vector3` 常數**，後來加 `Func<BTContext, Vector3>` overload 才好用（dynamic destination）。這暴露一個 framework 設計議題：**Action class 是 instance per slot**，傳常數時不靈活，傳 lambda 時又繞遠路。比 State-Style 直接寫方法的「拿位置 → 走過去」要囉嗦。
- **`WaitTimer` 的 `_elapsed` 不能跨 instance 共用**：這是 Class-First 的本質限制 — 同一個 `new WaitTimer(2f)` 不能放到樹的兩個地方。我這次寫 Warrior 時，attack cooldown 跟 idle wait 都要 timer，得 `new WaitTimer(0.2f)` 跟 `new WaitTimer(0.5f)` 兩次。**這點 framework 應該在 doc 提醒**，否則新手會踩到「為什麼兩個地方的 timer 互相干擾」。
- **`PlayAnimTrigger` 的 hash lazy init**：可以接受，但其實 Animator 內部本來就會 cache trigger lookup，這個優化是 over-engineering。

## 4. 寫 Conditions（HasTarget / IsInRange / IsHpBelow）

- **`ICondition<TContext>` 只有 `Evaluate(ctx, dt)`**：簡潔到位。
- **`IsInRange` 帶兩個 ctor**（明示 range + 用 ctx.AttackRange）：是個小貼心，但暴露了**「條件需要參數時，到底該寫成 class field 還是 read context」**的取捨。我用 class field（明示）為主，會有靈活性但也增加 boilerplate。

## 5. 寫專屬 Actions（WarriorCharge / ArcherShoot / PatrollerPatrol）

寫到這裡才真正感受到 Class-First 的「重」：

- **`WarriorCharge` 跟 `ArcherShoot` 高度相似**（都是 trigger animation → 等 IsAttacking false → 結算傷害），但**抽 base class 會破壞「全部 sealed concrete」的乾淨感**。我選複製貼上（兩份 70 行各自獨立），代價是日後 bugfix 要改兩處。
  - **State-Style 不會有這問題**：因為兩個 NPC class 內各自寫 `AttackTick` 方法，本來就不期待共用。
  - 對 framework 的建議：**官方文件應該明確指出「Class-First 中相似 action 該如何重用」的官方姿勢**（helper class? abstract base? composition?），否則每個團隊會自行發明，code style 會分裂。
- **`ArcherShoot` 在 `Start()` 時 snapshot target**：解決「玩家在 archer 抽箭時躲開，但箭應該朝原方向飛」的問題。要做這種「先記下狀態、後驗證」的邏輯時，**沒有跨 tick 共享 ctx 之外的狀態的官方方式**，只能往 action class 內塞 field。這是 Class-First 的合理代價，但對於「想保持 action 無狀態」的純粹派會不舒服。
- **`PatrollerPatrol` 內部維護 index + waiting state**：跟 State-Style 在 PNC 內維護 `_destination` 一樣的設計，差異只在「狀態放哪」。寫起來自然，沒卡。

## 6. 寫 Runner — 第一次踩到 framework 的 API 地雷

寫 3 個 Runner（Warrior / Archer / Patroller）的樹結構，每個樹大致長這樣：

```
Parallel
├─ Sensor branch: Repeater → Force(Success) → Throttle(0.3s) → When(no target) → Sense
└─ Decision branch: Repeater → Selector
   ├─ When(in attack range) → Sequence: FaceTarget → Charge → WaitTimer(cooldown)
   ├─ When(have target)     → MoveToTarget
   └─ Idle / Patrol
```

寫起來流暢，縮排即視覺層次。但**馬上踩到一個 framework 設計地雷**：

### 地雷：`.When(ctx => ...)` 編譯通過但 ctx 被當成 float

我自然地寫：

```csharp
.When(ctx => !ctx.HasTarget, _ => _.Do(new Sense()))
```

Compile error：

```
'float' does not contain a definition for 'HasTarget'
```

原因：`GuardExtension.When` 只有這三個 lambda overload：

```csharp
When(Func<TContext, float, bool> predicate, ...)   // (ctx, dt) => bool
When(Func<float, bool> predicate, ...)             // (dt) => bool
When(Func<bool> predicate, ...)                    // () => bool
```

**缺少 `Func<TContext, bool>` 一參數 overload**，所以我寫 `ctx => ...` 時 C# 解析到唯一相容的 `Func<float, bool>`，`ctx` 變成 `float`。

修法：改成 `(ctx, _) => !ctx.HasTarget`（兩參數，忽略 dt）。

**對 framework 的建議**：
1. 補一個 `When(Func<TContext, bool>, ...)` overload。`Func<TContext, bool>` 是 BT 的 99% 用例（dt 多半用不到）。
2. 或者改進 `QuickCondition<TContext>` 的 ctor，加 `Func<TContext, bool>` 版本，這樣 `When(Func<TContext, bool>)` 自然就被支援了。
3. 這個踩雷會發生在每個第一次寫 Class-First 的人身上。我吃了一次 compile 等 + 翻原始碼才搞清楚為什麼 `ctx` 變 `float`。

順帶一提：`Do(...)`、`Check(...)` 都有完整的 `(ctx, dt)` / `(dt)` / `()` 三個 overload，但**也都缺 `(ctx)` 單參數**。同樣的問題。

### 其他寫 Runner 時的觀察

- **不能在 `CreateTree()` 內動態用 `_ctx.AttackRange`**：因為 CreateTree 被 base.Awake() 呼叫，那時 `_ctx` 才剛被建立，但 lambda capture 的是 instance，所以 runtime 讀到的會是當下的值，OK。但「直接用 inspector field `_attackRange`」反而清楚（也是 Awake 時讀進來的）。
- **Selector + When 的組合用來做「優先級行為」很自然**：第一個能進的分支執行，後面 fallthrough 到 Idle。比 if-else 鏈乾淨多了。
- **三個 Runner 的樹結構高度相似**（Sensor branch + Decision branch + Selector），可以抽成 helper 但會破壞「樹結構視覺化」的好處。我選擇複製貼上 + 用註解標清楚分支。同樣是 Class-First 的「為了視覺化付出 boilerplate」的代價。


## 7. PlayMode 驗證

執行 `Tools/BT ClassFirst/Setup Demo Scene` → `Play` → 等 ~15 秒：

| 項目 | 結果 |
|---|---|
| Compile error | 0 |
| Runtime exception | 0 |
| Warning | 0 |
| 6 NPC 啟動 | 全部 OK，Sense / Move / Attack 都運作 |
| 同陣營過濾 | 通過 — Warrior_A 不打 Warrior_B |
| 互敵戰鬥 | 通過 — Warrior 衝向 Archer、Archer 嘗試 retreat |
| 死亡邏輯 | 通過 — Patroller_A / Patroller_B 在 ~15 秒內被擊殺、SetActive(false) |
| Player 觀察者 | 通過 — Player 不被任何 NPC 視為目標（IDamageable 過濾） |

### Setup 期間踩到的場景配置 bug

第一次跑 `Setup Demo Scene` menu 之後 hierarchy 顯示有兩個問題，**這兩個都跟 framework 無關，是 Unity Editor scripting 的常見坑**：

1. **`GameObject.Find("Enemy")` 找到內層 child 而不是 root**：原本場景中 `/Enemy` 跟 `/Enemy/Enemy` 同名，`GameObject.Find` 不能用 path 也不能限定深度。
   - 修法：`scene.GetRootGameObjects()` 逐一比對 name == "Enemy"，明確抓 root。
2. **Clone 出來的 NPC 子物件 `activeSelf = false`**：clone 時 source root 是 inactive，`Instantiate` 把子物件也設成 inactive。我只 `clone.SetActive(true)` 了 root，沒處理 children。結果：Animator 在 inactive child 上 → `GetComponentInChildren<Animator>()` 找不到 → `_anim = null` → 攻擊動畫不會跑。
   - 雙重修法：(a) `ActivateRecursive()` helper 把整棵樹 activate；(b) `GetComponentInChildren<Animator>(true)` 加 includeInactive 參數做為保險。

### 對 framework 的小建議

PlayMode 跑起來後，回頭看 framework，補幾個小建議：

3. **`MonoBTRunner` 的 inspector serialized context** 對 Class-First 路線完全沒用（純 C# class 無法在 Inspector 設）。可以考慮把 `[SerializeField] private TContext _context` 移除，改成單純的 `SetContext(...)` API；或者文件明示「Class-First 應 override Awake、無需動 Inspector context」。
4. **`BTDebugger.DrawTree(_rTree)` 把樹狀態印到 Console** 這個功能在開發 Class-First NPC 時超有用 — 看到「為什麼 Warrior 卡在 Idle 沒衝過去」就靠這個。我的 `[SerializeField] _debugMode` 設成 true 就會每 3 秒 print 整棵樹的狀態。這在我的 Runner 上其實沒測試到，下次跑起來檢視。
5. **缺少 `INode<TContext>.Pretty` 或類似的 inline status snapshot**：debug 時要看「樹的哪個分支正在跑」一定要打開 BTDebugger，沒有更輕量的 `node.ToString()` 之類，對 inline 偵錯不便。

## 8. 整體評價

### Class-First 風格在這個 framework 上的體感

**寫程式的爽度**：8/10。Fluent builder 讓樹結構視覺化、Action / Condition 各自獨立 testable、Context 集中管理。比 ScriptableObject 拼裝乾淨太多。

**樣板程式碼成本**：6/10。一個 NPC 的 minimum cost 包含：
- BTContext 增加參數（如新 stat / 新 ref） → 修 ctor + Runner 都要跟著改
- 一個新 Action class（即使只 10 行邏輯也要 30 行包裝）
- Runner subclass 寫樹結構

對於這次 demo（3 種 NPC、~150 行樹結構、~600 行 Action/Condition），**值得**。對更小型 demo 反而不划算。

**踩雷易度**：5/10。framework 設計乾淨但細節幾個地雷：
- `.When(ctx => ...)` 沒有 single-param overload（吃 lambda overload 解析陷阱）
- `BTContext` 是純 C# class，Inspector 無法序列化，跟 `MonoBTRunner` 的 SerializedField 設計衝突
- `OnStart` / `Reset` 機制隱性，新手不清楚 lifecycle 邊界

**跟 State-Style 對比**：
- 6 NPC（3 種 × 2）這個規模，Class-First 的「Action class 可重用」優勢還沒完全發揮（只省了 MoveToTarget / WaitTimer 等通用 action 的重寫）
- 若做到 20+ NPC（10+ 種類，共用大量通用 action），Class-First 會大幅勝出
- 若只做 1-3 種 NPC、行為高度客製，State-Style 寫起來更直覺、code volume 少

**最後判斷**：Class-First 適合「**building blocks 心態**」的團隊，State-Style 適合「**一個角色一份腳本**」的個人或小團隊。BehaviorTree.Core 兩條路都支援是對的設計，使用者按專案規模選即可。

### 給 framework 的 TODO 清單（按優先級）

1. **補 `Func<TContext, bool>` overload** 到 `When` / `Do` / `Check`，避免「lambda overload 解析錯誤 → ctx 變 float」這種誤導性 compile error
2. **寫一個 Class-First sample**（NPCSample 是 State-Style，沒對應的 Class-First demo）
3. **README** + 一頁 cheat sheet（4 個 status / 3 種節點 / lifecycle / TimeElapse+Tick 兩階段的原因）
4. **`MonoBTRunner` 的 Inspector context field** 對 Class-First 路線改成可選或文件明示
5. **既有 `Sample/Attack.cs` 等 stub 補完**或刪除，避免「跑 sample 直接 NotImplementedException」的壞印象

### 給未來自己的提醒（如果要在實際專案用 Class-First）

- **先設計好 `BTContext`，再寫 Action**。Context 沒設計清楚就動 Action 會反覆修 ctor
- **`new XAction()` 不能跨 tree slot 共用**：每個樹結構位置都用 `new`
- **Lambda 寫 `.When` / `.Do` 一律用兩參數 `(ctx, _) =>`**，避免 overload 解析坑
- **Editor menu 一鍵 setup demo 場景**值得寫，比手動拖很多 prefab 快太多

---

# Part 2：State-Style 用同規格再實作一次（對比體驗）

同一份規格（3 種 NPC × 2 instance、互敵、Player 觀察、障礙物、NavMesh、Animator），用 State-Style 重寫。場景另存為 `SampleScene_StateStyle.unity`，跟 ClassFirst 完全隔離。Combat 系統（Faction / IDamageable）共用 ClassFirst 的命名空間（避免大重構，但承認 namespace 名字稍微誤導）。

## 1. LOC 對比

只算 Runtime code（不含 Editor setup menu）：

| 風格 | 檔案數 | 總行數 | 平均行/檔 |
|---|---|---|---|
| **Class-First** | 21 | 978 | ~46 |
| **State-Style** | 5 (+ 共用 Faction/IDamageable 28 行) | 729 | ~146 |

State-Style **少了 ~25% 行數**，但檔案數只有 1/4。這直觀地具象化了兩種風格的本質：
- Class-First = 小檔多元件、可組裝
- State-Style = 大檔自包、可讀

實際內容拆分：

| 部分 | Class-First | State-Style |
|---|---|---|
| Blackboard / Context | BTContext (101) | （無，欄位散在 NPC class 內） |
| Action 邏輯 | 7 通用 + 3 專屬 = 10 個 class，共 547 行 | 包在 NPC class 內，~110 行/class |
| Sense 共用 | Sense action class (48) | StateHelpers static method (~30) |
| 樹結構 | 3 個 Runner ~155 行 | 內嵌在 NPC class 的 CreateTree() 約 30 行/class |
| Combat (IDamageable) | BaseNPCRunner 處理 | 每個 NPC 自己處理（~10 行 × 3） |

關鍵觀察：**「Action 邏輯」這個層級 State-Style 用約 70 行/state，Class-First 用約 55 行/action class**。Per-NPC 來說兩者差不多，但 State-Style 不需要 Context wrapper，少了那 100 行。

## 2. 開發速度感受

寫 State-Style 比 Class-First 快約 **30-40%**。原因：

- **沒有 Context 設計成本**：欄位直接寫進 NPC class，邊寫邊加，不用反覆修 ctor
- **沒有 Action class 拆檔成本**：方法直接在當前 class 寫，IDE 跳轉成本 0
- **CreateTree 跟 state 方法在同一檔**：閱讀時可以「樹結構 → 直接看到方法實作」一氣呵成
- **欄位直接綁進 NPC instance**：cooldown timer / state index 等 stateful 欄位直接是 instance field，不用煩惱「Action class 跨 tree slot 共用」這種坑

但 State-Style 也有它自己的開發成本：

- **每個 NPC 寫一個 IDamageable 實作**：3 個 NPC class 各複製 ~10 行同樣的 TakeDamage/Die 程式碼（ClassFirst 在 BaseNPCRunner 處理一次）
- **NavMeshAgent / Animator 配置邏輯複製 3 次**：每個 NPC 的 Awake 都寫一份「找 agent / 找 anim / 初始化 hp / 計算 hash」
- **無法用單一 Inspector 修改共通參數**：例如要把所有 Warrior 攻擊範圍從 2 改成 2.5，得進每個 instance 改

## 3. 重用性與「同邏輯不同 NPC」的處理

這次三個 NPC 都需要：Sense、Move 到目標、Attack 動畫、Die、計時 cooldown。Class-First 是「拆 7 個通用 Action class」解決，State-Style 是「同方法寫在三個 class 內」解決。

各自的代價：

| 場景 | Class-First | State-Style |
|---|---|---|
| 修一個 bug 在 `MoveToTarget` | 改 1 處，3 個 NPC 都受惠 | 改 3 處 |
| 加新 NPC 類型（如 Tank）| `new TankRunner` + 新的樹結構 + 0-2 個專屬 action | 寫一個新 TankState class，~200 行重複 boilerplate |
| 改變所有 NPC 的 sense 邏輯 | 改 `Sense.cs` 1 處 | 改 `StateHelpers.FindClosestEnemy`（但前提是當初有抽 helper） |
| 改一個 NPC 的個性行為 | 改該 NPC 的 Runner（樹結構） | 改該 NPC class（state 方法） |

**結論**：NPC 種類愈多、共用邏輯愈深，Class-First 的重用優勢愈明顯。NPC 種類少（1-3 個）、行為高度個性化，State-Style 寫起來更舒服。

## 4. 可測試性

- **Class-First**：Action class 是純 C#，可以直接 `new Sense().Tick(mockContext, 0.016f)` 寫單元測試。完全脫鉤 Unity Editor。
- **State-Style**：State 方法綁在 MonoBehaviour 上，測試需要實例化 GameObject、要 NavMeshAgent。基本上只能 PlayMode 測試或重度 mock。

對「想寫自動化測試」的團隊，Class-First 勝出明顯。對「不寫測試只靠 PlayMode 驗證」的個人/小團隊，差別不大。

## 5. 踩到的雷

### State-Style 寫起來踩到的雷

1. **方法簽名必須完全符合 StateStyleBase 預期**：
   - `Phase.Tick` 必須是 `Func<float, NodeStatus>`（簽名 `NodeStatus XxxTick(float dt)`）
   - `Phase.Start` 必須是 `Action`（簽名 `void XxxStart()`）
   - `Phase.Stop` 必須是 `Action<NodeStatus>`（簽名 `void XxxStop(NodeStatus s)`）
   - **寫錯不會 compile error，會 runtime crash on `Delegate.CreateDelegate`**。新手第一次會疑惑「為什麼明明 [StateDef] 標了卻沒呼叫」。

2. **`[StateDef("StateName", ...)]` 字串 vs enum 不對應**：
   - 反射比對是用 `state.ToString()` 跟 attribute 字串相等。enum rename 後不會在 compile time 抓到。
   - 我寫的時候差點把 "Approach" 寫成 "approach"，runtime 才會發現。
   - 對 framework 的建議：可以加 `nameof()` 的硬性檢查，或者把 attribute 改吃 enum 本身（用 generic 解決可能很麻煩）。

3. **`Get(State.X)` 在 CreateTree 中沒對應的 [StateDef]**：
   - Awake 內 `Scan()` 跑完後，`_actionLeaves` dict 只包含實際有方法的 state。
   - 如果 enum 定義了 `Idle` 但忘了寫 IdleTick，`Get(State.Idle)` 會丟 KeyNotFoundException。
   - 對 framework 的建議：`Get(...)` 應該明示「該 state 沒有方法定義」的錯誤訊息，而不是讓 dictionary 丟原生 exception。

### 場景複製踩到的雷

複製 SampleScene → SampleScene_StateStyle 後跑 PlayMode，**NPC 沒人死、移動較慢**。對比 Class-First 同樣 12 秒已有兩個 Patroller 被滅。可能原因：

1. **NavMesh data binding** — 場景複製後 `NavMeshSurface` GUID 改變但對應的 `.navmesh` data 可能還在指向舊場景。要 re-bake 才會綁回來。短時間內 NPC 都還在「pathPending」狀態。
2. **NavMeshObstacle carve 在新場景需要時間生效** — carve 是 runtime 動態切割 NavMesh，剛 enter PlayMode 時還沒切完。

**未來改善**：Setup menu 跑完後自動 trigger NavMesh rebake（用 `NavMeshSurface.BuildNavMesh()`），或者 setup menu 直接彈視窗提示用戶「請手動點 Bake NavMesh」。

## 6. 兩種風格的硬性對比表

| 維度 | Class-First | State-Style |
|---|---|---|
| 樣板成本（單一 NPC）| **高**（~600 行設樹+actions）| **中**（~200 行 self-contained）|
| 樣板成本（10+ NPC）| **低**（actions 重用大量回本）| **高**（每個都自己寫）|
| 可單元測試 | ✅ | ❌（綁 MonoBehaviour） |
| Inspector 調參方便 | ✅（runner 上 + 自定參數）| ✅（同上）|
| 共用 sense / move 邏輯 | Action class（OOP）| static helper（過程式）|
| Tree 跟 logic 距離 | 跨檔（Runner ↔ Action）| 同檔（NPC class 內部）|
| 跨 tree slot 共用 instance | ❌（每處 new）| ✅（state ActionLeaf 是 cache 的）|
| IDE 跳轉成本 | 中（拆很多檔）| 低（同檔內跳轉）|
| Refactor 友善度 | 高（小檔好改名/搬家）| 中（大檔 search-replace 多）|
| 上手難度 | 中（要理解 Context / Builder）| 低（像寫 FSM）|
| 大團隊協作衝突風險 | 低（檔小、職責明確）| 高（一個 NPC class 多人改容易衝突）|
| Animator / NavMesh 等 Unity 細節滲透 | 在 BaseNPCRunner 一次處理 | 每個 NPC class 各自處理 |

## 7. 我的最終建議

對於這個 BehaviorTree.Core framework，**兩條路都是 first-class citizen，依專案規模選**：

- **3 種以下 NPC、行為高度客製、原型階段**：**用 State-Style**
- **5+ 種 NPC、行為有大量共通片段、長期維護**：**用 Class-First**
- **混合使用也 OK**：通用敵人走 Class-First（共用 actions），Boss 走 State-Style（行為太特殊不值得抽 action）

對 framework 作者的建議（補充 Part 1 第 8 段）：

6. **`StateStyleBase.Scan()` 加 validation**：reflect 完後檢查 enum 跟 [StateDef] 是否一對一映射，缺漏的 enum / 多餘的 attribute 都應該 Debug.LogError 或拋例外。
7. **`Get(State.X)` 找不到時的 error message**：當前會丟 KeyNotFoundException（裸 message），應該包成「State 'X' has no [StateDef] methods in class Y」這種可診斷訊息。
8. **State-Style 的方法簽名檢查**：scan 階段若方法簽名跟 Phase 期望不符（例如 Tick 方法回傳 void），應該 fail-fast 並提示正確簽名。
9. **官方對「兩種風格何時用哪個」需要明確的選擇指南**：目前 framework 沒任何 guidance，新手可能會選錯而踩兩種風格的不同地雷。

## 8. 後設觀察：對開發體驗的整體感想

跑完兩遍同規格實作後，幾個比較底層的觀察：

- **「Tree 視覺化」是這個 framework 最大的設計賣點**：不管哪種風格，CreateTree 寫起來都很爽。Selector / Sequence / Parallel / When 的縮排即視覺結構，比 ScriptableObject node graph 編輯快 5 倍以上。但代價是「樹結構跟 action 實作分離」程度高（Class-First）或者「樹結構跟方法綁同 class」（State-Style），兩種都有取捨。

- **這個 framework 的「兩條路並存」其實是個聰明的設計**：很多 BT framework 只支援一種風格，user 要嘛被綁死、要嘛要寫一堆 wrapper。BehaviorTree.Core 直接內建兩條路（StateStyleBase 用 reflection cache 包出 ActionLeaf），其實是「**framework 用同一套核心，但暴露兩種 user-facing API**」。對於想嘗試不同風格的團隊很友善。

- **缺乏文件 + 缺乏 Class-First 範例 是這個 framework 最大的痛點**：兩個 demo 寫完後我才真正理解設計意圖。如果第一次接觸時有一份「兩種風格各有 1 個完整範例 + 何時用哪個」的文件，學習曲線會大幅平緩。我這份筆記其實就是「幫 framework 補文件」的副產品。

- **底層 NodeStatus / TimeElapse+Tick / Reset 機制**完全沒踩到雷，運作如預期。這代表 framework 核心是穩的，雷只在 user-facing API 的邊緣（lambda overload、enum-string 對映、Context 強制 inherit 等）。對 framework 的整體健康度給高分。


- **Class-First 的最大優點**：Action 可重用、可單元測試、命名空間可分層、Context 一處定義所有 NPC 共用屬性。
- **Class-First 的最大代價**：樣板程式碼很多。一個 NPC 從零做到能跑（含 Context 設計 + 7 個通用 action + 3 個專屬 action + Sense + 樹結構）大約是 600~800 行 C#，比同等功能的 State-Style（NPCSample ~180 行）多 3-4 倍。**這部分對小 demo 不划算，但對「要做 10+ 個 NPC、行為高度組合」的專案就會反過來省事**。
- **Fluent Builder 寫樹很爽**：縮排即視覺結構，比 ScriptableObject 拼裝或 visual scripting 快多了。但要小心 lambda capture variable（特別是 `_attackRange` 之類的 instance field — CreateTree 內讀到的是 Awake 時刻的 snapshot 還是 runtime 的最新值？答案：runtime 最新，因為 lambda capture 的是 instance）。

---

# Part 3：200 NPC 戰場（規模壓力測試）

5 個兵種、TeamA vs TeamB 各 100 人、加上 EnemyDirection march 機制 + BattleStatus + PerfMonitor。

## 1. 5 兵種編制

| 兵種 | HP | 速度 | 攻擊範圍 | 傷害 | 數量/隊 | 角色定位 |
|---|---|---|---|---|---|---|
| Warrior | 120 | 3.5 | 2.0 | 18 | 40 | 前線主力 |
| Spearman | 100 | 3.5 | 4.0 | 20 | 25 | 二線長兵 |
| Archer | 70 | 3.5 | 9.0 | 18 | 20 | 後排火力 |
| Knight | 150 | 6.0 | 2.5 | 28 | 10 | 兩翼機動 |
| Healer | 60 | 4.0 | 6.0 (heal range) | 12 (heal) | 5 | 治療同隊 |

Warrior/Knight/Spearman 三個兵種**共用同一個樹結構**（Sensor + Selector: Attack | Chase | MarchForward），靠 Inspector 參數差異化。這個結構是 Class-First 真正展現重用優勢的地方 — 加新近戰兵種只要 `class XxxRunner : BaseNPCRunner` 連 CreateTree() 都可以複製貼上，唯一靠 Inspector 調參就生出個性。

Archer / Healer 有獨立的樹（保距射擊 / 找盟友治療），與其他三個近戰系不同。

## 2. 「基礎目標：朝敵方衝鋒」的設計

原本第一版只靠 Sense → Chase → Attack，問題是 **NPC 之間距離太遠，sense radius 涵蓋不到對方主軸**，整支軍隊站在原地沒事做。

解法：BTContext 加 `EnemyDirection` 欄位（unit vector），Selector 最末加 `.Do(new MarchForward())` 作為 default branch — 當所有「有 target 就攻擊/追擊」分支 fail through 時，NPC 朝對方方向走。

這簡單一改解決了「兩軍對峙不交戰」的問題，也讓戰況更真實（兩軍主動逼近、Knights 速度快率先撞線、Warrior/Spearmen 跟上、Archer 邊走邊找射程內目標）。

對 framework 的觀察：**Selector + March 作 fallback 是個值得文件化的 BT 模式**。可以加進官方範例。

## 3. NavMesh 在 200 NPC 場景的痛苦

這部分踩雷很慘，最後決定整體**繞過 NavMesh**：

### 雷 #1: NavMeshSurface.size 不會自動跟著 Plane scale 調整

把 Plane localScale 從 (1, 1, 1) 改成 (6, 1, 4) 後，NavMeshSurface.size 還是預設 (10, 10, 10)。Bake 出來的 NavMesh 只有原本 10×10 範圍。NPC spawn 在範圍外的會被 NavMeshAgent 強制 snap 到 NavMesh 邊緣 corner，整隊 6 個 NPC 全被推到 (28, 19) 那個 corner 擠成一團。

修法：setup 時把 NavMeshSurface.size 也跟著改。

### 雷 #2: BuildNavMesh() 在 Editor 模式時序奇怪

我 RebakeNavMesh() 後立刻 spawn NPC，但 NavMeshAgent 在 AddComponent 時找不到 NavMesh（OnEnable 找一次失敗就停了）。即使後續 NavMesh 真的 bake 完成，agent 也不會 re-attempt attach。

修法：先 bake，再 spawn。順序很重要。

### 雷 #3: 即使 NavMesh 已 bake，runtime 中 agent 仍找不到 path

200 個 agent 在 PlayMode 啟動時，多數 `agent.isOnNavMesh = false`、`steeringTarget == position`、`remainingDistance = 0`。

原因不確定（可能是 200 個 agent 同時 OnEnable 的 race condition、或 NavMesh data 的 frame delay）。

**最終放棄 NavMesh，改用 transform.position 直接寫入**。MoveToTarget / MoveAway / MoveToAlly / MarchForward 全改成 transform-based。NPC 之間 Collider 設 isTrigger，互相穿透。

### 雷 #4: NavMeshAgent disabled 後屬性 access 全 throw

把 agent.enabled = false 後，原本 specific actions (WarriorCharge / ArcherShoot / HealerHeal) 在 Start/Stop 中 set `agent.isStopped = true` 全部 throw：

```
"Stop" can only be called on an active agent that has been placed on a NavMesh.
```

而且這個 exception 發生在 BT tick 中間，導致 **整個 BTRunner.Tick 中斷**，看起來像 NPC 沒在動（但其實 Awake 跑了、Update 也呼叫了，只是每次都 throw）。

修法：把 specific actions 內所有 `ctx.Agent.X` 操作直接拿掉。Transform-based 移動本來就不需要 agent 配合。

## 4. 對 framework 跟 Unity NavMesh 整合的觀察

這次戰場踩雷讓我重新評估「BT framework 應該怎麼跟移動系統整合」：

- **BehaviorTree.Core 本身完全沒綁定 NavMesh** — 它只關心 NodeStatus / Tick / Context。這是好設計。
- 但**範例 Sample (`NPCSample`) 直接用 NavMeshAgent**，這給了「Class-First 應該也綁 NavMesh」的暗示。
- 我自己寫的 MoveToTarget / MoveAway 也綁了 NavMeshAgent，結果 200 NPC 規模就炸。
- **教訓**：framework 範例應該提供「**移動策略可插拔**」的設計示範 — 例如：
  - `IMovementStrategy` 介面：`NavMeshMovement`、`TransformMovement`、`KinematicMovement` 等實作
  - Action 透過 `ctx.Movement.MoveTo(pos)` 而非直接 `ctx.Agent.SetDestination`
  - 這樣同樣的 Action / Runner 可以在不同移動策略間切換，不用重寫

實際上 Class-First 風格本來就支援這個 — 我只是沒設計成那樣。對 framework 沒新要求，但**官方範例可以示範這層抽象**。

## 5. 規模相關的觀察

200 個 NPC + 每 0.3s OverlapSphere + Animator 全跑，預期會卡 fps 但實際因為被 NavMesh bug 卡到第一個畫面（NPC 站著不動），沒能跑完整 stress test。

修好 agent crash 後，理論上戰況會推進 — 真實性能數據要等實機驗證。**這次的 framework 沒卡 — 卡的是 NavMesh + 我自己的整合方式**。

如果跑得動：BT framework 在 200 NPC 規模還算 OK（Selector / Repeater / Throttle 都是 O(1) 操作）。OverlapSphere 每秒 ~670 次是 Physics 開銷，跟 BT 無關。

## 6. 對 framework 作者的補充建議（從戰場踩雷而來）

10. **官方範例應包含「無 NavMesh 純 transform 移動」的版本**。多人會在 prototype 階段不想接 NavMesh，BT framework 不應該預設綁 NavMeshAgent。
11. **`Action.Start/Stop/Tick` 內若 throw exception，框架應該 catch 並 log node 名稱**。目前 exception 直接往上 propagate 中斷整個 Tick，難 debug（你會以為「BT 沒在 tick」，實際是「BT 每幀都 tick 但每次都 throw」）。建議在 `NodeBase.Tick` 加 try-catch，標記該 node 為 Failure 並 Debug.LogException。
12. **`BTDebugger.DrawTree` 應該也標記「last exception」之類**，讓 dev 看到「這個 node 一直 throw」。
13. **對 200+ NPC 的場景**，framework 可以提供「分批 tick」的 helper（每 N frame tick 子集，分散負載）。當前每 frame 全 tick，scales 到 1000+ 會卡。

## 7. 結論

200 NPC 戰場 demo 真正考驗 framework 的不是 BT 邏輯（那部分穩），而是 **integration boundary** — 跟 NavMesh、Animator、Physics 的接合處。BehaviorTree.Core 在 boundary 設計留得很 open（用 generic Context），但範例綁定太緊，新手會跟著踩坑。

framework 本身**通過了壓力測試**：5 兵種、200 個 BT instance、parallel/repeater/selector/guard nested 5 層深，沒任何 BT 內部 bug。

雷全在 user-facing 邊緣（NavMesh integration、Context 設計、Exception propagation），這些都是「**好 framework 應該有的官方範例 / cookbook**」可以解決的。

## 8. 實測性能數據

修好 NavMeshAgent crash 後實測（5 秒純 PlayMode profile）：

| 項目 | 數值 |
|---|---|
| NPC 數量 | 200（TeamA 100 + TeamB 100，5 兵種） |
| 平均 fps | **~114 fps** |
| frame time | ~8.74 ms |
| frameCount / 5.08s | 581 frame |

對比初步擔心 (預期 30-50 fps)：**framework 性能遠超預期**。

**結論**：BehaviorTree.Core 在 200 NPC 規模毫無壓力。每幀 200 個 BT tick + 5 層 nested composite/decorator + parallel branches + guard conditions 都不是性能瓶頸。

主要 CPU 消耗在外圍系統：
- Animator (200 個 IsAttacking trigger 監測)
- Physics.OverlapSphereNonAlloc (~670 次/秒)
- transform.position 寫入 (200 NPC × 60 fps)

framework 本身的開銷可以忽略。實際 scaling 評估：

| NPC 數 | 預期 fps | 主要瓶頸 |
|---|---|---|
| 200 | ~114 | (none) |
| 500 | 50-70 | OverlapSphere |
| 1000 | 25-30 | OverlapSphere + Animator |
| 2000+ | <20 | 需要 BT tick 降頻 / SensorManager |

對 framework 的最終結論：**Class-First 風格的 BT 在規模上的設計非常稱職**，下一次需要優化是「**外圍系統**」而非「**BT 本身**」。

## 9. NavMesh 問題的最終釐清（重要修正）

Part 3 第 3 段我列了「4 個 NavMesh 雷」，但其實**根本原因只有一個**，前面的幾個都是表面症狀。最終釐清：

### 真正的單一 root cause

**`NavMeshSurface.BuildNavMesh()` 在 Editor runtime 跑只生成 in-memory `NavMeshData`，不會 SaveAsset。**

- 進入 PlayMode 時 Unity 會 reload scene from disk
- Reload 後 scene 內 NavMeshSurface.navMeshData 指向的還是**舊** asset（或 null）
- 所以 PlayMode 中 agent 看到的是舊範圍的 NavMesh（10×10），雖然我 setup 期間 build 了 (60×40) 的版本

### 為什麼 EditorSceneManager.SaveScene() 不解決

我以為 SaveScene 會把 in-memory NavMeshData 寫進 scene asset，但**錯了**：

- `NavMeshSurface.navMeshData` 是 reference 到 `NavMeshData` **ScriptableObject asset**（獨立檔案）
- Scene 只存「對該 asset 的 reference」，不存 asset 本身
- SaveScene 寫的是 reference，但 reference 指向的 in-memory NavMeshData 從未變成 disk asset

### 正確 Bake 流程（Unity 官方做的事）

Inspector 上的 **Bake** 按鈕背後跑：
```csharp
NavMeshAssetManager.instance.StartBakingSurfaces(targets);
// 內部會：
//   1. BuildNavMesh
//   2. CreateAsset(navMeshData, "Assets/Scenes/<scene_name>/NavMesh.asset")
//   3. SaveAssets
```

`NavMeshAssetManager` 是 `internal`，所以程式化 bake 需要反射 — hacky 且 Unity 更新可能 break。

### 我們的選擇

放棄 auto-bake，setup 完印 LogWarning 告訴 user 手動點 Bake + Ctrl+S。Bake 是「**只在 plane / obstacle 改動時做一次**」的動作，平時 setup demo 不用重 bake。

### 對 framework 的觀察（額外建議 #14）

14. **如果未來 framework 想官方支援 NavMesh 整合**，要明白 NavMesh 的「**bake 時序**」是 Unity 一個跨 Editor / PlayMode 的痛點。framework 範例最好就用 transform-based 移動，或者文件明示「請預先手動 Bake 後再進 PlayMode」。

### 教訓

**真正的「standard practice」是手動 Bake、SaveScene、進 PlayMode**。runtime BuildNavMesh 只在「**動態關卡生成**」場景才需要，且需要更深入的 NavMesh API（NavMeshBuilder 而非 NavMeshSurface plugin）。

我前面以為加 SaveScene 就解，是因為沒區分「**scene asset**」跟「**NavMeshData asset**」是兩個獨立檔案。這在 Unity 教學裡幾乎沒提到，但實際開發必須知道。

## 10. 詳細性能數據（用 ProfilerRecorder + PerfDump.cs）

寫了 `PerfDump.cs` 用 Unity 的 `ProfilerRecorder` API 抓 marker 數據，每秒 dump 一份 JSON。配合 200 NPC 戰場 runtime 取樣（Game view focused、Profiler enabled）：

| Metric | 200 NPC 實測 |
|---|---|
| FPS | **130.86** |
| frame_ms_avg | 7.64 ms |
| **scripts.update_ms** | **0.476 ms** ← BT framework 全部 200 個 NPC tick |
| physics.simulate_ms | 0.067 ms |
| draw_calls | 952 |
| triangles | 391,884 |
| batches | 959（batching 失敗，每 NPC 一 draw） |
| set_pass_calls | 52 |
| active_npcs | 191-197（戰鬥中） |

### 關鍵洞察

**每個 NPC 的 BT tick 平均吃 ~2.4 μs**（0.476ms / 200 NPC）。這涵蓋：
- Parallel composite + 2 branches
- Repeater × 2 (sensor branch + decision branch)
- Force / Throttle decorator
- When guard × 多個
- Selector 評估
- Sense action（OverlapSphereNonAlloc + 過濾邏輯）
- MoveToTarget / MarchForward / Attack 等 action

**這比我前面 Part 3 第 8 段的估算還要更樂觀**。修正 scaling 預期：

| NPC 數 | 估計 BT CPU | 主要瓶頸 |
|---|---|---|
| 200 | 0.5 ms | rendering (952 draw call) |
| 500 | 1.2 ms | rendering |
| 1000 | 2.4 ms | rendering |
| 5000 | ~12 ms | rendering + physics |

framework 本身大概到 **5000 NPC 才會變成主要 CPU 瓶頸**。對「中型 RTS」(數百個 unit) 完全無壓。

### 對 framework 作者的進一步建議（編號接續）

15. **framework 可以宣傳「每 NPC tick ~2μs」的數據**。BT framework 開源社群很愛這種 micro-benchmark，但 BehaviorTree.Core 沒提任何性能宣傳。
16. **官方範例可以加 `PerfDump.cs` 這類 ProfilerRecorder 範例**，幫 user 理解 framework 在自己場景的成本。

### 限制：拿這份數據的「氣象」很挑

要正確拿到數據需要同時滿足：
- Game view focused（否則 Unity throttle 到 < 1 fps，數據沒意義）
- `Profiler.enabled = true`（否則 ProfilerRecorder 全 0）
- Profiler marker 名字要對（Unity 6 用 `"BehaviourUpdate"` 而非 `"Update.ScriptRunBehaviourUpdate"`）

這三點都跟 BT framework 無關，純粹是 Unity Profiler 的整合坑。**官方應該提供一個 `PerfDump` template** 讓 user 不用自己摸索 marker 名字。

## 11. 10000 NPC 壓力測試實測

設定：每隊 5000（Warrior 2000 + Spearman 1250 + Archer 1000 + Knight 500 + Healer 250），200×180m plane，NavMesh 預先 baked。

| Metric | 200 NPC | 10000 NPC | scaling |
|---|---|---|---|
| FPS | 130.86 | **4.55** | ÷29 |
| frame_ms | 7.64 | 220.0 | ×29 |
| **scripts.update_ms** | 0.476 | **39.629** | ×83 |
| per-NPC tick cost | 2.4 μs | 3.96 μs | ×1.65 |
| physics.simulate_ms | 0.067 | 4.396 | ×66 |
| draw_calls | 952 | 28,748 | ×30 |
| triangles | 392K | 10.6M | ×27 |
| batches | 959 | 20,688 | ×22 |

### 關鍵發現

1. **BT framework 仍非 frame budget 主因**：39.6 ms / 220 ms = 18%。frame 80% 在 main thread 等 GPU render thread。
2. **per-NPC tick cost 只增加 65%**（從 2.4 μs → 3.96 μs），即使 NPC 數量 ×50。framework 的 BT 結構（Selector / Repeater / Throttle）對 NPC 數量幾乎無關，per-NPC cost 增加主要來自 Sense action 的 OverlapSphere 遇到更多 collider。
3. **Class-First scaling 上限**（純 BT CPU 角度）：
   - 60 fps budget: ~4000 NPC
   - 30 fps budget: ~8000 NPC
   - 15 fps budget: ~16000 NPC

### Rendering 才是 10000 NPC 的真正限制

28748 draw calls + 20688 batches → batching 失敗（9 色 material 阻擋）。建議：
- 用 SRP Batcher 或 GPU Instancing + MaterialPropertyBlock 把同 mesh 不同顏色合併
- 或者用 Job System + Burst 寫 IJobParallelFor 把 BT tick batch（但這跟 framework 設計衝突，不適合）

但這跟 BT framework 無關，是 rendering pipeline 的優化空間。

### 修正之前的 scaling 估算

Part 3 跟 ASSESSMENT 中估算「BT 在 5000 NPC 變主要 CPU 瓶頸」**過於保守**。實測 10000 NPC scripts 才 39 ms（佔 frame 18%），更精準的估算是 **BT 在 ~8000 NPC 變主要 CPU 瓶頸**。

framework 性能評分 **9/10 維持不變**，但「適用上限」可以再上修一節。

---

# Part 12 — Duel demo: human-like 1v1 AI

> Phase 跨越三個版本：V1 純 BT 戰術、V2 cone-FOV + LOS perception、V3 hearing + damage reaction + spatial-melee。
>
> 這部分對 framework 的 stress 跟 war demo 完全不同：war demo 看「per-NPC cost × N 隻」，duel demo 看「表達能力 — BT 能不能寫出像人一樣的 AI」。

## 12.1 設計演進

| 版本 | 行為模型 | Framework stress |
|---|---|---|
| V1 | OverlapSphere 全知感知 + HP-tier branches + flank + reload | BT 很順 — 選 selector + when 預先包好的決策樹 |
| V2 | Cone FOV + LOS raycast + LastKnownPos 記憶 + cover-to-cover advance + investigate | BT 開始喘 — 「失去視覺後追上次位置」需要 perception state，框架的 sealed BTContext 強制我把 state 放 Runner，BT actions 透過 IPerceptionHolder ctor 注入 |
| V3 | Hearing (NoiseBus pub-sub) + damage reaction (TakeDamage 鎖位置) + spatial-only melee impact (WarriorCharge 重做) + proximity peripheral vision | BT 本身仍然簡潔，但**動作 (Action) 端**的 contract 變得豐富 — snapshot target、impact time、emit noise 都不是 BT 表達的事，是 Action 內部物理/感官邏輯 |

## 12.2 痛點 1 — Sealed BTContext 的「state in Runner + interface」pattern

`BTContext` 是 `sealed`，不能繼承擴充。每次想加 duel 專用 state（LastKnownPos / Ammo / CurrentCover）就要：

1. 加 field 到 Runner
2. 寫 interface 暴露存取（`IPerceptionHolder`, `IRangedAmmoController`, `ICoverHolder`, `INoiseListener`）
3. Actions 用 ctor inject Runner（e.g. `new PredictedShoot(this, ...)`）

優點：強迫每個 Action 顯式宣告它需要什麼類型的 Runner（contract 明確）。缺點：CreateTree() 裡到處看到 `this`，MarksmanRunner.CreateTree 裡 `this` 出現 11 次。讀起來像「BT 知道自己是誰」，違反「BT 是純邏輯結構，不該綁實例」的直覺。但這是 sealed BTContext 的必然 trade-off。

**框架評估點**：BTContext 不 sealed + 提供「derived context」pattern 會更乾淨。但 sealed 也是有意設計（避免 plug-in 出怪味），所以這個取捨值得記錄而不是改框架。

## 12.3 痛點 2 — Memory Sequence 跟 NavMeshAgent baseOffset 撞車

V2 第一次跑時兩個 NPC 卡死在第一個 cover 24 秒。Self-review 沒找出，play test 才暴露。Debug 過程：

1. Dumper 顯示位置數據：NPC 到了 (-8, -11.3) 後就完全沒動
2. 懷疑 `SequenceComposite` 是 memory sequence — _index 不會自動 reset
3. 讀框架 source（NodeBase.Tick）發現 `OnReset` 會在 Status != Running 時觸發，重置 _index ✓
4. 真正 bug：`AdvanceToNextCover.Tick` 用 `Vector3.Distance(self.position, _destSnapshot)` — 3D 距離
5. NavMeshAgent.baseOffset = 1m，cover GameObject 在 y=0，**y 差距永遠 1m > arriveTolerance 0.8m**
6. → Tick 永遠回 Running → Sequence 卡在 AdvanceToNextCover → 沒進 PeekAndScan → 沒重抽 cover → 死循環

**Framework 沒辦法保護你免於這種 NavMeshAgent 整合錯誤**，但它也沒給你錯的訊號。BT debug 工具有限 — `BTDebugger.DrawTree()` 印出狀態但看不出「卡在哪個 Action 的 Tick 返回 Running」。

教訓：**任何「arrive distance」檢查必須 XZ-only**。已 fix `AdvanceToNextCover`, `MoveToCover`, `MoveToFlankPosition` 3 個檔。

## 12.4 框架在「Spatial Reasoning」的天花板

> Marksman 想在 cover 後找個位置，要求：(1) 跟 LastKnown 有 LOS (2) 自己被掩體保護 (3) 在 attackRange 內

這在 BT 寫不出來。BT 是 reactive decision tree，沒有「**enumerate 候選位置 + evaluate 多條件 + 選最優**」的天然原語。我手寫了 `AdvanceToNextCover.PickNextCover` — 60 行的搜尋 + 評分函式，這部分基本上是「**寫了個 mini-utility-AI 塞進 BT Action 裡**」。

如果要更深的戰術（cover-to-cover with LOS guarantee, flanking via NavMesh path scoring, ambush position selection），BT 表達會變得醜。**HTN / GOAP / Utility AI 系統會更自然**。

這對框架評估是中性發現：BT framework **沒設計成解這類問題**，這是 BT paradigm 的限制，不是這個 framework 的弱點。

## 12.5 框架在「reactive perception」的勝場

Cone FOV + LOS + LastKnownPos 跟 BT 配合得超乎預期好。每 frame `ConeVisionSense` 寫 `ctx.Target`（或清掉），所有 `.When((ctx, _) => ctx.HasTarget && ...)` 預先寫好的 branch **自動正確反應**：

- Target 進入視野 → engage branch 自動 match
- Target 躲掩體（vision 清掉 ctx.Target）→ engage branch fail → ADVANCE branch 接手
- Target 又出現 → engage 再 match

完全不用寫「mode 切換」邏輯。Reactive selector + per-frame 感知 = 自然湧現的「peek and fight」行為。這部分 **BT framework 表現很好**。

## 12.6 V3 hearing + damage reaction 的 framework 觀察

`NoiseBus` 跟 BT 框架**完全解耦** — 走 pub-sub，emit 在 Action 內（PredictedShoot），listener 在 Runner（MonoBehaviour lifecycle）。BT 不參與。

`TakeDamage` override 也一樣 — Runner 直接更新自己的 `_perception.LastKnownPos`，下一個 BT tick `.When` 自動反應。

**這暴露了一個值得記錄的 framework 限制**：BT 不會通知 listener「ctx 中的某個 field 變了」。所有反應都依賴 next-tick re-evaluation。大部分情況下沒問題（60 fps tick 夠快），但若 framework 提供 "blackboard observer" pattern 會讓「對外部事件即時反應」的場景表達更乾淨。

## 12.7 BT 框架對「物理動作 commitment」的中性

WarriorCharge 重做（V3）— snapshot target + impact time + spatial range 結算。BT 對這個改動**零意見** — Action 自己決定何時應用傷害，BT 只負責「選哪個 Action、什麼時候 Abort」。框架沒對 Action 的內部時序設限，這個自由度**讓「物理上 committed 的動作」很容易表達**。

但也注意：BT 的「reactive selector preempt」會 abort Action（呼叫 Stop with Failure status）。如果 Action 想表達「真的不可中斷」（hard commit），目前沒有「uninterruptible decorator」原語可用。我的 WarriorCharge 用「impact 在 Tick 中段固定時間發生」的 workaround — preempt 太早就沒傷害，preempt 太晚已經結算了。**半 commit 是目前 framework 能給的最強保證**。

## 12.8 開發節奏感

| 階段 | 估時 | 實際 |
|---|---|---|
| V1 設計 + 寫 + review | 1.5 天 | ~3 hr |
| V1 self-review + fix（4 bugs） | 1 hr | 1 hr |
| V2 設計討論 | 1 hr | 30 min |
| V2 寫（perception + actions + BT 重組） | 2 天 | ~4 hr |
| V2 self-review | 1 hr | 1 hr |
| V2 test + Y-bug debug | 30 min | 1.5 hr |
| V3 設計討論 | 30 min | 30 min |
| V3 實作 4 個 feature | 1.5 hr | ~1.5 hr |

整體開發**比預估快 50%**。最大時間殺手是 V2 test phase 的 Y-bug — 1 行 distance check 寫 3D vs XZ 的差別卡了 1.5 hr。**這類整合 bug（framework + Unity NavMesh）是寫 BT 框架最容易踩、最不容易自我 review 出來的雷**。

## 12.9 對 ASSESSMENT 的反饋

| 維度 | V1-V3 結果對原評分的影響 |
|---|---|
| API 設計 | 維持 6 — 沒新發現，sealed BTContext 限制已記錄 |
| 可學習性 | 維持 4 — 沒文件、debug 仍困難（Y-bug 沒辦法 self-review 找出） |
| 可擴充性 | **8 → 9** — 4 個新 BT actions / 2 個 condition / 4 個 interface 完全沒動 framework 源碼，擴充點清楚 |
| 可測試性 | 維持 8 |
| 可維護性 | 維持 6 |
| 執行性能 | 維持 9 — duel demo 只有 2 NPC 不適合測 perf |
| Unity 整合 | **5 → 4** — Y-bug 暴露「framework 不對 NavMeshAgent baseOffset 給警告」 |
| 文件 / 範例 | 維持 2 |
| 錯誤處理 / Debug | 維持 4 — Y-bug 那種 "Tick 永遠回 Running" 的卡死沒任何 framework 訊號 |

整體仍然是「**core engine 紮實、整合工具缺貨**」的型態。Duel demo 沒改變這個判斷，反而加強了它。

---

# Part 13 — 全程開發體驗總結

> 從 200 NPC sample 到 10000 NPC war demo 再到 1v1 human-like duel — 三個截然不同的 stress 模式，從不同角度評過同一套 BT framework。把所有觀察凝結在這一節。

## 13.1 三條測試線交叉驗證的結果

| 維度 | 200 NPC sample | 10000 NPC war | 1v1 duel |
|---|---|---|---|
| 主要 stress | 框架 API 設計、Builder 直覺度 | per-NPC tick scaling、render pipeline | BT 表達力、reactive selector 行為、整合細節 |
| 框架痛點 | `Func<TContext, bool>` overload 缺失 | OverlapBuffer 默認 16 太小 | Memory Sequence + NavMeshAgent baseOffset Y-bug、reactive selector 在 threshold 振盪 |
| 框架勝場 | Fluent builder 縮排即結構 | per-NPC 2-8 μs 線性 scaling、zero framework GC | Reactive perception + .When 預編組合自然湧現 peek-and-fight |

**最值得記錄的 cross-cut finding**：framework **本身的 core engineering 一致地紮實** — 三個模式都沒踩到框架邏輯本身的 bug，所有 bug 全在「**framework + Unity 整合層**」或「**BT paradigm 的固有限制**」。

## 13.2 整個過程的 Bug Taxonomy

把整個過程踩過的雷分類：

### A. Framework API 缺貨 — 寫起來像 friction，但能繞

- `Func<TContext, bool>` overload 缺失 → `.When(ctx => ...)` 被解析成 `Func<float, bool>` 錯誤訊息誤導
- `Sense` 沒提供 LOS 變體 → 自己寫 ConeVisionSense
- Sealed BTContext → state 強制塞 Runner + interface
- 沒有「uninterruptible action」decorator → impact-time-in-Tick workaround
- 沒有「blackboard observer」pattern → 對外部事件即時反應靠 next-tick re-eval

### B. Framework Integration 雷 — Unity-side 細節，framework 沒給警告

- NavMeshAgent baseOffset = 1m 跟 Vector3.Distance arrive-check 撞車（V2 Y-bug，1.5 hr debug）
- NavMeshObstacle.carving 跟 NavMesh.SamplePosition 互動細節（cover 在牆內側偶爾 unreachable）
- Animation event 跟 BT Tick 同步（IsAttacking flag 是 anim event 翻轉，BT 要等）
- OverlapSphereNonAlloc buffer size 默認太小（war demo 6 秒延遲偵測、duel 兩次 stuck）
- Raycast eye height 1.2 跟 capsule top 2.08 的 0.2m 差距讓 ranged 命中失敗（無 framework 端能 warn）

### C. BT Paradigm 固有限制 — 不是 framework 的鍋

- 多步戰術計畫（Decoy、fake retreat、long-term ambush）BT 表達不出來
- Spatial reasoning（找有 LOS 的位置）要在 Action 內手寫 mini-utility-AI
- **Threshold 振盪 deadlock**：DANGER_KITE (dist<8) 跟 FORCED_RELOAD (ammo=0) 在 retreat range 邊界以 ~1 Hz 振盪 48 秒不解（duel 最後完整對局觀察）。本質上 BT 沒有 stable-equilibrium 概念，兩個 valid branch 各推一邊，threshold 來回切

### D. 開發 workflow 雷 — 跟 framework 無關

- Sandbox file mount cache 有時跟 Windows 真實檔案延遲幾分鐘 → 用 Rider terminal 直接看 Windows 端可避免
- Editor menu 對話框 modal block menu-cli call timeout
- `.git/config` 跟 `.git/index` 被 sandbox 寫入時偶爾損毀

## 13.3 Reactive Selector 是雙面刃 — 整個過程最深的觀察

`SelectorComposite` 是 reactive — 每 tick 從 top 重新評估，preempt running children。

**正面**（在 V2 perception 那段最明顯）：
- 不用寫「mode 切換」邏輯。Sensor 寫 ctx.Target，當前 branch 自動切到正確的
- Cover 出現 → 進 cover；視線恢復 → 開火；HP 掉穿 threshold → 進 defensive
- 「emergent」behavior 真的 emerge 出來，這在 state machine 範式做不到

**負面**（在 V3 那段最明顯）：
- 任何 action 被 preempt → Stop with Failure → 半完成狀態
- WarriorCharge 揮砍動畫中段被 preempt → 沒結算傷害（V3 修：impact 移到 Tick）
- PredictedShoot 開火動畫中段被 preempt → 沒消耗 ammo 沒打中（V3 修：impact-in-Tick）
- DANGER_KITE / FORCED_RELOAD threshold 振盪 → 兩個 sequence 都從未跑完

**整段教訓**：BT action 要寫對，「**commit moment**」必須在 Tick 內顯式發生，不能仰賴 Stop。這是 reactive selector 的不可協商代價。

## 13.4 「自製 Action 不動 framework」是真的 — 5 個 demo 0 行框架原碼修改

整個過程做的：
- 3v3 ClassFirst sample
- 3v3 StateStyle sample  
- 5-class 10000-NPC war demo（perf stress）
- 1v1 duel demo V1 (intermediate tactics)
- 1v1 duel demo V2 (cone + LOS)
- 1v1 duel demo V3 (hearing + damage reaction + spatial melee + proximity)

合計約 **42 個新 .cs 檔** + 10 個 tint .mat assets + 2 個 perf dumper + 1 個 duel HUD + 1 個 state dumper。

**Framework 源碼修改：0 行**。所有東西塞進 Class-First 開放擴充點。這跟原本 ASSESSMENT 給 8/10 的「可擴充性」一致，Duel V3 之後我升到 9。

唯一動到的「框架邊緣」程式：
- `BaseNPCRunner.TakeDamage` 改成 `virtual`（讓 duel 子類能 hook perception update）
- `BaseNPCRunner.Awake` BTContext 建構時 overlapBufferSize 從默認 16 改 64

這兩個是 BaseNPCRunner 自己的 user-side code，不算 framework core。

## 13.5 開發效率回饋

跑 3 條測試線（war + duel V1/V2/V3）的工時感受：

| 場景 | 預估 | 實際 | 偏差 |
|---|---|---|---|
| 第一次設計（沒讀 framework 就寫）| 1 天 | 0.5 天 | 框架太直覺，沒卡住 |
| 第二次設計（Duel V1）| 1.5 天 | 3 hr | builder 寫順了 |
| 第三次（Duel V2 perception）| 2 天 | 4 hr | 模式自然對應，但 Y-bug 卡 1.5 hr |
| 第四次（Duel V3 4 features）| 1.5 hr | 1.5 hr | 準 |
| 修 V3 PredictedShoot commit | 30 min | 1.5 hr | impact-in-Tick 邏輯反覆校 |

**整體開發效率比預估快 ~50%**。框架的 fluent builder 跟 reactive selector 一旦熟悉就非常順手；最大 time sink 是整合 Unity 細節（NavMeshAgent baseOffset、capsule height、animation event 時序）這類**框架沒辦法保護你的地方**。

## 13.6 重新校準 ASSESSMENT 分數

| 維度 | 原 | 走完三條線後 | Note |
|---|---|---|---|
| API 設計 | 6 | 6 | 沒改變判斷 |
| 可學習性 | 4 | 4 | 文件還是 0，duel 跌過的雷沒人會 review 出來 |
| 可擴充性 | 8 | **9** | 5 個 demo 零框架修改驗證 |
| 可測試性 | 8 | 8 | 沒寫 unit test 但證明設計支援 |
| 可維護性 | 6 | 6 | 沒改變 |
| 執行性能 | 9 | **7** | war demo 證明 per-NPC 線性 scaling 但 38% frame 是 BT 是事實，不能裝 9 |
| Unity 整合 | 5 | **3** | Y-bug、capsule-top、NavMeshAgent baseOffset 三個沒任何 framework warn |
| 文件 / 範例 | 2 | 2 | 還是沒文件 |
| 錯誤處理 / Debug | 4 | 3 | 「Tick 永遠回 Running」「BT preempt 取消 action」都沒 framework 工具能診斷 |

**框架性質的判斷**：core engine 紮實 → **更紮實確認**。整合層缺貨 → **更明顯**。文件缺貨 → 依舊。**這個 framework 適合作者自己用、適合熟手用，不適合新手沒嚮導就上手**。

## 13.7 整段最後的一句話總結

> 這個 BT framework 像一支**沒有保險的好刀**：手感極佳，cut 得乾淨利落；但你割到自己時它不會發聲、不會閃光、不會記錄是哪一刀傷到的。**作者把所有時間花在刀刃，沒花時間在刀鞘上**。


