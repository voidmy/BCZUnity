# OpenSpec 使用说明

面向使用 OpenSpec 进行规范驱动开发的 AI 编码助手的工作指引。

## TL;DR 快速检查清单

- 搜索已有工作：`openspec spec list --long`、`openspec list`（全文检索只用 `rg`）
- 确定范围：是新增能力，还是修改已有能力
- 选择唯一的 `change-id`：使用 kebab-case，动词开头（如 `add-`、`update-`、`remove-`、`refactor-`）
- 搭建脚手架：`proposal.md`、`tasks.md`、`design.md`（如有需要），以及对应能力的增量 spec
- 编写增量 spec：使用 `## ADDED|MODIFIED|REMOVED|RENAMED Requirements`；每条 Requirement 至少包含一个 `#### Scenario:`
- 校验：执行 `openspec validate [change-id] --strict` 并修复所有问题
- 请求审批：在 Proposal 被批准之前，不要开始实现

## 三阶段工作流

### 阶段 1：创建变更
在以下场景需要创建 Proposal：
- 增加新特性或功能
- 做破坏性变更（API、Schema 等）
- 修改架构或模式  
- 性能优化（会改变行为）
- 更新安全模式

触发语句（示例）：
- “Help me create a change proposal”
- “Help me plan a change”
- “Help me create a proposal”
- “I want to create a spec proposal”
- “I want to create a spec”

模糊匹配指引：
- 句子中包含：`proposal`、`change`、`spec` 之一
- 同时包含：`create`、`plan`、`make`、`start`、`help` 之一

以下情况可以跳过 Proposal：
- Bug 修复（恢复预期行为）
- 拼写错误、格式修改、注释
- 非破坏性的依赖更新
- 配置变更
- 为已有行为补充测试

**工作流：**
1. 阅读 `openspec/project.md`、`openspec list` 和 `openspec list --specs`，了解当前上下文。
2. 选择一个唯一且以动词开头的 `change-id`，在 `openspec/changes/<id>/` 下创建 `proposal.md`、`tasks.md`、可选的 `design.md`，以及 spec 增量文件。
3. 使用 `## ADDED|MODIFIED|REMOVED Requirements` 撰写 spec 增量，每条 Requirement 至少包含一个 `#### Scenario:`。
4. 运行 `openspec validate <id> --strict`，在分享 Proposal 前解决所有问题。

### 阶段 2：实施变更
把下面步骤当作 TODO，逐条完成：
1. **阅读 `proposal.md`** —— 理解要做什么、为什么做
2. **阅读 `design.md`**（如存在）—— 理解技术决策
3. **阅读 `tasks.md`** —— 获取实现清单
4. **按顺序实现任务** —— 按顺序完成每一条
5. **确认完成度** —— 在更新状态前，确保 `tasks.md` 里的每一项都已完成
6. **更新清单** —— 全部完成后，将所有任务勾选为 `- [x]`
7. **审批门槛** —— Proposal 审核通过前，不要开始实现

### 阶段 3：归档变更
部署完成后，单独创建一个 PR 来：
- 将 `changes/[name]/` 移动到 `changes/archive/YYYY-MM-DD-[name]/`
- 若能力有变化，更新 `specs/`
- 对仅工具层的变更，可使用 `openspec archive <change-id> --skip-specs --yes`（始终显式传入 change-id）
- 运行 `openspec validate --strict`，确认归档后的变更仍通过校验

## 在开始任何任务之前

**上下文检查清单：**
- [ ] 阅读相关能力的 `specs/[capability]/spec.md`
- [ ] 检查 `changes/` 下是否有冲突的未完成变更
- [ ] 阅读 `openspec/project.md`，了解项目约定
- [ ] 执行 `openspec list`，查看当前进行中的变更
- [ ] 执行 `openspec list --specs`，查看现有能力

**在创建 Specs 之前：**
- 总是先检查该能力是否已存在
- 优先修改已有 Spec，而不是创建重复能力
- 使用 `openspec show [spec]` 查看当前状态
- 若需求表述模糊，先提出 1–2 个澄清问题再搭建脚手架

