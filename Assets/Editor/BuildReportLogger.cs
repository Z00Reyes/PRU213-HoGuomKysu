using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
using System.Text;

public class BuildReportLogger : IPostprocessBuildWithReport
{
    public int callbackOrder => 999; // Run after other post-processors

    public void OnPostprocessBuild(BuildReport report)
    {
        string outputPath = @"C:\LEARNING\PRU\build_report_errors.txt";
        var sb = new StringBuilder();
        sb.AppendLine("Build Result: " + report.summary.result);
        sb.AppendLine("Total Errors: " + report.summary.totalErrors);
        sb.AppendLine("Total Warnings: " + report.summary.totalWarnings);
        sb.AppendLine("Total Time: " + report.summary.totalTime.TotalSeconds + "s");
        
        sb.AppendLine("\n--- BUILD STEPS ---");
        foreach (var step in report.steps)
        {
            sb.AppendLine($"Step: {step.name} (Duration: {step.duration.TotalSeconds}s)");
            foreach (var msg in step.messages)
            {
                sb.AppendLine($"  [{msg.type}] {msg.content}");
            }
        }
        
        File.WriteAllText(outputPath, sb.ToString());
        Debug.Log("Build report logged to " + outputPath);
    }
}
