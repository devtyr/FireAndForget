﻿using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FireAndForget.Core.Models;
using System;

namespace FireAndForget.TestClient
{
    public interface ITask
    {
        string MessageType { get; }
        DateTime? ExecuteAt { get; set; }
        object Data { get; set; }
        bool IsBulk { get; set; }
    }

    public class DefaultTask : ITask
    {
        public string MessageType { get { return this.GetType().Name; } }
        public DateTime? ExecuteAt { get; set; }
        public object Data { get; set; }
        public bool IsBulk { get; set; }
    }

    public class TestTask : ITask
    {
        public string MessageType { get { return this.GetType().Name; } }
        public DateTime? ExecuteAt { get; set; }
        public object Data { get; set; }
        public bool IsBulk { get; set; }
    }

    class Program
    {
        static HttpClient client = new HttpClient();

        static void SubmitMessage(ServiceBusMessage busmessage)
        {
            var json = JsonConvert.SerializeObject(busmessage);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var message = client.PostAsync("http://localhost:2900/api/v1/enqueue/", httpContent);
            message.Result.EnsureSuccessStatusCode();
        }

        static void Main(string[] args)
        {
            System.Console.WriteLine("Starting ....");

            var defaultTask = Task.Factory.StartNew(() => FillDefaultQueue());
            var testTask = Task.Factory.StartNew(() => FillTestQueue());
            var delayedTask = Task.Factory.StartNew(() => SendDelayedTasks());

            Task.WaitAll(new[] { defaultTask, testTask, delayedTask });
        }

        static void FillDefaultQueue()
        {
            for (int i = 0; i < 500; i++)
            {
                DefaultTask task = new DefaultTask();
                task.Data = "This is some data for the default handler" + i;
                SubmitMessage(new ServiceBusMessage() { Data = task });
            }
        }

        static void FillTestQueue()
        {
            for (int i = 0; i < 500; i++)
            {
                TestTask task = new TestTask();
                task.Data = "Data for the test handler" + i;
                SubmitMessage(new ServiceBusMessage() { Data = task });
            }
        }

        static void SendDelayedTasks()
        {
            for (int i = 0; i < 20; i++)
            {
                TestTask task = new TestTask();
                task.Data = "THIS TASK IS DELAYED!!!!";
                task.ExecuteAt = DateTime.Now.AddMinutes(1);
                task.IsBulk = true;
                SubmitMessage(new ServiceBusMessage() { Data = task });
            }
        }
    }
}
