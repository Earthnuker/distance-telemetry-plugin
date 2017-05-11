using Spectrum.API;

using Spectrum.API.Game;
using Spectrum.API.Game.Vehicle;
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

namespace Spectrum.Plugins.Telemetry
{

    public class Entry : IPlugin, IUpdatable
    {
        public StreamWriter logfile;
        public string FriendlyName => "Telemetry Plugin";
        public string Author => "Earthnuker";
        public string Contact => "earthnuker@gmail.com";
        public APILevel CompatibleAPILevel => APILevel.UltraViolet;
        public bool active = false;
        public Stopwatch sw = new Stopwatch();
        private Dictionary<string, object> data;
        JsonWriter writer = new JsonWriter();
        GameObject car;
        Rigidbody car_rb;
        CarLogic car_log;
        TcpClient tcpClient;
        NetworkStream tcpStream;
        TextWriter data_writer;
        Guid instance_id;
        Guid race_id;
        string conn_host="";
        int conn_port = -1;
        bool wings = false;
        bool has_wings = true;
        private Settings _settings;


        public void Callback(Dictionary<string, object> data)
        {
            //Console.WriteLine(writer.Write(data));
            data["Sender_ID"] = instance_id.ToString("B");
            data["Race_ID"] = race_id.ToString("B");
            if ((conn_host != "") && (conn_port != -1)) { 
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
            data = new Dictionary<string, object>();
            data["Level"] = Game.LevelName;
            data["Mode"] = Game.CurrentMode;
            data["Real Time"] = DateTime.Now;
            data["Event"] = "start";
            data["Type"] = e;
            data["Time"] = sw.Elapsed.TotalSeconds;
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
            data = new Dictionary<string, object>();
            data["Level"] = Game.LevelName;
            data["Mode"] = Game.CurrentMode;
            data["Real Time"] = DateTime.Now;
            data["Event"] = "end";
            data["Type"] = e;
            data["Time"] = sw.Elapsed.TotalSeconds;
            sw.Stop();
            active = false;
            Callback(data);
            return;
        }


        public void Initialize(IManager manager)
        {
            writer.Settings.PrettyPrint = true;
            instance_id = Guid.NewGuid();
            Console.WriteLine("[Telemetry] Initializing...");
            Console.WriteLine("[Telemetry] Instance ID {0} ...", instance_id.ToString("B"));
            Race.Started += RaceStarted;
            LocalVehicle.Finished += RaceEnded;
            _settings = new Settings(typeof(Entry));

            if (!_settings.ContainsKey("Host")) { 
                _settings["Host"] = conn_host;
            } else { 
                conn_host = _settings.GetItem<string>("Host");
            }
            if (!_settings.ContainsKey("Port")) { 
                _settings["Port"] = conn_port;
            }else { 
                conn_port = _settings.GetItem<int>("Port");
            }

            if (!_settings.ContainsKey("File_Prefix")) { 
                _settings["File_Prefix"] = "Telemetry";
            }

            _settings.Save();
            if (conn_port != 0 && conn_host != "") {
                Console.WriteLine("[Telemetry] Connecting to {0}:{1}...",conn_host,conn_port);
                tcpClient = new TcpClient(conn_host, conn_port);
                tcpStream = tcpClient.GetStream();
                data_writer = new StreamWriter(tcpStream);
            } else {
                string log_filename = "telemetry/" + _settings["File_Prefix"] + "_" + string.Format("{0:yyyy-MM-dd_HH-mm-ss}", DateTime.Now)+".jsonl";
                data_writer = new StreamWriter(log_filename);
                Console.WriteLine("[Telemetry] Opening {0} ...", log_filename);
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

        private void LocalVehicle_Honked(object sender, Spectrum.API.Game.EventArgs.Vehicle.HonkEventArgs e)
        {

            data = new Dictionary<string, object>();
            data["Level"] = Game.LevelName;
            data["Mode"] = Game.CurrentMode;
            data["Real Time"] = DateTime.Now;
            data["Time"] = sw.Elapsed.TotalSeconds;
            data["Event"] = "honked";
            data["Power"] = e.HornPower;
            Dictionary<string, object> position = new Dictionary<string, object>();
            position["X"] = e.Position.X;
            position["Y"] = e.Position.X;
            position["Z"] = e.Position.X;
            data["Pos"] = position;
            Callback(data);
        }

        private void LocalVehicle_Exploded(object sender, Spectrum.API.Game.EventArgs.Vehicle.DestroyedEventArgs e)
        {
            data = new Dictionary<string, object>();
            data["Level"] = Game.LevelName;
            data["Mode"] = Game.CurrentMode;
            data["Real Time"] = DateTime.Now;
            data["Time"] = sw.Elapsed.TotalSeconds;
            data["Event"] = "exploded";
            data["Cause"] = e.Cause;
            Callback(data);
        }

        private void LocalVehicle_Finished(object sender, Spectrum.API.Game.EventArgs.Vehicle.FinishedEventArgs e)
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

        private void LocalVehicle_Respawned(object sender, Spectrum.API.Game.EventArgs.Vehicle.RespawnEventArgs e)
        {
            data = new Dictionary<string, object>();
            data["Level"] = Game.LevelName;
            data["Mode"] = Game.CurrentMode;
            data["Real Time"] = DateTime.Now;
            data["Time"] = sw.Elapsed.TotalSeconds;
            data["Event"] = "respawn";
            Dictionary<string, object> position = new Dictionary<string, object>();
            position["X"] = e.Position.X;
            position["Y"] = e.Position.X;
            position["Z"] = e.Position.X;
            data["Pos"] = position;
            Dictionary<string, object> rotation = new Dictionary<string, object>();
            rotation["Pitch"] = e.Rotation.Pitch * 180;
            rotation["Roll"] = e.Rotation.Roll * 180;
            rotation["Yaw"] = e.Rotation.Yaw * 180;
            data["Rot"] = rotation;
            Callback(data);
        }

        private void LocalVehicle_Jumped(object sender, EventArgs e)
        {
            data = new Dictionary<string, object>();
            data["Level"] = Game.LevelName;
            data["Mode"] = Game.CurrentMode;
            data["Real Time"] = DateTime.Now;
            data["Time"] = sw.Elapsed.TotalSeconds;
            data["Event"] = "jump";
            Callback(data);
        }

        private void LocalVehicle_Destroyed(object sender, Spectrum.API.Game.EventArgs.Vehicle.DestroyedEventArgs e)
        {
            data = new Dictionary<string, object>();
            data["Level"] = Game.LevelName;
            data["Mode"] = Game.CurrentMode;
            data["Real Time"] = DateTime.Now;
            data["Time"] = sw.Elapsed.TotalSeconds;
            data["Event"] = "destroyed";
            data["Cause"] = e.Cause;
            Callback(data);
        }

        private void LocalVehicle_Collided(object sender, Spectrum.API.Game.EventArgs.Vehicle.ImpactEventArgs e)
        {
            data = new Dictionary<string, object>();
            data["Level"] = Game.LevelName;
            data["Mode"] = Game.CurrentMode;
            data["Real Time"] = DateTime.Now;
            data["Time"] = sw.Elapsed.TotalSeconds;
            data["Event"] = "collision";
            data["Target"] = e.ImpactedObjectName;
            Dictionary<string,object> position = new Dictionary<string, object>();
            position["X"] = e.Position.X;
            position["Y"] = e.Position.X;
            position["Z"] = e.Position.X;
            data["Pos"] = position;
            data["Speed"] = e.Speed;
            Callback(data);
        }

        private void LocalVehicle_CheckpointPassed(object sender, Spectrum.API.Game.EventArgs.Vehicle.CheckpointHitEventArgs e)
        {
            data = new Dictionary<string, object>();
            data["Level"] = Game.LevelName;
            data["Mode"] = Game.CurrentMode;
            data["Real Time"] = DateTime.Now;
            data["Time"] = sw.Elapsed.TotalSeconds;
            data["Event"] = "checkpoint";
            data["Checkpoint Index"] = e.CheckpointIndex;
            data["TrackT"] = e.TrackT;
            Callback(data);
        }

        private void LocalVehicle_Split(object sender, Spectrum.API.Game.EventArgs.Vehicle.SplitEventArgs e)
        {
            data = new Dictionary<string, object>();
            data["Level"] = Game.LevelName;
            data["Mode"] = Game.CurrentMode;
            data["Real Time"] = DateTime.Now;
            data["Time"] = sw.Elapsed.TotalSeconds;
            data["Event"] = "split";
            data["Penetration"] = e.Penetration;
            data["Separation Speed"] = e.SeparationSpeed;
            Callback(data);
        }
    
        private void LocalVehicle_TrickCompleted(object sender, Spectrum.API.Game.EventArgs.Vehicle.TrickCompleteEventArgs e)
        {
            data = new Dictionary<string, object>();
            data["Level"] = Game.LevelName;
            data["Mode"] = Game.CurrentMode;
            data["Real Time"] = DateTime.Now;
            data["Time"] = sw.Elapsed.TotalSeconds;
            data["Event"] = "trick";
            data["Points"] = e.PointsEarned;
            data["Cooldown"] = e.CooldownAmount;
            data["Grind"] = e.GrindMeters;
            data["Wallride"] = e.WallRideMeters;
            data["Ceiling"] = e.CeilingRideMeters;
            Callback(data);
        }

        public void Shutdown()
        {
            Console.WriteLine("[Telemetry] Shutting down...");
        }

        public void Update()
        {
            if (sw.IsRunning&&active) {
                car = GameObject.Find("LocalCar");
                
                car_rb = car.GetComponent<Rigidbody>();
                car_log =   car.GetComponent<CarLogic>();

                data = new Dictionary<string, object>();
                
                data["Level"] = Game.LevelName;
                data["Mode"] = Game.CurrentMode;
                data["Real Time"] = DateTime.Now;
                data["Event"] = "update";
                data["Time"]= sw.Elapsed.TotalSeconds;
                data["Speed_KPH"] = LocalVehicle.VelocityKPH;
                data["Heat"] = LocalVehicle.HeatLevel;


                Dictionary<string, object> rotation = new Dictionary<string, object>();
                rotation["X"] = car.transform.rotation.x * 180;
                rotation["Y"] = car.transform.rotation.y * 180;
                rotation["Z"] = car.transform.rotation.z * 180;
                rotation["W"] = car.transform.rotation.w * 180;
                data["Rot"] = rotation;

                Dictionary<string, object> position = new Dictionary<string, object>();
                position["X"] = car.transform.position.x;
                position["Y"] = car.transform.position.y;
                position["Z"] = car.transform.position.z;
                data["Pos"] = position;

                Dictionary<string, object> velocity = new Dictionary<string, object>();
                velocity["X"] = car_rb.velocity.x;
                velocity["Y"] = car_rb.velocity.y;
                velocity["Z"] = car_rb.velocity.z;
                data["Vel"] = velocity;

                Dictionary<string, object> angular_velocity = new Dictionary<string, object>();
                angular_velocity["X"] = car_rb.angularVelocity.x;
                angular_velocity["Y"] = car_rb.angularVelocity.y;
                angular_velocity["Z"] = car_rb.angularVelocity.z;
                data["Ang Vel"] = angular_velocity;

                Dictionary<string, object> inputs = new Dictionary<string, object>();
                inputs["Boost"] = car_log.CarDirectives_.boost_;
                inputs["Steer"] = car_log.CarDirectives_.steer_;
                inputs["Grip"] = car_log.CarDirectives_.grip_;
                inputs["Gas"] = car_log.CarDirectives_.gas_;
                inputs["Brake"] = car_log.CarDirectives_.brake_;

                Dictionary<string, object> rotation_ctl = new Dictionary<string, object>();
                rotation_ctl["X"] = car_log.CarDirectives_.rotation_.x;
                rotation_ctl["Y"] = car_log.CarDirectives_.rotation_.y;
                rotation_ctl["Z"] = car_log.CarDirectives_.rotation_.z;
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