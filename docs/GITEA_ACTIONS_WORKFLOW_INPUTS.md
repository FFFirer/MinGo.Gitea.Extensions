下面给出一个 **完整、可落地的流程设计**：
**根据指定 Workflow 或 Workflow 文件获取 `inputs` 定义，并生成可调用的“输入订单（input order/schema）”**，适用于 Gitea Actions，且兼容 GitHub Actions 结构。

目标系统通常用于：

* 自动生成 **触发表单**
* CLI 调用 workflow
* API 参数校验
* 自动 pipeline 编排

---

# 一、总体架构

整体流程：

```
用户选择 workflow
      │
      ▼
获取 workflow metadata
      │
      ▼
读取 workflow YAML
      │
      ▼
解析 workflow_dispatch.inputs
      │
      ▼
生成 Input Schema
      │
      ▼
生成 Input Order（排序后的输入定义）
      │
      ▼
生成调用参数 / 表单 / CLI
```

系统模块：

```
Workflow Resolver
Workflow Loader
Workflow Parser
Input Schema Builder
Input Order Generator
Input Validator
Dispatch Executor
```

---

# 二、完整流程

## Step 1 获取 workflow 列表

调用 API：

```
GET /repos/{owner}/{repo}/actions/workflows
```

返回：

```json
{
  "workflows": [
    {
      "id": "build.yml",
      "name": "CI Pipeline",
      "path": ".gitea/workflows/build.yml"
    }
  ]
}
```

得到：

```
workflowId
workflowName
workflowPath
```

---

## Step 2 定位 workflow 文件

两种输入方式：

### 方式 A：用户指定 workflow id

```
build.yml
```

查到：

```
.gitea/workflows/build.yml
```

---

### 方式 B：用户直接指定 workflow path

```
.gitea/workflows/deploy.yml
```

直接读取。

---

## Step 3 获取 workflow YAML

API：

```
GET /repos/{owner}/{repo}/contents/{path}
```

示例：

```
GET /repos/org/app/contents/.gitea/workflows/build.yml
```

返回：

```json
{
  "content": "base64..."
}
```

处理：

```
base64 decode
```

得到 YAML。

---

## Step 4 解析 workflow

解析 YAML AST。

重点结构：

```yaml
on:
  workflow_dispatch:
    inputs:
```

示例：

```yaml
on:
  workflow_dispatch:
    inputs:
      environment:
        description: Deploy env
        required: true
        default: dev

      version:
        description: image version
        required: false

      deploy:
        type: boolean
        default: false
```

---

## Step 5 构建 Input Schema

解析为结构化对象：

```json
{
  "environment": {
    "type": "string",
    "description": "Deploy env",
    "required": true,
    "default": "dev"
  },
  "version": {
    "type": "string",
    "description": "image version",
    "required": false
  },
  "deploy": {
    "type": "boolean",
    "default": false
  }
}
```

---

# 三、生成 Input Order（输入订单）

“输入订单”本质是：

**一个有序的输入参数列表**

排序规则推荐：

```
required first
then optional
then alphabetical
```

生成：

```json
{
  "inputs": [
    {
      "name": "environment",
      "type": "string",
      "required": true,
      "default": "dev"
    },
    {
      "name": "deploy",
      "type": "boolean",
      "required": false,
      "default": false
    },
    {
      "name": "version",
      "type": "string",
      "required": false
    }
  ]
}
```

---

# 四、生成 Input Order 的算法

伪代码：

```python
inputs = workflow["on"]["workflow_dispatch"]["inputs"]

schema = []

for name, meta in inputs.items():
    schema.append({
        "name": name,
        "type": meta.get("type", "string"),
        "required": meta.get("required", False),
        "default": meta.get("default"),
        "description": meta.get("description")
    })

schema.sort(
    key=lambda x: (
        not x["required"],
        x["name"]
    )
)
```

---

# 五、生成调用参数

转换为 dispatch API 参数。

API：

```
POST /repos/{owner}/{repo}/actions/workflows/{workflow}/dispatches
```

请求：

```json
{
  "ref": "main",
  "inputs": {
    "environment": "prod",
    "deploy": "true",
    "version": "1.2.3"
  }
}
```

---

# 六、生成 UI 表单

根据 schema 自动生成 UI。

| 字段      | 类型           |
| ------- | ------------ |
| string  | text input   |
| boolean | switch       |
| choice  | select       |
| number  | number input |

示例 UI：

```
Deploy Environment *
[ dev ▼ ]

Deploy
[ toggle ]

Version
[________]
```

---

# 七、CLI 调用模式

例如 CLI：

```
workflow run build.yml \
  --environment prod \
  --deploy true \
  --version 1.2.3
```

CLI 自动：

```
读取 schema
验证参数
生成 dispatch 请求
```

---

# 八、参数校验

根据 schema 校验：

规则：

```
required 必填
boolean true/false
choice 必须在 options
number 必须为数字
```

示例：

```
environment missing
```

错误：

```
Missing required input: environment
```

---

# 九、缓存机制（重要）

避免每次读取 workflow 文件。

缓存：

```
cache key:
repo + workflow + commit sha
```

失效策略：

```
repo push
workflow change
```

---

# 十、完整系统流程图

```
User
 │
 ▼
Select Workflow
 │
 ▼
Workflow API
 │
 ▼
Get workflow path
 │
 ▼
Load YAML
 │
 ▼
Parse inputs
 │
 ▼
Generate schema
 │
 ▼
Generate input order
 │
 ▼
Generate UI / CLI
 │
 ▼
Validate input
 │
 ▼
Dispatch workflow
```

---

# 十一、最终输出结构（推荐）

统一输出：

```json
{
  "workflow": "build.yml",
  "ref": "main",
  "inputs": [
    {
      "name": "environment",
      "type": "string",
      "required": true,
      "default": "dev",
      "description": "Deploy env"
    },
    {
      "name": "deploy",
      "type": "boolean",
      "default": false
    },
    {
      "name": "version",
      "type": "string"
    }
  ]
}
```

---

# 十二、生产级优化（强烈推荐）

增加：

### 1 Workflow version

```
workflow + commit SHA
```

避免 schema 不一致。

---

### 2 多 trigger 支持

```
workflow_dispatch
workflow_call
```

统一 schema。

---

### 3 inputs schema 标准化

转成 **JSON Schema**

```json
{
  "type": "object",
  "properties": {
    "environment": {
      "type": "string"
    }
  },
  "required": ["environment"]
}
```

---

💡 如果你愿意，我可以再给你一套 **真正生产级的 Gitea Workflow Orchestrator 设计**，包括：

* **自动获取 workflow run_id 的可靠方案**
* **workflow A 自动触发 workflow B**
* **workflow inputs 自动转 JSON Schema**
* **一个仓库实现完整 CI/CD DAG**

这套方案在很多团队里 **直接替代 Jenkins / GitLab CI 编排层**。
