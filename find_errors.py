import os

log_path = r"C:\Users\ThienNSH_SE190320\AppData\Local\Unity\Editor\Editor.log"
output_path = r"C:\LEARNING\PRU\filtered_errors.txt"

if not os.path.exists(log_path):
    with open(output_path, "w", encoding="utf-8") as f:
        f.write(f"Log file not found at: {log_path}\n")
    exit(0)

ignored_keywords = [
    "HubConnection",
    "McpManagerClientHub",
    "SocketException",
    "Connection attempt failed",
    "Microsoft.AspNetCore.SignalR",
    "Microsoft.AspNetCore.Http",
    "System.Net.Http",
    "System.Net.Sockets",
    "Microsoft.Extensions.Logging",
    "IvanMurzak.Unity.MCP",
    "IvanMurzak.McpPlugin",
    "R3.Observer",
    "R3.Subject",
    "R3.ThrottleFirst",
    "R3.AnonymousObserver",
    "System.Threading",
    "System.Runtime.CompilerServices",
    "System.Reflection",
    "ves_icall",
    "mono_jit",
    "do_runtime_invoke"
]

with open(log_path, "r", encoding="utf-8", errors="ignore") as f:
    lines = f.readlines()

filtered_lines = []
for line in lines:
    if any(keyword in line for keyword in ignored_keywords):
        continue
    filtered_lines.append(line)

# Keep the last 1500 lines of filtered logs
filtered_lines = filtered_lines[-1500:]

with open(output_path, "w", encoding="utf-8") as f:
    f.writelines(filtered_lines)

print(f"Filtered log written to {output_path}. Total lines: {len(filtered_lines)}")
