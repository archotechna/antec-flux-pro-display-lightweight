## Antec Flux Pro Display Service
A small, lightweight app that runs in the background that gets system cpu and gpu temperature using libre hardware monitor's library. Data is displayed directly to your Antec Flux Pro display on the side of the case. 

Requires Administrator privileges in order to fetch CPU and GPU data from libre hardware monitor's library.

## Installation
You will need to install the PawnIO driver from here: https://pawnio.eu
This is an open-source driver that avoids using Winring0 to retrieve CPU temperatures. Winring0 has been known to present security vulnerabilities, and this driver mitigates that issue.
More information about the codebase can be found here: https://github.com/namazso/PawnIO

The LibreHardwareMonitor library has since switched to using PawnIO over Winring0, and so this driver is needed for the application to run correctly.


## Notes
To build and export the project locally, I run this
`dotnet publish -c Release -p:SelfContained=false`
