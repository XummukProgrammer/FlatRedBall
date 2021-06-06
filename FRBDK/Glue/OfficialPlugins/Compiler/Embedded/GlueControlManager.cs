﻿{CompilerDirectives}


using FlatRedBall.Screens;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace {ProjectNamespace}
{

    public class GlueVariableSetData
    {
        public string VariableName { get; set; }
        public string Value { get; set; }
        public string Type { get; set; }
    }

    public class GlueControlManager
    {
        bool isRunning;
        private TcpListener listener;
        private Queue<string> GameToGlueCommands = new Queue<string>();
        
        public static GlueControlManager Self { get; private set; }
        
    
        /// <summary>
        /// Stores all commands that have been sent from Glue to game so they can be re-run when a Screen is re-loaded.
        /// </summary>
        private Queue<string> GlueToGameCommands = new Queue<string>();

        public GlueControlManager(int port)
        {
            Self = this;
            listener = new TcpListener(IPAddress.Any, port);
        }

        public void Start()
        {
            Thread serverThread = new Thread(new ThreadStart(Run));

            serverThread.Start();
        }

        private void Run()
        {
            isRunning = true;

            listener.Start();

            while (isRunning)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    HandleClient(client);

                    client.Close();
                }
                catch (System.Exception e)
                {
                    if (isRunning)
                    {
                        throw e;
                    }
                }
            }

            isRunning = false;

            listener.Stop();
        }

        public void Kill()
        {
            isRunning = false;
            listener.Stop();

        }

        public void SendCommandToGlue(string command)
        {
            lock(GameToGlueCommands)
            {
                GameToGlueCommands.Enqueue(command);
            }
        }

        private void HandleClient(TcpClient client)
        {
            StreamReader reader = new StreamReader(client.GetStream());
            var stringBuilder = new StringBuilder();
            while (reader.Peek() != -1)
            {
                stringBuilder.AppendLine(reader.ReadLine());
            }

            var response = ProcessMessage(stringBuilder.ToString()?.Trim());
            if (response == null)
            {
                response = "true";
            }
            byte[] messageAsBytes = System.Text.ASCIIEncoding.UTF8.GetBytes(response);
            client.GetStream().Write(messageAsBytes, 0, messageAsBytes.Length);

        }

        public void ReRunAllGlueToGameCommands()
        {
            var toProcess = GlueToGameCommands.ToArray();
            GlueToGameCommands.Clear();
            foreach (var message in toProcess)
            {
                ProcessMessage(message);
            }
        }

        private string ProcessMessage(string message)
        {
            var screen =
                FlatRedBall.Screens.ScreenManager.CurrentScreen;
            bool handledImmediately = false;

            string data = null;

            string action = message;

            if (message.Contains(":"))
            {
                data = message.Substring(message.IndexOf(":") + 1);
                action = message.Substring(0, message.IndexOf(":"));
            }

            switch (action)
            {
                case "GetCurrentScreen":
                    handledImmediately = true;
                    return screen.GetType().FullName;

                case "GetCommands":
                    handledImmediately = true;
                    string toReturn = string.Empty;
                    if (GameToGlueCommands.Count != 0)
                    {
                        lock (GameToGlueCommands)
                        {
                            toReturn = Newtonsoft.Json.JsonConvert.SerializeObject(GameToGlueCommands.ToArray());
                            GameToGlueCommands.Clear();
                        }
                    }
                    return toReturn;
            }

            if (!handledImmediately)
            {
                FlatRedBall.Instructions.InstructionManager.AddSafe(() =>
                {
                    switch (action)
                    {
                        case "RestartScreen":
                            screen.RestartScreen(true);
                            break;
                        case "ReloadGlobal":
                            GlobalContent.Reload(GlobalContent.GetFile(data));
                            break;
                        case "TogglePause":

                            if (screen.IsPaused)
                            {
                                screen.UnpauseThisScreen();
                            }
                            else
                            {
                                screen.PauseThisScreen();
                            }

                            break;

                        case "AdvanceOneFrame":
                            screen.UnpauseThisScreen();
                            var delegateInstruction = new FlatRedBall.Instructions.DelegateInstruction(() =>
                            {
                                screen.PauseThisScreen();
                            });
                            delegateInstruction.TimeToExecute = FlatRedBall.TimeManager.CurrentTime + .001;

                            FlatRedBall.Instructions.InstructionManager.Instructions.Add(delegateInstruction);
                            break;

                        case "SetSpeed":
                            var timeFactor = int.Parse(data);
                            FlatRedBall.TimeManager.TimeFactor = timeFactor / 100.0f;
                            break;

                        case "SetVariable":
                            HandleSetVariable(data);
                            GlueToGameCommands.Enqueue(message);
                            break;
                        case "AddObject":
                            HandleAddObject(data);
                            GlueToGameCommands.Enqueue(message);
                            break;
                        case "SetEditMode":
                            HandleSetEditMode(data);
                            break;

                    }
                });
            }

            return "true";
        }



        private void HandleSetEditMode(string data)
        {
        var value = bool.Parse(data);
#if SupportsEditMode
            FlatRedBall.Screens.ScreenManager.IsInEditMode = value;
            if(value)
            {
                var screen =
                    FlatRedBall.Screens.ScreenManager.CurrentScreen;
                // user may go into edit mode after moving through a level and wouldn't want it to restart fully....or would they? What if they
                // want to change the Player start location. Need to think that through...

                void HandleScreenLoaded(Screen _)
                {
                    ReRunAllGlueToGameCommands();

                    FlatRedBall.Screens.ScreenManager.ScreenLoaded -= HandleScreenLoaded;
                }

                FlatRedBall.Screens.ScreenManager.ScreenLoaded += HandleScreenLoaded;

                screen?.RestartScreen(reloadContent: true, applyRestartVariables:true);
            }
#endif
    }

        public void HandleSetVariable(string data)
        {
#if IncludeSetVariable

            var screen =
                FlatRedBall.Screens.ScreenManager.CurrentScreen;

            var deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<GlueVariableSetData>(data);

            object variableValue = deserialized.Value;

            switch (deserialized.Type)
            {
                case "float":
                    variableValue = float.Parse(deserialized.Value);
                    break;
                case "int":
                    variableValue = int.Parse(deserialized.Value);
                    break;
                case "bool":
                    variableValue = bool.Parse(deserialized.Value);
                    break;
                case "double":
                    variableValue = double.Parse(deserialized.Value);
                    break;
                case "Microsoft.Xna.Framework.Color":
                    variableValue = typeof(Microsoft.Xna.Framework.Color).GetProperty(deserialized.Value).GetValue(null);
                    break;
            }

            screen.ApplyVariable(deserialized.VariableName, variableValue);
#endif
        }

        public void HandleAddObject(string data)
        {
            need to make this added to the repository despite codegen
#if IncludeSetVariable
            var deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<GlueControl.Models.NamedObjectSave>(data);
            if(deserialized.SourceType == GlueControl.Models.SourceType.Entity)
            {
                var factory = FlatRedBall.TileEntities.TileEntityInstantiator.GetFactory(deserialized.SourceClassType);
                var instance = factory?.CreateNew() as FlatRedBall.PositionedObject;
                instance.Name = deserialized.InstanceName;
                instance.Velocity = Microsoft.Xna.Framework.Vector3.Zero;
                instance.Acceleration = Microsoft.Xna.Framework.Vector3.Zero;
            }
#endif

    }


}
}
