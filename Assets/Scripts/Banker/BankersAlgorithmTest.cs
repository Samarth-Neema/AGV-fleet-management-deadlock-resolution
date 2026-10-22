using System.Collections.Generic;
using UnityEngine;

public class BankersAlgorithmTest : MonoBehaviour
{
    [ContextMenu("Run Banker Test")]
    public void RunBankerTest()
    {
        Debug.Log("========================================");
        Debug.Log("        BANKER'S ALGORITHM TEST");
        Debug.Log("========================================");

        RunSafeRequestTest();

        Debug.Log("");

        RunUnsafeRequestTest();

        Debug.Log("========================================");
        Debug.Log("        BANKER TEST COMPLETE");
        Debug.Log("========================================");
    }

    private void RunSafeRequestTest()
    {
        BankersAlgorithm banker =
            CreateClassicExample();

        bool initialSafe =
            banker.IsSafe(out List<int> sequence);

        Debug.Log(
            "Initial System Safe: " +
            initialSafe
        );

        if (initialSafe)
        {
            Debug.Log(
                "Safe Sequence: " +
                FormatSequence(sequence)
            );
        }

        // AGV 1 requests [1,0,2]
        int[] request = { 1, 0, 2 };

        bool granted =
            banker.TryRequest(
                1,
                request,
                out string message,
                out List<int> newSequence
            );

        Debug.Log(
            "AGV 1 Request [1,0,2]: " +
            message
        );

        if (granted && newSequence != null)
        {
            Debug.Log(
                "New Safe Sequence: " +
                FormatSequence(newSequence)
            );
        }
    }

    private void RunUnsafeRequestTest()
    {
        BankersAlgorithm banker =
            CreateClassicExample();

        // AGV 0 requests [0,0,2]
        // This request is available,
        // but makes the resulting state unsafe.
        int[] request = { 0, 0, 2 };

        bool granted =
            banker.TryRequest(
                0,
                request,
                out string message,
                out List<int> sequence
            );

        Debug.Log(
            "AGV 0 Request [0,0,2]: " +
            message
        );

        if (!granted)
        {
            Debug.Log(
                "Correct behavior: request was rejected."
            );
        }
    }

    private BankersAlgorithm CreateClassicExample()
    {
        BankersAlgorithm banker =
            new BankersAlgorithm();

        // 5 AGVs
        // 3 resource types
        banker.Initialize(5, 3);

        int[] available =
        {
            3, 3, 2
        };

        int[,] allocation =
        {
            { 0, 1, 0 },
            { 2, 0, 0 },
            { 3, 0, 2 },
            { 2, 1, 1 },
            { 0, 0, 2 }
        };

        int[,] maximum =
        {
            { 7, 5, 3 },
            { 3, 2, 2 },
            { 9, 0, 2 },
            { 2, 2, 2 },
            { 4, 3, 3 }
        };

        banker.SetState(
            available,
            allocation,
            maximum,
            out string error
        );

        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogError(
                "Banker state error: " +
                error
            );
        }

        return banker;
    }

    private string FormatSequence(
        List<int> sequence)
    {
        if (sequence == null ||
            sequence.Count == 0)
        {
            return "None";
        }

        string result = "";

        for (int i = 0; i < sequence.Count; i++)
        {
            result += "AGV " + sequence[i];

            if (i < sequence.Count - 1)
                result += " -> ";
        }

        return result;
    }
}