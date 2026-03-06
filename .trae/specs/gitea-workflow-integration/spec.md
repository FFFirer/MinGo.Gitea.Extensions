# Gitea 工作流集成功能规格

## 1. 概述

根据原型设计 `docs/prototype.html`，重新实现 Gitea 工作流集成功能，包括工作流执行概览、手动触发工作流、执行历史和设置页面。

## 2. 技术栈

- .NET 10
- ASP.NET Core 10
- Blazor WebAssembly
- TailwindCSS
- EFCore + Sqlite

## 3. 页面结构

### 3.1 导航栏

- 实现与原型一致的导航栏，包含以下菜单项：
  - 首页
  - 触发工作流
  - 执行历史
  - 设置
- 导航栏应固定在页面顶部，使用白色背景和阴影效果
- 当前激活的菜单项应显示为蓝色

### 3.2 首页

- 显示工作流执行概览卡片：
  - 总执行次数
  - 成功率
  - 平均执行时间
  - 运行中
- 显示最近执行记录表格，包含：
  - 工作流名称
  - 仓库
  - 状态
  - 开始时间
  - 操作按钮

### 3.3 触发工作流

- 实现四步流程：
  1. 选择仓库
  2. 选择工作流
  3. 选择分支
  4. 触发工作流
- 每步都应有对应的步骤指示器
- 实现页面间的导航逻辑

### 3.4 执行历史

- 显示执行记录列表，包含：
  - 工作流名称
  - 仓库
  - 分支
  - 状态
  - 开始时间
  - 操作按钮
- 提供搜索过滤功能

### 3.5 设置页面

- 实现 Gitea API 配置表单：
  - Gitea URL
  - API Token
- 提供保存设置功能

## 4. 可复用组件

### 4.1 导航栏组件 (`Navbar.razor`)

- 实现与原型一致的导航栏
- 支持响应式设计
- 处理菜单项激活状态

### 4.2 卡片组件 (`Card.razor`)

- 实现通用卡片样式
- 支持自定义标题和内容
- 包含悬停效果

### 4.3 步骤指示器组件 (`Steps.razor`)

- 实现四步流程的步骤指示器
- 支持标记已完成和当前步骤
- 包含连接线和动画效果

### 4.4 工作流卡片组件 (`WorkflowCard.razor`)

- 实现工作流选择卡片
- 支持激活状态
- 包含工作流名称、描述和分支信息

### 4.5 消息提示组件 (`Toast.razor`)

- 实现成功、警告和错误消息提示
- 支持自动消失
- 包含动画效果

### 4.6 状态标签组件 (`StatusBadge.razor`)

- 实现不同状态的标签样式
- 支持成功、运行中、失败状态

## 5. 数据模型

### 5.1 工作流运行历史

```csharp
public class WorkflowRunHistory
{
    public int Id { get; set; }
    public string WorkflowName { get; set; }
    public string Repository { get; set; }
    public string Branch { get; set; }
    public string Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}
```

### 5.2 Gitea 配置

```csharp
public class GiteaConfig
{
    public string Url { get; set; }
    public string Token { get; set; }
}
```

## 6. 服务

### 6.1 工作流服务

- 提供获取工作流列表的方法
- 提供触发工作流的方法
- 提供获取工作流运行历史的方法

### 6.2 Gitea API 客户端

- 实现与 Gitea API 的交互
- 提供获取仓库、工作流和分支的方法
- 提供触发工作流的方法

## 7. 样式实现

- 使用 TailwindCSS 实现与原型一致的样式
- 定义与原型相同的颜色变量
- 实现响应式设计
- 确保所有动画效果与原型一致

## 8. 实现计划

1. 创建可复用组件
2. 实现导航栏和布局
3. 实现首页
4. 实现触发工作流流程
5. 实现执行历史页面
6. 实现设置页面
7. 集成服务和数据模型
8. 测试和优化

## 9. 验收标准

- 所有页面和组件与原型设计完全一致
- 所有功能正常工作
- 响应式设计适配不同屏幕尺寸
- 动画效果与原型一致
- 代码结构清晰，组件复用性高