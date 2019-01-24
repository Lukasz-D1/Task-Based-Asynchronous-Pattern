using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Task_Based_Asynchronous_Pattern
{
    class Program
    {
        static void Main(string[] args)
        {
            //foo(); // podkreslone na zielono bo wywolujemy syncrhonicznie metode ktora jest asynchroniczna

            // W taki sposob powinnismy uruchamiac metody asynchroniczne - nic sie nie podkresla i mozemy na to zadanie poczekac (task - zadanie)
            Task t1 = foo();

            // W jaki sposob czekamy na jedno konkretne zadanie? W nastepujacy sposob:
            //t1.Wait(); 

            Task t2 = foo();
            //t2.Wait(); 

            // Mozna tez poczekac na wiele zadan naraz. 
            // Task.WaitAll pobiera jako argument tablice taskow, ktora mozemy zadeklarowac inline - musimy powiedziec temu wait all na ktore watki ma czekac.
            // Task.WaitAll(new Task[]{ t1, t2});
            // Wait oznacza czekanie na wykonanie zadania - sluzy do synchronizacji watkow asynchronicznych z watkiem glownym (i reszta w sumie tez).

            // Jesli np. nie poczekamy na watek serwera to nie dojdzie do jego wykonania (watek glowny i metody foo skoncza sie szybciej).
            Task server = serverTask();
            Task client = clientTask("testowa wiadmosc1");

            // Wykorzystane WaitAll, w ktorym podajemy tablice skladajaca sie z konkretnych taskow, ktore wymagaja "poczekania".
            Task.WaitAll(new Task[] { t1, t2, server, client });
        }

        static async Task foo()
        {
            // await foo bedzie rekurencyjnie wywolywal sie w nieskonczononsc i zwroci stack overflow exception - nie robic tak
            // await foo();
            Console.WriteLine("foo");
        }

        static async Task serverTask()
        {
            // Klasycznie odpalamy serwer.
            TcpListener server = new TcpListener(IPAddress.Any, 2048);
            server.Start();
            while (true)
            {
                // Oczekujemy na klienta. W miedzyczasie mozemy wykonywac inne operacje (ale tutaj nie sa one zaimplementowane).
                TcpClient client = await server.AcceptTcpClientAsync();
                byte[] buffer = new byte[1024];

                // Oczekujemy na asynchronicznie odebranie strumienia danych od zaakceptowanego klienta.
                // Korzystamy z wyrazenia lambda do stworzenia funkcji inline.
                await client.GetStream().ReadAsync(buffer, 0, buffer.Length).ContinueWith(async (t) =>
                {
                    // Wynikiem tego taska jest dlugosc pobranych danych.
                    int i = t.Result;
                    Console.WriteLine(i);
                    while (true)
                    {
                        // W zwiazku z tym mozemy jej uzyc do asynchronicznego odpisania klientowi.
                        await client.GetStream().WriteAsync(buffer, 0, i);
                        i = await client.GetStream().ReadAsync(buffer, 0, buffer.Length);
                    }
                });

            }
        }


        static async Task clientTask(string message)
        {
            TcpClient client = new TcpClient();
            // Asynchronicznie sie laczymy z serwerem.
            await client.ConnectAsync(IPAddress.Loopback, 2048);
            while (true)
            {
                byte[] buffer = Encoding.ASCII.GetBytes(message);
                await client.GetStream().WriteAsync(buffer, 0, buffer.Length);
                byte[] buffer1 = new byte[1024];
                int t = await client.GetStream().ReadAsync(buffer1, 0, 1024);
                Console.WriteLine(ASCIIEncoding.ASCII.GetString(buffer1).Substring(0, t));
                Thread.Sleep(1000);
            }
        }

    }
}
