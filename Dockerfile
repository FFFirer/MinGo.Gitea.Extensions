# 第一阶段：构建前端
FROM swr.cn-north-4.myhuaweicloud.com/ddn-k8s/docker.io/library/node:20-alpine AS frontend-build

# 安装 pnpm
ARG NPM_REGISTRY=https://registry.npmjs.org/

ENV COREPACK_NPM_REGISTRY=${NPM_REGISTRY}
ENV PNPM_HOME="/pnpm"
ENV PATH="$PNPM_HOME:$PATH"

RUN npm install -g corepack@latest

RUN corepack enable && corepack prepare pnpm@latest --activate
RUN pnpm config set registry https://registry.npmmirror.com

# 设置工作目录
WORKDIR /app

# 复制 package.json 和 pnpm-lock.yaml 文件，用于缓存
COPY src/MinGo.Gitea.Admin/package.json src/MinGo.Gitea.Admin/pnpm-lock.yaml .

# 还原 npm 包（使用缓存）
RUN pnpm install --frozen-lockfile

# 复制前端项目目录其余文件
COPY src/MinGo.Gitea.Admin .

# 构建前端资源
RUN pnpm run build

# 第二阶段：构建 .NET 应用
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build

# 设置工作目录
WORKDIR /app

# 复制 .Net 的项目文件及解决方案文件，用于缓存
COPY main.slnx .
COPY src/MinGo.Gitea.Admin/MinGo.Gitea.Admin.csproj src/MinGo.Gitea.Admin/
COPY src/MinGo.Gitea.Core/MinGo.Gitea.Core.csproj src/MinGo.Gitea.Core/
COPY tests/MinGo.Gitea.Core.Tests/MinGo.Gitea.Core.Tests.csproj tests/MinGo.Gitea.Core.Tests/
COPY Directory.Packages`.props .
COPY nuget.config .

# 还原 nuget 包（使用缓存）
RUN dotnet restore

# 复制全部项目文件
COPY . .

# 复制前端构建产物
COPY --from=frontend-build /app/wwwroot ./src/MinGo.Gitea.Admin/wwwroot

# 使用 Release 编译项目
RUN dotnet build --configuration Release

# 发布 Web 站点
RUN dotnet publish src/MinGo.Gitea.Admin --configuration Release --no-build --output /app/publish

# 第三阶段：发布
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# 设置工作目录
WORKDIR /app

# 复制发布文件
COPY --from=backend-build /app/publish .

# 创建数据目录（如果需要）
RUN mkdir -p /app/data

# 暴露端口
EXPOSE 8080

# 设置环境变量
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# 运行应用
ENTRYPOINT ["dotnet", "MinGo.Gitea.Admin.dll"]