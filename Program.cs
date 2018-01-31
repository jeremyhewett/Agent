using System;
using Quobject.SocketIoClientDotNet.Client;

namespace Agent
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var socket = IO.Socket("http://localhost:8000");
            socket.On(Socket.EVENT_CONNECT, () =>
            {
                Console.WriteLine("Connected");
            });

            socket.On("command", (data) =>
            {
                Console.WriteLine(data);
                socket.Emit("response");
                socket.Disconnect();
            });
            Console.ReadLine();
        }
    }
}