### 搜索指引
- 列出所有 Specs：`openspec spec list --long`（或 `--json` 用于脚本）
- 列出所有 Changes：`openspec list`（或 `openspec change list --json`，已废弃但仍可用）
- 查看详情：
  - Spec：`openspec show <spec-id> --type spec`（可加 `--json` 做过滤）
  - Change：`openspec show <change-id> --json --deltas-only`
- 全文搜索（使用 ripgrep）：`rg -n "Requirement:|Scenario:" openspec/specs`

## 快速上手

### CLI 命令

```bash
# 核心命令
openspec list                  # 列出进行中的变更
openspec list --specs          # 列出所有规范（spec）
openspec show [item]           # 查看变更或规范详情
openspec validate [item]       # 校验变更或规范
openspec archive <change-id> [--yes|-y]   # 部署后归档（CI/自动化时加 --yes）

# 项目管理
openspec init [path]           # 初始化 OpenSpec
openspec update [path]         # 更新说明文件

# 交互模式
openspec show                  # 交互式选择条目
openspec validate              # 批量校验模式

# 调试
openspec show [change] --json --deltas-only
openspec validate [change] --strict
```

### 常用参数

- `--json`：机器可读输出
- `--type change|spec`：区分是 Change 还是 Spec
- `--strict`：严格模式，进行完整校验
- `--no-interactive`：关闭交互提示
- `--skip-specs`：归档时不更新 Specs
- `--yes` / `-y`：跳过确认提示（用于自动化）

## 目录结构

```
openspec/
├── project.md              # 项目约定
├── specs/                  # 当前真实情况——已经实现的能力
│   └── [capability]/       # 单一聚焦的能力
│       ├── spec.md         # 需求与场景
│       └── design.md       # 技术模式
├── changes/                # 变更提案——计划要改什么
│   ├── [change-name]/
│   │   ├── proposal.md     # 为什么改、改什么、影响
│   │   ├── tasks.md        # 实现清单
│   │   ├── design.md       # 技术决策（可选；视复杂度而定）
│   │   └── specs/          # Spec 增量
│   │       └── [capability]/
│   │           └── spec.md # ADDED/MODIFIED/REMOVED
│   └── archive/            # 已完成变更
```

## 创建变更提案（Change Proposals）

### 决策树

```
新需求？
├─ Bug 修复（恢复 Spec 行为）？ → 直接修
├─ 拼写 / 格式 / 注释？        → 直接改  
├─ 新特性 / 新能力？           → 创建 Proposal
├─ 破坏性变更？               → 创建 Proposal
├─ 架构变更？                  → 创建 Proposal
└─ 不确定？                    → 创建 Proposal（更安全）
```

### Proposal 结构

1. **创建目录：** `changes/[change-id]/`（kebab-case，动词开头，唯一）

2. **编写 `proposal.md`：**
```markdown
# Change: [简要描述这个变更]

## Why
[1–2 句话说明问题或机会]

## What Changes
- [列出具体改动]
- [对 **BREAKING** 变更加粗标记]

## Impact
- 受影响的 Specs：[列出能力]
- 受影响的代码：[关键文件 / 系统]
```

3. **创建 Spec 增量：** `specs/[capability]/spec.md`
```markdown
## ADDED Requirements
### Requirement: New Feature
The system SHALL provide...

#### Scenario: Success case
- **WHEN** user performs action
- **THEN** expected result

## MODIFIED Requirements
### Requirement: Existing Feature
[Complete modified requirement]

## REMOVED Requirements
### Requirement: Old Feature
**Reason**: [Why removing]
**Migration**: [How to handle]
```
如果影响多个能力，为每个能力在 `changes/[change-id]/specs/<capability>/spec.md` 下分别创建增量文件。

4. **创建 `tasks.md`：**
```markdown
## 1. Implementation
- [ ] 1.1 Create database schema
- [ ] 1.2 Implement API endpoint
- [ ] 1.3 Add frontend component
- [ ] 1.4 Write tests
```

5. **按需创建 `design.md`：**
若满足以下任一条件，则应创建 `design.md`，否则可省略：
- 变更跨多个服务 / 模块，或引入新的架构模式
- 新增外部依赖，或进行重要的数据模型修改
- 涉及安全、性能或复杂迁移
- 存在较大不确定性，需要先达成技术共识

