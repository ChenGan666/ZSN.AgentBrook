# 使用 .NET 10 运行时镜像
FROM mcr.m.daocloud.io/dotnet/aspnet:10.0
WORKDIR /app
EXPOSE 5006

# 将发布的文件复制到容器中
COPY ./publish/ ./
COPY ./appsettings.json ./appsettings.json

# 设置入口点
ENTRYPOINT ["dotnet", "ZSN.AgentBrook.Client.dll"]
