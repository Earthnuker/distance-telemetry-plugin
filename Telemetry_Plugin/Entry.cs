using Spectrum.API;

using Spectrum.Interop.Game;
using Spectrum.Interop.Game.Vehicle;
using Spectrum.API.Interfaces.Plugins;
using Spectrum.API.Interfaces.Systems;
using Spectrum.API.Configuration;

using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using JsonFx.Json;
using UnityEngine;
using System.Net.Sockets;
using Spectrum.API.Storage;

namespace Spectrum.Plugins.Telemetry
{

    public class Entry : IPlugin, IUpdatable
    {
        public StreamWriter logfile;
        public bool active = false;
        public Stopwatch sw = new Stopwatch();
        private Dictionary<string, object> data;
        FileSystem fs = new FileSystem();
        JsonWriter writer = new JsonWriter();
        GameObject car;
        Rigidbody car_rb;
        CarLogic car_log;
        TcpClient tcpClient;
        NetworkStream tcpStream;
        TextWriter data_writer;
        Guid instance_id;
        Guid race_id;
        string conn_host = "";
        int conn_port = -1;
        bool wings = false;
        bool has_wings = true;
        private Settings _settings;


        public void Callback(Dictionary<string, object> data)
        {
            //Console.WriteLine(writer.Write(data));
            data["Sender_ID"] = instance_id.ToString("B");
            data["Race_ID"] = race_id.ToString("B");
            if ((conn_host != "") && (conn_port != -1))
            {
                if (!tcpClient.Connected)
                {
                    Console.WriteLine("[Telemetry] Reconnecting...");
                    tcpClient = new TcpClient(conn_host, conn_port);
                    tcpClient.Connect(conn_host, conn_port);
                    tcpStream = tcpClient.GetStream();
                    data_writer = new StreamWriter(tcpStream);
                }
            }
            writer.Settings.PrettyPrint = false;
            writer.Write(data, data_writer);
            data_writer.WriteLine();
            data_writer.Flush();
            return;
        }

        private void RaceStarted(object sender, EventArgs e)
        {
            Console.WriteLine("[Telemetry] Starting...");
            race_id = Guid.NewGuid();
            sw = Stopwatch.StartNew();
            active = true;
            data = new Dictionary<string, object>
            {
                ["Level"] = Game.LevelName,
                ["Mode"] = Game.CurrentMode,
                ["Real Time"] = DateTime.Now,
                ["Event"] = "start",
                ["Type"] = e,
                ["Time"] = sw.Elapsed.TotalSeconds
            };
            Callback(data);
            /*
            foreach (var ob in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>()) {
                if (ob.name.ToLower().Contains("car")) { 
                    Console.WriteLine(ob.name);
                }
            }
            */
            return;
        }

        private void RaceEnded(object sender, EventArgs e)
        {
            Console.WriteLine("[Telemetry] Finished...");
            data = new Dictionary<string, object>
            {
                ["Level"] = Game.LevelName,
                ["Mode"] = Game.CurrentMode,
                ["Real Time"] = DateTime.Now,
                ["Event"] = "end",
                ["Type"] = e,
                ["Time"] = sw.Elapsed.TotalSeconds
            };
            sw.Stop();
            active = false;
            Callback(data);
            return;
        }


