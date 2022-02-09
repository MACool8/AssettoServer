using System.Numerics;
using AssettoServer.Network.Packets.Incoming;
using AssettoServer.Network.Packets.Outgoing;
using AssettoServer.Network.Packets.Shared;
using AssettoServer.Network.Tcp;
using AssettoServer.Server;
using Serilog;

namespace LightCommunicationPlugin
{
    internal class EntryCarLight
    {
        private readonly ACServer _server;
        private readonly EntryCar _entryCar;
        public int LightFlashCount { get; internal set; }

        private long LastLightFlashTime { get; set; } = 0;
        private long LastCommunicationTime { get; set; } = 0;

        /// <summary>
        /// Constructor for initialising 
        /// </summary>
        /// <param name="entryCar"></param>
        internal EntryCarLight(EntryCar entryCar)
        {
            _server = entryCar.Server;
            _entryCar = entryCar;
            _entryCar.PositionUpdateReceived += OnPositionUpdateReceived;
        }

        /// <summary>
        /// Checks on every PositionUpdate weather someone flashed their lights atleast 2 times and waited a bit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="positionUpdate"></param>
        private void OnPositionUpdateReceived(EntryCar sender, in PositionUpdateIn positionUpdate)
        {
            long currentTick = Environment.TickCount64;
            if (((_entryCar.Status.StatusFlag & CarStatusFlags.LightsOn) == 0 && (positionUpdate.StatusFlag & CarStatusFlags.LightsOn) != 0) || ((_entryCar.Status.StatusFlag & CarStatusFlags.HighBeamsOff) == 0 && (positionUpdate.StatusFlag & CarStatusFlags.HighBeamsOff) != 0))
            {
                LastLightFlashTime = currentTick;
                LightFlashCount++;
            }

            if (currentTick - LastLightFlashTime > 700 && LightFlashCount > 0)
            {
                if (LightFlashCount > 1)
                {
                    if (LightFlashCount > 4)
                        LightFlashCount = 4;

                    if (currentTick - LastCommunicationTime > 5000)
                    {
                        // using a cached Value as race conditions can zero out LightFlashCount before task was run.
                        int cachedLFC = LightFlashCount; 
                        Task.Run(() => MessageNearbyCar(cachedLFC, sender));
                        LastCommunicationTime = currentTick;
                    } 
                    else
                    {
                        SendMessage(sender, "You tried to sent messages to quickly. Wait atleast 5 seconds between LightCommunications.");
                    }
                }
                LightFlashCount = 0;
            }
        }

        /// <summary>
        /// Checks weather the second car is in front of the first one. Returns true if SecondCar is in front.
        /// </summary>
        /// <param name="FirstCar"></param>
        /// <param name="SecondCar"></param>
        /// <returns>true when SecondCar is in front of the SecondCar. Else false</returns>
        private bool IsInFront(EntryCar FirstCar, EntryCar SecondCar)
        {

            float challengedAngle = (float)(Math.Atan2(_entryCar.Status.Position.X - SecondCar.Status.Position.X, _entryCar.Status.Position.Z - SecondCar.Status.Position.Z) * 180 / Math.PI);
            if (challengedAngle < 0)
                challengedAngle += 360;
            float challengedRot = SecondCar.Status.GetRotationAngle();

            challengedAngle += challengedRot;
            challengedAngle %= 360;

            if (challengedAngle > 90 && challengedAngle < 270)
                return true;
            
            return false;
        }

        /// <summary>
        /// Takes the car and sends a server message to the player in this car.
        /// </summary>
        /// <param name="car">Targer Car</param>
        /// <param name="message">The message to be sent</param>
        private void SendMessage(EntryCar car, string message)
        {
            if (car.Client != null)
                car.Client.SendPacket(new ChatMessage { SessionId = 255, Message = message });
        }


        /// <summary>
        /// Determines the nearest car in a radius and sends a message fitting to the MsgNr.
        /// </summary>
        /// <param name="MsgNr">Which MsgNr should be displayed.</param>
        /// <param name="sender">Who is the sender.</param>
        private void MessageNearbyCar(int MsgNr, EntryCar sender)
        {
            EntryCar? receiver = null;
            float distanceSquared = 30 * 30;

            if (MsgNr < 2 || MsgNr > 4)
            {
                Log.Debug($"[LightCommunicationPlugin] {_entryCar.Client.Name} is trying to send Msg {MsgNr} and it doesn't exist.");
                return;
            }

            foreach (EntryCar car in _server.EntryCars)
            {
                ACTcpClient? carClient = car.Client;
                if (carClient != null && car != sender)
                {
                    float distanceToCar = Vector3.DistanceSquared(car.Status.Position, sender.Status.Position);
                    if (distanceToCar < distanceSquared)
                    {
                        receiver = car;
                        // set new shortest distance which needs to be beaten
                        distanceSquared = distanceToCar;
                    }
                }
            }

            if (receiver == null)
            {
                SendMessage(sender, "Noone is nearby.");
            }
            else
            {
                string senderName = sender.Client.Name ?? "Unknown";
                string receiverName = receiver.Client.Name ?? "Unknown";

                switch (MsgNr)
                {
                    case 2:
                        SendMessage(sender, $"You said hi to {receiverName}");
                        SendMessage(receiver, $"{senderName} said Hi.");
                        break;
                    case 3:
                        if(IsInFront(sender, receiver))
                        {
                            SendMessage(sender, $"You asked {receiverName} to let you pass.");
                            SendMessage(receiver, $"{senderName} asked you to let him pass.");
                        }
                        else
                        {
                            SendMessage(sender, $"You told {receiverName} to pass you.");
                            SendMessage(receiver, $"{senderName} says you can pass him.");
                        }
                        break;
                    case 4:
                        SendMessage(sender, $"You told {receiverName} that you have problems.");
                        SendMessage(receiver, $"{senderName} says he got problems.");
                        break;
                    default:
                        // do nothing
                        break;
                }

            }
                
        }
    }
}
