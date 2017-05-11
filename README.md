# About

This plugin sends JSON Records with various Telemetry Information like Speed,
Rotation, Position, Inputs, current Map, etc to a listening TCP Socket

Default Target is 127.0.0.1:31337, can be changed in the Plugin config File

This Plugin is supposed to be used with a (not yet existing) Dashboard
"reciever" to display realtime statistics on a dedicated screen.

It can also be used to do data analysis and visualization when the
output is redirected to a file.

# Compilation

After opening the Project you'll need to remove and re-add the following
References:
- Spectrum API (`Spectrum.API.dll` in your Spectrum folder)
- JsonFX (`JsonFx.dll` in your Spectrum folder)
- Assembly-CSharp (`Assembly-CSharp.dll` in your `Distance\Distance_Data\Managed` folder)
- UnityEngine (`UnityEngine.dll` in your `Distance\Distance_Data\Managed` folder)

# Installation

Just drop `Telemetry.plugin.dll` into your Spectrum plugins folder

# Configuration

The configuration file is located in `Distance\Distance_Data\Spectrum\Settings\`,
it's a simple JSON file with 3 keys:

- Host: Host to connect to for streaming data over TCP (use "" to disable TCP Streaming)
- Port: Port to connect on (use 0 to disable TCP Streaming)
- File_Prefix: Filename prefix to write data to (only use when TCP is disables)

Default Values are as follows:
- Host: ""
- Port: 0
- File_Prefix: "Telemetry"

Note:
you only need to set Host to "" or Port to 0 to disable TCP, of course you can also do both

# Usage

If TCP mode is enabled the Server component should be running when you start Distance
otherwise the Game may crash or hang

As soon as a Race starts the Plugin should start sending newline-separated JSON records
to the listening TCP Socket, the format should be fairly self-explanatory

When the connection drops an automatic reconnect attempt is made, this may cause
lag in Game.

If TCP mode is not enabled a file name \<Prefix>_\<Current Date and Time>.jsonl is
created in the telemetry subfolder under the Distance main folder
(where the main executable (Distance.exe on Windows) is).

This file contains the same data that would otherwise be sent over the Network.

If no configuration file exists at startup, one with the default values is automatically created