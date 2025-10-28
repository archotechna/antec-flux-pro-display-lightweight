## Antec Flux Pro Display Service
A small, lightweight app that runs in the background that gets system cpu and gpu temperature using libre hardware monitor's library. Data is displayed directly to your Antec Flux Pro display on the side of the case. 

Requires Administrator privileges in order to fetch CPU and GPU data from libre hardware monitor's library.

## Installation
You will need to install the PawnIO driver from here
- https://pawnio.eu
- Code: https://github.com/namazso/PawnIO

PawnIO is an alternative driver that retrieves CPU temperature data. Winring0 was the original method of retrieving this data, which presents security vulnerabilities.

The LibreHardwareMonitor library has since switched to using PawnIO over Winring0, and so this driver is needed for the application to run correctly.

## Notes
To build and export the project locally, I run this
`dotnet publish -c Release -p:SelfContained=false`
