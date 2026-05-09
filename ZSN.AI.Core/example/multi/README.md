# example/multi

多任务计划示例：Task1 → Task2 → Task3 串联，结果通过任务工作目录的 step1.txt/step2.txt/step3.txt 逐步传递。

## 构建

在首次运行前，请构建三个控制台程序：

```
# 在这三个目录分别执行
w:\...\example\multi\apps\Task1> dotnet build -c Release
w:\...\example\multi\apps\Task2> dotnet build -c Release
w:\...\example\multi\apps\Task3> dotnet build -c Release
```

成功后将生成 `bin/Release/net8.0/TaskX.exe` 或 `TaskX.dll`。

## 运行（由系统调度）

- LLM 根据 `SKILL.md` 生成严格 JSON 计划（含 `steps`）。
- `AgentSkillService.ExecuteWithPlanTrackingAsync(...)` 会：
  - 将计划写入任务目录 `plan.md`；
  - 依次运行 `tools/Task1.ps1 → Task2.ps1 → Task3.ps1`；
  - 每步把 stdout/stderr/exitCode 写回 `plan.md`，并在工作目录写入 `step1.txt/step2.txt/step3.txt`。
- 所有步骤成功后，`plan.md` 顶部状态为 `completed`。

## 结果

- `Task1` 输出写入 `step1.txt`
- `Task2` 读取 `step1.txt` 并输出到 `step2.txt`
- `Task3` 读取 `step2.txt` 并输出到 `step3.txt`
- 控制台也会打印每步结果；可在 `plan.md` 里查看汇总。
