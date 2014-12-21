using System;

namespace Notifier
{
    class Program
    {
        static void Main(string[] args)
        {
            var notify = new Notify(() => Console.WriteLine("I am the master"));

            notify.Start();

            Console.ReadKey();
        }
    }
}
