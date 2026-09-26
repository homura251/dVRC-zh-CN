# dVRC 简体中文说明

> 本仓库用于恢复你自己上传到 VRChat 的 Avatar / World 资源。请勿用于下载、复制或传播不属于你的内容。

本汉化基于上游 `200Tigersbloxed/dVRC` 的 `main` 分支，审查基准提交为 `2e2e89e043525c5daa5debe8dc43354538305f92`。汉化仅修改用户可见文字与错误提示，不修改 VRChat API 字段、下载地址、文件格式、平台标识或恢复流程。

## 使用方法

1. 将 dVRC 导入一个已经安装 VRCSDK 的 Unity 项目。
2. 打开 VRChat SDK 控制面板并登录。
3. 打开菜单 `dVRC > 主窗口`。
4. 在 Content Manager 中确保你的已上传内容已经加载。
5. 在 dVRC 中选择 Avatar 或世界，并下载你自己的资源。
6. 如需从 AssetBundle 中提取 Unity 工程资源，可在 dVRC 中下载 AssetRipper，然后选择本地 `.vrca` / `.vrcw` 文件进行提取。

## 重要安全提示

当前上游代码会从 AssetRipper GitHub Releases 的 `latest` 地址下载 ZIP，随后直接解压并执行，代码中没有固定版本、SHA-256 或数字签名校验。因此，首次安装或重新安装 AssetRipper 时存在第三方供应链风险。

资源下载由 VRChat SDK 提供的 API 完成。dVRC 本身未实现读取浏览器 Cookie、VRChat authToken、系统凭据或上传资源到作者服务器的逻辑；但 VRChat SDK 本身仍会按正常方式与 VRChat 服务通信。

AssetRipper 启动后通过 `127.0.0.1:42176` 与 dVRC 通信。该连接只指向本机，但端口固定且没有 dVRC 侧身份验证，安全敏感环境中应避免同时运行不受信任的本地程序。

## 许可证

上游 dVRC 使用 GNU GPL v3.0。此简体中文修改版继续遵守上游许可证，并明确标记为修改版本。
