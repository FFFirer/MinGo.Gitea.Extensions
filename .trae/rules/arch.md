# 目标

通过[[Gitea开放平台接口]]获取Gitea中工作流信息，实现更多触发工作流执行的方式

# Gitea开放平台接口

## 有哪些接口？

- OpenAPI文档：https://docs.gitea.cn/redocusaurus/plugin-redoc-2.yaml
- 接口文档地址：https://docs.gitea.cn/api/1.20/
    - 左边是按模块区分的导航菜单
    - 右边正文是接口描述及示例

## 如何接入？

- 参考API使用指南：https://docs.gitea.cn/development/api-usage 了解如何接入

# 技术框架

## 原则

- 使用.NET 10
- 使用ASP.NET Core 10
- 使用Blazor作为前端页面实现技术
- 使用TailwindCSS作为前端样式使用的技术
- 使用EFCore+Sqlite作为数据库数据持久化能力

## 架构

- MinGo.GiteaExtensionsAdmin：Blazor应用，页面及页面逻辑开发
- MinGo.GiteaExtensions：主要业务逻辑实现，访问Gitea API封装，基础通用能力
- 代码使用整洁架构/洋葱架构，使用文件夹区分不同的层次
  - Application, 业务逻辑实现
  - Core, 核心对象及通用能力
  - Infrastructure, 基础设施实现，如数据库访问、Gitea API封装等
  - Web, UI交互逻辑
  - Web依赖Infrastructure和Application，Infrastructure和Application依赖Core

# 开发模式

- 使用不同的角色分工不同的职责，完成不同的任务时使用不同的角色
  - 前端开发：负责页面设计及交互逻辑实现
  - 后端开发：负责业务逻辑实现，数据库访问等
  - 数据库开发：负责数据库设计及实现
  - 测试开发：负责测试用例编写及执行 

# 不能做

- 使用过时的框架或技术
- 机密信息不能混入到代码提交

# 实现规范

## 空值检查

- 如果是必要的参数，则抛出明确的异常
- 如果是可选的参数，则使用默认值

# 文件组织

- docs：存放项目文档