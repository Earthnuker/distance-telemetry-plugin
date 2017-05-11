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
- Assembly-CSharp (`Assembly-CSharp.dll` in your Distance\Distance_Data\Managed folder)
- UnityEngine (`UnityEngine.dll` in your Distance\Distance_Data\Managed folder)