最小化的 `design.md` 模板：
```markdown
## Context
[背景、约束、相关干系人]

## Goals / Non-Goals
- Goals: [...]
- Non-Goals: [...]

## Decisions
- Decision: [做了什么决定，为什么]
- Alternatives considered: [考虑过哪些方案，以及取舍]

## Risks / Trade-offs
- [风险] → 应对策略

## Migration Plan
[迁移步骤与回滚方案]

## Open Questions
- [...]
```

## Spec 文件格式

### 关键点：Scenario 格式

**正确示例**（使用 `####` 标题）：
```markdown
#### Scenario: User login success
- **WHEN** valid credentials provided
- **THEN** return JWT token
```

**错误示例**（不要用列表或粗体替代标题）：
```markdown
- **Scenario: User login**  ❌
**Scenario**: User login     ❌
### Scenario: User login      ❌
```

每条 Requirement **必须** 至少包含一个 Scenario。

### Requirement 的措辞
- 对规范性要求使用 SHALL / MUST（避免使用 should / may，除非明确是非强制要求）

### 增量操作类型（Delta Operations）

- `## ADDED Requirements`：新增能力
- `## MODIFIED Requirements`：已有行为发生变化
- `## REMOVED Requirements`：废弃能力
- `## RENAMED Requirements`：仅更名

匹配头部时使用 `trim(header)`，忽略前后空白。

#### 何时使用 ADDED vs MODIFIED
- **ADDED**：引入一个全新能力或子能力，可以单独作为 Requirement 存在。对于“正交”变化（例如新增 “Slash Command Configuration”），优先使用 ADDED，而不是修改旧 Requirement。
- **MODIFIED**：变更已有 Requirement 的行为、范围或验收标准。此时必须粘贴完整的 Requirement 内容（标题 + 所有 Scenarios）。归档时会用你提供的内容整体替换原 Requirement；若只写“差异”，会导致旧细节丢失。
- **RENAMED**：仅更名时使用。如果同时修改行为，应在 RENAMED（名称变更）之外，再写一个 MODIFIED（内容变更），并引用新名称。

常见坑：
- 把需要“新增关注点”的情况写成 MODIFIED，却没有带上旧文本。这会在归档时丢失历史细节。如果你不是在显式修改旧 Requirement，请在 ADDED 中新建一条 Requirement。

正确编写 MODIFIED Requirement 的步骤：
1. 在 `openspec/specs/<capability>/spec.md` 中找到已有 Requirement。
2. 从 `### Requirement: ...` 到所有 Scenarios，整体复制该块内容。
3. 粘贴到 `## MODIFIED Requirements` 下，再在此基础上修改为新行为。
4. 确保标题文本完全一致（忽略空白），并至少保留一个 `#### Scenario:`。

RENAMED 示例：
```markdown
## RENAMED Requirements
- FROM: `### Requirement: Login`
- TO: `### Requirement: User Authentication`
```

## 故障排查

### 常见错误

**“Change must have at least one delta”**
- 检查 `changes/[name]/specs/` 目录是否存在且包含 .md 文件
- 确认文件中存在操作头（如 `## ADDED Requirements`）

**“Requirement must have at least one scenario”**
- 检查是否使用了 `#### Scenario:` 形式（4 个井号）
- 不要使用列表项或粗体来写 Scenario 标题

**场景解析无输出（静默失败）**
- 必须精确使用 `#### Scenario: Name` 格式
- 调试命令：`openspec show [change] --json --deltas-only`

### 校验技巧

```bash
# 总是使用 strict 模式做完整校验
openspec validate [change] --strict

# 调试 delta 解析
openspec show [change] --json | jq '.deltas'

# 查看某个特定 Requirement
openspec show [spec] --json -r 1
```

## Happy Path 示例脚本

