// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.
using System;
using AMD.Schola.Training;

internal static class Program
{
    static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--self-test")
        {
            CheckLifecycle();
            return;
        }
        int port = args.Length == 0 ? 50051 : int.Parse(args[0]);
        var server = new GymStartupServer();
        try
        {
            server.Start(port);
            Console.WriteLine("READY " + server.Port);
            Console.ReadLine();
            Console.WriteLine("STARTUPS " + server.StartupCount);
        }
        finally
        {
            server.Stop();
            server.Stop();
        }
    }

    static void CheckLifecycle()
    {
        var first = new GymStartupServer();
        var second = new GymStartupServer();
        try
        {
            first.Start(0);
            int port = first.Port;
            bool rejected = false;
            try { first.Start(port); }
            catch (InvalidOperationException) { rejected = true; }
            if (!rejected) { throw new Exception("Duplicate Start should fail."); }
            rejected = false;
            try { second.Start(port); }
            catch { rejected = true; }
            if (!rejected || second.IsRunning) { throw new Exception("Occupied port should fail cleanly."); }
            first.Stop();
            first.Stop();
            second.Start(port); // Failed-start instance can retry after the port is released.
            second.Stop();
            first.Start(port); // Same object can restart after Stop.
            first.Stop();
            if (first.IsRunning || first.Port != 0) { throw new Exception("Cleanup state is wrong."); }
            Console.WriteLine("PASS duplicate Start, failed-start recovery, idempotent Stop, and same-object restart");
        }
        finally
        {
            first.Stop();
            second.Stop();
        }
    }
}
