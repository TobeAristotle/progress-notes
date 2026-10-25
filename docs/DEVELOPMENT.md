# 开发与验证

## 结构

DesktopNote.cs 包含桌面面板与编辑交互；Typography.cs 处理中文、英文混排与输入法状态；PaperStyle.cs 提供界面组件；Domain.cs 定义带版本的数据；Storage.cs 原子读写与恢复；SharedData.cs 固定目录、备份、显式导入和写锁；Appearance.cs 提供分层窗口与背景透明；AutoStart.cs 管理当前用户启动项。

## 测试范围

test.ps1 仅使用 test-results 下新建的隔离目录，检查序列化、备份恢复、格式迁移、写锁、未来格式拒绝和内存模拟的启动项。不会改实际个人数据或Windows启动设置。

有桌面会话时可另外运行 --editor-self-test、--ime-test、--settings-preview、--personal-preview 和 --transparency-test（均需指定输出文件或目录，参考对应 Main 分支）。输入法测试使用模拟消息，不能替代真实拼音候选框测试。不要在CI服务会话中运行桌面嵌入的 --smoke。

## 贡献

提交前运行构建和隔离测试。界面截图请用虚构内容；改动存储格式时更新版本检查、迁移前备份及失败恢复测试。不要直接覆盖用户的数据；不要提交运行数据、注册表导出、访问令牌或本机快捷方式。
