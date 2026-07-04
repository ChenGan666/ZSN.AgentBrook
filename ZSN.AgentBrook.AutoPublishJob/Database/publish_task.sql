-- ============================================================
-- 应用工厂 (App Factory) —— 发布任务表
-- 数据库：业务库(MySql, 与 tb_task_info 同库, connectionName=JobDb)
-- 说明：与工作流任务表 tb_task_info 物理隔离，由独立的
--       ZSN.AgentBrook.AutoPublishJob 服务轮询消费。
-- 状态机：Pending(0)→Cloning(1)→Customizing(2)→Building(3)→Verifying(4)→Done(5)
--         任何阶段失败 → Failed(-1)
-- ============================================================

DROP TABLE IF EXISTS `tb_publish_task`;
CREATE TABLE `tb_publish_task` (
  `TaskID` varchar(64) NOT NULL COMMENT '发布任务编号(主键 GUID)',
  `AppID` varchar(64) NOT NULL COMMENT '要导出的平台 App 的 AppID',
  `TemplateName` varchar(64) DEFAULT NULL COMMENT '模板名称(如 MeetingAssistant / FileConverter / Base)',
  `TemplateGitUrl` varchar(512) DEFAULT NULL COMMENT '模板 Git 仓库地址',
  `TemplateRef` varchar(64) DEFAULT NULL COMMENT '模板版本引用(git 分支/tag/commit)',
  `PublishConfig` json DEFAULT NULL COMMENT '品牌定制与构建配置(JSON)',
  `State` int(11) NOT NULL DEFAULT '0' COMMENT '状态：-1失败,0等待,1克隆中,2定制中,3构建中,4校验中,5完成',
  `Progress` int(11) NOT NULL DEFAULT '0' COMMENT '完成进度 0-100',
  `Stage` varchar(128) DEFAULT NULL COMMENT '当前阶段描述',
  `Logs` longtext COMMENT '累积构建日志(流式追加)',
  `TargetPlatforms` varchar(128) DEFAULT NULL COMMENT '构建目标平台(逗号分隔,如 WinX64,Web)',
  `ArtifactPath` varchar(512) DEFAULT NULL COMMENT '产物磁盘路径(完成后填写)',
  `ArtifactFileCode` varchar(64) DEFAULT NULL COMMENT '产物关联的 FilesInfo.FileCode',
  `ErrorMsg` text COMMENT '失败时的错误信息',
  `ReCallUrl` varchar(512) DEFAULT NULL COMMENT '完成回调URL(仿 MarkdownJob.ReCallUrl)',
  `CreateMemberID` varchar(64) DEFAULT NULL COMMENT '提交人(C端会员 MemberID)',
  `CreateUserID` varchar(64) DEFAULT NULL COMMENT '提交人(后台管理员 UserID)',
  `CreateTime` datetime NOT NULL COMMENT '创建时间',
  `StartTime` datetime DEFAULT NULL COMMENT '开始执行时间',
  `FinishTime` datetime DEFAULT NULL COMMENT '完成时间',
  `UpdateTime` datetime NOT NULL COMMENT '更新时间',
  PRIMARY KEY (`TaskID`),
  KEY `idx_state_createtime` (`State`, `CreateTime`),
  KEY `idx_appid` (`AppID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='应用工厂发布任务表';
