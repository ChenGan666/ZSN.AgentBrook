import type { ExecutionRecordInfo, NormalizedRecord } from '@/types/chat'

/**
 * Normalize a raw ExecutionRecordInfo (PascalCase from server) into
 * a NormalizedRecord (camelCase) for client-side consumption.
 */
export function normalizeRecord(r: ExecutionRecordInfo): NormalizedRecord {
  const outputs = Array.isArray(r.Outputs)
    ? r.Outputs.map((o) => {
        let parsedValue: any = o && typeof o.value === 'string' ? o.value : o?.value || ''
        if (typeof parsedValue === 'string' && parsedValue) {
          try {
            parsedValue = JSON.parse(parsedValue)
          } catch { /* keep as string */ }
        }
        return { varname: o?.varname, type: o?.type, value: parsedValue }
      })
    : []
  return {
    recordId: r.RecordID,
    sessionId: r.SessionID,
    processesId: r.ProcessesID,
    workflowId: r.WorkflowID,
    taskId: r.TaskID,
    fromMainTaskId: r.FromMainTaskID,
    nodeId: r.NodeID,
    nodeName: r.NodeName,
    nextNodeId: r.NextNodeID,
    startTime: r.StartTime,
    endTime: r.EndTime,
    status: r.Status,
    inputs: r.Inputs,
    outputs,
    logs: Array.isArray(r.Logs) ? r.Logs : [],
  }
}

/**
 * Merge two NormalizedRecord arrays, deduplicating by recordId.
 * Incoming records overwrite existing ones with the same recordId.
 */
export function mergeRecords(
  prev: NormalizedRecord[],
  incoming: NormalizedRecord[],
): NormalizedRecord[] {
  const map = new Map<string, NormalizedRecord>()
  for (const r of prev) map.set(r.recordId, r)
  for (const r of incoming) map.set(r.recordId, r)
  return Array.from(map.values())
}
