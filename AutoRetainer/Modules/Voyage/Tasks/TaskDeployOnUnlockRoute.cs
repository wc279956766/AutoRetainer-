﻿using AutoRetainer.Internal;
using AutoRetainer.Modules.Voyage.VoyageCalculator;
using AutoRetainerAPI.Configuration;

namespace AutoRetainer.Modules.Voyage.Tasks;

internal static unsafe class TaskDeployOnUnlockRoute
{
    internal static void Enqueue(string name, VoyageType type, SubmarineUnlockPlan unlock, UnlockMode mode)
    {
        VoyageUtils.Log($"Task enqueued: {nameof(TaskDeployOnUnlockRoute)} (plan: {unlock})");
        TaskIntelligentRepair.Enqueue(name, type);
        P.TaskManager.Enqueue(TaskDeployOnBestExpVoyage.SelectDeploy);
        EnqueuePickOrCalc(unlock, mode);
        P.TaskManager.Enqueue(TaskDeployOnBestExpVoyage.Deploy);
        TaskDeployAndSkipCutscene.Enqueue(true);
    }
    internal static void EnqueuePickOrCalc(SubmarineUnlockPlan unlock, UnlockMode mode)
    {
        P.TaskManager.Enqueue(() => PickFromPlanOrCalc(unlock, mode), $"PickFromPlanOrCalc({unlock}, {mode})");
        TaskCalculateAndPickBestExpRoute.Enqueue(unlock);
    }

    internal static void PickFromPlanOrCalc(SubmarineUnlockPlan unlock, UnlockMode mode)
    {
        var adjustedPoints = GetUnlockPointsFromPlan(unlock, mode);
        if(adjustedPoints.Length > 0)
        {
            TaskCalculateAndPickBestExpRoute.Stop = true;
            TaskPickSubmarineRoute.EnqueueImmediate(VoyageUtils.GetSubmarineExploration(adjustedPoints.First().point).Value.Map.RowId, adjustedPoints.Select(x => x.point).ToArray());
        }
    }

    internal static (uint point, string justification)[] GetUnlockPointsFromPlan(SubmarineUnlockPlan unlock, UnlockMode mode)
    {
        var points = unlock.GetPrioritizedPointList();
        VoyageUtils.Log($"GetPrioritizedPointList: {points.Select(x => $"{x.point}/{x.justification}").Join("\n")}");
        var numPoints = mode == UnlockMode.SpamOne ? 1 : 5;
        var subLevel = CurrentSubmarine.Get()->RankId;
        var adjustedPoints = points.Where(x => subLevel >= VoyageUtils.GetSubmarineExploration(x.point)?.RankReq).Take(numPoints);
        return adjustedPoints.ToArray();
    }
}
