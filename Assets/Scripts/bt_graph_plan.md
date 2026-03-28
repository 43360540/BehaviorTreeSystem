# BT Graph 計劃整理

## 目前範圍

先專注兩條線：

- `Native Runtime Mode`
- `Graph Editor`

暫時移出計劃：

- `Authoring DSL`

---

## 核心原則

- `graph-side` 不追求和 `native code-side` 一樣自由。
- `native runtime` 保留給程式人員使用，不強求可 graph 化。
- `graph-side` 以非程式人員可用、可驗證、可控為優先。
- 先做出可用版本，再考慮更高階整合。

---

## Graph Editor 主要設計

### 1. Tree Blackboard / Parameters

採用類似 `Animator Parameters` 的做法，在左側提供統一參數區。

第一版先只支援：

- `bool`
- `int`
- `float`

特點：

- 只做單一 `tree-level scope`
- 不做 public/private 分層
- 不提供使用者可編輯的 node 私有狀態

補充：

- runtime 內部狀態仍然存在
- 只是**不暴露**給 graph 使用者操作

---

### 2. Built-in Nodes

先保留一般 BT 內建節點，例如：

- `Selector`
- `Sequence`
- `Guard`
- 其他必要基礎節點

---

### 3. Custom Nodes

Graph-side 提供兩種自訂節點：

- `CustomConditionNode`
- `CustomActionNode`

不建議做單一萬能 `CustomNode` 再切型別。

原因：

- 驗證比較簡單
- port 結構比較清楚
- editor 行為比較穩定

---

### 4. MethodBinder

`MethodBinder` 用來綁定外部 `Component` 的 `public method`。

但不是無限制任意綁定，而是：

- 只顯示對當前 node / slot 合法的方法
- 由 BT node 類型決定可接受的方法簽名

`MethodBinder` 只是綁定描述，不是 BT 語意主體。

---

### 5. VariableRef / BlackboardKey

除了 `MethodBinder` 以外，再提供資料引用機制，例如：

- `VariableRef`
- `BlackboardKey`

用途：

- 讓 node 讀取或使用 graph blackboard 中的變數
- 與 `MethodBinder` 分開，不混成同一種欄位

---

## CustomConditionNode 規則

主要只提供：

- `Tick`

可接受的方法方向：

- `bool Method()`
- `bool Method(TContext context)`
- `bool Method(TContext context, float dt)`

重點：

- 回傳值必須是 `bool`
- 只接受符合條件語意的方法

---

## CustomActionNode 規則

第一版先以 `Tick` 為主，之後再考慮：

- `Start`
- `Stop`
- `Abort`

### `Tick` 主規格

優先接受：

- `NodeStatus Method()`
- `NodeStatus Method(TContext context)`
- `NodeStatus Method(TContext context, float dt)`

### 相容模式

可選擇支援：

- `bool Method()`
- `bool Method(TContext context)`
- `bool Method(TContext context, float dt)`

並明確映射為：

- `true -> NodeStatus.Success`
- `false -> NodeStatus.Failure`

但這只作為相容方案，不作為 action 的主規格。

---

## 明確不做或暫不做

目前不做：

- `Authoring DSL`
- graph 與 native code 完全雙向互通
- 任意 `public method` 無限制綁定
- 使用者可編輯的 node 私有狀態
- 多層 variable scope
- 過多資料型別

---

## 近期實作順序

### 第一階段

先做最小可用版本：

- `Graph Blackboard / Parameters`
- `CustomConditionNode`
- `CustomActionNode`
- `MethodBinder`
- 合法方法篩選

### 第二階段

補上：

- `Start / Stop / Abort`
- 更完整的 slot 驗證規則

### 第三階段

整理 graph asset 結構與儲存方式。

目前優先傾向：

- `ScriptableObject`

---

## 一句話總結

目前計劃是：保留 `Native Runtime Mode` 給程式人員自由使用；`Graph Editor` 則走受控設計，採用單一 tree-level blackboard、`CustomConditionNode / CustomActionNode`、`MethodBinder` 與 `VariableRef / BlackboardKey`，優先追求清楚、可驗證、對非程式人員友善的工作流。
