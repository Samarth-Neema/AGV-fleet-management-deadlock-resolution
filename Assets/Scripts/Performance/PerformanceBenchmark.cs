using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public class PerformanceBenchmark : MonoBehaviour
{
    [Header("Fleet Sizes")]
    public int[] fleetSizes = { 5, 10, 15, 20, 25, 30 };

    [Header("Benchmark Settings")]
    public int repetitions = 5000;

    private class BenchmarkResult
    {
        public int fleetSize;
        public double dictionaryTime;
        public double dfsTime;
        public double totalTime;
    }

    [ContextMenu("Run Fleet Benchmark")]
public void RunFleetBenchmark()
{
    List<BenchmarkResult> results =
        new List<BenchmarkResult>();

    foreach (int fleetSize in fleetSizes)
    {
        double dictionaryTime =
            BenchmarkDictionary(fleetSize);

        double dfsTime =
            BenchmarkDFS(fleetSize);

        double totalTime =
            dictionaryTime + dfsTime;

        results.Add(new BenchmarkResult
        {
            fleetSize = fleetSize,
            dictionaryTime = dictionaryTime,
            dfsTime = dfsTime,
            totalTime = totalTime
        });
    }

    // Print header
    UnityEngine.Debug.Log(
        "\n========== AGV FLEET PERFORMANCE =========="
    );

    UnityEngine.Debug.Log(
        "Repetitions: " + repetitions
    );

    UnityEngine.Debug.Log(
        "Fleet | Dictionary(us) | DFS(us) | Total(us)"
    );

    UnityEngine.Debug.Log(
        "--------------------------------------------"
    );

    // Print each result as ONE log entry
    foreach (BenchmarkResult result in results)
    {
        UnityEngine.Debug.Log(
            string.Format(
                " {0,4} | {1,14:F4} | {2,7:F4} | {3,9:F4}",
                result.fleetSize,
                result.dictionaryTime,
                result.dfsTime,
                result.totalTime
            )
        );
    }

    UnityEngine.Debug.Log(
        "============================================"
    );

    UnityEngine.Debug.Log(
        "Benchmark Complete"
    );

    SaveCSV(results);
}

    private double BenchmarkDictionary(int fleetSize)
    {
        Dictionary<int, int> data =
            new Dictionary<int, int>();

        for (int i = 0; i < fleetSize; i++)
        {
            data[i] = i * 10;
        }

        Stopwatch stopwatch =
            new Stopwatch();

        stopwatch.Start();

        int dummy = 0;

        for (int r = 0; r < repetitions; r++)
        {
            int key = r % fleetSize;

            if (data.TryGetValue(key, out int value))
            {
                dummy += value;
            }
        }

        stopwatch.Stop();

        GC.KeepAlive(dummy);

        return stopwatch.Elapsed.TotalMilliseconds * 1000.0
               / repetitions;
    }

    private double BenchmarkDFS(int fleetSize)
    {
        Dictionary<int, HashSet<int>> graph =
            new Dictionary<int, HashSet<int>>();

        // Create circular wait graph:
        // 0 -> 1 -> 2 -> ... -> last -> 0

        for (int i = 0; i < fleetSize; i++)
        {
            graph[i] =
                new HashSet<int>
                {
                    (i + 1) % fleetSize
                };
        }

        Stopwatch stopwatch =
            new Stopwatch();

        stopwatch.Start();

        bool dummy = false;

        for (int r = 0; r < repetitions; r++)
        {
            dummy = DetectCycle(graph, fleetSize);
        }

        stopwatch.Stop();

        GC.KeepAlive(dummy);

        return stopwatch.Elapsed.TotalMilliseconds * 1000.0
               / repetitions;
    }

    private bool DetectCycle(
        Dictionary<int, HashSet<int>> graph,
        int fleetSize)
    {
        HashSet<int> visited =
            new HashSet<int>();

        HashSet<int> recursionStack =
            new HashSet<int>();

        for (int i = 0; i < fleetSize; i++)
        {
            if (!visited.Contains(i))
            {
                if (DFS(
                    i,
                    graph,
                    visited,
                    recursionStack))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool DFS(
        int current,
        Dictionary<int, HashSet<int>> graph,
        HashSet<int> visited,
        HashSet<int> recursionStack)
    {
        if (recursionStack.Contains(current))
            return true;

        if (visited.Contains(current))
            return false;

        visited.Add(current);
        recursionStack.Add(current);

        if (graph.ContainsKey(current))
        {
            foreach (int neighbour in graph[current])
            {
                if (DFS(
                    neighbour,
                    graph,
                    visited,
                    recursionStack))
                {
                    return true;
                }
            }
        }

        recursionStack.Remove(current);

        return false;
    }

    private void SaveCSV(
        List<BenchmarkResult> results)
    {
        string path =
            Path.Combine(
                Application.persistentDataPath,
                "AGV_Fleet_Performance.csv"
            );

        using (StreamWriter writer =
            new StreamWriter(path))
        {
            writer.WriteLine(
                "FleetSize,Dictionary_us,DFS_us,Total_us"
            );

            foreach (BenchmarkResult result in results)
            {
                writer.WriteLine(
                    $"{result.fleetSize}," +
                    $"{result.dictionaryTime:F4}," +
                    $"{result.dfsTime:F4}," +
                    $"{result.totalTime:F4}"
                );
            }
        }

        UnityEngine.Debug.Log(
            "CSV saved to:\n" + path
        );
    }
}