        public void Initialize(IManager manager, string ipcIdentifier)
        {
            writer.Settings.PrettyPrint = true;
            instance_id = Guid.NewGuid();
            Console.WriteLine("[Telemetry] Initializing...");
            Console.WriteLine("[Telemetry] Instance ID {0} ...", instance_id.ToString("B"));
            Race.Started += RaceStarted;
            LocalVehicle.Finished += RaceEnded;
            _settings = new Settings("telemetry");

            if (!_settings.ContainsKey("Host"))
            {
                _settings["Host"] = conn_host;
            }
            else
            {
                conn_host = _settings.GetItem<string>("Host");
            }
            if (!_settings.ContainsKey("Port"))
            {
                _settings["Port"] = conn_port;
            }
            else
            {
                conn_port = _settings.GetItem<int>("Port");
            }

            if (!_settings.ContainsKey("File_Prefix"))
            {
                _settings["File_Prefix"] = "Telemetry";
            }

            _settings.Save();
            if (conn_port != 0 && conn_host != "")
            {
                Console.WriteLine("[Telemetry] Connecting to {0}:{1}...", conn_host, conn_port);
                tcpClient = new TcpClient(conn_host, conn_port);
                tcpStream = tcpClient.GetStream();
                data_writer = new StreamWriter(tcpStream);
            }
            else
            {
                fs.CreateDirectory("telemetry");

                var logname = "telemetry/" + _settings["File_Prefix"] + "_" + string.Format("{0:yyyy-MM-dd_HH-mm-ss}", DateTime.Now) + ".jsonl";
                var fileStream = fs.CreateFile(logname);
                data_writer = new StreamWriter(fileStream);
                Console.WriteLine("[Telemetry] Opening new filestream for {0} ...", fileStream);
            }

            LocalVehicle.WingsOpened += (sender, e) => { wings = true; };
            LocalVehicle.WingsClosed += (sender, e) => { wings = false; };
            LocalVehicle.WingsEnabled += (sender, e) => { has_wings = true; };
            LocalVehicle.WingsDisabled += (sender, e) => { has_wings = false; };
            LocalVehicle.TrickCompleted += LocalVehicle_TrickCompleted;
            LocalVehicle.Split += LocalVehicle_Split;
            LocalVehicle.CheckpointPassed += LocalVehicle_CheckpointPassed;
            LocalVehicle.Collided += LocalVehicle_Collided;
            LocalVehicle.Destroyed += LocalVehicle_Destroyed;
            LocalVehicle.Jumped += LocalVehicle_Jumped;
            LocalVehicle.Respawned += LocalVehicle_Respawned;
            LocalVehicle.Finished += LocalVehicle_Finished;
            LocalVehicle.Exploded += LocalVehicle_Exploded;
            LocalVehicle.Honked += LocalVehicle_Honked;
            return;
        }

        private void LocalVehicle_Honked(object sender, Interop.Game.EventArgs.Vehicle.HonkEventArgs e)
        {

            data = new Dictionary<string, object>
            {
                ["Level"] = Game.LevelName,
                ["Mode"] = Game.CurrentMode,
                ["Real Time"] = DateTime.Now,
                ["Time"] = sw.Elapsed.TotalSeconds,
                ["Event"] = "honked",
                ["Power"] = e.HornPower
            };

            var position = new Dictionary<string, object>
            {
                ["X"] = e.Position.X,
                ["Y"] = e.Position.X,
                ["Z"] = e.Position.X
            };
            data["Pos"] = position;
            Callback(data);
        }

        private void LocalVehicle_Exploded(object sender, Interop.Game.EventArgs.Vehicle.DestroyedEventArgs e)
        {
            data = new Dictionary<string, object>
            {
                ["Level"] = Game.LevelName,
                ["Mode"] = Game.CurrentMode,
                ["Real Time"] = DateTime.Now,
                ["Time"] = sw.Elapsed.TotalSeconds,
                ["Event"] = "exploded",
                ["Cause"] = e.Cause
            };
            Callback(data);
        }

        private void LocalVehicle_Finished(object sender, Interop.Game.EventArgs.Vehicle.FinishedEventArgs e)
        {
            data["Level"] = Game.LevelName;
            data["Mode"] = Game.CurrentMode;
            data["Real Time"] = DateTime.Now;
            data["Event"] = "finish";
            data["Final Time"] = e.FinalTime;
            data["Time"] = sw.Elapsed.TotalSeconds;
            data["Type"] = e.Type;
            Callback(data);
        }

