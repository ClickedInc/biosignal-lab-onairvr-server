using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using UnityEngine;

public class MPPMotionDataReader {
    public enum Type {
        Raw,
        PerfMetric
    }

    private const string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";
    private const string LINE_SPLIT_RE = @"\r\n|\n\r|\n|\r";

    private static readonly char[] TRIM_CHARS = { '\"' };

    private string[] _lines;
    private string[] _header;

    public string filepath { get; private set; }
    public Type type => ContainsKey("prediction_time") ? Type.Raw : Type.PerfMetric;
    public int count => (_lines?.Length ?? 1) - 1;

    public MPPMotionDataReader(string path) {
        filepath = path;

        var text = File.ReadAllText(path);
        if (string.IsNullOrEmpty(text)) { return; }

        _lines = text.Split('\r').Where((line) => string.IsNullOrWhiteSpace(line) == false).ToArray();
        if (_lines.Length < 1) { return; }

        _header = _lines[0].Split(',');
    }

    public Dictionary<string, object> Read(int index) {
        var line = index + 1;
        if (line >= _lines.Length) { return null; }

        var data = new Dictionary<string, object>();
        var cols = _lines[line].Split(',');

        for (var col = 0; col < _header.Length && col < cols.Length; col++) {
            var value = cols[col];
            value = value.TrimStart(TRIM_CHARS).TrimEnd(TRIM_CHARS).Replace("\\", "");

            var obj = double.TryParse(value, out double f) ? f : (object)value;
            data[_header[col]] = obj;
        }
        return data;
    }

    public bool ContainsKey(string key) {
        foreach (var col in _header) {
            if (col == key) {
                return true;
            }
        }
        return false;
    }
}
