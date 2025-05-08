using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text;

public class CallCenterWorkflowPlugin
{
    [KernelFunction]
    [Description("Generate a workflow for a call center software as a service")] 
    public string GenerateWorkflow(
        [Description("Name of the workflow")] string workflowName,
        [Description("Comma-separated list of workflow steps")] string steps)
    {
        var stepList = steps.Split(',');
        var sb = new StringBuilder();
        sb.AppendLine($"Workflow: {workflowName}");
        sb.AppendLine("Steps:");
        int i = 1;
        foreach (var step in stepList)
        {
            sb.AppendLine($"  {i++}. {step.Trim()}");
        }
        return sb.ToString();
    }
}
