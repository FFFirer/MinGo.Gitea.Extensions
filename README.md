# MinGo Gitea Extensions

Gitea / GitHub Actions 管理面板：多连接配置、工作流触发、运行状态跟踪。

## 功能

- **多 Provider**：同时管理多个 Gitea 与 GitHub 连接，仓库/运行记录合并展示
- **手动触发**：选择仓库 → 工作流 → 分支，支持 `workflow_dispatch` inputs
- **模板**：将历史触发保存为模板，一键重跑
- **状态跟踪**：后台轮询运行状态（completed / success 等）

## 连接配置

设置页可添加多条连接：

| 字段 | Gitea | GitHub |
|------|-------|--------|
| API URL | `https://gitea.example.com` | `https://api.github.com`（或 GHE `https://github.example.com/api/v3`） |
| Token | Gitea API Token | PAT（需 `repo` / `workflow` 权限） |

## 开发

```bash
dotnet restore main.slnx
dotnet build main.slnx
dotnet exec tests/MinGo.Gitea.Core.Tests/bin/Debug/net10.0/MinGo.Gitea.Core.Tests.dll
```

前端（Admin 内 Vite）：

```bash
cd src/MinGo.Gitea.Admin
pnpm install
pnpm run build
```

## 架构

```
IActionsApiClient
├── GiteaApiClient   /api/v1/*
└── GitHubApiClient  /repos/*/actions/*

ActionsClientFactory  → 按 ProviderConnection 创建客户端
ProviderConnectionService → 多连接 CRUD（表 ProviderConnections）
WorkflowService → 合并多连接仓库列表，按 ConnectionId 路由 API
```

# References

Gitea API Document: https://docs.gitea.com/zh-cn/api/1.25/
GitHub REST API: https://docs.github.com/en/rest/actions/workflows
