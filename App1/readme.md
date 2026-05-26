下载Microsoft.WindowsAppSDK
https://www.nuget.org/packages/Microsoft.WindowsAppSDK/2.1.4-experimental8#supportedframeworks-body-tab


要运行基于 WindowsAppSDK 的自包含应用，需确保平台配置正确并使用显式平台参数执行命令。以下是详细步骤：

1. 确认项目配置
确保 .csproj 文件中已正确设置以下属性（参考前文修改后的配置）：

xml
复制
<PropertyGroup>
  <Platform>x64</Platform> <!-- 必须明确指定平台 -->
  <RuntimeIdentifier>win-x64</RuntimeIdentifier> <!-- 与平台匹配 -->
  <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained> <!-- 自包含模式 -->
</PropertyGroup>
2. 运行应用的命令
方法 1：通过 dotnet run（开发环境）
显式指定平台：
bash
复制
dotnet run -p:Platform=x64
作用：在开发环境中直接运行应用，自动处理依赖项和编译。
注意：需在项目根目录（含 .csproj 文件的目录）执行。
方法 2：通过生成的可执行文件（发布后）
发布应用（生成独立可执行文件）：

bash
复制
dotnet publish -p:Platform=x64 -p:SelfContained=true -p:RuntimeIdentifier=win-x64
输出目录：默认在 bin/x64/<Configuration>/net<version>/publish/（如 bin/x64/Debug/net10.0-windows10.0.22621.0/publish/）。
运行可执行文件：

进入发布目录，直接双击 .exe 文件，或在命令行中执行：
bash
复制
./App1.exe
3. 关键注意事项
(1) 平台一致性
错误示例：若项目配置为 x64 但尝试用 dotnet run -p:Platform=x86 运行，会报平台不兼容错误。
解决方案：始终确保命令中的平台参数（如 x64）与 .csproj 中的 <Platform> 和 <RuntimeIdentifier> 一致。
(2) 自包含模式依赖项
自包含模式会将所有依赖项（包括 .NET 运行时和 WindowsAppSDK）打包到输出目录中，因此发布后的文件体积较大，但无需在目标机器上额外安装运行时。
(3) 调试与发布配置
调试配置（Debug）：
bash
复制
dotnet run -p:Platform=x64 -c Debug
发布配置（Release，优化性能）：
bash
复制
dotnet publish -p:Platform=x64 -c Release -p:SelfContained=true
4. 完整示例流程
修改 .csproj（确保平台和自包含配置正确）：

xml
复制
<PropertyGroup>
  <Platform>x64</Platform>
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
</PropertyGroup>
清理并还原项目：

bash
复制
dotnet clean
dotnet restore
运行应用：

bash
复制
dotnet run -p:Platform=x64
或发布后运行：

bash
复制
dotnet publish -p:Platform=x64 -p:SelfContained=true
cd bin/x64/Debug/net10.0-windows10.0.22621.0/publish/
./App1.exe
5. 常见问题
Q1：运行时报错“无法加载 DLL或依赖项”
原因：自包含模式未正确打包所有依赖项，或目标机器缺少 Visual C++ Redistributable。
解决方案：
确保使用 dotnet publish 发布应用。
在目标机器上安装 Microsoft Visual C++ Redistributable。