        private void LocalVehicle_Respawned(object sender, Interop.Game.EventArgs.Vehicle.RespawnEventArgs e)
        {
            data = new Dictionary<string, object>
            {
                ["Level"] = Game.LevelName,
                ["Mode"] = Game.CurrentMode,
                ["Real Time"] = DateTime.Now,
                ["Time"] = sw.Elapsed.TotalSeconds,
                ["Event"] = "respawn"
            };
            var position = new Dictionary<string, object>
            {
                ["X"] = e.Position.X,
                ["Y"] = e.Position.X,   
                ["Z"] = e.Position.X
            };
            data["Pos"] = position;
            var rotation = new Dictionary<string, object>
            {
                ["Pitch"] = e.Rotation.Pitch * 180,
                ["Roll"] = e.Rotation.Roll * 180,
                ["Yaw"] = e.Rotation.Yaw * 180
            };
            data["Rot"] = rotation;
            Callback(data);
        }

        private void LocalVehicle_Jumped(object sender, EventArgs e)
        {
            data = new Dictionary<string, object>
            {
                ["Level"] = Game.LevelName,
                ["Mode"] = Game.CurrentMode,
                ["Real Time"] = DateTime.Now,
                ["Time"] = sw.Elapsed.TotalSeconds,
                ["Event"] = "jump"
            };
            Callback(data);
        }

        private void LocalVehicle_Destroyed(object sender, Interop.Game.EventArgs.Vehicle.DestroyedEventArgs e)
        {
            data = new Dictionary<string, object>
            {
                ["Level"] = Game.LevelName,
                ["Mode"] = Game.CurrentMode,
                ["Real Time"] = DateTime.Now,
                ["Time"] = sw.Elapsed.TotalSeconds,
                ["Event"] = "destroyed",
                ["Cause"] = e.Cause
            };
            Callback(data);
        }

        private void LocalVehicle_Collided(object sender, Interop.Game.EventArgs.Vehicle.ImpactEventArgs e)
        {
            data = new Dictionary<string, object>
            {
                ["Level"] = Game.LevelName,
                ["Mode"] = Game.CurrentMode,
                ["Real Time"] = DateTime.Now,
                ["Time"] = sw.Elapsed.TotalSeconds,
                ["Event"] = "collision",
                ["Target"] = e.ImpactedObjectName
            };
            var position = new Dictionary<string, object>
            {
                ["X"] = e.Position.X,
                ["Y"] = e.Position.X,
                ["Z"] = e.Position.X
            };
            data["Pos"] = position;
            data["Speed"] = e.Speed;
            Callback(data);
        }

        private void LocalVehicle_CheckpointPassed(object sender, Interop.Game.EventArgs.Vehicle.CheckpointHitEventArgs e)
        {
            data = new Dictionary<string, object>
            {
                ["Level"] = Game.LevelName,
                ["Mode"] = Game.CurrentMode,
                ["Real Time"] = DateTime.Now,
                ["Time"] = sw.Elapsed.TotalSeconds,
                ["Event"] = "checkpoint",
                ["Checkpoint Index"] = e.CheckpointIndex,
                ["TrackT"] = e.TrackT
            };
            Callback(data);
        }

        private void LocalVehicle_Split(object sender, Interop.Game.EventArgs.Vehicle.SplitEventArgs e)
        {
            data = new Dictionary<string, object>
            {
                ["Level"] = Game.LevelName,
                ["Mode"] = Game.CurrentMode,
                ["Real Time"] = DateTime.Now,
                ["Time"] = sw.Elapsed.TotalSeconds,
                ["Event"] = "split",
                ["Penetration"] = e.Penetration,
                ["Separation Speed"] = e.SeparationSpeed
            };
            Callback(data);
        }

