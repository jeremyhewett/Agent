using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Quobject.EngineIoClientDotNet.ComponentEmitter;
using Quobject.SocketIoClientDotNet.Client;

namespace Agent
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var socket1 = CreateSocket(1);
            var socket2 = CreateSocket(2);
            
            Console.ReadLine();
        }

        private static Socket CreateSocket(int id)
        {
            var options = new IO.Options();
            //options.Transports = ImmutableList.Create("polling");
            options.ExtraHeaders = new Dictionary<string, string>() {{ "token", "123xyz" }};
            var socket = IO.Socket("http://localhost:8000", options);
            
            socket.On(Socket.EVENT_CONNECT, () =>
            {
                Console.WriteLine(id + " Connected");
            });
            
            socket.On(Socket.EVENT_DISCONNECT, () =>
            {
                Console.WriteLine(id + " Disconnected");
            });

            socket.On(Socket.EVENT_MESSAGE, (data) =>
            {
                Console.WriteLine(id + " received: " + data);
                socket.Send("ack");
                socket.Send(data);
            });
            
            socket.On("execute", new Acks2ListenerImpl((job, cb) =>
            {
                Console.WriteLine(id + " received: Execute " + job);
                var iack = (IAck)cb;
                iack.Call("received");
                var fakeJob = Task<bool>.Factory.StartNew(() =>
                {
                    Thread.Sleep(5000);
                    return true;
                });
                bool success = fakeJob.Result;
                var result = new
                {
                    id = ((dynamic) job).id,
                    success = success
                };
                Console.WriteLine("Emitting job complete");
                socket.Emit("complete", JsonConvert.SerializeObject(result));
            }));
            
            socket.On(Socket.EVENT_ERROR, (err) =>
            {
                Console.WriteLine($"Error: {err}");
            });

            return socket;
        }
        
        public class Acks2ListenerImpl : IListener
        {
            private static int id_counter = 0;
            private int Id;
            private readonly Action<object,object> fn;

            public Acks2ListenerImpl(Action<object,object> fn)
            {

                this.fn = fn;
                this.Id = id_counter++;
            }

            public void Call(params object[] args)
            {
                var arg1 = args.Length > 0 ? args[0] : null;
                var arg2 = args.Length > 1 ? args[1] : null;

                fn(arg1, arg2);
            }


            public int CompareTo(IListener other)
            {
                return GetId().CompareTo(other.GetId());
            }

            public int GetId()
            {
                return Id;
            }
        }
    }

}