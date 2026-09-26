# dVRC 源码安全审查（简体中文）

审查对象：`200Tigersbloxed/dVRC` `main` @ `2e2e89e043525c5daa5debe8dc43354538305f92`。

## 结论

公开源码中没有发现把 VRChat Token、Cookie、账号密码、系统凭据、剪贴板内容或本地文件主动上传到作者服务器的代码。网络行为主要分为三类：VRChat SDK 获取你账号下的资源信息和文件、GitHub 下载 AssetRipper、以及 dVRC 与本机 AssetRipper 的 `127.0.0.1:42176` HTTP 通信。

## 主要风险

1. **高：AssetRipper 下载未固定版本、未做完整性校验。** `RipperHandler` 使用 GitHub `releases/latest/download`，下载 ZIP 后直接解压并执行。若上游发布账户、Release 资产或依赖链被攻破，恶意可执行文件会以当前 Unity 用户权限运行。
   - 截至 2026-09-26，AssetRipper 最新正式版为 `2.0.0`（2026-08-24 发布），而 dVRC 当前 `main` 的最新提交日期是 2026-06-12。也就是说，dVRC 的 `latest` 下载策略现在会取得一个晚于其代码提交日期的 AssetRipper 版本，兼容性和行为并未由这个 dVRC 提交本身固定下来。
2. **中：固定本地 HTTP 端口且无应用层认证。** dVRC 向 `127.0.0.1:42176` 发送源文件绝对路径和导出目录。代码只检查是否存在名为 `AssetRipper.GUI.Free` 的进程，没有验证监听端口的进程身份。
3. **中：下载/重装 AssetRipper 会递归删除项目根目录下的 `AssetRipper` 文件夹。** 如果用户在该路径放置了其他文件，会直接被删除。
4. **低到中：停止流程会结束所有名为 `AssetRipper.GUI.Free` 的进程。** 可能影响其他项目正在运行的 AssetRipper 实例。
5. **低：`Rip` 使用 `async void` 且缺少异常清理。** 网络或本地 API 调用异常时，`IsWorking` 状态和 AssetRipper 进程可能不能按预期恢复，属于可靠性问题。

## 没有发现的行为

未发现自建作者域名、Webhook、遥测/Analytics、Socket/TCP 客户端、注册表读取、剪贴板读取、环境变量扫描、文件内容批量读取并上传、显式读取 `authToken` / Cookie 的实现。

## 审查边界

`ApiWorld.Fetch`、`ApiFile.DownloadFile`、`APIUser.IsLoggedIn` 等由 VRChat SDK 实现，本仓库只调用它们，因此 VRChat SDK 自身的网络行为不属于本次 dVRC 源码审查范围。AssetRipper 是独立第三方项目，本次仅审查 dVRC 如何下载和调用它，没有把 AssetRipper 全部源码纳入同一轮审计。
