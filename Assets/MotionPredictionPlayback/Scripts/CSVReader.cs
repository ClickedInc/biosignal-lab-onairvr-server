
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class CSVReader
{
    public static int lineLength;

    static string filePath;
    static string[] lines;
    static string[] header;
    static string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";

    static string LINE_SPLIT_RE = @"\r\n|\n\r|\n|\r";
    static char[] TRIM_CHARS = { '\"' };

    public static List<Dictionary<string, object>> Read(string path)
    {
        var list = new List<Dictionary<string, object>>();

        string data = System.IO.File.ReadAllText(path);
        if (data == null)
            Debug.LogAssertion("Filepath is null");

        var lines = Regex.Split(data, LINE_SPLIT_RE);

        if (lines.Length <= 1) return list;

        var header = Regex.Split(lines[0], SPLIT_RE);
        for (var i = 1; i < lines.Length; i++)
        {
            var values = Regex.Split(lines[i], SPLIT_RE);
            if (values.Length == 0 || values[0] == "") continue;

            var entry = new Dictionary<string, object>();
            for (var j = 0; j < header.Length && j < values.Length; j++)
            {
                string value = values[j];
                value = value.TrimStart(TRIM_CHARS).TrimEnd(TRIM_CHARS).Replace("\\", "");
                object finalvalue = value;
                double f;
                if (double.TryParse(value, out f))
                {
                    finalvalue = f;
                }
                entry[header[j]] = finalvalue;
            }
            list.Add(entry);
        }
        return list;
    }

    public static void Init(string path)
    {
        var csvData = System.IO.File.ReadAllText(path);

        if (csvData == null)
            Debug.LogAssertion("Filepath is null");

        filePath = path;

        lines = csvData.Split('\r');

        lineLength = lines.Length;

        header = lines[0].Split(',');
    }

    private static Dictionary<string, object> entry = new Dictionary<string, object>();
    public static Dictionary<string, object> ReadLine(int lineIndex)
    {
        if (lineLength <= lineIndex + 1)
        {
            return null;
        }

        var values = lines[lineIndex + 1].Split(',');

        for (var j = 0; j < header.Length && j < values.Length; j++)
        {
            string value = values[j];
            value = value.TrimStart(TRIM_CHARS).TrimEnd(TRIM_CHARS).Replace("\\", "");
            object finalvalue = value;
            double f;
            if (double.TryParse(value, out f))
            {
                finalvalue = f;
            }
            entry[header[j]] = finalvalue;
        }

        return entry;
    }

    public static bool GetExistKey(string key)
    {
        foreach (string item in header)
        {
            if (item == key)
                return true;
        }

        return false;
    }
}
