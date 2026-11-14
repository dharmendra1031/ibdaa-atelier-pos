# Windows Print Agent — IBDAA Atelier POS

The existing **Open PDF in a new tab** option is unchanged. The agent is used **only** when Shop Settings → **Open invoice PDF in a new tab** is OFF.

## Before deployment
1. Back up the database. Apply `database/041_print_agent_queue.sql` to the approved database **after setting the exact database name**. Verify with `database/042_verify_print_agent_queue.sql`. These scripts are not applied automatically.
2. Deploy the updated ASP.NET Core application to the VPS over HTTPS. The agent makes **outbound HTTPS** requests only; do not expose a printer port to the internet.
3. In Admin → Shop Settings, click **Add terminal** (e.g. `Cashier PC 1`). Copy the numeric **Terminal ID** and one-time **agent token**. Store the token only on the cashier's Windows PC. Disabling a terminal immediately revokes its token.
4. On that cashier's browser, open `https://YOUR-DOMAIN/Pos?terminalId=1` using the actual numeric ID, while signed in as a cashier. This binds the browser to that printer on this PC. Do not use the same terminal token on two computers.

## Windows cashier PC
Install the 80mm thermal printer and SumatraPDF on the **cashier PC**, not the VPS. Use a printer name or Windows default printer.

Publish the agent from the repository (on a developer machine with .NET 10):
```powershell
dotnet publish src/IbdaaAtelier.PrintAgent/IbdaaAtelier.PrintAgent.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\publish\print-agent
```

Copy the published folder to the cashier PC. Configure these environment variables **on that PC for the same Windows user that runs the agent**:

```powershell
[Environment]::SetEnvironmentVariable("IBDAA_AGENT_SERVER", "https://YOUR-DOMAIN", "User")
[Environment]::SetEnvironmentVariable("IBDAA_AGENT_TERMINAL", "1", "User")
[Environment]::SetEnvironmentVariable("IBDAA_AGENT_TOKEN", "ONE-TIME-TOKEN-FROM-ADMIN", "User")
[Environment]::SetEnvironmentVariable("IBDAA_AGENT_SUMATRA", "C:\Program Files\SumatraPDF\SumatraPDF.exe", "User")
# Optional; omit to use the Windows default printer:
[Environment]::SetEnvironmentVariable("IBDAA_AGENT_PRINTER", "YOUR-80MM-PRINTER-NAME", "User")
```

Open a **new** PowerShell session, then run `IbdaaAtelier.PrintAgent.exe`. Once the printer works, configure Windows Task Scheduler to launch the same executable **at logon** under that Windows user. Keep the account/password and agent token private; never commit the token to Git.

## Billing and recovery
- **PDF ON:** Save → existing RDLC PDF opens in a browser tab; user prints manually. No print queue or agent interaction.
- **PDF OFF:** Save → VPS persists the invoice/payment → queues one print job for the paired terminal → agent downloads the 80mm RDLC PDF → SumatraPDF submits it to the printer. The POS displays the print status.
- Printer is off: the job stays **Pending**, and the agent prints it after reconnecting. Don't issue manual duplicate prints while pending.
- Reported failure: POS offers **Retry Print**, which reuses the same numeric PrintJobId and never saves the order/payment again.
- Agent crashed after claiming a job: after two minutes the POS displays **Needs review**. Check the actual paper/spooler **before any manual reprint**. The system deliberately avoids automatic retries when the outcome is uncertain.
- **Completed** means the Windows print command succeeded, **not** proof that paper physically came out. If you need physical confirmation, check the printer.

For multiple cashier PCs, create one terminal per PC, configure each agent with its terminal token, and open the corresponding numeric terminal link in each cashier browser.

Never enable direct printing on VPS without the approved SQL migration, HTTPS, an active terminal and a running Windows agent.