```bash
# 1）查看当前状态
openspec spec list --long
openspec list
# 可选全文搜索：
# rg -n "Requirement:|Scenario:" openspec/specs
# rg -n "^#|Requirement:" openspec/changes

# 2）选择 change id 并搭建脚手架
CHANGE=add-two-factor-auth
mkdir -p openspec/changes/$CHANGE/{specs/auth}
printf "## Why\n...\n\n## What Changes\n- ...\n\n## Impact\n- ...\n" > openspec/changes/$CHANGE/proposal.md
printf "## 1. Implementation\n- [ ] 1.1 ...\n" > openspec/changes/$CHANGE/tasks.md

# 3）添加增量 spec（示例）
cat > openspec/changes/$CHANGE/specs/auth/spec.md << 'EOF'
## ADDED Requirements
### Requirement: Two-Factor Authentication
Users MUST provide a second factor during login.

#### Scenario: OTP required
- **WHEN** valid credentials are provided
- **THEN** an OTP challenge is required
EOF

# 4）校验
openspec validate $CHANGE --strict
```

## 多能力（Multi-Capability）示例

```
openspec/changes/add-2fa-notify/
├── proposal.md
├── tasks.md
└── specs/
    ├── auth/
    │   └── spec.md   # ADDED: Two-Factor Authentication
    └── notifications/
        └── spec.md   # ADDED: OTP email notification
```

`auth/spec.md`
```markdown
## ADDED Requirements
### Requirement: Two-Factor Authentication
...
```

`notifications/spec.md`
```markdown
## ADDED Requirements
### Requirement: OTP Email Notification
...
```

## 最佳实践

### 先追求简单（Simplicity First）
- 默认新增代码 < 100 行
- 优先单文件实现，除非明显不足以支撑
- 没有明确理由时，避免引入新框架
- 选择“无聊但证明有效”的模式

### 触发复杂度升级的条件
仅在以下情况下引入更复杂的方案：
- 有性能数据表明当前方案过慢
- 有明确的规模需求（>1000 用户，>100MB 数据等）
- 多个已验证用例需要抽象复用

### 清晰引用（Clear References）
- 代码位置用 `file.ts:42` 形式
- 引用 Spec 时使用 `specs/auth/spec.md`
- 关联相关 Changes 和 PR 链接

### 能力命名
- 使用“动词-名词”形式：`user-auth`、`payment-capture`
- 每个能力只做一件事
- 遵循“10 分钟可理解”原则
- 当描述中需要 “AND” 才说得清时，应拆分为多个能力

### Change ID 命名
- 使用 kebab-case，简短且有描述性：`add-two-factor-auth`
- 优先使用动词前缀：`add-`、`update-`、`remove-`、`refactor-`
- 确保唯一；若已被占用，可追加 `-2`、`-3` 等

## 工具选择指南

| Task | Tool | Why |
|------|------|-----|
| 按模式查找文件 | Glob | 快速的模式匹配 |
| 搜索代码内容   | Grep | 高效的正则搜索 |
| 读取指定文件   | Read | 直接访问文件 |
| 探索未知范围   | Task | 多步骤的调查流程 |

## 错误恢复

### 变更冲突（Change Conflicts）
1. 运行 `openspec list` 查看当前变更
2. 检查是否有重叠的 Specs
3. 与变更所有者沟通协调
4. 如有必要，考虑合并多个 Proposal

### 校验失败（Validation Failures）
1. 使用 `--strict` 参数重新运行
2. 查看 JSON 输出中的详细错误信息
3. 检查 Spec 文件格式是否正确
4. 确认 Scenario 格式合法

### 上下文缺失（Missing Context）
1. 先阅读 `project.md`
2. 检查相关 Specs
3. 回顾最近的归档变更
4. 向需求方或维护者请求澄清

## 快速参考

### 阶段标识
- `changes/` —— 提案中，尚未实现
- `specs/` —— 已实现并部署的能力
- `archive/` —— 已完成并归档的变更

### 文件用途
- `proposal.md` —— 说明为什么改、改什么
- `tasks.md` —— 实现步骤
- `design.md` —— 技术决策
- `spec.md` —— 需求与行为

### CLI 速查
```bash
openspec list              # 当前有哪些变更？
openspec show [item]       # 查看某个条目的详情
openspec validate --strict # 是否通过严格校验？
openspec archive <change-id> [--yes|-y]  # 标记完成并归档（自动化时加 --yes）
```

记住：**Specs 是事实来源（truth），Changes 是提案。务必保持两者同步。**