        private void LocalVehicle_TrickCompleted(object sender, Interop.Game.EventArgs.Vehicle.TrickCompleteEventArgs e)
        {
            data = new Dictionary<string, object>
            {
                ["Level"] = Game.LevelName,
                ["Mode"] = Game.CurrentMode,
                ["Real Time"] = DateTime.Now,
                ["Time"] = sw.Elapsed.TotalSeconds,
                ["Event"] = "trick",
                ["Points"] = e.PointsEarned,
                ["Cooldown"] = e.CooldownAmount,
                ["Grind"] = e.GrindMeters,
                ["Wallride"] = e.WallRideMeters,
                ["Ceiling"] = e.CeilingRideMeters
            };
            Callback(data);
        }

        public void Update()
        {
            if (sw.IsRunning && active)
            {
                car = GameObject.Find("LocalCar");

                car_rb = car.GetComponent<Rigidbody>();
                car_log = car.GetComponent<CarLogic>();

                data = new Dictionary<string, object>
                {
                    ["Level"] = Game.LevelName,
                    ["Mode"] = Game.CurrentMode,
                    ["Real Time"] = DateTime.Now,
                    ["Event"] = "update",
                    ["Time"] = sw.Elapsed.TotalSeconds,
                    ["Speed_KPH"] = LocalVehicle.VelocityKPH,
                    ["Heat"] = LocalVehicle.HeatLevel
                };


                Dictionary<string, object> rotation = new Dictionary<string, object>
                {
                    ["X"] = car.transform.rotation.x * 180,
                    ["Y"] = car.transform.rotation.y * 180,
                    ["Z"] = car.transform.rotation.z * 180,
                    ["W"] = car.transform.rotation.w * 180
                };
                data["Rot"] = rotation;

                Dictionary<string, object> position = new Dictionary<string, object>
                {
                    ["X"] = car.transform.position.x,
                    ["Y"] = car.transform.position.y,
                    ["Z"] = car.transform.position.z
                };
                data["Pos"] = position;

                Dictionary<string, object> velocity = new Dictionary<string, object>
                {
                    ["X"] = car_rb.velocity.x,
                    ["Y"] = car_rb.velocity.y,
                    ["Z"] = car_rb.velocity.z
                };
                data["Vel"] = velocity;

                Dictionary<string, object> angular_velocity = new Dictionary<string, object>
                {
                    ["X"] = car_rb.angularVelocity.x,
                    ["Y"] = car_rb.angularVelocity.y,
                    ["Z"] = car_rb.angularVelocity.z
                };
                data["Ang Vel"] = angular_velocity;

                Dictionary<string, object> inputs = new Dictionary<string, object>
                {
                    ["Boost"] = car_log.CarDirectives_.Boost_,
                    ["Steer"] = car_log.CarDirectives_.Steer_,
                    ["Grip"] = car_log.CarDirectives_.Grip_,
                    ["Gas"] = car_log.CarDirectives_.Gas_,
                    ["Brake"] = car_log.CarDirectives_.Brake_
                };

                Dictionary<string, object> rotation_ctl = new Dictionary<string, object>
                {
                    ["X"] = car_log.CarDirectives_.Rotation_.x,
                    ["Y"] = car_log.CarDirectives_.Rotation_.y,
                    ["Z"] = car_log.CarDirectives_.Rotation_.z
                };
                inputs["Rotation"] = rotation_ctl;

                data["Inputs"] = inputs;

                data["Grav"] = car_rb.useGravity;
                data["Drag"] = car_rb.drag;
                data["Angular Drag"] = car_rb.angularDrag;
                data["Wings"] = wings;
                data["Has Wings"] = has_wings;
                data["All Wheels Contacting"] = car_log.CarStats_.AllWheelsContacting_;
                data["Drive Wheel AVG Rot Vel"] = car_log.CarStats_.DriveWheelAvgRotVel_;
                data["Drive Wheel AVG RPM"] = car_log.CarStats_.DriveWheelAvgRPM_;

                Callback(data);

            }
        }
    }
}