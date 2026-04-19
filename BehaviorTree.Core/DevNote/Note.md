# Behavior Tree

## TODO

### Code-based Workflow

- NodeBase
  - [x] Add a method to retrieve child references
  - [x] Add construtors in subclasses
- [ ] Add node memory
- [x] runner / instance abstraction
- [x] introspection
- [ ] OnStop() / OnAbort() 若丟例外，node 會停在半退出狀態
- [x] guard 仍然有雙重 evaluate
- [x] DecoratorBuilderBase 仍然沒有 Condition(...)
- [x] Comment
- [x] Parallel composite
- [x] RootBuilder

BT -> composite b -> composite b -> INode / decorator b / composite b

None -> Running - Display: Running
     -> Success / Failure - Display: None

### Visual Workflow

- [ ] basic node data
- [ ] source generator
