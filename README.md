# About

This plugin sends JSON Records with various Telemetry Information like Speed,
Rotation, Position, Inputs, current Map, etc to a listening TCP Socket

Default Target is 127.0.0.1:31337, can be changed in the Plugin config File

# Compilation

After opening the Project you'll need to remove and re-add the following
References:
- Spectrum API (`Spectrum.API.dll` in your Spectrum folder)
- JsonFX (`JsonFx.dll` in your Spectrum folder)
- Assembly-CSharp (`Assembly-CSharp.dll` in your Distance\Distance_Data\Managed folder)
- UnityEngine (`UnityEngine.dll` in your Distance\Distance_Data\Managed folder)